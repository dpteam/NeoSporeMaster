using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Names;
using SporeMaster.Core.Presentation;

namespace SporeMaster.Tests;

public static class SearchFilterTests
{
    [SporeTest("name filter supports substring, wildcards and regex")]
    public static void NameFilterForms()
    {
        NameFilter substring = NameFilter.Compile("creature");
        Assert.True(substring.Matches("creature_parts~/ce_mouth.prop"));
        Assert.False(substring.Matches("animations~/civ.trigger"));

        NameFilter wildcard = NameFilter.Compile("*.rw4");
        Assert.True(wildcard.Matches("editor_rigblocks~/ce_limb_assembly_01.rw4"));
        Assert.False(wildcard.Matches("editor_rigblocks~/ce_limb_assembly_01.gmsh"));

        NameFilter question = NameFilter.Compile("ce_???_01.prop");
        Assert.True(question.Matches("ce_hat_01.prop"));
        Assert.False(question.Matches("ce_hat_02.prop"));

        NameFilter regex = NameFilter.Compile(@"re:^ce_.*\.rw4$");
        Assert.True(regex.IsRegularExpression);
        Assert.True(regex.Matches("ce_limb_assembly_01.rw4"));
        Assert.False(regex.Matches("foo_ce_bar.rw4"));

        // An invalid regex must not throw; it falls back to a literal.
        NameFilter bad = NameFilter.Compile("re:[unclosed");
        Assert.False(bad.IsEmpty);

        Assert.True(NameFilter.Compile("").IsEmpty);
        Assert.True(NameFilter.Compile(null).IsEmpty);
    }

    [SporeTest("name filter is applied per field in resource matching")]
    public static void ResourceItemFiltering()
    {
        string? packagePath = TestData.FinalPackages.FirstOrDefault(p => p.EndsWith("BP2_Data.package"));
        if (packagePath is null)
        {
            throw new AssertionException("BP2_Data.package is not available.");
        }

        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
        var items = package.Entries.Select(e => new ResourceItem(e, names)).ToList();

        // Wildcard on path: only .rw4 resources.
        List<ResourceItem> rw4 = ExplorerModel.Filter(items, "*.rw4");
        Assert.True(rw4.Count > 0, "expected some .rw4 resources");
        Assert.True(rw4.All(i => i.Path.EndsWith(".rw4", StringComparison.OrdinalIgnoreCase)));

        // Regex.
        List<ResourceItem> regex = ExplorerModel.Filter(items, @"re:\.prop$");
        Assert.True(regex.All(i => i.Path.EndsWith(".prop", StringComparison.OrdinalIgnoreCase)));
    }

    [SporeTest("content query parses text and hex markers")]
    public static void ContentQueryParsing()
    {
        ContentQuery text = ContentQuery.Parse("ce_*.rw4 text:gRob");
        Assert.Equal("ce_*.rw4", text.NamePart);
        Assert.Equal(ContentKind.Text, text.Kind);
        Assert.True(text.Matches(System.Text.Encoding.ASCII.GetBytes("...GROB...")));
        Assert.False(text.Matches(System.Text.Encoding.ASCII.GetBytes("nothing here")));

        ContentQuery hex = ContentQuery.Parse("hex:0B0A2C");
        Assert.Equal(ContentKind.Hex, hex.Kind);
        Assert.True(hex.Matches(new byte[] { 0xFF, 0x0B, 0x0A, 0x2C, 0x00 }));
        Assert.False(hex.Matches(new byte[] { 0x0B, 0x0A }));

        ContentQuery quoted = ContentQuery.Parse("text:\"hello world\"");
        Assert.Equal(ContentKind.Text, quoted.Kind);
        Assert.True(quoted.Matches(System.Text.Encoding.ASCII.GetBytes("say Hello World!")));

        Assert.False(ContentQuery.Parse("plain").HasContent);
    }

    [SporeTest("content search finds resources by their bytes")]
    public static void ContentSearch()
    {
        string? packagePath = TestData.FinalPackages.FirstOrDefault(p => p.EndsWith("Spore_Game.package"));
        if (packagePath is null)
        {
            throw new AssertionException("Spore_Game.package is not available.");
        }

        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
        var items = package.Entries.Select(e => new ResourceItem(e, names)).ToList();

        // Triggers are plain text; search for a known substring across the whole package.
        ContentQuery content = ContentQuery.Parse("text:property");
        ResourceContentSearch.Result result = ResourceContentSearch.Search(
            items,
            item => package.ReadEntry(item.Entry),
            NameFilter.Compile(content.NamePart),
            content,
            maxScan: 2000,
            maxResults: 50);

        Assert.True(result.Matches.Count > 0, "expected at least one text match");
        Assert.True(result.Scanned > 0, "expected some resources to be scanned");
    }
}
