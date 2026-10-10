using System.Globalization;
using System.Text;
using SporeMaster.Core.IO;

namespace SporeMaster.Core.Cell;

/// <summary>One populate entry of a cell world file: a placed populate and its starting tile.</summary>
public sealed class CellWorldPopulateEntry
{
    public uint PopulateId { get; set; }

    /// <summary>Non-zero when the populate starts the tile (kept as a full word for a byte-exact round-trip).</summary>
    public int StartTile { get; set; }

    public int PlayerSize { get; set; }
}

/// <summary>One advect entry of a cell world file: an advection field over the tile.</summary>
public sealed class CellWorldAdvectEntry
{
    public int Field0 { get; set; }

    public int PlayerSize { get; set; }

    public float Strength { get; set; } = 3.5f;

    public float Variance { get; set; }

    public float Period { get; set; } = 1.0f;

    public uint AdvectId { get; set; }
}

/// <summary>
/// A cell world file (type <c>0x9B8E862F</c>, extension <c>.world</c>). The format is a pair of
/// pointer/count headers followed by two entry tables; the layout matches SporeModder FX's
/// <c>CellWorldFile</c> and round-trips byte-for-byte.
/// </summary>
public sealed class CellWorldFile
{
    public List<CellWorldPopulateEntry> PopulateEntries { get; } = new();

    public List<CellWorldAdvectEntry> AdvectEntries { get; } = new();

    public static CellWorldFile Read(Stream s)
    {
        var file = new CellWorldFile();
        int populateCount = s.ReadI32();
        long populateOffset = s.ReadU32();
        int advectCount = s.ReadI32();
        long advectOffset = s.ReadU32();

        if (populateCount is < 0 or > 1_000_000 || advectCount is < 0 or > 1_000_000)
        {
            throw new InvalidDataException("Unreasonable cell world entry count.");
        }

        s.Seek(populateOffset, SeekOrigin.Begin);
        for (int i = 0; i < populateCount; i++)
        {
            file.PopulateEntries.Add(new CellWorldPopulateEntry
            {
                PopulateId = s.ReadU32(),
                StartTile = s.ReadI32(),
                PlayerSize = s.ReadI32(),
            });
        }

        s.Seek(advectOffset, SeekOrigin.Begin);
        for (int i = 0; i < advectCount; i++)
        {
            file.AdvectEntries.Add(new CellWorldAdvectEntry
            {
                Field0 = s.ReadI32(),
                PlayerSize = s.ReadI32(),
                Strength = s.ReadF32LE(),
                Variance = s.ReadF32LE(),
                Period = s.ReadF32LE(),
                AdvectId = s.ReadU32(),
            });
        }

        return file;
    }

    public static CellWorldFile Open(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Read(stream);
    }

    public void Write(Stream s)
    {
        s.WriteI32(PopulateEntries.Count);
        s.WriteU32(PopulateEntries.Count == 0 ? 0u : 16u);
        s.WriteI32(AdvectEntries.Count);
        s.WriteU32(AdvectEntries.Count == 0 ? 0u : (uint)(16 + (PopulateEntries.Count * 12)));

        foreach (CellWorldPopulateEntry entry in PopulateEntries)
        {
            s.WriteU32(entry.PopulateId);
            s.WriteI32(entry.StartTile);
            s.WriteI32(entry.PlayerSize);
        }

        foreach (CellWorldAdvectEntry entry in AdvectEntries)
        {
            s.WriteI32(entry.Field0);
            s.WriteI32(entry.PlayerSize);
            s.WriteF32LE(entry.Strength);
            s.WriteF32LE(entry.Variance);
            s.WriteF32LE(entry.Period);
            s.WriteU32(entry.AdvectId);
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
        sb.Append("# Cell world (text dump by SporeMaster 3)\n");
        sb.Append($"populateCount {PopulateEntries.Count}\n");
        foreach (CellWorldPopulateEntry e in PopulateEntries)
        {
            sb.Append($"populate 0x{e.PopulateId:X8} startTile 0x{e.StartTile:X8} playerSize {e.PlayerSize}\n");
        }

        sb.Append($"advectCount {AdvectEntries.Count}\n");
        foreach (CellWorldAdvectEntry e in AdvectEntries)
        {
            sb.Append(
                $"advect 0x{e.AdvectId:X8} field0 {e.Field0} playerSize {e.PlayerSize} " +
                $"strength {F(e.Strength)} variance {F(e.Variance)} period {F(e.Period)}\n");
        }

        return sb.ToString();
    }

    private static string F(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);
}
