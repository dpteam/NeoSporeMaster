using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Localization;

namespace SporeMaster.Tests;

public static class FaultToleranceTests
{
    [SporeTest("non-package input is rejected cleanly")]
    public static void NotAPackage()
    {
        string temp = TestData.CreateTempDirectory();
        string path = Path.Combine(temp, "garbage.package");
        try
        {
            File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
            Assert.Throws<NotAPackageException>(() =>
            {
                using DatabasePackedFile package = DatabasePackedFile.Open(path);
            });
        }
        finally
        {
            TestData.DeleteDirectory(temp);
        }
    }

    [SporeTest("truncated package degrades gracefully")]
    public static void TruncatedPackage()
    {
        string? source = TestData.FinalPackages.FirstOrDefault() ?? TestData.BetaPackages.FirstOrDefault();
        if (source is null)
        {
            throw new AssertionException("No reference packages available.");
        }

        string temp = TestData.CreateTempDirectory();
        string path = Path.Combine(temp, "truncated.package");
        try
        {
            byte[] full = File.ReadAllBytes(source);
            File.WriteAllBytes(path, full.AsSpan(0, Math.Min(200, full.Length)).ToArray());

            using DatabasePackedFile package = DatabasePackedFile.Open(path);
            // Header is valid but the index offset points past the end: no exception, just diagnostics.
            Assert.True(package.Diagnostics.Items.Count > 0, "a truncated package should produce diagnostics");
        }
        finally
        {
            TestData.DeleteDirectory(temp);
        }
    }

    [SporeTest("corrupted index entries do not throw")]
    public static void CorruptedIndex()
    {
        string? source = TestData.FinalPackages.FirstOrDefault();
        if (source is null)
        {
            throw new AssertionException("No reference packages available.");
        }

        string temp = TestData.CreateTempDirectory();
        string path = Path.Combine(temp, "corrupt.package");
        try
        {
            byte[] data = File.ReadAllBytes(source);
            // Clobber the region just past the header (data area) with 0xFF to break payloads.
            for (int i = DbpfConstants.HeaderSize; i < Math.Min(data.Length, DbpfConstants.HeaderSize + 4096); i += 7)
            {
                data[i] = 0xFF;
            }

            File.WriteAllBytes(path, data);

            using DatabasePackedFile package = DatabasePackedFile.Open(path);
            // Reading every entry must never raise, even with corrupt payloads.
            foreach (DbpfEntry entry in package.Entries)
            {
                _ = package.ReadEntry(entry);
            }
        }
        finally
        {
            TestData.DeleteDirectory(temp);
        }
    }

    [SporeTest("localizer returns keys and resolves English strings")]
    public static void LocalizerFallback()
    {
        Localizer loc = Localizer.Load("en");
        Assert.Equal("SporeMaster 3", loc["app.name"]);
        Assert.Equal("missing.key.name", loc["missing.key.name"]);
        Assert.True(Localizer.AvailableLanguages.Contains("en"));

        Localizer unknown = Localizer.Load("zz");
        Assert.Equal("SporeMaster 3", unknown["app.name"]);
    }
}
