using System;
using System.IO;
using System.Text;

namespace Gibbed.Spore.Helpers;

public static class StreamHelpers
{
	public static bool ReadBoolean(this Stream stream)
	{
		if (stream.ReadU8() <= 0)
		{
			return false;
		}
		return true;
	}

	public static void WriteBoolean(this Stream stream, bool value)
	{
		stream.WriteU8((byte)(value ? 1u : 0u));
	}

	public static byte ReadU8(this Stream stream)
	{
		return (byte)stream.ReadByte();
	}

	public static void WriteU8(this Stream stream, byte value)
	{
		stream.WriteByte(value);
	}

	public static char ReadS8(this Stream stream)
	{
		return (char)stream.ReadByte();
	}

	public static void WriteS8(this Stream stream, char value)
	{
		stream.WriteByte((byte)value);
	}

	public static short ReadS16(this Stream stream)
	{
		byte[] data = new byte[2];
		stream.Read(data, 0, 2);
		return BitConverter.ToInt16(data, 0);
	}

	public static void WriteS16(this Stream stream, short value)
	{
		byte[] data = BitConverter.GetBytes(value);
		stream.Write(data, 0, 2);
	}

	public static short ReadS16BE(this Stream stream)
	{
		byte[] data = new byte[2];
		stream.Read(data, 0, 2);
		return BitConverter.ToInt16(data, 0).Swap();
	}

	public static void WriteS16BE(this Stream stream, short value)
	{
		byte[] data = BitConverter.GetBytes(value.Swap());
		stream.Write(data, 0, 2);
	}

	public static ushort ReadU16(this Stream stream)
	{
		byte[] data = new byte[2];
		stream.Read(data, 0, 2);
		return BitConverter.ToUInt16(data, 0);
	}

	public static void WriteU16(this Stream stream, ushort value)
	{
		byte[] data = BitConverter.GetBytes(value);
		stream.Write(data, 0, 2);
	}

	public static ushort ReadU16BE(this Stream stream)
	{
		byte[] data = new byte[2];
		stream.Read(data, 0, 2);
		return BitConverter.ToUInt16(data, 0).Swap();
	}

	public static void WriteU16BE(this Stream stream, ushort value)
	{
		byte[] data = BitConverter.GetBytes(value.Swap());
		stream.Write(data, 0, 2);
	}

	public static uint ReadU24BE(this Stream stream)
	{
		byte[] data = new byte[4];
		stream.Read(data, 1, 3);
		return BitConverter.ToUInt32(data, 0).Swap();
	}

	public static void WriteU24BE(this Stream stream, uint value)
	{
		byte[] data = BitConverter.GetBytes(value.Swap());
		stream.Write(data, 1, 3);
	}

	public static int ReadS32(this Stream stream)
	{
		byte[] data = new byte[4];
		stream.Read(data, 0, 4);
		return BitConverter.ToInt32(data, 0);
	}

	public static void WriteS32(this Stream stream, int value)
	{
		byte[] data = BitConverter.GetBytes(value);
		stream.Write(data, 0, 4);
	}

	public static int ReadS32BE(this Stream stream)
	{
		byte[] data = new byte[4];
		stream.Read(data, 0, 4);
		return BitConverter.ToInt32(data, 0).Swap();
	}

	public static void WriteS32BE(this Stream stream, int value)
	{
		byte[] data = BitConverter.GetBytes(value.Swap());
		stream.Write(data, 0, 4);
	}

	public static uint ReadU32(this Stream stream)
	{
		byte[] data = new byte[4];
		stream.Read(data, 0, 4);
		return BitConverter.ToUInt32(data, 0);
	}

	public static void WriteU32(this Stream stream, uint value)
	{
		byte[] data = BitConverter.GetBytes(value);
		stream.Write(data, 0, 4);
	}

	public static uint ReadU32BE(this Stream stream)
	{
		byte[] data = new byte[4];
		stream.Read(data, 0, 4);
		return BitConverter.ToUInt32(data, 0).Swap();
	}

	public static void WriteU32BE(this Stream stream, uint value)
	{
		byte[] data = BitConverter.GetBytes(value.Swap());
		stream.Write(data, 0, 4);
	}

	public static long ReadS64(this Stream stream)
	{
		byte[] data = new byte[8];
		stream.Read(data, 0, 8);
		return BitConverter.ToInt64(data, 0);
	}

	public static long ReadS64BE(this Stream stream)
	{
		byte[] data = new byte[8];
		stream.Read(data, 0, 8);
		return BitConverter.ToInt64(data, 0).Swap();
	}

	public static ulong ReadU64(this Stream stream)
	{
		byte[] data = new byte[8];
		stream.Read(data, 0, 8);
		return BitConverter.ToUInt64(data, 0);
	}

	public static ulong ReadU64BE(this Stream stream)
	{
		byte[] data = new byte[8];
		stream.Read(data, 0, 8);
		return BitConverter.ToUInt64(data, 0).Swap();
	}

