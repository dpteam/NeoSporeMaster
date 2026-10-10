using System.Buffers.Binary;
using System.Text;

namespace SporeMaster.Core.IO;

/// <summary>Little-endian binary helpers used by the DBPF reader and writer.</summary>
public static class StreamExtensions
{
    public static uint ReadU32(this Stream s)
    {
        Span<byte> b = stackalloc byte[4];
        ReadExactly(s, b);
        return BinaryPrimitives.ReadUInt32LittleEndian(b);
    }

    public static int ReadI32(this Stream s)
    {
        Span<byte> b = stackalloc byte[4];
        ReadExactly(s, b);
        return BinaryPrimitives.ReadInt32LittleEndian(b);
    }

    public static ushort ReadU16(this Stream s)
    {
        Span<byte> b = stackalloc byte[2];
        ReadExactly(s, b);
        return BinaryPrimitives.ReadUInt16LittleEndian(b);
    }

    public static short ReadI16(this Stream s)
    {
        Span<byte> b = stackalloc byte[2];
        ReadExactly(s, b);
        return BinaryPrimitives.ReadInt16LittleEndian(b);
    }

    public static ulong ReadU64(this Stream s)
    {
        Span<byte> b = stackalloc byte[8];
        ReadExactly(s, b);
        return BinaryPrimitives.ReadUInt64LittleEndian(b);
    }

    public static long ReadI64(this Stream s)
    {
        Span<byte> b = stackalloc byte[8];
        ReadExactly(s, b);
        return BinaryPrimitives.ReadInt64LittleEndian(b);
    }

    public static byte ReadU8(this Stream s)
    {
        int v = s.ReadByte();
        if (v < 0)
        {
            throw new EndOfStreamException();
        }

        return (byte)v;
    }

    public static byte[] ReadBytesExactly(this Stream s, int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        byte[] buffer = new byte[count];
        ReadExactly(s, buffer);
        return buffer;
    }

    public static void WriteU32(this Stream s, uint value)
    {
        Span<byte> b = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(b, value);
        s.Write(b);
    }

    public static void WriteI32(this Stream s, int value)
    {
        Span<byte> b = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(b, value);
        s.Write(b);
    }

    public static void WriteU16(this Stream s, ushort value)
    {
        Span<byte> b = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(b, value);
        s.Write(b);
    }

    public static void WriteI16(this Stream s, short value)
    {
        Span<byte> b = stackalloc byte[2];
        BinaryPrimitives.WriteInt16LittleEndian(b, value);
        s.Write(b);
    }

    public static void WriteU64(this Stream s, ulong value)
    {
        Span<byte> b = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(b, value);
        s.Write(b);
    }

    public static void WriteI64(this Stream s, long value)
    {
        Span<byte> b = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(b, value);
        s.Write(b);
    }

    public static void WriteZeroes(this Stream s, int count)
    {
        if (count <= 0)
        {
            return;
        }

        s.Write(new byte[count]);
    }

    public static string ReadAscii(this Stream s, int count) => Encoding.ASCII.GetString(s.ReadBytesExactly(count));

    private static void ReadExactly(Stream s, Span<byte> buffer)
    {
        int read = 0;
        while (read < buffer.Length)
        {
            int n = s.Read(buffer[read..]);
            if (n <= 0)
            {
                throw new EndOfStreamException($"Unexpected end of stream (wanted {buffer.Length} bytes, got {read}).");
            }

            read += n;
        }
    }
}
