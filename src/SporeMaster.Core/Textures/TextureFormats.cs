namespace SporeMaster.Core.Textures;

/// <summary>
/// Texture format identifiers as they appear inside Spore <c>.raster</c> resources.
/// </summary>
/// <remarks>
/// A raster stores a single 32-bit "texture format" field. For uncompressed textures it is a
/// Direct3D <c>D3DFMT_*</c> value (for example <c>0x15</c> = <c>A8R8G8B8</c>); for block/YUV
/// formats it is a FourCC such as <c>DXT1</c> or <c>DXT5</c>. The values here mirror SporeModder
/// FX's <c>DDSPixelFormat</c>.
/// </remarks>
public static class TextureFormats
{
    // Direct3D formats (uncompressed).
    public const uint R8G8B8 = 20;
    public const uint A8R8G8B8 = 21;
    public const uint A8 = 28;

    // FourCC (little-endian packing of the ASCII tag).
    public static readonly uint Dxt1 = MakeFourCc("DXT1");
    public static readonly uint Dxt2 = MakeFourCc("DXT2");
    public static readonly uint Dxt3 = MakeFourCc("DXT3");
    public static readonly uint Dxt4 = MakeFourCc("DXT4");
    public static readonly uint Dxt5 = MakeFourCc("DXT5");
    public static readonly uint Ati1 = MakeFourCc("ATI1");
    public static readonly uint Ati2 = MakeFourCc("ATI2");
    public static readonly uint Bc4U = MakeFourCc("BC4U");
    public static readonly uint Bc4S = MakeFourCc("BC4S");
    public static readonly uint Bc5S = MakeFourCc("BC5S");
    public static readonly uint Rgbg = MakeFourCc("RGBG");
    public static readonly uint Grgb = MakeFourCc("GRGB");
    public static readonly uint Uyvy = MakeFourCc("UYVY");
    public static readonly uint Yuy2 = MakeFourCc("YUY2");
    public static readonly uint Dx10 = MakeFourCc("DX10");

    /// <summary>Packs a four-character code into its little-endian 32-bit value.</summary>
    public static uint MakeFourCc(string code)
    {
        if (code.Length != 4)
        {
            throw new ArgumentException("A FourCC must be exactly four characters.", nameof(code));
        }

        return (uint)(code[0] | (code[1] << 8) | (code[2] << 16) | (code[3] << 24));
    }

    /// <summary>
    /// Returns the bytes used by a single block for block-compressed formats, or 0 when the
    /// format is not block-compressed.
    /// </summary>
    public static int GetBlockSize(uint format)
    {
        if (format is 0)
        {
            return 0;
        }

        if (format == Dxt1 || format == Ati1 || format == Bc4U || format == Bc4S)
        {
            return 8;
        }

        if (format is not 0 && (format == Dxt2 || format == Dxt3 || format == Dxt4 || format == Dxt5 ||
            format == Ati2 || format == Bc5S))
        {
            return 16;
        }

        return 0;
    }

    /// <summary>
    /// Size in bytes of mipmap level <paramref name="mip"/> for the given format and dimensions.
    /// This is the value the raster stores for each level and what a DDS reader must recompute.
    /// </summary>
    public static int GetMipSize(uint format, int width, int height, int rgbBitCount)
    {
        int w = Math.Max(1, width);
        int h = Math.Max(1, height);

        int block = GetBlockSize(format);
        if (block != 0)
        {
            int blocks = Math.Max(1, (w + 3) / 4) * Math.Max(1, (h + 3) / 4);
            return block * blocks;
        }

        if (format == Rgbg || format == Grgb || format == Uyvy || format == Yuy2)
        {
            return w * h * 2;
        }

        // Uncompressed: bytes per pixel derived from the packed RGB bit count.
        int bytesPerPixel = rgbBitCount > 0 ? rgbBitCount / 8 : BytesPerPixelForD3dFormat(format);
        if (bytesPerPixel <= 0)
        {
            bytesPerPixel = 4;
        }

        return w * h * bytesPerPixel;
    }

    /// <summary>Bytes per pixel for a known uncompressed D3D format.</summary>
    public static int BytesPerPixelForD3dFormat(uint format) => format switch
    {
        R8G8B8 => 3,
        A8R8G8B8 => 4,
        A8 => 1,
        _ => 4,
    };

    /// <summary>Human readable name for a format id (best effort).</summary>
    public static string Describe(uint format)
    {
        if (format == R8G8B8)
        {
            return "R8G8B8";
        }

        if (format == A8R8G8B8)
        {
            return "A8R8G8B8";
        }

        if (format == A8)
        {
            return "A8";
        }

        var tag = new char[4];
        for (int i = 0; i < 4; i++)
        {
            char c = (char)((format >> (i * 8)) & 0xFF);
            tag[i] = c is >= ' ' and < (char)127 ? c : '?';
        }

        string text = new string(tag);
        return text.All(char.IsLetterOrDigit) ? text : "0x" + format.ToString("X8");
    }
}
