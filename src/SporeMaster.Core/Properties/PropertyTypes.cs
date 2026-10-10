namespace SporeMaster.Core.Properties;

/// <summary>
/// The type codes used by Spore <c>.prop</c> files.
/// Values match both SporeModder FX (<c>Property*.TYPE_CODE</c>) and the legacy SporeMaster
/// (<c>PropertyDefinitionAttribute.FileType</c>).
/// </summary>
public enum PropertyType : ushort
{
    Unknown = 0x00,
    Bool = 0x01,
    Char = 0x02,
    WChar = 0x03,
    Int8 = 0x05,
    UInt8 = 0x06,
    Int16 = 0x07,
    UInt16 = 0x08,
    Int32 = 0x09,
    UInt32 = 0x0A,
    Int64 = 0x0B,
    UInt64 = 0x0C,
    Float = 0x0D,
    Double = 0x0E,
    String8 = 0x12,
    String16 = 0x13,
    Key = 0x20,
    Text = 0x22,
    Vector2 = 0x30,
    Vector3 = 0x31,
    ColorRgb = 0x32,
    Vector4 = 0x33,
    ColorRgba = 0x34,
    Transform = 0x38,
    BoundingBox = 0x39,
}

/// <summary>Static metadata for a <see cref="PropertyType"/>.</summary>
public sealed record PropertyTypeInfo(PropertyType Type, string Name, string PluralName, int ElementSize)
{
    /// <summary>True when array items are fixed-size and can be resynchronised by stride.</summary>
    public bool HasFixedStride => ElementSize > 0;
}

public static class PropertyTypes
{
    private static readonly Dictionary<PropertyType, PropertyTypeInfo> Table = new()
    {
        [PropertyType.Unknown] = new(PropertyType.Unknown, "unknown", "unknowns", 16),
        [PropertyType.Bool] = new(PropertyType.Bool, "bool", "bools", 1),
        [PropertyType.Char] = new(PropertyType.Char, "char", "chars", 1),
        [PropertyType.WChar] = new(PropertyType.WChar, "wchar", "wchars", 2),
        [PropertyType.Int8] = new(PropertyType.Int8, "int8", "int8s", 1),
        [PropertyType.UInt8] = new(PropertyType.UInt8, "uint8", "uint8s", 1),
        [PropertyType.Int16] = new(PropertyType.Int16, "int16", "int16s", 2),
        [PropertyType.UInt16] = new(PropertyType.UInt16, "uint16", "uint16s", 2),
        [PropertyType.Int32] = new(PropertyType.Int32, "int32", "int32s", 4),
        [PropertyType.UInt32] = new(PropertyType.UInt32, "uint32", "uint32s", 4),
        [PropertyType.Int64] = new(PropertyType.Int64, "int64", "int64s", 8),
        [PropertyType.UInt64] = new(PropertyType.UInt64, "uint64", "uint64s", 8),
        [PropertyType.Float] = new(PropertyType.Float, "float", "floats", 4),
        [PropertyType.Double] = new(PropertyType.Double, "double", "doubles", 8),
        [PropertyType.String8] = new(PropertyType.String8, "string8", "string8s", -1),
        [PropertyType.String16] = new(PropertyType.String16, "string16", "string16s", -1),
        [PropertyType.Key] = new(PropertyType.Key, "key", "keys", 12),
        [PropertyType.Text] = new(PropertyType.Text, "text", "texts", 520),
        [PropertyType.Vector2] = new(PropertyType.Vector2, "vector2", "vector2s", 8),
        [PropertyType.Vector3] = new(PropertyType.Vector3, "vector3", "vector3s", 12),
        [PropertyType.ColorRgb] = new(PropertyType.ColorRgb, "colorRGB", "colorRGBs", 12),
        [PropertyType.Vector4] = new(PropertyType.Vector4, "vector4", "vector4s", 16),
        [PropertyType.ColorRgba] = new(PropertyType.ColorRgba, "colorRGBA", "colorRGBAs", 16),
        [PropertyType.Transform] = new(PropertyType.Transform, "transform", "transforms", 56),
        [PropertyType.BoundingBox] = new(PropertyType.BoundingBox, "bbox", "bboxes", 24),
    };

    public static PropertyTypeInfo Get(PropertyType type) =>
        Table.TryGetValue(type, out PropertyTypeInfo? info)
            ? info
            : new PropertyTypeInfo(type, $"0x{(ushort)type:X4}", $"0x{(ushort)type:X4}s", -1);

    public static bool TryGetByCode(ushort code, out PropertyType type)
    {
        type = (PropertyType)code;
        return Table.ContainsKey(type);
    }

    public static PropertyTypeInfo? FindByName(string name)
    {
        foreach (PropertyTypeInfo info in Table.Values)
        {
            if (string.Equals(info.Name, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(info.PluralName, name, StringComparison.OrdinalIgnoreCase))
            {
                return info;
            }
        }

        return null;
    }
}
