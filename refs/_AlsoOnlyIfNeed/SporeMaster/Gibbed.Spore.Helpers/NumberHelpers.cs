namespace Gibbed.Spore.Helpers;

public static class NumberHelpers
{
	public static short Swap(this short value)
	{
		ushort swapped = (ushort)((0xFF & (value >> 8)) | (0xFF00 & (value << 8)));
		return (short)swapped;
	}

	public static ushort Swap(this ushort value)
	{
		return (ushort)((0xFF & (value >> 8)) | (0xFF00 & (value << 8)));
	}

	public static int Swap(this int value)
	{
		return (0xFF & (value >>> 24)) | (0xFF00 & (value >>> 8)) | (0xFF0000 & (value << 8)) | (-16777216 & (value << 24));
	}

	public static uint Swap(this uint value)
	{
		return (0xFF & (value >> 24)) | (0xFF00 & (value >> 8)) | (0xFF0000 & (value << 8)) | (0xFF000000u & (value << 24));
	}

	public static long Swap(this long value)
	{
		return (0xFF & (value >>> 56)) | (0xFF00 & (value >>> 40)) | (0xFF0000 & (value >>> 24)) | (0xFF000000u & (value >>> 8)) | (0xFF00000000L & (value << 8)) | (0xFF0000000000L & (value << 24)) | (0xFF000000000000L & (value << 40)) | (-72057594037927936L & (value << 56));
	}

	public static ulong Swap(this ulong value)
	{
		return (0xFF & (value >> 56)) | (0xFF00 & (value >> 40)) | (0xFF0000 & (value >> 24)) | (0xFF000000u & (value >> 8)) | (0xFF00000000L & (value << 8)) | (0xFF0000000000L & (value << 24)) | (0xFF000000000000L & (value << 40)) | (0xFF00000000000000uL & (value << 56));
	}
}
