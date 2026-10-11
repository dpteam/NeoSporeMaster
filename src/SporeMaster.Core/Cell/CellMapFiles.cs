using System.Globalization;
using System.Text;
using SporeMaster.Core.IO;

namespace SporeMaster.Core.Cell;

/// <summary>One entry of a cell background map: an RGB colour and a blend factor.</summary>
public sealed class CellBackgroundMapEntry
{
    public float R { get; set; } = 1f;

    public float G { get; set; } = 1f;

    public float B { get; set; } = 1f;

    public float FieldC { get; set; } = 1f;
}

/// <summary>
/// A cell background map (type <c>0x612B3191</c>): a count/offset header followed by
/// <see cref="CellBackgroundMapEntry"/> records. All values are little-endian and the layout
/// round-trips byte-for-byte (verified against SporeModder FX and real data).
/// </summary>
public sealed class CellBackgroundMapFile
{
    public List<CellBackgroundMapEntry> Entries { get; } = new();

    public byte[] Trailing { get; set; } = Array.Empty<byte>();

    public static CellBackgroundMapFile Read(Stream s)
    {
        var file = new CellBackgroundMapFile();
        int count = s.ReadI32();
        int offset = s.ReadI32();
        if (offset != 8 && !(offset == 0 && count == 0))
        {
            throw new InvalidDataException($"Cell background map offset was {offset}, expected 8.");
        }

        for (int i = 0; i < count; i++)
        {
            file.Entries.Add(new CellBackgroundMapEntry
            {
                R = s.ReadF32LE(),
                G = s.ReadF32LE(),
                B = s.ReadF32LE(),
                FieldC = s.ReadF32LE(),
            });
        }

        file.CaptureTrailing(s);
        return file;
    }

    public static CellBackgroundMapFile Open(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Read(stream);
    }

