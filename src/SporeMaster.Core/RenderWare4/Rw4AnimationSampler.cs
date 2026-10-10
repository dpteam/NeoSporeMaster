using System.Numerics;

namespace SporeMaster.Core.RenderWare4;

/// <summary>The interpolated transform of one animated channel at a moment in time.</summary>
public sealed class Rw4SampledChannel
{
    public int ChannelId { get; init; }

    /// <summary>Rotation quaternion (x, y, z, w).</summary>
    public float[] Rotation { get; init; } = new float[4];

    public float[] Location { get; init; } = new float[3];

    public float[] Scale { get; init; } = new float[3];
}

/// <summary>
/// Evaluates RenderWare 4 keyframe animations: samples each channel's translation/rotation/scale at an
/// arbitrary time by interpolating its keyframes (linear for position/scale, spherical for rotation).
/// </summary>
public static class Rw4AnimationSampler
{
    /// <summary>
    /// Samples every channel of <paramref name="anim"/> at <paramref name="time"/> seconds. Channels
    /// without usable keyframes are skipped.
    /// </summary>
    public static List<Rw4SampledChannel> Sample(Rw4KeyframeAnim anim, float time)
    {
        ArgumentNullException.ThrowIfNull(anim);

        var result = new List<Rw4SampledChannel>(anim.Channels.Count);
        foreach (Rw4AnimationChannel channel in anim.Channels)
        {
            if (channel.Keyframes.Count == 0)
            {
                continue;
            }

            Rw4Keyframe sample = Interpolate(channel, time);
            result.Add(new Rw4SampledChannel
            {
                ChannelId = channel.ChannelId,
                Rotation = sample.Rotation,
                Location = sample.Location,
                Scale = sample.Scale,
            });
        }

        return result;
    }

    private static Rw4Keyframe Interpolate(Rw4AnimationChannel channel, float time)
    {
        List<Rw4Keyframe> frames = channel.Keyframes;

        // Find the pair bracketing the time (keyframes are time-ordered).
        int next = 0;
        while (next < frames.Count && frames[next].Time < time)
        {
            next++;
        }

        if (next == 0)
        {
            return frames[0];
        }

        if (next >= frames.Count)
        {
            return frames[^1];
        }

        Rw4Keyframe a = frames[next - 1];
        Rw4Keyframe b = frames[next];
        float span = b.Time - a.Time;
        float t = span > 1e-6f ? (time - a.Time) / span : 0f;

        var result = new Rw4Keyframe { Time = time };

        // Location and scale: linear.
        for (int i = 0; i < 3; i++)
        {
            result.Location[i] = Lerp(a.Location[i], b.Location[i], t);
            result.Scale[i] = Lerp(a.Scale[i], b.Scale[i], t);
        }

        // Rotation: spherical interpolation.
        Quaternion qa = new(a.Rotation[0], a.Rotation[1], a.Rotation[2], a.Rotation[3]);
        Quaternion qb = new(b.Rotation[0], b.Rotation[1], b.Rotation[2], b.Rotation[3]);
        Quaternion q = Quaternion.Slerp(qa, qb, t);
        result.Rotation[0] = q.X;
        result.Rotation[1] = q.Y;
        result.Rotation[2] = q.Z;
        result.Rotation[3] = q.W;

        return result;
    }

    private static float Lerp(float a, float b, float t) => a + ((b - a) * t);
}
