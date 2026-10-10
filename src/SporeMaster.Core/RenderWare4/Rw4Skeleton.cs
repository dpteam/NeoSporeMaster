using SporeMaster.Core.IO;

namespace SporeMaster.Core.RenderWare4;

/// <summary>A single bone of a RenderWare 4 skeleton.</summary>
public sealed class Rw4Bone
{
    /// <summary>The bone name as an FNV hash.</summary>
    public uint Name { get; set; }

    public int Flags { get; set; }

    /// <summary>Index of the parent bone in the skeleton, or -1 for a root bone.</summary>
    public int ParentIndex { get; set; } = -1;
}

/// <summary>A RenderWare 4 skeleton section (type <c>0x70002</c>): a bone hierarchy.</summary>
public sealed class Rw4Skeleton
{
    public int SkeletonId { get; set; }

    public List<Rw4Bone> Bones { get; } = new();

    /// <summary>Reads a skeleton section whose data starts at <paramref name="offset"/>.</summary>
    internal static Rw4Skeleton Read(Stream stream, long offset, long fileLength)
    {
        stream.Seek(offset, SeekOrigin.Begin);

        long boneFlagsOffset = stream.ReadU32();
        long boneParentsOffset = stream.ReadU32();
        long boneNamesOffset = stream.ReadU32();
        int count = stream.ReadI32();
        int skeletonId = stream.ReadI32();

        if (count < 0 || count > 100_000)
        {
            throw new InvalidDataException($"Unreasonable RW4 bone count {count}.");
        }

        var skeleton = new Rw4Skeleton { SkeletonId = skeletonId };
        for (int i = 0; i < count; i++)
        {
            skeleton.Bones.Add(new Rw4Bone());
        }

        if (InRange(boneNamesOffset, count * 4, fileLength))
        {
            stream.Seek(boneNamesOffset, SeekOrigin.Begin);
            for (int i = 0; i < count; i++)
            {
                skeleton.Bones[i].Name = stream.ReadU32();
            }
        }

        if (InRange(boneFlagsOffset, count * 4, fileLength))
        {
            stream.Seek(boneFlagsOffset, SeekOrigin.Begin);
            for (int i = 0; i < count; i++)
            {
                skeleton.Bones[i].Flags = stream.ReadI32();
            }
        }

        if (InRange(boneParentsOffset, count * 4, fileLength))
        {
            stream.Seek(boneParentsOffset, SeekOrigin.Begin);
            for (int i = 0; i < count; i++)
            {
                int parent = stream.ReadI32();
                skeleton.Bones[i].ParentIndex = parent >= 0 && parent < count ? parent : -1;
            }
        }

        return skeleton;
    }

    private static bool InRange(long offset, long length, long fileLength) =>
        offset >= 0 && length >= 0 && offset + length <= fileLength;
}
