using System.Text;

namespace SporeMaster.Core.Search;

/// <summary>One match found by <see cref="SearchIndex"/>.</summary>
public sealed class SearchMatch
{
    public SearchMatch(string path, int lineNumber, string line)
    {
        Path = path;
        LineNumber = lineNumber;
        Line = line;
    }

    /// <summary>The file the match was found in.</summary>
    public string Path { get; }

    /// <summary>1-based line number of the match.</summary>
    public int LineNumber { get; }

    /// <summary>The full line containing the match (trimmed).</summary>
    public string Line { get; }

    public override string ToString() => $"{Path}:{LineNumber}: {Line.Trim()}";
}

/// <summary>
/// A simple, self-contained full-text index over unpacked text files. It lower-cases content once
/// and answers case-insensitive substring queries with line numbers and snippets.
/// </summary>
/// <remarks>
/// This is intentionally simpler than SporeMaster 2's suffix-array index: it trades peak query
/// speed for much smaller code and no external index files, which is more than enough for mod
/// folders. Large or binary files are skipped.
/// </remarks>
public sealed class SearchIndex
{
    private const int MaxFileBytes = 8 * 1024 * 1024;

    private readonly List<Entry> _entries = new();

    private sealed record Entry(string Path, string Original, string Lower);

    /// <summary>Files that were skipped (too large, binary or unreadable).</summary>
    public List<string> Skipped { get; } = new();

    public int FileCount => _entries.Count;

    public void AddFile(string path)
    {
        string content;
        try
        {
            var info = new FileInfo(path);
            if (info.Length > MaxFileBytes)
            {
                Skipped.Add(path);
                return;
            }

            byte[] bytes = File.ReadAllBytes(path);
            if (LooksBinary(bytes))
            {
                Skipped.Add(path);
                return;
            }

            content = Decode(bytes);
        }
        catch (Exception)
        {
            Skipped.Add(path);
            return;
        }

        AddText(path, content);
    }

    public void AddText(string path, string content)
    {
        _entries.Add(new Entry(path, content, content.ToLowerInvariant()));
    }

    /// <summary>Indexes every text file under <paramref name="directory"/>.</summary>
    public static SearchIndex BuildFromDirectory(string directory, IEnumerable<string>? extensions = null, SearchOption option = SearchOption.AllDirectories)
    {
        var index = new SearchIndex();
        if (!Directory.Exists(directory))
        {
            return index;
        }

        string[]? allowed = extensions?.Select(e => e.StartsWith('.') ? e : "." + e).ToArray();

        foreach (string file in Directory.EnumerateFiles(directory, "*", option))
        {
            if (allowed is not null && allowed.Length > 0 &&
                !allowed.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            index.AddFile(file);
        }

        return index;
    }

    /// <summary>Finds all lines containing <paramref name="query"/> (case-insensitive).</summary>
    public IReadOnlyList<SearchMatch> Search(string query, int maxResults = 1000)
    {
        var results = new List<SearchMatch>();
        if (string.IsNullOrEmpty(query) || maxResults <= 0)
        {
            return results;
        }

        string needle = query.ToLowerInvariant();

        foreach (Entry entry in _entries)
        {
            string[] originalLines = entry.Original.Split('\n');
            string[] lowerLines = entry.Lower.Split('\n');

            for (int i = 0; i < lowerLines.Length; i++)
            {
                if (lowerLines[i].Contains(needle, StringComparison.Ordinal))
                {
                    results.Add(new SearchMatch(entry.Path, i + 1, originalLines[i].TrimEnd('\r')));
                    if (results.Count >= maxResults)
                    {
                        return results;
                    }
                }
            }
        }

        return results;
    }

    /// <summary>Finds all lines matching a regular expression (case-insensitive).</summary>
    public IReadOnlyList<SearchMatch> SearchRegex(string pattern, int maxResults = 1000)
    {
        var results = new List<SearchMatch>();
        if (string.IsNullOrEmpty(pattern) || maxResults <= 0)
        {
            return results;
        }

        System.Text.RegularExpressions.Regex regex;
        try
        {
            regex = new System.Text.RegularExpressions.Regex(
                pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));
        }
        catch (ArgumentException)
        {
            return results;
        }

        foreach (Entry entry in _entries)
        {
            string[] lines = entry.Original.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                bool match;
                try
                {
                    match = regex.IsMatch(lines[i]);
                }
                catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
                {
                    match = false;
                }

                if (match)
                {
                    results.Add(new SearchMatch(entry.Path, i + 1, lines[i].TrimEnd('\r')));
                    if (results.Count >= maxResults)
                    {
                        return results;
                    }
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Scans files for a raw byte sequence (works on binary too), reporting file and offset.
    /// Used by the CLI <c>search --hex</c> mode.
    /// </summary>
    public static List<(string Path, long Offset)> SearchHex(
        string directory,
        IEnumerable<string>? extensions,
        byte[] needle,
        int maxResults = 1000)
    {
        var results = new List<(string Path, long Offset)>();
        if (!Directory.Exists(directory) || needle.Length == 0 || maxResults <= 0)
        {
            return results;
        }

        string[]? allowed = extensions?.Select(e => e.StartsWith('.') ? e : "." + e).ToArray();

        foreach (string file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            if (allowed is not null && allowed.Length > 0 &&
                !allowed.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            byte[] data;
            try
            {
                data = File.ReadAllBytes(file);
            }
            catch
            {
                continue;
            }

            int offset = 0;
            while (offset <= data.Length - needle.Length)
            {
                int index = data.AsSpan(offset).IndexOf(needle);
                if (index < 0)
                {
                    break;
                }

                results.Add((file, offset + index));
                if (results.Count >= maxResults)
                {
                    return results;
                }

                offset += index + 1;
            }
        }

        return results;
    }

    private static bool LooksBinary(byte[] data)
    {
        int limit = Math.Min(data.Length, 1024);
        for (int i = 0; i < limit; i++)
        {
            if (data[i] == 0)
            {
                return true;
            }
        }

        return false;
    }

    private static string Decode(byte[] data)
    {
        try
        {
            return new UTF8Encoding(false, true).GetString(data);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(data);
        }
    }
}
