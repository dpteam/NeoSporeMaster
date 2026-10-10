using System.Globalization;
using System.Text;

namespace SporeMaster.Core.Locale;

/// <summary>One entry of a locale table: an instance id and its text.</summary>
public sealed class LocaleEntry
{
    public LocaleEntry(uint instanceId, string text)
    {
        InstanceId = instanceId;
        Text = text;
    }

    public uint InstanceId { get; set; }

    public string Text { get; set; }
}

/// <summary>
/// A Spore locale table (the <c>.locale</c> resource, type <c>0x02FAC0B6</c>).
/// </summary>
/// <remarks>
/// Locale tables are stored as plain UTF-8 text, one entry per line:
/// <code>
/// # optional comment
/// 0x00000001 Some text
/// 0x00000002 Another text
/// </code>
/// The game looks a text up by the pair <c>tableID!instanceID</c>, where <c>tableID</c> is the
/// resource's instance id (the file name without extension) and <c>instanceID</c> is the value
/// before each line. See <c>docs/LOCALE_FORMAT.md</c>.
/// </remarks>
public sealed class LocaleTable
{
    private readonly List<LocaleEntry> _entries = new();
    private List<string>? _sourceLines;

    public IReadOnlyList<LocaleEntry> Entries => _entries;

    public int Count => _entries.Count;

    /// <summary>Optional comment written at the top when the table is generated from scratch.</summary>
    public string? HeaderComment { get; set; }

    public void Add(uint instanceId, string text)
    {
        _entries.Add(new LocaleEntry(instanceId, text));
        _sourceLines = null;
    }

    public string? GetText(uint instanceId)
    {
        foreach (LocaleEntry entry in _entries)
        {
            if (entry.InstanceId == instanceId)
            {
                return entry.Text;
            }
        }

        return null;
    }

    public bool IsEmpty => _entries.Count == 0;

    /// <summary>Parses a locale table from text. Comment and blank lines are preserved.</summary>
    public static LocaleTable Parse(string text)
    {
        var table = new LocaleTable();
        var lines = new List<string>();

        string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        // Keep a trailing-newline flag by remembering the raw split without dropping the last empty.
        string[] rawLines = normalized.Split('\n');
        if (rawLines.Length > 0 && rawLines[^1].Length == 0)
        {
            Array.Resize(ref rawLines, rawLines.Length - 1);
            table.TrailingNewline = true;
        }

        foreach (string line in rawLines)
        {
            lines.Add(line);
            string trimmed = line.TrimStart();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            int space = IndexOfWhitespace(trimmed);
            string idToken = space < 0 ? trimmed : trimmed[..space];
            string value = space < 0 ? string.Empty : trimmed[(space + 1)..];

            if (TryParseId(idToken, out uint id))
            {
                table._entries.Add(new LocaleEntry(id, value));
            }
        }

        table._sourceLines = lines;
        return table;
    }

    /// <summary>True when the parsed source ended with a newline (preserved on write).</summary>
    public bool TrailingNewline { get; set; } = true;

    public static LocaleTable Read(TextReader reader) => Parse(reader.ReadToEnd());

    public static LocaleTable Read(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return Parse(reader.ReadToEnd());
    }

    /// <summary>Serialises the table; preserves the original line layout when unchanged.</summary>
    public string Serialize()
    {
        var builder = new StringBuilder();

        if (_sourceLines is not null)
        {
            foreach (string line in _sourceLines)
            {
                builder.Append(line).Append('\n');
            }

            if (!TrailingNewline && builder.Length > 0)
            {
                builder.Length--; // drop the final newline
            }

            return builder.ToString();
        }

        if (HeaderComment is not null)
        {
            builder.Append("# ").Append(HeaderComment).Append('\n');
        }

        foreach (LocaleEntry entry in _entries)
        {
            builder.Append("0x")
                .Append(entry.InstanceId.ToString("X8", CultureInfo.InvariantCulture))
                .Append(' ')
                .Append(entry.Text)
                .Append('\n');
        }

        return builder.ToString();
    }

    public void Write(TextWriter writer) => writer.Write(Serialize());

    public void Write(Stream stream)
    {
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
        writer.Write(Serialize());
        writer.Flush();
    }

    private static int IndexOfWhitespace(string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            if (char.IsWhiteSpace(value[i]))
            {
                return i;
            }
        }

        return -1;
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

        // Locale ids are conventionally hexadecimal even without a prefix.
        return uint.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out id);
    }
}
