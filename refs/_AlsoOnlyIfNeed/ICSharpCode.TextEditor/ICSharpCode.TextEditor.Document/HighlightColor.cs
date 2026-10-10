using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Xml;

namespace ICSharpCode.TextEditor.Document;

public class HighlightColor
{
	private bool systemColor;

	private string systemColorName;

	private bool systemBgColor;

	private string systemBgColorName;

	private Color color;

	private Color backgroundcolor = Color.WhiteSmoke;

	private bool bold;

	private bool italic;

	private bool hasForeground;

	private bool hasBackground;

	public bool HasForeground => hasForeground;

	public bool HasBackground => hasBackground;

	public bool Bold => bold;

	public bool Italic => italic;

	public Color BackgroundColor
	{
		get
		{
			if (!systemBgColor)
			{
				return backgroundcolor;
			}
			return ParseColorString(systemBgColorName);
		}
	}

	public Color Color
	{
		get
		{
			if (!systemColor)
			{
				return color;
			}
			return ParseColorString(systemColorName);
		}
	}

	public Font GetFont(FontContainer fontContainer)
	{
		if (Bold)
		{
			if (!Italic)
			{
				return fontContainer.BoldFont;
			}
			return fontContainer.BoldItalicFont;
		}
		if (!Italic)
		{
			return fontContainer.RegularFont;
		}
		return fontContainer.ItalicFont;
	}

	private Color ParseColorString(string colorName)
	{
		string[] array = colorName.Split('*');
		PropertyInfo property = typeof(SystemColors).GetProperty(array[0], BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
		Color result = (Color)property.GetValue(null, null);
		if (array.Length == 2)
		{
			double num = double.Parse(array[1]) / 100.0;
			result = Color.FromArgb((int)((double)(int)result.R * num), (int)((double)(int)result.G * num), (int)((double)(int)result.B * num));
		}
		return result;
	}

	public HighlightColor(XmlElement el)
	{
		if (el.Attributes["bold"] != null)
		{
			bold = bool.Parse(el.Attributes["bold"].InnerText);
		}
		if (el.Attributes["italic"] != null)
		{
			italic = bool.Parse(el.Attributes["italic"].InnerText);
		}
		if (el.Attributes["color"] != null)
		{
			string innerText = el.Attributes["color"].InnerText;
			if (innerText[0] == '#')
			{
				color = ParseColor(innerText);
			}
			else if (innerText.StartsWith("SystemColors."))
			{
				systemColor = true;
				systemColorName = innerText.Substring("SystemColors.".Length);
			}
			else
			{
				color = (Color)Color.GetType().InvokeMember(innerText, BindingFlags.GetProperty, null, Color, new object[0]);
			}
			hasForeground = true;
		}
		else
		{
			color = Color.Transparent;
		}
		if (el.Attributes["bgcolor"] != null)
		{
			string innerText2 = el.Attributes["bgcolor"].InnerText;
			if (innerText2[0] == '#')
			{
				backgroundcolor = ParseColor(innerText2);
			}
			else if (innerText2.StartsWith("SystemColors."))
			{
				systemBgColor = true;
				systemBgColorName = innerText2.Substring("SystemColors.".Length);
			}
			else
			{
				backgroundcolor = (Color)Color.GetType().InvokeMember(innerText2, BindingFlags.GetProperty, null, Color, new object[0]);
			}
			hasBackground = true;
		}
	}

	public HighlightColor(XmlElement el, HighlightColor defaultColor)
	{
		if (el.Attributes["bold"] != null)
		{
			bold = bool.Parse(el.Attributes["bold"].InnerText);
		}
		else
		{
			bold = defaultColor.Bold;
		}
		if (el.Attributes["italic"] != null)
		{
			italic = bool.Parse(el.Attributes["italic"].InnerText);
		}
		else
		{
			italic = defaultColor.Italic;
		}
		if (el.Attributes["color"] != null)
		{
			string innerText = el.Attributes["color"].InnerText;
			if (innerText[0] == '#')
			{
				color = ParseColor(innerText);
			}
			else if (innerText.StartsWith("SystemColors."))
			{
				systemColor = true;
				systemColorName = innerText.Substring("SystemColors.".Length);
			}
			else
			{
				color = (Color)Color.GetType().InvokeMember(innerText, BindingFlags.GetProperty, null, Color, new object[0]);
			}
			hasForeground = true;
		}
		else
		{
			color = defaultColor.color;
		}
		if (el.Attributes["bgcolor"] != null)
		{
			string innerText2 = el.Attributes["bgcolor"].InnerText;
			if (innerText2[0] == '#')
			{
				backgroundcolor = ParseColor(innerText2);
			}
			else if (innerText2.StartsWith("SystemColors."))
			{
				systemBgColor = true;
				systemBgColorName = innerText2.Substring("SystemColors.".Length);
			}
			else
			{
				backgroundcolor = (Color)Color.GetType().InvokeMember(innerText2, BindingFlags.GetProperty, null, Color, new object[0]);
			}
			hasBackground = true;
		}
		else
		{
			backgroundcolor = defaultColor.BackgroundColor;
		}
	}

	public HighlightColor(Color color, bool bold, bool italic)
	{
		hasForeground = true;
		this.color = color;
		this.bold = bold;
		this.italic = italic;
	}

	public HighlightColor(Color color, Color backgroundcolor, bool bold, bool italic)
	{
		hasForeground = true;
		hasBackground = true;
		this.color = color;
		this.backgroundcolor = backgroundcolor;
		this.bold = bold;
		this.italic = italic;
	}

	public HighlightColor(string systemColor, string systemBackgroundColor, bool bold, bool italic)
	{
		hasForeground = true;
		hasBackground = true;
		this.systemColor = true;
		systemColorName = systemColor;
		systemBgColor = true;
		systemBgColorName = systemBackgroundColor;
		this.bold = bold;
		this.italic = italic;
	}

	private static Color ParseColor(string c)
	{
		int alpha = 255;
		int num = 0;
		if (c.Length > 7)
		{
			num = 2;
			alpha = int.Parse(c.Substring(1, 2), NumberStyles.HexNumber);
		}
		int red = int.Parse(c.Substring(1 + num, 2), NumberStyles.HexNumber);
		int green = int.Parse(c.Substring(3 + num, 2), NumberStyles.HexNumber);
		int blue = int.Parse(c.Substring(5 + num, 2), NumberStyles.HexNumber);
		return Color.FromArgb(alpha, red, green, blue);
	}

	public override string ToString()
	{
		return string.Concat("[HighlightColor: Bold = ", Bold, ", Italic = ", Italic, ", Color = ", Color, ", BackgroundColor = ", BackgroundColor, "]");
	}
}
