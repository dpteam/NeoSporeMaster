using SporeMaster.Core.Hashing;
using SporeMaster.Core.Names;

namespace SporeMaster.Tests;

public static class GuesserValidationTests
{
    [SporeTest("guesser validation measures recovery of real names")]
    public static void GuesserValidationSynthetic()
    {
        // Build a ground-truth key map with a name the guesser can recover from fragments.
        const string name = "zzqleftzzqright";
        uint instance = Fnv.Hash(name);
        KeyMap keyMap = KeyMap.Parse(new[] { $"{{0x0b1b104 0x1 0x{instance:X8}}} \"g\\{name}.prop\"" });

        var guesser = new HashGuesser(new[] { "zzqleft" }, new[] { "zzqright" });
        GuesserValidationResult result = GuesserValidation.Validate(guesser, keyMap, maxEntries: 10);

        Assert.Equal(1, result.Attempted);
        Assert.Equal(1, result.Found);
        Assert.Equal(1, result.Correct);
        Assert.Equal(1, result.Top1Correct);
        Assert.Equal(0, result.Misses.Count);
    }

    [SporeTest("guesser validation instance-name extraction")]
    public static void InstanceNameExtraction()
    {
        Assert.Equal("foo", GuesserValidation.InstanceName("a\\b\\foo.prop"));
        Assert.Equal("foo", GuesserValidation.InstanceName("a/b/foo.rw4"));
        Assert.Equal("bar", GuesserValidation.InstanceName("bar"));
    }

    [SporeTest("guesser validation runs against the real key map")]
    public static void GuesserValidationReal()
    {
        string? keyMapPath = TestData.Combine("refs", "SporeUnpackedData", "SporeBeta2008", "Data", "KeyMap.txt");
        if (keyMapPath is null || !File.Exists(keyMapPath))
        {
            throw new AssertionException("KeyMap.txt is not available under refs/SporeUnpackedData.");
        }

        // Small dictionary so the test stays fast; the point is that validation runs end to end.
        var guesser = new HashGuesser(new[] { "creat", "spor" }, new[] { "ure", "e" });
        KeyMap keyMap = KeyMap.ParseFile(keyMapPath);

        GuesserValidationResult result = GuesserValidation.Validate(guesser, keyMap, maxEntries: 25);

        Assert.Equal(25, result.Attempted);
        Assert.True(result.Found >= 0);
        Assert.True(result.Correct <= result.Found);
        Assert.True(result.Top1Correct <= result.Correct);
    }
}
