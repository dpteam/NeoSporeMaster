using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Effects;

namespace SporeMaster.Tests;

public static class EffectsTests
{
    private const uint EffDirType = 0xEA5118B0;

    [SporeTest("effect directory container round-trips byte-exactly")]
    public static void RoundTrip()
    {
        int tested = 0;

        foreach (string packagePath in TestData.FinalPackages.Concat(TestData.BetaPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != EffDirType)
                {
                    continue;
                }

                byte[] original = package.ReadEntry(entry);
                EffectDirectoryFile effects = EffectDirectoryFile.Read(new MemoryStream(original));

                using var buffer = new MemoryStream();
                effects.Write(buffer);
                Assert.True(
                    original.AsSpan().SequenceEqual(buffer.ToArray()),
                    $"effdir {entry.Key} did not round-trip byte-exactly");

                Assert.Equal(4, effects.Version);
                Assert.True(effects.Sections.Count > 0, "effdir has sections");

                tested++;
                if (tested >= 10)
                {
                    return;
                }
            }
        }

        if (tested == 0)
        {
            Console.WriteLine("No effdir resources found; skipping.");
        }
    }

    [SporeTest("effect directory rejects a bad version")]
    public static void BadVersion()
    {
        var data = new byte[] { 0x00, 0x05 };
        bool threw = false;
        try
        {
            EffectDirectoryFile.Read(new MemoryStream(data));
        }
        catch (InvalidDataException)
        {
            threw = true;
        }

        Assert.True(threw, "version 5 should be rejected");
    }
}
