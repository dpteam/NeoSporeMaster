using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Names;
using SporeMaster.Core.Pctp;

namespace SporeMaster.Tests;

public static class PctpTests
{
    private const uint PctpType = 0x7C19AA7A;

    [SporeTest("real PCTP files round-trip binary and text")]
    public static void RealRoundTrip()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        int tested = 0;

        foreach (string packagePath in TestData.FinalPackages)
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != PctpType)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                PctpUnit original;
                try
                {
                    original = PctpUnit.Read(new MemoryStream(data));
                }
                catch (Exception ex)
                {
                    throw new AssertionException($"{Path.GetFileName(packagePath)} {entry.Key} len={data.Length}: {ex.Message}");
                }

                Assert.True(original.Version is 3 or 4, "version must be 3 or 4");
                Assert.True(original.Capabilities.Count > 0, "should have capabilities");

                // Binary -> text -> binary -> text must be text-stable.
                string text1 = original.ToScript(names);

                PctpUnit reparsed = PctpUnit.Parse(text1, names);
                using var binary = new MemoryStream();
                reparsed.Write(binary);
                binary.Position = 0;
                PctpUnit fromBinary = PctpUnit.Read(binary);
                string text2 = fromBinary.ToScript(names);

                Assert.Equal(text1, text2);

                tested++;
            }
        }

        if (tested == 0)
        {
            throw new AssertionException("No PCTP resources found in the reference packages.");
        }
    }

    [SporeTest("PCTP identifier packing round-trips")]
    public static void IdentifierRoundTrip()
    {
        // "HELP" must survive the reversed 4-byte identifier encoding.
        var unit = new PctpUnit { Version = 4, Priority = 1.5f };
        unit.Capabilities.Add(new PctpCapability("myCapability", "HELP"));
        unit.Remaps.Add(new PctpRemap(unchecked((int)SporeMaster.Core.Hashing.Fnv.Hash("myCapability")), "HELP", 0));

        using var stream = new MemoryStream();
        unit.Write(stream);
        stream.Position = 0;
        PctpUnit read = PctpUnit.Read(stream);

        Assert.Equal("HELP", read.Capabilities[0].Identifier);
        Assert.Equal("myCapability", read.Capabilities[0].Name);
        Assert.Equal(1.5f, read.Priority);
    }
}
