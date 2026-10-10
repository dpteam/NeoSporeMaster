using SporeMaster.Core.Names;

namespace SporeMaster.Tests;

public static class DdfListTests
{
    [SporeTest("DDFList parses folders, comments and includes")]
    public static void ParseWithIncludes()
    {
        string temp = TestData.CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(temp, "Core_DDFList.txt"),
                "// core stuff\nArenas\nConfig\nArenas/textures/Effects\n");

            File.WriteAllText(Path.Combine(temp, "Spore_DDFList.txt"),
                "#include Core_DDFList.txt\nUI\nMovies\n");

            DdfList list = DdfList.ParseFile(Path.Combine(temp, "Spore_DDFList.txt"));

            Assert.Equal(1, list.IncludeCount);
            Assert.True(list.Folders.Count >= 5, $"expected >= 5 folders, got {list.Folders.Count}");
            Assert.True(list.Contains("Config"), "Config is listed");
            Assert.True(list.Contains("Config\\AppProperties.txt"), "a file directly in Config");
            Assert.True(list.Contains("Arenas\\textures\\Effects\\thing.tga"), "a nested file");
            Assert.True(list.Contains("Movies\\intro.avi"), "a file from the including list");
            Assert.False(list.Contains("Notes\\x.txt"), "unlisted folder");
        }
        finally
        {
            TestData.DeleteDirectory(temp);
        }
    }

    [SporeTest("real DDFList parses when available")]
    public static void RealDdfList()
    {
        string? path = TestData.Combine("refs", "SporeUnpackedData", "SporeBeta2008", "Data", "Spore_DDFList.txt");
        if (path is null || !File.Exists(path))
        {
            Console.WriteLine("Spore_DDFList.txt not present; skipping.");
            return;
        }

        DdfList list = DdfList.ParseFile(path);
        Assert.True(list.Folders.Count > 100, $"expected many folders, got {list.Folders.Count}");
        Assert.True(list.Contains("Config"), "Config should be part of the Spore build");
    }

    [SporeTest("DDFList resolves sub-folder includes written against the data root")]
    public static void SubfolderIncludes()
    {
        string temp = TestData.CreateTempDirectory();
        try
        {
            string sub = Path.Combine(temp, "DDFLists");
            Directory.CreateDirectory(sub);
            File.WriteAllText(Path.Combine(sub, "Core_DDFList.txt"), "Arenas\nConfig\n");
            File.WriteAllText(
                Path.Combine(sub, "Spore_DDFList.txt"),
                "// master\n#include DDFLists/Core_DDFList.txt\nUI\n");

            DdfList list = DdfList.ParseFile(Path.Combine(sub, "Spore_DDFList.txt"));

            Assert.Equal(0, list.MissingIncludes.Count);
            Assert.Equal(1, list.IncludeCount);
            Assert.True(list.Contains("Config"), "include resolved via an ancestor (data root) folder");
            Assert.True(list.Contains("UI"), "a folder from the including file");
        }
        finally
        {
            TestData.DeleteDirectory(temp);
        }
    }

    [SporeTest("real Wii DDFList (sub-folder) parses fully")]
    public static void RealWiiDdfList()
    {
        string? path = TestData.Combine(
            "refs", "SporeUnpackedData", "SporeHeroWiiPCLeftOvers", "Data", "DDFLists", "Spore_DDFList.txt");
        if (path is null || !File.Exists(path))
        {
            Console.WriteLine("Wii DDFList not present; skipping.");
            return;
        }

        DdfList list = DdfList.ParseFile(path);
        Assert.Equal(0, list.MissingIncludes.Count);
        Assert.True(list.Folders.Count > 100, $"expected many folders, got {list.Folders.Count}");
    }
}
