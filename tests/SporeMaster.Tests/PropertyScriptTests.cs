using SporeMaster.Core.ArgScript;
using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Names;
using SporeMaster.Core.Properties;

namespace SporeMaster.Tests;

public static class PropertyScriptTests
{
    [SporeTest("prop_t text round-trips scalars and vectors")]
    public static void TextRoundTrip()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();

        var list = new PropertyList();
        var flag = new BoolProperty();
        flag.Add(true);
        list.Add(0x00B2CCCA, flag);

        var number = new IntegerProperty(PropertyType.Int32);
        number.Add(42);
        list.Add(0x00B2CCCB, number);

        var vec = new Vector3Property();
        vec.Add(new[] { 1f, 2.5f, -3f });
        vec.IsArray = false;
        list.Add(0x12345678, vec);

        string text = PropertyScript.Write(list, names);
        PropertyList parsed = PropertyScript.Parse(text, names);

        Assert.Equal(3, parsed.Count);
        Assert.True(parsed.Get(0x00B2CCCA) is BoolProperty { IsArray: false });
        Assert.True(((IntegerProperty)parsed.Get(0x00B2CCCB)!).Values[0] == 42);
        var reparsedVec = (Vector3Property)parsed.Get(0x12345678)!;
        Assert.Equal(1f, reparsedVec.GetComponents(0)[0]);
        Assert.Equal(2.5f, reparsedVec.GetComponents(0)[1]);
        Assert.Equal(-3f, reparsedVec.GetComponents(0)[2]);
    }

    [SporeTest("prop_t text round-trips arrays of text")]
    public static void TextArrayRoundTrip()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        uint localeName = names.GetFileHash("my_locale");

        var list = new PropertyList();
        var texts = new TextProperty();
        texts.Add(0, 0, "hello world");
        texts.Add(localeName, 1, "second");
        list.Add(0x00B2CCCA, texts);

        string text = PropertyScript.Write(list, names);
        PropertyList parsed = PropertyScript.Parse(text, names);

        var reparsed = (TextProperty)parsed.Get(0x00B2CCCA)!;
        Assert.Equal(2, reparsed.Count);
        Assert.Equal("hello world", reparsed.Values[0].Text);
        Assert.Equal(0u, reparsed.Values[0].TableId);
        Assert.Equal(localeName, reparsed.Values[1].TableId);
        Assert.Equal(1u, reparsed.Values[1].InstanceId);
        Assert.Equal("second", reparsed.Values[1].Text);
    }

    [SporeTest("prop_t text round-trips real property files")]
    public static void RealFilesRoundTrip()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        int tested = 0;
        int mismatches = 0;
        string? first = null;

        foreach (string packagePath in TestData.FinalPackages.Take(3))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (!ResourceTypeIds.IsProperty(entry.Key.TypeId))
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                if (data.Length < 4)
                {
                    continue;
                }

                PropertyList original = PropertyList.Read(new MemoryStream(data));
                string text1 = PropertyScript.Write(original, names);

                PropertyList reparsed = PropertyScript.Parse(text1, names);
                string text2 = PropertyScript.Write(reparsed, names);

                if (text1 != text2)
                {
                    mismatches++;
                    first ??= $"{Path.GetFileName(packagePath)} {entry.Key}";
                }

                tested++;
                if (tested >= 300)
                {
                    break;
                }
            }

            if (tested >= 300)
            {
                break;
            }
        }

        if (tested == 0)
        {
            throw new AssertionException("No property resources found.");
        }

        Assert.True(mismatches == 0, $"{mismatches} of {tested} prop_t round-trip(s) changed the text. First: {first}");
    }
}
