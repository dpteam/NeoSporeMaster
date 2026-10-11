using System.Globalization;
using System.Text;
using SporeMaster.Core.IO;

namespace SporeMaster.Core.Otdb;

/// <summary>One <c>parameter</c> of a <c>.summary</c> file: an integer or float value under a hash id.</summary>
public sealed class SummaryParameter
{
    public uint Id { get; set; }

    public bool IsFloat { get; set; }

    public int IntValue { get; set; }

    public float FloatValue { get; set; }
}

/// <summary>One <c>amTag</c> of a <c>.summary</c> file (a creature editor metadata tag).</summary>
public sealed class SummaryAmTag
{
    public uint TagGroup { get; set; }

    public uint TagId { get; set; }

    public float Value { get; set; }
}

/// <summary>
/// A Spore <c>.summary</c> file (type <c>0x02D5C9AF</c>), as read/written by SporeModder FX's
/// <c>SummaryFile</c>. All integers and floats are big-endian; the layout round-trips byte-for-byte.
/// </summary>
public sealed class SummaryFile
{
    public const int TypeInt = 0x02E1A75D;
    public const int TypeFloat = 0x02E1A7FF;

    /// <summary>Well-known parameter ids (from SporeModder FX).</summary>
    private static readonly Dictionary<uint, string> ParameterNames = new()
    {
        [0x02DD90AF] = "type",
        [0x02DC9D1E] = "subtype",
        [0x05B06E36] = "cost",
        [0x02F05C60] = "baseGear",
        [0x7358629A] = "height",
        [0x02F05C58] = "carnivore",
        [0x02F05C59] = "herbivore",
        [0x02F05C5E] = "cuteness",
        [0x02F05C5F] = "totalSocial",
        [0x05B15AA5] = "social",
        [0x02F05C61] = "numGraspers",
        [0x03FEA210] = "meanLookingScore",
        [0x03FEA1A0] = "totalAttack",
        [0x05B15A92] = "attack",
        [0x03FEA1C0] = "numFeet",
        [0x04AB3BD8] = "biteCapRange",
        [0x04AB3BD9] = "strikeCapRange",
        [0x04AB3BDA] = "chargeCapRange",
        [0x04AB3BDB] = "spitCapRange",
        [0xF42136D5] = "singCapRange",
        [0xF42136D6] = "danceCapRange",
        [0xF42136D7] = "charmCapRange",
        [0xF42136D8] = "poseCapRange",
        [0x05AC2B96] = "glideCapRange",
        [0x05AC2B97] = "stealthCapRange",
        [0x05AC2B98] = "sprintCapRange",
        [0x068DE3E8] = "senseCapRange",
        [0x05AC2B99] = "healthCapRange",
        [0x2E0FCC74] = "editorModelSaveLastChild",
        [0x99524FAF] = "editorModelForceSaveOver",
        [0xAE773A29] = "editorModelCopyConsequence",
        [0xEC2942CD] = "isMyCaptain",
    };

    /// <summary>Well-known amTag group ids (from SporeModder FX).</summary>
    private static readonly Dictionary<uint, string> AmTagGroupNames = new()
    {
        [0x01F71E85] = "BlockID",
        [0x04EA801F] = "EditorGroup",
        [0x04EAB460] = "GroupCount",
        [0x04EBFEBD] = "BlockTag",
        [0x05833344] = "TextureTag",
        [0x0518AF1D] = "Creator",
        [0x068375D7] = "ACPSet",
        [0x0518B883] = "Feed",
        [0x052B27B5] = "RegionColor",
        [0x052C812A] = "BaseColor",
        [0x052C8CA5] = "Texture",
    };

    public List<SummaryParameter> Parameters { get; } = new();

    public List<SummaryAmTag> AmTags { get; } = new();

    /// <summary>Reads the trailing bytes after the tags, kept so writing reproduces the file exactly.</summary>
    public byte[] Trailing { get; set; } = Array.Empty<byte>();

    public static SummaryFile Read(Stream s)
    {
        var file = new SummaryFile();

        int version = s.ReadI32BE();
        if (version != 4)
        {
            throw new InvalidDataException($"Unsupported .summary version {version}.");
        }

        int count = s.ReadI32BE();
        if (count is < 0 or > 1_000_000)
        {
            throw new InvalidDataException("Unreasonable .summary parameter count.");
        }

        for (int i = 0; i < count; i++)
        {
            uint id = s.ReadU32BE();
            int type = s.ReadI32BE();
            var parameter = new SummaryParameter { Id = id };
            switch (type)
            {
                case TypeInt:
                    parameter.IntValue = s.ReadI32BE();
                    break;
                case TypeFloat:
                    parameter.IsFloat = true;
                    parameter.FloatValue = s.ReadF32BE();
                    break;
                default:
                    throw new InvalidDataException($"Unknown .summary parameter type 0x{type:X8}.");
            }

            file.Parameters.Add(parameter);
        }

        int tagCount = s.ReadI32BE();
        if (tagCount is < 0 or > 1_000_000)
        {
            throw new InvalidDataException("Unreasonable .summary tag count.");
        }

        for (int i = 0; i < tagCount; i++)
        {
            file.AmTags.Add(new SummaryAmTag
            {
                TagGroup = s.ReadU32BE(),
                TagId = s.ReadU32BE(),
                Value = s.ReadF32BE(),
            });
        }

        long remaining = s.Length - s.Position;
        if (remaining > 0)
        {
            file.Trailing = s.ReadBytesExactly((int)remaining);
        }

        return file;
    }

    public static SummaryFile Open(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Read(stream);
    }

    public void Write(Stream s)
    {
        s.WriteI32BE(4);
        s.WriteI32BE(Parameters.Count);
        foreach (SummaryParameter parameter in Parameters)
        {
            s.WriteU32BE(parameter.Id);
            if (parameter.IsFloat)
            {
                s.WriteI32BE(TypeFloat);
                s.WriteF32BE(parameter.FloatValue);
            }
            else
            {
                s.WriteI32BE(TypeInt);
                s.WriteI32BE(parameter.IntValue);
            }
        }

        s.WriteI32BE(AmTags.Count);
        foreach (SummaryAmTag tag in AmTags)
        {
            s.WriteU32BE(tag.TagGroup);
            s.WriteU32BE(tag.TagId);
            s.WriteF32BE(tag.Value);
        }

        if (Trailing.Length > 0)
        {
            s.Write(Trailing);
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
        sb.Append("# Spore .summary (text dump by SporeMaster 3)\n");
        foreach (SummaryParameter p in Parameters)
        {
            string name = ParameterNames.TryGetValue(p.Id, out string? n) ? n : $"0x{p.Id:X8}";
            string value = p.IsFloat
                ? p.FloatValue.ToString("0.######", CultureInfo.InvariantCulture)
                : p.IntValue.ToString(CultureInfo.InvariantCulture);
            sb.Append($"parameter {(p.IsFloat ? "float" : "int")} {name} {value}\n");
        }

        sb.Append('\n');
        foreach (SummaryAmTag tag in AmTags)
        {
            string group = AmTagGroupNames.TryGetValue(tag.TagGroup, out string? g) ? g : $"0x{tag.TagGroup:X8}";
            string value = tag.Value.ToString("0.######", CultureInfo.InvariantCulture);
            sb.Append($"amTag {group} 0x{tag.TagId:X8} {value}\n");
        }

        return sb.ToString();
    }
}
