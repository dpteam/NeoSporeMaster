using System.Globalization;
using System.Text;
using SporeMaster.Core.IO;

namespace SporeMaster.Core.Cell;

/// <summary>The kind of a cell populate marker (mirrors SporeModder's marker types).</summary>
public enum CellPopulateMarkerType
{
    Distribute = 0,
    Cluster = 1,
    Plant = 2,
    Encounter = 3,
}

/// <summary>One marker inside a cell populate file (a spawn point of a resource).</summary>
public sealed class CellPopulateMarker
{
    public int Field0 { get; set; }

    public int Field4 { get; set; }

    public int Field8 { get; set; }

    public float ZOffset { get; set; }

    public float ZOffsetMax { get; set; }

    public int Field14 { get; set; }

    public uint DistributeCellId { get; set; }

    public uint ClusterCellId { get; set; }

    public uint EncounterPopulateId { get; set; }

    public int PlantType { get; set; }

    public CellPopulateMarkerType Type { get; set; }

    public float Count { get; set; } = -1f;

    public float CountEasy { get; set; } = -1f;

    public float CountMed { get; set; } = -1f;

    public float CountHard { get; set; } = -1f;

    public int Size { get; set; } = 1;

    public int Parts { get; set; } = 5;

    public int Linear { get; set; }

    public float EncounterScale { get; set; }
}

/// <summary>
/// A cell populate file (type <c>0xDA141C1B</c>): a header (scale, mask texture, count, offset) and
/// <see cref="CellPopulateMarker"/> records (76 bytes each). Little-endian; the layout matches
/// SporeModder FX and round-trips byte-for-byte for the release data.
/// </summary>
public sealed class CellPopulateFile
{
    public int Scale { get; set; } = 1;

    public uint MaskTexture { get; set; }

    /// <summary>The header offset (0x10 when populated, 0 when empty); preserved for byte-exactness.</summary>
    public int Offset { get; set; } = 0x10;

    public List<CellPopulateMarker> Markers { get; } = new();

    public byte[] Trailing { get; set; } = Array.Empty<byte>();

    public static CellPopulateFile Read(Stream s)
    {
        var file = new CellPopulateFile();
        int scale = s.ReadI32();
        uint maskTexture = s.ReadU32();
        int count = s.ReadI32();
        long offset = s.ReadU32();
        if (offset != 0x10 && count != 0)
        {
            throw new InvalidDataException($"Cell populate offset was {offset}, expected 0x10.");
        }

        if (count is < 0 or > 1_000_000)
        {
            throw new InvalidDataException("Unreasonable cell populate marker count.");
        }

        file.Scale = scale;
        file.MaskTexture = maskTexture;
        file.Offset = (int)offset;

        for (int i = 0; i < count; i++)
        {
            file.Markers.Add(new CellPopulateMarker
            {
                Field0 = s.ReadI32(),
                Field4 = s.ReadI32(),
                Field8 = s.ReadI32(),
                ZOffset = s.ReadF32LE(),
                ZOffsetMax = s.ReadF32LE(),
                Field14 = s.ReadI32(),
                DistributeCellId = s.ReadU32(),
                ClusterCellId = s.ReadU32(),
                EncounterPopulateId = s.ReadU32(),
                PlantType = s.ReadI32(),
                Type = (CellPopulateMarkerType)s.ReadI32(),
                Count = s.ReadF32LE(),
                CountEasy = s.ReadF32LE(),
                CountMed = s.ReadF32LE(),
                CountHard = s.ReadF32LE(),
                Size = s.ReadI32(),
                Parts = s.ReadI32(),
                Linear = s.ReadI32(),
                EncounterScale = s.ReadF32LE(),
            });
        }

        CellBackgroundMapFile.CaptureTrailing(s, b => file.Trailing = b);
        return file;
    }

    public static CellPopulateFile Open(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Read(stream);
    }

    public void Write(Stream s)
    {
        s.WriteI32(Scale);
        s.WriteU32(MaskTexture);
        s.WriteI32(Markers.Count);
        s.WriteI32(Offset);

        foreach (CellPopulateMarker marker in Markers)
        {
            s.WriteI32(marker.Field0);
            s.WriteI32(marker.Field4);
            s.WriteI32(marker.Field8);
            s.WriteF32LE(marker.ZOffset);
            s.WriteF32LE(marker.ZOffsetMax);
            s.WriteI32(marker.Field14);
            s.WriteU32(marker.DistributeCellId);
            s.WriteU32(marker.ClusterCellId);
            s.WriteU32(marker.EncounterPopulateId);
            s.WriteI32(marker.PlantType);
            s.WriteI32((int)marker.Type);
            s.WriteF32LE(marker.Count);
            s.WriteF32LE(marker.CountEasy);
            s.WriteF32LE(marker.CountMed);
            s.WriteF32LE(marker.CountHard);
            s.WriteI32(marker.Size);
            s.WriteI32(marker.Parts);
            s.WriteI32(marker.Linear);
            s.WriteF32LE(marker.EncounterScale);
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
        string F(float v) => CellBackgroundMapFile.F(v);
        var sb = new StringBuilder();
        sb.Append("# Cell populate (text dump by SporeMaster 3)\n");
        sb.Append($"scale {Scale}\n");
        sb.Append($"maskTexture 0x{MaskTexture:X8}\n");
        sb.Append($"markers {Markers.Count}\n");
        foreach (CellPopulateMarker m in Markers)
        {
            sb.Append(
                $"cMarker {m.Type} {m.Field0} {m.Field4} {m.Field8} count {F(m.Count)} size {m.Size} " +
                $"zOffset {F(m.ZOffset)} zOffsetMax {F(m.ZOffsetMax)}");
            if (m.Type == CellPopulateMarkerType.Distribute)
            {
                sb.Append($" distributeCellID 0x{m.DistributeCellId:X8}");
            }
            else if (m.Type == CellPopulateMarkerType.Cluster)
            {
                sb.Append($" clusterCellID 0x{m.ClusterCellId:X8} parts {m.Parts} linear {m.Linear}");
            }
            else if (m.Type == CellPopulateMarkerType.Plant)
            {
                sb.Append($" plantType {m.PlantType}");
            }
            else if (m.Type == CellPopulateMarkerType.Encounter)
            {
                sb.Append($" encounterPopulateID 0x{m.EncounterPopulateId:X8} encounterScale {F(m.EncounterScale)}");
            }

            sb.Append('\n');
        }

        return sb.ToString();
    }
}

/// <summary>One attachment of a cell structure (bone, kind, ids, level range and colour).</summary>
public sealed class CellStructureAttachment
{
    public int Bone { get; set; }

