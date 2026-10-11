using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using SporeMaster.Core.Dbpf;
using SporeMaster.Core.IO;
using SporeMaster.Core.Names;

namespace SporeMaster.Core.Properties;

// ---------------------------------------------------------------------------
// Composite properties. Their inner values are stored little-endian (both the
// game and legacy SporeMaster agree on this), unlike the big-endian scalars.
// Standalone composite values are padded to a 16-byte slot.
// ---------------------------------------------------------------------------

/// <summary>Resource reference property (<c>key</c>).</summary>
public sealed class KeyProperty : Property
{
    private readonly List<ResourceKey> _values = new();
    private byte[] _singlePadding = Array.Empty<byte>();

    public override PropertyType Type => PropertyType.Key;

    public override int Count => _values.Count;

    public IReadOnlyList<ResourceKey> Values => _values;

    public override void Clear()
    {
        _values.Clear();
        _singlePadding = Array.Empty<byte>();
    }

    /// <summary>Appends a value.</summary>
    public void Add(ResourceKey value) => _values.Add(value);

    public override void ReadElement(Stream s, bool isArray)
    {
        uint instance = s.ReadU32();
        uint type = s.ReadU32();
        uint group = s.ReadU32();
        if (!isArray)
        {
            _singlePadding = s.ReadBytesExactly(4);
        }

        _values.Add(new ResourceKey(type, group, instance));
    }

    public override void WriteElement(Stream s, int index, bool isArray)
    {
        ResourceKey k = _values[index];
        s.WriteU32(k.InstanceId);
        s.WriteU32(k.TypeId);
        s.WriteU32(k.GroupId);
        if (!isArray)
        {
            if (_singlePadding.Length == 4)
            {
                s.Write(_singlePadding);
            }
            else
            {
                s.WriteZeroes(4);
            }
        }
    }

    public override void WriteXmlValue(XmlWriter w, int index, NameRegistrySet names)
    {
        ResourceKey k = _values[index];
        if (k.GroupId != 0)
        {
            w.WriteAttributeString("groupid", names.GetFileName(k.GroupId));
        }

        w.WriteAttributeString("instanceid", names.GetFileName(k.InstanceId));

        if (k.TypeId != 0)
        {
            w.WriteAttributeString("typeid", names.GetTypeName(k.TypeId));
        }
    }

    public override void ReadXmlElement(XElement element, NameRegistrySet names)
    {
        uint instance = ResolveHash(names, (string?)element.Attribute("instanceid"));
        uint group = ResolveHash(names, (string?)element.Attribute("groupid"));
        uint type = ResolveHash(names, (string?)element.Attribute("typeid"));
        _values.Add(new ResourceKey(type, group, instance));
    }

    private static uint ResolveHash(NameRegistrySet names, string? value) =>
        string.IsNullOrEmpty(value) ? 0u : names.GetFileHash(value);
}

/// <summary>Localized text property (<c>text</c>). Always stored as an array.</summary>
public sealed class TextProperty : Property
{
    private readonly List<LocalizedText> _values = new();

    public override PropertyType Type => PropertyType.Text;

    public override int Count => _values.Count;

    public IReadOnlyList<LocalizedText> Values => _values;

    public override void Clear() => _values.Clear();

    /// <summary>Appends a localized text entry.</summary>
    public void Add(LocalizedText value)
    {
        _values.Add(value);
        IsArray = true;
    }

    /// <summary>Appends a localized text entry.</summary>
    public void Add(uint tableId, uint instanceId, string text) =>
        Add(new LocalizedText { TableId = tableId, InstanceId = instanceId, Text = text });

    public override void ReadElement(Stream s, bool isArray)
    {
        uint tableId = s.ReadU32();
        uint instanceId = s.ReadU32();
        byte[] field = s.ReadBytesExactly(LocalizedText.TextFieldBytes);

        int end = 0;
        while (end + 1 < field.Length && !(field[end] == 0 && field[end + 1] == 0))
        {
            end += 2;
        }

        _values.Add(new LocalizedText
        {
            TableId = tableId,
            InstanceId = instanceId,
            Text = Encoding.Unicode.GetString(field, 0, end),
        });
    }