    public void Write(Stream s)
    {
        s.WriteI32(Entries.Count);
        s.WriteI32(Entries.Count == 0 ? 0 : 8);
        foreach (CellBackgroundMapEntry entry in Entries)
        {
            s.WriteF32LE(entry.R);
            s.WriteF32LE(entry.G);
            s.WriteF32LE(entry.B);
            s.WriteF32LE(entry.FieldC);
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

    public string ToText()
    {
        var sb = new StringBuilder();
        sb.Append("# Cell background map (text dump by SporeMaster 3)\n");
        sb.Append($"entries {Entries.Count}\n");
        foreach (CellBackgroundMapEntry e in Entries)
        {
            sb.Append($"background {F(e.R)} {F(e.G)} {F(e.B)} {F(e.FieldC)}\n");
        }

        return sb.ToString();
    }

    internal static string F(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    internal static void CaptureTrailing(Stream s, Action<byte[]> set)
    {
        long remaining = s.Length - s.Position;
        if (remaining > 0)
        {
            set(s.ReadBytesExactly((int)remaining));
        }
    }

    private void CaptureTrailing(Stream s) => CaptureTrailing(s, b => Trailing = b);
}

/// <summary>One entry of a cell effect map: an effect and its placement parameters.</summary>
public sealed class CellEffectMapEntry
{
    public uint EffectId { get; set; }

    public int Type { get; set; }

    public float Field8 { get; set; }

    public float FieldC { get; set; }

    public float Field10 { get; set; }

    public float Field14 { get; set; }

    public int Field18 { get; set; }
}

/// <summary>
/// A cell effect map (type <c>0x433FB70C</c>): a count/offset header followed by
/// <see cref="CellEffectMapEntry"/> records (28 bytes each). Little-endian, byte-exact.
/// </summary>
public sealed class CellEffectMapFile
{
    public List<CellEffectMapEntry> Entries { get; } = new();

    public byte[] Trailing { get; set; } = Array.Empty<byte>();

    public static CellEffectMapFile Read(Stream s)
    {
        var file = new CellEffectMapFile();
        int count = s.ReadI32();
        int offset = s.ReadI32();
        if (offset != 8)
        {
            throw new InvalidDataException($"Cell effect map offset was {offset}, expected 8.");
        }

        for (int i = 0; i < count; i++)
        {
            file.Entries.Add(new CellEffectMapEntry
            {
                EffectId = s.ReadU32(),
                Type = s.ReadI32(),
                Field8 = s.ReadF32LE(),
                FieldC = s.ReadF32LE(),
                Field10 = s.ReadF32LE(),
                Field14 = s.ReadF32LE(),
                Field18 = s.ReadI32(),
            });
        }

        CellBackgroundMapFile.CaptureTrailing(s, b => file.Trailing = b);
        return file;
    }

    public static CellEffectMapFile Open(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Read(stream);
    }

    public void Write(Stream s)
    {
        s.WriteI32(Entries.Count);
        s.WriteI32(8);
        foreach (CellEffectMapEntry e in Entries)
        {
            s.WriteU32(e.EffectId);
            s.WriteI32(e.Type);
            s.WriteF32LE(e.Field8);
            s.WriteF32LE(e.FieldC);
            s.WriteF32LE(e.Field10);
            s.WriteF32LE(e.Field14);
            s.WriteI32(e.Field18);
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

    public string ToText()
    {
        string F1(float v) => CellBackgroundMapFile.F(v);
        var sb = new StringBuilder();
        sb.Append("# Cell effect map (text dump by SporeMaster 3)\n");
        sb.Append($"entries {Entries.Count}\n");
        foreach (CellEffectMapEntry e in Entries)
        {
            sb.Append(
                $"effect 0x{e.EffectId:X8} type {e.Type} {F1(e.Field8)} {F1(e.FieldC)} {F1(e.Field10)} {F1(e.Field14)} {e.Field18}\n");
        }

        return sb.ToString();
    }
}

/// <summary>One entry of a cell random-creature table: how often a creature type spawns.</summary>
public sealed class CellRandomCreatureEntry
{
    public int Type { get; set; }

    public uint CreatureId { get; set; }

    public float Weight { get; set; } = 1f;

    public int SpeedMin { get; set; } = -1;

    public int SpeedMax { get; set; } = -1;

    public int DangerMin { get; set; } = -1;

    public int DangerMax { get; set; } = -1;
}

/// <summary>
/// A cell random-creature table (type <c>0xF9C3D770</c>): a count/offset header followed by
/// <see cref="CellRandomCreatureEntry"/> records (28 bytes each). Little-endian, byte-exact.
/// </summary>
public sealed class CellRandomCreatureFile
{
    public List<CellRandomCreatureEntry> Entries { get; } = new();

    public byte[] Trailing { get; set; } = Array.Empty<byte>();

    public static CellRandomCreatureFile Read(Stream s)
    {
        var file = new CellRandomCreatureFile();
        int count = s.ReadI32();
        int offset = s.ReadI32();
        if (offset != 8)
        {
            throw new InvalidDataException($"Cell random creature offset was {offset}, expected 8.");
        }

        for (int i = 0; i < count; i++)
        {
            file.Entries.Add(new CellRandomCreatureEntry
            {
                Type = s.ReadI32(),
                CreatureId = s.ReadU32(),
                Weight = s.ReadF32LE(),
                SpeedMin = s.ReadI32(),
                SpeedMax = s.ReadI32(),
                DangerMin = s.ReadI32(),
                DangerMax = s.ReadI32(),
            });
        }

        CellBackgroundMapFile.CaptureTrailing(s, b => file.Trailing = b);
        return file;
    }

    public static CellRandomCreatureFile Open(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Read(stream);
    }

    public void Write(Stream s)
    {
        s.WriteI32(Entries.Count);
        s.WriteI32(8);
        foreach (CellRandomCreatureEntry e in Entries)
        {
            s.WriteI32(e.Type);
            s.WriteU32(e.CreatureId);
            s.WriteF32LE(e.Weight);
            s.WriteI32(e.SpeedMin);
            s.WriteI32(e.SpeedMax);
            s.WriteI32(e.DangerMin);
            s.WriteI32(e.DangerMax);
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

    public string ToText()
    {
        var sb = new StringBuilder();
        sb.Append("# Cell random creature table (text dump by SporeMaster 3)\n");
        sb.Append($"entries {Entries.Count}\n");
        foreach (CellRandomCreatureEntry e in Entries)
        {
            sb.Append(
                $"creature type {e.Type} id 0x{e.CreatureId:X8} weight {CellBackgroundMapFile.F(e.Weight)} " +
                $"speed [{e.SpeedMin}..{e.SpeedMax}] danger [{e.DangerMin}..{e.DangerMax}]\n");
        }

        return sb.ToString();
    }
}
