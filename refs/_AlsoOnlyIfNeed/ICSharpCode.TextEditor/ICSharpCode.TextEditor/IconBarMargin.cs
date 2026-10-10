using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor;

public class IconBarMargin : AbstractMargin
{
	private const int iconBarWidth = 18;

	private static readonly Size iconBarSize = new Size(18, -1);

	public override Size Size => iconBarSize;

	public override bool IsVisible => textArea.TextEditorProperties.IsIconBarVisible;

	public IconBarMargin(TextArea textArea)
		: base(textArea)
	{
	}

	public override void Paint(Graphics g, Rectangle rect)
	{
		if (rect.Width <= 0 || rect.Height <= 0)
		{
			return;
		}
		g.FillRectangle(SystemBrushes.Control, new Rectangle(drawingPosition.X, rect.Top, drawingPosition.Width - 1, rect.Height));
		g.DrawLine(SystemPens.ControlDark, drawingPosition.Right - 1, rect.Top, drawingPosition.Right - 1, rect.Bottom);
		foreach (Bookmark mark in textArea.Document.BookmarkManager.Marks)
		{
			int visibleLine = textArea.Document.GetVisibleLine(mark.LineNumber);
			int fontHeight = textArea.TextView.FontHeight;
			int num = visibleLine * fontHeight - textArea.VirtualTop.Y;
			if (IsLineInsideRegion(num, num + fontHeight, rect.Y, rect.Bottom) && visibleLine != textArea.Document.GetVisibleLine(mark.LineNumber - 1))
			{
				mark.Draw(this, g, new Point(0, num));
			}
		}
		base.Paint(g, rect);
	}

	public override void HandleMouseDown(Point mousePos, MouseButtons mouseButtons)
	{
		int lineNumber = (mousePos.Y + textArea.VirtualTop.Y) / textArea.TextView.FontHeight;
		int firstLogicalLine = textArea.Document.GetFirstLogicalLine(lineNumber);
		if ((mouseButtons & MouseButtons.Right) == MouseButtons.Right && textArea.Caret.Line != firstLogicalLine)
		{
			textArea.Caret.Line = firstLogicalLine;
		}
		IList<Bookmark> marks = textArea.Document.BookmarkManager.Marks;
		List<Bookmark> list = new List<Bookmark>();
		int count = marks.Count;
		foreach (Bookmark item in marks)
		{
			if (item.LineNumber == firstLogicalLine)
			{
				list.Add(item);
			}
		}
		for (int num = list.Count - 1; num >= 0; num--)
		{
			Bookmark bookmark = list[num];
			if (bookmark.Click(textArea, new MouseEventArgs(mouseButtons, 1, mousePos.X, mousePos.Y, 0)))
			{
				if (count != marks.Count)
				{
					textArea.UpdateLine(firstLogicalLine);
				}
				return;
			}
		}
		base.HandleMouseDown(mousePos, mouseButtons);
	}

