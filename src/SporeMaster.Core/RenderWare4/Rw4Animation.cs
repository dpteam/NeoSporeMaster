using SporeMaster.Core.IO;

namespace SporeMaster.Core.RenderWare4;

/// <summary>The layout of a keyframe, derived from its component mask.</summary>
public enum Rw4KeyframeComponents : int
{
    /// <summary>Just a blend factor (8 bytes).</summary>
    BlendFactor = 0x100,

    /// <summary>Rotation and location (32 bytes).</summary>
    LocRot = 0x101,

    /// <summary>Rotation, location and scale (48 bytes).</summary>
    LocRotScale = 0x601,
}

/// <summary>A single animation keyframe. Which fields are meaningful depends on <see cref="Components"/>.</summary>
public sealed class Rw4Keyframe
{
    public float Time { get; set; }

    public Rw4KeyframeComponents Components { get; set; }

    /// <summary>Rotation quaternion (x, y, z, w).</summary>
    public float[] Rotation { get; } = new float[4];

    /// <summary>Translation.</summary>
    public float[] Location { get; } = new float[3];

    /// <summary>Scale.</summary>
    public float[] Scale { get; } = new float[3];

    /// <summary>Blend factor (only for <see cref="Rw4KeyframeComponents.BlendFactor"/>).</summary>
    public float Factor { get; set; }

    /// <summary>The on-disk size in bytes for a component layout.</summary>
    public static int SizeOf(Rw4KeyframeComponents components) => components switch
    {
        Rw4KeyframeComponents.BlendFactor => 8,
        Rw4KeyframeComponents.LocRot => 32,
        Rw4KeyframeComponents.LocRotScale => 48,
        _ => 0,
    };

    internal static Rw4Keyframe Read(Stream s, Rw4KeyframeComponents components)
    {
        var k = new Rw4Keyframe { Components = components };
        switch (components)
        {
            case Rw4KeyframeComponents.LocRotScale:
                for (int i = 0; i < 4; i++)
                {
                    k.Rotation[i] = s.ReadF32LE();
                }

                for (int i = 0; i < 3; i++)
                {
                    k.Location[i] = s.ReadF32LE();
                }

                for (int i = 0; i < 3; i++)
                {
                    k.Scale[i] = s.ReadF32LE();
                }

                s.ReadU32();
                k.Time = s.ReadF32LE();
                break;

            case Rw4KeyframeComponents.LocRot:
                for (int i = 0; i < 4; i++)
                {
                    k.Rotation[i] = s.ReadF32LE();
                }

                for (int i = 0; i < 3; i++)
                {
                    k.Location[i] = s.ReadF32LE();
                }

                k.Time = s.ReadF32LE();
                break;

            case Rw4KeyframeComponents.BlendFactor:
                k.Factor = s.ReadF32LE();
                k.Time = s.ReadF32LE();
                break;
        }

        return k;
    }
}

/// <summary>One animated channel: a bone (by name hash) with a list of keyframes.</summary>
public sealed class Rw4AnimationChannel
{
    /// <summary>The channel name as an FNV hash (usually a bone name).</summary>
    public int ChannelId { get; set; }

    public Rw4KeyframeComponents Components { get; set; }

    public int PoseSize { get; set; }

    public List<Rw4Keyframe> Keyframes { get; } = new();
}

/// <summary>
/// A keyframe animation section (type <c>0x70001</c>): a set of channels, each holding keyframes for
/// one bone of a skeleton.
/// </summary>
public sealed class Rw4KeyframeAnim
{
    public int SkeletonId { get; set; }

    /// <summary>The animation length in seconds.</summary>
    public float Length { get; set; }

    public int Flags { get; set; }

    /// <summary>Padding bytes that trailed the last channel (kept for fidelity).</summary>
    public int Padding { get; set; } = -1;

    public List<Rw4AnimationChannel> Channels { get; } = new();

