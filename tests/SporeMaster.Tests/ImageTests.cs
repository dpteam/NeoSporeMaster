using System.Buffers.Binary;
using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Imaging;

namespace SporeMaster.Tests;

public static class ImageTests
{
    [SporeTest("TGA decodes uncompressed true-colour (top-left origin)")]
    public static void TgaUncompressed()
    {
        // 2x2 24-bit, top-left origin (descriptor bit 5).
        byte[] tga = BuildTga(imageType: 2, width: 2, height: 2, pixelDepth: 24, descriptor: 0x20, new byte[]
        {
            0, 0, 255,   // top-left  red   (BGR)
            0, 255, 0,   // top-right green
            255, 0, 0,   // bottom-left blue
            255, 255, 255, // bottom-right white
        });

        RgbaImage image = TgaCodec.Decode(tga);
        Assert.Equal(2, image.Width);
        Assert.Equal(2, image.Height);
        AssertPixel(image, 0, 0, 255, 0, 0);
        AssertPixel(image, 1, 0, 0, 255, 0);
        AssertPixel(image, 0, 1, 0, 0, 255);
        AssertPixel(image, 1, 1, 255, 255, 255);
    }

    [SporeTest("TGA decodes RLE true-colour")]
    public static void TgaRle()
    {
        // 4x1 RLE: a single run of 4 matching pixels (packet 0x83 = run of 4).
        byte[] tga = BuildTga(imageType: 10, width: 4, height: 1, pixelDepth: 24, descriptor: 0x20, new byte[]
        {
            0x83, 0x20, 0x40, 0x60, // run of 4, BGR (0x20,0x40,0x60)
        });

        RgbaImage image = TgaCodec.Decode(tga);
        Assert.Equal(4, image.Width);
        for (int x = 0; x < 4; x++)
        {
            AssertPixel(image, x, 0, 0x60, 0x40, 0x20);
        }
    }

    [SporeTest("ICO/CUR decodes a 32-bit DIB")]
    public static void IcoDib()
    {
        byte[] cur = BuildCur32();
        RgbaImage image = IcoCodec.Decode(cur);
        Assert.Equal(2, image.Width);
        Assert.Equal(2, image.Height);
        AssertPixel(image, 0, 0, 255, 0, 0);
        AssertPixel(image, 1, 0, 0, 255, 0);
        AssertPixel(image, 0, 1, 0, 0, 255);
        AssertPixel(image, 1, 1, 255, 255, 255);
    }

    [SporeTest("ImageFormats sniffs the container")]
    public static void Dispatch()
    {
        byte[] tga = BuildTga(2, 1, 1, 24, 0x20, new byte[] { 1, 2, 3 });
        Assert.Equal(1, ImageFormats.Decode(tga).Width);

        byte[] png = PngCodec.Encode(new RgbaImage(3, 2));
        RgbaImage fromPng = ImageFormats.Decode(png);
        Assert.Equal(3, fromPng.Width);
        Assert.Equal(2, fromPng.Height);

        Assert.True(IcoCodec.LooksLikeIco(BuildCur32()), "synthesised CUR should be recognised");
        Assert.False(IcoCodec.LooksLikeIco(tga), "a TGA must not be mistaken for an ICO");
    }

    [SporeTest("real tga resources decode when available")]
    public static void RealTga()
    {
        int tested = 0;
        foreach (string package in TestData.FinalPackages.Concat(TestData.BetaPackages))
        {
            using DatabasePackedFile dbpf = DatabasePackedFile.Open(package);
            foreach (DbpfEntry entry in dbpf.Entries)
            {
                if (entry.Key.TypeId != 0x2F7D0006)
                {
                    continue;
                }

                RgbaImage image = TgaCodec.Decode(dbpf.ReadEntry(entry));
                Assert.True(image.Width > 0 && image.Height > 0, "decoded TGA has a size");

                tested++;
                if (tested >= 20)
                {
                    return;
                }
            }
        }

        if (tested == 0)
        {
            Console.WriteLine("No tga resources found; skipping.");
        }
    }

    [SporeTest("real cur resources decode when available")]
    public static void RealCur()
    {
        int tested = 0;
        int animated = 0;
        foreach (string package in TestData.FinalPackages.Concat(TestData.BetaPackages))
        {
            using DatabasePackedFile dbpf = DatabasePackedFile.Open(package);
            foreach (DbpfEntry entry in dbpf.Entries)
            {
                if (entry.Key.TypeId != 0x02393756)
                {
                    continue;
                }

                byte[] data = dbpf.ReadEntry(entry);
                if (!IcoCodec.LooksLikeIco(data))
                {
                    // Some cursors are RIFF AVI animated ones, which we deliberately do not decode.
                    animated++;
                    continue;
                }

                RgbaImage image = IcoCodec.Decode(data);
                Assert.True(image.Width > 0 && image.Height > 0, "decoded cursor has a size");

                tested++;
                if (tested >= 20)
                {
                    Console.WriteLine($"decoded {tested} icon cursor(s); saw {animated} animated (RIFF) cursor(s)");
                    return;
                }
            }
        }

        if (tested == 0)
        {
            Console.WriteLine("No cur resources found; skipping.");
        }
    }

    private static void AssertPixel(RgbaImage image, int x, int y, byte r, byte g, byte b)
    {
        (byte pr, byte pg, byte pb, byte _) = image.GetPixel(x, y);
        Assert.True(
            pr == r && pg == g && pb == b,
            $"pixel ({x},{y}) = {pr},{pg},{pb} but expected {r},{g},{b}");
    }

    private static byte[] BuildTga(int imageType, int width, int height, int pixelDepth, int descriptor, byte[] data)
    {
        var header = new byte[18];
        header[2] = (byte)imageType;
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(12), (ushort)width);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(14), (ushort)height);
        header[16] = (byte)pixelDepth;
        header[17] = (byte)descriptor;

        var result = new byte[18 + data.Length];
        Array.Copy(header, result, 18);
        Array.Copy(data, 0, result, 18, data.Length);
        return result;
    }

    private static byte[] BuildCur32()
    {
        // One 2x2 32-bit cursor image.
        const int headerSize = 22;      // 6 (dir) + 16 (entry)
        const int dibSize = 40;
        const int xorStride = 8;        // 2 px * 4 bytes
        const int andStride = 4;        // ((2 + 31) / 32) * 4
        int payload = dibSize + (xorStride * 2) + (andStride * 2);

        var result = new byte[headerSize + payload];
        // ICONDIR: reserved 0, type 2 (cursor), count 1.
        result[2] = 2;
        result[4] = 1;

        // ICONDIRENTRY at offset 6.
        result[6] = 2;      // width
        result[7] = 2;      // height
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(6 + 8), (uint)payload);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(6 + 12), headerSize);

        // BITMAPINFOHEADER.
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(headerSize), 40);
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(headerSize + 4), 2);
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(headerSize + 8), 4); // doubled height
        BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(headerSize + 12), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(headerSize + 14), 32);

        // XOR bitmap, bottom-up: row 0 = bottom (blue, white), row 1 = top (red, green).
        int xor = headerSize + dibSize;
        result[xor + 0] = 255;                           // blue B,G,R,A
        result[xor + 3] = 255;
        result[xor + 4] = 255; result[xor + 5] = 255; result[xor + 6] = 255; result[xor + 7] = 255; // white
        result[xor + xorStride + 0] = 0; result[xor + xorStride + 2] = 255; result[xor + xorStride + 3] = 255; // red
        result[xor + xorStride + 4] = 0; result[xor + xorStride + 5] = 255; result[xor + xorStride + 7] = 255; // green

        return result;
    }
}
