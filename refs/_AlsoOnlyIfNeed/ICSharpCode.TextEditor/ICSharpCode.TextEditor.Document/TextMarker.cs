using System.Drawing;

namespace ICSharpCode.TextEditor.Document;

public class TextMarker : AbstractSegment
{
	private TextMarkerType textMarkerType;

	private Color color;

	private Color foreColor;

	private string toolTip;

	private bool overrideForeColor;

	public TextMarkerType TextMarkerType => textMarkerType;

	public Color Color => color;

	public Color ForeColor => foreColor;

	public bool OverrideForeColor => overrideForeColor;

	public bool IsReadOnly { get; set; }

	public string ToolTip
	{
		get
		{
			return toolTip;
		}
		set
		{
			toolTip = value;
		}
	}

	public int EndOffset => Offset + Length - 1;

	public TextMarker(int offset, int length, TextMarkerType textMarkerType)
		: this(offset, length, textMarkerType, Color.Red)
	{
	}

	public TextMarker(int offset, int length, TextMarkerType textMarkerType, Color color)
	{
		if (length < 1)
		{
			length = 1;
		}
		base.offset = offset;
		base.length = length;
		this.textMarkerType = textMarkerType;
		this.color = color;
	}

	public TextMarker(int offset, int length, TextMarkerType textMarkerType, Color color, Color foreColor)
	{
		if (length < 1)
		{
			length = 1;
		}
		base.offset = offset;
		base.length = length;
		this.textMarkerType = textMarkerType;
		this.color = color;
		this.foreColor = foreColor;
		overrideForeColor = true;
	}
}
