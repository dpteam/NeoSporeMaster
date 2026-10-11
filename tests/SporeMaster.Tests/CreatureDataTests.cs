using SporeMaster.Core.CreatureData;
using SporeMaster.Core.Dbpf;

namespace SporeMaster.Tests;

public static class CreatureDataTests
{
    private const uint CreatureDataType = 0x0F43029A;

    [SporeTest("creaturedata reads and writes back byte-exactly")]
    public static void RoundTrip()
    {
        int tested = 0;

        foreach (string packagePath in TestData.FinalPackages.Concat(TestData.BetaPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != CreatureDataType)
                {
                    continue;
                }

                byte[] original = package.ReadEntry(entry);
                CreatureDataFile data = CreatureDataFile.Read(new MemoryStream(original));

                using var buffer = new MemoryStream();
                data.Write(buffer);
                Assert.True(
                    original.AsSpan().SequenceEqual(buffer.ToArray()),
                    $"creaturedata {entry.Key} did not round-trip byte-exactly");

                Assert.True(data.Version is 9 or 10, "creaturedata version is 9 or 10");

                tested++;
                if (tested >= 10)
                {
                    return;
                }
            }
        }

        if (tested == 0)
        {
            Console.WriteLine("No creaturedata resources found; skipping.");
        }
    }

    [SporeTest("creaturedata text dump lists capabilities and morphs")]
    public static void TextDump()
    {
        var data = new CreatureDataFile { Version = 9, Field110 = 0x12345678 };
        data.Capabilities.Add(new CreatureCapability { Id = "bite", Number = 3 });
        data.Morphs.Add(new CreatureMorph { Id = 0xABCDEF01, Value1 = 1.5f, Value2 = 2f });

        string text = data.ToText();
        Assert.Contains(text, "version 9");
        Assert.Contains(text, "bite: 3");
        Assert.Contains(text, "0xABCDEF01: 1.5  2");
    }

    [SporeTest("creaturedata rejects a wrong header")]
    public static void BadHeader()
    {
        var buffer = new MemoryStream();
        buffer.Write(BitConverter.GetBytes(0x11223344));
        buffer.Write(BitConverter.GetBytes(9));
        buffer.Position = 0;

        bool threw = false;
        try
        {
            CreatureDataFile.Read(buffer);
        }
        catch (InvalidDataException)
        {
            threw = true;
        }

        Assert.True(threw, "a wrong header should be rejected");
    }
}
