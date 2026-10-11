using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Hashing;
using SporeMaster.Core.Names;
using SporeMaster.Core.Properties;

namespace SporeMaster.Core.ArgScript;

/// <summary>One token of an ArgScript line.</summary>
internal readonly record struct Token(string Text, bool Quoted);

/// <summary>
/// Minimal ArgScript tokenizer sufficient for property files: splits a line into words,
/// handling <c>//</c> comments, quoted strings and parenthesised groups (kept as one word).
/// </summary>
internal static class ArgScriptTokenizer
{
    public static List<Token> Tokenize(string line)
    {
        var tokens = new List<Token>();
        int i = 0;

        while (i < line.Length)
        {
            char c = line[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (c == '/' && i + 1 < line.Length && line[i + 1] == '/')
            {
                break; // comment
            }

            if (c == '"')
            {
                int start = ++i;
                var sb = new StringBuilder();
                while (i < line.Length && line[i] != '"')
                {
                    sb.Append(line[i++]);
                }

                i++; // closing quote
                tokens.Add(new Token(ArgScriptFormat.ToOriginalText(sb.ToString()), true));
                continue;
            }

            if (c == '(')
            {
                var sb = new StringBuilder("(");
                i++;
                int depth = 1;
                while (i < line.Length && depth > 0)
                {
                    char d = line[i];
                    if (d == '(')
                    {
                        depth++;
                    }
                    else if (d == ')')
                    {
                        depth--;
                    }

                    sb.Append(d);
                    i++;
                }

                tokens.Add(new Token(sb.ToString(), false));
                continue;
            }

            // Bare word.
            int wordStart = i;
            while (i < line.Length && !char.IsWhiteSpace(line[i]))
            {
                i++;
            }

            tokens.Add(new Token(line[wordStart..i], false));
        }

        return tokens;
    }
}

/// <summary>Shared helpers for the ArgScript text format.</summary>
public static class ArgScriptFormat
{
    /// <summary>Escapes text for a quoted ArgScript literal.</summary>
    public static string ToValidText(string text) => text.Replace("\"", "&quot;").Replace("#", "&hash;");

    /// <summary>Reverses <see cref="ToValidText"/>.</summary>
    public static string ToOriginalText(string text) => text.Replace("&quot;", "\"").Replace("&hash;", "#");

    /// <summary>Formats a float like SporeModder (invariant, up to 7 decimals).</summary>
    public static string Float(float value) => value.ToString("0.#######", CultureInfo.InvariantCulture);

    public static string Double(double value) => value.ToString("0.#######", CultureInfo.InvariantCulture);
}

/// <summary>
/// Reads and writes Spore property files in the ArgScript text form (<c>.prop_t</c>) used by
/// SporeModder FX.
/// </summary>
/// <remarks>
/// This is a focused subset covering every property type except that <c>transform</c> is emitted
/// in a raw 13-value form (which round-trips exactly) rather than SporeModder's
/// offset/scale/rotate options. See <c>docs/PROP_FORMAT.md</c>.
/// </remarks>
public static class PropertyScript
{
    private static readonly Regex VectorRegex = new(@"^\(\s*(.*?)\s*\)$", RegexOptions.Compiled);

    // ---- writing ----

    public static string Write(PropertyList list, NameRegistrySet names)
    {
        var builder = new StringBuilder();
        foreach ((uint id, Property property) in OrderForOutput(list, names))
        {
            PropertyTypeInfo info = property.Info;
            string keyword = property.IsArray ? info.PluralName : info.Name;
            string name = names.GetPropName(id);

            if (property.IsArray)
            {
                builder.Append(keyword).Append(' ').Append(name).Append('\n');
                for (int i = 0; i < property.Count; i++)
                {
                    builder.Append('\t').Append(FormatElement(property, i, names)).Append('\n');
                }

                builder.Append("end\n");
            }
            else
            {
                builder.Append(keyword).Append(' ').Append(name)
                    .Append(' ').Append(FormatElement(property, 0, names)).Append('\n');
            }
        }

        return builder.ToString();
    }

