using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Diff;

namespace SporeMaster.Tests;

public static class ArchiveDiffTests
{
    [SporeTest("archive diff finds added, removed and changed resources")]
    public static void CompareSynthetic()
    {
        string temp = TestData.CreateTempDirectory();
        try
        {
            var k1 = new ResourceKey(0x00B1B104, 1, 1);
            var k2 = new ResourceKey(0x00B1B104, 1, 2);
            var k3 = new ResourceKey(0x00B1B104, 1, 3);
            var k4 = new ResourceKey(0x00B1B104, 1, 4);

            string a = Path.Combine(temp, "a.package");
            string b = Path.Combine(temp, "b.package");

            Write(a,
                new DbpfWriteItem(k1, "one"u8.ToArray()),
                new DbpfWriteItem(k2, "two"u8.ToArray()),
                new DbpfWriteItem(k3, "three"u8.ToArray()));

            Write(b,
                new DbpfWriteItem(k2, "two"u8.ToArray()),
                new DbpfWriteItem(k3, "three-DIFFERENT"u8.ToArray()),
                new DbpfWriteItem(k4, "four"u8.ToArray()));

            using DatabasePackedFile packageA = DatabasePackedFile.Open(a);
            using DatabasePackedFile packageB = DatabasePackedFile.Open(b);

            ArchiveDiffResult result = ArchiveDiff.Compare(packageA, packageB);

            Assert.Equal(3, result.CountA);
            Assert.Equal(3, result.CountB);
            Assert.Equal(1, result.Removed.Count);
            Assert.Equal(k1, result.Removed[0].Key);
            Assert.Equal(1, result.Added.Count);
            Assert.Equal(k4, result.Added[0].Key);
            Assert.Equal(1, result.Changed.Count);
            Assert.Equal(k3, result.Changed[0].Key);
            Assert.Equal(1, result.Identical);
            Assert.False(result.IsIdentical);

            // Key-only comparison treats the changed k3 as identical.
            ArchiveDiffResult keyOnly = ArchiveDiff.Compare(packageA, packageB, compareContent: false);
            Assert.Equal(0, keyOnly.Changed.Count);
            Assert.Equal(2, keyOnly.Identical);
            Assert.False(keyOnly.IsIdentical);
        }
        finally
        {
            TestData.DeleteDirectory(temp);
        }
    }

    [SporeTest("archive diff reports identical archives")]
    public static void CompareIdentical()
    {
        string temp = TestData.CreateTempDirectory();
        try
        {
            string a = Path.Combine(temp, "a.package");
            var items = new[]
            {
                new DbpfWriteItem(new ResourceKey(0x00B1B104, 1, 1), "alpha"u8.ToArray()),
                new DbpfWriteItem(new ResourceKey(0x2F4E681B, 1, 2), "beta"u8.ToArray()),
            };

            Write(a, items);
            Write(Path.Combine(temp, "b.package"), items);

            using DatabasePackedFile packageA = DatabasePackedFile.Open(a);
            using DatabasePackedFile packageB = DatabasePackedFile.Open(Path.Combine(temp, "b.package"));

            ArchiveDiffResult result = ArchiveDiff.Compare(packageA, packageB);

            Assert.True(result.IsIdentical, "archives with the same resources must compare identical");
            Assert.Equal(2, result.Identical);
        }
        finally
        {
            TestData.DeleteDirectory(temp);
        }
    }

    private static void Write(string path, params DbpfWriteItem[] items)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
        DatabasePackedFile.Write(stream, items, new DbpfWriteOptions { Compress = true });
    }
}
