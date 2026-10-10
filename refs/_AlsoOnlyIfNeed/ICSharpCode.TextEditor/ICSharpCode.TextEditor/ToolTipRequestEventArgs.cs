using System.Drawing;

namespace ICSharpCode.TextEditor;

public class ToolTipRequestEventArgs
{
	private Point mousePosition;

	private TextLocation logicalPosition;

	private bool inDocument;

	internal string toolTipText;

	public Point MousePosition => mousePosition;

	public TextLocation LogicalPosition => logicalPosition;

	public bool InDocument => inDocument;

	public bool ToolTipShown => toolTipText != null;

	public void ShowToolTip(string text)
	{
		toolTipText = text;
	}

	public ToolTipRequestEventArgs(Point mousePosition, TextLocation logicalPosition, bool inDocument)
	{
		this.mousePosition = mousePosition;
		this.logicalPosition = logicalPosition;
		this.inDocument = inDocument;
	}
}
