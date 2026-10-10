using System.Numerics;

namespace SporeMaster.Core.RenderWare4;

/// <summary>
/// Poses a RenderWare 4 skeleton at a moment in time by evaluating a keyframe animation and composing
/// each bone's animated transform through the hierarchy. Channels are matched to bones by name hash.
/// </summary>
/// <remarks>
/// This produces a bone stick-figure (joint positions) for previewing motion. It is approximate: it
/// does not apply skinning to the mesh (that needs the vertex blend data), so it is meant as a visual
/// aid rather than a physically exact pose.
/// </remarks>
public static class Rw4SkeletonPoser
{
    /// <summary>
    /// Returns the world position of every joint of <paramref name="skeleton"/> at
    /// <paramref name="time"/> seconds. <paramref name="bindOrigins"/> are the rest-pose bone origins
    /// (usually from the skin matrix buffer). Returns an empty list when there is nothing to pose.
    /// </summary>
    public static List<float[]> PosePositions(
        Rw4Skeleton skeleton,
        Rw4KeyframeAnim animation,
        IReadOnlyList<float[]> bindOrigins,
        float time)
    {
        ArgumentNullException.ThrowIfNull(skeleton);
        ArgumentNullException.ThrowIfNull(animation);

        int count = skeleton.Bones.Count;
        var positions = new List<float[]>(count);
        if (count == 0)
        {
            return positions;
        }

        // Map channel id -> sampled transform.
        var samples = new Dictionary<int, Rw4SampledChannel>();
        foreach (Rw4SampledChannel sample in Rw4AnimationSampler.Sample(animation, time))
        {
            samples[sample.ChannelId] = sample;
        }

        var worldPos = new Vector3[count];
        var worldRot = new Quaternion[count];

        for (int i = 0; i < count; i++)
        {
            Rw4Bone bone = skeleton.Bones[i];
            Vector3 rest = bindOrigins is not null && i < bindOrigins.Count && bindOrigins[i].Length >= 3
                ? new Vector3(bindOrigins[i][0], bindOrigins[i][1], bindOrigins[i][2])
                : Vector3.Zero;

            Vector3 localPos = Vector3.Zero;
            Quaternion localRot = Quaternion.Identity;
            if (samples.TryGetValue(unchecked((int)bone.Name), out Rw4SampledChannel? s))
            {
                localPos = new Vector3(s.Location[0], s.Location[1], s.Location[2]);
                localRot = new Quaternion(s.Rotation[0], s.Rotation[1], s.Rotation[2], s.Rotation[3]);
            }

            int parent = bone.ParentIndex;
            if (parent < 0 || parent >= i)
            {
                // Root (or a forward reference): start from the rest origin plus the animated offset.
                worldPos[i] = rest + localPos;
                worldRot[i] = localRot;
            }
            else
            {
                worldPos[i] = worldPos[parent] + Vector3.Transform(localPos, worldRot[parent]);
                worldRot[i] = Quaternion.Normalize(worldRot[parent] * localRot);
            }
        }

        foreach (Vector3 p in worldPos)
        {
            positions.Add(new[] { p.X, p.Y, p.Z });
        }

        return positions;
    }
}
