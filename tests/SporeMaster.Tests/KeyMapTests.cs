using SporeMaster.Core.Hashing;
using SporeMaster.Core.Names;

namespace SporeMaster.Tests;

public static class KeyMapTests
{
    [SporeTest("key map parses keys and paths")]
    public static void ParseKeysAndPaths()
    {
        string[] lines =
        {
            "// header comment",
            "{0x01c135da 0x40606000 0x01dcaf31} \"Arenas\\Editors\\ce_editor_background_sporepedia.gmsh\"",
            "{0x2f4e681b 0x40606000 0x01dcaf31} \"Arenas\\Editors\\ce_editor_background_sporepedia.rdx9\"",
        };

        KeyMap map = KeyMap.Parse(lines);
        Assert.Equal(2, map.Entries.Count);

        KeyMapEntry first = map.Entries[0];
        Assert.Equal(0x01C135DAu, first.TypeId);
        Assert.Equal(0x40606000u, first.GroupId);
        Assert.Equal(0x01DCAF31u, first.InstanceId);
        Assert.Contains(first.Path, "ce_editor_background_sporepedia.gmsh");

        Assert.Equal("ce_editor_background_sporepedia", map.InstanceNames[0x01DCAF31]);
        Assert.Equal("Editors", map.GroupNames[0x40606000]);
        Assert.Equal("gmsh", map.TypeNames[0x01C135DA]);
        Assert.Equal("rdx9", map.TypeNames[0x2F4E681B]);
    }

    [SporeTest("key map counts hashes that match their names")]
    public static void VerifiedNames()
    {
        // "prop" hashes to its registered type id (from reg_type.txt convention) is not guaranteed,
        // but the instance name below is a plain FNV name, so it must verify.
        string instanceName = "mything";
        uint instance = Fnv.Hash(instanceName);
        string[] lines = { $"{{0x1 0x2 0x{instance:X8}}} \"group\\{instanceName}.prop\"" };

        KeyMap map = KeyMap.Parse(lines);
        Assert.Equal(1, map.VerifiedInstanceNames);
        Assert.Equal(0, map.UnverifiedEntries);
        Assert.Equal(instanceName, map.InstanceNames[instance]);
    }

    [SporeTest("key map names apply to a registry and serialise")]
    public static void ApplyAndSerialize()
    {
        uint instance = Fnv.Hash("cellthing");
        KeyMap map = KeyMap.Parse(new[] { $"{{0x1 0x2 0x{instance:X8}}} \"g\\cellthing.rw4\"" });

        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        Assert.False(names.IsFileNameKnown(instance));
        map.ApplyTo(names);
        Assert.Equal("cellthing", names.Files.GetName(instance));

        string registry = map.BuildFileNameRegistry();
        Assert.Contains(registry, "cellthing");
    }

    [SporeTest("key map resolves an exact path for a resource key")]
    public static void GetPath()
    {
        KeyMap map = KeyMap.Parse(new[] { "{0x2f4e681b 0x40606000 0x01dcaf31} \"Arenas\\Editors\\thing.rdx9\"" });

        Assert.Equal(@"Arenas\Editors\thing.rdx9", map.GetPath(0x2F4E681B, 0x40606000, 0x01DCAF31));
        Assert.True(map.GetPath(0x1, 0x2, 0x3) is null);
    }

    [SporeTest("key map exposes a type to extension map")]
    public static void ExtensionMap()
    {
        KeyMap map = KeyMap.Parse(new[]
        {
            "{0x024a0e52 0x0 0x1} \"Config\\AppProperties.txt\"",
            "{0x024a0e52 0x0 0x2} \"Config\\Options.txt\"",
            "{0x2f4e681b 0x0 0x3} \"Arenas\\thing.rdx9\"",
        });

        Assert.Equal("txt", map.GetExtension(0x024A0E52));
        Assert.Equal("rdx9", map.GetExtension(0x2F4E681B));
        Assert.True(map.GetExtension(0xDEADBEEF) is null);

        string table = map.BuildExtensionTable();
        Assert.Contains(table, "0x024a0e52\ttxt");
        Assert.Contains(table, "0x2f4e681b\trdx9");
    }

    [SporeTest("applying a key map updates the name registry")]
    public static void ApplyKeyMapToRegistry()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        KeyMap map = KeyMap.Parse(new[] { "{0x01a527db 0x021407ee 0x07c31016} \"Audio\\physical_chorus_30sec.snr\"" });

        map.ApplyTo(names);
        Assert.Equal("physical_chorus_30sec", names.GetFileName(0x07C31016));
    }

    [SporeTest("build a name registry for the keys used by a package")]
    public static void BuildUsedNameRegistry()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        uint group = names.GetFileHash("0x40626000");
        uint instance = names.GetFileHash("0x47356FB7");

        string registry = names.BuildNameRegistry(new[] { (0x00B1B104u, group, instance) });
        Assert.Contains(registry, "creature_parts");
        Assert.Contains(registry, "ce_movementTech_Brute_02");
    }

    [SporeTest("real key map resolves many resources in a retail package")]
    public static void RealKeyMapResolvesPackage()
    {
        string? keyMapPath = TestData.Combine("refs", "SporeUnpackedData", "SporeBeta2008", "Data", "KeyMap.txt");
        string? packagePath = TestData.FinalPackages.FirstOrDefault(p => p.EndsWith("Spore_Game.package"));
        if (keyMapPath is null || packagePath is null || !File.Exists(keyMapPath))
        {
            throw new AssertionException("KeyMap.txt or Spore_Game.package is not available.");
        }

        KeyMap map = KeyMap.ParseFile(keyMapPath);
        using var package = Core.Dbpf.DatabasePackedFile.Open(packagePath);

        int resolved = 0;
        foreach (Core.Dbpf.DbpfEntry entry in package.Entries)
        {
            if (map.GetPath(entry.Key.TypeId, entry.Key.GroupId, entry.Key.InstanceId) is not null)
            {
                resolved++;
            }
        }

        // Shared content across versions means a large fraction should resolve.
        Assert.True(resolved > 1000, $"expected many resolved keys, got {resolved}");
    }

    [SporeTest("real KeyMap.txt parses when available")]
    public static void RealKeyMap()
    {
        string? path = TestData.Combine("refs", "SporeUnpackedData", "SporeBeta2008", "Data", "KeyMap.txt");
        if (path is null || !File.Exists(path))
        {
            throw new AssertionException("KeyMap.txt is not available under refs/SporeUnpackedData.");
        }

        KeyMap map = KeyMap.ParseFile(path);
        Assert.True(map.Entries.Count > 1000, $"expected many entries, got {map.Entries.Count}");

        // The vast majority of instance names really hash to their ids.
        Assert.True(
            map.VerifiedInstanceNames > map.UnverifiedEntries * 10,
            $"verified {map.VerifiedInstanceNames}, unverified {map.UnverifiedEntries}");
    }
}