    private static string FormatElement(Property property, int index, NameRegistrySet names)
    {
        switch (property)
        {
            case BoolProperty b:
                return b.Values[index] ? "true" : "false";
            case CharProperty ch:
                return ch.Values[index].ToString(CultureInfo.InvariantCulture);
            case WCharProperty w:
                return w.Values[index].ToString(CultureInfo.InvariantCulture);
            case IntegerProperty ip:
                return FormatInt(ip.Values[index], ip.Type, names);
            case FloatProperty f:
                return ArgScriptFormat.Float(f.Values[index]);
            case DoubleProperty d:
                return ArgScriptFormat.Double(d.Values[index]);
            case StringProperty sp:
                return "\"" + ArgScriptFormat.ToValidText(sp.GetText(index)) + "\"";
            case KeyProperty kp:
                return FormatKey(kp.Values[index], names);
            case TextProperty tp:
                return FormatText(tp.Values[index], names);
            case FloatVectorProperty v:
                return "(" + string.Join(", ", v.GetComponents(index).Select(ArgScriptFormat.Float)) + ")";
            case BoundingBoxProperty bb:
                float[] box = bb.GetBox(index);
                return $"({ArgScriptFormat.Float(box[0])}, {ArgScriptFormat.Float(box[1])}, {ArgScriptFormat.Float(box[2])}) " +
                       $"({ArgScriptFormat.Float(box[3])}, {ArgScriptFormat.Float(box[4])}, {ArgScriptFormat.Float(box[5])})";
            case TransformProperty t:
            {
                TransformProperty.TransformValue tv = t.GetTransform(index);
                var sb = new StringBuilder();
                sb.Append("0x").Append(tv.Flags.ToString("X4", CultureInfo.InvariantCulture)).Append(' ');
                sb.Append(ArgScriptFormat.Float(tv.Scale)).Append(' ');
                sb.Append('(').Append(ArgScriptFormat.Float(tv.Offset[0])).Append(", ")
                    .Append(ArgScriptFormat.Float(tv.Offset[1])).Append(", ")
                    .Append(ArgScriptFormat.Float(tv.Offset[2])).Append(") (");
                for (int r = 0; r < 9; r++)
                {
                    if (r > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(ArgScriptFormat.Float(tv.Rotation[r]));
                }

                sb.Append(')');
                return sb.ToString();
            }
            default:
                return string.Empty;
        }
    }

    private static string FormatInt(long value, PropertyType type, NameRegistrySet names)
    {
        bool unsigned = type is PropertyType.UInt8 or PropertyType.UInt16 or PropertyType.UInt32 or PropertyType.UInt64;
        if (!unsigned && value is > -10_000_000 and < 10_000_000)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        if (unsigned && (ulong)value < 10_000_000)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        uint hash = (uint)value;
        string name = names.GetFileName(hash);
        if (!name.StartsWith('#'))
        {
            return "hash(" + name + ")";
        }

        return "0x" + hash.ToString("X8", CultureInfo.InvariantCulture);
    }

    private static string FormatKey(ResourceKey key, NameRegistrySet names)
    {
        var sb = new StringBuilder();
        if (key.GroupId != 0)
        {
            sb.Append(names.GetFileName(key.GroupId)).Append('!');
        }

        sb.Append(names.GetFileName(key.InstanceId));
        if (key.TypeId != 0)
        {
            sb.Append('.').Append(names.GetTypeName(key.TypeId));
        }

        string text = sb.ToString();
        return text.Contains(' ') ? "\"" + text + "\"" : text;
    }

    private static string FormatText(Properties.LocalizedText text, NameRegistrySet names)
    {
        string literal = "\"" + ArgScriptFormat.ToValidText(text.Text) + "\"";
        if (text.TableId == 0)
        {
            return literal;
        }

        string table = names.GetFileName(text.TableId);
        return $"({table}!0x{text.InstanceId:X8}) {literal}";
    }

    private static IEnumerable<(uint Id, Property Property)> OrderForOutput(PropertyList list, NameRegistrySet names)
    {
        const uint description = 0x00B2CCCA;
        const uint parent = 0x00B2CCCB;
        var ordered = new List<(uint, Property)>();
        foreach (uint special in new[] { parent, description })
        {
            Property? p = list.Get(special);
            if (p is not null)
            {
                ordered.Add((special, p));
            }
        }

        ordered.AddRange(list.Properties
            .Where(kv => kv.Key != parent && kv.Key != description)
            .OrderBy(kv => names.GetPropName(kv.Key), StringComparer.OrdinalIgnoreCase)
            .ThenBy(kv => kv.Key)
            .Select(kv => (kv.Key, kv.Value)));
        return ordered;
    }

    // ---- parsing ----

    public static PropertyList Parse(string text, NameRegistrySet names)
    {
        var list = new PropertyList();
        string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        int i = 0;
        while (i < lines.Length)
        {
            string line = lines[i].Trim();
            i++;

            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith("//"))
            {
                continue;
            }

            List<Token> tokens = ArgScriptTokenizer.Tokenize(line);
            if (tokens.Count == 0 || tokens[0].Quoted)
            {
                continue;
            }

            string keyword = tokens[0].Text;
            PropertyTypeInfo? info = PropertyTypes.FindByName(keyword);
            if (info is null)
            {
                continue; // unknown command
            }

            bool isArray = string.Equals(info.PluralName, keyword, StringComparison.OrdinalIgnoreCase);
            Property property = PropertyFactory.Create(info.Type);
            property.Clear();
            property.IsArray = isArray;

            if (tokens.Count < 2)
            {
                // No name argument; skip (or consume block).
                if (isArray)
                {
                    i = SkipBlock(lines, i);
                }

                continue;
            }

            string name = tokens[1].Text;
            uint id = names.GetPropHash(name);

            if (isArray)
            {
                var block = new List<string>();
                while (i < lines.Length && lines[i].Trim() != "end")
                {
                    string blockLine = lines[i].Trim();
                    if (blockLine.Length > 0 && !blockLine.StartsWith("//"))
                    {
                        block.Add(blockLine);
                    }

                    i++;
                }

                i++; // consume "end"
                foreach (string elementLine in block)
                {
                    ParseElement(property, elementLine, names);
                }
            }
            else
            {
                ParseElement(property, tokens, names, startIndex: 2);
            }

            if (property.Count > 0)
            {
                list.Add(id, property);
            }
        }

        return list;
    }

