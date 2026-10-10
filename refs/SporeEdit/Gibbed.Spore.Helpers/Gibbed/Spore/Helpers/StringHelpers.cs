using System.Globalization;

namespace Gibbed.Spore.Helpers
{
	public static class StringHelpers
	{
		public static uint FNV(this string input)
		{
			string text = input.ToLower();
			uint num = 2166136261u;
			for (int i = 0; i < text.Length; i++)
			{
				num *= 16777619;
				num ^= text[i];
			}
			return num;
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
}