	public void DrawBreakpoint(Graphics g, int y, bool isEnabled, bool isHealthy)
	{
		int num = Math.Min(16, textArea.TextView.FontHeight);
		Rectangle rect = new Rectangle(1, y + (textArea.TextView.FontHeight - num) / 2, num, num);
		using GraphicsPath graphicsPath = new GraphicsPath();
		graphicsPath.AddEllipse(rect);
		using PathGradientBrush pathGradientBrush = new PathGradientBrush(graphicsPath);
		pathGradientBrush.CenterPoint = new PointF(rect.Left + rect.Width / 3, rect.Top + rect.Height / 3);
		pathGradientBrush.CenterColor = Color.MistyRose;
		Color[] surroundColors = new Color[1] { isHealthy ? Color.Firebrick : Color.Olive };
		pathGradientBrush.SurroundColors = surroundColors;
		if (isEnabled)
		{
			g.FillEllipse(pathGradientBrush, rect);
			return;
		}
		g.FillEllipse(SystemBrushes.Control, rect);
		using Pen pen = new Pen(pathGradientBrush);
		g.DrawEllipse(pen, new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2));
	}

	public void DrawBookmark(Graphics g, int y, bool isEnabled)
	{
		int num = textArea.TextView.FontHeight / 8;
		Rectangle r = new Rectangle(1, y + num, drawingPosition.Width - 4, textArea.TextView.FontHeight - num * 2);
		if (isEnabled)
		{
			using Brush b = new LinearGradientBrush(new Point(r.Left, r.Top), new Point(r.Right, r.Bottom), Color.SkyBlue, Color.White);
			FillRoundRect(g, b, r);
		}
		else
		{
			FillRoundRect(g, Brushes.White, r);
		}
		using Brush brush = new LinearGradientBrush(new Point(r.Left, r.Top), new Point(r.Right, r.Bottom), Color.SkyBlue, Color.Blue);
		using Pen p = new Pen(brush);
		DrawRoundRect(g, p, r);
	}

	public void DrawArrow(Graphics g, int y)
	{
		int num = textArea.TextView.FontHeight / 8;
		Rectangle r = new Rectangle(1, y + num, drawingPosition.Width - 4, textArea.TextView.FontHeight - num * 2);
		using (Brush b = new LinearGradientBrush(new Point(r.Left, r.Top), new Point(r.Right, r.Bottom), Color.LightYellow, Color.Yellow))
		{
			FillArrow(g, b, r);
		}
		using Brush brush = new LinearGradientBrush(new Point(r.Left, r.Top), new Point(r.Right, r.Bottom), Color.Yellow, Color.Brown);
		using Pen p = new Pen(brush);
		DrawArrow(g, p, r);
	}

	private GraphicsPath CreateArrowGraphicsPath(Rectangle r)
	{
		GraphicsPath graphicsPath = new GraphicsPath();
		int num = r.Width / 2;
		int num2 = r.Height / 2;
		graphicsPath.AddLine(r.X, r.Y + num2 / 2, r.X + num, r.Y + num2 / 2);
		graphicsPath.AddLine(r.X + num, r.Y + num2 / 2, r.X + num, r.Y);
		graphicsPath.AddLine(r.X + num, r.Y, r.Right, r.Y + num2);
		graphicsPath.AddLine(r.Right, r.Y + num2, r.X + num, r.Bottom);
		graphicsPath.AddLine(r.X + num, r.Bottom, r.X + num, r.Bottom - num2 / 2);
		graphicsPath.AddLine(r.X + num, r.Bottom - num2 / 2, r.X, r.Bottom - num2 / 2);
		graphicsPath.AddLine(r.X, r.Bottom - num2 / 2, r.X, r.Y + num2 / 2);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}

	private GraphicsPath CreateRoundRectGraphicsPath(Rectangle r)
	{
		GraphicsPath graphicsPath = new GraphicsPath();
		int num = r.Width / 2;
		graphicsPath.AddLine(r.X + num, r.Y, r.Right - num, r.Y);
		graphicsPath.AddArc(r.Right - num, r.Y, num, num, 270f, 90f);
		graphicsPath.AddLine(r.Right, r.Y + num, r.Right, r.Bottom - num);
		graphicsPath.AddArc(r.Right - num, r.Bottom - num, num, num, 0f, 90f);
		graphicsPath.AddLine(r.Right - num, r.Bottom, r.X + num, r.Bottom);
		graphicsPath.AddArc(r.X, r.Bottom - num, num, num, 90f, 90f);
		graphicsPath.AddLine(r.X, r.Bottom - num, r.X, r.Y + num);
		graphicsPath.AddArc(r.X, r.Y, num, num, 180f, 90f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}

	private void DrawRoundRect(Graphics g, Pen p, Rectangle r)
	{
		using GraphicsPath path = CreateRoundRectGraphicsPath(r);
		g.DrawPath(p, path);
	}

	private void FillRoundRect(Graphics g, Brush b, Rectangle r)
	{
		using GraphicsPath path = CreateRoundRectGraphicsPath(r);
		g.FillPath(b, path);
	}

	private void DrawArrow(Graphics g, Pen p, Rectangle r)
	{
		using GraphicsPath path = CreateArrowGraphicsPath(r);
		g.DrawPath(p, path);
	}

	private void FillArrow(Graphics g, Brush b, Rectangle r)
	{
		using GraphicsPath path = CreateArrowGraphicsPath(r);
		g.FillPath(b, path);
	}

	private static bool IsLineInsideRegion(int top, int bottom, int regionTop, int regionBottom)
	{
		if (top >= regionTop && top <= regionBottom)
		{
			return true;
		}
		if (regionTop > top && regionTop < bottom)
		{
			return true;
		}
		return false;
	}
}
