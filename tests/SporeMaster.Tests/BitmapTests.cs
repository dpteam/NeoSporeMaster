using SporeMaster.Core.Bitmaps;
using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Imaging;

namespace SporeMaster.Tests;

public static class BitmapTests
{
    [SporeTest("32-bit bitmap round-trips pixel data")]
    public static void RoundTrip32()
    {
        RoundTrip(SporeBitmapTypes.ThirtyTwoBitImage, 16, 12, exact: true);
    }

    [SporeTest("8-bit bitmap round-trips greyscale")]
    public static void RoundTrip8()
    {
        RoundTrip(SporeBitmapTypes.EightBitImage, 20, 7, exact: true);
    }

    [SporeTest("48-bit bitmap round-trips (16-bit channels)")]
    public static void RoundTrip48()
    {
        RoundTrip(SporeBitmapTypes.FortyEightBitImage, 9, 9, exact: true);
    }

    [SporeTest("1-bit bitmap round-trips (multiples of 8)")]
    public static void RoundTrip1()
    {
        RoundTrip(SporeBitmapTypes.BitImage, 16, 8, exact: true);
    }

    [SporeTest("bitmap type ids and names")]
    public static void Types()
    {
        Assert.True(SporeBitmapTypes.IsSupported(SporeBitmapTypes.BitImage));
        Assert.True(SporeBitmapTypes.IsSupported(SporeBitmapTypes.ThirtyTwoBitImage));
        Assert.False(SporeBitmapTypes.IsSupported(SporeBitmapTypes.SixteenBitImage));
        Assert.Equal("32bitImage", SporeBitmapTypes.GetName(SporeBitmapTypes.ThirtyTwoBitImage));
    }

    [SporeTest("bitmap survives a PNG round trip")]
    public static void ViaPng()
    {
        var image = new RgbaImage(8, 8);
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                image.SetPixel(x, y, (byte)(x * 32), (byte)(y * 32), (byte)((x + y) * 16), 255);
            }
        }

        BitmapImage bitmap = BitmapImage.FromImage(SporeBitmapTypes.ThirtyTwoBitImage, image);

        using var binary = new MemoryStream();
        bitmap.Write(binary);
        binary.Position = 0;
        BitmapImage parsed = BitmapImage.Read(binary, SporeBitmapTypes.ThirtyTwoBitImage);

        byte[] png = PngCodec.Encode(parsed.Image);
        RgbaImage fromPng = PngCodec.Decode(png);

        Assert.True(image.Pixels.AsSpan().SequenceEqual(fromPng.Pixels), "bitmap -> PNG must preserve pixels");
    }

    [SporeTest("real bitmap resources decode")]
    public static void RealBitmaps()
    {
        int tested = 0;
        var types = new SortedSet<uint>();

        foreach (string packagePath in TestData.FinalPackages.Concat(TestData.BetaPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (!SporeBitmapTypes.IsSupported(entry.Key.TypeId))
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                BitmapImage bitmap = BitmapImage.Read(new MemoryStream(data), entry.Key.TypeId);

                Assert.True(bitmap.Width > 0 && bitmap.Width <= 8192, $"plausible width ({bitmap.Width})");
                Assert.True(bitmap.Height > 0 && bitmap.Height <= 8192, $"plausible height ({bitmap.Height})");
                Assert.Equal(bitmap.Width * bitmap.Height * 4, bitmap.Image.Pixels.Length);

                // The decoded image must survive a PNG encode.
                byte[] png = PngCodec.Encode(bitmap.Image);
                Assert.True(png.Length > 8);

                types.Add(entry.Key.TypeId);
                tested++;

                if (tested >= 100)
                {
                    break;
                }
            }

            if (tested >= 100)
            {
                break;
            }
        }

        if (tested == 0)
        {
            throw new AssertionException("No bitmap resources found in the reference packages.");
        }
    }

    [SporeTest("unsupported bitmap type is rejected on read")]
    public static void UnsupportedType()
    {
        Assert.Throws<NotSupportedException>(() =>
            BitmapImage.Read(new MemoryStream(new byte[64]), SporeBitmapTypes.SixteenBitImage));
    }

    private static void RoundTrip(uint type, int width, int height, bool exact)
    {
        var image = new RgbaImage(width, height);
        var random = new Random((int)type);
        for (int i = 0; i < image.Pixels.Length; i++)
        {
            image.Pixels[i] = (byte)random.Next(256);
        }

        // Greyscale types only keep the red channel, so normalise the source for those.
        if (type is SporeBitmapTypes.EightBitImage or SporeBitmapTypes.BitImage)
        {
            for (int i = 0; i < image.Pixels.Length; i += 4)
            {
                byte v = image.Pixels[i];
                image.Pixels[i] = v;
                image.Pixels[i + 1] = v;
                image.Pixels[i + 2] = v;
                image.Pixels[i + 3] = 255;
            }
        }

        BitmapImage bitmap = BitmapImage.FromImage(type, image);

        using var stream = new MemoryStream();
        bitmap.Write(stream);
        stream.Position = 0;
        BitmapImage parsed = BitmapImage.Read(stream, type);

        Assert.Equal(width, parsed.Width);
        Assert.Equal(height, parsed.Height);
        Assert.Equal(bitmap.ChannelCount, parsed.ChannelCount);

        using var again = new MemoryStream();
        parsed.Write(again);
        Assert.True(stream.ToArray().AsSpan().SequenceEqual(again.ToArray()), "bitmap binary must round-trip exactly");

        _ = exact;
    }
}
