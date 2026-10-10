namespace SporeMaster.Core.Properties;

/// <summary>Creates <see cref="Property"/> instances from a property type code.</summary>
public static class PropertyFactory
{
    public static Property Create(PropertyType type) => type switch
    {
        PropertyType.Bool => new BoolProperty(),
        PropertyType.Char => new CharProperty(),
        PropertyType.WChar => new WCharProperty(),
        PropertyType.Int8 or PropertyType.UInt8 or
        PropertyType.Int16 or PropertyType.UInt16 or
        PropertyType.Int32 or PropertyType.UInt32 or
        PropertyType.Int64 or PropertyType.UInt64 => new IntegerProperty(type),
        PropertyType.Float => new FloatProperty(),
        PropertyType.Double => new DoubleProperty(),
        PropertyType.String8 or PropertyType.String16 => new StringProperty(type),
        PropertyType.Key => new KeyProperty(),
        PropertyType.Text => new TextProperty(),
        PropertyType.Vector2 => new Vector2Property(),
        PropertyType.Vector3 => new Vector3Property(),
        PropertyType.Vector4 => new Vector4Property(),
        PropertyType.ColorRgb => new ColorRgbProperty(),
        PropertyType.ColorRgba => new ColorRgbaProperty(),
        PropertyType.BoundingBox => new BoundingBoxProperty(),
        PropertyType.Transform => new TransformProperty(),
        _ => new UnknownProperty(type),
    };
}
