using System.Text;
using SporeMaster.Core.IO;

namespace SporeMaster.Core.Effects;

/// <summary>One component or resource section of an <c>.effdir</c> file (kept raw).</summary>
public sealed class EffectSection
{
    public int Type { get; set; }

    public int Version { get; set; }

    /// <summary>Number of elements declared in the section header.</summary>
    public int Count { get; set; }

    /// <summary>Whether this is a resource section (as opposed to a component section).</summary>
    public bool IsResource { get; set; }

    /// <summary>The raw section body (the bytes after type/version/size, including the count word).</summary>
    public byte[] Body { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// A Spore effect directory (type <c>0xEA5118B0</c>, extension <c>.effdir</c>). This reads the
/// container at the section level: a version, the component and resource sections (each with a
/// type/version/size/count header and a raw body) and the visual-effect section header. The
/// component semantics require the dozens of effect types from SporeModder FX and are left opaque;
/// the container round-trips byte-for-byte.
/// </summary>
/// <remarks>
/// All header fields are big-endian (matching SporeModder FX's <c>EffectDirectory</c>). The bytes that
/// follow the visual-effect header (effect data, exports and imports) are preserved verbatim.
/// </remarks>
public sealed class EffectDirectoryFile
{
    public int Version { get; set; }

    public List<EffectSection> Sections { get; } = new();

    /// <summary>Version of the visual-effect section that follows the components/resources.</summary>
    public int VisualEffectVersion { get; set; }

    public int VisualEffectCount { get; set; }

    /// <summary>Everything after the visual-effect header (effect data, exports and imports).</summary>
    public byte[] Trailing { get; set; } = Array.Empty<byte>();

    public static EffectDirectoryFile Read(Stream s)
    {
        var file = new EffectDirectoryFile();
        file.Version = s.ReadU16BE();
        if (file.Version != 4)
        {
            throw new InvalidDataException($"Unsupported EFFDIR version {file.Version}.");
        }

        ReadSections(s, file, isResource: false);
        ReadSections(s, file, isResource: true);

        file.VisualEffectVersion = s.ReadU16BE();
        file.VisualEffectCount = s.ReadI32BE();

        long remaining = s.Length - s.Position;
        if (remaining > 0)
        {
            file.Trailing = s.ReadBytesExactly((int)remaining);
        }

        return file;
    }

    private static void ReadSections(Stream s, EffectDirectoryFile file, bool isResource)
    {
        while (true)
        {
            int type = s.ReadU16BE();
            if (type == 0xFFFF)
            {
                return;
            }

            int version = s.ReadU16BE();
            int size = s.ReadI32BE();
            if (size < 4 || size > 200_000_000)
            {
                throw new InvalidDataException($"Unreasonable EFFDIR section size {size}.");
            }

            byte[] body = s.ReadBytesExactly(size);
            int count = (body[0] << 24) | (body[1] << 16) | (body[2] << 8) | body[3];
            file.Sections.Add(new EffectSection
            {
                Type = type,
                Version = version,
                Count = count,
                IsResource = isResource,
                Body = body,
            });
        }
    }

    public static EffectDirectoryFile Open(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Read(stream);
    }

    public void Write(Stream s)
    {
        s.WriteU16BE((ushort)Version);

        foreach (EffectSection section in Sections.Where(x => !x.IsResource))
        {
            WriteSection(s, section);
        }

        s.WriteU16BE(0xFFFF);

        foreach (EffectSection section in Sections.Where(x => x.IsResource))
        {
            WriteSection(s, section);
        }

        s.WriteU16BE(0xFFFF);

        s.WriteU16BE((ushort)VisualEffectVersion);
        s.WriteI32BE(VisualEffectCount);
        s.Write(Trailing);
    }

    private static void WriteSection(Stream s, EffectSection section)
    {
        s.WriteU16BE((ushort)section.Type);
        s.WriteU16BE((ushort)section.Version);
        s.WriteI32BE(section.Body.Length);
        s.Write(section.Body);
    }

    public void WriteFile(string path)
    {
        using FileStream stream = File.Create(path);
        Write(stream);
    }

    /// <summary>Human-readable section dump (not a round-trip format).</summary>
    public string ToText()
    {
        var sb = new StringBuilder();
        sb.Append("# Spore effect directory (.effdir) section dump by SporeMaster 3\n");
        sb.Append($"version {Version}\n");
        sb.Append($"components {Sections.Count(x => !x.IsResource)}\n");
        sb.Append($"resources {Sections.Count(x => x.IsResource)}\n");
        sb.Append($"visualEffects {VisualEffectCount} (section version {VisualEffectVersion})\n\n");

        sb.Append("## components\n");
        foreach (EffectSection section in Sections.Where(x => !x.IsResource))
        {
            sb.Append(
                $"  type {section.Type} v{section.Version} count {section.Count} body {section.Body.Length} byte(s)\n");
        }

        sb.Append("## resources\n");
        foreach (EffectSection section in Sections.Where(x => x.IsResource))
        {
            sb.Append(
                $"  type {section.Type} v{section.Version} count {section.Count} body {section.Body.Length} byte(s)\n");
        }

        sb.Append(
            $"\n# effect data + exports + imports: {Trailing.Length} byte(s); decoding components requires SporeModder's effect types.\n");
        return sb.ToString();
    }
}
