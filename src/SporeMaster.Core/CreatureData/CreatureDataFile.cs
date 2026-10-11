using System.Globalization;
using System.Text;
using SporeMaster.Core.IO;

namespace SporeMaster.Core.CreatureData;

/// <summary>One PCTP capability identifier and its number inside a <c>.creaturedata</c> file.</summary>
public sealed class CreatureCapability
{
    public string Id { get; set; } = string.Empty;

    public byte Number { get; set; }
}

/// <summary>One morph target of a <c>.creaturedata</c> file: its id and two values.</summary>
public sealed class CreatureMorph
{
    public uint Id { get; set; }

    public float Value1 { get; set; }

    public float Value2 { get; set; }
}

/// <summary>
/// A Spore creature editor save (type <c>0x0F43029A</c>, extension <c>.creaturedata</c>). The layout
/// follows SporeModder FX's <c>CreatureData</c>: a header, a small run-length-compressed buffer and
/// several data tables. Segments we do not interpret are kept verbatim, so writing reproduces the
/// file byte-for-byte.
/// </summary>
public sealed class CreatureDataFile
{
    public const int Header = unchecked((int)0xABB455B7);

    public int Version { get; set; }

    /// <summary>Raw <c>counts[0]</c> rows (0x8C bytes each).</summary>
    public byte[] Data0 { get; set; } = Array.Empty<byte>();

    public List<CreatureCapability> Capabilities { get; } = new();

    public List<CreatureMorph> Morphs { get; } = new();

    public int Field110 { get; set; }

    /// <summary>Version 10 only: <c>counts[3]</c> rows of 0x38 bytes.</summary>
    public byte[] Data3 { get; set; } = Array.Empty<byte>();

    /// <summary>The uncompressed header buffer (kept raw).</summary>
    public byte[] UncompressedBuffer { get; set; } = Array.Empty<byte>();

    /// <summary>The raw run-length-compressed buffer bytes (size prefixes and data).</summary>
    public byte[] CompressedBuffer { get; set; } = Array.Empty<byte>();

    public static CreatureDataFile Read(Stream s)
    {
        var file = new CreatureDataFile();

        int header = s.ReadI32();
        if (header != Header)
        {
            throw new InvalidDataException($"Unexpected .creaturedata header 0x{header:X8}.");
        }

        file.Version = s.ReadI32();
        if (file.Version is < 9 or > 10)
        {
            throw new InvalidDataException($"Unsupported .creaturedata version {file.Version}.");
        }

        int countCount = file.Version < 10 ? 3 : 4;
        var counts = new int[countCount];
        for (int i = 0; i < countCount; i++)
        {
            counts[i] = s.ReadI32();
            if (counts[i] is < 0 or > 1_000_000)
            {
                throw new InvalidDataException("Unreasonable .creaturedata count.");
            }
        }

        int uncompressedLength = file.Version >= 8 ? 0x80 - 0x3C : 0x80;
        (byte[] raw, byte[] decompressed) = ReadCompressedBuffer(s, uncompressedLength);
        file.CompressedBuffer = raw;
        file.UncompressedBuffer = decompressed;

        file.Data0 = s.ReadBytesExactly(counts[0] * 0x8C);

        var capabilityIds = new string[counts[1]];
        for (int i = 0; i < counts[1]; i++)
        {
            byte[] four = s.ReadBytesExactly(4);
            capabilityIds[i] = Encoding.ASCII.GetString(four);
        }

        for (int i = 0; i < counts[1]; i++)
        {
            file.Capabilities.Add(new CreatureCapability
            {
                Id = capabilityIds[i],
                Number = s.ReadU8(),
            });
        }

        var morphIds = new uint[counts[2]];
        for (int i = 0; i < counts[2]; i++)
        {
            morphIds[i] = s.ReadU32();
        }

        var values1 = new float[counts[2]];
        for (int i = 0; i < counts[2]; i++)
        {
            values1[i] = s.ReadF32LE();
        }

        var values2 = new float[counts[2]];
        for (int i = 0; i < counts[2]; i++)
        {
            values2[i] = s.ReadF32LE();
        }

        for (int i = 0; i < counts[2]; i++)
        {
            file.Morphs.Add(new CreatureMorph { Id = morphIds[i], Value1 = values1[i], Value2 = values2[i] });
        }

        file.Field110 = s.ReadI32();

        if (file.Version >= 10)
        {
            file.Data3 = s.ReadBytesExactly(counts[3] * 0x38);
        }

        return file;
    }