    private static int SkipBlock(string[] lines, int i)
    {
        while (i < lines.Length && lines[i].Trim() != "end")
        {
            i++;
        }

        return i + 1;
    }

    private static void ParseElement(Property property, string line, NameRegistrySet names)
    {
        List<Token> tokens = ArgScriptTokenizer.Tokenize(line);
        ParseElement(property, tokens, names, startIndex: 0);
    }

    private static void ParseElement(Property property, List<Token> tokens, NameRegistrySet names, int startIndex)
    {
        if (startIndex >= tokens.Count)
        {
            return;
        }

        switch (property)
        {
            case BoolProperty b:
                b.Add(tokens[startIndex].Text.Equals("true", StringComparison.OrdinalIgnoreCase));
                break;
            case CharProperty ch:
                ch.Add((byte)ParseUInt(tokens[startIndex].Text, names));
                break;
            case WCharProperty w:
                w.Add((ushort)ParseUInt(tokens[startIndex].Text, names));
                break;
            case IntegerProperty ip:
                ip.Add(ParseInt(tokens[startIndex].Text, names));
                break;
            case FloatProperty f:
                f.Add(ParseFloat(tokens[startIndex].Text));
                break;
            case DoubleProperty d:
                d.Add(ParseFloat(tokens[startIndex].Text));
                break;
            case StringProperty sp:
                sp.Add(tokens[startIndex].Text);
                break;
            case KeyProperty kp:
                kp.Add(ParseKey(tokens[startIndex].Text, names));
                break;
            case TextProperty tp:
                tp.Add(ParseText(tokens, startIndex, names));
                break;
            case FloatVectorProperty v:
                v.Add(ParseVector(tokens, startIndex));
                break;
            case BoundingBoxProperty bb:
                bb.Add(ParseBox(tokens, startIndex));
                break;
            case TransformProperty t:
                t.Add(ParseTransform(tokens, startIndex));
                break;
        }
    }

