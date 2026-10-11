using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Scn;

namespace SporeMaster.Tests;

public static class ScnTests
{
    private const uint ScnType = 0x047B8300;

    [SporeTest("scn reads and writes back byte-exactly")]
    public static void RoundTrip()
    {
        int tested = 0;

        foreach (string packagePath in TestData.FinalPackages.Concat(TestData.BetaPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != ScnType)
                {
                    continue;
                }

                byte[] original = package.ReadEntry(entry);
                ScnFile scn = ScnFile.Read(new MemoryStream(original));

                using var buffer = new MemoryStream();
                scn.Write(buffer);
                Assert.True(
                    original.AsSpan().SequenceEqual(buffer.ToArray()),
                    $"scn {entry.Key} did not round-trip byte-exactly");

                Assert.True(scn.Markers.Count > 0, "scn has markers");

                tested++;
                if (tested >= 20)
                {
                    return;
                }
            }
        }

        if (tested == 0)
        {
            Console.WriteLine("No scn resources found; skipping.");
        }
    }

    [SporeTest("scn text dump lists markers")]
    public static void TextDump()
    {
        var scn = new ScnFile { Version = 3, DataSize = 4 };
        scn.Markers.Add(new ScnMarker { Type = 1, Id = 2, DefinitionId = 3, OffsetX = 1.5f, Data = new byte[4] });

        string text = scn.ToText();
        Assert.Contains(text, "version 3");
        Assert.Contains(text, "marker [0] type 1 id 2 definition 3");
        Assert.Contains(text, "1.5");
    }
}
