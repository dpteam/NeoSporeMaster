using System.Globalization;

namespace Gibbed.Spore.Helpers;

public static class StringHelpers
{
	public static uint FNV(this string input)
	{
		string lower = input.ToLower();
		uint rez = 2166136261u;
		for (int i = 0; i < lower.Length; i++)
		{
			rez *= 16777619;
			rez ^= lower[i];
		}
		return rez;
	}

	public static uint GetHexNumber(this string input)
	{
		if (input.StartsWith("0x"))
		{
			return uint.Parse(input.Substring(2), NumberStyles.AllowHexSpecifier);
		}
		return uint.Parse(input);
	}
}
