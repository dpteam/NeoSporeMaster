using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Otdb;

namespace SporeMaster.Tests;

public static class SummaryTests
{
    private const uint SummaryType = 0x02D5C9AF;

    [SporeTest("summary reads and writes back byte-exactly")]
    public static void RoundTrip()
    {
        int tested = 0;

        foreach (string packagePath in TestData.FinalPackages.Concat(TestData.BetaPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != SummaryType)
                {
                    continue;
                }

                byte[] original = package.ReadEntry(entry);
                SummaryFile summary = SummaryFile.Read(new MemoryStream(original));

                using var buffer = new MemoryStream();
                summary.Write(buffer);
                Assert.True(
                    original.AsSpan().SequenceEqual(buffer.ToArray()),
                    $"summary {entry.Key} did not round-trip byte-exactly");

                Assert.True(summary.Parameters.Count > 0, "summary has parameters");

                tested++;
                if (tested >= 20)
                {
                    return;
                }
            }
        }

        if (tested == 0)
        {
            Console.WriteLine("No summary resources found; skipping.");
        }
    }

    [SporeTest("summary text dump names known parameters")]
    public static void TextDump()
    {
        var summary = new SummaryFile();
        summary.Parameters.Add(new SummaryParameter { Id = 0x02F05C60, IsFloat = true, FloatValue = 2f }); // baseGear
        summary.Parameters.Add(new SummaryParameter { Id = 0x02DD90AF, IntValue = 0x2B978C46 });          // type
        summary.AmTags.Add(new SummaryAmTag { TagGroup = 0x04EAB460, TagId = 0x0720AA55, Value = 70f });  // GroupCount

        string text = summary.ToText();
        Assert.Contains(text, "parameter float baseGear 2");
        Assert.Contains(text, "parameter int type 731352134");
        Assert.Contains(text, "amTag GroupCount 0x0720AA55 70");
    }

    [SporeTest("summary rejects an unsupported version")]
    public static void BadVersion()
    {
        byte[] data = { 0, 0, 0, 5, 0, 0, 0, 0 };
        bool threw = false;
        try
        {
            SummaryFile.Read(new MemoryStream(data));
        }
        catch (InvalidDataException)
        {
            threw = true;
        }

        Assert.True(threw, "version 5 should be rejected");
    }

    [SporeTest("pollen metadata reads and writes back byte-exactly")]
    public static void PollenRoundTrip()
    {
        int tested = 0;

        foreach (string packagePath in TestData.FinalPackages.Concat(TestData.BetaPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != 0x030BDEE3)
                {
                    continue;
                }

                byte[] original = package.ReadEntry(entry);
                PollenMetadata metadata = PollenMetadata.Read(new MemoryStream(original));

                using var buffer = new MemoryStream();
                metadata.Write(buffer);
                Assert.True(
                    original.AsSpan().SequenceEqual(buffer.ToArray()),
                    $"pollen metadata {entry.Key} did not round-trip byte-exactly");

                tested++;
                if (tested >= 20)
                {
                    return;
                }
            }
        }

        if (tested == 0)
        {
            Console.WriteLine("No pollen metadata resources found; skipping.");
        }
    }
}
