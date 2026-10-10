using SporeMaster.Core.Hashing;
using SporeMaster.Core.Names;

namespace SporeMaster.Tests;

public static class HashGuesserTests
{
    [SporeTest("hash guesser recovers a name from two fragments")]
    public static void RecoversFromFragments()
    {
        var guesser = new HashGuesser(
            leftFragments: new[] { "creat", "spor", "abc" },
            rightFragments: new[] { "ure", "e", "game" });

        uint target = Fnv.Hash("creature");
        IReadOnlyList<string> results = guesser.Guess(target);

        Assert.True(results.Contains("creature"), $"expected 'creature' in [{string.Join(", ", results)}]");
    }

    [SporeTest("hash guesser honours prefix, suffix and contains")]
    public static void HonoursHints()
    {
        var guesser = new HashGuesser(
            leftFragments: new[] { "sub", "sup" },
            rightFragments: new[] { "war", "sea" });

        // name = prefix("my") + L("sub") + contains("_training_") + R("war") + suffix("car")
        uint target = Fnv.Hash("mysub_training_warcar");
        IReadOnlyList<string> results = guesser.Guess(target, prefix: "my", suffix: "car", contains: "_training_");

        Assert.True(
            results.Contains("mysub_training_warcar"),
            $"expected 'mysub_training_warcar' in [{string.Join(", ", results)}]");
    }

    [SporeTest("hash guesser port recovers a name from the real dictionaries")]
    public static void RealDictionaries()
    {
        // Prefer the dictionaries loaded from the data/ folder; fall back to the legacy build.
        HashGuesser? guesser = GuessDictionary.TryLoad(null, maxPerList: 200_000);
        string? dictDir = TestData.Combine("data");
        if (guesser is null && dictDir is not null && Directory.Exists(dictDir))
        {
            guesser = GuessDictionary.TryLoad(dictDir, maxPerList: 200_000);
        }

        if (guesser is null || guesser.LeftCount == 0 || guesser.RightCount == 0)
        {
            throw new AssertionException("Guess dictionaries could not be loaded (expected under data/).");
        }

        string? leftPath = GuessDictionary.Find(null, GuessDictionary.LeftFileName);
        string? rightPath = GuessDictionary.Find(null, GuessDictionary.RightFileName);
        Assert.NotNull(leftPath);
        Assert.NotNull(rightPath);

        // Build a name from a real left fragment and a real right fragment, hash it, and find it.
        string left = File.ReadLines(leftPath!).First();
        string right = File.ReadLines(rightPath!).First();
        string name = left + right;

        IReadOnlyList<string> results = guesser.Guess(Fnv.Hash(name));
        Assert.True(
            results.Contains(name),
            $"expected '{name}' to be recovered from the real dictionaries");
    }

    [SporeTest("hash guesser dictionary search path is populated")]
    public static void SearchPathsAreNonEmpty()
    {
        var paths = GuessDictionary.SearchPaths(null).ToList();
        Assert.True(paths.Count > 0);
        Assert.True(paths.All(p => !string.IsNullOrEmpty(p)));
    }
}
