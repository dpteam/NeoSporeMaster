using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor;

public class FoldMargin : AbstractMargin
{
	private int selectedFoldLine = -1;

	public override Size Size => new Size(textArea.TextView.FontHeight, -1);

	public override bool IsVisible => textArea.TextEditorProperties.EnableFolding;

	public FoldMargin(TextArea textArea)
		: base(textArea)
	{
	}

	public override void Paint(Graphics g, Rectangle rect)
	{
		if (rect.Width <= 0 || rect.Height <= 0)
		{
			return;
		}
		HighlightColor colorFor = textArea.Document.HighlightingStrategy.GetColorFor("LineNumbers");
		textArea.Document.HighlightingStrategy.GetColorFor("FoldLine");
		for (int i = 0; i < (DrawingPosition.Height + textArea.TextView.VisibleLineDrawingRemainder) / textArea.TextView.FontHeight + 1; i++)
		{
			Rectangle rectangle = new Rectangle(DrawingPosition.X, DrawingPosition.Top + i * textArea.TextView.FontHeight - textArea.TextView.VisibleLineDrawingRemainder, DrawingPosition.Width, textArea.TextView.FontHeight);
			if (rect.IntersectsWith(rectangle))
			{
				if (textArea.Document.TextEditorProperties.ShowLineNumbers)
				{
					g.FillRectangle(BrushRegistry.GetBrush(textArea.Enabled ? colorFor.BackgroundColor : SystemColors.InactiveBorder), new Rectangle(rectangle.X + 1, rectangle.Y, rectangle.Width - 1, rectangle.Height));
					g.DrawLine(BrushRegistry.GetDotPen(colorFor.Color, colorFor.BackgroundColor), drawingPosition.X, rectangle.Y, drawingPosition.X, rectangle.Bottom);
				}
				else
				{
					g.FillRectangle(BrushRegistry.GetBrush(textArea.Enabled ? colorFor.BackgroundColor : SystemColors.InactiveBorder), rectangle);
				}
				int firstLogicalLine = textArea.Document.GetFirstLogicalLine(textArea.TextView.FirstPhysicalLine + i);
				if (firstLogicalLine < textArea.Document.TotalNumberOfLines)
				{
					PaintFoldMarker(g, firstLogicalLine, rectangle);
				}
			}
		}
	}

