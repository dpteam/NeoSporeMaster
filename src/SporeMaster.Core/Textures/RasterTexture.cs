using SporeMaster.Core.IO;

namespace SporeMaster.Core.Textures;

/// <summary>
/// A Spore <c>.raster</c> texture resource (type <c>0x2F4E681C</c>): a small header followed by
/// the raw bytes of each mipmap level.
/// </summary>
/// <remarks>
/// Layout (all little-endian):
/// <code>
/// int32  version       // 1
/// int32  width
/// int32  height
/// int32  mipmapCount
/// int32  pixelWidth    // 8
/// int32  textureFormat // a D3DFMT value or a FourCC
/// for each mipmap:
///   int32 size
///   byte[size] data
/// </code>
/// See <c>docs/TEXTURE_FORMAT.md</c>.
/// </remarks>
public sealed class RasterTexture
{
    public const uint TypeId = 0x2F4E681C;
    public const int Version = 1;

    public int Width { get; set; }

    public int Height { get; set; }

    public int MipmapCount { get; set; } = 1;

    public int PixelWidth { get; set; } = 8;

    public uint TextureFormat { get; set; }

    /// <summary>The raw bytes of each mipmap level, largest first.</summary>
    public byte[][] Mipmaps { get; set; } = Array.Empty<byte[]>();

    public static RasterTexture Read(Stream stream)
    {
        var raster = new RasterTexture();

        uint version = stream.ReadU32();
        if (version != Version)
        {
            throw new InvalidDataException($"Unsupported raster version {version} (expected {Version}).");
        }

        raster.Width = stream.ReadI32();
        raster.Height = stream.ReadI32();
        raster.MipmapCount = stream.ReadI32();
        raster.PixelWidth = stream.ReadI32();
        raster.TextureFormat = stream.ReadU32();

        if (raster.MipmapCount < 0 || raster.MipmapCount > 32)
        {
            throw new InvalidDataException($"Unreasonable raster mipmap count {raster.MipmapCount}.");
        }

        var mipmaps = new byte[raster.MipmapCount][];
        for (int i = 0; i < raster.MipmapCount; i++)
        {
            int size = stream.ReadI32();
            if (size < 0)
            {
                throw new InvalidDataException($"Negative mipmap size at level {i}.");
            }

            mipmaps[i] = stream.ReadBytesExactly(size);
        }

        raster.Mipmaps = mipmaps;
        return raster;
    }

    public void Write(Stream stream)
    {
        stream.WriteU32(Version);
        stream.WriteI32(Width);
        stream.WriteI32(Height);
        stream.WriteI32(MipmapCount);
        stream.WriteI32(PixelWidth);
        stream.WriteU32(TextureFormat);

        foreach (byte[] mip in Mipmaps)
        {
            stream.WriteI32(mip.Length);
            stream.Write(mip);
        }
    }

    /// <summary>Converts this raster into a <see cref="DdsTexture"/>.</summary>
    public DdsTexture ToDds()
    {
        int total = 0;
        foreach (byte[] mip in Mipmaps)
        {
            total += mip.Length;
        }

        var data = new byte[total];
        int position = 0;
        foreach (byte[] mip in Mipmaps)
        {
            Array.Copy(mip, 0, data, position, mip.Length);
            position += mip.Length;
        }

        return new DdsTexture
        {
            Width = Width,
            Height = Height,
            MipmapCount = Math.Max(1, MipmapCount),
            Format = TextureFormat,
            RgbBitCount = 8 * TextureFormats.BytesPerPixelForD3dFormat(TextureFormat),
            Data = data,
        };
    }

    /// <summary>Builds a raster from a <see cref="DdsTexture"/>, splitting its data by mipmap size.</summary>
    public static RasterTexture FromDds(DdsTexture dds)
    {
        int count = Math.Max(1, dds.MipmapCount);
        int[] sizes = dds.GetMipmapSizes();
        var mipmaps = new byte[count][];

        int offset = 0;
        for (int i = 0; i < count; i++)
        {
            int size = i < sizes.Length ? sizes[i] : 0;
            if (offset + size > dds.Data.Length)
            {
                size = Math.Max(0, dds.Data.Length - offset);
            }

            var mip = new byte[size];
            Array.Copy(dds.Data, offset, mip, 0, size);
            offset += size;
            mipmaps[i] = mip;
        }

        return new RasterTexture
        {
            Width = dds.Width,
            Height = dds.Height,
            MipmapCount = count,
            PixelWidth = 8,
            TextureFormat = dds.Format,
            Mipmaps = mipmaps,
        };
    }

    // ---- convenience file helpers ----

    public static RasterTexture ReadFile(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Read(stream);
    }

    public void WriteFile(string path)
    {
        using FileStream stream = File.Create(path);
        Write(stream);
    }

    public static void RasterToDds(string inputPath, string outputPath)
    {
        using FileStream input = File.OpenRead(inputPath);
        RasterTexture raster = Read(input);
        using FileStream output = File.Create(outputPath);
        raster.ToDds().Write(output);
    }

    public static void DdsToRaster(string inputPath, string outputPath)
    {
        using FileStream input = File.OpenRead(inputPath);
        DdsTexture dds = DdsTexture.Read(input);
        using FileStream output = File.Create(outputPath);
        FromDds(dds).Write(output);
    }
}
