using System.Globalization;
using System.Text;
using SporeMaster.Core.IO;
using SporeMaster.Core.Names;

namespace SporeMaster.Core.Cell;

/// <summary>A loot-table entry type.</summary>
public enum LootTableEntryType
{
    Nothing = 0,
    Cell = 1,
    Table = 2,
}

/// <summary>One entry of a cell-stage loot table.</summary>
public sealed class LootTableEntry
{
    public LootTableEntryType Type { get; set; }

    public int CellId { get; set; }

    public int TableId { get; set; }

    public float Weight { get; set; } = 1.0f;

    public int Count { get; set; }

    public int CountDelta { get; set; }

    public int LevelOffset { get; set; }
}

/// <summary>
/// A Spore cell-stage loot table (<c>.loottable</c>, type <c>0xD92AF091</c>).
/// </summary>
/// <remarks>All numbers are little-endian; see <c>docs/LOOTTABLE_FORMAT.md</c>.</remarks>
public sealed class CellLootTableFile
{
    public List<LootTableEntry> Entries { get; } = new();

    public float MinRadius { get; set; }

    public float MaxRadius { get; set; }

    public float InitialAlpha { get; set; } = 1.0f;

    public float ExpelForce { get; set; }

    public int Effect { get; set; }

    public bool MustHavePart { get; set; }

    public float Delay { get; set; }

    public static CellLootTableFile Read(Stream s)
    {
        int offset = s.ReadI32();
        int count = s.ReadI32();

        if (offset != 36 && !(offset == 0 && count == 0))
        {
            throw new InvalidDataException($"Unexpected loot table offset {offset} (expected 36).");
        }

        var file = new CellLootTableFile
        {
            MinRadius = s.ReadF32LE(),
            MaxRadius = s.ReadF32LE(),
            InitialAlpha = s.ReadF32LE(),
            ExpelForce = s.ReadF32LE(),
            Effect = s.ReadI32(),
            MustHavePart = s.ReadU8() != 0,
            Delay = ReadAfterSkip(s, 3),
        };

        for (int i = 0; i < count; i++)
        {
            file.Entries.Add(new LootTableEntry
            {
                Type = (LootTableEntryType)s.ReadI32(),
                CellId = s.ReadI32(),
                TableId = s.ReadI32(),
                Weight = s.ReadF32LE(),
                Count = s.ReadI32(),
                CountDelta = s.ReadI32(),
                LevelOffset = s.ReadI32(),
            });
        }

        return file;
    }

    private static float ReadAfterSkip(Stream s, int skip)
    {
        s.Seek(skip, SeekOrigin.Current);
        return s.ReadF32LE();
    }

    public void Write(Stream s)
    {
        s.WriteI32(Entries.Count == 0 ? 0 : 36);
        s.WriteI32(Entries.Count);
        s.WriteF32LE(MinRadius);
        s.WriteF32LE(MaxRadius);
        s.WriteF32LE(InitialAlpha);
        s.WriteF32LE(ExpelForce);
        s.WriteI32(Effect);
        s.WriteI32(MustHavePart ? 1 : 0);
        s.WriteF32LE(Delay);

        foreach (LootTableEntry entry in Entries)
        {
            s.WriteI32((int)entry.Type);
            s.WriteI32(entry.CellId);
            s.WriteI32(entry.TableId);
            s.WriteF32LE(entry.Weight);
            s.WriteI32(entry.Count);
            s.WriteI32(entry.CountDelta);
            s.WriteI32(entry.LevelOffset);
        }
    }

    // ---- text ----