    public static CreatureDataFile Open(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Read(stream);
    }

    public void Write(Stream s)
    {
        s.WriteI32(Header);
        s.WriteI32(Version);

        int countCount = Version < 10 ? 3 : 4;
        int count0 = Data0.Length / 0x8C;
        int count1 = Capabilities.Count;
        int count2 = Morphs.Count;
        s.WriteI32(count0);
        s.WriteI32(count1);
        s.WriteI32(count2);
        if (countCount == 4)
        {
            s.WriteI32(Data3.Length / 0x38);
        }

        s.Write(CompressedBuffer);
        s.Write(Data0);

        foreach (CreatureCapability capability in Capabilities)
        {
            byte[] four = new byte[4];
            Encoding.ASCII.GetBytes(capability.Id, 0, Math.Min(4, capability.Id.Length), four, 0);
            s.Write(four);
        }

        foreach (CreatureCapability capability in Capabilities)
        {
            s.WriteByte(capability.Number);
        }

        foreach (CreatureMorph morph in Morphs)
        {
            s.WriteU32(morph.Id);
        }

        foreach (CreatureMorph morph in Morphs)
        {
            s.WriteF32LE(morph.Value1);
        }

        foreach (CreatureMorph morph in Morphs)
        {
            s.WriteF32LE(morph.Value2);
        }

        s.WriteI32(Field110);
        s.Write(Data3);
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
        sb.Append("# Spore .creaturedata (text dump by SporeMaster 3)\n");
        sb.Append($"version {Version}\n");
        sb.Append($"rows data0 {Data0.Length / 0x8C}\n");
        sb.Append($"field110 0x{Field110:X8}\n\n");

        sb.Append($"capabilities {Capabilities.Count}\n");
        foreach (CreatureCapability capability in Capabilities)
        {
            sb.Append($"  {capability.Id}: {capability.Number}\n");
        }

        sb.Append($"\nmorphs {Morphs.Count}\n");
        foreach (CreatureMorph morph in Morphs)
        {
            sb.Append(
                $"  0x{morph.Id:X8}: {F(morph.Value1)}  {F(morph.Value2)}\n");
        }

        return sb.ToString();
    }

    private static string F(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    private static (byte[] Raw, byte[] Decompressed) ReadCompressedBuffer(Stream s, int length)
    {
        var raw = new List<byte>();
        var dst = new byte[length];
        int dstIndex = 0;
        byte lastByte = 0;
        byte[]? current = null;
        int indexInBuffer = 0;

        while (dstIndex < length)
        {
            if (current is null || indexInBuffer >= current.Length)
            {
                int size = s.ReadU16();
                if (size < 1 || size > 0x800)
                {
                    throw new InvalidDataException($"Bad .creaturedata compressed chunk size {size}.");
                }

                byte[] chunk = s.ReadBytesExactly(size);
                raw.Add((byte)(size & 0xFF));
                raw.Add((byte)(size >> 8));
                raw.AddRange(chunk);

                indexInBuffer = 0;
                current = chunk;
            }

            if (current[indexInBuffer] != 254)
            {
                // Mirrors SporeModder FX, which assigns the chunk's first byte here (not the current
                // one). The decompressed buffer is only informational; raw bytes are kept verbatim.
                lastByte = current[0];
                dst[dstIndex] = lastByte;
                dstIndex++;
            }
            else
            {
                indexInBuffer++;
                if (indexInBuffer < current.Length)
                {
                    int count = current[indexInBuffer];
                    if (count == 0)
                    {
                        dst[dstIndex++] = 0xFE;
                    }
                    else
                    {
                        for (int i = 0; i < count && dstIndex < length; i++)
                        {
                            dst[dstIndex++] = lastByte;
                        }
                    }
                }
            }

            indexInBuffer++;
        }

        return (raw.ToArray(), dst);
    }
}
