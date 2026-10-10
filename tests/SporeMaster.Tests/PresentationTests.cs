using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Imaging;
using SporeMaster.Core.Names;
using SporeMaster.Core.Presentation;
using SporeMaster.Core.Properties;

namespace SporeMaster.Tests;

public static class PresentationTests
{
    [SporeTest("resource item resolves names and matches filters")]
    public static void ResourceItemFiltering()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        var entry = new DbpfEntry
        {
            Index = 3,
            Key = new ResourceKey(names.GetTypeHash("prop"), names.GetFileHash("CreatureGame"), names.GetFileHash("mything")),
            IsCompressed = true,
            PackedSize = 100,
            MemSize = 250,
        };

        var item = new ResourceItem(entry, names);
        Assert.Equal(3, item.Index);
        Assert.Equal("prop", item.TypeName);
        Assert.Equal("RefPack", item.Compression);
        Assert.True(item.Matches("prop"));
        Assert.True(item.Matches("CreatureGame"));
        Assert.False(item.Matches("nonexistentxyz"));
        Assert.True(item.Matches(string.Empty));
    }

    [SporeTest("preview renders a property file as XML")]
    public static void PreviewProperty()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();

        var list = new PropertyList();
        var flag = new BoolProperty();
        list.Add(0x00B2CCCA, flag);
        flag.Read(new MemoryStream(new byte[] { 1 }), 1, false);

        using var binary = new MemoryStream();
        list.Write(binary);

        var entry = new DbpfEntry { Key = new ResourceKey(ResourceTypeIds.Prop, 0, 0) };
        var item = new ResourceItem(entry, names);

        ResourcePreview preview = PreviewEngine.Build(item, binary.ToArray(), names);
        Assert.Equal("props", preview.Kind);
        Assert.Contains(preview.Text!, "description");
    }

    [SporeTest("preview falls back to hex for binary data")]
    public static void PreviewHex()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        var entry = new DbpfEntry { Key = new ResourceKey(0xDEADBEEF, 0, 0) };
        var item = new ResourceItem(entry, names);

        byte[] data = { 0x00, 0x01, 0x02, 0xFF };
        ResourcePreview preview = PreviewEngine.Build(item, data, names);
        Assert.Equal("hex", preview.Kind);
        Assert.Contains(preview.Text!, "00000000");
    }

    [SporeTest("preview shows raw PNG resources as images")]
    public static void PreviewRawPng()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        var entry = new DbpfEntry { Key = new ResourceKey(0x2F7D0004, 0, 0) };
        var item = new ResourceItem(entry, names);

        // Encode a tiny image to a PNG and pretend it is the raw resource payload.
        var image = new RgbaImage(2, 2);
        byte[] png = PngCodec.Encode(image);

        ResourcePreview preview = PreviewEngine.Build(item, png, names);
        Assert.Equal("image", preview.Kind);
        Assert.NotNull(preview.PngBytes);
    }

    [SporeTest("preview summarises a RenderWare 4 model")]
    public static void PreviewRenderWare4()
    {
        string? packagePath = TestData.FinalPackages.FirstOrDefault(p => p.EndsWith("CSA_Graphics.package"));
        if (packagePath is null)
        {
            throw new AssertionException("CSA_Graphics.package is not available.");
        }

        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);

        DbpfEntry? target = package.Entries.FirstOrDefault(e =>
            e.Key.TypeId == 0x2F4E681B && e.Key.GroupId == 0x40606000 && e.Key.InstanceId == 0x315CCB89);
        if (target is null)
        {
            throw new AssertionException("The expected model resource is missing.");
        }

        var item = new ResourceItem(target, names);
        ResourcePreview preview = PreviewEngine.Build(item, package.ReadEntry(target), names);

        // A model with geometry now previews as a 3D model (Kind == "model"); the text still lists sections.
        Assert.Equal("model", preview.Kind);
        Assert.True(preview.IsModel, "the resource should expose 3D geometry");
        Assert.True(preview.Model!.Meshes.Count > 0, "the model should have meshes");
        Assert.True(preview.Model.TotalTriangles > 0, "the model should have triangles");
        (float[] min, float[] max) = preview.Model.Bounds();
        Assert.True(max[0] >= min[0] && max[1] >= min[1] && max[2] >= min[2], "bounds are valid");
        Assert.Contains(preview.Text!, "RenderWare 4 resource");
        Assert.Contains(preview.Text!, "3D:");
    }

    [SporeTest("resource glyphs are chosen per type")]
    public static void ResourceGlyphs()
    {
        Assert.Equal("\U0001F4C4", ResourceGlyph.For("group/thing.prop", 0x00B1B104));
        Assert.Equal("\U0001F9CA", ResourceGlyph.For("group/model.rw4", 0x2F4E681B));
        Assert.Equal("\U0001F5BC", ResourceGlyph.For("group/tex.raster", 0x2F4E681C));
        Assert.Equal("\U0001F310", ResourceGlyph.For("group/lang.locale", 0x02FAC0B6));
        Assert.Equal("\U0001F4E6", ResourceGlyph.For("0x0B1B104/0x0.thing", 0xDEADBEEF));

        // Unknown extension falls back to the type id.
        Assert.Equal("\U0001F9CA", ResourceGlyph.For("0x2F4E681B/0x0", 0x2F4E681B));
    }

    [SporeTest("resource selector matches folders, types and names")]
    public static void ResourceSelectorMatching()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();

        // Unknown group: match by resolved name (group names carry a trailing '~' alias marker).
        var selector = ResourceSelector.Parse(types: null, groups: "creature_parts", name: null, names);
        Assert.False(selector.IsEmpty);
        uint group = names.GetFileHash("0x40626000");
        Assert.True(selector.Matches(0x00B1B104, group, "creature_parts~/thing.prop"), "matches by folder name");
        Assert.False(selector.Matches(0x00B1B104, 0x12345678, "other~/thing.prop"), "other folder excluded");

        // Multiple folders.
        var multi = ResourceSelector.Parse(null, "creature_parts, animations~", null, names);
        Assert.Equal(2, multi.Groups.Count);

        // Match by explicit hash literal.
        var byHash = ResourceSelector.Parse(null, "0x40626000", null, names);
        Assert.True(byHash.Matches(0x00B1B104, 0x40626000, "creature_parts~/x.prop"));
        Assert.False(byHash.Matches(0x00B1B104, 0x40626001, "creature_parts~/x.prop"));

        // Type and name filters.
        var byType = ResourceSelector.Parse("prop", null, null, names);
        Assert.True(byType.Matches(0x00B1B104, 1, "g/n.prop"));
        Assert.False(byType.Matches(0x2F4E681B, 1, "g/n.rw4"));

        var byName = ResourceSelector.Parse(null, null, "brute", names);
        Assert.True(byName.Matches(0x00B1B104, 1, "creature_parts~/ce_Brute_02.prop"));
        Assert.False(byName.Matches(0x00B1B104, 1, "creature_parts~/ce_mouth.prop"));

        // Empty selector matches everything.
        var empty = ResourceSelector.Parse(null, null, null, names);
        Assert.True(empty.IsEmpty);
        Assert.True(empty.Matches(0x00B1B104, 0x99999999, "x/y.z"));
    }

    [SporeTest("preview summarises a Spore animation")]
    public static void PreviewAnimation()
    {
        string? packagePath = TestData.FinalPackages.FirstOrDefault(p => p.EndsWith("CSA_Main.package"));
        if (packagePath is null)
        {
            throw new AssertionException("CSA_Main.package is not available.");
        }

        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);

        DbpfEntry? target = package.Entries.FirstOrDefault(e => e.Key.TypeId == 0xEE17C6AD && e.Key.InstanceId == 0x178D84B4);
        if (target is null)
        {
            throw new AssertionException("The expected animation resource is missing.");
        }

        var item = new ResourceItem(target, names);
        ResourcePreview preview = PreviewEngine.Build(item, package.ReadEntry(target), names);

        Assert.Equal("anim", preview.Kind);
        Assert.Contains(preview.Text!, "Spore animation");
        Assert.Contains(preview.Text!, "Channels:");
    }

    [SporeTest("preview shows trigger resources as text")]
    public static void PreviewTriggerText()
    {
        string? packagePath = TestData.FinalPackages.FirstOrDefault(p => p.EndsWith("Spore_Game.package"));
        if (packagePath is null)
        {
            throw new AssertionException("Spore_Game.package is not available.");
        }

        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);

        DbpfEntry? target = package.Entries.FirstOrDefault(e => e.Key.TypeId == 0x024A0E52);
        if (target is null)
        {
            throw new AssertionException("No trigger resource found.");
        }

        var item = new ResourceItem(target, names);
        ResourcePreview preview = PreviewEngine.Build(item, package.ReadEntry(target), names);

        Assert.Equal("text", preview.Kind);
    }

    [SporeTest("a textured RW4 model previews with a decoded texture")]
    public static void PreviewTexturedModel()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();

        foreach (string packagePath in TestData.FinalPackages)
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries.Take(400))
            {
                if (entry.Key.TypeId != 0x2F4E681B)
                {
                    continue;
                }

                var item = new ResourceItem(entry, names);
                ResourcePreview preview = PreviewEngine.Build(item, package.ReadEntry(entry), names);
                if (preview.IsModel && preview.Model!.TexturePng is not null)
                {
                    Assert.True(preview.Model.TexturePng.Length > 0, "texture PNG should not be empty");
                    // The texture is a PNG (signature).
                    Assert.Equal(0x89, preview.Model.TexturePng[0]);
                    Assert.Equal((byte)'P', preview.Model.TexturePng[1]);
                    return;
                }
            }
        }

        throw new AssertionException("No textured RW4 model preview was found.");
    }

    [SporeTest("a model with a skin exposes skeleton joints")]
    public static void PreviewSkeleton()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();

        foreach (string packagePath in TestData.FinalPackages)
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries.Take(400))
            {
                if (entry.Key.TypeId != 0x2F4E681B)
                {
                    continue;
                }

                var item = new ResourceItem(entry, names);
                ResourcePreview preview = PreviewEngine.Build(item, package.ReadEntry(entry), names);
                if (preview.IsModel && preview.Model!.SkeletonJoints.Count > 0)
                {
                    Assert.Equal(preview.Model.SkeletonJoints.Count, preview.Model.SkeletonBones.Count);
                    foreach ((int child, int parent) in preview.Model.SkeletonBones)
                    {
                        Assert.True(parent < preview.Model.SkeletonJoints.Count, "parent index in range");
                        Assert.True(child >= 0 && child < preview.Model.SkeletonJoints.Count, "child index in range");
                    }

                    return;
                }
            }
        }
    }

    [SporeTest("explorer model groups resources into folders")]
    public static void ExplorerFolders()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();

        DbpfEntry Make(string group, string instance, string type) => new()
        {
            Key = new ResourceKey(names.GetTypeHash(type), names.GetFileHash(group), names.GetFileHash(instance)),
        };

        var items = new List<ResourceItem>
        {
            new(Make("CreatureGame", "a", "prop"), names),
            new(Make("CreatureGame", "b", "rw4"), names),
            new(Make("CellGame", "c", "prop"), names),
        };

        List<ResourceFolder> folders = ExplorerModel.BuildFolders(items);
        Assert.Equal(2, folders.Count);

        ResourceFolder creature = folders.First(f => f.Items.Any(i => i.Path.Contains("CreatureGame")));
        Assert.Equal(2, creature.Count);
        Assert.Equal(2, creature.TypeCount);
    }

    [SporeTest("explorer model filters and sorts")]
    public static void ExplorerFilterSort()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        var items = new List<ResourceItem>
        {
            new(new DbpfEntry { Key = new ResourceKey(names.GetTypeHash("prop"), names.GetFileHash("g"), names.GetFileHash("alpha")) }, names),
            new(new DbpfEntry { Key = new ResourceKey(names.GetTypeHash("rw4"), names.GetFileHash("g"), names.GetFileHash("beta")) }, names),
        };

        Assert.Equal(2, ExplorerModel.Filter(items, null).Count);
        Assert.Equal(1, ExplorerModel.Filter(items, "alpha").Count);
        Assert.Equal(0, ExplorerModel.Filter(items, "zzz").Count);

        List<ResourceItem> byType = ExplorerModel.Sort(items, ResourceSort.Type, descending: false);
        Assert.Contains(byType[0].TypeName, "prop");
    }

    [SporeTest("package session enumerates and reads a real package")]
    public static void SessionReads()
    {
        string? path = TestData.BetaPackages.FirstOrDefault();
        if (path is null)
        {
            throw new AssertionException("No beta packages available.");
        }

        using PackageSession session = PackageSession.Open(path);
        IReadOnlyList<ResourceItem> items = session.BuildItems();
        Assert.Equal(session.Count, items.Count);
        Assert.True(items.Count > 0);

        // Every item must be readable through the session.
        int read = 0;
        foreach (ResourceItem item in items.Take(25))
        {
            byte[] data = session.Read(item.Entry);
            Assert.NotNull(data);
            read++;
        }

        Assert.Equal(Math.Min(25, items.Count), read);
    }
}
