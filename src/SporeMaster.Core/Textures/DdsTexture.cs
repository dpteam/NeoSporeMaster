using SporeMaster.Core.IO;

namespace SporeMaster.Core.Textures;

/// <summary>
/// A DirectDraw Surface (<c>.dds</c>) file: a 128-byte header followed by raw texture data
/// (all mipmap levels concatenated).
/// </summary>
/// <remarks>
/// Reading supports the common uncompressed and block-compressed layouts. The header produced
/// by <see cref="Write"/> is compatible with GIMP and Photoshop, and matches the layout that
/// SporeModder FX uses so that Spore texture files interoperate.
/// </remarks>
public sealed class DdsTexture
{
    public const uint Magic = 0x20534444; // "DDS "
    public const int HeaderSize = 128;

    private const uint FlagCaps = 0x1;
    private const uint FlagHeight = 0x2;
    private const uint FlagWidth = 0x4;
    private const uint FlagPixelFormat = 0x1000;
    private const uint FlagMipmapCount = 0x20000;
    private const uint FlagLinearSize = 0x80000;

    private const uint PfAlphaPixels = 0x1;
    private const uint PfFourCc = 0x4;
    private const uint PfRgb = 0x40;

    private const uint CapsComplex = 0x8;
    private const uint CapsMipmap = 0x400000;
    private const uint CapsTexture = 0x1000;

    private const uint Caps2Cubemap = 0x200;
    private const uint Caps2CubemapAll = 0xFC00;

    public int Width { get; set; }

    public int Height { get; set; }

    public int MipmapCount { get; set; } = 1;

    /// <summary>Texture format id in the raster sense (a D3DFMT value or a FourCC).</summary>
    public uint Format { get; set; }

    /// <summary>Packed RGB bit count for uncompressed formats (e.g. 32, 24, 8).</summary>
    public int RgbBitCount { get; set; } = 32;

    public bool IsCubeMap { get; set; }

    public byte[] Data { get; set; } = Array.Empty<byte>();

    public static DdsTexture Read(Stream stream)
    {
        uint magic = stream.ReadU32();
        if (magic != Magic)
        {
            throw new InvalidDataException($"Not a DDS file (magic 0x{magic:X8}).");
        }

        var texture = new DdsTexture();

        stream.ReadU32();                 // dwSize (124)
        uint flags = stream.ReadU32();
        texture.Height = (int)stream.ReadU32();
        texture.Width = (int)stream.ReadU32();
        stream.ReadU32();                 // pitchOrLinearSize
        stream.ReadU32();                 // depth
        uint mipCount = stream.ReadU32();
        stream.Seek(44, SeekOrigin.Current); // reserved1[11]

        stream.ReadU32();                 // ddspf.dwSize (32)
        uint pfFlags = stream.ReadU32();
        uint fourCc = stream.ReadU32();
        uint rgbBitCount = stream.ReadU32();
        uint rMask = stream.ReadU32();
        uint gMask = stream.ReadU32();
        uint bMask = stream.ReadU32();
        uint aMask = stream.ReadU32();

        uint caps = stream.ReadU32();
        uint caps2 = stream.ReadU32();
        stream.ReadU32();                 // caps3
        stream.ReadU32();                 // caps4
        stream.ReadU32();                 // reserved2

        texture.MipmapCount = mipCount == 0 ? 1 : (int)mipCount;
        texture.RgbBitCount = (int)(rgbBitCount == 0 ? 32 : rgbBitCount);
        texture.IsCubeMap = (caps2 & Caps2Cubemap) != 0;

        texture.Format = ResolveFormat(pfFlags, fourCc, rgbBitCount, rMask, gMask, bMask, aMask);

        int remaining = (int)(stream.Length - stream.Position);
        texture.Data = remaining > 0 ? stream.ReadBytesExactly(remaining) : Array.Empty<byte>();
        return texture;
    }

    /// <summary>Determines the Spore texture format id from the DDS pixel-format fields.</summary>
    private static uint ResolveFormat(uint pfFlags, uint fourCc, uint rgbBitCount, uint rMask, uint gMask, uint bMask, uint aMask)
    {
        if ((pfFlags & PfFourCc) != 0)
        {
            return fourCc;
        }

        // Spore's A8R8G8B8 textures are written with fourCC 0 and pfFlags 0x41 (RGB | ALPHAPIXELS).
        if (pfFlags == (PfRgb | PfAlphaPixels) && rgbBitCount == 32)
        {
            return TextureFormats.A8R8G8B8;
        }

        if (rgbBitCount == 8)
        {
            return TextureFormats.A8;
        }

        if (rgbBitCount == 24)
        {
            return TextureFormats.R8G8B8;
        }

        return TextureFormats.A8R8G8B8;
    }

