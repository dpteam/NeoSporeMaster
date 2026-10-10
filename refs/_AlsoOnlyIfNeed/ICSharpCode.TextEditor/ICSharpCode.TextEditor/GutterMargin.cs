using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor;

public class GutterMargin : AbstractMargin, IDisposable
{
	private StringFormat numberStringFormat = (StringFormat)StringFormat.GenericTypographic.Clone();

	public static Cursor RightLeftCursor;

	public override Cursor Cursor => RightLeftCursor;

	public override Size Size => new Size(textArea.TextView.WideSpaceWidth * Math.Max(3, (int)Math.Log10(textArea.Document.TotalNumberOfLines) + 1), -1);

	public override bool IsVisible => textArea.TextEditorProperties.ShowLineNumbers;

	static GutterMargin()
	{
		Stream manifestResourceStream = Assembly.GetCallingAssembly().GetManifestResourceStream("ICSharpCode.TextEditor.Resources.RightArrow.cur");
		if (manifestResourceStream == null)
		{
			throw new Exception("could not find cursor resource");
		}
		RightLeftCursor = new Cursor(manifestResourceStream);
		manifestResourceStream.Close();
	}

	public void Dispose()
	{
		numberStringFormat.Dispose();
	}

	public GutterMargin(TextArea textArea)
		: base(textArea)
	{
		numberStringFormat.LineAlignment = StringAlignment.Far;
		numberStringFormat.FormatFlags = StringFormatFlags.FitBlackBox | StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoWrap | StringFormatFlags.NoClip;
	}

	public override void Paint(Graphics g, Rectangle rect)
	{
		if (rect.Width <= 0 || rect.Height <= 0)
		{
			return;
		}
		HighlightColor colorFor = textArea.Document.HighlightingStrategy.GetColorFor("LineNumbers");
		int fontHeight = textArea.TextView.FontHeight;
		Brush brush = (textArea.Enabled ? BrushRegistry.GetBrush(colorFor.BackgroundColor) : SystemBrushes.InactiveBorder);
		Brush brush2 = BrushRegistry.GetBrush(colorFor.Color);
		for (int i = 0; i < (DrawingPosition.Height + textArea.TextView.VisibleLineDrawingRemainder) / fontHeight + 1; i++)
		{
			int y = drawingPosition.Y + fontHeight * i - textArea.TextView.VisibleLineDrawingRemainder;
			Rectangle rectangle = new Rectangle(drawingPosition.X, y, drawingPosition.Width, fontHeight);
			if (rect.IntersectsWith(rectangle))
			{
				g.FillRectangle(brush, rectangle);
				int firstLogicalLine = textArea.Document.GetFirstLogicalLine(textArea.Document.GetVisibleLine(textArea.TextView.FirstVisibleLine) + i);
				if (firstLogicalLine < textArea.Document.TotalNumberOfLines)
				{
					g.DrawString((firstLogicalLine + 1).ToString(), colorFor.GetFont(TextEditorProperties.FontContainer), brush2, rectangle, numberStringFormat);
				}
			}
		}
	}

	public override void HandleMouseDown(Point mousepos, MouseButtons mouseButtons)
	{
		textArea.SelectionManager.selectFrom.where = 1;
		int logicalLine = textArea.TextView.GetLogicalLine(mousepos.Y);
		if (logicalLine < 0 || logicalLine >= textArea.Document.TotalNumberOfLines)
		{
			return;
		}
		if ((Control.ModifierKeys & Keys.Shift) != 0)
		{
			if (!textArea.SelectionManager.HasSomethingSelected && logicalLine != textArea.Caret.Position.Y)
			{
				if (logicalLine >= textArea.Caret.Position.Y)
				{
					TextLocation position = textArea.Caret.Position;
					if (logicalLine < textArea.Document.TotalNumberOfLines - 1)
					{
						textArea.SelectionManager.SetSelection(new DefaultSelection(textArea.Document, position, new TextLocation(0, logicalLine + 1)));
						textArea.Caret.Position = new TextLocation(0, logicalLine + 1);
					}
					else
					{
						textArea.SelectionManager.SetSelection(new DefaultSelection(textArea.Document, position, new TextLocation(textArea.Document.GetLineSegment(logicalLine).Length + 1, logicalLine)));
						textArea.Caret.Position = new TextLocation(textArea.Document.GetLineSegment(logicalLine).Length + 1, logicalLine);
					}
				}
				else
				{
					TextLocation position = textArea.Caret.Position;
					textArea.SelectionManager.SetSelection(new DefaultSelection(textArea.Document, position, new TextLocation(position.X, position.Y)));
					textArea.SelectionManager.ExtendSelection(new TextLocation(position.X, position.Y), new TextLocation(0, logicalLine));
					textArea.Caret.Position = new TextLocation(0, logicalLine);
				}
			}
			else
			{
				MouseEventArgs e = new MouseEventArgs(mouseButtons, 1, mousepos.X, mousepos.Y, 0);
				textArea.RaiseMouseMove(e);
			}
		}
		else
		{
			textArea.mousepos = mousepos;
			TextLocation position = new TextLocation(0, logicalLine);
			textArea.SelectionManager.ClearSelection();
			if (logicalLine < textArea.Document.TotalNumberOfLines - 1)
			{
				textArea.SelectionManager.SetSelection(new DefaultSelection(textArea.Document, position, new TextLocation(position.X, position.Y + 1)));
				textArea.Caret.Position = new TextLocation(position.X, position.Y + 1);
			}
			else
			{
				textArea.SelectionManager.SetSelection(new DefaultSelection(textArea.Document, new TextLocation(0, logicalLine), new TextLocation(textArea.Document.GetLineSegment(logicalLine).Length + 1, position.Y)));
				textArea.Caret.Position = new TextLocation(textArea.Document.GetLineSegment(logicalLine).Length + 1, position.Y);
			}
		}
	}
}