    public override void WriteElement(Stream s, int index, bool isArray)
    {
        LocalizedText text = _values[index];
        s.WriteU32(text.TableId);
        s.WriteU32(text.InstanceId);

        byte[] data = Encoding.Unicode.GetBytes(text.Text);
        if (data.Length > LocalizedText.TextFieldBytes)
        {
            Array.Resize(ref data, LocalizedText.TextFieldBytes);
        }

        s.Write(data);
        s.WriteZeroes(LocalizedText.TextFieldBytes - data.Length);
    }

    public override void WriteXmlValue(XmlWriter w, int index, NameRegistrySet names)
    {
        LocalizedText text = _values[index];
        if (text.TableId != 0)
        {
            w.WriteAttributeString("tableid", names.GetFileName(text.TableId));
        }

        if (text.InstanceId != 0)
        {
            w.WriteAttributeString("instanceid", names.GetFileName(text.InstanceId));
        }

        w.WriteString(text.Text);
    }

    public override void ReadXmlElement(XElement element, NameRegistrySet names)
    {
        string? tableId = (string?)element.Attribute("tableid");
        string? instanceId = (string?)element.Attribute("instanceid");
        _values.Add(new LocalizedText
        {
            TableId = tableId is null ? 0 : names.GetFileHash(tableId),
            InstanceId = instanceId is null ? 0 : names.GetFileHash(instanceId),
            Text = element.Value,
        });
    }
}

/// <summary>2D vector property (<c>vector2</c>).</summary>
public sealed class Vector2Property : FloatVectorProperty
{
    public Vector2Property()
        : base(PropertyType.Vector2, "x", "y")
    {
    }
}

/// <summary>3D vector property (<c>vector3</c>).</summary>
public sealed class Vector3Property : FloatVectorProperty
{
    public Vector3Property()
        : base(PropertyType.Vector3, "x", "y", "z")
    {
    }
}

/// <summary>4D vector property (<c>vector4</c>).</summary>
public sealed class Vector4Property : FloatVectorProperty
{
    public Vector4Property()
        : base(PropertyType.Vector4, "x", "y", "z", "w")
    {
    }
}

/// <summary>RGB colour property (<c>colorRGB</c>).</summary>
public sealed class ColorRgbProperty : FloatVectorProperty
{
    public ColorRgbProperty()
        : base(PropertyType.ColorRgb, "r", "g", "b")
    {
    }
}

/// <summary>RGBA colour property (<c>colorRGBA</c>).</summary>
public sealed class ColorRgbaProperty : FloatVectorProperty
{
    public ColorRgbaProperty()
        : base(PropertyType.ColorRgba, "r", "g", "b", "a")
    {
    }
}

/// <summary>Shared implementation for fixed-length float vector/colour properties.</summary>
public abstract class FloatVectorProperty : Property
{
    private readonly PropertyType _type;
    private readonly string[] _componentNames;
    private readonly List<float[]> _values = new();
    private byte[] _singlePadding = Array.Empty<byte>();

    protected FloatVectorProperty(PropertyType type, params string[] componentNames)
    {
        _type = type;
        _componentNames = componentNames;
    }

    public int Components => _componentNames.Length;

    public int PaddingSize => (16 - (Components * 4)) % 16;

    /// <summary>Returns a copy of the components of element <paramref name="index"/>.</summary>
    public float[] GetComponents(int index) => (float[])_values[index].Clone();

    public override PropertyType Type => _type;

    public override int Count => _values.Count;

    public override void Clear()
    {
        _values.Clear();
        _singlePadding = Array.Empty<byte>();
    }

    /// <summary>Appends a value (must have <see cref="Components"/> entries).</summary>
    public void Add(float[] value) => _values.Add((float[])value.Clone());

