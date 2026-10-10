using SporeMaster.Core.Dbpf;

namespace SporeMaster.Core.Diff;

/// <summary>
/// Verifies that an archive survives a read → write → re-read cycle unchanged. This is the automated
/// form of the "repack validation" check: it repacks every resource with our writer and compares the
/// result with the original by key and content, independent of ordering, compression and padding.
/// </summary>
public static class ArchiveRoundTrip
{
    /// <summary>
    /// Repacks <paramref name="source"/> with the given options and compares it with the original.
    /// When <paramref name="keepPath"/> is null the repacked archive is written to a temporary file
    /// and deleted afterwards.
    /// </summary>
    public static ArchiveDiffResult Run(
        DatabasePackedFile source,
        DbpfWriteOptions options,
        string? keepPath = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        var items = new List<DbpfWriteItem>(source.Entries.Count);
        foreach (DbpfEntry entry in source.Entries)
        {
            items.Add(new DbpfWriteItem(entry.Key, source.ReadEntry(entry)));
        }

        string path = keepPath ?? Path.Combine(Path.GetTempPath(), $"sporemaster-roundtrip-{Guid.NewGuid():N}.package");
        bool temporary = keepPath is null;

        try
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
            {
                DatabasePackedFile.Write(stream, items, options);
            }

            using DatabasePackedFile repacked = DatabasePackedFile.Open(path);
            return ArchiveDiff.Compare(source, repacked);
        }
        finally
        {
            if (temporary)
            {
                try
                {
                    File.Delete(path);
                }
                catch
                {
                    // best effort
                }
            }
        }
    }
}
