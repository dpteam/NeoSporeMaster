using SporeMaster.Core.Textures;

namespace SporeMaster.Tests;

public static class BlockCompressionTests
{
    [SporeTest("BC1 (DXT1) decodes a solid colour block")]
    public static void DecodeDxt1Solid()
    {
        // c0 == c1 == red (RGB565 0xF800); all indices 0 -> red.
        var block = new byte[8];
        block[0] = 0x00;
        block[1] = 0xF8;
        block[2] = 0x00;
        block[3] = 0xF8;

        byte[]? rgba = BlockCompressionDecoder.Decode(TextureFormats.Dxt1, block, 4, 4);
        Assert.NotNull(rgba);
        Assert.Equal(4 * 4 * 4, rgba!.Length);
        Assert.Equal(255, rgba[0]);
        Assert.Equal(0, rgba[1]);
        Assert.Equal(0, rgba[2]);
        Assert.Equal(255, rgba[3]);
    }

    [SporeTest("BC3 (DXT5) decodes colour and alpha")]
    public static void DecodeDxt5()
    {
        var block = new byte[16];
        block[0] = 255; // a0
        block[1] = 255; // a1  -> indices 0 give 255
        // colour block: c0 == c1 == green (0x07E0), indices 0.
        block[8] = 0xE0;
        block[9] = 0x07;
        block[10] = 0xE0;
        block[11] = 0x07;

        byte[]? rgba = BlockCompressionDecoder.Decode(TextureFormats.Dxt5, block, 4, 4);
        Assert.NotNull(rgba);
        Assert.Equal(0, rgba![0]);
        Assert.Equal(255, rgba[1]);
        Assert.Equal(0, rgba[2]);
        Assert.Equal(255, rgba[3]);
    }

    [SporeTest("block decoder reports unsupported formats")]
    public static void UnsupportedFormat()
    {
        Assert.True(BlockCompressionDecoder.CanDecode(TextureFormats.Dxt1));
        Assert.True(BlockCompressionDecoder.CanDecode(TextureFormats.A8R8G8B8));
        Assert.False(BlockCompressionDecoder.CanDecode(TextureFormats.MakeFourCc("ATI2")));
        Assert.True(BlockCompressionDecoder.Decode(TextureFormats.MakeFourCc("ATI2"), new byte[16], 4, 4) is null);
    }
}
