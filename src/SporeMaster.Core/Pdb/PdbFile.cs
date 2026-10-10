using System.Buffers.Binary;
using System.Text;

namespace SporeMaster.Core.Pdb;

/// <summary>One module (object file) recorded in a PDB's DBI stream.</summary>
public sealed class PdbModule
{
    public string Name { get; set; } = string.Empty;

    public string ObjectFile { get; set; } = string.Empty;

    public ushort SymbolStreamIndex { get; set; }

    public uint SymbolBytes { get; set; }
}

/// <summary>A single symbol recovered from a PDB (name plus its CodeView kind).</summary>
public sealed class PdbSymbol
{
    public required string Name { get; init; }

    public required ushort Kind { get; init; }

    public string KindName => PdbFile.SymbolKindName(Kind);
}

/// <summary>A parsed PDB file: container summary, DBI header, modules and symbol names.</summary>
public sealed class PdbFile : IDisposable
{
    // CodeView symbol record kinds (subset that carries a readable name).
    public const ushort S_UDT = 0x1108;
    public const ushort S_LDATA32 = 0x110C;
    public const ushort S_GDATA32 = 0x110D;
    public const ushort S_PUB32 = 0x110E;
    public const ushort S_LPROC32 = 0x110F;
    public const ushort S_GPROC32 = 0x1110;
    public const ushort S_LTHREAD32 = 0x1112;
    public const ushort S_GTHREAD32 = 0x1113;
    public const ushort S_LPROCREF = 0x1127;
    public const ushort S_PROCREF = 0x1125;
    public const ushort S_LABEL32 = 0x1105;

    private readonly MsfFile _msf;

    private PdbFile(MsfFile msf)
    {
        _msf = msf;
    }

    public MsfFile Container => _msf;

    /// <summary>PDB info stream (stream 1) version.</summary>
    public uint InfoVersion { get; private set; }

    /// <summary>The GUID that identifies the matching executable.</summary>
    public string InfoGuid { get; private set; } = string.Empty;

    public uint InfoAge { get; private set; }

    // DBI header (stream 3).
    public uint DbiVersionHeader { get; private set; }

    public uint DbiAge { get; private set; }

    public ushort DbiMachine { get; private set; }

    public ushort GlobalSymbolStream { get; private set; }

    public ushort PublicSymbolStream { get; private set; }

    public ushort SymbolRecordStream { get; private set; }

    /// <summary>Size of the module-info sub-stream inside the DBI stream.</summary>
    public uint ModiSubstreamSize { get; private set; }

    public static PdbFile Open(string path)
    {
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        try
        {
            PdbFile file = Read(stream, ownsStream: true);
            return file;
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    public static PdbFile Read(Stream stream, bool ownsStream = false)
    {
        var msf = MsfFile.Read(stream, ownsStream);
        var pdb = new PdbFile(msf);
        pdb.ReadInfo();
        pdb.ReadDbi();
        return pdb;
    }

    private void ReadInfo()
    {
        byte[] info = _msf.ReadStream(1);
        if (info.Length < 28)
        {
            return;
        }

        int offset = 0;
        InfoVersion = ReadU32(info, ref offset);
        byte[] guid = new byte[16];
        Array.Copy(info, offset, guid, 0, 16);
        offset += 16;
        InfoAge = ReadU32(info, ref offset);
        InfoGuid = FormatGuid(guid);
    }

    private void ReadDbi()
    {
        byte[] dbi = _msf.ReadStream(3);
        if (dbi.Length < 64)
        {
            return;
        }

        int offset = 4; // skip VersionSignature (always -1)
        DbiVersionHeader = ReadU32(dbi, ref offset);
        DbiAge = ReadU32(dbi, ref offset);
        GlobalSymbolStream = ReadU16(dbi, ref offset);
        offset += 2; // BuildNumber
        PublicSymbolStream = ReadU16(dbi, ref offset);
        offset += 2; // PdbDllVersion
        SymbolRecordStream = ReadU16(dbi, ref offset);
        offset += 2; // PdbDllRbld
        ModiSubstreamSize = ReadU32(dbi, ref offset);
        offset += 4 * 6; // seccontrib, secmap, fileinfo, tsmap, mfc, optionaldbg
        offset += 4; // ECSubstreamSize
        offset += 2; // Flags
        DbiMachine = ReadU16(dbi, ref offset);
    }

    /// <summary>Reads the module list from the DBI module-info sub-stream.</summary>
    /// <remarks>
    /// The exact module-descriptor layout varies between PDB versions (the section-contribution entry
    /// changed size), so instead of risking a mis-parse we scan the module-info region for the file
    /// paths it stores (internal build paths, object/source names). This is what is useful for
    /// research and is robust across versions.
    /// </remarks>
    public IReadOnlyList<PdbModule> ReadModules()
    {
        var modules = new List<PdbModule>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        byte[] dbi = _msf.ReadStream(3);
        if (dbi.Length < 64 || ModiSubstreamSize == 0)
        {
            return modules;
        }

        int start = 64;
        int end = Math.Min(dbi.Length, 64 + (int)ModiSubstreamSize);

        int runStart = -1;
        for (int i = start; i <= end; i++)
        {
            bool printable = i < end && dbi[i] is >= 32 and < 127;
            if (printable)
            {
                if (runStart < 0)
                {
                    runStart = i;
                }
            }
            else if (runStart >= 0)
            {
                int length = i - runStart;
                if (length >= 4)
                {
                    string value = Encoding.ASCII.GetString(dbi, runStart, length);
                    if (LooksLikeModulePath(value) && seen.Add(value))
                    {
                        modules.Add(new PdbModule { Name = value, ObjectFile = value });
                    }
                }

                runStart = -1;
            }
        }

        return modules;
    }

    private static bool LooksLikeModulePath(string value)
    {
        if (value.Contains('\\'))
        {
            return true;
        }

        string lower = value.ToLowerInvariant();
        return lower.EndsWith(".obj") || lower.EndsWith(".res") || lower.EndsWith(".cpp")
            || lower.EndsWith(".c") || lower.EndsWith(".h") || lower.EndsWith(".hpp")
            || lower.EndsWith(".cs");
    }

    /// <summary>Reads every readable symbol name from the symbol record stream.</summary>
    public IReadOnlyList<PdbSymbol> ReadSymbols()
    {
        var symbols = new List<PdbSymbol>();
        byte[] data = _msf.ReadStream(SymbolRecordStream);
        ReadSymbolStream(data, symbols);
        return symbols;
    }

    /// <summary>Reads readable symbols from the global symbol stream.</summary>
    public IReadOnlyList<PdbSymbol> ReadGlobalSymbols()
    {
        var symbols = new List<PdbSymbol>();
        byte[] data = _msf.ReadStream(GlobalSymbolStream);
        ReadSymbolStream(data, symbols);
        return symbols;
    }

    private static void ReadSymbolStream(byte[] data, List<PdbSymbol> output)
    {
        int offset = 0;
        while (offset + 4 <= data.Length)
        {
            int length = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset));
            if (length < 2 || offset + 2 + length > data.Length)
            {
                break;
            }

            ushort kind = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset + 2));
            string? name = DecodeName(data, offset + 4, length - 2, kind);
            if (!string.IsNullOrEmpty(name))
            {
                output.Add(new PdbSymbol { Name = name!, Kind = kind });
            }

