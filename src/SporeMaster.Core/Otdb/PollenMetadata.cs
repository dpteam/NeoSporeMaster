using System.Text;
using SporeMaster.Core.IO;

namespace SporeMaster.Core.Otdb;

/// <summary>A reference to a localised text resource (group/instance pair) inside a pollen metadata file.</summary>
public sealed class PollenLocaleRef
{
    public uint GroupId { get; set; }

    public uint InstanceId { get; set; }
}

/// <summary>
/// A Spore <c>.pollen_metadata</c> file (type <c>0x030BDEE3</c>), as read/written by SporeModder FX's
/// <c>PollenMetadata</c>. All integers/longs are big-endian; strings are prefixed by their character
/// count. The layout is version-dependent (versions 1..13) and round-trips byte-for-byte.
/// </summary>
public sealed class PollenMetadata
{
    public int MetadataVersion { get; set; }

    public long AssetId { get; set; } = -1;

    public uint AssetKeyType { get; set; }

    public uint AssetKeyGroup { get; set; }

    public uint AssetKeyInstance { get; set; }

    public uint ParentKeyType { get; set; }

    public uint ParentKeyGroup { get; set; }

    public uint ParentKeyInstance { get; set; }

    public long ParentAssetId { get; set; } = -1;

    public long OriginalParentAssetId { get; set; } = -1;

    public long TimeCreated { get; set; } = -1;

    public long TimeDownloaded { get; set; } = -1;

    /// <summary>Version 10 stores "no download time" as a single 0xFFFFFFFF word; keep that detail.</summary>
    public bool TimeDownloadedShort { get; set; }

    /// <summary>True when the names are stored as locale ids rather than inline text.</summary>
    public bool UseLocale { get; set; }

    public long AuthorId { get; set; } = -1;

    public string AuthorName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public PollenLocaleRef? AuthorNameLocale { get; set; }

    public PollenLocaleRef? NameLocale { get; set; }

    public PollenLocaleRef? DescriptionLocale { get; set; }

    public List<string> Authors { get; } = new();

    public List<string> Tags { get; } = new();

    public PollenLocaleRef? TagsLocale { get; set; }

    public bool IsShareable { get; set; }

    public List<int> ConsequenceTraits { get; } = new();

    public static PollenMetadata Read(Stream s)
    {
        var m = new PollenMetadata();
        m.MetadataVersion = s.ReadI32BE();
        if (m.MetadataVersion > 13)
        {
            throw new InvalidDataException($"Unsupported .pollen_metadata version {m.MetadataVersion}.");
        }

        int v = m.MetadataVersion;
        m.AssetId = ReadLong(s);
        ReadKey(s, out uint at, out uint ag, out uint ai);
        m.AssetKeyType = at;
        m.AssetKeyGroup = ag;
        m.AssetKeyInstance = ai;
        ReadKey(s, out uint pt, out uint pg, out uint pi);
        m.ParentKeyType = pt;
        m.ParentKeyGroup = pg;
        m.ParentKeyInstance = pi;

        if (v >= 10)
        {
            m.ParentAssetId = ReadLong(s);
        }

        if (v >= 12)
        {
            m.OriginalParentAssetId = ReadLong(s);
        }

        m.TimeCreated = ReadLong(s);

        if (v >= 9)
        {
            if (v == 10)
            {
                uint value1 = s.ReadU32BE();
                if (value1 != 0xFFFFFFFFu)
                {
                    uint value2 = s.ReadU32BE();
                    m.TimeDownloaded = (long)(((ulong)value1 << 32) | value2);
                }
                else
                {
                    m.TimeDownloadedShort = true;
                }
            }
            else
            {
                m.TimeDownloaded = ReadLong(s);
            }
        }

        int localeFlag = s.ReadI32BE();
        if (localeFlag == 0)
        {
            m.AuthorId = ReadLong(s);
            m.AuthorName = ReadString(s, utf16: true);
            m.Name = ReadString(s, utf16: true);
            m.Description = ReadString(s, utf16: true);
            m.UseLocale = false;
        }
        else
        {
            m.UseLocale = true;
            uint group = s.ReadU32BE();
            m.AuthorNameLocale = new PollenLocaleRef { GroupId = group, InstanceId = s.ReadU32BE() };
            m.NameLocale = new PollenLocaleRef { GroupId = group, InstanceId = s.ReadU32BE() };
            m.DescriptionLocale = new PollenLocaleRef { GroupId = group, InstanceId = s.ReadU32BE() };
        }

        if (v >= 1)
        {
            if (v >= 2)
            {
                int count = s.ReadI32BE();
                for (int i = 0; i < count; i++)
                {
                    m.Authors.Add(ReadString(s, utf16: false));
                }
            }
            else
            {
                m.Authors.Add(ReadString(s, utf16: false));
            }
        }

        int localeTags = 0;
        if (v >= 13)
        {
            localeTags = s.ReadI32BE();
        }

        if (localeTags == 0)
        {
            int count = s.ReadI32BE();
            for (int i = 0; i < count; i++)
            {
                m.Tags.Add(ReadString(s, utf16: true));
            }
        }
        else
        {
            m.TagsLocale = new PollenLocaleRef { GroupId = s.ReadU32BE(), InstanceId = s.ReadU32BE() };
        }

        if (v >= 8)
        {
            m.IsShareable = s.ReadI32BE() != 0;
            int count = s.ReadI32BE();
            for (int i = 0; i < count; i++)
            {
                m.ConsequenceTraits.Add(s.ReadI32BE());
            }
        }

        return m;
    }

