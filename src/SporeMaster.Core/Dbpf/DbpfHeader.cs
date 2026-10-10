using SporeMaster.Core.Diagnostics;
using SporeMaster.Core.IO;

namespace SporeMaster.Core.Dbpf;

/// <summary>The parsed header of a DBPF/DBBF container.</summary>
public sealed class DbpfHeader
{
    public DbpfContainer Container { get; init; }

    public DbpfVersion Version { get; init; }

    /// <summary>Number of entries recorded in the index.</summary>
    public uint IndexCount { get; init; }

    /// <summary>Absolute offset of the index header.</summary>
    public long IndexOffset { get; init; }

    /// <summary>Size in bytes of the index block.</summary>
    public long IndexSize { get; init; }

    public bool IsBig => Container == DbpfContainer.BigPacked;

    public bool IsProtected => Container == DbpfContainer.Protected;

    /// <summary>Reads a header from the current position of <paramref name="stream"/> (which must be 0).</summary>
    public static DbpfHeader Read(Stream stream, DiagnosticSink sink)
    {
        stream.Seek(0, SeekOrigin.Begin);
        uint magic = stream.ReadU32();

        DbpfContainer container = magic switch
        {
            DbpfConstants.MagicDbpf => DbpfContainer.Packed,
            DbpfConstants.MagicDbbf => DbpfContainer.BigPacked,
            DbpfConstants.MagicDbpp => DbpfContainer.Protected,
            _ => throw new NotAPackageException(
                $"Not a Spore package: expected DBPF/DBBF/DBPP magic, found 0x{magic:X8}."),
        };

        if (container == DbpfContainer.Protected)
        {
            sink.Warn("Protected container (DBPP) detected; the index may be unreadable.");
        }

        if (container == DbpfContainer.BigPacked)
        {
            uint major = stream.ReadU32();
            uint minor = stream.ReadU32();
            stream.Seek(20, SeekOrigin.Current);
            uint indexMajor = stream.ReadU32();
            uint indexCount = stream.ReadU32();
            ulong indexSize = stream.ReadU64();
            stream.Seek(4, SeekOrigin.Current);
            uint indexMinor = stream.ReadU32();
            ulong indexOffset = stream.ReadU64();
            stream.Seek(56, SeekOrigin.Current);

            if (indexOffset > long.MaxValue)
            {
                sink.Warn("DBBF index offset exceeds 64-bit range; clamping.");
                indexOffset = (ulong)stream.Length;
            }

            return new DbpfHeader
            {
                Container = container,
                Version = new DbpfVersion(major, minor, indexMajor, indexMinor),
                IndexCount = indexCount,
                IndexOffset = (long)indexOffset,
                IndexSize = (long)indexSize,
            };
        }

        uint maj = stream.ReadU32();
        uint min = stream.ReadU32();
        stream.Seek(20, SeekOrigin.Current);
        uint idxMajor = stream.ReadU32();
        uint idxCount = stream.ReadU32();
        stream.Seek(4, SeekOrigin.Current);
        uint idxSize = stream.ReadU32();
        stream.Seek(12, SeekOrigin.Current);
        uint idxMinor = stream.ReadU32();
        uint idxOffset = stream.ReadU32();

        // Remaining 28 bytes are reserved.

        return new DbpfHeader
        {
            Container = container,
            Version = new DbpfVersion(maj, min, idxMajor, idxMinor),
            IndexCount = idxCount,
            IndexOffset = idxOffset,
            IndexSize = idxSize,
        };
    }

    /// <summary>Writes an empty header, reserving room for the index metadata.</summary>
    public static void WritePlaceholder(Stream output, DbpfVersion version, bool big)
    {
        int headerSize = big ? DbpfConstants.BigHeaderSize : DbpfConstants.HeaderSize;
        var buffer = new byte[headerSize];

        // Magic.
        buffer[0] = (byte)'D';
        buffer[1] = (byte)'B';
        buffer[2] = (byte)(big ? 'B' : 'P');
        buffer[3] = (byte)'F';

        WriteU32(buffer, 0x04, version.Major);
        WriteU32(buffer, 0x08, version.Minor);

        if (big)
        {
            WriteU32(buffer, 0x20, version.IndexMajor);
            // indexCount @ 0x24, indexSize @ 0x28 (8 bytes), indexMinor @ 0x34, indexOffset @ 0x38 (8 bytes)
            WriteU32(buffer, 0x34, version.IndexMinor);
        }
        else
        {
            WriteU32(buffer, 0x20, version.IndexMajor);
            WriteU32(buffer, 0x3C, version.IndexMinor);
        }

        output.Write(buffer);
    }

    /// <summary>Patches the index metadata into an already written header.</summary>
    public static void Patch(
        Stream output,
        DbpfVersion version,
        bool big,
        uint indexCount,
        long indexOffset,
        long indexSize)
    {
        output.Seek(0x24, SeekOrigin.Begin);
        output.WriteU32(indexCount);

        if (big)
        {
            output.Seek(0x28, SeekOrigin.Begin);
            output.WriteU64((ulong)indexSize);
            output.Seek(0x38, SeekOrigin.Begin);
            output.WriteU64((ulong)indexOffset);
        }
        else
        {
            output.Seek(0x2C, SeekOrigin.Begin);
            output.WriteU32((uint)indexSize);
            output.Seek(0x40, SeekOrigin.Begin);
            output.WriteU32((uint)indexOffset);
        }
    }

    private static void WriteU32(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
    }
}
