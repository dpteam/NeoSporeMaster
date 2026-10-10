using System.Buffers.Binary;
using System.IO.Compression;

namespace SporeMaster.Core.Imaging;

/// <summary>
/// A small, dependency-free PNG codec built on the BCL's zlib (<see cref="ZLibStream"/>).
/// </summary>
/// <remarks>
/// Encoding always emits 8-bit RGBA (colour type 6), which is lossless for the common case.
/// Decoding supports the widely used subset: colour types 0 (grey), 2 (RGB), 3 (palette),
/// 4 (grey+alpha) and 6 (RGBA), bit depths 1/2/4/8/16, with tRNS transparency. Adam7
/// interlacing is not supported.
/// </remarks>
public static class PngCodec
{
    private static readonly byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    // ---- encoding ----

    public static byte[] Encode(RgbaImage image)
    {
        using var buffer = new MemoryStream();
        Encode(image, buffer);
        return buffer.ToArray();
    }

    public static void Encode(RgbaImage image, Stream output)
    {
        output.Write(Signature);

        // IHDR: width, height, bit depth 8, colour type 6 (RGBA), deflate, no filter method, no interlace.
        var ihdr = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(0), (uint)image.Width);
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(4), (uint)image.Height);
        ihdr[8] = 8;
        ihdr[9] = 6;
        ihdr[10] = 0;
        ihdr[11] = 0;
        ihdr[12] = 0;
        WriteChunk(output, "IHDR", ihdr);

        // Raw scanlines: one filter byte (0 = none) followed by RGBA rows.
        int stride = image.Width * 4;
        var raw = new byte[(stride + 1) * image.Height];
        int position = 0;
        for (int y = 0; y < image.Height; y++)
        {
            raw[position++] = 0;
            Array.Copy(image.Pixels, y * stride, raw, position, stride);
            position += stride;
        }

        byte[] compressed;
        using (var compressedStream = new MemoryStream())
        {
            using (var zlib = new ZLibStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
            {
                zlib.Write(raw);
            }

            compressed = compressedStream.ToArray();
        }

        WriteChunk(output, "IDAT", compressed);
        WriteChunk(output, "IEND", Array.Empty<byte>());
    }

    private static void WriteChunk(Stream output, string type, byte[] data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, (uint)data.Length);
        output.Write(length);

        var typeBytes = new byte[4];
        for (int i = 0; i < 4; i++)
        {
            typeBytes[i] = (byte)type[i];
        }

        output.Write(typeBytes);
        output.Write(data);

        uint crc = Crc32.Compute(typeBytes, 0, 4);
        crc = Crc32.Update(crc, data, 0, data.Length);
        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        output.Write(crcBytes);
    }

    // ---- decoding ----

    public static RgbaImage Decode(Stream stream)
    {
        Span<byte> signature = stackalloc byte[8];
        ReadExactly(stream, signature);
        if (!signature.SequenceEqual(Signature))
        {
            throw new InvalidDataException("Not a PNG file (bad signature).");
        }

        int width = 0, height = 0, bitDepth = 8, colorType = 6, interlace = 0;
        byte[]? palette = null;
        byte[]? transparency = null;
        using var idat = new MemoryStream();

        var header = new byte[8];
        var crcBuffer = new byte[4];
        while (true)
        {
            ReadExactly(stream, header);
            int length = (int)BinaryPrimitives.ReadUInt32BigEndian(header);
            string type = System.Text.Encoding.ASCII.GetString(header, 4, 4);

            if (length < 0 || length > 0x7FFFFFF)
            {
                throw new InvalidDataException("PNG chunk length is invalid.");
            }

            byte[] data = new byte[length];
            ReadExactly(stream, data);
            ReadExactly(stream, crcBuffer); // CRC (not verified)

            switch (type)
            {
                case "IHDR":
                    width = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0));
                    height = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(4));
                    bitDepth = data[8];
                    colorType = data[9];
                    interlace = data[12];
                    break;
                case "PLTE":
                    palette = data;
                    break;
                case "tRNS":
                    transparency = data;
                    break;
                case "IDAT":
                    idat.Write(data);
                    break;
                case "IEND":
                    goto done;
            }
        }

    done:
        if (width <= 0 || height <= 0)
        {
            throw new InvalidDataException("PNG image has no dimensions.");
        }

        if (interlace != 0)
        {
            throw new NotSupportedException("Interlaced (Adam7) PNG images are not supported.");
        }

        idat.Position = 0;
        byte[] raw;
        using (var zlib = new ZLibStream(idat, CompressionMode.Decompress))
        using (var outStream = new MemoryStream())
        {
            zlib.CopyTo(outStream);
            raw = outStream.ToArray();
        }

        return Expand(raw, width, height, bitDepth, colorType, palette, transparency);
    }

    public static RgbaImage Decode(byte[] data) => Decode(new MemoryStream(data));

    private static RgbaImage Expand(
        byte[] raw,
        int width,
        int height,
        int bitDepth,
        int colorType,
        byte[]? palette,
        byte[]? transparency)
    {
        int channels = colorType switch
        {
            0 => 1,
            2 => 3,
            3 => 1,
            4 => 2,
            6 => 4,
            _ => throw new NotSupportedException($"Unsupported PNG colour type {colorType}."),
        };

        int bitsPerPixel = channels * bitDepth;
        int bytesPerPixel = Math.Max(1, bitsPerPixel / 8);
        int bytesPerScanline = ((width * bitsPerPixel) + 7) / 8;

        // Reverse the per-scanline filters in place.
        var unfiltered = Unfilter(raw, height, bytesPerScanline, bytesPerPixel);

        var image = new RgbaImage(width, height);
        for (int y = 0; y < height; y++)
        {
            int rowStart = y * bytesPerScanline;
            for (int x = 0; x < width; x++)
            {
                (byte r, byte g, byte b, byte a) = colorType switch
                {
                    0 => ReadGrayscale(unfiltered, rowStart, x, bitDepth, transparency),
                    3 => ReadPalette(unfiltered, rowStart, x, bitDepth, palette, transparency),
                    2 => ReadRgb(unfiltered, rowStart, x, bitDepth, transparency),
                    4 => ReadGrayscaleAlpha(unfiltered, rowStart, x, bitDepth),
                    6 => ReadRgba(unfiltered, rowStart, x, bitDepth),
                    _ => ((byte)0, (byte)0, (byte)0, (byte)255),
                };
                image.SetPixel(x, y, r, g, b, a);
            }
        }

        return image;
    }

    private static byte[] Unfilter(byte[] raw, int height, int bytesPerScanline, int bytesPerPixel)
    {
        var result = new byte[height * bytesPerScanline];
        int previousStart = -1;
        int rawPosition = 0;

        for (int y = 0; y < height; y++)
        {
            if (rawPosition >= raw.Length)
            {
                break;
            }

            int filter = raw[rawPosition++];
            int resultStart = y * bytesPerScanline;
            Array.Copy(raw, rawPosition, result, resultStart, Math.Min(bytesPerScanline, raw.Length - rawPosition));
            rawPosition += bytesPerScanline;

            for (int i = 0; i < bytesPerScanline; i++)
            {
                int a = i >= bytesPerPixel ? result[resultStart + i - bytesPerPixel] : 0;
                int b = previousStart >= 0 ? result[previousStart + i] : 0;
                int c = (previousStart >= 0 && i >= bytesPerPixel) ? result[previousStart + i - bytesPerPixel] : 0;
                int value = result[resultStart + i];

                result[resultStart + i] = filter switch
                {
                    0 => (byte)value,
                    1 => (byte)(value + a),
                    2 => (byte)(value + b),
                    3 => (byte)(value + ((a + b) >> 1)),
                    4 => (byte)(value + Paeth(a, b, c)),
                    _ => (byte)value,
                };
            }

            previousStart = resultStart;
        }

        return result;
    }

    private static int Paeth(int a, int b, int c)
    {
        int p = a + b - c;
        int pa = Math.Abs(p - a);
        int pb = Math.Abs(p - b);
        int pc = Math.Abs(p - c);
        if (pa <= pb && pa <= pc)
        {
            return a;
        }

        return pb <= pc ? b : c;
    }

    private static int Sample(byte[] data, int rowStart, int x, int bitDepth, int channelCount, int channel)
    {
        if (bitDepth == 8)
        {
            return data[rowStart + (x * channelCount) + channel];
        }

        if (bitDepth == 16)
        {
            int offset = rowStart + ((x * channelCount + channel) * 2);
            return BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset));
        }

        // Sub-byte depths (1, 2, 4): only for single-channel (grey/palette) images.
        int bitsPerPixel = bitDepth;
        int bitIndex = x * bitsPerPixel;
        int byteIndex = rowStart + (bitIndex / 8);
        int shift = 8 - bitsPerPixel - (bitIndex % 8);
        return (data[byteIndex] >> shift) & ((1 << bitDepth) - 1);
    }

    private static (byte R, byte G, byte B, byte A) ReadGrayscale(
        byte[] data, int rowStart, int x, int bitDepth, byte[]? transparency)
    {
        int value = Sample(data, rowStart, x, bitDepth, 1, 0);
        byte grey = ToByte(value, bitDepth);
        byte alpha = 255;
        if (transparency is not null && transparency.Length >= 2 && bitDepth <= 8)
        {
            int transparentValue = transparency[1];
            if (value == transparentValue)
            {
                alpha = 0;
            }
        }

        return (grey, grey, grey, alpha);
    }

    private static (byte R, byte G, byte B, byte A) ReadGrayscaleAlpha(byte[] data, int rowStart, int x, int bitDepth)
    {
        int grey = Sample(data, rowStart, x, bitDepth, 2, 0);
        int alpha = Sample(data, rowStart, x, bitDepth, 2, 1);
        byte g = ToByte(grey, bitDepth);
        return (g, g, g, ToByte(alpha, bitDepth));
    }

    private static (byte R, byte G, byte B, byte A) ReadRgb(
        byte[] data, int rowStart, int x, int bitDepth, byte[]? transparency)
    {
        int r = Sample(data, rowStart, x, bitDepth, 3, 0);
        int g = Sample(data, rowStart, x, bitDepth, 3, 1);
        int b = Sample(data, rowStart, x, bitDepth, 3, 2);
        byte alpha = 255;
        if (transparency is not null && transparency.Length >= 6)
        {
            int tr = BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(0));
            int tg = BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(2));
            int tb = BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(4));
            if (r == tr && g == tg && b == tb)
            {
                alpha = 0;
            }
        }

        return (ToByte(r, bitDepth), ToByte(g, bitDepth), ToByte(b, bitDepth), alpha);
    }

    private static (byte R, byte G, byte B, byte A) ReadRgba(byte[] data, int rowStart, int x, int bitDepth)
    {
        int r = Sample(data, rowStart, x, bitDepth, 4, 0);
        int g = Sample(data, rowStart, x, bitDepth, 4, 1);
        int b = Sample(data, rowStart, x, bitDepth, 4, 2);
        int a = Sample(data, rowStart, x, bitDepth, 4, 3);
        return (ToByte(r, bitDepth), ToByte(g, bitDepth), ToByte(b, bitDepth), ToByte(a, bitDepth));
    }

    private static (byte R, byte G, byte B, byte A) ReadPalette(
        byte[] data, int rowStart, int x, int bitDepth, byte[]? palette, byte[]? transparency)
    {
        if (palette is null)
        {
            return (0, 0, 0, 255);
        }

        int index = Sample(data, rowStart, x, bitDepth, 1, 0);
        int offset = index * 3;
        if (offset + 2 >= palette.Length)
        {
            return (0, 0, 0, 255);
        }

        byte a = 255;
        if (transparency is not null && index < transparency.Length)
        {
            a = transparency[index];
        }

        return (palette[offset], palette[offset + 1], palette[offset + 2], a);
    }

    private static byte ToByte(int value, int bitDepth) => bitDepth switch
    {
        16 => (byte)(value >> 8),
        8 => (byte)value,
        4 => (byte)(value * 17),
        2 => (byte)(value * 85),
        1 => (byte)(value * 255),
        _ => (byte)value,
    };

    private static void ReadExactly(Stream stream, Span<byte> buffer)
    {
        int read = 0;
        while (read < buffer.Length)
        {
            int n = stream.Read(buffer[read..]);
            if (n <= 0)
            {
                throw new EndOfStreamException("Unexpected end of PNG stream.");
            }

            read += n;
        }
    }
}
