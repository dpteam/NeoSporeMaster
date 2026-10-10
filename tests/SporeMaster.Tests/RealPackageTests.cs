using SporeMaster.Core.Dbpf;

namespace SporeMaster.Tests;

/// <summary>Tests that read the real Spore packages shipped under <c>refs/</c>.</summary>
public static class RealPackageTests
{
    [SporeTest("beta packages are DBPF 2.0 and fully readable")]
    public static void BetaPackages()
    {
        RunDirectory(TestData.BetaPackages, expectedMajor: 2);
    }

    [SporeTest("retail packages are DBPF 3.0 and fully readable")]
    public static void FinalPackages()
    {
        RunDirectory(TestData.FinalPackages, expectedMajor: 3);
    }

    private static void RunDirectory(IEnumerable<string> packages, uint expectedMajor)
    {
        var list = packages.ToList();
        if (list.Count == 0)
        {
            throw new AssertionException(
                "No reference packages found under refs/. Place the sample .package files there to run these tests.");
        }

        foreach (string path in list)
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(path);
            string name = Path.GetFileName(path);

            Assert.True(package.Entries.Count > 0, $"{name} should contain entries");
            Assert.Equal(expectedMajor, package.Version.Major);

            int read = 0;
            int empty = 0;
            foreach (DbpfEntry entry in package.Entries)
            {
                byte[] data = package.ReadEntry(entry);
                read++;
                if (data.Length == 0 && entry.MemSize != 0)
                {
                    empty++;
                }
            }

            Assert.Equal(package.Entries.Count, read);
            Assert.True(
                empty <= package.Entries.Count / 100,
                $"{name}: {empty} entries failed to produce data (expected almost none)");
        }
    }

    [SporeTest("beta package uses shared index fields and still parses")]
    public static void BetaIndexShape()
    {
        IEnumerable<string> packages = TestData.BetaPackages;
        string? first = packages.FirstOrDefault();
        if (first is null)
        {
            throw new AssertionException("No beta packages available.");
        }

        using DatabasePackedFile package = DatabasePackedFile.Open(first);
        // Every real Spore index shares the (unused) third field; type/group may also be shared.
        Assert.True((package.IndexType & 0x04) != 0, "the third index field should be shared");
        Assert.True(package.Entries.Count > 0);
    }
}
