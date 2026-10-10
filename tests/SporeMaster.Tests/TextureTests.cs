using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Textures;

namespace SporeMaster.Tests;

public static class TextureTests
{
    [SporeTest("raster mip sizes match the decoded real textures")]
    public static void RasterRoundTrip()
    {
        int tested = 0;
        int mismatches = 0;
        string? first = null;

        foreach (string packagePath in AllPackages())
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != RasterTexture.TypeId)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                if (data.Length < 24)
                {
                    continue;
                }

                try
                {
                    RasterTexture raster = RasterTexture.Read(new MemoryStream(data));

                    // The declared mipmap sizes must match what the format implies.
                    int[] expectedSizes = raster.ToDds().GetMipmapSizes();
                    for (int i = 0; i < raster.Mipmaps.Length; i++)
                    {
                        if (raster.Mipmaps[i].Length != expectedSizes[i])
                        {
                            mismatches++;
                            first ??= $"{Path.GetFileName(packagePath)} {entry.Key} mip {i}: {raster.Mipmaps[i].Length} != {expectedSizes[i]}";
                            break;
                        }
                    }

                    tested++;
                }
                catch (Exception ex)
                {
                    mismatches++;
                    first ??= $"{Path.GetFileName(packagePath)} {entry.Key}: {ex.Message}";
                }
            }
        }

        if (tested == 0)
        {
            throw new AssertionException("No raster resources found in the reference packages.");
        }

        Assert.True(mismatches == 0, $"{mismatches} of {tested} raster(s) failed mip-size validation. First: {first}");
    }

    [SporeTest("raster -> DDS -> raster is byte-exact")]
    public static void RasterDdsRoundTrip()
    {
        int tested = 0;
        string? first = null;

        foreach (string packagePath in AllPackages())
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != RasterTexture.TypeId)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                if (data.Length < 24)
                {
                    continue;
                }

                RasterTexture raster = RasterTexture.Read(new MemoryStream(data));
                DdsTexture dds = raster.ToDds();

                // Serialise the DDS and read it back to emulate a real file round trip.
                using var ddsStream = new MemoryStream();
                dds.Write(ddsStream);
                ddsStream.Position = 0;
                DdsTexture reparsed = DdsTexture.Read(ddsStream);

                RasterTexture reborn = RasterTexture.FromDds(reparsed);
                using var rasterStream = new MemoryStream();
                reborn.Write(rasterStream);

                if (!data.AsSpan().SequenceEqual(rasterStream.ToArray()))
                {
                    first ??= $"{Path.GetFileName(packagePath)} {entry.Key} ({data.Length} vs {rasterStream.Length})";
                }

                tested++;
            }
        }

        if (tested == 0)
        {
            throw new AssertionException("No raster resources found in the reference packages.");
        }

        Assert.True(first is null, $"Raster/DDS round-trip mismatch: {first}");
    }

    [SporeTest("DDS header fields are self-consistent")]
    public static void DdsHeaderFields()
    {
        var dds = new DdsTexture
        {
            Width = 32,
            Height = 32,
            MipmapCount = 6,
            Format = TextureFormats.A8R8G8B8,
            RgbBitCount = 32,
            Data = new byte[32 * 32 * 4],
        };

        using var stream = new MemoryStream();
        dds.Write(stream);
        Assert.Equal(DdsTexture.HeaderSize + dds.Data.Length, (int)stream.Length);

        stream.Position = 0;
        DdsTexture read = DdsTexture.Read(stream);
        Assert.Equal(32, read.Width);
        Assert.Equal(32, read.Height);
        Assert.Equal(6, read.MipmapCount);
        Assert.Equal(TextureFormats.A8R8G8B8, read.Format);

        int[] sizes = read.GetMipmapSizes();
        Assert.Equal(4096, sizes[0]);
        Assert.Equal(4, sizes[5]);
    }

    [SporeTest("block format mip size helpers")]
    public static void BlockSizes()
    {
        Assert.Equal(8, TextureFormats.GetBlockSize(TextureFormats.Dxt1));
        Assert.Equal(16, TextureFormats.GetBlockSize(TextureFormats.Dxt5));
        Assert.Equal(0, TextureFormats.GetBlockSize(TextureFormats.A8R8G8B8));

        // 32x32 DXT1 = 8*8 blocks * 8 bytes = 512
        Assert.Equal(512, TextureFormats.GetMipSize(TextureFormats.Dxt1, 32, 32, 0));
        Assert.Equal(128, TextureFormats.GetMipSize(TextureFormats.Dxt1, 16, 16, 0));
        Assert.Equal(4096, TextureFormats.GetMipSize(TextureFormats.A8R8G8B8, 32, 32, 32));
    }

    private static IEnumerable<string> AllPackages() =>
        TestData.BetaPackages.Concat(TestData.FinalPackages);
}
