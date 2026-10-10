using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Diff;

namespace SporeMaster.Tests;

public static class RawRepackTests
{
    [SporeTest("raw key paths round-trip through format and parse")]
    public static void RawKeyPathRoundTrip()
    {
        var key = new ResourceKey(0x00B1B104, 0x40626000, 0x47356FB7);
        string path = RawKeyPath.Format(key);

        Assert.Equal("_raw/00B1B104/40626000/47356FB7.bin", path);
        Assert.True(RawKeyPath.TryParse(path, out ResourceKey parsed));
        Assert.Equal(key, parsed);

        // Backslashes and a leading slash are tolerated; other layouts are rejected.
        Assert.True(RawKeyPath.TryParse("_raw\\00B1B104\\40626000\\47356FB7.bin", out _));
        Assert.False(RawKeyPath.TryParse("creature_parts~/thing.prop", out _));
        Assert.False(RawKeyPath.TryParse("_raw/00B1B104/40626000.bin", out _));
    }

    [SporeTest("raw unpack and repack reproduces the same resources")]
    public static void RawUnpackRepackIsLossless()
    {
        string temp = TestData.CreateTempDirectory();
        try
        {
            string original = Path.Combine(temp, "original.package");
            var payloads = new Dictionary<ResourceKey, byte[]>
            {
                [new ResourceKey(0x00B1B104, 1, 1)] = Randomish(1000, 1),
                [new ResourceKey(0x2F4E681B, 2, 2)] = Randomish(5000, 2),
                [new ResourceKey(0x024A0E52, 3, 3)] = "hello text"u8.ToArray(),
                [new ResourceKey(0x2F4E681C, 4, 4)] = Array.Empty<byte>(),
            };

            using (var stream = new FileStream(original, FileMode.Create, FileAccess.ReadWrite))
            {
                DatabasePackedFile.Write(
                    stream,
                    payloads.Select(p => new DbpfWriteItem(p.Key, p.Value)),
                    new DbpfWriteOptions { Compress = true });
            }

            // Simulate unpack --raw then pack: names never enter the picture.
            var repackedItems = new List<DbpfWriteItem>();
            using (DatabasePackedFile package = DatabasePackedFile.Open(original))
            {
                foreach (DbpfEntry entry in package.Entries)
                {
                    string relative = RawKeyPath.Format(entry.Key);
                    Assert.True(RawKeyPath.TryParse(relative, out ResourceKey key), "raw path must parse");
                    Assert.Equal(entry.Key, key);
                    repackedItems.Add(new DbpfWriteItem(key, package.ReadEntry(entry)));
                }
            }

            string repacked = Path.Combine(temp, "repacked.package");
            using (var stream = new FileStream(repacked, FileMode.Create, FileAccess.ReadWrite))
            {
                DatabasePackedFile.Write(stream, repackedItems, new DbpfWriteOptions { Compress = true });
            }

            using DatabasePackedFile a = DatabasePackedFile.Open(original);
            using DatabasePackedFile b = DatabasePackedFile.Open(repacked);
            ArchiveDiffResult diff = ArchiveDiff.Compare(a, b);

            Assert.True(diff.IsIdentical, "raw unpack/repack must be lossless (same keys and content)");
            Assert.Equal(payloads.Count, diff.Identical);
        }
        finally
        {
            TestData.DeleteDirectory(temp);
        }
    }

    [SporeTest("archive round-trip is lossless on a real package")]
    public static void RealRoundTrip()
    {
        string? packagePath = TestData.FinalPackages.FirstOrDefault(p => p.EndsWith("BP2_Data.package"));
        if (packagePath is null)
        {
            Console.WriteLine("BP2_Data.package not available; skipping.");
            return;
        }

        using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
        ArchiveDiffResult result = ArchiveRoundTrip.Run(package, new DbpfWriteOptions { Compress = true });

        Assert.True(result.IsIdentical, "repacking a real package must be lossless");
        Assert.Equal(package.Entries.Count, result.Identical);
    }

    private static byte[] Randomish(int length, int seed)
    {
        var data = new byte[length];
        var random = new Random(seed);
        random.NextBytes(data);
        return data;
    }
}
