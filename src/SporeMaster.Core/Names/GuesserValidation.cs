using SporeMaster.Core.Names;

namespace SporeMaster.Core.Names;

/// <summary>The outcome of validating the hash guesser against known (real) names.</summary>
public sealed class GuesserValidationResult
{
    /// <summary>Number of names the guesser was run against.</summary>
    public int Attempted { get; set; }

    /// <summary>Number of names for which the guesser returned at least one candidate.</summary>
    public int Found { get; set; }

    /// <summary>Number of names where the real name appeared anywhere in the results.</summary>
    public int Correct { get; set; }

    /// <summary>Number of names where the first candidate was the real name.</summary>
    public int Top1Correct { get; set; }

    /// <summary>Examples of names the guesser missed (real name and its top guess).</summary>
    public List<(string Real, string? TopGuess)> Misses { get; } = new();

    public double FoundRate => Attempted == 0 ? 0 : (double)Found / Attempted;

    public double CorrectRate => Attempted == 0 ? 0 : (double)Correct / Attempted;

    public double Top1Rate => Attempted == 0 ? 0 : (double)Top1Correct / Attempted;
}

/// <summary>
/// Measures how well the <see cref="HashGuesser"/> recovers real names. The real names come from
/// the game's key maps (<c>KeyMap.txt</c> / <c>PreBakedKeys.txt</c>), which are authoritative, so
/// they make an ideal ground truth: for every known key we know the true name and can check whether
/// the guesser finds it.
/// </summary>
public static class GuesserValidation
{
    /// <summary>
    /// Runs the guesser against the real names in <paramref name="keyMap"/>.
    /// </summary>
    /// <param name="guesser">The guesser to validate.</param>
    /// <param name="keyMap">Ground-truth keys and their real paths.</param>
    /// <param name="maxEntries">Maximum number of names to test (keeps runtime bounded).</param>
    /// <param name="maxResultsPerHash">How many candidates to ask for per hash.</param>
    /// <param name="progress">Called with (done, total) after each attempt.</param>
    public static GuesserValidationResult Validate(
        HashGuesser guesser,
        KeyMap keyMap,
        int maxEntries = 500,
        int maxResultsPerHash = 5,
        Action<int, int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(guesser);
        ArgumentNullException.ThrowIfNull(keyMap);

        var result = new GuesserValidationResult();

        // Only test instance names that actually hash to their key (the others are aliases).
        var candidates = new List<(uint Hash, string Name)>();
        foreach (KeyMapEntry entry in keyMap.Entries)
        {
            string name = InstanceName(entry.Path);
            if (name.Length > 0 && Hashing.Fnv.Hash(name) == entry.InstanceId)
            {
                candidates.Add((entry.InstanceId, name));
            }
        }

        // Sample evenly across the whole map so the result is representative (the map is grouped by
        // asset kind, so taking the first N would only test one kind).
        if (candidates.Count > maxEntries && maxEntries > 0)
        {
            int stride = Math.Max(1, candidates.Count / maxEntries);
            var sampled = new List<(uint Hash, string Name)>(maxEntries);
            for (int i = 0; i < candidates.Count && sampled.Count < maxEntries; i += stride)
            {
                sampled.Add(candidates[i]);
            }

            candidates = sampled;
        }

        int done = 0;
        foreach ((uint hash, string real) in candidates)
        {
            IReadOnlyList<string> guesses = guesser.Guess(hash, maxResults: maxResultsPerHash);

            result.Attempted++;
            if (guesses.Count > 0)
            {
                result.Found++;
                if (guesses.Contains(real))
                {
                    result.Correct++;
                }

                if (string.Equals(guesses[0], real, StringComparison.Ordinal))
                {
                    result.Top1Correct++;
                }
                else if (result.Misses.Count < 50)
                {
                    result.Misses.Add((real, guesses[0]));
                }
            }
            else if (result.Misses.Count < 50)
            {
                result.Misses.Add((real, null));
            }

            done++;
            progress?.Invoke(done, candidates.Count);
        }

        return result;
    }

    /// <summary>Extracts the instance name (file name without extension) from a path.</summary>
    public static string InstanceName(string path)
    {
        string normalized = path.Replace('\\', '/');
        int slash = normalized.LastIndexOf('/');
        string fileName = slash >= 0 ? normalized[(slash + 1)..] : normalized;
        int dot = fileName.LastIndexOf('.');
        return dot > 0 ? fileName[..dot] : fileName;
    }
}