    public string ToScript(NameRegistrySet names)
    {
        var builder = new StringBuilder();
        builder.Append("minRadius ").Append(Format(MinRadius)).Append('\n');
        builder.Append("maxRadius ").Append(Format(MaxRadius)).Append('\n');
        builder.Append("initialAlpha ").Append(Format(InitialAlpha)).Append('\n');
        builder.Append("expelForce ").Append(Format(ExpelForce)).Append('\n');
        builder.Append("effect ").Append(names.GetFileName(unchecked((uint)Effect))).Append('\n');
        builder.Append("mustHavePart ").Append(MustHavePart ? "true" : "false").Append('\n');
        builder.Append("delay ").Append(Format(Delay)).Append('\n');

        foreach (LootTableEntry entry in Entries)
        {
            builder.Append("\ncLootTableEntry ").Append(TypeName(entry.Type));
            switch (entry.Type)
            {
                case LootTableEntryType.Cell:
                    builder.Append(' ').Append(names.GetFileName(unchecked((uint)entry.CellId)));
                    builder.Append(" -weight ").Append(Format(entry.Weight));
                    builder.Append(" -count ").Append(entry.Count);
                    builder.Append(" -countDelta ").Append(entry.CountDelta);
                    builder.Append(" -levelOffset ").Append(entry.LevelOffset);
                    break;
                case LootTableEntryType.Table:
                    builder.Append(' ').Append(names.GetFileName(unchecked((uint)entry.TableId)));
                    builder.Append(" -weight ").Append(Format(entry.Weight));
                    builder.Append(" -count ").Append(entry.Count);
                    builder.Append(" -countDelta ").Append(entry.CountDelta);
                    break;
                default:
                    builder.Append(" -weight ").Append(Format(entry.Weight));
                    break;
            }

            builder.Append('\n');
        }

        return builder.ToString();
    }

    public static CellLootTableFile Parse(string text, NameRegistrySet names)
    {
        var file = new CellLootTableFile();
        foreach (string raw in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            List<string> tokens = Tokenize(line);
            switch (tokens[0])
            {
                case "minRadius": file.MinRadius = ParseFloat(tokens[1]); break;
                case "maxRadius": file.MaxRadius = ParseFloat(tokens[1]); break;
                case "initialAlpha": file.InitialAlpha = ParseFloat(tokens[1]); break;
                case "expelForce": file.ExpelForce = ParseFloat(tokens[1]); break;
                case "effect": file.Effect = unchecked((int)names.GetFileHash(tokens[1])); break;
                case "mustHavePart": file.MustHavePart = tokens[1].Equals("true", StringComparison.OrdinalIgnoreCase); break;
                case "delay": file.Delay = ParseFloat(tokens[1]); break;
                case "cLootTableEntry":
                    {
                        var entry = new LootTableEntry { Type = ParseType(tokens[1]) };
                        if (entry.Type == LootTableEntryType.Cell && tokens.Count > 2)
                        {
                            entry.CellId = unchecked((int)names.GetFileHash(tokens[2]));
                        }
                        else if (entry.Type == LootTableEntryType.Table && tokens.Count > 2)
                        {
                            entry.TableId = unchecked((int)names.GetFileHash(tokens[2]));
                        }

                        entry.Weight = Option(tokens, "-weight", 1.0f);
                        entry.Count = (int)Option(tokens, "-count", entry.Type == LootTableEntryType.Nothing ? 0 : 1);
                        entry.CountDelta = (int)Option(tokens, "-countDelta", 0);
                        entry.LevelOffset = (int)Option(tokens, "-levelOffset", 0);
                        file.Entries.Add(entry);
                        break;
                    }
            }
        }

        return file;
    }

    private static float Option(List<string> tokens, string name, float fallback)
    {
        int index = tokens.IndexOf(name);
        return index >= 0 && index + 1 < tokens.Count ? ParseFloat(tokens[index + 1]) : fallback;
    }

    private static LootTableEntryType ParseType(string token) => token switch
    {
        "cell" => LootTableEntryType.Cell,
        "table" => LootTableEntryType.Table,
        _ => LootTableEntryType.Nothing,
    };

    private static string TypeName(LootTableEntryType type) => type switch
    {
        LootTableEntryType.Cell => "cell",
        LootTableEntryType.Table => "table",
        _ => "nothing",
    };

    private static float ParseFloat(string token) => float.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static string Format(float value) => value.ToString("0.#######", CultureInfo.InvariantCulture);

    private static List<string> Tokenize(string line)
    {
        var tokens = new List<string>();
        int i = 0;
        while (i < line.Length)
        {
            if (char.IsWhiteSpace(line[i]))
            {
                i++;
                continue;
            }

            int start = i;
            while (i < line.Length && !char.IsWhiteSpace(line[i]))
            {
                i++;
            }

            tokens.Add(line[start..i]);
        }

        return tokens;
    }
}