	private bool SelectedFoldingFrom(List<FoldMarker> list)
	{
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (selectedFoldLine == list[i].StartLine)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void PaintFoldMarker(Graphics g, int lineNumber, Rectangle drawingRectangle)
	{
		HighlightColor colorFor = textArea.Document.HighlightingStrategy.GetColorFor("FoldLine");
		HighlightColor colorFor2 = textArea.Document.HighlightingStrategy.GetColorFor("SelectedFoldLine");
		List<FoldMarker> foldingsWithStart = textArea.Document.FoldingManager.GetFoldingsWithStart(lineNumber);
		List<FoldMarker> foldingsContainsLineNumber = textArea.Document.FoldingManager.GetFoldingsContainsLineNumber(lineNumber);
		List<FoldMarker> foldingsWithEnd = textArea.Document.FoldingManager.GetFoldingsWithEnd(lineNumber);
		bool flag = foldingsWithStart.Count > 0;
		bool flag2 = foldingsContainsLineNumber.Count > 0;
		bool flag3 = foldingsWithEnd.Count > 0;
		bool flag4 = SelectedFoldingFrom(foldingsWithStart);
		bool flag5 = SelectedFoldingFrom(foldingsContainsLineNumber);
		bool flag6 = SelectedFoldingFrom(foldingsWithEnd);
		int num = (int)Math.Round((float)textArea.TextView.FontHeight * 0.57f);
		num -= num % 2;
		int num2 = drawingRectangle.Y + (drawingRectangle.Height - num) / 2;
		int num3 = drawingRectangle.X + (drawingRectangle.Width - num) / 2 + num / 2;
		if (flag)
		{
			bool flag7 = true;
			bool flag8 = false;
			foreach (FoldMarker item in foldingsWithStart)
			{
				if (item.IsFolded)
				{
					flag7 = false;
				}
				else
				{
					flag8 = item.EndLine > item.StartLine;
				}
			}
			bool flag9 = false;
			foreach (FoldMarker item2 in foldingsWithEnd)
			{
				if (item2.EndLine > item2.StartLine && !item2.IsFolded)
				{
					flag9 = true;
				}
			}
			DrawFoldMarker(g, new RectangleF(drawingRectangle.X + (drawingRectangle.Width - num) / 2, num2, num, num), flag7, flag4);
			if (flag2 || flag9)
			{
				g.DrawLine(BrushRegistry.GetPen(flag5 ? colorFor2.Color : colorFor.Color), num3, drawingRectangle.Top, num3, num2 - 1);
			}
			if (flag2 || flag8)
			{
				g.DrawLine(BrushRegistry.GetPen((flag6 || (flag4 && flag7) || flag5) ? colorFor2.Color : colorFor.Color), num3, num2 + num + 1, num3, drawingRectangle.Bottom);
			}
		}
		else if (flag3)
		{
			int num4 = drawingRectangle.Top + drawingRectangle.Height / 2;
			g.DrawLine(BrushRegistry.GetPen(flag6 ? colorFor2.Color : colorFor.Color), num3, num4, num3 + num / 2, num4);
			g.DrawLine(BrushRegistry.GetPen((flag5 || flag6) ? colorFor2.Color : colorFor.Color), num3, drawingRectangle.Top, num3, num4);
			if (flag2)
			{
				g.DrawLine(BrushRegistry.GetPen(flag5 ? colorFor2.Color : colorFor.Color), num3, num4 + 1, num3, drawingRectangle.Bottom);
			}
		}
		else if (flag2)
		{
			g.DrawLine(BrushRegistry.GetPen(flag5 ? colorFor2.Color : colorFor.Color), num3, drawingRectangle.Top, num3, drawingRectangle.Bottom);
		}
	}

	public override void HandleMouseMove(Point mousepos, MouseButtons mouseButtons)
	{
		bool enableFolding = textArea.Document.TextEditorProperties.EnableFolding;
		int lineNumber = (mousepos.Y + textArea.VirtualTop.Y) / textArea.TextView.FontHeight;
		int firstLogicalLine = textArea.Document.GetFirstLogicalLine(lineNumber);
		if (enableFolding && firstLogicalLine >= 0 && firstLogicalLine + 1 < textArea.Document.TotalNumberOfLines)
		{
			List<FoldMarker> foldingsWithStart = textArea.Document.FoldingManager.GetFoldingsWithStart(firstLogicalLine);
			int num = selectedFoldLine;
			if (foldingsWithStart.Count > 0)
			{
				selectedFoldLine = firstLogicalLine;
			}
			else
			{
				selectedFoldLine = -1;
			}
			if (num != selectedFoldLine)
			{
				textArea.Refresh(this);
			}
		}
	}

	public override void HandleMouseDown(Point mousepos, MouseButtons mouseButtons)
	{
		bool enableFolding = textArea.Document.TextEditorProperties.EnableFolding;
		int lineNumber = (mousepos.Y + textArea.VirtualTop.Y) / textArea.TextView.FontHeight;
		int firstLogicalLine = textArea.Document.GetFirstLogicalLine(lineNumber);
		textArea.Focus();
		if (!enableFolding || firstLogicalLine < 0 || firstLogicalLine + 1 >= textArea.Document.TotalNumberOfLines)
		{
			return;
		}
		List<FoldMarker> foldingsWithStart = textArea.Document.FoldingManager.GetFoldingsWithStart(firstLogicalLine);
		foreach (FoldMarker item in foldingsWithStart)
		{
			item.IsFolded = !item.IsFolded;
		}
		textArea.Document.FoldingManager.NotifyFoldingsChanged(EventArgs.Empty);
	}

	public override void HandleMouseLeave(EventArgs e)
	{
		if (selectedFoldLine != -1)
		{
			selectedFoldLine = -1;
			textArea.Refresh(this);
		}
	}

	private void DrawFoldMarker(Graphics g, RectangleF rectangle, bool isOpened, bool isSelected)
	{
		HighlightColor colorFor = textArea.Document.HighlightingStrategy.GetColorFor("FoldMarker");
		HighlightColor colorFor2 = textArea.Document.HighlightingStrategy.GetColorFor("FoldLine");
		HighlightColor colorFor3 = textArea.Document.HighlightingStrategy.GetColorFor("SelectedFoldLine");
		Rectangle rect = new Rectangle((int)rectangle.X, (int)rectangle.Y, (int)rectangle.Width, (int)rectangle.Height);
		g.FillRectangle(BrushRegistry.GetBrush(colorFor.BackgroundColor), rect);
		g.DrawRectangle(BrushRegistry.GetPen(isSelected ? colorFor3.Color : colorFor.Color), rect);
		int num = (int)Math.Round((double)rectangle.Height / 8.0) + 1;
		int num2 = rect.Height / 2 + rect.Height % 2;
		g.DrawLine(BrushRegistry.GetPen(colorFor2.BackgroundColor), rectangle.X + (float)num, rectangle.Y + (float)num2, rectangle.X + rectangle.Width - (float)num, rectangle.Y + (float)num2);
		if (!isOpened)
		{
			g.DrawLine(BrushRegistry.GetPen(colorFor2.BackgroundColor), rectangle.X + (float)num2, rectangle.Y + (float)num, rectangle.X + (float)num2, rectangle.Y + rectangle.Height - (float)num);
		}
	}
}
