using System.Xml;
using System.Xml.Linq;
using SporeMaster.Core.Names;

namespace SporeMaster.Core.Properties;

/// <summary>One localized text entry stored inside a <c>text</c> property.</summary>
public sealed class LocalizedText
{
    public uint TableId { get; set; }

    public uint InstanceId { get; set; }

    public string Text { get; set; } = string.Empty;

    /// <summary>Number of bytes reserved for the UTF-16 buffer in a <c>text</c> element.</summary>
    public const int TextFieldBytes = 512;
}

/// <summary>
/// Base class for a single property. A property holds one or more elements; when
/// <see cref="IsArray"/> is set it represents an array (the "...s" element in the text formats).
/// </summary>
public abstract class Property
{
    /// <summary>True when this property is an array of elements.</summary>
    public bool IsArray { get; set; }

    /// <summary>
    /// The original descriptor flags read from a binary file, if any. Preserved so that
    /// round-tripping keeps the exact (game-specific) bits instead of a normalised value.
    /// </summary>
    internal ushort? SourceFlags { get; set; }

    /// <summary>The original array stride read from a binary file, if any.</summary>
    internal int? SourceItemSize { get; set; }

    public abstract PropertyType Type { get; }

    public PropertyTypeInfo Info => PropertyTypes.Get(Type);

    /// <summary>The number of stored elements.</summary>
    public abstract int Count { get; }

    /// <summary>Removes all elements.</summary>
    public abstract void Clear();

    /// <summary>Reads one element from the stream and appends it.</summary>
    public abstract void ReadElement(Stream stream, bool isArray);

    /// <summary>Writes the element at <paramref name="index"/> to the stream.</summary>
    public abstract void WriteElement(Stream stream, int index, bool isArray);

    /// <summary>Writes the XML body for the element at <paramref name="index"/>.</summary>
    public abstract void WriteXmlValue(XmlWriter writer, int index, NameRegistrySet names);

    /// <summary>Reads one XML element's body and appends the parsed value.</summary>
    public abstract void ReadXmlElement(XElement element, NameRegistrySet names);

    /// <summary>
    /// Writes extra attributes onto the property's XML element (used to preserve the trailing padding
    /// bytes of standalone composite values so the XML round-trip is byte-accurate). No-op by default.
    /// </summary>
    internal virtual void WriteXmlAttributes(XmlWriter writer)
    {
    }

    /// <summary>Reads the extra attributes written by <see cref="WriteXmlAttributes"/>. No-op by default.</summary>
    internal virtual void ReadXmlAttributes(XElement element)
    {
    }

    /// <summary>Parses a hex byte string (as written by <see cref="WriteXmlAttributes"/>), or null.</summary>
    internal static byte[]? ParseHexBytes(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        string hex = text.Trim();
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            hex = hex[2..];
        }

        if (hex.Length == 0 || (hex.Length & 1) != 0)
        {
            // Odd length: pad a leading zero (cannot happen for our writer, but be lenient).
            hex = "0" + hex;
        }

        try
        {
            return Convert.FromHexString(hex);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    /// <summary>Reads <paramref name="count"/> elements, appending them.</summary>
    public void Read(Stream stream, int count, bool isArray)
    {
        for (int i = 0; i < count; i++)
        {
            ReadElement(stream, isArray);
        }
    }

    /// <summary>Writes every stored element.</summary>
    public void Write(Stream stream, bool isArray)
    {
        for (int i = 0; i < Count; i++)
        {
            WriteElement(stream, i, isArray);
        }
    }

    /// <summary>Returns the number of bytes produced by writing the element at <paramref name="index"/>.</summary>
    public int MeasureElement(int index, bool isArray)
    {
        var buffer = new MemoryStream();
        WriteElement(buffer, index, isArray);
        return (int)buffer.Length;
    }

    // ---- shared helpers ----

    protected static string FloatText(float value) =>
        value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    protected static float ParseFloat(string text) =>
        float.Parse(text.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);

    protected static string DoubleText(double value) => value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    protected static double ParseDouble(string text) =>
        double.Parse(text.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
}
