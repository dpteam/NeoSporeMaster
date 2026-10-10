using SporeMaster.Core.Cell;
using SporeMaster.Core.Dbpf;

namespace SporeMaster.Tests;

public static class LookTableTests
{
    private const uint LookTableType = 0x8C042499;

    [SporeTest("look table binary round-trip is byte-exact")]
    public static void BinaryRoundTrip()
    {
        int tested = 0;

        foreach (string path in TestData.BetaPackages.Concat(TestData.FinalPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(path);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != LookTableType)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                CellLookTableFile file = CellLookTableFile.Read(new MemoryStream(data));

                using var stream = new MemoryStream();
                file.Write(stream);
                Assert.True(
                    data.AsSpan().SequenceEqual(stream.ToArray()),
                    $"look table binary round-trip changed bytes for {entry.Key}");

                tested++;
            }
        }

        if (tested == 0)
        {
            throw new AssertionException("No look table resources found.");
        }
    }

    [SporeTest("look table text round-trip preserves the binary")]
    public static void TextRoundTrip()
    {
        int tested = 0;

        foreach (string path in TestData.BetaPackages.Concat(TestData.FinalPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(path);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != LookTableType)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                CellLookTableFile file = CellLookTableFile.Read(new MemoryStream(data));

                string text = file.ToScript();
                CellLookTableFile reparsed = CellLookTableFile.Parse(text);
                using var stream = new MemoryStream();
                reparsed.Write(stream);

                Assert.True(
                    data.AsSpan().SequenceEqual(stream.ToArray()),
                    $"look table text round-trip changed bytes for {entry.Key}");

                tested++;
            }
        }

        if (tested == 0)
        {
            throw new AssertionException("No look table resources found.");
        }
    }
}
