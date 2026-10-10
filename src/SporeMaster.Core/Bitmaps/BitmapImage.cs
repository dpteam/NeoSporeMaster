using SporeMaster.Core.Imaging;
using SporeMaster.Core.IO;

namespace SporeMaster.Core.Bitmaps;

/// <summary>Spore raw bitmap resource types (from <c>reg_type.txt</c>).</summary>
public static class SporeBitmapTypes
{
    public const uint BitImage = 0x03E421E9;      // 1-bit
    public const uint EightBitImage = 0x03E421EC; // 8-bit greyscale
    public const uint SixteenBitImage = 0x03E421F0; // 16-bit (not supported by SporeModder FX)
    public const uint ThirtyTwoBitImage = 0x03E421ED; // 32-bit BGRA
    public const uint FortyEightBitImage = 0x03E421EF; // 48-bit RGB (16-bit per channel)

    public static bool IsSupported(uint typeId) =>
        typeId is BitImage or EightBitImage or ThirtyTwoBitImage or FortyEightBitImage;

    public static string GetName(uint typeId) => typeId switch
    {
        BitImage => "bitImage",
        EightBitImage => "8bitImage",
        SixteenBitImage => "16bitImage",
        ThirtyTwoBitImage => "32bitImage",
        FortyEightBitImage => "48bitImage",
        _ => "0x" + typeId.ToString("X8"),
    };
}

/// <summary>
/// A Spore raw bitmap resource (types <c>bitImage</c>, <c>8bitImage</c>, <c>32bitImage</c>,
/// <c>48bitImage</c>).
/// </summary>
/// <remarks>
/// Layout (little-endian):
/// <code>
/// int32  version     // 0
/// int32  width
/// int32  height
/// int32  channelCount // informational: 0 / 1 / 2 / 3 by type
/// int32  bufferSize
/// byte[bufferSize] data
/// int32  unknown     // only for the 1-bit type
/// </code>
/// Pixel order differs by type: 1-bit packs 8 pixels per byte (LSB first, greyscale), 8-bit is
/// greyscale, 32-bit is BGRA and 48-bit is 16-bit-per-channel RGB. See <c>docs/BITMAP_FORMAT.md</c>.
/// </remarks>
public sealed class BitmapImage
{
    public const int Version = 0;

    public uint Type { get; set; }

    public int Width { get; private set; }

    public int Height { get; private set; }

    public int ChannelCount { get; private set; }

    public int Unknown { get; set; }

    public RgbaImage Image { get; private set; } = new(1, 1);

    private static int ChannelsFor(uint type) => type switch
    {
        SporeBitmapTypes.BitImage => 0,
        SporeBitmapTypes.EightBitImage => 1,
        SporeBitmapTypes.ThirtyTwoBitImage => 2,
        SporeBitmapTypes.FortyEightBitImage => 3,
        _ => 0,
    };

    public static BitmapImage FromImage(uint type, RgbaImage image)
    {
        if (!SporeBitmapTypes.IsSupported(type))
        {
            throw new ArgumentException($"Unsupported bitmap type 0x{type:X8}.", nameof(type));
        }

        if (type == SporeBitmapTypes.BitImage && (image.Width % 8 != 0 || image.Height % 8 != 0))
        {
            throw new ArgumentException("1-bit bitmaps require dimensions that are multiples of 8.", nameof(image));
        }

        return new BitmapImage
        {
            Type = type,
            Width = image.Width,
            Height = image.Height,
            ChannelCount = ChannelsFor(type),
            Image = image,
            Unknown = type == SporeBitmapTypes.BitImage ? image.Width / 8 : 0,
        };
    }

    public static BitmapImage Read(Stream stream, uint type)
    {
        if (!SporeBitmapTypes.IsSupported(type))
        {
            throw new NotSupportedException($"Unsupported bitmap type 0x{type:X8}.");
        }

        int version = stream.ReadI32BE();
        if (version != Version)
        {
            throw new InvalidDataException($"Unsupported bitmap version {version}.");
        }

        int width = stream.ReadI32BE();
        int height = stream.ReadI32BE();
        int channelCount = stream.ReadI32BE();
        int bufferSize = stream.ReadI32BE();

        if (width <= 0 || height <= 0 || width > 1 << 16 || height > 1 << 16)
        {
            throw new InvalidDataException($"Bitmap dimensions look invalid ({width}x{height}).");
        }

        if (bufferSize < 0 || bufferSize > 1 << 30)
        {
            throw new InvalidDataException($"Bitmap data size looks invalid ({bufferSize}).");
        }

        byte[] data = stream.ReadBytesExactly(bufferSize);

        var image = new BitmapImage
        {
            Type = type,
            Width = width,
            Height = height,
            ChannelCount = channelCount,
        };

        if (type == SporeBitmapTypes.BitImage)
        {
            image.Unknown = stream.ReadI32BE();
        }

        image.Image = Decode(type, width, height, data);
        return image;
    }