    internal static Rw4KeyframeAnim Read(Stream s, long offset, long fileLength)
    {
        long end = fileLength;
        s.Seek(offset, SeekOrigin.Begin);

        long pChannelNames = s.ReadU32();
        int channelCount = s.ReadI32();
        int skeletonId = s.ReadI32();
        s.ReadI32(); // field_C
        long pChannelData = s.ReadU32();
        long pPaddingEnd = s.ReadU32();
        s.ReadU32(); // channel count again
        s.ReadI32(); // field_1C
        float length = s.ReadF32LE();
        s.ReadI32(); // field_24 (usually 12)
        int flags = s.ReadI32();
        long pChannelInfo = s.ReadU32();

        var anim = new Rw4KeyframeAnim { SkeletonId = skeletonId, Length = length, Flags = flags };

        if (channelCount <= 0 || channelCount > 100_000)
        {
            return anim;
        }

        long dataEnd = pPaddingEnd > offset && pPaddingEnd <= fileLength ? pPaddingEnd : end;

        // Channel names.
        var channelNames = new int[channelCount];
        if (InRange(pChannelNames, channelCount * 4L, fileLength))
        {
            s.Seek(pChannelNames, SeekOrigin.Begin);
            for (int i = 0; i < channelCount; i++)
            {
                channelNames[i] = s.ReadI32();
            }
        }

        // Channel info: offset (relative to the section), pose size and component mask.
        var positions = new long[channelCount];
        var poseSizes = new int[channelCount];
        var components = new Rw4KeyframeComponents[channelCount];
        if (InRange(pChannelInfo, channelCount * 12L, fileLength))
        {
            s.Seek(pChannelInfo, SeekOrigin.Begin);
            for (int i = 0; i < channelCount; i++)
            {
                positions[i] = s.ReadU32();
                poseSizes[i] = s.ReadI32();
                components[i] = (Rw4KeyframeComponents)s.ReadI32();
            }
        }

        for (int i = 0; i < channelCount; i++)
        {
            anim.Channels.Add(new Rw4AnimationChannel
            {
                ChannelId = channelNames[i],
                Components = components[i],
                PoseSize = poseSizes[i],
            });
        }

        // Keyframe data: positions are relative to the section start.
        for (int i = 0; i < channelCount; i++)
        {
            Rw4AnimationChannel channel = anim.Channels[i];
            int poseSize = poseSizes[i] > 0 ? poseSizes[i] : Rw4Keyframe.SizeOf(components[i]);
            if (poseSize <= 0)
            {
                continue;
            }

            long start = offset + positions[i];
            if (start < offset || start >= dataEnd)
            {
                continue;
            }

            long limit;
            if (i < channelCount - 1)
            {
                long next = offset + positions[i + 1];
                limit = next > start && next <= dataEnd ? next : dataEnd;
            }
            else
            {
                limit = dataEnd;
            }

            s.Seek(start, SeekOrigin.Begin);
            float lastTime = float.NegativeInfinity;
            while (s.Position + poseSize <= limit)
            {
                Rw4Keyframe keyframe = Rw4Keyframe.Read(s, components[i]);

                // The last channel may run into padding; stop when the time sequence breaks.
                if (i == channelCount - 1 && keyframe.Time < lastTime)
                {
                    anim.Padding = (int)(dataEnd - (s.Position - poseSize));
                    break;
                }

                lastTime = keyframe.Time;
                channel.Keyframes.Add(keyframe);
            }
        }

        return anim;
    }

    private static bool InRange(long offset, long length, long fileLength) =>
        offset >= 0 && length >= 0 && offset + length <= fileLength;
}

/// <summary>An animations section (type <c>0xFF0001</c>): maps animation ids to their keyframe anims.</summary>
public sealed class Rw4AnimationSet
{
    /// <summary>Index of the sub-reference to this object.</summary>
    public int SubReferenceIndex { get; set; }

    /// <summary>Animation id to object index pairs (object index resolves to a section).</summary>
    public List<(int AnimationId, int ObjectIndex)> Animations { get; } = new();
}
