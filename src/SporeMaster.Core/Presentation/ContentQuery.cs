using System.Globalization;
using System.Text;

namespace SporeMaster.Core.Presentation;

/// <summary>The kind of content a <see cref="ContentQuery"/> searches for.</summary>
public enum ContentKind
{
    /// <summary>A case-insensitive substring in the resource decoded as text.</summary>
    Text,

    /// <summary>A byte sequence (given as hex) inside the resource.</summary>
    Hex,
}

/// <summary>
/// Parses a filter query into a name part plus an optional content clause, e.g.
/// <c>ce_*.rw4 text:gRob</c> or <c>hex:0B0A2C</c>. The content clause uses the markers
/// <c>text:&lt;substring&gt;</c> and <c>hex:&lt;bytes&gt;</c> (quotes allowed for spaces:
/// <c>text:"foo bar"</c>). Everything else is the name filter.
/// </summary>
public sealed class ContentQuery
{
    private ContentQuery(string namePart)
    {
        NamePart = namePart;
    }

    /// <summary>The name-filter part of the query (may be empty).</summary>
    public string NamePart { get; }

    public ContentKind? Kind { get; private set; }

    private string _text = string.Empty;
    private byte[] _bytes = Array.Empty<byte>();

    public static ContentQuery Parse(string? query)
    {
        string value = query?.Trim() ?? string.Empty;
        int marker = FindMarker(value);
        if (marker < 0)
        {
            return new ContentQuery(value);
        }

        var result = new ContentQuery(value[..marker].Trim());
        string clause = value[marker..].Trim();

        if (clause.StartsWith("text:", StringComparison.OrdinalIgnoreCase))
        {
            result.Kind = ContentKind.Text;
            result._text = Unquote(clause[5..].Trim());
        }
        else if (clause.StartsWith("hex:", StringComparison.OrdinalIgnoreCase))
        {
            result.Kind = ContentKind.Hex;
            result._bytes = ParseHex(clause[4..]);
        }

        return result;
    }

    /// <summary>True when a content clause is present.</summary>
    public bool HasContent => Kind is not null;

    /// <summary>A human-readable description of the content clause.</summary>
    public string Describe() => Kind switch
    {
        ContentKind.Text => $"text \"{_text}\"",
        ContentKind.Hex => "hex " + Convert.ToHexString(_bytes),
        _ => string.Empty,
    };

    /// <summary>True when the resource data matches the content clause.</summary>
    public bool Matches(byte[] data)
    {
        switch (Kind)
        {
            case ContentKind.Hex:
                return _bytes.Length > 0 && ContainsBytes(data, _bytes);
            case ContentKind.Text:
                if (_text.Length == 0)
                {
                    return true;
                }

                string decoded = Encoding.UTF8.GetString(data);
                return System.Text.RegularExpressions.Regex.IsMatch(
                    decoded,
                    System.Text.RegularExpressions.Regex.Escape(_text),
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            default:
                return true;
        }
    }

    private static int FindMarker(string value)
    {
        // Find " text:" or " hex:" or a marker at the very start, case-insensitively.
        for (int i = 0; i + 5 <= value.Length; i++)
        {
            bool atStart = i == 0 || char.IsWhiteSpace(value[i - 1]);
            if (!atStart)
            {
                continue;
            }

            if (value.AsSpan(i).StartsWith("text:", StringComparison.OrdinalIgnoreCase)
                || value.AsSpan(i).StartsWith("hex:", StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static string Unquote(string value) =>
        value.Length >= 2 && value[0] == '"' && value[^1] == '"' ? value[1..^1] : value;

    private static byte[] ParseHex(string value)
    {
        value = Unquote(value).Replace(" ", string.Empty).Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase);
        if (value.Length % 2 != 0)
        {
            return Array.Empty<byte>();
        }

        var bytes = new byte[value.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            if (!byte.TryParse(value.AsSpan(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out bytes[i]))
            {
                return Array.Empty<byte>();
            }
        }

        return bytes;
    }

    private static bool ContainsBytes(byte[] haystack, byte[] needle)
    {
        if (needle.Length == 0 || haystack.Length < needle.Length)
        {
            return false;
        }

        return haystack.AsSpan().IndexOf(needle) >= 0;
    }
}