    public void Write(Stream stream)
    {
        byte[] data = Encode();

        stream.WriteI32BE(Version);
        stream.WriteI32BE(Width);
        stream.WriteI32BE(Height);
        stream.WriteI32BE(ChannelCount);
        stream.WriteI32BE(data.Length);
        stream.Write(data);

        if (Type == SporeBitmapTypes.BitImage)
        {
            stream.WriteI32BE(Unknown);
        }
    }

    private byte[] Encode()
    {
        int width = Width;
        int height = Height;

        return Type switch
        {
            SporeBitmapTypes.BitImage => EncodeBits(width, height),
            SporeBitmapTypes.EightBitImage => EncodeGray8(width, height),
            SporeBitmapTypes.ThirtyTwoBitImage => EncodeBgra(width, height),
            SporeBitmapTypes.FortyEightBitImage => EncodeRgb48(width, height),
            _ => throw new NotSupportedException($"Unsupported bitmap type 0x{Type:X8}."),
        };
    }

    private byte[] EncodeBits(int width, int height)
    {
        var data = new byte[width * height / 8];
        int p = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x += 8)
            {
                int value = 0;
                for (int k = 7; k >= 0; k--)
                {
                    bool on = Image.GetPixel(x + k, y).R > 127;
                    if (on)
                    {
                        value |= 1;
                    }

                    if (k != 0)
                    {
                        value <<= 1;
                    }
                }

                data[p++] = (byte)value;
            }
        }

        return data;
    }

    private byte[] EncodeGray8(int width, int height)
    {
        var data = new byte[width * height];
        int p = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                data[p++] = Image.GetPixel(x, y).R;
            }
        }

        return data;
    }

    private byte[] EncodeBgra(int width, int height)
    {
        var data = new byte[width * height * 4];
        int p = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                (byte r, byte g, byte b, byte a) = Image.GetPixel(x, y);
                data[p++] = b;
                data[p++] = g;
                data[p++] = r;
                data[p++] = a;
            }
        }

        return data;
    }

    private byte[] EncodeRgb48(int width, int height)
    {
        var data = new byte[width * height * 6];
        int p = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                (byte r, byte g, byte b, _) = Image.GetPixel(x, y);
                WriteU16(data, ref p, Scale16(r));
                WriteU16(data, ref p, Scale16(g));
                WriteU16(data, ref p, Scale16(b));
            }
        }

        return data;
    }

    private static ushort Scale16(byte value) => (ushort)(value * 257);

    private static void WriteU16(byte[] data, ref int p, ushort value)
    {
        data[p++] = (byte)value;
        data[p++] = (byte)(value >> 8);
    }

    private static RgbaImage Decode(uint type, int width, int height, byte[] data)
    {
        var image = new RgbaImage(width, height);

        switch (type)
        {
            case SporeBitmapTypes.BitImage:
            {
                int p = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x += 8)
                    {
                        int value = p < data.Length ? data[p++] : 0;
                        for (int k = 0; k < 8; k++)
                        {
                            byte grey = (value & 1) != 0 ? (byte)255 : (byte)0;
                            if (x + k < width)
                            {
                                image.SetPixel(x + k, y, grey, grey, grey, 255);
                            }

                            value >>= 1;
                        }
                    }
                }

                break;
            }

            case SporeBitmapTypes.EightBitImage:
            {
                int p = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        byte grey = p < data.Length ? data[p++] : (byte)0;
                        image.SetPixel(x, y, grey, grey, grey, 255);
                    }
                }

                break;
            }

            case SporeBitmapTypes.ThirtyTwoBitImage:
            {
                int p = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        byte b = p < data.Length ? data[p++] : (byte)0;
                        byte g = p < data.Length ? data[p++] : (byte)0;
                        byte r = p < data.Length ? data[p++] : (byte)0;
                        byte a = p < data.Length ? data[p++] : (byte)0;
                        image.SetPixel(x, y, r, g, b, a);
                    }
                }

                break;
            }

            case SporeBitmapTypes.FortyEightBitImage:
            {
                int p = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        byte r = ReadHighByte(data, ref p);
                        byte g = ReadHighByte(data, ref p);
                        byte b = ReadHighByte(data, ref p);
                        image.SetPixel(x, y, r, g, b, 255);
                    }
                }

                break;
            }

            default:
                throw new NotSupportedException($"Unsupported bitmap type 0x{type:X8}.");
        }

        return image;
    }

    private static byte ReadHighByte(byte[] data, ref int p)
    {
        int low = p < data.Length ? data[p++] : 0;
        int high = p < data.Length ? data[p++] : 0;
        return (byte)high;
    }
}
