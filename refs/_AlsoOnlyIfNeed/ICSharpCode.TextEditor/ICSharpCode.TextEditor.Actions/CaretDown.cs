using System.Drawing;

namespace ICSharpCode.TextEditor.Actions;

public class CaretDown : AbstractEditAction
{
	public override void Execute(TextArea textArea)
	{
		TextLocation position = textArea.Caret.Position;
		int y = position.Y;
		int visibleLine = textArea.Document.GetVisibleLine(y);
		if (visibleLine < textArea.Document.GetVisibleLine(textArea.Document.TotalNumberOfLines))
		{
			Point mousePosition = new Point(textArea.TextView.GetDrawingXPos(y, position.X), textArea.TextView.DrawingPosition.Y + (visibleLine + 1) * textArea.TextView.FontHeight - textArea.TextView.TextArea.VirtualTop.Y);
			textArea.Caret.Position = textArea.TextView.GetLogicalPosition(mousePosition);
			textArea.SetCaretToDesiredColumn();
		}
	}
}
