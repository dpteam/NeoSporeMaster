using System.Text;
using SporeMaster.Core.IO;
namespace SporeMaster.Core.Spui;

/// <summary>A resource key inside a <c>.spui</c> file (type/group/instance, little-endian).</summary>
public readonly record struct SpuiResourceKey(uint Type, uint Group, uint Instance);

/// <summary>One hit-mask record of a <c>.spui</c> file (kept raw for a byte-exact round-trip).</summary>
public sealed class SpuiHitMask
{
    /// <summary>Version 3+: true when the mask is stored as run-length-encoded data, false for a file key.</summary>
    public bool UseRle { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public SpuiResourceKey FileKey { get; set; }

    /// <summary>The raw bytes of the hit-mask record (kept verbatim).</summary>
    public byte[] Raw { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// The header of a Spore user-interface layout (type <c>0x250FE9A2</c>, extension <c>.spui</c>).
/// This reads and writes the container exactly (magic <c>0xE3FE3FB8</c>, version, resource lists,
/// hit masks and class proxy ids), keeping the element section verbatim. Interpreting the elements
/// requires the external "designer" class definitions, which are outside the scope of this reader.
/// </summary>
public sealed class SpuiFile
{
    public const uint Magic = 0xE3FE3FB8;
    public const uint MagicEnd = 0x1C01C047;

    public int Version { get; set; }

    public List<SpuiResourceKey> ImageResources { get; } = new();

    public List<SpuiResourceKey> AtlasResources { get; } = new();

    public List<SpuiHitMask> HitMasks { get; } = new();

    public List<int> ProxyIds { get; } = new();

    /// <summary>The element section (everything after the proxy ids), preserved verbatim.</summary>
    public byte[] ElementData { get; set; } = Array.Empty<byte>();

    public static SpuiFile Read(Stream s)
    {
        var file = new SpuiFile();

        uint magic = s.ReadU32();
        if (magic != Magic)
        {
            throw new InvalidDataException($"Unsupported SPUI magic 0x{magic:X8}.");
        }

        file.Version = s.ReadU16();
        if (file.Version > 3)
        {
            throw new InvalidDataException($"Unsupported SPUI version {file.Version}.");
        }

        int imageCount = s.ReadU16();
        int atlasCount = s.ReadU16();
        int hitMaskCount = s.ReadU16();
        int proxyCount = s.ReadU16();

        if (imageCount + atlasCount + hitMaskCount + proxyCount > 6000)
        {
            throw new InvalidDataException("Unreasonable SPUI resource count.");
        }

        for (int i = 0; i < imageCount; i++)
        {
            file.ImageResources.Add(ReadKey(s));
        }

        for (int i = 0; i < atlasCount; i++)
        {
            file.AtlasResources.Add(ReadKey(s));
        }

        for (int i = 0; i < hitMaskCount; i++)
        {
            long start = s.Position;
            var mask = new SpuiHitMask();
            if (file.Version >= 3)
            {
                mask.UseRle = s.ReadU8() != 0;
            }

            if (mask.UseRle)
            {
                mask.Width = s.ReadI32();
                mask.Height = s.ReadI32();
                int wordCount = s.ReadI32();
                if (wordCount is < 0 or > 10_000_000)
                {
                    throw new InvalidDataException("Unreasonable SPUI hit-mask size.");
                }

                s.ReadBytesExactly(wordCount * 2);
            }
            else
            {
                mask.FileKey = ReadKey(s);
            }

            int length = (int)(s.Position - start);
            s.Seek(start, SeekOrigin.Begin);
            mask.Raw = s.ReadBytesExactly(length);
            file.HitMasks.Add(mask);
        }

        for (int i = 0; i < proxyCount; i++)
        {
            file.ProxyIds.Add(s.ReadI32());
        }

        long remaining = s.Length - s.Position;
        if (remaining > 0)
        {
            file.ElementData = s.ReadBytesExactly((int)remaining);
        }

        return file;
    }

    public static SpuiFile Open(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Read(stream);
    }

    public void Write(Stream s)
    {
        s.WriteU32(Magic);
        s.WriteU16((ushort)Version);
        s.WriteU16((ushort)ImageResources.Count);
        s.WriteU16((ushort)AtlasResources.Count);
        s.WriteU16((ushort)HitMasks.Count);
        s.WriteU16((ushort)ProxyIds.Count);

        foreach (SpuiResourceKey key in ImageResources)
        {
            WriteKey(s, key);
        }

        foreach (SpuiResourceKey key in AtlasResources)
        {
            WriteKey(s, key);
        }

        foreach (SpuiHitMask mask in HitMasks)
        {
            s.Write(mask.Raw);
        }

        foreach (int proxyId in ProxyIds)
        {
            s.WriteI32(proxyId);
        }

        s.Write(ElementData);
    }

    public void WriteFile(string path)
    {
        using FileStream stream = File.Create(path);
        Write(stream);
    }

    /// <summary>Human-readable dump of the container (not a round-trip format).</summary>
    public string ToText()
    {
        var sb = new StringBuilder();
        sb.Append("# Spore UI layout (.spui) header dump by SporeMaster 3\n");
        sb.Append($"version {Version}\n");
        sb.Append($"imageResources {ImageResources.Count}\n");
        sb.Append($"atlasResources {AtlasResources.Count}\n");
        sb.Append($"hitMasks {HitMasks.Count}\n");
        sb.Append($"classes {ProxyIds.Count}\n\n");

        for (int i = 0; i < ImageResources.Count; i++)
        {
            sb.Append($"image [{i}] {Key(ImageResources[i])}\n");
        }

        for (int i = 0; i < AtlasResources.Count; i++)
        {
            sb.Append($"atlas [{i}] {Key(AtlasResources[i])}\n");
        }

        for (int i = 0; i < HitMasks.Count; i++)
        {
            SpuiHitMask mask = HitMasks[i];
            sb.Append(mask.UseRle
                ? $"hitMask [{i}] rle {mask.Width}x{mask.Height}\n"
                : $"hitMask [{i}] key {Key(mask.FileKey)}\n");
        }

        sb.Append('\n');
        for (int i = 0; i < ProxyIds.Count; i++)
        {
            sb.Append($"class [{i}] 0x{unchecked((uint)ProxyIds[i]):X8}\n");
        }

        sb.Append(
            $"\n# element section: {ElementData.Length} byte(s); decoding requires the SporeModder UI designer definitions.\n");
        return sb.ToString();
    }

    private static string Key(SpuiResourceKey key) =>
        $"instance 0x{key.Instance:X8} type 0x{key.Type:X8} group 0x{key.Group:X8}";

    private static SpuiResourceKey ReadKey(Stream s)
    {
        uint instance = s.ReadU32();
        uint type = s.ReadU32();
        uint group = s.ReadU32();
        return new SpuiResourceKey(type, group, instance);
    }

    private static void WriteKey(Stream s, SpuiResourceKey key)
    {
        s.WriteU32(key.Instance);
        s.WriteU32(key.Type);
        s.WriteU32(key.Group);
    }
}