    public override void ReadElement(Stream s, bool isArray)
    {
        var v = new float[Components];
        for (int c = 0; c < Components; c++)
        {
            v[c] = s.ReadF32LE();
        }

        if (!isArray && PaddingSize > 0)
        {
            _singlePadding = s.ReadBytesExactly(PaddingSize);
        }

        _values.Add(v);
    }

    public override void WriteElement(Stream s, int index, bool isArray)
    {
        float[] v = _values[index];
        foreach (float f in v)
        {
            s.WriteF32LE(f);
        }

        if (!isArray && PaddingSize > 0)
        {
            if (_singlePadding.Length == PaddingSize)
            {
                s.Write(_singlePadding);
            }
            else
            {
                s.WriteZeroes(PaddingSize);
            }
        }
    }

    public override void WriteXmlValue(XmlWriter w, int index, NameRegistrySet names)
    {
        float[] v = _values[index];
        for (int c = 0; c < Components; c++)
        {
            w.WriteStartElement(_componentNames[c]);
            w.WriteString(FloatText(v[c]));
            w.WriteEndElement();
        }
    }

    public override void ReadXmlElement(XElement element, NameRegistrySet names)
    {
        var v = new float[Components];
        for (int c = 0; c < Components; c++)
        {
            XElement? child = element.Element(_componentNames[c]);
            v[c] = child is null ? 0 : ParseFloat(child.Value);
        }

        _values.Add(v);
    }
}

/// <summary>Axis-aligned bounding box (<c>bbox</c>).</summary>
public sealed class BoundingBoxProperty : Property
{
    private readonly List<float[]> _values = new();

    public override PropertyType Type => PropertyType.BoundingBox;

    public override int Count => _values.Count;

    /// <summary>Returns min (0..2) and max (3..5) of element <paramref name="index"/>.</summary>
    public float[] GetBox(int index) => (float[])_values[index].Clone();

    public override void Clear() => _values.Clear();

    /// <summary>Appends a value (min xyz, max xyz).</summary>
    public void Add(float[] value) => _values.Add((float[])value.Clone());

    public override void ReadElement(Stream s, bool isArray)
    {
        var v = new float[6];
        for (int c = 0; c < 6; c++)
        {
            v[c] = s.ReadF32LE();
        }

        _values.Add(v);
    }

    public override void WriteElement(Stream s, int index, bool isArray)
    {
        foreach (float f in _values[index])
        {
            s.WriteF32LE(f);
        }
    }

    public override void WriteXmlValue(XmlWriter w, int index, NameRegistrySet names)
    {
        float[] v = _values[index];
        w.WriteStartElement("min");
        w.WriteString($"{FloatText(v[0])}, {FloatText(v[1])}, {FloatText(v[2])}");
        w.WriteEndElement();
        w.WriteStartElement("max");
        w.WriteString($"{FloatText(v[3])}, {FloatText(v[4])}, {FloatText(v[5])}");
        w.WriteEndElement();
    }

    public override void ReadXmlElement(XElement element, NameRegistrySet names)
    {
        var v = new float[6];
        ParseTriple(element.Element("min"), v, 0);
        ParseTriple(element.Element("max"), v, 3);
        _values.Add(v);
    }

    private static void ParseTriple(XElement? element, float[] target, int offset)
    {
        if (element is null)
        {
            return;
        }

        string[] parts = element.Value.Split(',');
        for (int i = 0; i < 3 && i < parts.Length; i++)
        {
            target[offset + i] = ParseFloat(parts[i]);
        }
    }
}

/// <summary>
/// A 3D transformation (<c>transform</c>): flags, a transform count, an offset (3 floats), a scale
/// and a 3x3 rotation (9 floats, row-major). Little-endian, 56 bytes, matching SporeModder FX.
/// </summary>
public sealed class TransformProperty : Property
{
    /// <summary>One transformation value.</summary>
    public readonly struct TransformValue
    {
        public ushort Flags { get; init; }

        public short Count { get; init; }

        /// <summary>Translation (3 floats).</summary>
        public float[] Offset { get; init; }

        public float Scale { get; init; }

        /// <summary>Rotation basis, row-major (9 floats).</summary>
        public float[] Rotation { get; init; }
    }

