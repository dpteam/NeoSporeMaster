using SporeMaster.Core.Diagnostics;
using SporeMaster.Core.IO;

namespace SporeMaster.Core.RenderWare4;

/// <summary>Thrown when a stream does not look like a RenderWare 4 resource.</summary>
public sealed class NotRenderWare4Exception : Exception
{
    public NotRenderWare4Exception(string message)
        : base(message)
    {
    }
}

/// <summary>
/// A RenderWare 4 resource (type <c>0x2F4E681B</c>): the container Spore uses for meshes,
/// textures, skeletons, compiled material states and animations.
/// </summary>
/// <remarks>
/// This parses the container: the header, the section manifest and the section table. It exposes
/// the section list (type, offset, size, alignment) which is enough to enumerate and inspect the
/// structure of every RW4 resource. Decoding the individual sections (meshes, vertex/index
/// buffers, materials, skeletons, animations) is future work — see <c>docs/RW4_FORMAT.md</c>.
/// </remarks>
public sealed class RenderWare4File : IDisposable
{
    /// <summary>The RenderWare 4 file magic: <c>89 52 57 34 77 33 32</c> ("\x89RW4w32").</summary>
    private static readonly byte[] Magic = { 0x89, 0x52, 0x57, 0x34, 0x77, 0x33, 0x32 };

    /// <summary>Offset of the section manifest within the file.</summary>
    private const long ManifestOffset = 0x98;

    private Stream? _stream;
    private bool _ownsStream;
    private long _length;

    private RenderWare4File()
    {
    }

    public Rw4ContainerType ContainerType { get; private set; }

    /// <summary>Number of sections declared by the file.</summary>
    public int SectionCount { get; private set; }

    public long SectionInfoOffset { get; private set; }

    /// <summary>Offset of the base-resource buffer area.</summary>
    public long BufferDataOffset { get; private set; }

    public int UnknownBits { get; private set; }

    /// <summary>The container type value exactly as stored (may be an unknown kind).</summary>
    public uint RawType { get; private set; }

    public Rw4SectionManifest Manifest { get; private set; } = new();

    public List<Rw4SectionInfo> Sections { get; } = new();

    public DiagnosticSink Diagnostics { get; } = new();

    public string? SourceName { get; private set; }

    /// <summary>The resource length in bytes (0 when the backing stream is not seekable).</summary>
    public long Length => _length;

    public static RenderWare4File Open(string path)
    {
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        RenderWare4File file = Read(stream, ownsStream: true);
        file.SourceName = path;
        return file;
    }

    public static RenderWare4File Read(Stream stream, bool ownsStream = false)
    {
        var file = new RenderWare4File
        {
            _stream = stream,
            _ownsStream = ownsStream,
            _length = stream.CanSeek ? stream.Length : 0,
        };
        file.ReadInternal(stream);
        return file;
    }

    private void ReadInternal(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        Span<byte> magic = stackalloc byte[Magic.Length];
        ReadExactly(stream, magic);
        if (!magic.SequenceEqual(Magic))
        {
            throw new NotRenderWare4Exception("Not a RenderWare 4 resource (bad magic).");
        }

        // The header magic occupies 28 bytes; the real fields start at 0x1C.
        stream.Seek(0x1C, SeekOrigin.Begin);
        RawType = stream.ReadU32();
        ContainerType = Enum.IsDefined((Rw4ContainerType)RawType) ? (Rw4ContainerType)RawType : 0;

        stream.Seek(0x24, SeekOrigin.Begin);
        SectionCount = stream.ReadI32();

        stream.Seek(0x30, SeekOrigin.Begin);
        SectionInfoOffset = stream.ReadU32();

        stream.Seek(0x44, SeekOrigin.Begin);
        BufferDataOffset = stream.ReadU32();

        stream.Seek(0x64, SeekOrigin.Begin);
        UnknownBits = stream.ReadI32();

        // The manifest sits at a fixed offset and describes the section type table.
        stream.Seek(ManifestOffset, SeekOrigin.Begin);
        Manifest = Rw4SectionManifest.Read(stream, ManifestOffset, Diagnostics);

        ReadSectionTable(stream);
        Manifest.SubReferences.ReadReferences(stream, Diagnostics);
    }

    private void ReadSectionTable(Stream stream)
    {
        if (SectionCount < 0 || SectionCount > 1_000_000)
        {
            Diagnostics.Error($"RW4 declares an unreasonable section count ({SectionCount}); not reading the table.");
            return;
        }

        if (SectionInfoOffset <= 0 || SectionInfoOffset >= stream.Length)
        {
            Diagnostics.Error($"RW4 section table offset 0x{SectionInfoOffset:X} is outside the file.");
            return;
        }

        stream.Seek(SectionInfoOffset, SeekOrigin.Begin);
        for (int i = 0; i < SectionCount; i++)
        {
            if (stream.Position + 24 > stream.Length)
            {
                Diagnostics.Warn($"RW4 section table is truncated after {i} of {SectionCount} entries.");
                break;
            }

            var info = new Rw4SectionInfo
            {
                DataOffset = stream.ReadU32(),
                Field04 = stream.ReadI32(),
                Size = stream.ReadI32(),
                Alignment = stream.ReadI32(),
                TypeCodeIndex = stream.ReadI32(),
                TypeCode = stream.ReadI32(),
            };

            // Base-resource section offsets are relative to the buffer area.
            if (info.TypeCode == (int)Rw4SectionType.BaseResource)
            {
                info.DataOffset += BufferDataOffset;
            }

            Sections.Add(info);
        }
    }

    private static void ReadExactly(Stream stream, Span<byte> buffer)
    {
        int read = 0;
        while (read < buffer.Length)
        {
            int n = stream.Read(buffer[read..]);
            if (n <= 0)
            {
                throw new NotRenderWare4Exception("Not a RenderWare 4 resource (truncated magic).");
            }

            read += n;
        }
    }

    // ---- section decoding ----

    /// <summary>Returns the section at <paramref name="index"/>, or null when out of range.</summary>
    public Rw4SectionInfo? GetSection(int index) =>
        index >= 0 && index < Sections.Count ? Sections[index] : null;

    /// <summary>Reads the mesh sections of this resource (empty for non-mesh containers).</summary>
    public IReadOnlyList<Rw4Mesh> ReadMeshes()
    {
        if (_stream is null)
        {
            return Array.Empty<Rw4Mesh>();
        }

        var meshes = new List<Rw4Mesh>();
        foreach (Rw4SectionInfo section in Sections)
        {
            if (section.TypeCode != (int)Rw4SectionType.Mesh || section.Size < 36)
            {
                continue;
            }

            try
            {
                meshes.Add(Rw4Mesh.Read(_stream, section.DataOffset, this));
            }
            catch (EndOfStreamException ex)
            {
                Diagnostics.Warn($"RW4 mesh section at 0x{section.DataOffset:X} is truncated: {ex.Message}");
            }
        }

        return meshes;
    }

    /// <summary>Reads a little-endian int at an absolute offset (used by mesh decoding).</summary>
    internal int ReadIntAt(long offset)
    {
        if (_stream is null)
        {
            throw new InvalidOperationException("No stream.");
        }

        _stream.Seek(offset, SeekOrigin.Begin);
        return _stream.ReadI32();
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
