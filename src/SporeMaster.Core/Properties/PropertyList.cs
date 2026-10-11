using System.Xml;
using System.Xml.Linq;
using SporeMaster.Core.Diagnostics;
using SporeMaster.Core.IO;
using SporeMaster.Core.Names;

namespace SporeMaster.Core.Properties;

/// <summary>
/// A Spore property file (<c>.prop</c> and its relatives <c>.soundProp</c>, <c>.submix</c>,
/// <c>.mode</c>, <c>.children</c>). It is a map from property id (hash) to a <see cref="Property"/>.
/// </summary>
/// <remarks>
/// The binary layout is big-endian for the descriptor and scalar values but little-endian for
/// composite values; see <c>docs/PROP_FORMAT.md</c>. The list is always serialised ordered by
/// ascending unsigned id, which the game requires.
/// </remarks>
public sealed class PropertyList
{
    private readonly SortedDictionary<uint, Property> _properties = new();

    public IReadOnlyDictionary<uint, Property> Properties => _properties;

    public int Count => _properties.Count;

    public void Add(uint id, Property property) => _properties[id] = property;

    public Property? Get(uint id) => _properties.TryGetValue(id, out Property? p) ? p : null;

    public bool Remove(uint id) => _properties.Remove(id);

    /// <summary>
    /// Clears references to auto-generated locales (undoing <c>AutoLocale.Generate</c>), so the
    /// texts can be regenerated when the property is packed again. Mirrors SporeMaster 2's
    /// <c>stripGeneratedLocaleReferences</c>.
    /// </summary>
    public int StripAutoLocaleReferences(NameRegistrySet names) =>
        StripAutoLocaleReferences(id => Locale.AutoLocale.IsAutoLocale(names, id));

    /// <summary>Clears references whose table id equals a known auto-locale id.</summary>
    public int StripAutoLocaleReferences(uint autoTableId) =>
        StripAutoLocaleReferences(id => id == autoTableId);

    /// <summary>Clears references whose table id satisfies <paramref name="isAutoLocale"/>.</summary>
    public int StripAutoLocaleReferences(Func<uint, bool> isAutoLocale)
    {
        int stripped = 0;
        foreach (Property property in _properties.Values)
        {
            if (property is not TextProperty text)
            {
                continue;
            }

            foreach (LocalizedText value in text.Values)
            {
                if (value.TableId != 0 && isAutoLocale(value.TableId))
                {
                    value.TableId = 0;
                    value.InstanceId = 0;
                    stripped++;
                }
            }
        }

        return stripped;
    }

    // ---- binary ----

    public static PropertyList Read(Stream stream, DiagnosticSink? diagnostics = null)
    {
        var list = new PropertyList();
        list.ReadInternal(stream, diagnostics);
        return list;
    }

    private void ReadInternal(Stream stream, DiagnosticSink? diagnostics)
    {
        int count = stream.ReadI32BE();
        if (count < 0)
        {
            throw new InvalidDataException($"Property file declares a negative entry count ({count}).");
        }

        // Sanity bound: each entry needs at least 8 descriptor bytes.
        long available = stream.CanSeek ? stream.Length - stream.Position : long.MaxValue;
        if (available != long.MaxValue && count > available / 8 + 1)
        {
            diagnostics?.Warn($"Property count {count} exceeds what fits in the remaining {available} bytes; clamping.");
            count = (int)Math.Max(0, available / 8);
        }

        for (int i = 0; i < count; i++)
        {
            uint id = stream.ReadU32BE();
            ushort typeCode = stream.ReadU16BE();
            ushort flags = stream.ReadU16BE();

            if (!PropertyTypes.TryGetByCode(typeCode, out PropertyType type))
            {
                diagnostics?.Warn($"Entry {i} (id 0x{id:X8}) has unknown property type 0x{typeCode:X4}; treating as opaque.");
                type = (PropertyType)typeCode;
            }

            Property property = PropertyFactory.Create(type);
            property.Clear();
            property.SourceFlags = flags;

            if ((flags & 0x30) == 0)
            {
                property.IsArray = false;
                property.Read(stream, 1, false);
            }
            else if ((flags & 0x40) == 0)
            {
                property.IsArray = true;
                int itemCount = stream.ReadI32BE();
                int itemSize = stream.ReadI32BE();
                property.SourceItemSize = itemSize;
                ReadArrayItems(stream, property, itemCount, itemSize, diagnostics, id);
            }
            else
            {
                // "Empty" variant; the game never emits it. Keep it as an empty array.
                property.IsArray = true;
                diagnostics?.Warn($"Entry {i} (id 0x{id:X8}) uses the reserved empty flag 0x40.");
            }

            _properties[id] = property;
        }
    }

