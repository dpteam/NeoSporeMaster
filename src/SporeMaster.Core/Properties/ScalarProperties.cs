using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using SporeMaster.Core.Hashing;
using SporeMaster.Core.IO;
using SporeMaster.Core.Names;

namespace SporeMaster.Core.Properties;

// ---------------------------------------------------------------------------
// Big-endian scalar properties. Their element values are big-endian.
// ---------------------------------------------------------------------------

public sealed class BoolProperty : Property
{
    private readonly List<bool> _values = new();

    public override PropertyType Type => PropertyType.Bool;

    public override int Count => _values.Count;

    public IReadOnlyList<bool> Values => _values;

    public override void Clear() => _values.Clear();

    /// <summary>Appends a value.</summary>
    public void Add(bool value) => _values.Add(value);

    public override void ReadElement(Stream s, bool isArray) => _values.Add(s.ReadByte() != 0);

    public override void WriteElement(Stream s, int index, bool isArray) =>
        s.WriteByte(_values[index] ? (byte)1 : (byte)0);

    public override void WriteXmlValue(XmlWriter w, int index, NameRegistrySet names) =>
        w.WriteString(_values[index] ? "true" : "false");

    public override void ReadXmlElement(XElement element, NameRegistrySet names)
    {
        string text = element.Value.Trim();
        _values.Add(string.Equals(text, "true", StringComparison.OrdinalIgnoreCase) || text == "1");
    }
}

public sealed class CharProperty : Property
{
    private readonly List<byte> _values = new();

    public override PropertyType Type => PropertyType.Char;

    public override int Count => _values.Count;

    public IReadOnlyList<byte> Values => _values;

    public override void Clear() => _values.Clear();

    /// <summary>Appends a value.</summary>
    public void Add(byte value) => _values.Add(value);

    public override void ReadElement(Stream s, bool isArray) => _values.Add((byte)s.ReadByte());

    public override void WriteElement(Stream s, int index, bool isArray) => s.WriteByte(_values[index]);

    public override void WriteXmlValue(XmlWriter w, int index, NameRegistrySet names) =>
        w.WriteString(_values[index].ToString(CultureInfo.InvariantCulture));

    public override void ReadXmlElement(XElement element, NameRegistrySet names) =>
        _values.Add(byte.Parse(element.Value.Trim(), CultureInfo.InvariantCulture));
}

public sealed class WCharProperty : Property
{
    private readonly List<ushort> _values = new();

    public override PropertyType Type => PropertyType.WChar;

    public override int Count => _values.Count;

    public IReadOnlyList<ushort> Values => _values;

    public override void Clear() => _values.Clear();

    /// <summary>Appends a value.</summary>
    public void Add(ushort value) => _values.Add(value);

    public override void ReadElement(Stream s, bool isArray) => _values.Add(s.ReadU16BE());

    public override void WriteElement(Stream s, int index, bool isArray) => s.WriteU16BE(_values[index]);

    public override void WriteXmlValue(XmlWriter w, int index, NameRegistrySet names) =>
        w.WriteString(_values[index].ToString(CultureInfo.InvariantCulture));

    public override void ReadXmlElement(XElement element, NameRegistrySet names) =>
        _values.Add(ushort.Parse(element.Value.Trim(), CultureInfo.InvariantCulture));
}

/// <summary>Big-endian signed/unsigned integer property (int8/16/32/64, uint8/16/32/64).</summary>
public sealed class IntegerProperty : Property
{
    private readonly PropertyTypeInfo _info;
    private readonly List<long> _values = new();

    public IntegerProperty(PropertyType type) => _info = PropertyTypes.Get(type);

    public override PropertyType Type => _info.Type;

    public override int Count => _values.Count;

    public IReadOnlyList<long> Values => _values;

    public override void Clear() => _values.Clear();

    /// <summary>Appends a value.</summary>
    public void Add(long value) => _values.Add(value);

    public override void ReadElement(Stream s, bool isArray) => _values.Add(ReadOne(s));

    public override void WriteElement(Stream s, int index, bool isArray) => WriteOne(s, _values[index]);

    private long ReadOne(Stream s) => _info.Type switch
    {
        PropertyType.Int8 => (sbyte)s.ReadByte(),
        PropertyType.UInt8 => s.ReadByte(),
        PropertyType.Int16 => s.ReadI16BE(),
        PropertyType.UInt16 => s.ReadU16BE(),
        PropertyType.Int32 => s.ReadI32BE(),
        PropertyType.UInt32 => s.ReadU32BE(),
        PropertyType.Int64 or PropertyType.UInt64 => s.ReadI64BE(),
        _ => 0L,
    };

    private void WriteOne(Stream s, long v)
    {
        switch (_info.Type)
        {
            case PropertyType.Int8:
            case PropertyType.UInt8:
                s.WriteByte((byte)v);
                break;
            case PropertyType.Int16:
                s.WriteI16BE((short)v);
                break;
            case PropertyType.UInt16:
                s.WriteU16BE((ushort)v);
                break;
            case PropertyType.Int32:
                s.WriteI32BE((int)v);
                break;
            case PropertyType.UInt32:
                s.WriteU32BE((uint)v);
                break;
            case PropertyType.Int64:
            case PropertyType.UInt64:
                s.WriteI64BE(v);
                break;
        }
    }

