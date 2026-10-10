using System.Globalization;
using System.Text;
using SporeMaster.Core.Dbpf;

namespace SporeMaster.Core.Names;

/// <summary>The outcome of a batch name-recovery pass over a package.</summary>
public sealed class NameRecoveryResult
{
    /// <summary>Recovered hash -> name pairs.</summary>
    public Dictionary<uint, string> Recovered { get; } = new();

    /// <summary>Number of distinct hashes that had no registered name.</summary>
    public int UnnamedCount { get; set; }

    /// <summary>Number of hashes actually attempted (bounded by the caller's limit).</summary>
    public int InspectedCount { get; set; }

    /// <summary>Number of unique names written.</summary>
    public int RecoveredCount => Recovered.Count;
}

/// <summary>
/// Recovers names for every unknown hash in a package by running the <see cref="HashGuesser"/>,
/// and can persist the result as a SporeMaster-compatible <c>names.txt</c> registry.
/// </summary>
public static class NameRecovery
{
    /// <summary>
    /// Scans the package for unknown instance (and optionally group) hashes and tries to recover
    /// their names. <paramref name="progress"/> is called with (done, total) after each attempt.
    /// </summary>
    public static NameRecoveryResult Recover(
        DatabasePackedFile package,
        HashGuesser guesser,
        NameRegistrySet names,
        bool includeGroups = false,
        int maxHashes = 200,
        int maxResultsPerHash = 5,
        Action<int, int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(guesser);
        ArgumentNullException.ThrowIfNull(names);

        var result = new NameRecoveryResult();

        // Collect distinct unknown hashes first (group and instance names share one registry).
        var unknown = new List<uint>();
        var seen = new HashSet<uint>();

        foreach (DbpfEntry entry in package.Entries)
        {
            Add(entry.Key.InstanceId);
            if (includeGroups)
            {
                Add(entry.Key.GroupId);
            }
        }

        void Add(uint hash)
        {
            if (hash == 0 || !seen.Add(hash))
            {
                return;
            }

            if (names.Files.GetName(hash) is null)
            {
                unknown.Add(hash);
            }
        }

        result.UnnamedCount = unknown.Count;

        int inspected = 0;
        foreach (uint hash in unknown)
        {
            if (inspected >= maxHashes)
            {
                break;
            }

            IReadOnlyList<string> guesses = guesser.Guess(hash, maxResults: maxResultsPerHash);
            if (guesses.Count > 0)
            {
                result.Recovered[hash] = guesses[0];
            }

            inspected++;
            progress?.Invoke(inspected, Math.Min(unknown.Count, maxHashes));
        }

        result.InspectedCount = inspected;
        return result;
    }

    /// <summary>Applies recovered names to a registry (so later operations can use them).</summary>
    public static void Apply(NameRecoveryResult result, NameRegistrySet names)
    {
        foreach ((uint hash, string name) in result.Recovered)
        {
            names.AddFileName(hash, name);
        }
    }

    /// <summary>Serialises recovered names in the registry text format.</summary>
    public static string Serialize(NameRecoveryResult result)
    {
        var builder = new StringBuilder();
        builder.Append("# Names recovered by SporeMaster 3\n");

        foreach ((uint hash, string name) in result.Recovered.OrderBy(p => p.Key))
        {
            bool alias = name.EndsWith('~') || Hashing.Fnv.Hash(name) != hash;
            if (alias)
            {
                builder.Append(name).Append('\t').Append("0x")
                    .Append(hash.ToString("x8", CultureInfo.InvariantCulture)).Append('\n');
            }
            else
            {
                builder.Append(name).Append('\n');
            }
        }

        return builder.ToString();
    }

    public static void Save(NameRecoveryResult result, string path) =>
        File.WriteAllText(path, Serialize(result), new UTF8Encoding(false));
}
