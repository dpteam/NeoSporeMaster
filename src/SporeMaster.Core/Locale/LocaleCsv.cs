using System.Globalization;
using System.Text;

namespace SporeMaster.Core.Locale;

/// <summary>
/// Exports and imports locale tables as CSV/TSV (for translators and spreadsheets). The first column
/// is the instance id (8-hex) and the second is the text. Fields containing the separator, quotes or
/// newlines are quoted; embedded quotes are doubled (RFC 4180 style).
/// </summary>
public static class LocaleCsv
{
    /// <summary>Serialises a locale table to CSV (<c>id,text</c>), with a header row.</summary>
    public static string Export(LocaleTable table, char separator = ',')
    {
        ArgumentNullException.ThrowIfNull(table);

        var builder = new StringBuilder();
        builder.Append("id").Append(separator).Append("text").Append('\n');
        foreach (LocaleEntry entry in table.Entries)
        {
            builder.Append(entry.InstanceId.ToString("X8", CultureInfo.InvariantCulture));
            builder.Append(separator);
            builder.Append(Quote(entry.Text, separator));
            builder.Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>Parses a CSV/TSV of <c>id,text</c> rows back into a locale table.</summary>
    public static LocaleTable Import(string text, char separator = ',')
    {
        var table = new LocaleTable();
        if (string.IsNullOrEmpty(text))
        {
            return table;
        }

        foreach (List<string> fields in ParseRows(text, separator))
        {
            if (fields.Count < 2)
            {
                continue;
            }

            string idText = fields[0].Trim();
            if (idText.Equals("id", StringComparison.OrdinalIgnoreCase))
            {
                continue; // header
            }

            if (TryParseId(idText, out uint id))
            {
                table.Add(id, fields[1]);
            }
        }

        return table;
    }

    /// <summary>Detects the separator (tab wins over comma) so TSV and CSV both work.</summary>
    public static char DetectSeparator(string text)
    {
        int firstNewline = text.IndexOf('\n');
        string header = firstNewline >= 0 ? text[..firstNewline] : text;
        return header.Contains('\t') ? '\t' : ',';
    }

    private static string Quote(string value, char separator)
    {
        bool needsQuote = value.Contains(separator) || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        if (!needsQuote)
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static IEnumerable<List<string>> ParseRows(string text, char separator)
    {
        var row = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;
        string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');

        for (int i = 0; i < normalized.Length; i++)
        {
            char c = normalized[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < normalized.Length && normalized[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }
            }
            else if (c == '"' && field.Length == 0)
            {
                inQuotes = true;
            }
            else if (c == separator)
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if (c == '\n')
            {
                row.Add(field.ToString());
                field.Clear();
                yield return row;
                row = new List<string>();
            }
            else
            {
                field.Append(c);
            }
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            yield return row;
        }
    }

    private static bool TryParseId(string token, out uint id)
    {
        token = token.Trim();
        if (token.StartsWith('#'))
        {
            return uint.TryParse(token[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out id);
        }

        if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(token[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out id);
        }

        return uint.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out id);
    }
}
