namespace SporeMaster.Core.Textures;

/// <summary>
/// Decodes the block-compressed and common uncompressed texture formats that Spore stores in rasters
/// and RenderWare 4 textures into 32-bit RGBA. Supports BC1 (DXT1), BC2 (DXT3), BC3 (DXT5) and the
/// uncompressed <c>A8R8G8B8</c>/<c>R8G8B8</c>/<c>A8</c> formats.
/// </summary>
/// <remarks>
/// Only the top mip level is decoded; that is enough to preview a texture. The BC1/2/3 algorithms
/// are the standard S3TC ones.
/// </remarks>
public static class BlockCompressionDecoder
{
    /// <summary>True when the format can be decoded by this class.</summary>
    public static bool CanDecode(uint format) =>
        format == TextureFormats.Dxt1 || format == TextureFormats.Dxt2 || format == TextureFormats.Dxt3
            || format == TextureFormats.Dxt4 || format == TextureFormats.Dxt5
            || format == TextureFormats.A8R8G8B8 || format == TextureFormats.R8G8B8 || format == TextureFormats.A8;

    /// <summary>
    /// Decodes the top mip level to a tightly packed RGBA byte array (<c>width * height * 4</c>),
    /// or null when the format is unsupported.
    /// </summary>
    public static byte[]? Decode(uint format, byte[] data, int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return null;
        }

        var rgba = new byte[width * height * 4];

        if (format == TextureFormats.A8R8G8B8)
        {
            DecodeBgra(data, rgba, width, height);
            return rgba;
        }

        if (format == TextureFormats.R8G8B8)
        {
            DecodeBgr(data, rgba, width, height);
            return rgba;
        }

        if (format == TextureFormats.A8)
        {
            DecodeA8(data, rgba, width, height);
            return rgba;
        }

        if (format == TextureFormats.Dxt1)
        {
            return DecodeBc1(data, rgba, width, height, forceFourColor: false) ? rgba : null;
        }

        if (format == TextureFormats.Dxt3)
        {
            return DecodeBc2(data, rgba, width, height) ? rgba : null;
        }

        if (format == TextureFormats.Dxt5)
        {
            return DecodeBc3(data, rgba, width, height) ? rgba : null;
        }

        // DXT2/DXT4 differ only in alpha premultiplication; treat them as DXT3/DXT5 for preview.
        if (format == TextureFormats.Dxt2)
        {
            return DecodeBc2(data, rgba, width, height) ? rgba : null;
        }

        if (format == TextureFormats.Dxt4)
        {
            return DecodeBc3(data, rgba, width, height) ? rgba : null;
        }