            offset += 2 + length;
        }
    }

    /// <summary>Extracts the name carried by a CodeView symbol record, or null when it has none.</summary>
    private static string? DecodeName(byte[] data, int payloadOffset, int payloadLength, ushort kind)
    {
        switch (kind)
        {
            case S_UDT:
                return ReadCString(data, payloadOffset + 4, payloadOffset + payloadLength);
            case S_LDATA32:
            case S_GDATA32:
            case S_LTHREAD32:
            case S_GTHREAD32:
            case S_PUB32:
            case S_PROCREF:
            case S_LPROCREF:
                return ReadCString(data, payloadOffset + 10, payloadOffset + payloadLength);
            case S_LPROC32:
            case S_GPROC32:
                return ReadCString(data, payloadOffset + 35, payloadOffset + payloadLength);
            case S_LABEL32:
                return ReadCString(data, payloadOffset + 7, payloadOffset + payloadLength);
            default:
                return null;
        }
    }

    private static string ReadCString(byte[] data, int offset, int limit)
    {
        int end = offset;
        while (end < limit && end < data.Length && data[end] != 0)
        {
            end++;
        }

        if (end <= offset)
        {
            return string.Empty;
        }

        return Encoding.UTF8.GetString(data, offset, end - offset);
    }

    private static string ReadCString(byte[] data, ref int offset)
    {
        int start = offset;
        while (offset < data.Length && data[offset] != 0)
        {
            offset++;
        }

        string result = offset > start ? Encoding.UTF8.GetString(data, start, offset - start) : string.Empty;
        if (offset < data.Length)
        {
            offset++; // skip the terminator
        }

        return result;
    }

    public static string SymbolKindName(ushort kind) => kind switch
    {
        S_UDT => "UDT",
        S_LDATA32 => "LDATA32",
        S_GDATA32 => "GDATA32",
        S_PUB32 => "PUB32",
        S_LPROC32 => "LPROC32",
        S_GPROC32 => "GPROC32",
        S_LTHREAD32 => "LTHREAD32",
        S_GTHREAD32 => "GTHREAD32",
        S_LABEL32 => "LABEL32",
        S_PROCREF => "PROCREF",
        S_LPROCREF => "LPROCREF",
        _ => $"0x{kind:X4}",
    };

    private static string FormatGuid(byte[] guid)
    {
        if (guid.Length < 16)
        {
            return string.Empty;
        }

        uint a = BinaryPrimitives.ReadUInt32LittleEndian(guid);
        ushort b = BinaryPrimitives.ReadUInt16LittleEndian(guid.AsSpan(4));
        ushort c = BinaryPrimitives.ReadUInt16LittleEndian(guid.AsSpan(6));
        return $"{a:X8}-{b:X4}-{c:X4}-{guid[8]:X2}{guid[9]:X2}-{Convert.ToHexString(guid.AsSpan(10, 6))}";
    }

    private static uint ReadU32(byte[] data, ref int offset)
    {
        uint value = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset));
        offset += 4;
        return value;
    }

    private static ushort ReadU16(byte[] data, ref int offset)
    {
        ushort value = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset));
        offset += 2;
        return value;
    }

    public void Dispose() => _msf.Dispose();
}
