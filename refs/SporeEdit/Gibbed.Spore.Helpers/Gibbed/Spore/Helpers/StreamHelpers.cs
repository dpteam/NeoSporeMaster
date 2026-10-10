using System;
using System.IO;
using System.Text;

namespace Gibbed.Spore.Helpers
{
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
			byte[] array = new byte[2];
			stream.Read(array, 0, 2);
			return BitConverter.ToInt16(array, 0);
		}

		public static void WriteS16(this Stream stream, short value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			stream.Write(bytes, 0, 2);
		}

		public static short ReadS16BE(this Stream stream)
		{
			byte[] array = new byte[2];
			stream.Read(array, 0, 2);
			return BitConverter.ToInt16(array, 0).Swap();
		}

		public static void WriteS16BE(this Stream stream, short value)
		{
			byte[] bytes = BitConverter.GetBytes(value.Swap());
			stream.Write(bytes, 0, 2);
		}

		public static ushort ReadU16(this Stream stream)
		{
			byte[] array = new byte[2];
			stream.Read(array, 0, 2);
			return BitConverter.ToUInt16(array, 0);
		}

		public static void WriteU16(this Stream stream, ushort value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			stream.Write(bytes, 0, 2);
		}

		public static ushort ReadU16BE(this Stream stream)
		{
			byte[] array = new byte[2];
			stream.Read(array, 0, 2);
			return BitConverter.ToUInt16(array, 0).Swap();
		}

		public static void WriteU16BE(this Stream stream, ushort value)
		{
			byte[] bytes = BitConverter.GetBytes(value.Swap());
			stream.Write(bytes, 0, 2);
		}

		public static uint ReadU24BE(this Stream stream)
		{
			byte[] array = new byte[4];
			stream.Read(array, 1, 3);
			return BitConverter.ToUInt32(array, 0).Swap();
		}

		public static void WriteU24BE(this Stream stream, uint value)
		{
			byte[] bytes = BitConverter.GetBytes(value.Swap());
			stream.Write(bytes, 1, 3);
		}

		public static int ReadS32(this Stream stream)
		{
			byte[] array = new byte[4];
			stream.Read(array, 0, 4);
			return BitConverter.ToInt32(array, 0);
		}

		public static void WriteS32(this Stream stream, int value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			stream.Write(bytes, 0, 4);
		}

		public static int ReadS32BE(this Stream stream)
		{
			byte[] array = new byte[4];
			stream.Read(array, 0, 4);
			return BitConverter.ToInt32(array, 0).Swap();
		}

		public static void WriteS32BE(this Stream stream, int value)
		{
			byte[] bytes = BitConverter.GetBytes(value.Swap());
			stream.Write(bytes, 0, 4);
		}

		public static uint ReadU32(this Stream stream)
		{
			byte[] array = new byte[4];
			stream.Read(array, 0, 4);
			return BitConverter.ToUInt32(array, 0);
		}

		public static void WriteU32(this Stream stream, uint value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			stream.Write(bytes, 0, 4);
		}

		public static uint ReadU32BE(this Stream stream)
		{
			byte[] array = new byte[4];
			stream.Read(array, 0, 4);
			return BitConverter.ToUInt32(array, 0).Swap();
		}

		public static void WriteU32BE(this Stream stream, uint value)
		{
			byte[] bytes = BitConverter.GetBytes(value.Swap());
			stream.Write(bytes, 0, 4);
		}

		public static long ReadS64(this Stream stream)
		{
			byte[] array = new byte[8];
			stream.Read(array, 0, 8);
			return BitConverter.ToInt64(array, 0);
		}

		public static long ReadS64BE(this Stream stream)
		{
			byte[] array = new byte[8];
			stream.Read(array, 0, 8);
			return BitConverter.ToInt64(array, 0).Swap();
		}

		public static ulong ReadU64(this Stream stream)
		{
			byte[] array = new byte[8];
			stream.Read(array, 0, 8);
			return BitConverter.ToUInt64(array, 0);
		}

		public static ulong ReadU64BE(this Stream stream)
		{
			byte[] array = new byte[8];
			stream.Read(array, 0, 8);
			return BitConverter.ToUInt64(array, 0).Swap();
		}

		public static float ReadF32(this Stream stream)
		{
			byte[] array = new byte[4];
			stream.Read(array, 0, 4);
			return BitConverter.ToSingle(array, 0);
		}

		public static void WriteF32(this Stream stream, float value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			stream.Write(bytes, 0, 4);
		}

		public static float ReadF32BE(this Stream stream)
		{
			byte[] array = new byte[4];
			stream.Read(array, 0, 4);
			uint value = BitConverter.ToUInt32(array, 0).Swap();
			array = BitConverter.GetBytes(value);
			return BitConverter.ToSingle(array, 0);
		}

		public static void WriteF32BE(this Stream stream, float value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			uint value2 = BitConverter.ToUInt32(bytes, 0).Swap();
			bytes = BitConverter.GetBytes(value2);
			stream.Write(bytes, 0, 4);
		}

		public static double ReadF64(this Stream stream)
		{
			byte[] array = new byte[8];
			stream.Read(array, 0, 8);
			return BitConverter.ToDouble(array, 0);
		}

		public static void WriteF64(this Stream stream, double value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			stream.Write(bytes, 0, 8);
		}

		public static double ReadF64BE(this Stream stream)
		{
			return BitConverter.Int64BitsToDouble((long)stream.ReadU64BE());
		}

		public static void WriteF64BE(this Stream stream, double value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			ulong value2 = BitConverter.ToUInt64(bytes, 0).Swap();
			bytes = BitConverter.GetBytes(value2);
			stream.Write(bytes, 0, 8);
		}

		public static string ReadASCII(this Stream stream, uint size)
		{
			byte[] array = new byte[size];
			stream.Read(array, 0, array.Length);
			return Encoding.ASCII.GetString(array);
		}

		public static void WriteASCII(this Stream stream, string value)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(value);
			stream.Write(bytes, 0, bytes.Length);
		}

		public static void ReadRefPackCompressionHeader(this Stream stream)
		{
			byte[] array = new byte[2];
			stream.Read(array, 0, array.Length);
			if ((array[0] & 0x3E) == 16)
			{
				_ = array[1];
				_ = 251;
			}
			bool flag = (array[0] & 0x80) != 0;
			bool flag2 = (array[0] & 1) != 0;
			byte[] array2 = new byte[(flag ? 4 : 3) * ((!flag2) ? 1 : 2)];
			stream.Read(array2, 0, array2.Length);
			uint num = (uint)(((array2[0] << 8) + array2[1] << 8) + array2[2]);
			if (flag)
			{
				num = (num << 8) + array2[3];
			}
		}

		public static byte[] RefPackDecompress(this Stream stream, uint compressedSize, uint decompressedSize)
		{
			long position = stream.Position;
			byte[] array = new byte[decompressedSize];
			uint num = 0u;
			stream.ReadRefPackCompressionHeader();
			while (stream.Position < position + compressedSize)
			{
				bool flag = false;
				uint num2 = 0u;
				uint num3 = 0u;
				uint num4 = 0u;
				byte b = stream.ReadU8();
				if (b >= 192)
				{
					if (b >= 224)
					{
						if (b >= 252)
						{
							num2 = b & 3u;
							flag = true;
						}
						else
						{
							num2 = (uint)(((b & 0x1F) + 1) * 4);
						}
					}
					else
					{
						byte[] array2 = new byte[3];
						stream.Read(array2, 0, array2.Length);
						num2 = b & 3u;
						num3 = (uint)((((b & 0xC) << 6) | array2[2]) + 5);
						num4 = (uint)((((((b & 0x10) << 4) | array2[0]) << 8) | array2[1]) + 1);
					}
				}
				else if (b >= 128)
				{
					byte[] array3 = new byte[2];
					stream.Read(array3, 0, array3.Length);
					num2 = (uint)(array3[0] >> 6);
					num3 = (uint)((b & 0x3F) + 4);
					num4 = (uint)((((array3[0] & 0x3F) << 8) | array3[1]) + 1);
				}
				else
				{
					byte b2 = stream.ReadU8();
					num2 = b & 3u;
					num3 = (uint)(((b & 0x1C) >> 2) + 3);
					num4 = (uint)((((b & 0x60) << 3) | b2) + 1);
				}
				if (num2 != 0)
				{
					stream.Read(array, (int)num, (int)num2);
					num += num2;
				}
				if (num3 != 0)
				{
					for (uint num5 = 0u; num5 < num3; num5++)
					{
						array[num + num5] = array[num - num4 + num5];
					}
					num += num3;
				}
				if (flag)
				{
					break;
				}
			}
			return array;
		}
	}
}
