using SporeMaster.Core.IO;

namespace SporeMaster.Core.RenderWare4;

/// <summary>
/// A morph handle section (type <c>0xFF0000</c>): a named morph target with a start and end position
/// and a linked keyframe animation.
/// </summary>
public sealed class Rw4MorphHandle
{
    public int HandleId { get; set; }

    public int Field4 { get; set; }

    public double[] StartPosition { get; } = new double[3];

    public double[] EndPosition { get; } = new double[3];

    /// <summary>Progress of the morph when no handles were modified.</summary>
    public float DefaultTime { get; set; }

    /// <summary>Reference to the keyframe animation driving this morph.</summary>
    public int AnimationReference { get; set; } = -1;

    internal static Rw4MorphHandle Read(Stream s, long offset)
    {
        s.Seek(offset, SeekOrigin.Begin);
        var handle = new Rw4MorphHandle
        {
            HandleId = s.ReadI32(),
            Field4 = s.ReadI32(),
        };

        for (int i = 0; i < 3; i++)
        {
            handle.StartPosition[i] = s.ReadF64LE();
        }

        for (int i = 0; i < 3; i++)
        {
            handle.EndPosition[i] = s.ReadF64LE();
        }

        handle.DefaultTime = s.ReadF32LE();
        handle.AnimationReference = s.ReadI32();
        return handle;
    }
}

/// <summary>
/// A blend-shape section (type <c>0xFF0002</c>): a set of shape ids. The per-vertex deltas live in a
/// separate <see cref="Rw4BlendShapeBuffer"/>.
/// </summary>
public sealed class Rw4BlendShape
{
    public int Id { get; set; }

    public List<int> ShapeIds { get; } = new();

    internal static Rw4BlendShape Read(Stream s, long offset, long fileLength)
    {
        s.Seek(offset, SeekOrigin.Begin);
        s.ReadI32(); // replaced by code
        s.ReadI32(); // function pointer, replaced
        s.ReadI32(); // sub-reference index
        int shapeCount = s.ReadI32();
        s.ReadI32(); // sub-reference index
        int shapeCountAgain = s.ReadI32();

        var shape = new Rw4BlendShape { Id = s.ReadI32() };

        if (shapeCount < 0 || shapeCount > 100_000 || shapeCount != shapeCountAgain)
        {
            throw new InvalidDataException("Malformed RW4 blend shape.");
        }

        s.Seek(shapeCount * 4L, SeekOrigin.Current); // shape times buffer
        for (int i = 0; i < shapeCount; i++)
        {
            shape.ShapeIds.Add(s.ReadI32());
        }

        return shape;
    }
}

/// <summary>
/// A blend-shape buffer section (type <c>0x200AF</c>): the per-vertex morph deltas split into up to 11
/// streams (position, normal, tangent, texcoord, …, blend indices, blend weights).
/// </summary>
public sealed class Rw4BlendShapeBuffer
{
    private const int StreamCount = 11;

    public int ShapeCount { get; private set; }

    public int VertexCount { get; private set; }

    public int Unknown { get; private set; }

    public int BoneIndicesCount { get; private set; }

    /// <summary>Raw bytes for each of the 11 streams (null when a stream is absent).</summary>
    public byte[]?[] Streams { get; } = new byte[]?[StreamCount];

    internal static Rw4BlendShapeBuffer Read(Stream s, long offset, int size, long fileLength)
    {
        long baseOffset = offset;
        s.Seek(offset, SeekOrigin.Begin);

        int type = s.ReadI32();
        if (type != 1)
        {
            throw new InvalidDataException($"Unsigned RW4 blend-shape buffer type 0x{type:X}.");
        }

        var offsets = new long[StreamCount];
        for (int i = 0; i < StreamCount; i++)
        {
            offsets[i] = s.ReadU32();
        }

        var buffer = new Rw4BlendShapeBuffer
        {
            ShapeCount = s.ReadI32(),
            VertexCount = s.ReadI32(),
            Unknown = s.ReadI32(),
            BoneIndicesCount = s.ReadI32(),
        };

        long end = baseOffset + size;
        for (int i = 0; i < StreamCount; i++)
        {
            if (offsets[i] == 0)
            {
                continue;
            }

            long start = baseOffset + offsets[i];

            // The stream ends where the next present stream starts (or at the section end).
            long next = end;
            for (int j = i + 1; j < StreamCount; j++)
            {
                if (offsets[j] != 0)
                {
                    next = baseOffset + offsets[j];
                    break;
                }
            }

            if (start < baseOffset || next > end || next < start)
            {
                continue;
            }

            s.Seek(start, SeekOrigin.Begin);
            buffer.Streams[i] = s.ReadBytesExactly((int)(next - start));
        }

        return buffer;
    }
}
