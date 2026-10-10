using System.Globalization;
using System.Text;
using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Hashing;
using SporeMaster.Core.IO;
using SporeMaster.Core.Names;

namespace SporeMaster.Core.Pctp;

/// <summary>A deform specification inside a <c>.pctp</c> file.</summary>
public sealed class PctpDeform
{
    public int DeformId { get; set; }

    public float Range0 { get; set; }

    public float Range1 { get; set; }

    /// <summary>Bit 0: rendered (0 = helper). Bit 1: wrap (1 = curve wraps mod 1).</summary>
    public int Flags { get; set; }
}

/// <summary>A capability name and its 4-character identifier.</summary>
public sealed class PctpCapability
{
    public PctpCapability(string name, string identifier)
    {
        Name = name;
        Identifier = identifier;
    }

    public string Name { get; }

    public string Identifier { get; }
}

/// <summary>A capability remap (a file hash pointing at a capability).</summary>
public sealed class PctpRemap
{
    public PctpRemap(int key, string identifier, int index)
    {
        Key = key;
        Identifier = identifier;
        Index = index;
    }

    public int Key { get; }

    public string Identifier { get; }

    public int Index { get; }
}

/// <summary>
/// A Spore part-capabilities file (<c>.pctp</c>, type <c>0x7C19AA7A</c>).
/// </summary>
/// <remarks>
/// The binary format mixes big-endian integers with little-endian 4-character identifiers; see
/// <c>docs/PCTP_FORMAT.md</c>. The text form is SporeModder's ArgScript (<c>.pctp_t</c>).
/// </remarks>
public sealed class PctpUnit
{
    public const int Magic = 0x70637470;

    public int Version { get; set; } = 4;

    public float Priority { get; set; }

    public List<PctpCapability> Capabilities { get; } = new();

    public List<PctpRemap> Remaps { get; } = new();

    /// <summary>aggregate identifier -> member identifiers.</summary>
    public List<KeyValuePair<string, List<string>>> Aggregates { get; } = new();

    /// <summary>deformSpec identifier -> deforms.</summary>
    public List<KeyValuePair<string, List<PctpDeform>>> DeformSpecs { get; } = new();

    // ---- binary ----

    public static PctpUnit Read(Stream stream)
    {
        if (stream.ReadI32BE() != Magic)
        {
            throw new InvalidDataException("Not a PCTP file (bad magic).");
        }

        var unit = new PctpUnit { Version = stream.ReadI32BE() };
        if (unit.Version is not (3 or 4))
        {
            throw new InvalidDataException($"Unsupported PCTP version {unit.Version}.");
        }

        if (unit.Version > 3)
        {
            unit.Priority = stream.ReadF32BE();
        }

        int capabilityCount = stream.ReadI32BE();
        for (int i = 0; i < capabilityCount; i++)
        {
            int nameLength = stream.ReadI32BE();
            string name = Encoding.ASCII.GetString(stream.ReadBytesExactly(Math.Max(0, nameLength)));
            string identifier = ReadIdentifier(stream);
            unit.Capabilities.Add(new PctpCapability(name, identifier));
        }

        int remapCount = stream.ReadI32BE();
        for (int i = 0; i < remapCount; i++)
        {
            int key = stream.ReadI32BE();
            string identifier = ReadIdentifier(stream);
            int index = stream.ReadI32BE();
            unit.Remaps.Add(new PctpRemap(key, identifier, index));
        }

        int aggregateCount = stream.ReadI32BE();
        for (int i = 0; i < aggregateCount; i++)
        {
            string key = ReadIdentifier(stream);
            int n = stream.ReadI32BE();
            var list = new List<string>(Math.Max(0, n));
            for (int j = 0; j < n; j++)
            {
                list.Add(ReadIdentifier(stream));
            }

            unit.Aggregates.Add(new KeyValuePair<string, List<string>>(key, list));
        }

        int deformCount = stream.ReadI32BE();
        for (int i = 0; i < deformCount; i++)
        {
            string identifier = ReadIdentifier(stream);
            int n = stream.ReadI32BE();
            var list = new List<PctpDeform>(Math.Max(0, n));
            for (int j = 0; j < n; j++)
            {
                var deform = new PctpDeform { DeformId = stream.ReadI32BE(), Range0 = stream.ReadF32BE() };
                if (unit.Version >= 3)
                {
                    deform.Range1 = stream.ReadF32BE();
                }

                deform.Flags = stream.ReadI32BE();
                list.Add(deform);
            }

            unit.DeformSpecs.Add(new KeyValuePair<string, List<PctpDeform>>(identifier, list));
        }

        return unit;
    }