    public static PollenMetadata Open(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Read(stream);
    }

    public void Write(Stream s)
    {
        int v = MetadataVersion;
        s.WriteI32BE(v);
        WriteLong(s, AssetId);
        WriteKey(s, AssetKeyType, AssetKeyGroup, AssetKeyInstance);
        WriteKey(s, ParentKeyType, ParentKeyGroup, ParentKeyInstance);

        if (v >= 10)
        {
            WriteLong(s, ParentAssetId);
        }

        if (v >= 12)
        {
            WriteLong(s, OriginalParentAssetId);
        }

        WriteLong(s, TimeCreated);

        if (v >= 9)
        {
            if (v == 10)
            {
                if (TimeDownloadedShort)
                {
                    s.WriteU32BE(0xFFFFFFFFu);
                }
                else
                {
                    WriteLong(s, TimeDownloaded);
                }
            }
            else
            {
                WriteLong(s, TimeDownloaded);
            }
        }

        if (UseLocale)
        {
            s.WriteI32BE(-1);
            uint group = AuthorNameLocale?.GroupId ?? 0;
            s.WriteU32BE(group);
            s.WriteU32BE(AuthorNameLocale?.InstanceId ?? 0);
            s.WriteU32BE(NameLocale?.InstanceId ?? 0);
            s.WriteU32BE(DescriptionLocale?.InstanceId ?? 0);
        }
        else
        {
            s.WriteI32BE(0);
            WriteLong(s, AuthorId);
            WriteString(s, AuthorName, utf16: true);
            WriteString(s, Name, utf16: true);
            WriteString(s, Description, utf16: true);
        }

        if (v >= 1)
        {
            if (v >= 2)
            {
                s.WriteI32BE(Authors.Count);
                foreach (string author in Authors)
                {
                    WriteString(s, author, utf16: false);
                }
            }
            else if (Authors.Count > 0)
            {
                WriteString(s, Authors[0], utf16: false);
            }
        }

        if (TagsLocale is not null && v >= 13)
        {
            s.WriteI32BE(-1);
            s.WriteU32BE(TagsLocale.GroupId);
            s.WriteU32BE(TagsLocale.InstanceId);
        }
        else
        {
            if (v >= 13)
            {
                s.WriteI32BE(0);
            }

            s.WriteI32BE(Tags.Count);
            foreach (string tag in Tags)
            {
                WriteString(s, tag, utf16: true);
            }
        }

        if (v >= 8)
        {
            s.WriteI32BE(IsShareable ? -1 : 0);
            s.WriteI32BE(ConsequenceTraits.Count);
            foreach (int trait in ConsequenceTraits)
            {
                s.WriteI32BE(trait);
            }
        }
    }

    public void WriteFile(string path)
    {
        using FileStream stream = File.Create(path);
        Write(stream);
    }

