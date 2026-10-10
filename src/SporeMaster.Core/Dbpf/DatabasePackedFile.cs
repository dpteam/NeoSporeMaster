using SporeMaster.Core.Compression;
using SporeMaster.Core.Diagnostics;
using SporeMaster.Core.IO;

namespace SporeMaster.Core.Dbpf;

/// <summary>Options controlling how a package is written.</summary>
public sealed class DbpfWriteOptions
{
    /// <summary>Header + index version. Defaults to DBPF 2.0 with index 0.3 (SporeMaster's classic output).</summary>
    public DbpfVersion Version { get; set; } = DbpfVersion.Default;

    /// <summary>When true each payload is RefPack-compressed if that makes it smaller.</summary>
    public bool Compress { get; set; }

    /// <summary>When true a DBBF (big) container with 64-bit offsets is produced.</summary>
    public bool Big { get; set; }
}

/// <summary>A key/payload pair used when building a package.</summary>
public readonly record struct DbpfWriteItem(ResourceKey Key, byte[] Data);

/// <summary>
/// A Spore DBPF/DBBF package. Reading is fault-tolerant: malformed fields are reported
/// through <see cref="Diagnostics"/> and parsing continues wherever a useful result remains.
/// </summary>
public sealed class DatabasePackedFile : IDisposable
{
    private readonly object _readLock = new();
    private Stream? _stream;
    private bool _ownsStream;
    private long _length;

    public DbpfContainer Container { get; private set; } = DbpfContainer.Packed;

    public DbpfVersion Version { get; private set; } = DbpfVersion.Default;

    /// <summary>The parsed header (available once the index has been read).</summary>
    public DbpfHeader? Header { get; private set; }

    /// <summary>The index header type-bits (which of type/group/unknown are shared constants).</summary>
    public int IndexType { get; private set; }

    public uint ConstantTypeId { get; private set; } = uint.MaxValue;

    public uint ConstantGroupId { get; private set; } = uint.MaxValue;

    public uint ConstantUnknown { get; private set; } = uint.MaxValue;

    public List<DbpfEntry> Entries { get; } = new();

    public DiagnosticSink Diagnostics { get; } = new();

    public string? SourceName { get; private set; }

    public bool IsBig => Container == DbpfContainer.BigPacked;

    public bool IsProtected => Container == DbpfContainer.Protected;

    public long Length => _length;

    private DatabasePackedFile()
    {
    }

