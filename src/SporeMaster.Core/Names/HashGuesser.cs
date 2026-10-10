using System.Globalization;
using SporeMaster.Core.Hashing;

namespace SporeMaster.Core.Names;

/// <summary>
/// Recovers the original name behind a hash by combining prefix/suffix/contains hints with the
/// fragment dictionaries that ship with SporeMaster (<c>guess_left.txt</c> / <c>guess_right.txt</c>).
/// </summary>
/// <remarks>
/// The algorithm is a faithful port of the legacy SporeMaster <c>HashGuesser</c>. Because Spore's
/// hash is FNV-1 (multiply-then-xor), it can be partially inverted: given the tail of a name we can
/// "un-hash" it and look the remainder up in a precomputed table.
///
/// For a candidate name <c>prefix + L + contains + R + suffix</c> (where <c>L</c> and <c>R</c> are
/// dictionary fragments):
/// <list type="number">
///   <item><c>h = FNV_rev(target, suffix)</c> removes the known suffix;</item>
///   <item>for each right fragment <c>R</c>, <c>h2 = FNV_rev(h, R)</c> removes it too, and if
///   <c>contains</c> is set, <c>h3 = FNV_rev(h2, contains)</c> removes that;</item>
///   <item>if <c>h3</c> equals <c>FNV(prefix + L)</c> for some left fragment <c>L</c> (found via a
///   hash map), the name is reconstructed.</item>
/// </list>
/// </remarks>
public sealed class HashGuesser
{
    /// <summary>The modular inverse of the FNV prime, used to reverse the hash.</summary>
    private const uint FnvUnstep = 0x359C449B;

    private readonly string[] _left;
    private readonly string[] _right;

    public HashGuesser(IEnumerable<string> leftFragments, IEnumerable<string> rightFragments)
    {
        _left = Prepare(leftFragments);
        _right = Prepare(rightFragments);
    }

    public int LeftCount => _left.Length;

    public int RightCount => _right.Length;

    private static string[] Prepare(IEnumerable<string> fragments) =>
        fragments
            .Where(f => !string.IsNullOrEmpty(f))
            .Select(f => f.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    /// <summary>Attempts to recover the name behind <paramref name="target"/>.</summary>
    public IReadOnlyList<string> Guess(
        uint target,
        string prefix = "",
        string suffix = "",
        string contains = "",
        int maxResults = 50)
    {
        prefix = prefix.ToLowerInvariant();
        suffix = suffix.ToLowerInvariant();
        contains = contains.ToLowerInvariant();

        var results = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        // Map FNV(prefix + fragment) -> the fragments that produce it.
        var leftHash = new Dictionary<uint, List<string>>();
        foreach (string fragment in _left)
        {
            uint h = Fnv.Hash(prefix + fragment);
            if (!leftHash.TryGetValue(h, out List<string>? list))
            {
                list = new List<string>();
                leftHash[h] = list;
            }

            list.Add(fragment);
        }

        uint th = FnvReverse(target, suffix);

        if (contains.Length > 0)
        {
            uint onlyContains = FnvReverse(th, contains);
            if (leftHash.TryGetValue(onlyContains, out List<string>? leftOnly))
            {
                foreach (string l in leftOnly)
                {
                    Add(prefix + l + contains + suffix);
                }
            }
        }

        foreach (string r in _right)
        {
            uint h = FnvReverse(th, r);
            if (leftHash.TryGetValue(h, out List<string>? leftList))
            {
                foreach (string l in leftList)
                {
                    Add(prefix + l + r + suffix);
                }
            }

            if (contains.Length > 0)
            {
                uint h2 = FnvReverse(h, contains);
                if (leftHash.TryGetValue(h2, out List<string>? leftList2))
                {
                    foreach (string l in leftList2)
                    {
                        Add(prefix + l + contains + r + suffix);
                    }
                }
            }

            if (results.Count >= maxResults)
            {
                break;
            }
        }

        return results;

        void Add(string candidate)
        {
            if (results.Count >= maxResults || !seen.Add(candidate))
            {
                return;
            }

            // Only keep candidates that really hash to the target.
            if (Fnv.Hash(candidate) == target)
            {
                results.Add(candidate);
            }
        }
    }

    /// <summary>
    /// Like <see cref="Guess"/>, but tries the result of each candidate as the prefix for the next
    /// round (used to recover longer names made of several fragments).
    /// </summary>
    public IReadOnlyList<string> GuessIterative(
        uint target,
        string prefix = "",
        string suffix = "",
        string contains = "",
        int rounds = 2,
        int maxResults = 50)
    {
        var results = new List<string>(Guess(target, prefix, suffix, contains, maxResults));
        var seeds = new List<string>(results);

        for (int round = 1; round < rounds && results.Count < maxResults; round++)
        {
            var newSeeds = new List<string>();
            foreach (string seed in seeds)
            {
                foreach (string guess in Guess(target, seed, suffix, contains, maxResults))
                {
                    if (!results.Contains(guess))
                    {
                        results.Add(guess);
                        newSeeds.Add(guess);
                        if (results.Count >= maxResults)
                        {
                            break;
                        }
                    }
                }

                if (results.Count >= maxResults)
                {
                    break;
                }
            }

            if (newSeeds.Count == 0)
            {
                break;
            }

            seeds = newSeeds;
        }

        return results;
    }

    private static uint FnvReverse(uint hash, string chars)
    {
        unchecked
        {
            for (int i = chars.Length - 1; i >= 0; i--)
            {
                hash ^= char.ToLowerInvariant(chars[i]);
                hash *= FnvUnstep;
            }

            return hash;
        }
    }
}

/// <summary>Loads the <c>guess_left.txt</c> / <c>guess_right.txt</c> fragment dictionaries.</summary>
public static class GuessDictionary
{
    public const string LeftFileName = "guess_left.txt";
    public const string RightFileName = "guess_right.txt";

    /// <summary>
    /// Searches for the dictionaries in the usual places and builds a <see cref="HashGuesser"/>,
    /// or returns null when the files cannot be found.
    /// </summary>
    public static HashGuesser? TryLoad(string? explicitDirectory = null, int maxPerList = 5_000_000)
    {
        string? left = Find(explicitDirectory, LeftFileName);
        string? right = Find(explicitDirectory, RightFileName);
        if (left is null || right is null)
        {
            return null;
        }

        return new HashGuesser(ReadLines(left, maxPerList), ReadLines(right, maxPerList));
    }

    /// <summary>Returns the directories that are searched for the dictionaries.</summary>
    public static IEnumerable<string> SearchPaths(string? explicitDirectory)
    {
        if (!string.IsNullOrEmpty(explicitDirectory))
        {
            yield return explicitDirectory;
        }

        yield return Directory.GetCurrentDirectory();
        yield return AppContext.BaseDirectory;

        string? directory = AppContext.BaseDirectory;
        for (int i = 0; i < 8 && directory is not null; i++)
        {
            yield return Path.Combine(directory, "data");
            directory = Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar));
        }
    }

    public static string? Find(string? explicitDirectory, string fileName)
    {
        foreach (string directory in SearchPaths(explicitDirectory))
        {
            if (string.IsNullOrEmpty(directory))
            {
                continue;
            }

            string candidate = Path.Combine(directory, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<string> ReadLines(string path, int max)
    {
        using var reader = new StreamReader(path);
        int count = 0;
        string? line;
        while (count < max && (line = reader.ReadLine()) is not null)
        {
            line = line.Trim();
            if (line.Length > 0)
            {
                yield return line;
                count++;
            }
        }
    }
}
