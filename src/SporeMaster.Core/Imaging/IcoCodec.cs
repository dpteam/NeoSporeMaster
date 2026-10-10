namespace SporeMaster.Core.Imaging;

/// <summary>
/// Decodes Windows icon/cursor files (the Spore <c>cur</c> type, <c>0x02393756</c>). Supports
/// PNG-compressed entries and classic DIB entries at 32/24/8/4/1 bits per pixel.
/// </summary>
public static class IcoCodec
{
    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    /// <summary>True when the bytes look like an ICO/CUR container (both share the layout).</summary>
    public static bool LooksLikeIco(byte[] data) =>
        data.Length >= 6 && data[0] == 0 && data[1] == 0 &&
        (data[2] == 1 || data[2] == 2) && data[3] == 0 && (data[4] != 0 || data[5] != 0);

    /// <summary>
    /// Decodes one image from an ICO/CUR file. When <paramref name="index"/> is negative the largest
    /// entry is chosen.
    /// </summary>
    public static RgbaImage Decode(byte[] data, int index = -1)
    {
        if (!LooksLikeIco(data))
        {
            throw new InvalidDataException("Not an ICO/CUR file.");
        }

        int count = data[4] | (data[5] << 8);
        if (count <= 0 || 6 + (count * 16) > data.Length)
        {
            throw new InvalidDataException("ICO/CUR has no valid directory entries.");
        }

        if (index < 0)
        {
            index = SelectLargest(data, count);
        }

        if (index >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        int entry = 6 + (index * 16);
        int width = data[entry] == 0 ? 256 : data[entry];
        int height = data[entry + 1] == 0 ? 256 : data[entry + 1];
        long size = ReadU32(data, entry + 8);
        long offset = ReadU32(data, entry + 12);
        if (offset < 0 || offset + size > data.Length || size <= 0)
        {
            throw new InvalidDataException("ICO/CUR entry points outside the file.");
        }

        var payload = new byte[size];
        Array.Copy(data, offset, payload, 0, size);

        if (payload.Length >= 8 && payload.AsSpan(0, 8).SequenceEqual(PngSignature))
        {
            return PngCodec.Decode(payload);
        }

        return DecodeDib(payload, width, height);
    }

    private static int SelectLargest(byte[] data, int count)
    {
        int best = 0;
        int bestArea = -1;
        for (int i = 0; i < count; i++)
        {
            int w = data[6 + (i * 16)] == 0 ? 256 : data[6 + (i * 16)];
            int h = data[7 + (i * 16)] == 0 ? 256 : data[7 + (i * 16)];
            int area = w * h;
            if (area > bestArea)
            {
                bestArea = area;
                best = i;
            }
        }

        return best;
    }

    private static RgbaImage DecodeDib(byte[] dib, int declaredWidth, int declaredHeight)
    {
        long headerSize = ReadU32(dib, 0);
        if (headerSize != 40)
        {
            throw new InvalidDataException($"Unsupported icon DIB header size {headerSize}.");
        }

        int width = ReadI32(dib, 4);
        int doubledHeight = ReadI32(dib, 8);
        int bitCount = ReadU16(dib, 14);
        long clrUsed = ReadU32(dib, 32);

        int height = doubledHeight / 2;
        if (width <= 0 || height <= 0)
        {
            // Fall back to the directory entry when the DIB header is bogus.
            width = declaredWidth;
            height = declaredHeight;
        }

        int paletteCount = 0;
        if (bitCount <= 8)
        {
            paletteCount = (int)(clrUsed != 0 ? clrUsed : 1u << bitCount);
        }

        int paletteOffset = (int)headerSize;
        int xorOffset = paletteOffset + (paletteCount * 4);
        int xorStride = (((width * bitCount) + 31) / 32) * 4;
        int andStride = ((width + 31) / 32) * 4;
        int andOffset = xorOffset + (xorStride * height);

        if (xorOffset + (xorStride * height) > dib.Length)
        {
            throw new InvalidDataException("Icon DIB pixel data is truncated.");
        }

        var image = new RgbaImage(width, height);
        bool hasAlpha = bitCount == 32 && HasAlpha(dib, xorOffset, width, height, xorStride);

        for (int y = 0; y < height; y++)
        {
            int row = xorOffset + ((height - 1 - y) * xorStride);
            int andRow = andOffset + ((height - 1 - y) * andStride);
            for (int x = 0; x < width; x++)
            {
                byte r, g, b, a;
                switch (bitCount)
                {
                    case 32:
                        b = dib[row + (x * 4)];
                        g = dib[row + (x * 4) + 1];
                        r = dib[row + (x * 4) + 2];
                        a = hasAlpha ? dib[row + (x * 4) + 3] : MaskedAlpha(dib, andRow, andOffset, x);
                        break;
                    case 24:
                        b = dib[row + (x * 3)];
                        g = dib[row + (x * 3) + 1];
                        r = dib[row + (x * 3) + 2];
                        a = MaskedAlpha(dib, andRow, andOffset, x);
                        break;
                    case 8:
                    case 4:
                    case 1:
                    {
                        int index = ReadIndexed(dib, row, x, bitCount);
                        int p = paletteOffset + (index * 4);
                        if (p + 3 >= dib.Length)
                        {
                            r = g = b = 0;
                            a = 255;
                        }
                        else
                        {
                            b = dib[p];
                            g = dib[p + 1];
                            r = dib[p + 2];
                            a = MaskedAlpha(dib, andRow, andOffset, x);
                        }

                        break;
                    }

                    default:
                        throw new InvalidDataException($"Unsupported icon bit depth {bitCount}.");
                }

                image.SetPixel(x, y, r, g, b, a);
            }
        }

        return image;
    }

    private static bool HasAlpha(byte[] dib, int xorOffset, int width, int height, int stride)
    {
        for (int y = 0; y < height; y++)
        {
            int row = xorOffset + (y * stride);
            for (int x = 0; x < width; x++)
            {
                if (dib[row + (x * 4) + 3] != 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static byte MaskedAlpha(byte[] dib, int andRow, int andOffset, int x)
    {
        if (andRow + (x / 8) >= dib.Length)
        {
            return 255;
        }

        // The AND mask is 1bpp; a set bit means transparent.
        bool transparent = (dib[andRow + (x / 8)] & (0x80 >> (x % 8))) != 0;
        return (byte)(transparent ? 0 : 255);
    }

    private static int ReadIndexed(byte[] dib, int row, int x, int bitCount) => bitCount switch
    {
        8 => dib[row + x],
        4 => (x % 2 == 0 ? dib[row + (x / 2)] >> 4 : dib[row + (x / 2)]) & 0x0F,
        _ => (dib[row + (x / 8)] >> (7 - (x % 8))) & 0x01,
    };

    private static int ReadU16(byte[] data, int offset) => data[offset] | (data[offset + 1] << 8);

    private static uint ReadU32(byte[] data, int offset) =>
        (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));

    private static int ReadI32(byte[] data, int offset) => (int)ReadU32(data, offset);
}
