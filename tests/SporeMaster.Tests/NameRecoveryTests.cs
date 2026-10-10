using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Hashing;
using SporeMaster.Core.Names;

namespace SporeMaster.Tests;

public static class NameRecoveryTests
{
    // A name assembled from fragments that (with overwhelming probability) is not in the registry.
    private const string LeftFragment = "zzqleft";
    private const string RightFragment = "zzqright";
    private const string RecoverableName = "zzqleftzzqright";

    [SporeTest("name recovery finds unknown hashes in a package")]
    public static void RecoversFromPackage()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        uint target = Fnv.Hash(RecoverableName);
        Assert.False(names.IsFileNameKnown(target), "precondition: the name must be unknown");

        using DatabasePackedFile package = BuildPackage(target);
        var guesser = new HashGuesser(new[] { LeftFragment }, new[] { RightFragment });

        NameRecoveryResult result = NameRecovery.Recover(package, guesser, names, maxHashes: 10);

        Assert.Equal(1, result.UnnamedCount);
        Assert.True(result.Recovered.ContainsKey(target), "the hash should be recovered");
        Assert.Equal(RecoverableName, result.Recovered[target]);
    }

    [SporeTest("recovered names survive a save/load round trip")]
    public static void PersistsNames()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        uint target = Fnv.Hash(RecoverableName);

        using DatabasePackedFile package = BuildPackage(target);
        var guesser = new HashGuesser(new[] { LeftFragment }, new[] { RightFragment });
        NameRecoveryResult result = NameRecovery.Recover(package, guesser, names, maxHashes: 10);

        string path = Path.Combine(TestData.CreateTempDirectory(), "names.txt");
        try
        {
            NameRecovery.Save(result, path);

            var reloaded = NameRegistrySet.LoadEmbedded();
            reloaded.LoadProjectNames(path);
            Assert.Equal(RecoverableName, reloaded.Files.GetName(target));
            Assert.True(reloaded.IsFileNameKnown(target));
        }
        finally
        {
            TestData.DeleteDirectory(Path.GetDirectoryName(path)!);
        }
    }

    [SporeTest("applying recovered names updates the registry")]
    public static void ApplyUpdatesRegistry()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        uint target = Fnv.Hash(RecoverableName);

        using DatabasePackedFile package = BuildPackage(target);
        var guesser = new HashGuesser(new[] { LeftFragment }, new[] { RightFragment });
        NameRecoveryResult result = NameRecovery.Recover(package, guesser, names, maxHashes: 10);

        Assert.False(names.IsFileNameKnown(target));
        NameRecovery.Apply(result, names);
        Assert.True(names.IsFileNameKnown(target));
        Assert.Equal(RecoverableName, names.Files.GetName(target));
    }

    [SporeTest("recovery respects the unknown-hash limit")]
    public static void RespectsLimit()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        var guesser = new HashGuesser(Array.Empty<string>(), Array.Empty<string>());

        // A package with a couple of arbitrary hashes.
        var items = new List<DbpfWriteItem>
        {
            new(new ResourceKey(0x00B1B104, 0x12345678, 0x11111111), new byte[] { 1 }),
            new(new ResourceKey(0x00B1B104, 0x12345678, 0x22222222), new byte[] { 2 }),
        };

        using var stream = new MemoryStream();
        DatabasePackedFile.Write(stream, items, new DbpfWriteOptions());
        stream.Position = 0;
        using DatabasePackedFile package = DatabasePackedFile.Read(stream);

        NameRecoveryResult result = NameRecovery.Recover(package, guesser, names, maxHashes: 1);
        Assert.True(result.UnnamedCount >= 1);
        Assert.Equal(1, result.InspectedCount);
        Assert.Equal(0, result.RecoveredCount); // empty dictionary -> nothing found
    }

    private static DatabasePackedFile BuildPackage(uint instanceHash)
    {
        var items = new List<DbpfWriteItem>
        {
            new(new ResourceKey(0x00B1B104, 0x12345678, instanceHash), new byte[] { 1, 2, 3 }),
        };

        var stream = new MemoryStream();
        DatabasePackedFile.Write(stream, items, new DbpfWriteOptions());
        stream.Position = 0;
        return DatabasePackedFile.Read(stream);
    }
}