    /// <summary>Human-readable text dump (not a round-trip format).</summary>
    public string ToText()
    {
        var sb = new StringBuilder();
        sb.Append("# Spore .pollen_metadata (text dump by SporeMaster 3)\n");
        sb.Append($"metadataVersion {MetadataVersion}\n");
        sb.Append($"assetID {AssetId}\n");
        sb.Append($"assetKey 0x{AssetKeyType:X8} 0x{AssetKeyGroup:X8} 0x{AssetKeyInstance:X8}\n");
        sb.Append($"parentAssetKey 0x{ParentKeyType:X8} 0x{ParentKeyGroup:X8} 0x{ParentKeyInstance:X8}\n");
        if (MetadataVersion >= 10)
        {
            sb.Append($"parentAssetID {ParentAssetId}\n");
        }

        if (MetadataVersion >= 12)
        {
            sb.Append($"originalParentAssetID {OriginalParentAssetId}\n");
        }

        sb.Append($"timeCreated {TimeCreated}\n");
        if (MetadataVersion >= 9)
        {
            sb.Append($"timeDownloaded {TimeDownloaded}\n");
        }

        if (UseLocale)
        {
            sb.Append($"authorNameLocale {Locale(AuthorNameLocale)}\n");
            sb.Append($"nameLocale {Locale(NameLocale)}\n");
            sb.Append($"descriptionLocale {Locale(DescriptionLocale)}\n");
        }
        else
        {
            sb.Append($"authorID {AuthorId}\n");
            sb.Append($"authorName \"{Escape(AuthorName)}\"\n");
            sb.Append($"name \"{Escape(Name)}\"\n");
            sb.Append($"description \"{Escape(Description)}\"\n");
        }

        foreach (string author in Authors)
        {
            sb.Append($"author \"{Escape(author)}\"\n");
        }

        if (TagsLocale is not null)
        {
            sb.Append($"tagsLocale {Locale(TagsLocale)}\n");
        }
        else
        {
            foreach (string tag in Tags)
            {
                sb.Append($"tag \"{Escape(tag)}\"\n");
            }
        }

        if (MetadataVersion >= 8)
        {
            sb.Append($"isShareable {(IsShareable ? 1 : 0)}\n");
            foreach (int trait in ConsequenceTraits)
            {
                sb.Append($"consequenceTrait 0x{trait:X8}\n");
            }
        }

        return sb.ToString();
    }

    private static string Locale(PollenLocaleRef? reference) =>
        reference is null ? "-" : $"0x{reference.GroupId:X8}:0x{reference.InstanceId:X8}";

    private static string Escape(string text) =>
        text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");

    private static void ReadKey(Stream s, out uint type, out uint group, out uint instance)
    {
        type = s.ReadU32BE();
        group = s.ReadU32BE();
        instance = s.ReadU32BE();
    }

    private static void WriteKey(Stream s, uint type, uint group, uint instance)
    {
        s.WriteU32BE(type);
        s.WriteU32BE(group);
        s.WriteU32BE(instance);
    }

    private static long ReadLong(Stream s)
    {
        uint high = s.ReadU32BE();
        uint low = s.ReadU32BE();
        return (long)(((ulong)high << 32) | low);
    }

    private static void WriteLong(Stream s, long value)
    {
        s.WriteU32BE((uint)((ulong)value >> 32));
        s.WriteU32BE((uint)value);
    }

    private static string ReadString(Stream s, bool utf16)
    {
        int length = s.ReadI32BE();
        if (length < 0 || length > 10_000_000)
        {
            throw new InvalidDataException($"Unreasonable pollen string length {length}.");
        }

        byte[] bytes = s.ReadBytesExactly(utf16 ? length * 2 : length);
        return utf16 ? Encoding.Unicode.GetString(bytes) : Encoding.ASCII.GetString(bytes);
    }

    private static void WriteString(Stream s, string text, bool utf16)
    {
        byte[] bytes = utf16 ? Encoding.Unicode.GetBytes(text) : Encoding.ASCII.GetBytes(text);
        s.WriteI32BE(bytes.Length / (utf16 ? 2 : 1));
        s.Write(bytes);
    }
}
