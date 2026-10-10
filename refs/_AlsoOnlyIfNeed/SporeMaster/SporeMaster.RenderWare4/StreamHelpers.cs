using System.IO;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

public static class StreamHelpers
{
	public static byte[] ReadBytes(this Stream r, int count)
	{
		byte[] a = new byte[count];
		if (r.Read(a, 0, count) != count)
		{
			throw new EndOfStreamException();
		}
		return a;
	}

	public static void WriteU32s(this Stream w, uint[] values)
	{
		foreach (uint v in values)
		{
			w.WriteU32(v);
		}
	}

	public static void ReadPadding(this Stream r, long pad)
	{
		if (pad < 0)
		{
			throw new ModelFormatException(r, "Negative padding", pad);
		}
		for (long i = 0L; i < pad; i++)
		{
			if (r.ReadByte() != 0)
			{
				throw new ModelFormatException(r, "Nonzero padding", null);
			}
		}
	}

	public static void WritePadding(this Stream w, long pad)
	{
		if (pad < 0)
		{
			throw new ModelFormatException(w, "Negative padding", pad);
		}
		for (long i = 0L; i < pad; i++)
		{
			w.WriteByte(0);
		}
	}

	public static void expect(this Stream r, uint value, string error)
	{
		uint actual = r.ReadU32();
		if (actual != value)
		{
			throw new ModelFormatException(r, error, actual);
		}
	}

	public static void expect(this Stream r, uint[] values, string error)
	{
		foreach (uint v in values)
		{
			r.expect(v, error);
		}
	}
}
