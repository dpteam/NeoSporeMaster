using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Names;
using SporeMaster.Core.Tlsa;

namespace SporeMaster.Tests;

public static class TlsaTests
{
    private const uint TlsaType = 0x4AEB6BC6;

    [SporeTest("TLSA binary round-trip is byte-exact")]
    public static void BinaryRoundTrip()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        int tested = 0;

        foreach (string path in TestData.FinalPackages)
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(path);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != TlsaType)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                TlsaUnit unit = TlsaUnit.Read(new MemoryStream(data));

                using var stream = new MemoryStream();
                unit.Write(stream);
                Assert.True(
                    data.AsSpan().SequenceEqual(stream.ToArray()),
                    $"TLSA binary round-trip changed bytes for {entry.Key} in {Path.GetFileName(path)}");

                Assert.True(unit.Groups.Count > 0, "should have animation groups");
                tested++;
            }
        }

        if (tested == 0)
        {
            throw new AssertionException("No TLSA resources found.");
        }
    }

    [SporeTest("TLSA text round-trip preserves the structure")]
    public static void TextRoundTrip()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        int tested = 0;

        foreach (string path in TestData.FinalPackages)
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(path);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != TlsaType)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                TlsaUnit unit = TlsaUnit.Read(new MemoryStream(data));

                // The text form normalises probabilities/defaults (like SporeModder), so compare
                // structure rather than raw bytes: same groups, choices and animation references.
                string text = unit.ToScript(names);
                TlsaUnit reparsed = TlsaUnit.Parse(text, names);

                Assert.Equal(unit.Version, reparsed.Version);
                Assert.Equal(unit.Groups.Count, reparsed.Groups.Count);

                for (int g = 0; g < unit.Groups.Count; g++)
                {
                    TlsaGroup original = unit.Groups[g];
                    TlsaGroup copy = reparsed.Groups[g];
                    Assert.Equal(original.Id, copy.Id);
                    Assert.Equal(original.Name, copy.Name);
                    Assert.Equal(original.EndMode, copy.EndMode);
                    Assert.Equal(original.Choices.Count, copy.Choices.Count);

                    for (int c = 0; c < original.Choices.Count; c++)
                    {
                        Assert.Equal(original.Choices[c].Animations.Count, copy.Choices[c].Animations.Count);
                        for (int a = 0; a < original.Choices[c].Animations.Count; a++)
                        {
                            Assert.Equal(original.Choices[c].Animations[a].Id, copy.Choices[c].Animations[a].Id);
                            Assert.Equal(original.Choices[c].Animations[a].Description, copy.Choices[c].Animations[a].Description);
                        }
                    }
                }

                tested++;
            }
        }

        if (tested == 0)
        {
            throw new AssertionException("No TLSA resources found.");
        }
    }

    [SporeTest("TLSA instance name derives from description")]
    public static void InstanceName()
    {
        Assert.Equal("foo", TlsaGroup.InstanceFromDescription("path/to/foo"));
        Assert.Equal("bar", TlsaGroup.InstanceFromDescription("bar"));
    }
}
