using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Gait;

namespace SporeMaster.Tests;

public static class GaitTests
{
    private const uint GaitType = 0x25DF0112;

    [SporeTest("GAIT binary round-trip is byte-exact")]
    public static void BinaryRoundTrip()
    {
        int tested = 0;

        foreach (string path in TestData.FinalPackages)
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(path);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != GaitType)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                GaitFile gait = GaitFile.Read(new MemoryStream(data));

                using var stream = new MemoryStream();
                gait.Write(stream);
                Assert.True(
                    data.AsSpan().SequenceEqual(stream.ToArray()),
                    $"GAIT binary round-trip changed bytes for {entry.Key} in {Path.GetFileName(path)}");

                tested++;
                if (tested >= 20)
                {
                    break;
                }
            }

            if (tested >= 20)
            {
                break;
            }
        }

        if (tested == 0)
        {
            throw new AssertionException("No GAIT resources found.");
        }
    }

    [SporeTest("GAIT text round-trip is stable")]
    public static void TextRoundTrip()
    {
        int tested = 0;

        foreach (string path in TestData.FinalPackages)
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(path);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != GaitType)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                GaitFile gait = GaitFile.Read(new MemoryStream(data));

                // The text form uses SporeModder's 7-decimal float formatting, so it is equivalent
                // (and stable) rather than byte-exact.
                string text1 = gait.ToScript();
                GaitFile reparsed = GaitFile.Parse(text1);
                string text2 = reparsed.ToScript();

                Assert.Equal(text1, text2);

                tested++;
                if (tested >= 20)
                {
                    break;
                }
            }

            if (tested >= 20)
            {
                break;
            }
        }

        if (tested == 0)
        {
            throw new AssertionException("No GAIT resources found.");
        }
    }
}
