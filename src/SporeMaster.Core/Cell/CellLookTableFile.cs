using System.Globalization;
using System.Text;
using SporeMaster.Core.IO;

namespace SporeMaster.Core.Cell;

/// <summary>The behavioural inputs a cell's "look" (steering) table can weight.</summary>
public enum LookTableEntryType
{
    Up = 0,
    Forward = 1,
    Left = 2,
    Right = 3,
    PursueTarget = 4,
    FleeTarget = 5,
    Object = 6,
    Food = 7,
    Predator = 8,
    Destination = 9,
    Cursor = 10,
    Player = 11,
}

/// <summary>One weighted entry of a look table.</summary>
public sealed class LookTableEntry
{
    public LookTableEntryType Type { get; set; }

    public float Value { get; set; }
}

/// <summary>
/// A Spore cell look (steering) table (<c>.look_table</c>, type <c>0x8C042499</c>).
/// All numbers are little-endian; see <c>docs/LOOKTABLE_FORMAT.md</c>.
/// </summary>
public sealed class CellLookTableFile
{
    public List<LookTableEntry> Entries { get; } = new();

    public static CellLookTableFile Read(Stream s)
    {
        int offset = s.ReadI32();
        if (offset != 8)
        {
            throw new InvalidDataException($"Unexpected look table offset {offset} (expected 8).");
        }

        var file = new CellLookTableFile();
        int count = s.ReadI32();
        for (int i = 0; i < count; i++)
        {
            file.Entries.Add(new LookTableEntry
            {
                Type = (LookTableEntryType)s.ReadI32(),
                Value = s.ReadF32LE(),
            });
        }

        return file;
    }

    public void Write(Stream s)
    {
        s.WriteI32(8);
        s.WriteI32(Entries.Count);
        foreach (LookTableEntry entry in Entries)
        {
            s.WriteI32((int)entry.Type);
            s.WriteF32LE(entry.Value);
        }
    }

    public string ToScript()
    {
        var builder = new StringBuilder();
        foreach (LookTableEntry entry in Entries)
        {
            builder.Append("cLookTableEntry ").Append(TypeName(entry.Type)).Append(' ')
                .Append(entry.Value.ToString("0.#######", CultureInfo.InvariantCulture)).Append('\n');
        }

        return builder.ToString();
    }

    public static CellLookTableFile Parse(string text)
    {
        var file = new CellLookTableFile();
        foreach (string raw in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            string[] parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3 && parts[0] == "cLookTableEntry")
            {
                file.Entries.Add(new LookTableEntry
                {
                    Type = ParseType(parts[1]),
                    Value = float.Parse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture),
                });
            }
        }

        return file;
    }

    private static LookTableEntryType ParseType(string token) => token switch
    {
        "up" => LookTableEntryType.Up,
        "forward" => LookTableEntryType.Forward,
        "left" => LookTableEntryType.Left,
        "right" => LookTableEntryType.Right,
        "pursue_target" => LookTableEntryType.PursueTarget,
        "flee_target" => LookTableEntryType.FleeTarget,
        "object" => LookTableEntryType.Object,
        "food" => LookTableEntryType.Food,
        "predator" => LookTableEntryType.Predator,
        "destination" => LookTableEntryType.Destination,
        "cursor" => LookTableEntryType.Cursor,
        "player" => LookTableEntryType.Player,
        _ => LookTableEntryType.Up,
    };

    private static string TypeName(LookTableEntryType type) => type switch
    {
        LookTableEntryType.Up => "up",
        LookTableEntryType.Forward => "forward",
        LookTableEntryType.Left => "left",
        LookTableEntryType.Right => "right",
        LookTableEntryType.PursueTarget => "pursue_target",
        LookTableEntryType.FleeTarget => "flee_target",
        LookTableEntryType.Object => "object",
        LookTableEntryType.Food => "food",
        LookTableEntryType.Predator => "predator",
        LookTableEntryType.Destination => "destination",
        LookTableEntryType.Cursor => "cursor",
        LookTableEntryType.Player => "player",
        _ => "up",
    };
}