    private readonly List<TransformValue> _values = new();

    public override PropertyType Type => PropertyType.Transform;

    public override int Count => _values.Count;

    public TransformValue GetTransform(int index) => _values[index];

    public override void Clear() => _values.Clear();

    /// <summary>Appends a value.</summary>
    public void Add(TransformValue value) => _values.Add(value);

    public override void ReadElement(Stream s, bool isArray)
    {
        ushort flags = s.ReadU16();
        short count = s.ReadI16();
        var offset = new float[3];
        for (int i = 0; i < 3; i++)
        {
            offset[i] = s.ReadF32LE();
        }

        float scale = s.ReadF32LE();

        var rotation = new float[9];
        for (int i = 0; i < 9; i++)
        {
            rotation[i] = s.ReadF32LE();
        }

        _values.Add(new TransformValue { Flags = flags, Count = count, Offset = offset, Scale = scale, Rotation = rotation });
    }

    public override void WriteElement(Stream s, int index, bool isArray)
    {
        TransformValue v = _values[index];
        s.WriteU16(v.Flags);
        s.WriteI16(v.Count);
        foreach (float f in v.Offset)
        {
            s.WriteF32LE(f);
        }

        s.WriteF32LE(v.Scale);
        foreach (float f in v.Rotation)
        {
            s.WriteF32LE(f);
        }
    }

    public override void WriteXmlValue(XmlWriter w, int index, NameRegistrySet names)
    {
        TransformValue v = _values[index];
        w.WriteAttributeString("flags", "0x" + v.Flags.ToString("X4", CultureInfo.InvariantCulture));
        w.WriteAttributeString("offset", $"{FloatText(v.Offset[0])},{FloatText(v.Offset[1])},{FloatText(v.Offset[2])}");
        w.WriteAttributeString("scale", FloatText(v.Scale));
        w.WriteAttributeString("row1", $"{FloatText(v.Rotation[0])},{FloatText(v.Rotation[1])},{FloatText(v.Rotation[2])}");
        w.WriteAttributeString("row2", $"{FloatText(v.Rotation[3])},{FloatText(v.Rotation[4])},{FloatText(v.Rotation[5])}");
        w.WriteAttributeString("row3", $"{FloatText(v.Rotation[6])},{FloatText(v.Rotation[7])},{FloatText(v.Rotation[8])}");
    }

    public override void ReadXmlElement(XElement element, NameRegistrySet names)
    {
        ushort flags = 0;
        string? flagsAttr = (string?)element.Attribute("flags");
        if (flagsAttr is not null)
        {
            flags = (ushort)ParseHex(flagsAttr);
        }

        var offset = new float[3];
        if (!ReadTriple(element, "offset", offset, 0, 1, 2))
        {
            ReadTriple(element, "pos", offset, 0, 1, 2);
        }

        float scale = 1f;
        string? scaleAttr = (string?)element.Attribute("scale");
        if (scaleAttr is not null)
        {
            scale = ParseFloat(scaleAttr);
        }

        var rotation = new float[9];
        RotationIdentity(rotation);
        bool hasRows = ReadTriple(element, "row1", rotation, 0, 1, 2);
        hasRows |= ReadTriple(element, "row2", rotation, 3, 4, 5);
        hasRows |= ReadTriple(element, "row3", rotation, 6, 7, 8);

        // Accept SporeModder's human-friendly rotation forms when no raw rows were given.
        if (!hasRows)
        {
            var euler = new double[3];
            bool usesEuler = false;

            string? eulerAttr = (string?)element.Attribute("euler");
            if (eulerAttr is not null)
            {
                Array.Copy(ParseTripleDoubles(eulerAttr), euler, 3);
                usesEuler = true;
            }

            ReadEulerComponent(element, "rot_x", "rotX", euler, 0, ref usesEuler);
            ReadEulerComponent(element, "rot_y", "rotY", euler, 1, ref usesEuler);
            ReadEulerComponent(element, "rot_z", "rotZ", euler, 2, ref usesEuler);

            string? rotateXyz = (string?)element.Attribute("rotateXYZ");
            if (rotateXyz is not null)
            {
                Array.Copy(ParseTripleDoubles(rotateXyz), euler, 3);
                usesEuler = true;
            }

            if (usesEuler)
            {
                RotateMatrix(rotation, euler[0], euler[1], euler[2]);
            }
        }

        _values.Add(new TransformValue { Flags = flags, Count = 1, Offset = offset, Scale = scale, Rotation = rotation });
    }

