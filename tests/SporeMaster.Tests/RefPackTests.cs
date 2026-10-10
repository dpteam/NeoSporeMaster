using SporeMaster.Core.Compression;

namespace SporeMaster.Tests;

public static class RefPackTests
{
    [SporeTest("refpack round-trips repetitive data")]
    public static void RoundTripRepetitive()
    {
        byte[] data = new byte[50_000];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)('A' + (i % 7));
        }

        byte[] compressed = RefPack.Compress(data);
        Assert.True(RefPack.LooksCompressed(compressed), "output should carry the 10 FB marker");
        Assert.True(compressed.Length < data.Length, "repetitive data should compress");

        byte[] restored = RefPack.Decompress(compressed, data.Length);
        Assert.Equal(data.Length, restored.Length);
        Assert.True(data.AsSpan().SequenceEqual(restored), "round-tripped data must match");
    }

    [SporeTest("refpack round-trips pseudo-random data")]
    public static void RoundTripRandom()
    {
        var random = new Random(1234);
        byte[] data = new byte[64 * 1024];
        random.NextBytes(data);

        byte[] compressed = RefPack.Compress(data);
        byte[] restored = RefPack.Decompress(compressed, data.Length);
        Assert.True(data.AsSpan().SequenceEqual(restored), "random data must survive a round trip");
    }

    [SporeTest("refpack handles edge sizes")]
    public static void EdgeSizes()
    {
        foreach (int size in new[] { 0, 1, 2, 3, 4, 5, 1023, 1028, 4096 })
        {
            byte[] data = new byte[size];
            for (int i = 0; i < size; i++)
            {
                data[i] = (byte)i;
            }

            byte[] compressed = RefPack.Compress(data);
            byte[] restored = RefPack.Decompress(compressed, size);
            Assert.Equal(size, restored.Length);
            Assert.True(data.AsSpan().SequenceEqual(restored), $"size {size} must round-trip");
        }
    }

    [SporeTest("decompressed size header is readable")]
    public static void SizeHeader()
    {
        byte[] data = new byte[1234];
        byte[] compressed = RefPack.Compress(data);
        Assert.Equal(1234, RefPack.GetDecompressedSize(compressed));
    }
}
