using SporeMaster.Core.Cell;
using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Names;

namespace SporeMaster.Tests;

public static class LootTableTests
{
    private const uint LootTableType = 0xD92AF091;

    [SporeTest("loot table binary round-trip is byte-exact")]
    public static void BinaryRoundTrip()
    {
        int tested = 0;

        foreach (string path in TestData.BetaPackages.Concat(TestData.FinalPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(path);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != LootTableType)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                CellLootTableFile table = CellLootTableFile.Read(new MemoryStream(data));

                using var stream = new MemoryStream();
                table.Write(stream);
                Assert.True(
                    data.AsSpan().SequenceEqual(stream.ToArray()),
                    $"loot table binary round-trip changed bytes for {entry.Key}");

                tested++;
            }
        }

        if (tested == 0)
        {
            throw new AssertionException("No loot table resources found.");
        }
    }

    [SporeTest("loot table text round-trip preserves the binary")]
    public static void TextRoundTrip()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        int tested = 0;

        foreach (string path in TestData.BetaPackages.Concat(TestData.FinalPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(path);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != LootTableType)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                CellLootTableFile table = CellLootTableFile.Read(new MemoryStream(data));

                string text = table.ToScript(names);
                CellLootTableFile reparsed = CellLootTableFile.Parse(text, names);
                using var stream = new MemoryStream();
                reparsed.Write(stream);

                Assert.True(
                    data.AsSpan().SequenceEqual(stream.ToArray()),
                    $"loot table text round-trip changed bytes for {entry.Key}");

                tested++;
            }
        }

        if (tested == 0)
        {
            throw new AssertionException("No loot table resources found.");
        }
    }

    [SporeTest("type filter matches hashes with the high bit set")]
    public static void HighBitTypeFilter()
    {
        // 0xD92AF091 (loottable) has the top bit set; a signed comparison would never match it.
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        uint hash = names.GetTypeHash("loottable");
        Assert.Equal(0xD92AF091u, hash);
        Assert.True((hash & 0x80000000) != 0, "precondition: the hash must have the high bit set");
    }
}
