using System.Text.RegularExpressions;

namespace SporeMaster.Core.Presentation;

/// <summary>
/// Compiles a resource filter query into a matcher. Supported forms:
/// <list type="bullet">
/// <item>plain text — case-insensitive substring (fast, no regex);</item>
/// <item>wildcards — <c>*</c> (any run) and <c>?</c> (any single char), e.g. <c>*.rw4</c>;</item>
/// <item>regular expression — when the query starts with <c>re:</c>, e.g. <c>re:^ce_.*\.gmsh$</c>.</item>
/// </list>
/// </summary>
public sealed class NameFilter
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

    private string? _substring;
    private Regex? _regex;

    private NameFilter()
    {
    }

    /// <summary>True when the filter accepts everything (an empty query).</summary>
    public bool IsEmpty => _substring is null && _regex is null;

    /// <summary>True when the query is a regular expression (a <c>re:</c> prefix).</summary>
    public bool IsRegularExpression { get; private init; }

    public static NameFilter Compile(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new NameFilter();
        }

        string value = query.Trim();

        try
        {
            if (value.StartsWith("re:", StringComparison.OrdinalIgnoreCase))
            {
                return new NameFilter
                {
                    _regex = new Regex(value[3..], RegexOptions.IgnoreCase, RegexTimeout),
                    IsRegularExpression = true,
                };
            }

            if (value.IndexOfAny(new[] { '*', '?' }) >= 0)
            {
                return new NameFilter
                {
                    _regex = new Regex("^" + WildcardToRegex(value) + "$", RegexOptions.IgnoreCase, RegexTimeout),
                };
            }

            return new NameFilter { _substring = value };
        }
        catch (ArgumentException)
        {
            // An invalid regex must not break filtering: fall back to a literal substring.
            return new NameFilter { _substring = value };
        }
    }

    /// <summary>True when <paramref name="text"/> matches the query.</summary>
    public bool Matches(string text)
    {
        if (_substring is not null)
        {
            return text.Contains(_substring, StringComparison.OrdinalIgnoreCase);
        }

        if (_regex is null)
        {
            return true;
        }

        try
        {
            return _regex.IsMatch(text);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private static string WildcardToRegex(string wildcard)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char c in wildcard)
        {
            sb.Append(c switch
            {
                '*' => ".*",
                '?' => ".",
                _ => Regex.Escape(c.ToString()),
            });
        }

        return sb.ToString();
    }
}