    public int Type { get; set; }

    public uint StructureId { get; set; }

    public uint RandomCreatureId { get; set; }

    public uint EffectId { get; set; }

    public int LevelMin { get; set; }

    public int LevelMax { get; set; }

    public float ColorR { get; set; } = 1f;

    public float ColorG { get; set; } = 1f;

    public float ColorB { get; set; } = 1f;
}

/// <summary>
/// A cell structure file (type <c>0x4B9EF6DC</c>): five death/hatch effect ids, a pointer-style header
/// (offset 28) and <see cref="CellStructureAttachment"/> records (40 bytes each). Little-endian; the
/// layout matches SporeModder FX and round-trips byte-for-byte for the release data.
/// </summary>
public sealed class CellStructureFile
{
    public int OnDeath { get; set; }

    public int OnDeathSmall { get; set; }

    public int OnDeathLarge { get; set; }

    public int OnHatch { get; set; }

    public int OnStartHatch { get; set; }

    public List<CellStructureAttachment> Entries { get; } = new();

    public byte[] Trailing { get; set; } = Array.Empty<byte>();

    public static CellStructureFile Read(Stream s)
    {
        var file = new CellStructureFile
        {
            OnDeath = s.ReadI32(),
            OnDeathSmall = s.ReadI32(),
            OnDeathLarge = s.ReadI32(),
            OnHatch = s.ReadI32(),
            OnStartHatch = s.ReadI32(),
        };

        long offset = s.ReadU32();
        int count = s.ReadI32();
        if (offset != 28)
        {
            throw new InvalidDataException($"Cell structure offset was {offset}, expected 28.");
        }

        if (count is < 0 or > 1_000_000)
        {
            throw new InvalidDataException("Unreasonable cell structure entry count.");
        }

        for (int i = 0; i < count; i++)
        {
            file.Entries.Add(new CellStructureAttachment
            {
                Bone = s.ReadI32(),
                Type = s.ReadI32(),
                StructureId = s.ReadU32(),
                RandomCreatureId = s.ReadU32(),
                EffectId = s.ReadU32(),
                LevelMin = s.ReadI32(),
                LevelMax = s.ReadI32(),
                ColorR = s.ReadF32LE(),
                ColorG = s.ReadF32LE(),
                ColorB = s.ReadF32LE(),
            });
        }

        CellBackgroundMapFile.CaptureTrailing(s, b => file.Trailing = b);
        return file;
    }

    public static CellStructureFile Open(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Read(stream);
    }

    public void Write(Stream s)
    {
        s.WriteI32(OnDeath);
        s.WriteI32(OnDeathSmall);
        s.WriteI32(OnDeathLarge);
        s.WriteI32(OnHatch);
        s.WriteI32(OnStartHatch);
        s.WriteI32(28);
        s.WriteI32(Entries.Count);

        foreach (CellStructureAttachment e in Entries)
        {
            s.WriteI32(e.Bone);
            s.WriteI32(e.Type);
            s.WriteU32(e.StructureId);
            s.WriteU32(e.RandomCreatureId);
            s.WriteU32(e.EffectId);
            s.WriteI32(e.LevelMin);
            s.WriteI32(e.LevelMax);
            s.WriteF32LE(e.ColorR);
            s.WriteF32LE(e.ColorG);
            s.WriteF32LE(e.ColorB);
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
        string F(float v) => CellBackgroundMapFile.F(v);
        var sb = new StringBuilder();
        sb.Append("# Cell structure (text dump by SporeMaster 3)\n");
        sb.Append($"onDeath 0x{OnDeath:X8}\n");
        sb.Append($"onDeathSmall 0x{OnDeathSmall:X8}\n");
        sb.Append($"onDeathLarge 0x{OnDeathLarge:X8}\n");
        sb.Append($"onHatch 0x{OnHatch:X8}\n");
        sb.Append($"onStartHatch 0x{OnStartHatch:X8}\n");
        sb.Append($"attachments {Entries.Count}\n");
        foreach (CellStructureAttachment e in Entries)
        {
            sb.Append(
                $"attachment bone {e.Bone} type {e.Type} structureID 0x{e.StructureId:X8} " +
                $"randomCreatureID 0x{e.RandomCreatureId:X8} effectID 0x{e.EffectId:X8} " +
                $"level [{e.LevelMin}..{e.LevelMax}] color {F(e.ColorR)},{F(e.ColorG)},{F(e.ColorB)}\n");
        }

        return sb.ToString();
    }
}
