using SporeMaster.Core.Dbpf;
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
