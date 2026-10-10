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

    /// <summary>
    /// Resolves a RenderWare 4 reference. The top two bits encode the kind: <c>0</c> is a section
    /// (object) index, <c>1</c> means "no object" and <c>2</c> is a sub-reference into the manifest's
    /// sub-reference table. Returns null when the reference points at nothing.
    /// </summary>
    public Rw4SectionInfo? ResolveReference(int reference)
    {
        switch ((int)((uint)reference >> 22))
        {
            case 0:
                return GetSection(reference);
            case 2:
                int subIndex = reference & 0x3FFFF;
                IReadOnlyList<(int ObjectIndex, int FieldOffset)> refs = Manifest.SubReferences.References;
                return subIndex >= 0 && subIndex < refs.Count ? GetSection(refs[subIndex].ObjectIndex) : null;
            default:
                return null;
        }
    }

    /// <summary>Reads the mesh sections of this resource (empty for non-mesh containers).</summary>
    public IReadOnlyList<Rw4Mesh> ReadMeshes()
    {
        if (_stream is null)
        {
            return Array.Empty<Rw4Mesh>();
        }

        var meshes = new List<Rw4Mesh>();
        for (int i = 0; i < Sections.Count; i++)
        {
            Rw4SectionInfo section = Sections[i];
            if (section.TypeCode != (int)Rw4SectionType.Mesh || section.Size < 36)
            {
                continue;
            }

            try
            {
                meshes.Add(Rw4Mesh.Read(_stream, section.DataOffset, i));
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

    /// <summary>Reads the raw bytes of a section (its <see cref="Rw4SectionInfo.Size"/> bytes).</summary>
    public byte[] ReadSectionBytes(Rw4SectionInfo section)
    {
        if (_stream is null || section.Size <= 0)
        {
            return Array.Empty<byte>();
        }

        if (section.DataOffset < 0 || section.DataOffset + section.Size > _length)
        {
            Diagnostics.Warn($"RW4 base resource at 0x{section.DataOffset:X} is out of range.");
            return Array.Empty<byte>();
        }

        _stream.Seek(section.DataOffset, SeekOrigin.Begin);
        return _stream.ReadBytesExactly(section.Size);
    }

    /// <summary>Reads the skeleton sections of this resource (empty for non-skeletal containers).</summary>
    public IReadOnlyList<Rw4Skeleton> ReadSkeletons()
    {
        if (_stream is null)
        {
            return Array.Empty<Rw4Skeleton>();
        }

        var skeletons = new List<Rw4Skeleton>();
        foreach (Rw4SectionInfo section in Sections)
        {
            if (section.TypeCode != (int)Rw4SectionType.Skeleton || section.Size < 24)
            {
                continue;
            }

            try
            {
                skeletons.Add(Rw4Skeleton.Read(_stream, section.DataOffset, _length));
            }
            catch (Exception ex)
            {
                Diagnostics.Warn($"RW4 skeleton section at 0x{section.DataOffset:X} could not be read: {ex.Message}");
            }
        }

        return skeletons;
    }

    /// <summary>
    /// Reads the skin linkage sections of this resource: each entry pairs a "skins in K" section
    /// with its skin matrix buffer, skeleton and animation skin (resolved by section index).
    /// </summary>
    public IReadOnlyList<Rw4SkinLinkage> ReadSkins()
    {
        if (_stream is null)
        {
            return Array.Empty<Rw4SkinLinkage>();
        }

        var links = new List<Rw4SkinLinkage>();
        foreach (Rw4SectionInfo section in Sections)
        {
            if (section.TypeCode != (int)Rw4SectionType.SkinInK || section.Size < 20)
            {
                continue;
            }

            try
            {
                Rw4SkinsInK link = Rw4SkinsInK.Read(_stream, section.DataOffset);
                var linkage = new Rw4SkinLinkage { Link = link };

                Rw4SectionInfo? matrixSection = ResolveReference(link.SkinMatrixBufferIndex);
                if (matrixSection is { TypeCode: (int)Rw4SectionType.SkinMatrixBuffer })
                {
                    linkage.MatrixBuffer = Rw4SkinMatrixBuffer.Read(_stream, matrixSection.DataOffset, _length);
                }

                Rw4SectionInfo? skeletonSection = ResolveReference(link.SkeletonIndex);
                if (skeletonSection is { TypeCode: (int)Rw4SectionType.Skeleton })
                {
                    linkage.Skeleton = Rw4Skeleton.Read(_stream, skeletonSection.DataOffset, _length);
                }

                Rw4SectionInfo? animSection = GetSection(link.AnimationSkinIndex);
                if (animSection is { TypeCode: (int)Rw4SectionType.AnimationSkin })
                {
                    linkage.AnimationSkin = Rw4AnimationSkin.Read(_stream, animSection.DataOffset, _length);
                }

                links.Add(linkage);
            }
            catch (Exception ex)
            {
                Diagnostics.Warn($"RW4 skin section at 0x{section.DataOffset:X} could not be read: {ex.Message}");
            }
        }

        return links;
    }

    /// <summary>
    /// Reads every keyframe-animation section (type <c>0x70001</c>) of this resource. Each section
    /// is a set of channels (usually one per bone) with their keyframes.
    /// </summary>
    public IReadOnlyList<Rw4KeyframeAnim> ReadKeyframeAnimations()
    {
        if (_stream is null)
        {
            return Array.Empty<Rw4KeyframeAnim>();
        }

        var animations = new List<Rw4KeyframeAnim>();
        foreach (Rw4SectionInfo section in Sections)
        {
            if (section.TypeCode != (int)Rw4SectionType.KeyframeAnim || section.Size < 48)
            {
                continue;
            }

            try
            {
                animations.Add(Rw4KeyframeAnim.Read(_stream, section.DataOffset, _length));
            }
            catch (Exception ex)
            {
                Diagnostics.Warn($"RW4 keyframe animation at 0x{section.DataOffset:X} could not be read: {ex.Message}");
            }
        }

        return animations;
    }

    /// <summary>
    /// Reads every animations section (type <c>0xFF0001</c>): tables that map an animation id to the
    /// keyframe-animation section holding it. Each object index is resolved to a section when it
    /// points at a <see cref="Rw4KeyframeAnim"/> section.
    /// </summary>
    public IReadOnlyList<Rw4AnimationSet> ReadAnimationSets()
    {
        if (_stream is null)
        {
            return Array.Empty<Rw4AnimationSet>();
        }

        var sets = new List<Rw4AnimationSet>();
        foreach (Rw4SectionInfo section in Sections)
        {
            if (section.TypeCode != (int)Rw4SectionType.Animations || section.Size < 8)
            {
                continue;
            }

            try
            {
                _stream.Seek(section.DataOffset, SeekOrigin.Begin);
                var set = new Rw4AnimationSet { SubReferenceIndex = _stream.ReadI32() };
                int count = _stream.ReadI32();
                if (count < 0 || count > 100_000 || section.DataOffset + 8L + count * 8L > _length)
                {
                    sets.Add(set);
                    continue;
                }

                for (int i = 0; i < count; i++)
                {
                    int animationId = _stream.ReadI32();
                    int objectIndex = _stream.ReadI32();
                    set.Animations.Add((animationId, objectIndex));
                }

                sets.Add(set);
            }
            catch (Exception ex)
            {
                Diagnostics.Warn($"RW4 animations section at 0x{section.DataOffset:X} could not be read: {ex.Message}");
            }
        }

        return sets;
    }

    /// <summary>
    /// Reads every compiled material state (section type <c>0x2000B</c>). Each is decoded into a
    /// <see cref="Rw4Material"/>; undecodable sections keep their raw bytes.
    /// </summary>
    public IReadOnlyList<Rw4Material> ReadMaterials()
    {
        if (_stream is null)
        {
            return Array.Empty<Rw4Material>();
        }

        var materials = new List<Rw4Material>();
        foreach (Rw4SectionInfo section in Sections)
        {
            if (section.TypeCode != (int)Rw4SectionType.CompiledState || section.Size <= 0)
            {
                continue;
            }

            materials.Add(Rw4Material.Read(_stream, section.DataOffset, section.Size, _length));
        }

        return materials;
    }

    /// <summary>
    /// Reads every mesh-compiled-state link (section type <c>0x2001A</c>): which material sections a
    /// mesh uses.
    /// </summary>
    public IReadOnlyList<Rw4MaterialLink> ReadMaterialLinks()
    {
        if (_stream is null)
        {
            return Array.Empty<Rw4MaterialLink>();
        }

        var links = new List<Rw4MaterialLink>();
        foreach (Rw4SectionInfo section in Sections)
        {
            if (section.TypeCode != (int)Rw4SectionType.MeshCompiledStateLink || section.Size < 8)
            {
                continue;
            }

            try
            {
                _stream.Seek(section.DataOffset, SeekOrigin.Begin);
                var link = new Rw4MaterialLink { MeshSectionIndex = _stream.ReadI32() };
                int count = _stream.ReadI32();
                if (count < 0 || count > 100_000 || section.DataOffset + 8L + count * 4L > _length)
                {
                    links.Add(link);
                    continue;
                }

                for (int i = 0; i < count; i++)
                {
                    link.MaterialSectionIndices.Add(_stream.ReadI32());
                }

                links.Add(link);
            }
            catch (Exception ex)
            {
                Diagnostics.Warn($"RW4 material link at 0x{section.DataOffset:X} could not be read: {ex.Message}");
            }
        }

        return links;
    }

    /// <summary>
    /// Reads the triangle geometry for every mesh in this resource. Best-effort: meshes whose
    /// buffers cannot be resolved or decoded are reported through <see cref="Diagnostics"/>.
    /// </summary>
    public IReadOnlyList<Rw4MeshGeometry> ReadGeometry()
    {
        if (_stream is null)
        {
            return Array.Empty<Rw4MeshGeometry>();
        }

        var result = new List<Rw4MeshGeometry>();

        foreach (Rw4Mesh mesh in ReadMeshes())
        {
            try
            {
                Rw4MeshGeometry? geometry = ReadMeshGeometry(mesh);
                if (geometry is not null)
                {
                    result.Add(geometry);
                }
            }
            catch (Exception ex)
            {
                Diagnostics.Warn($"RW4 mesh #{mesh.MeshSectionIndex} geometry could not be decoded: {ex.Message}");
            }
        }

        return result;
    }

    private Rw4MeshGeometry? ReadMeshGeometry(Rw4Mesh mesh)
    {
        if (mesh.VertexBufferIndices.Count == 0)
        {
            return null;
        }

        Rw4SectionInfo? vertexBufferSection = GetSection(mesh.VertexBufferIndices[0]);
        if (vertexBufferSection is null || vertexBufferSection.TypeCode != (int)Rw4SectionType.VertexBuffer)
        {
            return null;
        }

        Rw4VertexBuffer vertexBuffer = ReadAt(vertexBufferSection, Rw4VertexBuffer.Read);
        Rw4SectionInfo? descriptionSection = GetSection(vertexBuffer.VertexDescriptionIndex);
        Rw4SectionInfo? vertexDataSection = GetSection(vertexBuffer.VertexDataIndex);
        if (descriptionSection is null || vertexDataSection is null)
        {
            return null;
        }

        Rw4VertexDescription description = ReadAt(descriptionSection, Rw4VertexDescription.Read);
        int positionOffset = description.PositionOffset;
        if (positionOffset < 0)
        {
            return null;
        }

        byte[] vertexData = ReadSectionBytes(vertexDataSection);
        int stride = vertexBuffer.VertexSize > 0 ? vertexBuffer.VertexSize : description.VertexSize;
        if (stride <= 0)
        {
            return null;
        }

        int vertexCount = vertexBuffer.VertexCount;
        var positions = new float[vertexCount * 3];
        for (int i = 0; i < vertexCount; i++)
        {
            int baseOffset = (i * stride) + positionOffset;
            if (baseOffset + 12 > vertexData.Length)
            {
                break;
            }

            positions[(i * 3) + 0] = BitConverter.ToSingle(vertexData, baseOffset);
            positions[(i * 3) + 1] = BitConverter.ToSingle(vertexData, baseOffset + 4);
            positions[(i * 3) + 2] = BitConverter.ToSingle(vertexData, baseOffset + 8);
        }

        float[] normals = ReadFloatElements(vertexData, description, stride, vertexCount, usageCode: 2, componentCount: 3);
        float[] texCoords = ReadFloatElements(vertexData, description, stride, vertexCount, usageCode: 6, componentCount: 2);

        int[] indices = ReadIndices(mesh, out _);
        if (indices.Length < 3)
        {
            return null;
        }

        return new Rw4MeshGeometry
        {
            MeshSectionIndex = mesh.MeshSectionIndex,
            Positions = positions,
            Normals = normals,
            TexCoords = texCoords,
            Indices = indices,
        };
    }

    /// <summary>
    /// Extracts a float vector element (e.g. normal = usage code 2, texcoord0 = usage code 6) from
    /// every vertex, when the element is stored as plain floats.
    /// </summary>
    private static float[] ReadFloatElements(
        byte[] vertexData,
        Rw4VertexDescription description,
        int stride,
        int vertexCount,
        int usageCode,
        int componentCount)
    {
        Rw4VertexElement? element = description.Elements.FirstOrDefault(e => e.TypeCode == usageCode);
        // Only FLOAT1..FLOAT4 element types hold plain floats; block/encoded formats are skipped.
        if (element is null || element.Type is < 0 or > 3)
        {
            return Array.Empty<float>();
        }

        int available = Math.Min(componentCount, element.Type + 1);
        var values = new float[vertexCount * componentCount];
        for (int i = 0; i < vertexCount; i++)
        {
            int baseOffset = (i * stride) + element.Offset;
            if (baseOffset + (available * 4) > vertexData.Length)
            {
                break;
            }

            for (int c = 0; c < available; c++)
            {
                values[(i * componentCount) + c] = BitConverter.ToSingle(vertexData, baseOffset + (c * 4));
            }
        }

        return values;
    }

    private int[] ReadIndices(Rw4Mesh mesh, out Rw4IndexBuffer? indexBuffer)
    {
        indexBuffer = null;
        Rw4SectionInfo? indexSection = GetSection(mesh.IndexBufferIndex);
        if (indexSection is null || indexSection.TypeCode != (int)Rw4SectionType.IndexBuffer)
        {
            return Array.Empty<int>();
        }

        indexBuffer = ReadAt(indexSection, Rw4IndexBuffer.Read);
        Rw4SectionInfo? dataSection = GetSection(indexBuffer.IndexDataIndex);
        if (dataSection is null)
        {
            return Array.Empty<int>();
        }

        byte[] data = ReadSectionBytes(dataSection);
        bool wide = indexBuffer.Format == 102; // D3DFMT_INDEX32
        int elementSize = wide ? 4 : 2;
        int available = data.Length / elementSize;

        // The mesh declares how many indices belong to it (triangleCount * 3 for triangle lists).
        int wanted = mesh.PrimitiveCount > 0 && mesh.PrimitiveCount <= available ? mesh.PrimitiveCount : available;
        int start = indexBuffer.StartIndex >= 0 && indexBuffer.StartIndex < available ? indexBuffer.StartIndex : 0;
        wanted = Math.Min(wanted, available - start);

        var indices = new int[wanted];
        for (int i = 0; i < wanted; i++)
        {
            int offset = (start + i) * elementSize;
            indices[i] = wide
                ? BitConverter.ToInt32(data, offset)
                : BitConverter.ToUInt16(data, offset);
        }

        return indices;
    }

    private T ReadAt<T>(Rw4SectionInfo section, Func<Stream, T> reader)
    {
        _stream!.Seek(section.DataOffset, SeekOrigin.Begin);
        return reader(_stream);
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
