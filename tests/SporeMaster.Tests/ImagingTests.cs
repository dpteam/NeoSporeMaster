using System.IO.Compression;
using SporeMaster.Core.Imaging;

namespace SporeMaster.Tests;

public static class ImagingTests
{
    [SporeTest("PNG encode/decode round-trips RGBA")]
    public static void PngRoundTrip()
    {
        var image = new RgbaImage(37, 23);
        var random = new Random(99);
        for (int i = 0; i < image.Pixels.Length; i++)
        {
            image.Pixels[i] = (byte)random.Next(256);
        }

        byte[] png = PngCodec.Encode(image);
        Assert.Equal((byte)0x89, png[0]);
        Assert.Equal((byte)'P', png[1]);

        RgbaImage decoded = PngCodec.Decode(png);
        Assert.Equal(image.Width, decoded.Width);
        Assert.Equal(image.Height, decoded.Height);
        Assert.True(image.Pixels.AsSpan().SequenceEqual(decoded.Pixels), "RGBA pixels must survive a PNG round trip");
    }

    [SporeTest("PNG decoder handles grey and RGB colour types")]
    public static void PngColourTypes()
    {
        // 2x2 8-bit greyscale (colour type 0).
        byte[] grey = BuildPng(2, 2, 0, new byte[]
        {
            0x00, 0x10, 0x20,
            0x00, 0x30, 0x40,
        });
        RgbaImage greyImage = PngCodec.Decode(grey);
        Assert.Equal(2, greyImage.Width);
        Assert.Equal((byte)0x10, greyImage.GetPixel(0, 0).R);
        Assert.Equal((byte)0x40, greyImage.GetPixel(1, 1).R);
        Assert.Equal((byte)255, greyImage.GetPixel(0, 0).A);

        // 1x2 8-bit RGB (colour type 2).
        byte[] rgb = BuildPng(1, 2, 2, new byte[]
        {
            0x00, 0xAA, 0xBB, 0xCC,
            0x00, 0x01, 0x02, 0x03,
        });
        RgbaImage rgbImage = PngCodec.Decode(rgb);
        Assert.Equal((byte)0xAA, rgbImage.GetPixel(0, 0).R);
        Assert.Equal((byte)0xBB, rgbImage.GetPixel(0, 0).G);
        Assert.Equal((byte)0xCC, rgbImage.GetPixel(0, 0).B);
        Assert.Equal((byte)255, rgbImage.GetPixel(0, 0).A);
    }

    [SporeTest("PNG filter reconstruction is correct")]
    public static void PngFilters()
    {
        // A 4x1 RGB image whose only scanline uses the "Up" filter (type 2) with no prior row.
        byte[] up = BuildPng(4, 1, 2, new byte[]
        {
            0x02, 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90, 0xA0, 0xB0, 0xC0,
        });
        RgbaImage image = PngCodec.Decode(up);
        Assert.Equal((byte)0x10, image.GetPixel(0, 0).R);
        Assert.Equal((byte)0xC0, image.GetPixel(3, 0).B);
    }

    /// <summary>Builds a PNG with the given colour type from raw (already filtered) scanlines.</summary>
    private static byte[] BuildPng(int width, int height, int colourType, byte[] rawScanlines)
    {
        using var output = new MemoryStream();
        output.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        var ihdr = new byte[13];
        WriteU32(ihdr, 0, (uint)width);
        WriteU32(ihdr, 4, (uint)height);
        ihdr[8] = 8;
        ihdr[9] = (byte)colourType;
        WriteChunk(output, "IHDR", ihdr);

        byte[] compressed;
        using (var buffer = new MemoryStream())
        {
            using (var zlib = new ZLibStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
            {
                zlib.Write(rawScanlines);
            }

            compressed = buffer.ToArray();
        }

        WriteChunk(output, "IDAT", compressed);
        WriteChunk(output, "IEND", Array.Empty<byte>());
        return output.ToArray();
    }

    private static void WriteChunk(Stream output, string type, byte[] data)
    {
        var length = new byte[4];
        WriteU32(length, 0, (uint)data.Length);
        output.Write(length);

        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        output.Write(typeBytes);
        output.Write(data);

        uint crc = Crc(typeBytes, 0, typeBytes.Length);
        crc = CrcUpdate(crc, data, 0, data.Length);
        var crcBytes = new byte[4];
        WriteU32(crcBytes, 0, crc);
        output.Write(crcBytes);
    }

    private static void WriteU32(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    // A deliberately separate CRC implementation so the tests do not depend on the production one.
    private static uint Crc(byte[] data, int offset, int count) => CrcUpdate(0xFFFFFFFFu, data, offset, count) ^ 0xFFFFFFFFu;

    private static uint CrcUpdate(uint crc, byte[] data, int offset, int count)
    {
        for (int i = 0; i < count; i++)
        {
            crc ^= data[offset + i];
            for (int k = 0; k < 8; k++)
            {
                crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
            }
        }

        return crc;
    }
}