    private static Properties.LocalizedText ParseText(List<Token> tokens, int startIndex, NameRegistrySet names)
    {
        var result = new Properties.LocalizedText();
        string first = tokens[startIndex].Text;

        if (first.StartsWith('(') && !tokens[startIndex].Quoted)
        {
            // (table!0xinstance)
            string inner = first.Trim('(', ')');
            string[] parts = inner.Split('!');
            if (parts.Length == 2)
            {
                result.TableId = names.GetFileHash(parts[0]);
                result.InstanceId = ParseUInt(parts[1], names);
            }

            if (startIndex + 1 < tokens.Count)
            {
                result.Text = tokens[startIndex + 1].Text;
            }
        }
        else
        {
            result.Text = first;
        }

        return result;
    }

    private static float[] ParseVector(List<Token> tokens, int startIndex)
    {
        if (startIndex >= tokens.Count)
        {
            return Array.Empty<float>();
        }

        string inner = VectorRegex.Replace(tokens[startIndex].Text, "$1");
        return inner.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseFloat)
            .ToArray();
    }

    private static float[] ParseBox(List<Token> tokens, int startIndex)
    {
        float[] min = ParseVector(tokens, startIndex);
        float[] max = ParseVector(tokens, startIndex + 1);
        var box = new float[6];
        Array.Copy(min, box, Math.Min(3, min.Length));
        for (int i = 0; i < 3 && i < max.Length; i++)
        {
            box[3 + i] = max[i];
        }

        return box;
    }

    private static TransformProperty.TransformValue ParseTransform(List<Token> tokens, int startIndex)
    {
        ushort flags = 0;
        float scale = 1f;
        var offset = new float[3];
        var rotation = new float[9];
        rotation[0] = 1f;
        rotation[4] = 1f;
        rotation[8] = 1f;

        if (startIndex < tokens.Count)
        {
            flags = (ushort)ParseUInt(tokens[startIndex].Text, null);
        }

        if (startIndex + 1 < tokens.Count)
        {
            scale = ParseFloat(tokens[startIndex + 1].Text);
        }

        if (startIndex + 2 < tokens.Count)
        {
            float[] values = ParseVector(tokens, startIndex + 2);
            Array.Copy(values, offset, Math.Min(3, values.Length));
        }

        if (startIndex + 3 < tokens.Count)
        {
            float[] values = ParseVector(tokens, startIndex + 3);
            Array.Copy(values, rotation, Math.Min(9, values.Length));
        }

        return new TransformProperty.TransformValue
        {
            Flags = flags,
            Count = 1,
            Offset = offset,
            Scale = scale,
            Rotation = rotation,
        };
    }

    private static long ParseInt(string token, NameRegistrySet? names) => unchecked((long)ParseUIntInner(token, names, signed: true));

    private static uint ParseUInt(string token, NameRegistrySet? names) => ParseUIntInner(token, names, signed: false);

    private static uint ParseUIntInner(string token, NameRegistrySet? names, bool signed)
    {
        token = token.Trim();
        if (token.StartsWith("hash(", StringComparison.OrdinalIgnoreCase) && token.EndsWith(')'))
        {
            string name = token[5..^1];
            return names?.GetFileHash(name) ?? Fnv.Hash(name);
        }

        if (token.StartsWith('#'))
        {
            return uint.Parse(token[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.Parse(token[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        if (token.EndsWith('b') && token.Length > 1)
        {
            return Convert.ToUInt32(token[..^1], 2);
        }

        if (signed && token.StartsWith('-'))
        {
            return unchecked((uint)(long.Parse(token, CultureInfo.InvariantCulture)));
        }

        return uint.Parse(token, CultureInfo.InvariantCulture);
    }

    private static float ParseFloat(string token) =>
        float.Parse(token.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);

    private static ResourceKey ParseKey(string token, NameRegistrySet names)
    {
        token = token.Trim('"');
        uint group = 0;
        uint type = 0;
        uint instance;

        int bang = token.IndexOf('!');
        string instanceAndType = token;
        if (bang >= 0)
        {
            group = names.GetFileHash(token[..bang]);
            instanceAndType = token[(bang + 1)..];
        }

        int dot = instanceAndType.LastIndexOf('.');
        if (dot >= 0)
        {
            instance = names.GetFileHash(instanceAndType[..dot]);
            type = names.GetTypeHash(instanceAndType[(dot + 1)..]);
        }
        else
        {
            instance = names.GetFileHash(instanceAndType);
        }

        return new ResourceKey(type, group, instance);
    }
}