    /// <summary>Opens a package from disk and reads its index.</summary>
    public static DatabasePackedFile Open(string path)
    {
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1 << 20);
        var package = Read(stream, ownsStream: true);
        package.SourceName = path;
        return package;
    }

    /// <summary>Reads a package from an already open, seekable stream.</summary>
    public static DatabasePackedFile Read(Stream stream, bool ownsStream = false, string? sourceName = null)
    {
        var package = new DatabasePackedFile
        {
            _stream = stream,
            _ownsStream = ownsStream,
            _length = stream.CanSeek ? stream.Length : 0,
            SourceName = sourceName,
        };

        try
        {
            package.ReadHeaderAndIndex();
        }
        catch (NotAPackageException)
        {
            if (ownsStream)
            {
                stream.Dispose();
            }

            throw;
        }

        return package;
    }

    private void ReadHeaderAndIndex()
    {
        if (_stream is null)
        {
            throw new InvalidOperationException("No stream.");
        }

        DbpfHeader header = DbpfHeader.Read(_stream, Diagnostics);
        Header = header;
        Container = header.Container;
        Version = header.Version;

        if (header.IndexCount == 0)
        {
            Diagnostics.Info("Package contains no index entries.");
            return;
        }

        if (header.IndexOffset <= 0 || header.IndexOffset >= _length)
        {
            Diagnostics.Error(
                $"Index offset 0x{header.IndexOffset:X} is outside the file (length 0x{_length:X}); index not read.");
            return;
        }

        _stream.Seek(header.IndexOffset, SeekOrigin.Begin);
        ReadIndex(header.IndexCount, header.IsBig);
    }

    private void ReadIndex(uint indexCount, bool big)
    {
        if (_stream is null)
        {
            return;
        }

        int indexType = _stream.ReadI32();
        if (indexType is < 0 or > 7)
        {
            // Not the fallback: some tools write a plain 0..7 bitmask; anything else is unusable.
            Diagnostics.Error($"Unsupported index type 0x{indexType:X8}; aborting index read.");
            return;
        }

        IndexType = indexType;

        if ((indexType & DbpfConstants.IndexTypeIdShared) != 0)
        {
            ConstantTypeId = _stream.ReadU32();
        }

        if ((indexType & DbpfConstants.IndexGroupIdShared) != 0)
        {
            ConstantGroupId = _stream.ReadU32();
        }

        if ((indexType & DbpfConstants.IndexUnknownShared) != 0)
        {
            ConstantUnknown = _stream.ReadU32();
        }

        bool sharedType = (indexType & DbpfConstants.IndexTypeIdShared) != 0;
        bool sharedGroup = (indexType & DbpfConstants.IndexGroupIdShared) != 0;
        bool sharedUnknown = (indexType & DbpfConstants.IndexUnknownShared) != 0;

        // Each entry is at least: instance(4) + offset(4) + packed(4) + mem(4) + label(2) + flags(2) = 20 bytes.
        int minEntryBytes = big ? 24 : 20;
        long remaining = _length - _stream.Position;
        long maxEntries = remaining / minEntryBytes;
        int limit = (int)Math.Min(indexCount, Math.Max(0, maxEntries));

        if (limit < indexCount)
        {
            Diagnostics.Warn(
                $"Index declares {indexCount} entries but only {limit} fit before end-of-file; reading what is available.");
        }

        int errors = 0;
        for (int i = 0; i < limit; i++)
        {
            DbpfEntry entry;
            try
            {
                entry = new DbpfEntry { Index = i };

                uint typeId = sharedType ? ConstantTypeId : _stream.ReadU32();
                uint groupId = sharedGroup ? ConstantGroupId : _stream.ReadU32();
                uint unknown = sharedUnknown ? ConstantUnknown : _stream.ReadU32();
                uint instanceId = _stream.ReadU32();

                long offset = big ? _stream.ReadI64() : _stream.ReadU32();
                uint packedSize = _stream.ReadU32();
                uint memSize = _stream.ReadU32();
                short label = _stream.ReadI16();
                ushort flags = _stream.ReadU16();

                entry.Key = new ResourceKey(typeId, groupId, instanceId);
                entry.Unknown = unknown;
                entry.ChunkOffset = offset;
                entry.PackedSize = packedSize & 0x7FFFFFFF;
                entry.MemSize = memSize;
                entry.Flags = flags;
                entry.CompressionLabel = label;
                entry.IsCompressed = label == DbpfConstants.CompressionRefPack;

                if (label != DbpfConstants.CompressionNone && label != DbpfConstants.CompressionRefPack)
                {
                    Diagnostics.Warn($"Entry {i} has an unusual compression label 0x{label:X4}; treating it as uncompressed.");
                }

                if (offset < 0 || offset >= _length)
                {
                    Diagnostics.Warn($"Entry {i} offset 0x{offset:X} is outside the file; keeping entry but data will be clamped.");
                }
            }
            catch (EndOfStreamException)
            {
                Diagnostics.Warn($"Index is truncated after {i} of {indexCount} entries.");
                break;
            }

            Entries.Add(entry);
        }

        if (errors > 0)
        {
            Diagnostics.Warn($"Index read completed with {errors} recoverable error(s).");
        }
    }

    /// <summary>Reads and (if necessary) decompresses the payload for one entry.</summary>
    public byte[] ReadEntry(DbpfEntry entry)
    {
        if (_stream is null)
        {
            throw new InvalidOperationException("This package is not backed by a stream.");
        }

        lock (_readLock)
        {
            long length = entry.IsCompressed ? entry.PackedSize : entry.MemSize;
            if (length <= 0)
            {
                length = entry.MemSize > 0 ? entry.MemSize : entry.PackedSize;
            }

            if (length <= 0)
            {
                return Array.Empty<byte>();
            }

            if (entry.ChunkOffset < 0 || entry.ChunkOffset >= _length)
            {
                Diagnostics.Warn($"Entry {entry.Index} points outside the file; returning empty payload.");
                return Array.Empty<byte>();
            }

            long available = _length - entry.ChunkOffset;
            if (length > available)
            {
                Diagnostics.Warn(
                    $"Entry {entry.Index} declares {length} bytes but only {available} remain; clamping.");
                length = available;
            }

            _stream.Seek(entry.ChunkOffset, SeekOrigin.Begin);
            byte[] raw = _stream.ReadBytesExactly((int)Math.Min(length, int.MaxValue));

            if (!entry.IsCompressed)
            {
                return raw;
            }

            try
            {
                return RefPack.Decompress(raw, (int)Math.Min(entry.MemSize, int.MaxValue));
            }
            catch (Exception ex)
            {
                Diagnostics.Error($"Failed to decompress entry {entry.Index}: {ex.Message}");
                return raw;
            }
        }
    }

    /// <summary>Reads every entry; a failure on one entry does not stop the others.</summary>
    public IEnumerable<(DbpfEntry Entry, byte[] Data)> ReadAll()
    {
        foreach (DbpfEntry entry in Entries)
        {
            byte[] data;
            try
            {
                data = ReadEntry(entry);
            }
            catch (Exception ex)
            {
                Diagnostics.Error($"Entry {entry.Index} could not be read: {ex.Message}");
                continue;
            }

            yield return (entry, data);
        }
    }

    /// <summary>
    /// Builds a package from a set of keyed payloads. Returns the number of entries written.
    /// </summary>
    public static int Write(Stream output, IEnumerable<DbpfWriteItem> items, DbpfWriteOptions options)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(items);
        options ??= new DbpfWriteOptions();

        bool big = options.Big;
        int headerSize = big ? DbpfConstants.BigHeaderSize : DbpfConstants.HeaderSize;

        output.Seek(0, SeekOrigin.Begin);
        DbpfHeader.WritePlaceholder(output, options.Version, big);

        var written = new List<DbpfEntry>();
        long position = headerSize;

        foreach (DbpfWriteItem item in items)
        {
            byte[] data = item.Data ?? Array.Empty<byte>();
            byte[] payload = data;
            bool compressed = false;

            if (options.Compress && data.Length > 0)
            {
                byte[] candidate = RefPack.Compress(data);
                if (candidate.Length < data.Length)
                {
                    payload = candidate;
                    compressed = true;
                }
            }

            var entry = new DbpfEntry
            {
                Index = written.Count,
                Key = item.Key,
                ChunkOffset = position,
                PackedSize = (uint)payload.Length,
                MemSize = (uint)data.Length,
                IsCompressed = compressed,
                CompressionLabel = compressed ? DbpfConstants.CompressionRefPack : DbpfConstants.CompressionNone,
                Flags = 1,
            };

            output.Write(payload);
            position += payload.Length;
            written.Add(entry);
        }

        long indexOffset = position;
        WriteIndex(output, written, big, options.Version);
        long indexSize = output.Position - indexOffset;

        DbpfHeader.Patch(output, options.Version, big, (uint)written.Count, indexOffset, indexSize);
        output.Flush();
        return written.Count;
    }

    private static void WriteIndex(Stream output, List<DbpfEntry> entries, bool big, DbpfVersion version)
    {
        // 0x04: the third ("unknown") field is shared and always zero; type and group are per-entry.
        output.WriteI32(0x04);
        output.WriteU32(0);

        foreach (DbpfEntry entry in entries)
        {
            output.WriteU32(entry.Key.TypeId);
            output.WriteU32(entry.Key.GroupId);
            output.WriteU32(entry.Key.InstanceId);

            if (big)
            {
                output.WriteI64(entry.ChunkOffset);
            }
            else
            {
                output.WriteU32((uint)entry.ChunkOffset);
            }

            output.WriteU32(entry.PackedSize | 0x80000000);
            output.WriteU32(entry.MemSize);
            output.WriteI16(entry.IsCompressed ? DbpfConstants.CompressionRefPack : DbpfConstants.CompressionNone);
            output.WriteU16(entry.Flags);
        }
    }

    public void Dispose()
    {
        if (_ownsStream)
        {
            _stream?.Dispose();
        }

        _stream = null;
    }
}