	public static float ReadF32(this Stream stream)
	{
		byte[] data = new byte[4];
		stream.Read(data, 0, 4);
		return BitConverter.ToSingle(data, 0);
	}

	public static void WriteF32(this Stream stream, float value)
	{
		byte[] data = BitConverter.GetBytes(value);
		stream.Write(data, 0, 4);
	}

	public static float ReadF32BE(this Stream stream)
	{
		byte[] data = new byte[4];
		stream.Read(data, 0, 4);
		uint value = BitConverter.ToUInt32(data, 0).Swap();
		data = BitConverter.GetBytes(value);
		return BitConverter.ToSingle(data, 0);
	}

	public static void WriteF32BE(this Stream stream, float value)
	{
		byte[] data = BitConverter.GetBytes(value);
		uint swappedvalue = BitConverter.ToUInt32(data, 0).Swap();
		data = BitConverter.GetBytes(swappedvalue);
		stream.Write(data, 0, 4);
	}

	public static double ReadF64(this Stream stream)
	{
		byte[] data = new byte[8];
		stream.Read(data, 0, 8);
		return BitConverter.ToDouble(data, 0);
	}

	public static void WriteF64(this Stream stream, double value)
	{
		byte[] data = BitConverter.GetBytes(value);
		stream.Write(data, 0, 8);
	}

	public static double ReadF64BE(this Stream stream)
	{
		return BitConverter.Int64BitsToDouble((long)stream.ReadU64BE());
	}

	public static void WriteF64BE(this Stream stream, double value)
	{
		byte[] data = BitConverter.GetBytes(value);
		ulong swappedvalue = BitConverter.ToUInt64(data, 0).Swap();
		data = BitConverter.GetBytes(swappedvalue);
		stream.Write(data, 0, 8);
	}

	public static string ReadASCII(this Stream stream, uint size)
	{
		byte[] data = new byte[size];
		stream.Read(data, 0, data.Length);
		return Encoding.ASCII.GetString(data);
	}

	public static void WriteASCII(this Stream stream, string value)
	{
		byte[] data = Encoding.ASCII.GetBytes(value);
		stream.Write(data, 0, data.Length);
	}

	public static void ReadRefPackCompressionHeader(this Stream stream)
	{
		byte[] header = new byte[2];
		stream.Read(header, 0, header.Length);
		if ((header[0] & 0x3E) == 16)
		{
			_ = header[1];
			_ = 251;
		}
		bool isLong = (header[0] & 0x80) != 0;
		bool hasMore = (header[0] & 1) != 0;
		byte[] data = new byte[(isLong ? 4 : 3) * ((!hasMore) ? 1 : 2)];
		stream.Read(data, 0, data.Length);
		uint realLength = (uint)(((data[0] << 8) + data[1] << 8) + data[2]);
		if (isLong)
		{
			realLength = (realLength << 8) + data[3];
		}
	}

	public static byte[] RefPackDecompress(this Stream stream, uint compressedSize, uint decompressedSize)
	{
		long baseOffset = stream.Position;
		byte[] outputData = new byte[decompressedSize];
		uint offset = 0u;
		stream.ReadRefPackCompressionHeader();
		while (stream.Position < baseOffset + compressedSize)
		{
			bool stop = false;
			uint plainSize = 0u;
			uint copySize = 0u;
			uint copyOffset = 0u;
			byte prefix = stream.ReadU8();
			if (prefix >= 192)
			{
				if (prefix >= 224)
				{
					if (prefix >= 252)
					{
						plainSize = (uint)(prefix & 3);
						stop = true;
					}
					else
					{
						plainSize = (uint)(((prefix & 0x1F) + 1) * 4);
					}
				}
				else
				{
					byte[] extra = new byte[3];
					stream.Read(extra, 0, extra.Length);
					plainSize = (uint)(prefix & 3);
					copySize = (uint)((((prefix & 0xC) << 6) | extra[2]) + 5);
					copyOffset = (uint)((((((prefix & 0x10) << 4) | extra[0]) << 8) | extra[1]) + 1);
				}
			}
			else if (prefix >= 128)
			{
				byte[] extra2 = new byte[2];
				stream.Read(extra2, 0, extra2.Length);
				plainSize = (uint)(extra2[0] >> 6);
				copySize = (uint)((prefix & 0x3F) + 4);
				copyOffset = (uint)((((extra2[0] & 0x3F) << 8) | extra2[1]) + 1);
			}
			else
			{
				byte extra3 = stream.ReadU8();
				plainSize = (uint)(prefix & 3);
				copySize = (uint)(((prefix & 0x1C) >> 2) + 3);
				copyOffset = (uint)((((prefix & 0x60) << 3) | extra3) + 1);
			}
			if (plainSize != 0)
			{
				stream.Read(outputData, (int)offset, (int)plainSize);
				offset += plainSize;
			}
			if (copySize != 0)
			{
				for (uint i = 0u; i < copySize; i++)
				{
					outputData[offset + i] = outputData[offset - copyOffset + i];
				}
				offset += copySize;
			}
			if (stop)
			{
				break;
			}
		}
		return outputData;
	}
}