    private static void ReadArrayItems(
        Stream stream,
        Property property,
        int itemCount,
        int itemSize,
        DiagnosticSink? diagnostics,
        uint id)
    {
        if (itemCount < 0)
        {
            diagnostics?.Warn($"Entry 0x{id:X8} declares a negative array length ({itemCount}).");
            return;
        }

        bool fixedStride = property.Info.HasFixedStride && itemSize > 0;

        for (int j = 0; j < itemCount; j++)
        {
            long start = stream.Position;
            try
            {
                property.ReadElement(stream, true);
            }
            catch (EndOfStreamException)
            {
                diagnostics?.Warn($"Entry 0x{id:X8}: array truncated after {j} of {itemCount} items.");
                return;
            }

            if (fixedStride)
            {
                // Resynchronise to the declared stride so a mis-sized element cannot desync the stream.
                long target = start + itemSize;
                if (stream.Position != target)
                {
                    if (!stream.CanSeek || target > stream.Length || target < stream.Position)
                    {
                        continue;
                    }

                    stream.Seek(target, SeekOrigin.Begin);
                }
            }
        }
    }

    public void Write(Stream stream)
    {
        stream.WriteI32BE(_properties.Count);

        foreach ((uint id, Property property) in _properties)
        {
            stream.WriteU32BE(id);
            stream.WriteU16BE((ushort)property.Type);

            bool array = property.IsArray;
            ushort flags = property.SourceFlags ?? (array ? (ushort)0x30 : (ushort)0);
            stream.WriteU16BE(flags);

            if (!array)
            {
                property.WriteElement(stream, 0, false);
                continue;
            }

            int itemSize = property.SourceItemSize ?? ComputeItemSize(property);
            stream.WriteI32BE(property.Count);
            stream.WriteI32BE(itemSize);
            property.Write(stream, true);
        }
    }

    private static int ComputeItemSize(Property property)
    {
        if (property.Info.HasFixedStride)
        {
            return property.Info.ElementSize;
        }

        int max = 0;
        for (int i = 0; i < property.Count; i++)
        {
            max = Math.Max(max, property.MeasureElement(i, true));
        }

        return max;
    }

    // ---- XML ----

    /// <summary>Reads the SporeMaster-compatible <c>.prop.xml</c> representation.</summary>
    public static PropertyList ReadXml(Stream stream, NameRegistrySet names)
    {
        XDocument document = XDocument.Load(stream);
        XElement root = document.Root ?? throw new InvalidDataException("Missing root element.");
        var list = new PropertyList();

        foreach (XElement element in root.Elements())
        {
            PropertyTypeInfo? info = PropertyTypes.FindByName(element.Name.LocalName);
            if (info is null)
            {
                continue;
            }

            bool isArray = string.Equals(info.PluralName, element.Name.LocalName, StringComparison.OrdinalIgnoreCase);
            Property property = PropertyFactory.Create(info.Type);
            property.Clear();
            property.IsArray = isArray;

            // The id can come from a "name" attribute (hashed) or a legacy "id" attribute.
            string? name = (string?)element.Attribute("name");
            string? legacyId = (string?)element.Attribute("id");
            uint id = legacyId is not null ? ParseId(legacyId) : names.GetPropHash(name ?? string.Empty);

            // A preserved descriptor flags word (written when it differs from the default).
            string? flagsAttr = (string?)element.Attribute("flags");
            if (flagsAttr is not null)
            {
                property.SourceFlags = (ushort)ParseId(flagsAttr);
            }

            property.ReadXmlAttributes(element);

            string? itemSizeAttr = (string?)element.Attribute("itemsize");
            if (itemSizeAttr is not null &&
                int.TryParse(itemSizeAttr, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int itemSize))
            {
                property.SourceItemSize = itemSize;
            }

            if (isArray)
            {
                foreach (XElement child in element.Elements())
                {
                    property.ReadXmlElement(child, names);
                }
            }
            else
            {
                property.ReadXmlElement(element, names);
            }

            list.Add(id, property);
        }

        return list;
    }

