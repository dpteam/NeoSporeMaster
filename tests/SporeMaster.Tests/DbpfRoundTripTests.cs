using SporeMaster.Core.Dbpf;

namespace SporeMaster.Tests;

public static class DbpfRoundTripTests
{
    [SporeTest("dbpf write/read round-trip (uncompressed, v2.0)")]
    public static void RoundTripUncompressed()
    {
        RoundTrip(compress: false, big: false, new DbpfVersion(2, 0, 0, 3));
    }

    [SporeTest("dbpf write/read round-trip (compressed, v3.0)")]
    public static void RoundTripCompressed()
    {
        RoundTrip(compress: true, big: false, new DbpfVersion(3, 0, 0, 3));
    }

    [SporeTest("dbpf write/read round-trip (big container)")]
    public static void RoundTripBig()
    {
        RoundTrip(compress: true, big: true, new DbpfVersion(3, 0, 0, 3));
    }

    private static void RoundTrip(bool compress, bool big, DbpfVersion version)
    {
        string temp = TestData.CreateTempDirectory();
        string path = Path.Combine(temp, "roundtrip.package");
        try
        {
            var items = new List<DbpfWriteItem>
            {
                new(new ResourceKey(0x2F4E681B, 0x12345678, 0xAABBCCDD), MakeText(10_000)),
                new(new ResourceKey(0x00B1B104, 0x12345678, 0x00000001), MakeText(3)),
                new(new ResourceKey(0x02FAC0B6, 0xDEADBEEF, 0x00000002), Array.Empty<byte>()),
                new(new ResourceKey(0x00B1B104, 0x12345678, 0x00000003), MakeRandom(5_000, 7)),
            };

            var options = new DbpfWriteOptions { Compress = compress, Big = big, Version = version };
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
            {
                int written = DatabasePackedFile.Write(stream, items, options);
                Assert.Equal(items.Count, written);
            }

            using DatabasePackedFile package = DatabasePackedFile.Open(path);
            Assert.Equal(big, package.IsBig);
            Assert.Equal(version.Major, package.Version.Major);
            Assert.Equal(items.Count, package.Entries.Count);
            Assert.True(package.Header!.IndexSize > 0, "index size should be recorded");
            Assert.True(package.Header!.IndexOffset >= (big ? 120 : 96), "index should follow the header");

            foreach (DbpfWriteItem item in items)
            {
                DbpfEntry? entry = package.Entries.FirstOrDefault(e => e.Key == item.Key);
                Assert.NotNull(entry, $"entry {item.Key} should exist");
                byte[] data = package.ReadEntry(entry!);
                Assert.Equal(item.Data.Length, data.Length);
                Assert.True(item.Data.AsSpan().SequenceEqual(data), $"payload for {item.Key} must match");
            }
        }
        finally
        {
            TestData.DeleteDirectory(temp);
        }
    }

    [SporeTest("resource key parsing")]
    public static void ResourceKeyParsing()
    {
        Assert.True(ResourceKey.TryParse("0x2F4E681B:0x1234:0xAB", out ResourceKey key));
        Assert.Equal(0x2F4E681Bu, key.TypeId);
        Assert.Equal(0x1234u, key.GroupId);
        Assert.Equal(0xABu, key.InstanceId);
        Assert.True(ResourceKey.TryParse("#2F4E681B:4660:171", out key));
        Assert.Equal(0x1234u, key.GroupId);
        Assert.False(ResourceKey.TryParse("nope", out _));
    }

    private static byte[] MakeText(int length)
    {
        byte[] data = new byte[length];
        for (int i = 0; i < length; i++)
        {
            data[i] = (byte)('a' + (i % 26));
        }

        return data;
    }

    private static byte[] MakeRandom(int length, int seed)
    {
        var random = new Random(seed);
        byte[] data = new byte[length];
        random.NextBytes(data);
        return data;
    }
}