    public override void WriteXmlValue(XmlWriter w, int index, NameRegistrySet names) =>
        w.WriteString(FormatValue(_values[index]));

    public override void ReadXmlElement(XElement element, NameRegistrySet names) =>
        _values.Add(ParseValue(element.Value.Trim()));

    private string FormatValue(long v) => _info.Type switch
    {
        PropertyType.UInt8 => ((byte)v).ToString(CultureInfo.InvariantCulture),
        PropertyType.UInt16 => ((ushort)v).ToString(CultureInfo.InvariantCulture),
        PropertyType.UInt32 => ((uint)v).ToString(CultureInfo.InvariantCulture),
        _ => v.ToString(CultureInfo.InvariantCulture),
    };

    private static long ParseValue(string text)
    {
        if (text.StartsWith('#'))
        {
            return unchecked((long)ulong.Parse(text[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
        }

        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return unchecked((long)ulong.Parse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
        }

        if (text.StartsWith('$'))
        {
            return unchecked((int)Fnv.Hash(text[1..]));
        }

        return long.Parse(text, CultureInfo.InvariantCulture);
    }
}

public sealed class FloatProperty : Property
{
    private readonly List<float> _values = new();

    public override PropertyType Type => PropertyType.Float;

    public override int Count => _values.Count;

    public IReadOnlyList<float> Values => _values;

    public override void Clear() => _values.Clear();

    /// <summary>Appends a value.</summary>
    public void Add(float value) => _values.Add(value);

    public override void ReadElement(Stream s, bool isArray) => _values.Add(s.ReadF32BE());

    public override void WriteElement(Stream s, int index, bool isArray) => s.WriteF32BE(_values[index]);

    public override void WriteXmlValue(XmlWriter w, int index, NameRegistrySet names) =>
        w.WriteString(FloatText(_values[index]));

    public override void ReadXmlElement(XElement element, NameRegistrySet names) =>
        _values.Add(ParseFloat(element.Value));
}

public sealed class DoubleProperty : Property
{
    private readonly List<double> _values = new();

    public override PropertyType Type => PropertyType.Double;

    public override int Count => _values.Count;

    public IReadOnlyList<double> Values => _values;

    public override void Clear() => _values.Clear();

    /// <summary>Appends a value.</summary>
    public void Add(double value) => _values.Add(value);

    public override void ReadElement(Stream s, bool isArray) => _values.Add(s.ReadF64BE());

    public override void WriteElement(Stream s, int index, bool isArray) => s.WriteF64BE(_values[index]);

    public override void WriteXmlValue(XmlWriter w, int index, NameRegistrySet names) =>
        w.WriteString(DoubleText(_values[index]));

    public override void ReadXmlElement(XElement element, NameRegistrySet names) =>
        _values.Add(ParseDouble(element.Value));
}

/// <summary>String values: <c>string8</c> stores bytes, <c>string16</c> stores UTF-16.</summary>
public sealed class StringProperty : Property
{
    private readonly PropertyTypeInfo _info;
    private readonly List<byte[]> _raw = new();

    public StringProperty(PropertyType type) => _info = PropertyTypes.Get(type);

    public override PropertyType Type => _info.Type;

    public override int Count => _raw.Count;

    private bool Wide => _info.Type == PropertyType.String16;

    public override void Clear() => _raw.Clear();

    /// <summary>Appends a value.</summary>
    public void Add(string value) => _raw.Add(Encode(value));

    public override void ReadElement(Stream s, bool isArray)
    {
        int length = s.ReadI32BE();
        _raw.Add(s.ReadBytesExactly(Wide ? Math.Max(0, length) * 2 : Math.Max(0, length)));
    }

    public override void WriteElement(Stream s, int index, bool isArray)
    {
        byte[] data = _raw[index];
        s.WriteI32BE(Wide ? data.Length / 2 : data.Length);
        s.Write(data);
    }

    public string GetText(int index = 0) => Decode(_raw[index]);

    public override void WriteXmlValue(XmlWriter w, int index, NameRegistrySet names) => w.WriteString(Decode(_raw[index]));

    public override void ReadXmlElement(XElement element, NameRegistrySet names) => _raw.Add(Encode(element.Value));

    private string Decode(byte[] data) => Wide ? Encoding.Unicode.GetString(data) : StringCodec.DecodeBytes(data);

    private byte[] Encode(string value) => Wide ? Encoding.Unicode.GetBytes(value) : StringCodec.EncodeBytes(value);
}

/// <summary>Encodes/decodes <c>string8</c> byte buffers, preferring UTF-8 and preserving bytes.</summary>
internal static class StringCodec
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static string DecodeBytes(byte[] data)
    {
        try
        {
            return StrictUtf8.GetString(data);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(data);
        }
    }

    public static byte[] EncodeBytes(string value) => Encoding.UTF8.GetBytes(value);
}