    public void Write(Stream stream)
    {
        int mipCount = Math.Max(1, MipmapCount);
        int block = TextureFormats.GetBlockSize(Format);
        bool compressed = block != 0;

        uint pitchOrLinearSize = compressed
            ? (uint)(Math.Max(1, (Width + 3) / 4) * Math.Max(1, (Height + 3) / 4) * block)
            : (uint)(Width * Height * (RgbBitCount / 8));

        uint pfFlags;
        uint fourCc = 0;
        uint rgbBitCount = 32;
        uint rMask = 0x00FF0000;
        uint gMask = 0x0000FF00;
        uint bMask = 0x000000FF;
        uint aMask = 0xFF000000;

        if (Format == TextureFormats.A8)
        {
            pfFlags = PfAlphaPixels;
            rgbBitCount = 8;
            rMask = gMask = bMask = 0;
            aMask = 0x000000FF;
        }
        else if (Format == TextureFormats.R8G8B8)
        {
            pfFlags = PfRgb;
            rgbBitCount = 24;
            rMask = 0x00FF0000;
            gMask = 0x0000FF00;
            bMask = 0x000000FF;
            aMask = 0;
        }
        else if (compressed || (!IsUncompressedD3dFormat(Format) && Format != 0))
        {
            pfFlags = PfFourCc;
            fourCc = Format;
            rgbBitCount = 0;
            rMask = gMask = bMask = aMask = 0;
        }
        else
        {
            pfFlags = PfRgb | PfAlphaPixels;
            rgbBitCount = 32;
        }

        uint caps = CapsTexture;
        uint caps2 = 0;
        if (mipCount > 1)
        {
            caps |= CapsComplex | CapsMipmap;
        }

        if (IsCubeMap)
        {
            caps |= CapsComplex;
            caps2 |= Caps2Cubemap | Caps2CubemapAll;
        }

        uint flags = FlagCaps | FlagHeight | FlagWidth | FlagPixelFormat | FlagLinearSize;
        if (mipCount > 1)
        {
            flags |= FlagMipmapCount;
        }

        stream.WriteU32(Magic);
        stream.WriteU32(124);
        stream.WriteU32(flags);
        stream.WriteU32((uint)Height);
        stream.WriteU32((uint)Width);
        stream.WriteU32(pitchOrLinearSize);
        stream.WriteU32(0);              // depth
        stream.WriteU32((uint)mipCount);
        stream.WriteZeroes(44);

        stream.WriteU32(32);
        stream.WriteU32(pfFlags);
        stream.WriteU32(fourCc);
        stream.WriteU32(rgbBitCount);
        stream.WriteU32(rMask);
        stream.WriteU32(gMask);
        stream.WriteU32(bMask);
        stream.WriteU32(aMask);

        stream.WriteU32(caps);
        stream.WriteU32(caps2);
        stream.WriteU32(0);
        stream.WriteU32(0);
        stream.WriteU32(0);

        stream.Write(Data);
    }

    private static bool IsUncompressedD3dFormat(uint format) =>
        format is TextureFormats.A8 or TextureFormats.R8G8B8 or TextureFormats.A8R8G8B8;

    /// <summary>Byte size of each mipmap level, in order from largest to smallest.</summary>
    public int[] GetMipmapSizes()
    {
        int count = Math.Max(1, MipmapCount);
        var sizes = new int[count];
        for (int i = 0; i < count; i++)
        {
            sizes[i] = TextureFormats.GetMipSize(Format, Width >> i, Height >> i, RgbBitCount);
        }

        return sizes;
    }

    /// <summary>Extracts one mipmap level's bytes from <see cref="Data"/>.</summary>
    public byte[] GetMipmap(int index)
    {
        int[] sizes = GetMipmapSizes();
        int offset = 0;
        for (int i = 0; i < index && i < sizes.Length; i++)
        {
            offset += sizes[i];
        }

        int size = index < sizes.Length ? sizes[index] : 0;
        if (offset >= Data.Length)
        {
            return Array.Empty<byte>();
        }

        size = Math.Min(size, Data.Length - offset);
        var result = new byte[size];
        Array.Copy(Data, offset, result, 0, size);
        return result;
    }
}
