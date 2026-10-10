using System.Xml;

namespace ICSharpCode.TextEditor.Document;

public class PrevMarker
{
	private string what;

	private HighlightColor color;

	private bool markMarker;

	public string What => what;

	public HighlightColor Color => color;

	public bool MarkMarker => markMarker;

	public PrevMarker(XmlElement mark)
	{
		color = new HighlightColor(mark);
		what = mark.InnerText;
		if (mark.Attributes["markmarker"] != null)
		{
			markMarker = bool.Parse(mark.Attributes["markmarker"].InnerText);
		}
	}
}
