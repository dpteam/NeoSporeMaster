using System;
using System.Drawing;

namespace ICSharpCode.TextEditor.Document;

public class FontContainer
{
	private Font defaultFont;

	private Font regularfont;

	private Font boldfont;

	private Font italicfont;

	private Font bolditalicfont;

	private static float twipsPerPixelY;

	public Font RegularFont => regularfont;

	public Font BoldFont => boldfont;

	public Font ItalicFont => italicfont;

	public Font BoldItalicFont => bolditalicfont;

	public static float TwipsPerPixelY
	{
		get
		{
			if (twipsPerPixelY == 0f)
			{
				using Bitmap image = new Bitmap(1, 1);
				using Graphics graphics = Graphics.FromImage(image);
				twipsPerPixelY = 1440f / graphics.DpiY;
			}
			return twipsPerPixelY;
		}
	}

	public Font DefaultFont
	{
		get
		{
			return defaultFont;
		}
		set
		{
			float num = (float)Math.Round(value.SizeInPoints * 20f / TwipsPerPixelY);
			defaultFont = value;
			regularfont = new Font(value.FontFamily, num * TwipsPerPixelY / 20f, FontStyle.Regular);
			boldfont = new Font(regularfont, FontStyle.Bold);
			italicfont = new Font(regularfont, FontStyle.Italic);
			bolditalicfont = new Font(regularfont, FontStyle.Bold | FontStyle.Italic);
		}
	}

	public static Font ParseFont(string font)
	{
		string[] array = font.Split(',', '=');
		return new Font(array[1], float.Parse(array[3]));
	}

	public FontContainer(Font defaultFont)
	{
		DefaultFont = defaultFont;
	}
}