        return null;
    }

    private static bool DecodeBc1(byte[] data, byte[] rgba, int width, int height, bool forceFourColor)
    {
        int blocksX = Math.Max(1, (width + 3) / 4);
        int blocksY = Math.Max(1, (height + 3) / 4);
        if (data.Length < blocksX * blocksY * 8)
        {
            return false;
        }

        int offset = 0;
        var palette = new byte[4 * 4];
        for (int by = 0; by < blocksY; by++)
        {
            for (int bx = 0; bx < blocksX; bx++)
            {
                ushort c0 = (ushort)(data[offset] | (data[offset + 1] << 8));
                ushort c1 = (ushort)(data[offset + 2] | (data[offset + 3] << 8));
                uint bits = (uint)(data[offset + 4] | (data[offset + 5] << 8) | (data[offset + 6] << 16) | (data[offset + 7] << 24));
                offset += 8;

                BuildBc1Palette(c0, c1, palette, forceFourColor);

                for (int y = 0; y < 4; y++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        int px = bx * 4 + x;
                        int py = by * 4 + y;
                        if (px >= width || py >= height)
                        {
                            continue;
                        }

                        int index = (int)((bits >> (2 * (y * 4 + x))) & 0x3);
                        WritePixel(rgba, width, px, py, palette, index);
                    }
                }
            }
        }

        return true;
    }

    private static void BuildBc1Palette(ushort c0, ushort c1, byte[] palette, bool forceFourColor)
    {
        Expand565(c0, palette, 0);
        Expand565(c1, palette, 4);

        if (c0 > c1 || forceFourColor)
        {
            for (int i = 0; i < 3; i++)
            {
                palette[8 + i] = (byte)(((2 * palette[i]) + palette[4 + i]) / 3);
                palette[12 + i] = (byte)((palette[i] + (2 * palette[4 + i])) / 3);
            }

            palette[11] = 255;
            palette[15] = 255;
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                palette[8 + i] = (byte)((palette[i] + palette[4 + i]) / 2);
            }

            palette[11] = 255;
            // palette[12..15] stays 0 (transparent black).
        }

        palette[3] = 255;
        palette[7] = 255;
    }

    private static bool DecodeBc2(byte[] data, byte[] rgba, int width, int height)
    {
        int blocksX = Math.Max(1, (width + 3) / 4);
        int blocksY = Math.Max(1, (height + 3) / 4);
        if (data.Length < blocksX * blocksY * 16)
        {
            return false;
        }

        int offset = 0;
        var palette = new byte[4 * 4];
        for (int by = 0; by < blocksY; by++)
        {
            for (int bx = 0; bx < blocksX; bx++)
            {
                long alpha = 0;
                for (int i = 0; i < 8; i++)
                {
                    alpha |= (long)data[offset + i] << (8 * i);
                }

                ushort c0 = (ushort)(data[offset + 8] | (data[offset + 9] << 8));
                ushort c1 = (ushort)(data[offset + 10] | (data[offset + 11] << 8));
                uint bits = (uint)(data[offset + 12] | (data[offset + 13] << 8) | (data[offset + 14] << 16) | (data[offset + 15] << 24));
                offset += 16;

                BuildBc1Palette(c0, c1, palette, forceFourColor: true);

                for (int y = 0; y < 4; y++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        int px = bx * 4 + x;
                        int py = by * 4 + y;
                        if (px >= width || py >= height)
                        {
                            continue;
                        }

                        int index = (int)((bits >> (2 * (y * 4 + x))) & 0x3);
                        WritePixel(rgba, width, px, py, palette, index);

                        int nibble = (int)((alpha >> (4 * (y * 4 + x))) & 0xF);
                        rgba[(((py * width) + px) * 4) + 3] = (byte)(nibble * 17);
                    }
                }
            }
        }

        return true;
    }

    private static bool DecodeBc3(byte[] data, byte[] rgba, int width, int height)
    {
        int blocksX = Math.Max(1, (width + 3) / 4);
        int blocksY = Math.Max(1, (height + 3) / 4);
        if (data.Length < blocksX * blocksY * 16)
        {
            return false;
        }

        int offset = 0;
        var palette = new byte[4 * 4];
        var alpha = new byte[16];
        for (int by = 0; by < blocksY; by++)
        {
            for (int bx = 0; bx < blocksX; bx++)
            {
                BuildBc3Alpha(data, offset, alpha);

                ushort c0 = (ushort)(data[offset + 8] | (data[offset + 9] << 8));
                ushort c1 = (ushort)(data[offset + 10] | (data[offset + 11] << 8));
                uint bits = (uint)(data[offset + 12] | (data[offset + 13] << 8) | (data[offset + 14] << 16) | (data[offset + 15] << 24));
                offset += 16;

                BuildBc1Palette(c0, c1, palette, forceFourColor: true);

                for (int y = 0; y < 4; y++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        int px = bx * 4 + x;
                        int py = by * 4 + y;
                        if (px >= width || py >= height)
                        {
                            continue;
                        }

                        int index = (int)((bits >> (2 * (y * 4 + x))) & 0x3);
                        WritePixel(rgba, width, px, py, palette, index);
                        rgba[(((py * width) + px) * 4) + 3] = alpha[(y * 4) + x];
                    }
                }
            }
        }

        return true;
    }

    private static void BuildBc3Alpha(byte[] data, int offset, byte[] alpha)
    {
        int a0 = data[offset];
        int a1 = data[offset + 1];

        var values = new int[8];
        values[0] = a0;
        values[1] = a1;
        if (a0 > a1)
        {
            for (int i = 1; i <= 6; i++)
            {
                values[i + 1] = ((7 - i) * a0 + i * a1) / 7;
            }
        }
        else
        {
            for (int i = 1; i <= 4; i++)
            {
                values[i + 1] = ((5 - i) * a0 + i * a1) / 5;
            }

            values[6] = 0;
            values[7] = 255;
        }

        // 48 bits of 3-bit indices for 16 texels.
        long indexBits = 0;
        for (int i = 0; i < 6; i++)
        {
            indexBits |= (long)data[offset + 2 + i] << (8 * i);
        }

        for (int i = 0; i < 16; i++)
        {
            alpha[i] = (byte)values[(int)((indexBits >> (3 * i)) & 0x7)];
        }
    }

    private static void WritePixel(byte[] rgba, int width, int x, int y, byte[] palette, int index)
    {
        int p = index * 4;
        int i = ((y * width) + x) * 4;
        rgba[i] = palette[p];
        rgba[i + 1] = palette[p + 1];
        rgba[i + 2] = palette[p + 2];
        rgba[i + 3] = palette[p + 3];
    }

    private static void Expand565(ushort c, byte[] palette, int offset)
    {
        int r = (c >> 11) & 0x1F;
        int g = (c >> 5) & 0x3F;
        int b = c & 0x1F;
        palette[offset] = (byte)((r << 3) | (r >> 2));
        palette[offset + 1] = (byte)((g << 2) | (g >> 4));
        palette[offset + 2] = (byte)((b << 3) | (b >> 2));
        palette[offset + 3] = 255;
    }

    private static void DecodeBgra(byte[] data, byte[] rgba, int width, int height)
    {
        int count = Math.Min(width * height, data.Length / 4);
        for (int i = 0; i < count; i++)
        {
            rgba[(i * 4) + 0] = data[(i * 4) + 2];
            rgba[(i * 4) + 1] = data[(i * 4) + 1];
            rgba[(i * 4) + 2] = data[(i * 4) + 0];
            rgba[(i * 4) + 3] = data[(i * 4) + 3];
        }
    }

    private static void DecodeBgr(byte[] data, byte[] rgba, int width, int height)
    {
        int count = Math.Min(width * height, data.Length / 3);
        for (int i = 0; i < count; i++)
        {
            rgba[(i * 4) + 0] = data[(i * 3) + 2];
            rgba[(i * 4) + 1] = data[(i * 3) + 1];
            rgba[(i * 4) + 2] = data[(i * 3) + 0];
            rgba[(i * 4) + 3] = 255;
        }
    }

    private static void DecodeA8(byte[] data, byte[] rgba, int width, int height)
    {
        int count = Math.Min(width * height, data.Length);
        for (int i = 0; i < count; i++)
        {
            byte a = data[i];
            rgba[(i * 4) + 0] = a;
            rgba[(i * 4) + 1] = a;
            rgba[(i * 4) + 2] = a;
            rgba[(i * 4) + 3] = 255;
        }
    }
}
