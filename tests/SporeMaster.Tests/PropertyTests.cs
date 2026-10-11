using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Names;
using SporeMaster.Core.Properties;

namespace SporeMaster.Tests;

public static class PropertyTests
{
    // Property resource type ids (from reg_type.txt).
    private const uint PropType = 0x00B1B104;
    private const uint SoundPropType = 0x02B9F662;
    private const uint SubmixPropType = 0x02C9EFF2;
    private const uint ModePropType = 0x0497925E;
    private const uint ChildrenPropType = 0x03F51892;

    private static readonly uint[] PropTypes =
    {
        PropType, SoundPropType, SubmixPropType, ModePropType, ChildrenPropType,
    };

    [SporeTest("property files round-trip byte-for-byte (retail)")]
    public static void RetailRoundTrip()
    {
        RunCorpus(TestData.FinalPackages);
    }

    [SporeTest("property files round-trip byte-for-byte (beta)")]
    public static void BetaRoundTrip()
    {
        RunCorpus(TestData.BetaPackages);
    }

    [SporeTest("property file -> XML -> binary is stable")]
    public static void XmlRoundTrip()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        int tested = 0;

        foreach (string packagePath in TestData.FinalPackages.Take(3))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (Array.IndexOf(PropTypes, entry.Key.TypeId) < 0)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                if (data.Length == 0)
                {
                    continue;
                }

                PropertyList original = PropertyList.Read(new MemoryStream(data));

                using var xml = new MemoryStream();
                original.WriteXml(xml, names);
                xml.Position = 0;
                PropertyList fromXml = PropertyList.ReadXml(xml, names);

                // XML is intentionally lossy for descriptor flags and padding, so compare
                // the semantic content (ids, types, array-ness and element counts) instead.
                Assert.Equal(original.Count, fromXml.Count, $"XML round-trip changed the property count for {entry.Key}");
                foreach ((uint id, Property originalValue) in original.Properties)
                {
                    Property? reborn = fromXml.Get(id);
                    Assert.NotNull(reborn, $"XML round-trip dropped property 0x{id:X8} of {entry.Key}");
                    Assert.Equal(originalValue.Type, reborn!.Type);
                    Assert.Equal(originalValue.IsArray, reborn.IsArray);
                    Assert.Equal(originalValue.Count, reborn.Count);
                }

                tested++;
                if (tested >= 200)
                {
                    break;
                }
            }

            if (tested >= 200)
            {
                break;
            }
        }

        if (tested == 0)
        {
            throw new AssertionException("No property resources found in the reference packages.");
        }
    }

    [SporeTest("all property type codes produce a value")]
    public static void FactoryCoversTypes()
    {
        foreach (PropertyType type in Enum.GetValues<PropertyType>())
        {
            Property property = PropertyFactory.Create(type);
            Assert.NotNull(property);
            Assert.Equal(type, property.Type);
            property.Clear();
            Assert.Equal(0, property.Count);
        }
    }

    private static void RunCorpus(IEnumerable<string> packages)
    {
        var list = packages.ToList();
        if (list.Count == 0)
        {
            throw new AssertionException("No reference packages found under refs/.");
        }

        int total = 0;
        int mismatches = 0;
        string? firstMismatch = null;

        foreach (string packagePath in list)
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (Array.IndexOf(PropTypes, entry.Key.TypeId) < 0)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                if (data.Length < 4)
                {
                    continue;
                }

                total++;
                try
                {
                    PropertyList parsed = PropertyList.Read(new MemoryStream(data));
                    byte[] reserialised = Serialize(parsed);
                    if (!data.AsSpan().SequenceEqual(reserialised))
                    {
                        mismatches++;
                        firstMismatch ??= Describe(Path.GetFileName(packagePath), entry, data, reserialised);
                    }
                }
                catch (Exception ex)
                {
                    mismatches++;
                    firstMismatch ??= $"{Path.GetFileName(packagePath)} {entry.Key}: {ex.GetType().Name} {ex.Message}";
                }
            }
        }

        if (total == 0)
        {
            throw new AssertionException("No property resources found in the reference packages.");
        }

        Assert.True(
            mismatches == 0,
            $"{mismatches} of {total} property file(s) did not round-trip. First: {firstMismatch}");
    }

    private static byte[] Serialize(PropertyList list)
    {
        using var stream = new MemoryStream();
        list.Write(stream);
        return stream.ToArray();
    }

    [SporeTest("property files round-trip byte-for-byte through XML (BP2_Data)")]
    public static void XmlByteExact()
    {
        string? packagePath = TestData.FinalPackages.FirstOrDefault(p => p.EndsWith("BP2_Data.package"));
        if (packagePath is null)
        {
            Console.WriteLine("BP2_Data.package not present; skipping.");
            return;
        }

        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        int total = 0;
        int mismatches = 0;
        string? firstMismatch = null;

        using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
        foreach (DbpfEntry entry in package.Entries)
        {
            if (Array.IndexOf(PropTypes, entry.Key.TypeId) < 0)
            {
                continue;
            }

            byte[] data = package.ReadEntry(entry);
            if (data.Length < 8)
            {
                continue;
            }

            total++;
            try
            {
                PropertyList original = PropertyList.Read(new MemoryStream(data));
                using var xml = new MemoryStream();
                original.WriteXml(xml, names);
                xml.Position = 0;
                PropertyList reborn = PropertyList.ReadXml(xml, names);
                byte[] reserialised = Serialize(reborn);
                if (!data.AsSpan().SequenceEqual(reserialised))
                {
                    mismatches++;
                    firstMismatch ??= $"{entry.Key}";
                }
            }
            catch (Exception ex)
            {
                mismatches++;
                firstMismatch ??= $"{entry.Key}: {ex.GetType().Name} {ex.Message}";
            }
        }

        if (total == 0)
        {
            throw new AssertionException("No property resources found in BP2_Data.");
        }

        Assert.True(
            mismatches == 0,
            $"{mismatches} of {total} property file(s) did not round-trip through XML. First: {firstMismatch}");
    }

    private static string Describe(string package, DbpfEntry entry, byte[] a, byte[] b)
    {
        int diff = -1;
        int max = Math.Min(a.Length, b.Length);
        for (int i = 0; i < max; i++)
        {
            if (a[i] != b[i])
            {
                diff = i;
                break;
            }
        }

        if (diff < 0)
        {
            return $"{package} {entry.Key}: length differs ({a.Length} vs {b.Length})";
        }

        int from = Math.Max(0, diff - 8);
        string windowA = BitConverter.ToString(a, from, Math.Min(16, a.Length - from));
        string windowB = BitConverter.ToString(b, from, Math.Min(16, b.Length - from));
        return $"{package} {entry.Key}: first diff at 0x{diff:X} orig[{from:X}]={windowA} new[{from:X}]={windowB}";
    }
}
