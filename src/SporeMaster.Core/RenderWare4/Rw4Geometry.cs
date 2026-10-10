using SporeMaster.Core.IO;

namespace SporeMaster.Core.RenderWare4;

/// <summary>A single vertex element from a RenderWare 4 vertex description.</summary>
/// <remarks>Mirrors SporeModder FX's <c>RWVertexElement</c>.</remarks>
public sealed class Rw4VertexElement
{
    public int Stream { get; private init; }

    /// <summary>Byte offset of this element inside the vertex data.</summary>
    public int Offset { get; private init; }

    public int Type { get; private init; }

    public int Method { get; private init; }

    public int Usage { get; private init; }

    public byte UsageIndex { get; private init; }

    /// <summary>The Spore-specific element code (0 = position, 2 = normal, 6 = texcoord0, ...).</summary>
    public int TypeCode { get; private init; }

    /// <summary>The size in bytes of this element's data, derived from <see cref="Type"/>.</summary>
    public int Size => Type switch
    {
        0 => 4,   // FLOAT1
        1 => 8,   // FLOAT2
        2 => 12,  // FLOAT3
        3 => 16,  // FLOAT4
        4 => 4,   // D3DCOLOR
        5 => 4,   // UBYTE4
        6 => 4,   // SHORT2
        7 => 8,   // SHORT4
        8 => 4,   // UBYTE4N
        9 => 4,   // SHORT2N
        10 => 8,  // SHORT4N
        11 => 4,  // USHORT2N
        12 => 8,  // USHORT4N
        13 => 4,  // UDEC3
        14 => 4,  // DEC3N
        15 => 4,  // FLOAT16_2
        16 => 8,  // FLOAT16_4
        _ => 0,
    };

    internal static Rw4VertexElement Read(Stream stream) => new()
    {
        Stream = stream.ReadU16(),
        Offset = stream.ReadU16(),
        Type = stream.ReadU8(),
        Method = stream.ReadU8(),
        Usage = stream.ReadU8(),
        UsageIndex = stream.ReadU8(),
        TypeCode = stream.ReadI32(),
    };
}

/// <summary>A vertex description section (type <c>0x20004</c>).</summary>
public sealed class Rw4VertexDescription
{
    public int Field0 { get; private set; }

    public int Field4 { get; private set; }

    public int VertexSize { get; private set; }

    public List<Rw4VertexElement> Elements { get; } = new();

    internal static Rw4VertexDescription Read(Stream stream)
    {
        var description = new Rw4VertexDescription
        {
            Field0 = stream.ReadI32(),
            Field4 = stream.ReadI32(),
        };

        stream.ReadI32(); // dxVertexDeclaration
        int count = stream.ReadU16();
        stream.ReadU8(); // field_0E
        description.VertexSize = stream.ReadU8();
        stream.ReadI32(); // elementFlags
        stream.ReadI32(); // field_14

        if (count is > 0 and < 256)
        {
            for (int i = 0; i < count; i++)
            {
                description.Elements.Add(Rw4VertexElement.Read(stream));
            }
        }

        return description;
    }

    /// <summary>Returns the byte offset of the primary position element, or -1 when absent.</summary>
    public int PositionOffset
    {
        get
        {
            foreach (Rw4VertexElement element in Elements)
            {
                if (element.TypeCode == 0)
                {
                    return element.Offset;
                }
            }

            return -1;
        }
    }
}

/// <summary>A vertex buffer section (type <c>0x20005</c>).</summary>
public sealed class Rw4VertexBuffer
{
    public int VertexDescriptionIndex { get; private set; }

    public int BaseVertexIndex { get; private set; }

    public int VertexCount { get; private set; }

    public int VertexSize { get; private set; }

    public int VertexDataIndex { get; private set; }

    internal static Rw4VertexBuffer Read(Stream stream)
    {
        int description = stream.ReadI32();
        stream.ReadI32(); // field_4
        int baseVertexIndex = stream.ReadI32();
        int vertexCount = stream.ReadI32();
        stream.ReadI32(); // field_10
        int vertexSize = stream.ReadI32();
        int vertexData = stream.ReadI32();

        return new Rw4VertexBuffer
        {
            VertexDescriptionIndex = description,
            BaseVertexIndex = baseVertexIndex,
            VertexCount = vertexCount,
            VertexSize = vertexSize,
            VertexDataIndex = vertexData,
        };
    }
}

/// <summary>An index buffer section (type <c>0x20007</c>).</summary>
public sealed class Rw4IndexBuffer
{
    public int StartIndex { get; private set; }

    public int PrimitiveCount { get; private set; }

    public int Format { get; private set; }

    public int PrimitiveType { get; private set; }

    public int IndexDataIndex { get; private set; }

    internal static Rw4IndexBuffer Read(Stream stream)
    {
        stream.ReadI32(); // dxIndexBuffer
        int startIndex = stream.ReadI32();
        int primitiveCount = stream.ReadI32();
        stream.ReadI32(); // usage
        int format = stream.ReadI32();
        int primitiveType = stream.ReadI32();
        int indexData = stream.ReadI32();

        return new Rw4IndexBuffer
        {
            StartIndex = startIndex,
            PrimitiveCount = primitiveCount,
            Format = format,
            PrimitiveType = primitiveType,
            IndexDataIndex = indexData,
        };
    }
}

/// <summary>Extracted mesh geometry: positions and triangle indices.</summary>
public sealed class Rw4MeshGeometry
{
    /// <summary>Position triplets (x, y, z), length = 3 * vertex count.</summary>
    public float[] Positions { get; init; } = Array.Empty<float>();

    /// <summary>Triangle indices, length = 3 * triangle count.</summary>
    public int[] Indices { get; init; } = Array.Empty<int>();

    /// <summary>The section index of the mesh this geometry came from.</summary>
    public int MeshSectionIndex { get; init; }

    public int VertexCount => Positions.Length / 3;

    public int TriangleCount => Indices.Length / 3;
}
