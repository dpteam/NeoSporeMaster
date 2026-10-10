using SporeMaster.Core.IO;

namespace SporeMaster.Core.RenderWare4;

/// <summary>Direct3D primitive types used by RW4 meshes.</summary>
public enum Rw4PrimitiveType
{
    PointList = 1,
    LineList = 2,
    LineStrip = 3,
    TriangleList = 4,
    TriangleStrip = 5,
    TriangleFan = 6,
}

/// <summary>Index buffer formats used by RW4 meshes.</summary>
public enum Rw4IndexFormat
{
    Index16 = 101,
    Index32 = 102,
}

/// <summary>A mesh section of a RenderWare 4 resource.</summary>
/// <remarks>Layout mirrors SporeModder FX's <c>RWMesh</c>.</remarks>
public sealed class Rw4Mesh
{
    public int Field0 { get; private init; }

    public Rw4PrimitiveType PrimitiveType { get; private init; }

    public int IndexBufferIndex { get; private init; }

    public int TriangleCount { get; private init; }

    public int FirstIndex { get; private init; }

    public int PrimitiveCount { get; private init; }

    public int FirstVertex { get; private init; }

    public int VertexCount { get; private init; }

    public List<int> VertexBufferIndices { get; } = new();

    internal static Rw4Mesh Read(Stream stream, long offset, RenderWare4File file)
    {
        stream.Seek(offset, SeekOrigin.Begin);

        int field0 = stream.ReadI32();
        var primitive = (Rw4PrimitiveType)stream.ReadI32();
        int indexBufferIndex = stream.ReadI32();
        int triangleCount = stream.ReadI32();
        int buffersCount = stream.ReadI32();
        int firstIndex = stream.ReadI32();
        int primitiveCount = stream.ReadI32();
        int firstVertex = stream.ReadI32();
        int vertexCount = stream.ReadI32();

        var mesh = new Rw4Mesh
        {
            Field0 = field0,
            PrimitiveType = Enum.IsDefined(primitive) ? primitive : Rw4PrimitiveType.TriangleList,
            IndexBufferIndex = indexBufferIndex,
            TriangleCount = triangleCount,
            FirstIndex = firstIndex,
            PrimitiveCount = primitiveCount,
            FirstVertex = firstVertex,
            VertexCount = vertexCount,
        };

        if (buffersCount is > 0 and < 4096)
        {
            for (int i = 0; i < buffersCount; i++)
            {
                mesh.VertexBufferIndices.Add(stream.ReadI32());
            }
        }

        return mesh;
    }
}
