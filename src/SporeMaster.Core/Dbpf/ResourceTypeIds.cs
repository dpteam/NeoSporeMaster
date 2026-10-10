namespace SporeMaster.Core.Dbpf;

/// <summary>Well-known resource type ids used by Spore (values from <c>reg_type.txt</c>).</summary>
public static class ResourceTypeIds
{
    public const uint Prop = 0x00B1B104;
    public const uint SoundProp = 0x02B9F662;
    public const uint AudioProp = 0x02B9F662;
    public const uint SubmixProp = 0x02C9EFF2;
    public const uint ModeProp = 0x0497925E;
    public const uint ChildrenProp = 0x03F51892;
    public const uint Locale = 0x02FAC0B6;
    public const uint Raster = 0x2F4E681C;
    public const uint Rw4 = 0x2F4E681B;

    private static readonly uint[] PropTypes =
    {
        Prop, SoundProp, SubmixProp, ModeProp, ChildrenProp,
    };

    /// <summary>True when the type id denotes a property file that can be converted to XML.</summary>
    public static bool IsProperty(uint typeId) => Array.IndexOf(PropTypes, typeId) >= 0;

    public static IReadOnlyList<uint> PropertyTypes => PropTypes;

    /// <summary>True when the type id denotes a rasterized texture (.raster) convertible to DDS.</summary>
    public static bool IsTexture(uint typeId) => typeId == Raster;

    /// <summary>True when the type id denotes a RenderWare 4 resource.</summary>
    public static bool IsRenderWare4(uint typeId) => typeId == Rw4;
}
