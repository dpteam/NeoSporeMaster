namespace SporeMaster.Core.RenderWare4;

/// <summary>Section type codes used inside a RenderWare 4 resource.</summary>
/// <remarks>
/// Values mirror SporeModder FX's <c>RW*.TYPE_CODE</c> constants.
/// </remarks>
public enum Rw4SectionType : int
{
    Unknown = 0,
    BaseResource = 0x10030,
    SectionManifest = 0x10004,
    SectionTypes = 0x10005,
    SectionExternalArenas = 0x10006,
    SectionSubReferences = 0x10007,
    SectionAtoms = 0x10008,
    Raster = 0x20003,
    VertexDescription = 0x20004,
    VertexBuffer = 0x20005,
    IndexBuffer = 0x20007,
    TextureOverride = 0x20008,
    Mesh = 0x20009,
    MeshCompiledStateLink = 0x2001A,
    BlendShapeBuffer = 0x200AF,
    CompiledState = 0x2000B,
    KeyframeAnim = 0x70001,
    Skeleton = 0x70002,
    AnimationSkin = 0x70003,
    SkinMatrixBuffer = 0x7000F,
    SkinInK = 0x7000C,
    SkeletonsInK = 0x7000B,
    BBox = 0x80005,
    TriangleKDTreeProcedural = 0x80003,
    MorphHandle = 0xFF0000,
    Animations = 0xFF0001,
    BlendShape = 0xFF0002,
}

/// <summary>The three top-level RenderWare container kinds.</summary>
public enum Rw4ContainerType : uint
{
    Model = 0x00000001,
    Texture = 0x04000000,
    Special = 0xCAFED00D,
}

public static class Rw4SectionTypes
{
    public static bool TryGet(int code, out Rw4SectionType type)
    {
        type = (Rw4SectionType)code;
        return Enum.IsDefined(type);
    }

    /// <summary>Human readable name for a section type code.</summary>
    public static string Describe(int code)
    {
        if (Enum.IsDefined((Rw4SectionType)code))
        {
            return ((Rw4SectionType)code).ToString();
        }

        return "0x" + code.ToString("X8");
    }

    public static string DescribeContainer(uint code) =>
        Enum.IsDefined((Rw4ContainerType)code) ? ((Rw4ContainerType)code).ToString() : "0x" + code.ToString("X8");
}
