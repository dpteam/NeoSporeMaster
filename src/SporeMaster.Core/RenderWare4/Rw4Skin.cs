using SporeMaster.Core.IO;

namespace SporeMaster.Core.RenderWare4;

/// <summary>A 3x4 matrix used by RenderWare 4 (three rows of four floats; the last column is the
/// translation, the first three columns are the rotation).</summary>
public sealed class Rw4Matrix3x4
{
    public float[] Values { get; } = new float[12];

    public static Rw4Matrix3x4 Read(Stream s)
    {
        var m = new Rw4Matrix3x4();
        for (int i = 0; i < 12; i++)
        {
            m.Values[i] = s.ReadF32LE();
        }

        return m;
    }
}

/// <summary>The bind pose of one bone inside an <see cref="Rw4AnimationSkin"/>.</summary>
public sealed class Rw4BonePose
{
    /// <summary>The absolute bind-pose rotation (first 3x3 of the matrix).</summary>
    public float[] Rotation { get; } = new float[9];

    public float InvPoseX { get; set; }

    public float InvPoseY { get; set; }

    public float InvPoseZ { get; set; }
}

/// <summary>An animation skin section (type <c>0x70003</c>): per-bone bind poses.</summary>
public sealed class Rw4AnimationSkin
{
    public int Field8 { get; private set; }

    public int FieldC { get; private set; }

    public List<Rw4BonePose> Poses { get; } = new();

    internal static Rw4AnimationSkin Read(Stream s, long offset, long fileLength)
    {
        s.Seek(offset, SeekOrigin.Begin);
        long dataOffset = s.ReadU32();
        int count = s.ReadI32();
        var skin = new Rw4AnimationSkin { Field8 = s.ReadI32(), FieldC = s.ReadI32() };

        if (count < 0 || count > 100_000 || dataOffset < 0 || dataOffset + (long)count * 64 > fileLength)
        {
            return skin;
        }

        s.Seek(dataOffset, SeekOrigin.Begin);
        for (int i = 0; i < count; i++)
        {
            var pose = new Rw4BonePose();
            for (int c = 0; c < 9; c++)
            {
                pose.Rotation[c] = s.ReadF32LE();
            }

            // Skip the remaining row element (offset in the 3x4 matrix).
            s.ReadF32LE();
            pose.InvPoseX = s.ReadF32LE();
            pose.InvPoseY = s.ReadF32LE();
            pose.InvPoseZ = s.ReadF32LE();
            s.ReadU32();
            skin.Poses.Add(pose);
        }

        return skin;
    }
}

/// <summary>A resolved skin: the linkage section plus the buffers it references.</summary>
public sealed class Rw4SkinLinkage
{
    public Rw4SkinsInK Link { get; init; } = new();

    public Rw4SkinMatrixBuffer? MatrixBuffer { get; set; }

    public Rw4Skeleton? Skeleton { get; set; }

    public Rw4AnimationSkin? AnimationSkin { get; set; }
}

public sealed class Rw4SkinMatrixBuffer
{
    public int Field8 { get; private set; }

    public int FieldC { get; private set; }

    public List<Rw4Matrix3x4> Matrices { get; } = new();

    internal static Rw4SkinMatrixBuffer Read(Stream s, long offset, long fileLength)
    {
        s.Seek(offset, SeekOrigin.Begin);
        long dataOffset = s.ReadU32();
        int count = s.ReadI32();
        var buffer = new Rw4SkinMatrixBuffer { Field8 = s.ReadI32(), FieldC = s.ReadI32() };

        if (count < 0 || count > 1_000_000 || dataOffset < 0 || dataOffset + (long)count * 48 > fileLength)
        {
            return buffer;
        }

        s.Seek(dataOffset, SeekOrigin.Begin);
        for (int i = 0; i < count; i++)
        {
            buffer.Matrices.Add(Rw4Matrix3x4.Read(s));
        }

        return buffer;
    }
}

/// <summary>
/// A "skins in K" section (type <c>0x7000C</c>): links a mesh's skin matrix buffer, skeleton and
/// animation skin together. References are indices into the section table.
/// </summary>
public sealed class Rw4SkinsInK
{
    public int Field0Index { get; private set; }

    public int Field4 { get; private set; }

    public int SkinMatrixBufferIndex { get; private set; }

    public int SkeletonIndex { get; private set; }

    public int AnimationSkinIndex { get; private set; }

    internal static Rw4SkinsInK Read(Stream s, long offset)
    {
        s.Seek(offset, SeekOrigin.Begin);
        return new Rw4SkinsInK
        {
            Field0Index = s.ReadI32(),
            Field4 = s.ReadI32(),
            SkinMatrixBufferIndex = s.ReadI32(),
            SkeletonIndex = s.ReadI32(),
            AnimationSkinIndex = s.ReadI32(),
        };
    }
}
