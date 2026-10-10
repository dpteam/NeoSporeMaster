namespace SporeMaster.Core.Imaging;

/// <summary>
/// Decodes Truevision TGA images (the Spore <c>tga</c> type, <c>0x2F7D0006</c>). Supports
/// uncompressed and RLE true-colour, grayscale and colour-mapped data at 8/16/24/32 bits per pixel.
/// </summary>
public static class TgaCodec
{
    public static RgbaImage Decode(byte[] data)
    {
        if (data.Length < 18)
        {
            throw new InvalidDataException("TGA file is too short.");
        }

        int idLength = data[0];
        int colorMapType = data[1];
        int imageType = data[2];
        int colorMapLength = data[5] | (data[6] << 8);
        int colorMapDepth = data[7];
        int width = data[12] | (data[13] << 8);
        int height = data[14] | (data[15] << 8);
        int pixelDepth = data[16];
        int descriptor = data[17];

        if (width <= 0 || height <= 0)
        {
            throw new InvalidDataException($"TGA has an invalid size {width}x{height}.");
        }

        bool rle = imageType is 9 or 10 or 11;
        bool colorMapped = imageType is 1 or 9;
        bool grayscale = imageType is 3 or 11;
        bool trueColor = imageType is 2 or 10;
        if (!colorMapped && !grayscale && !trueColor)
        {
            throw new InvalidDataException($"Unsupported TGA image type {imageType}.");
        }

        int pos = 18 + idLength;

        // Colour-map (palette), when present.
        byte[]? palette = null;
        if (colorMapType == 1 && colorMapLength > 0)
        {
            int entryBytes = (colorMapDepth + 7) / 8;
            palette = new byte[colorMapLength * 4];
            for (int i = 0; i < colorMapLength; i++)
            {                (byte pr, byte pg, byte pb, byte pa) = ReadPixel(data, pos, colorMapDepth, false, false);
                palette[(i * 4) + 0] = pr;
                palette[(i * 4) + 1] = pg;
                palette[(i * 4) + 2] = pb;
                palette[(i * 4) + 3] = pa;
                pos += entryBytes;
            }
        }

        int pixelBytes = (pixelDepth + 7) / 8;
        int indexBytes = colorMapped ? (pixelDepth <= 8 ? 1 : 2) : pixelBytes;
        int total = width * height;
        var pixels = new (byte R, byte G, byte B, byte A)[total];

        int read = 0;
        while (read < total)
        {
            int count = 1;
            bool repeated = false;
            if (rle)
            {
                int packet = data[pos++];
                count = (packet & 0x7F) + 1;
                repeated = (packet & 0x80) != 0;
            }

            if (repeated)
            {
                (byte r, byte g, byte b, byte a) = ReadPixel(data, pos, pixelDepth, colorMapped, grayscale, palette);
                pos += indexBytes;
                for (int i = 0; i < count && read < total; i++)
                {
                    pixels[read++] = (r, g, b, a);
                }
            }
            else
            {
                for (int i = 0; i < count && read < total; i++)
                {
                    pixels[read++] = ReadPixel(data, pos, pixelDepth, colorMapped, grayscale, palette);
                    pos += indexBytes;
                }
            }
        }

        bool topLeft = (descriptor & 0x20) != 0;
        bool rightToLeft = (descriptor & 0x10) != 0;
        var image = new RgbaImage(width, height);
        for (int y = 0; y < height; y++)
        {
            int srcY = topLeft ? y : (height - 1 - y);
            for (int x = 0; x < width; x++)
            {
                int srcX = rightToLeft ? (width - 1 - x) : x;
                (byte r, byte g, byte b, byte a) = pixels[(srcY * width) + srcX];
                image.SetPixel(x, y, r, g, b, a);
            }
        }

        return image;
    }

    private static (byte R, byte G, byte B, byte A) ReadPixel(
        byte[] data,
        int pos,
        int pixelDepth,
        bool colorMapped,
        bool grayscale,
        byte[]? palette = null)
    {
        if (colorMapped)
        {
            int index = pixelDepth <= 8 ? data[pos] : data[pos] | (data[pos + 1] << 8);
            if (palette is null || (index * 4) + 3 >= palette.Length)
            {
                return (0, 0, 0, 255);
            }

            return (palette[(index * 4) + 0], palette[(index * 4) + 1],
                palette[(index * 4) + 2], palette[(index * 4) + 3]);
        }

        if (grayscale)
        {
            byte gray = data[pos];
            return (gray, gray, gray, 255);
        }

        switch (pixelDepth)
        {
            case 32:
                return (data[pos + 2], data[pos + 1], data[pos], data[pos + 3]);
            case 24:
                return (data[pos + 2], data[pos + 1], data[pos], 255);
            case 16:
            {
                int value = data[pos] | (data[pos + 1] << 8);
                bool opaque = (value & 0x8000) != 0;
                return (Expand5((value >> 10) & 0x1F), Expand5((value >> 5) & 0x1F), Expand5(value & 0x1F),
                    (byte)(opaque ? 255 : 0));
            }

            case 15:
            {
                int value = data[pos] | (data[pos + 1] << 8);
                return (Expand5((value >> 10) & 0x1F), Expand5((value >> 5) & 0x1F), Expand5(value & 0x1F), 255);
            }

            case 8:
                return (data[pos], data[pos], data[pos], 255);
            default:
                throw new InvalidDataException($"Unsupported TGA pixel depth {pixelDepth}.");
        }
    }

    private static byte Expand5(int value) => (byte)((value << 3) | (value >> 2));
}
