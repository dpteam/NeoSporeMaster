using System.Drawing;
using System.Xml;

namespace ICSharpCode.TextEditor.Document;

public class HighlightBackground : HighlightColor
{
	private Image backgroundImage;

	public Image BackgroundImage => backgroundImage;

	public HighlightBackground(XmlElement el)
		: base(el)
	{
		if (el.Attributes["image"] != null)
		{
			backgroundImage = new Bitmap(el.Attributes["image"].InnerText);
		}
	}

	public HighlightBackground(Color color, Color backgroundcolor, bool bold, bool italic)
		: base(color, backgroundcolor, bold, italic)
	{
	}

	public HighlightBackground(string systemColor, string systemBackgroundColor, bool bold, bool italic)
		: base(systemColor, systemBackgroundColor, bold, italic)
	{
	}
}
