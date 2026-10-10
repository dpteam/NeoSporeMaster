using SporeMaster.Core.Dbpf;

namespace SporeMaster.Core.Diff;

/// <summary>Which kind of change a resource represents when comparing two archives.</summary>
public enum DiffKind
{
    /// <summary>Present only in the second archive.</summary>
    Added,

    /// <summary>Present only in the first archive.</summary>
    Removed,

    /// <summary>Present in both, but with different payloads.</summary>
    Changed,
}

/// <summary>One difference between two archives.</summary>
public sealed class DiffEntry
{
    public required ResourceKey Key { get; init; }

    public required DiffKind Kind { get; init; }

    /// <summary>Payload size in the first archive, or null when the resource was added.</summary>
    public uint? SizeA { get; init; }

    /// <summary>Payload size in the second archive, or null when the resource was removed.</summary>
    public uint? SizeB { get; init; }
}

/// <summary>The result of comparing two archives.</summary>
public sealed class ArchiveDiffResult
{
    public int CountA { get; init; }

    public int CountB { get; init; }

    /// <summary>Resources present only in the second archive.</summary>
    public List<DiffEntry> Added { get; } = new();

    /// <summary>Resources present only in the first archive.</summary>
    public List<DiffEntry> Removed { get; } = new();

    /// <summary>Resources present in both, with different payloads.</summary>
    public List<DiffEntry> Changed { get; } = new();

    /// <summary>Resources present in both with identical payloads.</summary>
    public int Identical { get; set; }

    /// <summary>True when the two archives hold exactly the same resources with the same payloads.</summary>
    public bool IsIdentical => Added.Count == 0 && Removed.Count == 0 && Changed.Count == 0;

    public IEnumerable<DiffEntry> AllDifferences =>
        Removed.Concat(Added).Concat(Changed);
}

/// <summary>
/// Compares two <c>.package</c> archives by resource key and (optionally) by payload. Losing the
/// dependence on ordering, compression and padding makes this useful for mods and for checking a
/// repack against the original.
/// </summary>
public static class ArchiveDiff
{
    /// <summary>
    /// Compares <paramref name="a"/> against <paramref name="b"/>. When <paramref name="compareContent"/>
    /// is true, resources present in both are read and compared byte for byte (decompressed); otherwise
    /// only the key sets are compared.
    /// </summary>
    public static ArchiveDiffResult Compare(
        DatabasePackedFile a,
        DatabasePackedFile b,
        bool compareContent = true,
        Action<int, int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        var result = new ArchiveDiffResult { CountA = a.Entries.Count, CountB = b.Entries.Count };

        var mapA = new Dictionary<ResourceKey, DbpfEntry>(a.Entries.Count);
        foreach (DbpfEntry entry in a.Entries)
        {
            mapA[entry.Key] = entry;
        }

        var mapB = new Dictionary<ResourceKey, DbpfEntry>(b.Entries.Count);
        foreach (DbpfEntry entry in b.Entries)
        {
            mapB[entry.Key] = entry;
        }

        foreach (DbpfEntry entry in a.Entries)
        {
            if (!mapB.ContainsKey(entry.Key))
            {
                result.Removed.Add(new DiffEntry
                {
                    Key = entry.Key,
                    Kind = DiffKind.Removed,
                    SizeA = entry.MemSize,
                });
            }
        }

        foreach (DbpfEntry entry in b.Entries)
        {
            if (!mapA.ContainsKey(entry.Key))
            {
                result.Added.Add(new DiffEntry
                {
                    Key = entry.Key,
                    Kind = DiffKind.Added,
                    SizeB = entry.MemSize,
                });
            }
        }

        int shared = mapA.Count - result.Removed.Count;
        int done = 0;
        foreach (DbpfEntry entryA in a.Entries)
        {
            if (!mapB.TryGetValue(entryA.Key, out DbpfEntry? entryB))
            {
                continue;
            }

            bool differs = compareContent && !SamePayload(a, entryA, b, entryB);
            if (differs)
            {
                result.Changed.Add(new DiffEntry
                {
                    Key = entryA.Key,
                    Kind = DiffKind.Changed,
                    SizeA = entryA.MemSize,
                    SizeB = entryB.MemSize,
                });
            }
            else
            {
                result.Identical++;
            }

            done++;
            progress?.Invoke(done, shared);
        }

        return result;
    }

    private static bool SamePayload(DatabasePackedFile a, DbpfEntry entryA, DatabasePackedFile b, DbpfEntry entryB)
    {
        // Fast path: equal sizes and (for uncompressed) equal payloads.
        if (entryA.MemSize != 0 && entryB.MemSize != 0 && entryA.MemSize != entryB.MemSize)
        {
            return false;
        }

        byte[] dataA = a.ReadEntry(entryA);
        byte[] dataB = b.ReadEntry(entryB);
        return dataA.AsSpan().SequenceEqual(dataB);
    }
}