    /// <summary>Writes the SporeMaster-compatible <c>.prop.xml</c> representation.</summary>
    public void WriteXml(Stream stream, NameRegistrySet names)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new System.Text.UTF8Encoding(false),
            CloseOutput = false,
        };

        using XmlWriter writer = XmlWriter.Create(stream, settings);
        writer.WriteStartDocument();
        writer.WriteStartElement("properties");

        foreach ((uint id, Property property) in OrderForOutput(names))
        {
            PropertyTypeInfo info = property.Info;
            string elementName = property.IsArray ? info.PluralName : info.Name;

            writer.WriteStartElement(elementName);
            string name = names.GetPropName(id);
            writer.WriteAttributeString("name", name);

            // For aliased/hashed ids the name alone would hash to a different value, so also
            // record the literal id to keep the XML round-trippable.
            if (name.StartsWith('#') || Hashing.Fnv.Hash(name) != id)
            {
                writer.WriteAttributeString("id", "0x" + id.ToString("X8", System.Globalization.CultureInfo.InvariantCulture));
            }

            // Preserve the descriptor flags when they differ from the default for the element kind,
            // so unpack -> XML -> pack keeps the original descriptor (only padding may differ).
            if (property.SourceFlags is ushort sourceFlags &&
                sourceFlags != (property.IsArray ? 0x30 : 0x00))
            {
                writer.WriteAttributeString(
                    "flags", "0x" + sourceFlags.ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
            }

            property.WriteXmlAttributes(writer);

            // Preserve a non-default array element stride so the XML round-trip is byte-accurate.
            if (property.IsArray && property.SourceItemSize is int itemSize && itemSize != ComputeItemSize(property))
            {
                writer.WriteAttributeString(
                    "itemsize", itemSize.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (property.IsArray)
            {
                for (int i = 0; i < property.Count; i++)
                {
                    writer.WriteStartElement(info.Name);
                    property.WriteXmlValue(writer, i, names);
                    writer.WriteEndElement();
                }
            }
            else
            {
                property.WriteXmlValue(writer, 0, names);
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();
        writer.Flush();
    }

    /// <summary>Orders properties for XML output: description/parent first, then by name.</summary>
    private IEnumerable<(uint Id, Property Property)> OrderForOutput(NameRegistrySet names)
    {
        const uint description = 0x00B2CCCA;
        const uint parent = 0x00B2CCCB;
        const uint audioParent = 0x5F6317D5;
        const uint nameHash = 0x2F8B3BF4;

        var ordered = new List<(uint Id, Property Property)>();
        foreach (uint special in new[] { parent, audioParent, nameHash, description })
        {
            if (_properties.TryGetValue(special, out Property? p))
            {
                ordered.Add((special, p));
            }
        }

        var rest = _properties
            .Where(kv => kv.Key != parent && kv.Key != audioParent && kv.Key != nameHash && kv.Key != description)
            .OrderBy(kv => names.GetPropName(kv.Key), StringComparer.OrdinalIgnoreCase)
            .ThenBy(kv => kv.Key)
            .Select(kv => (kv.Key, kv.Value));

        ordered.AddRange(rest);
        return ordered;
    }

    private static uint ParseId(string text)
    {
        text = text.Trim();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || text.StartsWith('#'))
        {
            string hex = text.StartsWith('#') ? text[1..] : text[2..];
            return uint.Parse(hex, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
        }

        return uint.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
    }
}
