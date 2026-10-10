using System.Text;

namespace SporeMaster.Core.Binary;

/// <summary>A printable string found inside a binary file, with its byte offset.</summary>
public sealed class BinaryString
{
    public required long Offset { get; init; }

    public required string Value { get; init; }

    /// <summary>True when the string was stored as UTF-16 (two bytes per character).</summary>
    public bool Unicode { get; init; }
}

/// <summary>
/// Extracts printable strings (ASCII and UTF-16LE) from arbitrary binaries such as executables and
/// PDB/DLL files. Useful for research: internal names, file paths, error messages, configuration
/// keys and asset references often survive in a release build.
/// </summary>
public static class StringsExtractor
{
    /// <summary>Scans a byte buffer for printable ASCII and UTF-16LE strings.</summary>
    public static List<BinaryString> Extract(
        byte[] data,
        int minimumLength = 4,
        bool ascii = true,
        bool unicode = true,
        string? contains = null,
        int limit = int.MaxValue)
    {
        var results = new List<BinaryString>();
        if (minimumLength < 1)
        {
            minimumLength = 1;
        }

        if (ascii)
        {
            ExtractAscii(data, minimumLength, contains, results, limit);
        }

        if (unicode && results.Count < limit)
        {
            ExtractUnicode(data, Math.Max(2, minimumLength), contains, results, limit);
        }

        results.Sort((a, b) => a.Offset.CompareTo(b.Offset));
        return results;
    }

    private static void ExtractAscii(
        byte[] data,
        int minimumLength,
        string? contains,
        List<BinaryString> results,
        int limit)
    {
        int start = -1;
        for (int i = 0; i <= data.Length; i++)
        {
            bool printable = i < data.Length && IsPrintableAscii(data[i]);
            if (printable)
            {
                if (start < 0)
                {
                    start = i;
                }
            }
            else if (start >= 0)
            {
                int length = i - start;
                if (length >= minimumLength)
                {
                    string value = Encoding.ASCII.GetString(data, start, length);
                    if (contains is null || value.Contains(contains, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(new BinaryString { Offset = start, Value = value });
                        if (results.Count >= limit)
                        {
                            return;
                        }
                    }
                }

                start = -1;
            }
        }
    }

    private static void ExtractUnicode(
        byte[] data,
        int minimumChars,
        string? contains,
        List<BinaryString> results,
        int limit)
    {
        int start = -1;
        int i = 0;
        while (i + 1 <= data.Length)
        {
            bool printable = i + 1 < data.Length
                && data[i + 1] == 0
                && IsPrintableAscii(data[i]);

            if (printable)
            {
                if (start < 0)
                {
                    start = i;
                }

                i += 2;
            }
            else
            {
                if (start >= 0)
                {
                    int chars = (i - start) / 2;
                    if (chars >= minimumChars)
                    {
                        var sb = new StringBuilder(chars);
                        for (int c = start; c < i; c += 2)
                        {
                            sb.Append((char)data[c]);
                        }

                        string value = sb.ToString();
                        if (contains is null || value.Contains(contains, StringComparison.OrdinalIgnoreCase))
                        {
                            results.Add(new BinaryString { Offset = start, Value = value, Unicode = true });
                            if (results.Count >= limit)
                            {
                                return;
                            }
                        }
                    }

                    start = -1;
                }

                i++;
            }
        }
    }

    private static bool IsPrintableAscii(byte b) => b is >= 32 and < 127;
}