    private static bool ReadTriple(XElement element, string name, float[] target, int a, int b, int c)
    {
        string? attr = (string?)element.Attribute(name);
        if (attr is null)
        {
            return false;
        }

        string[] parts = attr.Split(',');
        if (parts.Length != 3)
        {
            return false;
        }

        target[a] = ParseFloat(parts[0]);
        target[b] = ParseFloat(parts[1]);
        target[c] = ParseFloat(parts[2]);
        return true;
    }

    private static double[] ParseTripleDoubles(string text)
    {
        string[] parts = text.Split(',');
        var values = new double[3];
        for (int i = 0; i < 3 && i < parts.Length; i++)
        {
            values[i] = double.Parse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        return values;
    }

    private static void ReadEulerComponent(XElement element, string name, string alias, double[] euler, int index, ref bool usesEuler)
    {
        string? attr = (string?)element.Attribute(name) ?? (string?)element.Attribute(alias);
        if (attr is not null)
        {
            euler[index] = double.Parse(attr.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
            usesEuler = true;
        }
    }

    private static void RotationIdentity(float[] r)
    {
        r[0] = 1f;
        r[4] = 1f;
        r[8] = 1f;
    }

    /// <summary>Composes a row-major rotation matrix from Euler angles (degrees), like SporeModder.</summary>
    private static void RotateMatrix(float[] r, double ex, double ey, double ez)
    {
        double x = ex * Math.PI / 180.0;
        double y = ey * Math.PI / 180.0;
        double z = ez * Math.PI / 180.0;
        double ci = Math.Cos(x), cj = Math.Cos(y), ch = Math.Cos(z);
        double si = Math.Sin(x), sj = Math.Sin(y), sh = Math.Sin(z);
        double cc = ci * ch, cs = ci * sh, sc = si * ch, ss = si * sh;

        r[0] = (float)(cj * ch);
        r[1] = (float)(sj * sc - cs);
        r[2] = (float)(sj * cc + ss);
        r[3] = (float)(cj * sh);
        r[4] = (float)(sj * ss + cc);
        r[5] = (float)(sj * cs - sc);
        r[6] = (float)-sj;
        r[7] = (float)(cj * si);
        r[8] = (float)(cj * ci);
    }

    private static uint ParseHex(string text) =>
        uint.Parse(text.Trim().TrimStart('0', 'x', 'X'), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
}

/// <summary>Placeholder for a type code the reader does not recognise (16 opaque bytes).</summary>
public sealed class UnknownProperty : Property
{
    private readonly PropertyType _type;
    private int _count;

    public UnknownProperty(PropertyType type) => _type = type;

    public override PropertyType Type => _type;

    public override int Count => _count;

    public override void Clear() => _count = 0;

    public override void ReadElement(Stream s, bool isArray)
    {
        s.Seek(16, SeekOrigin.Current);
        _count++;
    }

    public override void WriteElement(Stream s, int index, bool isArray) => s.WriteZeroes(16);

    public override void WriteXmlValue(XmlWriter w, int index, NameRegistrySet names) => w.WriteString(string.Empty);

    public override void ReadXmlElement(XElement element, NameRegistrySet names) => _count++;
}
