using System.Buffers.Binary;

namespace SporeMaster.Core.Pdb;

/// <summary>Thrown when a file is not a Microsoft MSF (PDB) container.</summary>
public sealed class NotMsfException : Exception
{
    public NotMsfException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// A minimal reader for the Microsoft "MSF" (Multi-Stream File) container used by PDB files.
/// It exposes the page size and the stream directory, and can read individual streams.
/// </summary>
/// <remarks>
/// Only the container is parsed here (superblock + stream directory); PDB-specific meaning is added
/// by <see cref="PdbFile"/>. Endianness is little-endian throughout.
/// </remarks>
public sealed class MsfFile : IDisposable
{
    private static readonly byte[] Magic =
    {
        0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66, 0x74, 0x20, 0x43, 0x2F, 0x43, 0x2B, 0x2B, 0x20,
        0x4D, 0x53, 0x46, 0x20, 0x37, 0x2E, 0x30, 0x30, 0x0D, 0x0A, 0x1A, 0x44, 0x53, 0x00, 0x00, 0x00,
    };

    private readonly Stream _stream;
    private readonly bool _ownsStream;
    private readonly List<uint[]>?[] _streamBlocks;

    private MsfFile(Stream stream, bool ownsStream, uint pageSize, uint blockCount, int streamCount)
    {
        _stream = stream;
        _ownsStream = ownsStream;
        PageSize = pageSize;
        BlockCount = blockCount;
        StreamCount = streamCount;
        _streamBlocks = new List<uint[]>[streamCount];
    }

    public uint PageSize { get; }

    public uint BlockCount { get; }

    public int StreamCount { get; }

    /// <summary>Size in bytes of each stream; 0xFFFFFFFF marks an unused ("nil") stream.</summary>
    public IReadOnlyList<uint> StreamSizes { get; private set; } = Array.Empty<uint>();

    public static MsfFile Read(Stream stream, bool ownsStream = false)
    {
        if (!stream.CanSeek)
        {
            throw new NotMsfException("The MSF container requires a seekable stream.");
        }

        Span<byte> header = stackalloc byte[0x38];
        stream.Seek(0, SeekOrigin.Begin);
        ReadExactly(stream, header);

        if (!header[..Magic.Length].SequenceEqual(Magic))
        {
            throw new NotMsfException("Not an MSF 7.00 file (bad magic).");
        }

        uint pageSize = BinaryPrimitives.ReadUInt32LittleEndian(header[0x20..]);
        uint blockCount = BinaryPrimitives.ReadUInt32LittleEndian(header[0x28..]);
        uint directoryBytes = BinaryPrimitives.ReadUInt32LittleEndian(header[0x2C..]);
        uint blockMapAddr = BinaryPrimitives.ReadUInt32LittleEndian(header[0x34..]);

        if (pageSize == 0 || pageSize > 1 << 20)
        {
            throw new NotMsfException($"Unreasonable MSF page size {pageSize}.");
        }

        int directoryBlockCount = (int)((directoryBytes + pageSize - 1) / pageSize);

        // The block at BlockMapAddr lists the block numbers that make up the stream directory.
        int blockMapBytes = directoryBlockCount * 4;
        byte[] blockMap = ReadRaw(stream, (long)blockMapAddr * pageSize, blockMapBytes);

        var directoryBlockNumbers = new uint[directoryBlockCount];
        for (int i = 0; i < directoryBlockCount; i++)
        {
            directoryBlockNumbers[i] = BinaryPrimitives.ReadUInt32LittleEndian(blockMap.AsSpan(i * 4));
        }

        byte[] directory = ReadBlocks(stream, pageSize, directoryBlockNumbers, (int)directoryBytes);

        // Directory: stream count, then each stream's size, then each stream's block list.
        int offset = 0;
        int streamCount = (int)ReadU32(directory, ref offset);
        if (streamCount < 0 || streamCount > 100_000)
        {
            throw new NotMsfException($"Unreasonable MSF stream count {streamCount}.");
        }

        var sizes = new uint[streamCount];
        for (int i = 0; i < streamCount; i++)
        {
            sizes[i] = ReadU32(directory, ref offset);
        }

        var file = new MsfFile(stream, ownsStream, pageSize, blockCount, streamCount)
        {
            StreamSizes = sizes,
        };

        for (int i = 0; i < streamCount; i++)
        {
            if (sizes[i] == uint.MaxValue || sizes[i] == 0)
            {
                file._streamBlocks[i] = new List<uint[]>();
                continue;
            }

            int blocks = (int)((sizes[i] + pageSize - 1) / pageSize);
            var numbers = new uint[blocks];
            for (int b = 0; b < blocks; b++)
            {
                numbers[b] = ReadU32(directory, ref offset);
            }

            file._streamBlocks[i] = new List<uint[]> { numbers };
        }

        return file;
    }

    /// <summary>Reads the full contents of a stream, or an empty array for an unused stream.</summary>
    public byte[] ReadStream(int index)
    {
        if (index < 0 || index >= StreamCount)
        {
            return Array.Empty<byte>();
        }

        uint size = StreamSizes[index];
        if (size == uint.MaxValue || size == 0)
        {
            return Array.Empty<byte>();
        }

        uint[]? blocks = _streamBlocks[index]?[0];
        if (blocks is null)
        {
            return Array.Empty<byte>();
        }

        return ReadBlocks(_stream, PageSize, blocks, (int)size);
    }

    public void Dispose()
    {
        if (_ownsStream)
        {
            _stream.Dispose();
        }
    }

    // ---- helpers ----

    private static byte[] ReadBlocks(Stream stream, uint pageSize, uint[] blockNumbers, int length)
    {
        var buffer = new byte[(int)((long)blockNumbers.Length * pageSize)];
        int position = 0;
        foreach (uint block in blockNumbers)
        {
            stream.Seek((long)block * pageSize, SeekOrigin.Begin);
            int toRead = (int)Math.Min(pageSize, buffer.Length - position);
            ReadExactly(stream, buffer.AsSpan(position, toRead));
            position += toRead;
        }

        if (length < buffer.Length)
        {
            Array.Resize(ref buffer, length);
        }

        return buffer;
    }

    private static byte[] ReadRaw(Stream stream, long offset, int length)
    {
        stream.Seek(offset, SeekOrigin.Begin);
        var buffer = new byte[length];
        ReadExactly(stream, buffer);
        return buffer;
    }

    private static uint ReadU32(byte[] data, ref int offset)
    {
        uint value = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset));
        offset += 4;
        return value;
    }

    private static void ReadExactly(Stream stream, Span<byte> buffer)
    {
        int read = 0;
        while (read < buffer.Length)
        {
            int n = stream.Read(buffer[read..]);
            if (n <= 0)
            {
                throw new EndOfStreamException();
            }

            read += n;
        }
    }
}
