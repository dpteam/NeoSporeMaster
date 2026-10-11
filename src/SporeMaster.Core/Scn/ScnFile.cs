using System.Globalization;
using System.Text;
using SporeMaster.Core.IO;

namespace SporeMaster.Core.Scn;

/// <summary>
/// One gameplay marker of a <c>.scn</c> level: its world placement plus a fixed-size, type-specific
/// payload (kept raw here).
/// </summary>
public sealed class ScnMarker
{
    public float OffsetX { get; set; }

    public float OffsetY { get; set; }

    public float OffsetZ { get; set; }

    public float OrientX { get; set; }

    public float OrientY { get; set; }

    public float OrientZ { get; set; }

    public float OrientW { get; set; }

    public int Type { get; set; }

    public int Id { get; set; }

    public int DefinitionId { get; set; }

    /// <summary>The type-specific data payload (usually 0x88 = 136 bytes).</summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>Version-2 only: the raw extra block (flag + count + words), kept for a byte-exact round-trip.</summary>
    public byte[]? V2Extra { get; set; }
}

/// <summary>
/// A Spore scenario/level file (type <c>0x047B8300</c>, extension <c>.scn</c>/<c>.lvl</c>), as read by
/// SporeModder FX's <c>LevelDefinition</c>: a version/count/size header followed by markers. All
/// scalars are big-endian; the layout round-trips byte-for-byte.
/// </summary>
public sealed class ScnFile
{
    public int Version { get; set; } = 3;

    /// <summary>Fixed marker payload size (normally 0x88).</summary>
    public int DataSize { get; set; } = 0x88;

    public List<ScnMarker> Markers { get; } = new();

    /// <summary>Bytes after the markers (preserved for a byte-exact round-trip).</summary>
    public byte[] Trailing { get; set; } = Array.Empty<byte>();

    public static ScnFile Read(Stream s)
    {
        var file = new ScnFile();
        file.Version = s.ReadI32BE();
        int count = s.ReadI32BE();
        file.DataSize = s.ReadU16BE();

        if (count is < 0 or > 10_000_000 || file.DataSize > 0x10000)
        {
            throw new InvalidDataException("Unreasonable .scn marker count or data size.");
        }

        for (int i = 0; i < count; i++)
        {
            var marker = new ScnMarker
            {
                OffsetX = s.ReadF32BE(),
                OffsetY = s.ReadF32BE(),
                OffsetZ = s.ReadF32BE(),
                OrientX = s.ReadF32BE(),
                OrientY = s.ReadF32BE(),
                OrientZ = s.ReadF32BE(),
                OrientW = s.ReadF32BE(),
                Type = s.ReadI32BE(),
                Id = s.ReadI32BE(),
                DefinitionId = s.ReadI32BE(),
            };

            if (file.Version == 2)
            {
                byte[] head = s.ReadBytesExactly(5);
                int extra = (head[1] << 24) | (head[2] << 16) | (head[3] << 8) | head[4];
                if (extra is < 0 or > 1_000_000)
                {
                    throw new InvalidDataException("Unreasonable .scn v2 extra count.");
                }

                byte[] tail = s.ReadBytesExactly(extra * 4);
                marker.V2Extra = new byte[head.Length + tail.Length];
                Array.Copy(head, marker.V2Extra, head.Length);
                Array.Copy(tail, 0, marker.V2Extra, head.Length, tail.Length);
            }

            marker.Data = s.ReadBytesExactly(file.DataSize);
            file.Markers.Add(marker);
        }

        long remaining = s.Length - s.Position;
        if (remaining > 0)
        {
            file.Trailing = s.ReadBytesExactly((int)remaining);
        }

        return file;
    }

    public static ScnFile Open(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Read(stream);
    }

    public void Write(Stream s)
    {
        s.WriteI32BE(Version);
        s.WriteI32BE(Markers.Count);
        s.WriteU16BE((ushort)DataSize);

        foreach (ScnMarker marker in Markers)
        {
            s.WriteF32BE(marker.OffsetX);
            s.WriteF32BE(marker.OffsetY);
            s.WriteF32BE(marker.OffsetZ);
            s.WriteF32BE(marker.OrientX);
            s.WriteF32BE(marker.OrientY);
            s.WriteF32BE(marker.OrientZ);
            s.WriteF32BE(marker.OrientW);
            s.WriteI32BE(marker.Type);
            s.WriteI32BE(marker.Id);
            s.WriteI32BE(marker.DefinitionId);

            if (marker.V2Extra is not null)
            {
                s.Write(marker.V2Extra);
            }

            byte[] data = marker.Data.Length == DataSize
                ? marker.Data
                : PadOrTrim(marker.Data, DataSize);
            s.Write(data);
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
        sb.Append("# Spore level / scenario (.scn) dump by SporeMaster 3\n");
        sb.Append($"version {Version}\n");
        sb.Append($"dataSize {DataSize}\n");
        sb.Append($"markers {Markers.Count}\n\n");

        for (int i = 0; i < Markers.Count; i++)
        {
            ScnMarker m = Markers[i];
            sb.Append(
                $"marker [{i}] type {m.Type} id {m.Id} definition {m.DefinitionId} " +
                $"offset ({F(m.OffsetX)}, {F(m.OffsetY)}, {F(m.OffsetZ)}) " +
                $"orient ({F(m.OrientX)}, {F(m.OrientY)}, {F(m.OrientZ)}, {F(m.OrientW)})\n");
            sb.Append($"  data: {Convert.ToHexString(m.Data)}\n");
        }

        return sb.ToString();
    }

    private static byte[] PadOrTrim(byte[] data, int size)
    {
        var result = new byte[size];
        Array.Copy(data, result, Math.Min(data.Length, size));
        return result;
    }

    private static string F(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);
}
