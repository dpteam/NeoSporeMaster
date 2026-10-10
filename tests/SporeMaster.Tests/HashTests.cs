using SporeMaster.Core.Hashing;
using SporeMaster.Core.Names;

namespace SporeMaster.Tests;

public static class HashTests
{
    [SporeTest("fnv matches canonical FNV-1 vectors")]
    public static void FnvKnownValues()
    {
        // Canonical 32-bit FNV-1 (offset 0x811C9DC5, prime 0x01000193) test vectors.
        Assert.Equal(0x811C9DC5u, Fnv.Hash(string.Empty));
        Assert.Equal(0x050C5D7Eu, Fnv.Hash("a"));
        Assert.Equal(0x31F0B262u, Fnv.Hash("foobar"));

        // Spore's naming hash is FNV, but type/property IDs are EA-assigned constants that do
        // not equal the FNV of their (conventional) names. The registries bridge the two.
        Assert.Equal(0x74DA5446u, Fnv.Hash("prop"));
        Assert.Equal(0x00B1B104u, NameRegistrySet.LoadEmbedded().GetTypeHash("prop"));
    }

    [SporeTest("fnv is case-insensitive")]
    public static void FnvCaseInsensitive()
    {
        Assert.Equal(Fnv.Hash("Creature"), Fnv.Hash("CREATURE"));
        Assert.Equal(Fnv.Hash("Spore_Game"), Fnv.Hash("spore_game"));
    }

    [SporeTest("literal hash parsing")]
    public static void LiteralParsing()
    {
        Assert.True(Fnv.TryParseLiteral("#2F4E681B", out uint hash));
        Assert.Equal(0x2F4E681Bu, hash);
        Assert.True(Fnv.TryParseLiteral("0x2f4e681b", out hash));
        Assert.Equal(0x2F4E681Bu, hash);
        Assert.True(Fnv.TryParseLiteral("12345", out hash));
        Assert.Equal(12345u, hash);
        Assert.False(Fnv.TryParseLiteral("not-a-number", out _));
    }

    [SporeTest("embedded registries load and resolve names")]
    public static void RegistryResolvesNames()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        Assert.Equal(0x00B1B104u, names.GetTypeHash("prop"));
        Assert.True(names.Types.Count > 0, "type registry should not be empty");
        Assert.True(names.Files.Count > 1000, "file registry should be large");
        Assert.Equal("prop", names.GetTypeName(0x00B1B104));
    }

    [SporeTest("relative path round-trips through the registry")]
    public static void RelativePathRoundTrip()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        uint type = names.GetTypeHash("prop");
        uint group = names.GetFileHash("CreatureGame");
        uint instance = names.GetFileHash("SomeInstance");

        string path = names.GetRelativePath(type, group, instance);

        // SomeInstance is not registered, so it resolves to hex; hashing that hex must be a no-op-name.
        Assert.True(names.TryParseRelativePath("CreatureGame/rw4_something.rw4", out uint t, out uint g, out uint i));
        Assert.Equal(names.GetTypeHash("rw4"), t);
        Assert.Equal(names.GetFileHash("CreatureGame"), g);
    }
}
