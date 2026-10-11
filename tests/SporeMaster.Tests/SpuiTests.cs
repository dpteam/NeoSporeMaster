using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Spui;

namespace SporeMaster.Tests;

public static class SpuiTests
{
    private const uint SpuiType = 0x250FE9A2;

    [SporeTest("spui container reads and writes back byte-exactly")]
    public static void RoundTrip()
    {
        int tested = 0;

        foreach (string packagePath in TestData.FinalPackages.Concat(TestData.BetaPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != SpuiType)
                {
                    continue;
                }

                byte[] original = package.ReadEntry(entry);
                SpuiFile spui = SpuiFile.Read(new MemoryStream(original));

                using var buffer = new MemoryStream();
                spui.Write(buffer);
                Assert.True(
                    original.AsSpan().SequenceEqual(buffer.ToArray()),
                    $"spui {entry.Key} did not round-trip byte-exactly");

                Assert.True(spui.Version is >= 1 and <= 3, "spui version in range");

                tested++;
                if (tested >= 20)
                {
                    return;
                }
            }
        }

        if (tested == 0)
        {
            Console.WriteLine("No spui resources found; skipping.");
        }
    }

    [SporeTest("spui rejects a wrong magic")]
    public static void BadMagic()
    {
        var data = new byte[16];
        bool threw = false;
        try
        {
            SpuiFile.Read(new MemoryStream(data));
        }
        catch (InvalidDataException)
        {
            threw = true;
        }

        Assert.True(threw, "a wrong magic should be rejected");
    }
}