    public void Write(Stream stream)
    {
        stream.WriteI32BE(Magic);
        stream.WriteI32BE(Version);
        if (Version > 3)
        {
            stream.WriteF32BE(Priority);
        }

        // Capability names are written alphabetically.
        List<PctpCapability> ordered = Capabilities.OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
        stream.WriteI32BE(ordered.Count);
        foreach (PctpCapability cap in ordered)
        {
            stream.WriteI32BE(cap.Name.Length);
            stream.Write(Encoding.ASCII.GetBytes(cap.Name));
            WriteIdentifier(stream, cap.Identifier);
        }

        // Every capability mapping (identity and extra) is written here.
        stream.WriteI32BE(Remaps.Count);
        foreach (PctpRemap remap in Remaps)
        {
            stream.WriteI32BE(remap.Key);
            WriteIdentifier(stream, remap.Identifier);
            stream.WriteI32BE(remap.Index);
        }

        stream.WriteI32BE(Aggregates.Count);
        foreach (KeyValuePair<string, List<string>> aggregate in Aggregates)
        {
            WriteIdentifier(stream, aggregate.Key);
            stream.WriteI32BE(aggregate.Value.Count);
            foreach (string member in aggregate.Value)
            {
                WriteIdentifier(stream, member);
            }
        }

        stream.WriteI32BE(DeformSpecs.Count);
        foreach (KeyValuePair<string, List<PctpDeform>> spec in DeformSpecs)
        {
            WriteIdentifier(stream, spec.Key);
            stream.WriteI32BE(spec.Value.Count);
            foreach (PctpDeform deform in spec.Value)
            {
                stream.WriteI32BE(deform.DeformId);
                stream.WriteF32BE(deform.Range0);
                if (Version >= 3)
                {
                    stream.WriteF32BE(deform.Range1);
                }

                stream.WriteI32BE(deform.Flags);
            }
        }
    }

