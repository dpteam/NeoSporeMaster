using SporeMaster.Core.Cell;
using SporeMaster.Core.Dbpf;

namespace SporeMaster.Tests;

public static class CellWorldTests
{
    private const uint WorldType = 0x9B8E862F;

    [SporeTest("cell world reads and writes back byte-exactly")]
    public static void RoundTrip()
    {
        int tested = 0;

        foreach (string packagePath in TestData.BetaPackages.Concat(TestData.FinalPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != WorldType)
                {
                    continue;
                }

                byte[] original = package.ReadEntry(entry);
                CellWorldFile world = CellWorldFile.Read(new MemoryStream(original));

                using var buffer = new MemoryStream();
                world.Write(buffer);
                Assert.True(
                    original.AsSpan().SequenceEqual(buffer.ToArray()),
                    $"world {entry.Key} did not round-trip byte-exactly");

                tested++;
                if (tested >= 20)
                {
                    return;
                }
            }
        }

        if (tested == 0)
        {
            Console.WriteLine("No cell world resources found; skipping.");
        }
    }

    [SporeTest("cell world text dump lists entries")]
    public static void TextDump()
    {
        var world = new CellWorldFile();
        world.PopulateEntries.Add(new CellWorldPopulateEntry { PopulateId = 0x11223344, StartTile = 1, PlayerSize = -1 });
        world.AdvectEntries.Add(new CellWorldAdvectEntry { AdvectId = 0x55667788, Field0 = 10, Strength = 3.5f, Period = 1f });

        string text = world.ToText();
        Assert.Contains(text, "populateCount 1");
        Assert.Contains(text, "0x11223344");
        Assert.Contains(text, "advectCount 1");
        Assert.Contains(text, "0x55667788");
    }
}
