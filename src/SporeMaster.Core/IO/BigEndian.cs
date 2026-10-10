using System.Buffers.Binary;

namespace SporeMaster.Core.IO;

/// <summary>
/// Big-endian read/write helpers. Spore property files mix endianness: the property
/// descriptor and most scalar values are big-endian, while composite values (keys, vectors,
/// colours, transforms) are little-endian. Both helper sets are provided so the property
/// code can be explicit about which one it needs.
/// </summary>
public static class BigEndian
{
    public static uint ReadU32BE(this Stream s)
    {
        Span<byte> b = stackalloc byte[4];
        Fill(s, b);
        return BinaryPrimitives.ReadUInt32BigEndian(b);
    }

    public static int ReadI32BE(this Stream s)
    {
        Span<byte> b = stackalloc byte[4];
        Fill(s, b);
        return BinaryPrimitives.ReadInt32BigEndian(b);
    }

    public static ushort ReadU16BE(this Stream s)
    {
        Span<byte> b = stackalloc byte[2];
        Fill(s, b);
        return BinaryPrimitives.ReadUInt16BigEndian(b);
    }

    public static short ReadI16BE(this Stream s)
    {
        Span<byte> b = stackalloc byte[2];
        Fill(s, b);
        return BinaryPrimitives.ReadInt16BigEndian(b);
    }

    public static ulong ReadU64BE(this Stream s)
    {
        Span<byte> b = stackalloc byte[8];
        Fill(s, b);
        return BinaryPrimitives.ReadUInt64BigEndian(b);
    }

    public static long ReadI64BE(this Stream s)
    {
        Span<byte> b = stackalloc byte[8];
        Fill(s, b);
        return BinaryPrimitives.ReadInt64BigEndian(b);
    }

    public static float ReadF32BE(this Stream s) =>
        BitConverter.Int32BitsToSingle(s.ReadI32BE());

    public static double ReadF64BE(this Stream s) =>
        BitConverter.Int64BitsToDouble(s.ReadI64BE());

    // Little-endian floats (used by composite property values).
    public static float ReadF32LE(this Stream s)
    {
        Span<byte> b = stackalloc byte[4];
        Fill(s, b);
        return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(b));
    }

    public static void WriteU32BE(this Stream s, uint value)
    {
        Span<byte> b = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(b, value);
        s.Write(b);
    }

    public static void WriteI32BE(this Stream s, int value)
    {
        Span<byte> b = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(b, value);
        s.Write(b);
    }

    public static void WriteU16BE(this Stream s, ushort value)
    {
        Span<byte> b = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(b, value);
        s.Write(b);
    }

    public static void WriteI16BE(this Stream s, short value)
    {
        Span<byte> b = stackalloc byte[2];
        BinaryPrimitives.WriteInt16BigEndian(b, value);
        s.Write(b);
    }

    public static void WriteU64BE(this Stream s, ulong value)
    {
        Span<byte> b = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(b, value);
        s.Write(b);
    }

    public static void WriteI64BE(this Stream s, long value)
    {
        Span<byte> b = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(b, value);
        s.Write(b);
    }

    public static void WriteF32BE(this Stream s, float value) => s.WriteI32BE(BitConverter.SingleToInt32Bits(value));

    public static void WriteF64BE(this Stream s, double value) => s.WriteI64BE(BitConverter.DoubleToInt64Bits(value));

    public static void WriteF32LE(this Stream s, float value)
    {
        Span<byte> b = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(b, BitConverter.SingleToInt32Bits(value));
        s.Write(b);
    }

    private static void Fill(Stream s, Span<byte> buffer)
    {
        int read = 0;
        while (read < buffer.Length)
        {
            int n = s.Read(buffer[read..]);
            if (n <= 0)
            {
                throw new EndOfStreamException();
            }

            read += n;
        }
    }
}