    private static string ReadIdentifier(Stream stream)
    {
        byte[] bytes = stream.ReadBytesExactly(4);
        Array.Reverse(bytes);
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == 0)
            {
                bytes[i] = 0x20;
            }
        }

        return Encoding.ASCII.GetString(bytes);
    }

    private static void WriteIdentifier(Stream stream, string identifier)
    {
        var bytes = new byte[4];
        byte[] ascii = Encoding.ASCII.GetBytes(identifier.Length > 4 ? identifier[..4] : identifier);
        Array.Copy(ascii, bytes, ascii.Length);
        Array.Reverse(bytes);
        stream.Write(bytes);
    }

    // ---- text (ArgScript .pctp_t) ----

    public string ToScript(NameRegistrySet names)
    {
        var builder = new StringBuilder();
        builder.Append("version ").Append(Version).Append('\n');
        if (Version > 3)
        {
            builder.Append("priority ").Append(Format(Priority)).Append('\n');
        }

        builder.Append('\n');

        // Order capabilities by their mapping index (compile order).
        var byIndex = new SortedDictionary<int, PctpCapability>();
        var remaining = new List<PctpRemap>(Remaps);
        foreach (PctpCapability cap in Capabilities)
        {
            int key = unchecked((int)Fnv.Hash(cap.Name));
            PctpRemap? remap = remaining.FirstOrDefault(r => r.Key == key);
            if (remap is not null)
            {
                byIndex[remap.Index] = cap;
                remaining.Remove(remap);
            }
        }

        foreach (PctpCapability cap in byIndex.Values)
        {
            builder.Append("cap ").Append(cap.Name).Append(' ').Append(cap.Identifier).Append('\n');
        }

        builder.Append('\n');
        foreach (KeyValuePair<string, List<string>> aggregate in Aggregates)
        {
            builder.Append("aggregate ").Append(aggregate.Key);
            foreach (string member in aggregate.Value)
            {
                builder.Append(' ').Append(member);
            }

            builder.Append('\n');
        }

        builder.Append('\n');
        foreach (PctpRemap remap in remaining)
        {
            string capName = byIndex.TryGetValue(remap.Index, out PctpCapability? cap) ? cap.Name : remap.Index.ToString(CultureInfo.InvariantCulture);
            builder.Append("remap ").Append(names.GetFileName(unchecked((uint)remap.Key))).Append(' ').Append(capName).Append('\n');
        }

        builder.Append('\n');
        foreach (KeyValuePair<string, List<PctpDeform>> spec in DeformSpecs)
        {
            builder.Append("deformSpec ").Append(spec.Key).Append('\n');
            foreach (PctpDeform deform in spec.Value)
            {
                builder.Append("\tdeform ").Append(names.GetFileName(unchecked((uint)deform.DeformId)));
                builder.Append(' ').Append(Format(deform.Range0));
                if (Version >= 3)
                {
                    builder.Append(' ').Append(Format(deform.Range1));
                }

                builder.Append(' ').Append((deform.Flags & 1) != 0 ? "0" : "1");
                builder.Append(' ').Append((deform.Flags & 2) != 0 ? "1" : "0");
                builder.Append('\n');
            }

            builder.Append("end\n\n");
        }

        return builder.ToString();
    }

    public static PctpUnit Parse(string text, NameRegistrySet names)
    {
        var unit = new PctpUnit();
        string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        List<PctpDeform>? currentSpec = null;

        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            string[] parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            switch (parts[0])
            {
                case "version":
                    unit.Version = int.Parse(parts[1], CultureInfo.InvariantCulture);
                    break;
                case "priority":
                    unit.Priority = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    break;
                case "cap":
                    {
                        string name = parts[1];
                        string identifier = parts.Length > 2 ? parts[2] : "    ";
                        unit.Capabilities.Add(new PctpCapability(name, identifier));
                        unit.Remaps.Add(new PctpRemap(unchecked((int)Fnv.Hash(name)), identifier, unit.Capabilities.Count - 1));
                        break;
                    }

                case "aggregate":
                    {
                        var members = new List<string>();
                        for (int i = 2; i < parts.Length; i++)
                        {
                            members.Add(parts[i]);
                        }

                        unit.Aggregates.Add(new KeyValuePair<string, List<string>>(parts[1], members));
                        break;
                    }

                case "remap":
                    {
                        int key = unchecked((int)names.GetFileHash(parts[1]));
                        PctpCapability? cap = unit.Capabilities.FirstOrDefault(c => c.Name == parts[2]);
                        if (cap is not null)
                        {
                            unit.Remaps.Add(new PctpRemap(key, cap.Identifier, unit.Capabilities.IndexOf(cap)));
                        }

                        break;
                    }

                case "deformSpec":
                    currentSpec = new List<PctpDeform>();
                    unit.DeformSpecs.Add(new KeyValuePair<string, List<PctpDeform>>(parts[1], currentSpec));
                    break;
                case "deform":
                    if (currentSpec is null)
                    {
                        break;
                    }

                    {
                        var deform = new PctpDeform { DeformId = unchecked((int)names.GetFileHash(parts[1])) };
                        int idx = 2;
                        deform.Range0 = float.Parse(parts[idx++], CultureInfo.InvariantCulture);
                        if (unit.Version >= 3 && idx < parts.Length)
                        {
                            deform.Range1 = float.Parse(parts[idx++], CultureInfo.InvariantCulture);
                        }

                        if (idx < parts.Length && parts[idx++] == "0")
                        {
                            deform.Flags |= 1;
                        }

                        if (idx < parts.Length && parts[idx] != "0")
                        {
                            deform.Flags |= 2;
                        }

                        currentSpec.Add(deform);
                    }

                    break;
                case "end":
                    currentSpec = null;
                    break;
            }
        }

        return unit;
    }

    private static string Format(float value) => value.ToString("0.#######", CultureInfo.InvariantCulture);
}
