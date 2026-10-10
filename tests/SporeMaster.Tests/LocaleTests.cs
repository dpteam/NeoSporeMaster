using System.Text;
using SporeMaster.Core.Hashing;
using SporeMaster.Core.Locale;
using SporeMaster.Core.Names;
using SporeMaster.Core.Properties;

namespace SporeMaster.Tests;

public static class LocaleTests
{
    [SporeTest("locale CSV export/import round-trips entries")]
    public static void CsvRoundTrip()
    {
        var table = new LocaleTable();
        table.Add(0x00000001, "Hello, world");
        table.Add(0x00000002, "Quote \"x\" test");
        table.Add(0x00000003, "Plain");

        string csv = LocaleCsv.Export(table);
        Assert.Contains(csv, "id,text");
        Assert.Contains(csv, "\"Hello, world\"");      // comma triggers quoting
        Assert.Contains(csv, "\"Quote \"\"x\"\" test\""); // quotes are doubled

        LocaleTable back = LocaleCsv.Import(csv);
        Assert.Equal(3, back.Count);
        Assert.Equal("Hello, world", back.GetText(0x00000001));
        Assert.Equal("Quote \"x\" test", back.GetText(0x00000002));
        Assert.Equal("Plain", back.GetText(0x00000003));

        // TSV variant.
        var tsv = new LocaleTable();
        tsv.Add(0x0000000A, "a\tb");
        string tsvText = LocaleCsv.Export(tsv, '\t');
        LocaleTable backTsv = LocaleCsv.Import(tsvText, '\t');
        Assert.Equal("a\tb", backTsv.GetText(0x0000000A));
        Assert.Equal('\t', LocaleCsv.DetectSeparator(tsvText));
    }

    [SporeTest("locale table round-trips text exactly")]
    public static void RoundTripText()
    {
        const string input =
            "# a comment\n" +
            "\n" +
            "0x00000001 Hello world\n" +
            "0x00000002 Weapons of mass destruction are cool (if you are on the right end, I guess)\n";

        LocaleTable table = LocaleTable.Parse(input);
        Assert.Equal(2, table.Count);
        Assert.Equal("Hello world", table.GetText(1));
        Assert.Equal("Weapons of mass destruction are cool (if you are on the right end, I guess)", table.GetText(2));
        Assert.Equal(input, table.Serialize());
    }

    [SporeTest("generated locale table uses 0x-id text lines")]
    public static void GenerateText()
    {
        var table = new LocaleTable();
        table.Add(1, "Alpha");
        table.Add(2, "Beta");

        string text = table.Serialize();
        Assert.Contains(text, "0x00000001 Alpha");
        Assert.Contains(text, "0x00000002 Beta");
    }

    [SporeTest("locale id parsing accepts 0x and bare hex ids")]
    public static void IdParsing()
    {
        LocaleTable table = LocaleTable.Parse("0x0000000A ten\n00000014 twenty\n");
        Assert.Equal(2, table.Count);
        Assert.Equal("ten", table.GetText(0x0A));
        Assert.Equal("twenty", table.GetText(0x14));
    }

    [SporeTest("auto-locale name convention")]
    public static void AutoLocaleName()
    {
        Assert.Equal("auto_mygroup_myprop", AutoLocale.GetName("mygroup", "myprop"));
        Assert.True(AutoLocale.IsAutoLocaleName("auto_mygroup_myprop"));
        Assert.True(AutoLocale.IsAutoLocaleName("something_auto"));
        Assert.False(AutoLocale.IsAutoLocaleName("cell_properties"));
    }

    [SporeTest("auto-locale generation assigns ids and collects text")]
    public static void AutoLocaleGeneration()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();

        var list = new PropertyList();
        var text = new TextProperty();
        text.Add(0, 0, "First placeholder");
        text.Add(0, 0, "Second placeholder");
        list.Add(0x00B2CCCA, text);

        string localeName = AutoLocale.GetName("mygroup", "myprop");
        LocaleTable? table = AutoLocale.Generate(list, localeName, names);

        Assert.NotNull(table);
        uint tableId = Fnv.Hash(localeName);
        Assert.Equal(tableId, text.Values[0].TableId);
        Assert.Equal(1u, text.Values[0].InstanceId);
        Assert.Equal(tableId, text.Values[1].TableId);
        Assert.Equal(2u, text.Values[1].InstanceId);
        Assert.Equal("First placeholder", table!.GetText(1));
        Assert.Equal("Second placeholder", table.GetText(2));

        // Nothing to do the second time.
        Assert.True(AutoLocale.Generate(list, localeName, names) is null);
    }

    [SporeTest("stripping auto-locale references clears the ids")]
    public static void StripReferences()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();

        var list = new PropertyList();
        var text = new TextProperty();
        text.Add(0, 0, "Placeholder");
        list.Add(0x00B2CCCA, text);

        string localeName = AutoLocale.GetName("mygroup", "myprop");
        AutoLocale.Generate(list, localeName, names);
        Assert.True(text.Values[0].TableId != 0);

        int stripped = list.StripAutoLocaleReferences(Fnv.Hash(localeName));
        Assert.Equal(1, stripped);
        Assert.Equal(0u, text.Values[0].TableId);
        Assert.Equal(0u, text.Values[0].InstanceId);
    }

    [SporeTest("property with placeholder text survives a pack/unpack cycle")]
    public static void PropertyLocaleCycle()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();

        // Build a property with a placeholder text, as if loaded from a .prop.xml.
        var list = new PropertyList();
        var text = new TextProperty();
        text.Add(0, 0, "Creature name placeholder");
        list.Add(0x00B2CCCA, text);

        string localeName = AutoLocale.GetName("mygroup", "myprop");
        LocaleTable? generated = AutoLocale.Generate(list, localeName, names);
        Assert.NotNull(generated);

        // Serialise the property and the locale, then read both back.
        using var propStream = new MemoryStream();
        list.Write(propStream);
        propStream.Position = 0;
        PropertyList reparsed = PropertyList.Read(propStream);

        var reparsedText = (TextProperty)reparsed.Get(0x00B2CCCA)!;
        Assert.Equal(Fnv.Hash(localeName), reparsedText.Values[0].TableId);

        LocaleTable reloaded = LocaleTable.Parse(generated!.Serialize());
        Assert.Equal("Creature name placeholder", reloaded.GetText(reparsedText.Values[0].InstanceId));
    }
}
