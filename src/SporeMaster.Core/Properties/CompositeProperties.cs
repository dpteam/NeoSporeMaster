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

    public override PropertyType Type => _type;

    public override int Count => _values.Count;

    public override void Clear()
    {
        _values.Clear();
        _singlePadding = Array.Empty<byte>();
    }

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

    public override void Clear() => _values.Clear();

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
/// Transformation matrix (<c>transform</c>). Stored as a big-endian flags word followed by a
/// little-endian 4x3 matrix plus a reserved word (56 bytes total, matching SporeModder FX).
/// </summary>
public sealed class TransformProperty : Property
{
    private readonly List<(uint Flags, float[] Matrix, uint Reserved)> _values = new();

    public override PropertyType Type => PropertyType.Transform;

    public override int Count => _values.Count;

    public override void Clear() => _values.Clear();

    public override void ReadElement(Stream s, bool isArray)
    {
        uint flags = s.ReadU32BE();
        var matrix = new float[12];
        for (int c = 0; c < 12; c++)
        {
            matrix[c] = s.ReadF32LE();
        }

        _values.Add((flags, matrix, s.ReadU32()));
    }

    public override void WriteElement(Stream s, int index, bool isArray)
    {
        (uint flags, float[] matrix, uint reserved) = _values[index];
        s.WriteU32BE(flags);
        foreach (float f in matrix)
        {
            s.WriteF32LE(f);
        }

        s.WriteU32(reserved);
    }

    public override void WriteXmlValue(XmlWriter w, int index, NameRegistrySet names)
    {
        (uint flags, float[] m, _) = _values[index];
        w.WriteAttributeString("flags", "0x" + flags.ToString("X8", CultureInfo.InvariantCulture));
        w.WriteAttributeString("pos", $"{FloatText(m[0])},{FloatText(m[1])},{FloatText(m[2])}");
        w.WriteAttributeString("vx", $"{FloatText(m[3])},{FloatText(m[6])},{FloatText(m[9])}");
        w.WriteAttributeString("vy", $"{FloatText(m[4])},{FloatText(m[7])},{FloatText(m[10])}");
        w.WriteAttributeString("vz", $"{FloatText(m[5])},{FloatText(m[8])},{FloatText(m[11])}");
    }

    public override void ReadXmlElement(XElement element, NameRegistrySet names)
    {
        var m = new float[12];
        uint flags = 0;

        string? flagsAttr = (string?)element.Attribute("flags");
        if (flagsAttr is not null)
        {
            flags = ParseHex(flagsAttr);
            ReadTriple(element, "pos", m, 0, 1, 2);
            ReadTriple(element, "vx", m, 3, 6, 9);
            ReadTriple(element, "vy", m, 4, 7, 10);
            ReadTriple(element, "vz", m, 5, 8, 11);
        }

        _values.Add((flags, m, 0));
    }

    private static void ReadTriple(XElement element, string name, float[] m, int a, int b, int c)
    {
        string? attr = (string?)element.Attribute(name);
        if (attr is null)
        {
            return;
        }

        string[] parts = attr.Split(',');
        if (parts.Length == 3)
        {
            m[a] = ParseFloat(parts[0]);
            m[b] = ParseFloat(parts[1]);
            m[c] = ParseFloat(parts[2]);
        }
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
