using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor;

public class TextView : AbstractMargin, IDisposable
{
	private struct MarkerToDraw(TextMarker marker, RectangleF drawingRect)
	{
		internal TextMarker marker = marker;

		internal RectangleF drawingRect = drawingRect;
	}

	private struct WordFontPair(string word, Font font)
	{
		private string word = word;

		private Font font = font;

		public override bool Equals(object obj)
		{
			WordFontPair wordFontPair = (WordFontPair)obj;
			if (!word.Equals(wordFontPair.word))
			{
				return false;
			}
			return font.Equals(wordFontPair.font);
		}

		public override int GetHashCode()
		{
			return word.GetHashCode() ^ font.GetHashCode();
		}
	}

	private const int additionalFoldTextSize = 1;

	private const int MaximumWordLength = 1000;

	private const int MaximumCacheSize = 2000;

	private const TextFormatFlags textFormatFlags = TextFormatFlags.NoPrefix | TextFormatFlags.PreserveGraphicsClipping | TextFormatFlags.NoPadding;

	private const int MinTabWidth = 4;

	private int fontHeight;

	private Highlight highlight;

	private int physicalColumn;

	private int spaceWidth;

	private int wideSpaceWidth;

	private Font lastFont;

	private List<MarkerToDraw> markersToDraw = new List<MarkerToDraw>();

	private Dictionary<WordFontPair, int> measureCache = new Dictionary<WordFontPair, int>();

	private Dictionary<Font, Dictionary<char, int>> fontBoundCharWidth = new Dictionary<Font, Dictionary<char, int>>();

	public Highlight Highlight
	{
		get
		{
			return highlight;
		}
		set
		{
			highlight = value;
		}
	}

	public int FirstPhysicalLine => textArea.VirtualTop.Y / fontHeight;

	public int LineHeightRemainder => textArea.VirtualTop.Y % fontHeight;

	public int FirstVisibleLine
	{
		get
		{
			return textArea.Document.GetFirstLogicalLine(textArea.VirtualTop.Y / fontHeight);
		}
		set
		{
			if (FirstVisibleLine != value)
			{
				textArea.VirtualTop = new Point(textArea.VirtualTop.X, textArea.Document.GetVisibleLine(value) * fontHeight);
			}
		}
	}

	public int VisibleLineDrawingRemainder => textArea.VirtualTop.Y % fontHeight;

	public int FontHeight => fontHeight;

	public int VisibleLineCount => 1 + DrawingPosition.Height / fontHeight;

	public int VisibleColumnCount => DrawingPosition.Width / WideSpaceWidth - 1;

	public int SpaceWidth => spaceWidth;

	public int WideSpaceWidth => wideSpaceWidth;

	public void Dispose()
	{
		measureCache.Clear();
	}

	public TextView(TextArea textArea)
		: base(textArea)
	{
		base.Cursor = Cursors.IBeam;
		OptionsChanged();
	}

	private static int GetFontHeight(Font font)
	{
		int height = TextRenderer.MeasureText("_", font).Height;
		int val = (int)Math.Ceiling(font.GetHeight());
		return Math.Max(height, val) + 1;
	}

	public void OptionsChanged()
	{
		lastFont = TextEditorProperties.FontContainer.RegularFont;
		fontHeight = GetFontHeight(lastFont);
		spaceWidth = Math.Max(GetWidth(' ', lastFont), 1);
		wideSpaceWidth = Math.Max(spaceWidth, GetWidth('x', lastFont));
	}

	public override void Paint(Graphics g, Rectangle rect)
	{
		if (rect.Width <= 0 || rect.Height <= 0)
		{
			return;
		}
		if (lastFont != TextEditorProperties.FontContainer.RegularFont)
		{
			OptionsChanged();
			textArea.Invalidate();
		}
		int x = textArea.VirtualTop.X;
		if (x > 0)
		{
			g.SetClip(DrawingPosition);
		}
		for (int i = 0; i < (DrawingPosition.Height + VisibleLineDrawingRemainder) / fontHeight + 1; i++)
		{
			Rectangle rectangle = new Rectangle(DrawingPosition.X - x, DrawingPosition.Top + i * fontHeight - VisibleLineDrawingRemainder, DrawingPosition.Width + x, fontHeight);
			if (rect.IntersectsWith(rectangle))
			{
				textArea.Document.GetVisibleLine(FirstVisibleLine);
				int firstLogicalLine = textArea.Document.GetFirstLogicalLine(textArea.Document.GetVisibleLine(FirstVisibleLine) + i);
				PaintDocumentLine(g, firstLogicalLine, rectangle);
			}
		}
		DrawMarkerDraw(g);
		if (x > 0)
		{
			g.ResetClip();
		}
		textArea.Caret.PaintCaret(g);
	}

	private void PaintDocumentLine(Graphics g, int lineNumber, Rectangle lineRectangle)
	{
		Brush bgColorBrush = GetBgColorBrush(lineNumber);
		Brush brush = (textArea.Enabled ? bgColorBrush : SystemBrushes.InactiveBorder);
		if (lineNumber >= textArea.Document.TotalNumberOfLines)
		{
			g.FillRectangle(brush, lineRectangle);
			if (TextEditorProperties.ShowInvalidLines)
			{
				DrawInvalidLineMarker(g, lineRectangle.Left, lineRectangle.Top);
			}
			if (TextEditorProperties.ShowVerticalRuler)
			{
				DrawVerticalRuler(g, lineRectangle);
			}
			return;
		}
		int num = lineRectangle.X;
		int num2 = 0;
		physicalColumn = 0;
		if (TextEditorProperties.EnableFolding)
		{
			while (true)
			{
				List<FoldMarker> foldedFoldingsWithStartAfterColumn = textArea.Document.FoldingManager.GetFoldedFoldingsWithStartAfterColumn(lineNumber, num2 - 1);
				if (foldedFoldingsWithStartAfterColumn == null || foldedFoldingsWithStartAfterColumn.Count <= 0)
				{
					if (lineNumber < textArea.Document.TotalNumberOfLines)
					{
						num = PaintLinePart(g, lineNumber, num2, textArea.Document.GetLineSegment(lineNumber).Length, lineRectangle, num);
					}
					break;
				}
				FoldMarker foldMarker = foldedFoldingsWithStartAfterColumn[0];
				foreach (FoldMarker item in foldedFoldingsWithStartAfterColumn)
				{
					if (item.StartColumn < foldMarker.StartColumn)
					{
						foldMarker = item;
					}
				}
				foldedFoldingsWithStartAfterColumn.Clear();
				num = PaintLinePart(g, lineNumber, num2, foldMarker.StartColumn, lineRectangle, num);
				num2 = foldMarker.EndColumn;
				lineNumber = foldMarker.EndLine;
				if (lineNumber >= textArea.Document.TotalNumberOfLines)
				{
					break;
				}
				ColumnRange selectionAtLine = textArea.SelectionManager.GetSelectionAtLine(lineNumber);
				bool drawSelected = ColumnRange.WholeColumn.Equals(selectionAtLine) || (foldMarker.StartColumn >= selectionAtLine.StartColumn && foldMarker.EndColumn <= selectionAtLine.EndColumn);
				num = PaintFoldingText(g, lineNumber, num, lineRectangle, foldMarker.FoldText, drawSelected);
			}
		}
		else
		{
			num = PaintLinePart(g, lineNumber, 0, textArea.Document.GetLineSegment(lineNumber).Length, lineRectangle, num);
		}
		if (lineNumber < textArea.Document.TotalNumberOfLines)
		{
			ColumnRange selectionAtLine2 = textArea.SelectionManager.GetSelectionAtLine(lineNumber);
			LineSegment lineSegment = textArea.Document.GetLineSegment(lineNumber);
			HighlightColor colorFor = textArea.Document.HighlightingStrategy.GetColorFor("Selection");
			bool flag = selectionAtLine2.EndColumn > lineSegment.Length || ColumnRange.WholeColumn.Equals(selectionAtLine2);
			if (TextEditorProperties.ShowEOLMarker)
			{
				HighlightColor colorFor2 = textArea.Document.HighlightingStrategy.GetColorFor("EOLMarkers");
				num += DrawEOLMarker(g, colorFor2.Color, flag ? bgColorBrush : brush, num, lineRectangle.Y);
			}
			else if (flag)
			{
				g.FillRectangle(BrushRegistry.GetBrush(colorFor.BackgroundColor), new RectangleF(num, lineRectangle.Y, WideSpaceWidth, lineRectangle.Height));
				num += WideSpaceWidth;
			}
			Brush brush2 = ((flag && TextEditorProperties.AllowCaretBeyondEOL) ? bgColorBrush : brush);
			g.FillRectangle(brush2, new RectangleF(num, lineRectangle.Y, lineRectangle.Width - num + lineRectangle.X, lineRectangle.Height));
		}
		if (TextEditorProperties.ShowVerticalRuler)
		{
			DrawVerticalRuler(g, lineRectangle);
		}
	}

	private bool DrawLineMarkerAtLine(int lineNumber)
	{
		if (lineNumber == textArea.Caret.Line)
		{
			return textArea.MotherTextAreaControl.TextEditorProperties.LineViewerStyle == LineViewerStyle.FullRow;
		}
		return false;
	}

	private Brush GetBgColorBrush(int lineNumber)
	{
		if (DrawLineMarkerAtLine(lineNumber))
		{
			HighlightColor colorFor = textArea.Document.HighlightingStrategy.GetColorFor("CaretMarker");
			return BrushRegistry.GetBrush(colorFor.Color);
		}
		HighlightColor colorFor2 = textArea.Document.HighlightingStrategy.GetColorFor("Default");
		Color backgroundColor = colorFor2.BackgroundColor;
		return BrushRegistry.GetBrush(backgroundColor);
	}

	private int PaintFoldingText(Graphics g, int lineNumber, int physicalXPos, Rectangle lineRectangle, string text, bool drawSelected)
	{
		HighlightColor colorFor = textArea.Document.HighlightingStrategy.GetColorFor("Selection");
		Brush brush = (drawSelected ? BrushRegistry.GetBrush(colorFor.BackgroundColor) : GetBgColorBrush(lineNumber));
		Brush brush2 = (textArea.Enabled ? brush : SystemBrushes.InactiveBorder);
		Font regularFont = textArea.TextEditorProperties.FontContainer.RegularFont;
		int num = MeasureStringWidth(g, text, regularFont) + 1;
		Rectangle rect = new Rectangle(physicalXPos, lineRectangle.Y, num, lineRectangle.Height - 1);
		g.FillRectangle(brush2, rect);
		physicalColumn += text.Length;
		DrawString(g, text, regularFont, drawSelected ? colorFor.Color : Color.Gray, rect.X + 1, rect.Y);
		g.DrawRectangle(BrushRegistry.GetPen(drawSelected ? Color.DarkGray : Color.Gray), rect.X, rect.Y, rect.Width, rect.Height);
		return physicalXPos + num + 1;
	}

	private void DrawMarker(Graphics g, TextMarker marker, RectangleF drawingRect)
	{
		markersToDraw.Add(new MarkerToDraw(marker, drawingRect));
	}

	private void DrawMarkerDraw(Graphics g)
	{
		foreach (MarkerToDraw item in markersToDraw)
		{
			TextMarker marker = item.marker;
			RectangleF drawingRect = item.drawingRect;
			float num = drawingRect.Bottom - 1f;
			switch (marker.TextMarkerType)
			{
			case TextMarkerType.Underlined:
				g.DrawLine(BrushRegistry.GetPen(marker.Color), drawingRect.X, num, drawingRect.Right, num);
				break;
			case TextMarkerType.WaveLine:
			{
				int num2 = (int)drawingRect.X % 6;
				for (float num3 = (int)drawingRect.X - num2; num3 < drawingRect.Right; num3 += 6f)
				{
					g.DrawLine(BrushRegistry.GetPen(marker.Color), num3, num + 3f - 4f, num3 + 3f, num + 1f - 4f);
					if (num3 + 3f < drawingRect.Right)
					{
						g.DrawLine(BrushRegistry.GetPen(marker.Color), num3 + 3f, num + 1f - 4f, num3 + 6f, num + 3f - 4f);
					}
				}
				break;
			}
			case TextMarkerType.SolidBlock:
				g.FillRectangle(BrushRegistry.GetBrush(marker.Color), drawingRect);
				break;
			}
		}
		markersToDraw.Clear();
	}

	private Brush GetMarkerBrushAt(int offset, int length, ref Color foreColor, out IList<TextMarker> markers)
	{
		markers = Document.MarkerStrategy.GetMarkers(offset, length);
		foreach (TextMarker marker in markers)
		{
			if (marker.TextMarkerType == TextMarkerType.SolidBlock)
			{
				if (marker.OverrideForeColor)
				{
					foreColor = marker.ForeColor;
				}
				return BrushRegistry.GetBrush(marker.Color);
			}
		}
		return null;
	}

	private int PaintLinePart(Graphics g, int lineNumber, int startColumn, int endColumn, Rectangle lineRectangle, int physicalXPos)
	{
		bool flag = DrawLineMarkerAtLine(lineNumber);
		Brush brush = (textArea.Enabled ? GetBgColorBrush(lineNumber) : SystemBrushes.InactiveBorder);
		HighlightColor colorFor = textArea.Document.HighlightingStrategy.GetColorFor("Selection");
		ColumnRange selectionAtLine = textArea.SelectionManager.GetSelectionAtLine(lineNumber);
		HighlightColor colorFor2 = textArea.Document.HighlightingStrategy.GetColorFor("TabMarkers");
		HighlightColor colorFor3 = textArea.Document.HighlightingStrategy.GetColorFor("SpaceMarkers");
		LineSegment lineSegment = textArea.Document.GetLineSegment(lineNumber);
		Brush brush2 = BrushRegistry.GetBrush(colorFor.BackgroundColor);
		if (lineSegment.Words == null)
		{
			return physicalXPos;
		}
		int num = 0;
		TextWord textWord = null;
		FontContainer fontContainer = TextEditorProperties.FontContainer;
		for (int i = 0; i < lineSegment.Words.Count; i++)
		{
			TextWord word = lineSegment.Words[i];
			if (num < startColumn)
			{
				num += word.Length;
				continue;
			}
			while (num < endColumn && physicalXPos < lineRectangle.Right)
			{
				int num2 = num + word.Length - 1;
				Color foreColor = word.Type switch
				{
					TextWordType.Space => colorFor3.Color, 
					TextWordType.Tab => colorFor2.Color, 
					_ => word.Color, 
				};
				Brush brush3 = GetMarkerBrushAt(lineSegment.Offset + num, word.Length, ref foreColor, out var markers);
				if (word.Length > 1)
				{
					int num3 = int.MaxValue;
					if (highlight != null)
					{
						if (highlight.OpenBrace.Y == lineNumber && highlight.OpenBrace.X >= num && highlight.OpenBrace.X <= num2)
						{
							num3 = Math.Min(num3, highlight.OpenBrace.X - num);
						}
						if (highlight.CloseBrace.Y == lineNumber && highlight.CloseBrace.X >= num && highlight.CloseBrace.X <= num2)
						{
							num3 = Math.Min(num3, highlight.CloseBrace.X - num);
						}
						if (num3 == 0)
						{
							num3 = 1;
						}
					}
					if (endColumn < num2)
					{
						num3 = Math.Min(num3, endColumn - num);
					}
					if (selectionAtLine.StartColumn > num && selectionAtLine.StartColumn <= num2)
					{
						num3 = Math.Min(num3, selectionAtLine.StartColumn - num);
					}
					else if (selectionAtLine.EndColumn > num && selectionAtLine.EndColumn <= num2)
					{
						num3 = Math.Min(num3, selectionAtLine.EndColumn - num);
					}
					foreach (TextMarker item in markers)
					{
						int num4 = item.Offset - lineSegment.Offset;
						int num5 = item.EndOffset - lineSegment.Offset + 1;
						if (num4 > num && num4 <= num2)
						{
							num3 = Math.Min(num3, num4 - num);
						}
						else if (num5 > num && num5 <= num2)
						{
							num3 = Math.Min(num3, num5 - num);
						}
					}
					if (num3 != int.MaxValue)
					{
						if (textWord != null)
						{
							throw new ApplicationException("split part invalid: first part cannot be splitted further");
						}
						textWord = TextWord.Split(ref word, num3);
						continue;
					}
				}
				if (ColumnRange.WholeColumn.Equals(selectionAtLine) || (selectionAtLine.StartColumn <= num && selectionAtLine.EndColumn > num2))
				{
					brush3 = brush2;
					if (colorFor.HasForeground)
					{
						foreColor = colorFor.Color;
					}
				}
				else if (flag)
				{
					brush3 = brush;
				}
				if (brush3 == null)
				{
					brush3 = ((word.SyntaxColor == null || !word.SyntaxColor.HasBackground) ? brush : BrushRegistry.GetBrush(word.SyntaxColor.BackgroundColor));
				}
				RectangleF rectangleF;
				if (word.Type == TextWordType.Space)
				{
					physicalColumn++;
					rectangleF = new RectangleF(physicalXPos, lineRectangle.Y, SpaceWidth, lineRectangle.Height);
					g.FillRectangle(brush3, rectangleF);
					if (TextEditorProperties.ShowSpaces)
					{
						DrawSpaceMarker(g, foreColor, physicalXPos, lineRectangle.Y);
					}
					physicalXPos += SpaceWidth;
				}
				else if (word.Type == TextWordType.Tab)
				{
					physicalColumn += TextEditorProperties.TabIndent;
					physicalColumn = physicalColumn / TextEditorProperties.TabIndent * TextEditorProperties.TabIndent;
					int num6 = (physicalXPos + 4 - lineRectangle.X) / WideSpaceWidth / TextEditorProperties.TabIndent * WideSpaceWidth * TextEditorProperties.TabIndent + lineRectangle.X;
					num6 += WideSpaceWidth * TextEditorProperties.TabIndent;
					rectangleF = new RectangleF(physicalXPos, lineRectangle.Y, num6 - physicalXPos, lineRectangle.Height);
					g.FillRectangle(brush3, rectangleF);
					if (TextEditorProperties.ShowTabs)
					{
						DrawTabMarker(g, foreColor, physicalXPos, lineRectangle.Y);
					}
					physicalXPos = num6;
				}
				else
				{
					int num7 = DrawDocumentWord(g, word.Word, new Point(physicalXPos, lineRectangle.Y), word.GetFont(fontContainer), foreColor, brush3);
					rectangleF = new RectangleF(physicalXPos, lineRectangle.Y, num7, lineRectangle.Height);
					physicalXPos += num7;
				}
				foreach (TextMarker item2 in markers)
				{
					if (item2.TextMarkerType != TextMarkerType.SolidBlock)
					{
						DrawMarker(g, item2, rectangleF);
					}
				}
				if (highlight != null && ((highlight.OpenBrace.Y == lineNumber && highlight.OpenBrace.X == num) || (highlight.CloseBrace.Y == lineNumber && highlight.CloseBrace.X == num)))
				{
					DrawBracketHighlight(g, new Rectangle((int)rectangleF.X, lineRectangle.Y, (int)rectangleF.Width - 1, lineRectangle.Height - 1));
				}
				num += word.Length;
				if (textWord != null)
				{
					word = textWord;
					textWord = null;
					continue;
				}
				goto IL_067c;
			}
			break;
			IL_067c:;
		}
		if (physicalXPos < lineRectangle.Right && endColumn >= lineSegment.Length)
		{
			IList<TextMarker> markers2 = Document.MarkerStrategy.GetMarkers(lineSegment.Offset + lineSegment.Length);
			foreach (TextMarker item3 in markers2)
			{
				if (item3.TextMarkerType != TextMarkerType.SolidBlock)
				{
					DrawMarker(g, item3, new RectangleF(physicalXPos, lineRectangle.Y, WideSpaceWidth, lineRectangle.Height));
				}
			}
		}
		return physicalXPos;
	}

	private int DrawDocumentWord(Graphics g, string word, Point position, Font font, Color foreColor, Brush backBrush)
	{
		if (word == null || word.Length == 0)
		{
			return 0;
		}
		if (word.Length > 1000)
		{
			int num = 0;
			for (int i = 0; i < word.Length; i += 1000)
			{
				Point position2 = position;
				position2.X += num;
				num = ((i + 1000 >= word.Length) ? (num + DrawDocumentWord(g, word.Substring(i, word.Length - i), position2, font, foreColor, backBrush)) : (num + DrawDocumentWord(g, word.Substring(i, 1000), position2, font, foreColor, backBrush)));
			}
			return num;
		}
		int num2 = MeasureStringWidth(g, word, font);
		g.FillRectangle(backBrush, new RectangleF(position.X, position.Y, num2 + 1, FontHeight));
		DrawString(g, word, font, foreColor, position.X, position.Y);
		return num2;
	}

	private int MeasureStringWidth(Graphics g, string word, Font font)
	{
		if (word == null || word.Length == 0)
		{
			return 0;
		}
		int num;
		if (word.Length > 1000)
		{
			num = 0;
			for (int i = 0; i < word.Length; i += 1000)
			{
				num = ((i + 1000 >= word.Length) ? (num + MeasureStringWidth(g, word.Substring(i, word.Length - i), font)) : (num + MeasureStringWidth(g, word.Substring(i, 1000), font)));
			}
			return num;
		}
		if (measureCache.TryGetValue(new WordFontPair(word, font), out num))
		{
			return num;
		}
		if (measureCache.Count > 2000)
		{
			measureCache.Clear();
		}
		num = TextRenderer.MeasureText(g, word, font, new Size(32767, 32767), TextFormatFlags.NoPrefix | TextFormatFlags.PreserveGraphicsClipping | TextFormatFlags.NoPadding).Width;
		measureCache.Add(new WordFontPair(word, font), num);
		return num;
	}

	public int GetWidth(char ch, Font font)
	{
		if (!fontBoundCharWidth.ContainsKey(font))
		{
			fontBoundCharWidth.Add(font, new Dictionary<char, int>());
		}
		if (!fontBoundCharWidth[font].ContainsKey(ch))
		{
			using (Graphics g = textArea.CreateGraphics())
			{
				return GetWidth(g, ch, font);
			}
		}
		return fontBoundCharWidth[font][ch];
	}

	public int GetWidth(Graphics g, char ch, Font font)
	{
		if (!fontBoundCharWidth.ContainsKey(font))
		{
			fontBoundCharWidth.Add(font, new Dictionary<char, int>());
		}
		if (!fontBoundCharWidth[font].ContainsKey(ch))
		{
			fontBoundCharWidth[font].Add(ch, MeasureStringWidth(g, ch.ToString(), font));
		}
		return fontBoundCharWidth[font][ch];
	}

	public int GetVisualColumn(int logicalLine, int logicalColumn)
	{
		int column = 0;
		using Graphics g = textArea.CreateGraphics();
		CountColumns(ref column, 0, logicalColumn, logicalLine, g);
		return column;
	}

	public int GetVisualColumnFast(LineSegment line, int logicalColumn)
	{
		int offset = line.Offset;
		int tabIndent = Document.TextEditorProperties.TabIndent;
		int num = 0;
		for (int i = 0; i < logicalColumn; i++)
		{
			char c = ((i < line.Length) ? Document.GetCharAt(offset + i) : ' ');
			char c2 = c;
			if (c2 == '\t')
			{
				num += tabIndent;
				num = num / tabIndent * tabIndent;
			}
			else
			{
				num++;
			}
		}
		return num;
	}

	public TextLocation GetLogicalPosition(Point mousePosition)
	{
		FoldMarker inFoldMarker;
		return GetLogicalColumn(GetLogicalLine(mousePosition.Y), mousePosition.X, out inFoldMarker);
	}

	public TextLocation GetLogicalPosition(int visualPosX, int visualPosY)
	{
		FoldMarker inFoldMarker;
		return GetLogicalColumn(GetLogicalLine(visualPosY), visualPosX, out inFoldMarker);
	}

	public FoldMarker GetFoldMarkerFromPosition(int visualPosX, int visualPosY)
	{
		GetLogicalColumn(GetLogicalLine(visualPosY), visualPosX, out var inFoldMarker);
		return inFoldMarker;
	}

	public int GetLogicalLine(int visualPosY)
	{
		int lineNumber = Math.Max(0, (visualPosY + textArea.VirtualTop.Y) / fontHeight);
		return Document.GetFirstLogicalLine(lineNumber);
	}

	internal TextLocation GetLogicalColumn(int lineNumber, int visualPosX, out FoldMarker inFoldMarker)
	{
		visualPosX += textArea.VirtualTop.X;
		inFoldMarker = null;
		if (lineNumber >= Document.TotalNumberOfLines)
		{
			return new TextLocation(visualPosX / WideSpaceWidth, lineNumber);
		}
		if (visualPosX <= 0)
		{
			return new TextLocation(0, lineNumber);
		}
		int num = 0;
		int drawingPos = 0;
		int logicalColumnInternal;
		using (Graphics g = textArea.CreateGraphics())
		{
			do
			{
				LineSegment lineSegment = Document.GetLineSegment(lineNumber);
				FoldMarker foldMarker = FindNextFoldedFoldingOnLineAfterColumn(lineNumber, num - 1);
				int end = foldMarker?.StartColumn ?? int.MaxValue;
				logicalColumnInternal = GetLogicalColumnInternal(g, lineSegment, num, end, ref drawingPos, visualPosX);
				if (logicalColumnInternal >= 0)
				{
					continue;
				}
				lineNumber = foldMarker.EndLine;
				num = foldMarker.EndColumn;
				int num2 = drawingPos + 1 + MeasureStringWidth(g, foldMarker.FoldText, TextEditorProperties.FontContainer.RegularFont);
				if (num2 >= visualPosX)
				{
					inFoldMarker = foldMarker;
					if (IsNearerToAThanB(visualPosX, drawingPos, num2))
					{
						return new TextLocation(foldMarker.StartColumn, foldMarker.StartLine);
					}
					return new TextLocation(foldMarker.EndColumn, foldMarker.EndLine);
				}
				drawingPos = num2;
			}
			while (logicalColumnInternal < 0);
		}
		return new TextLocation(logicalColumnInternal, lineNumber);
	}

	private int GetLogicalColumnInternal(Graphics g, LineSegment line, int start, int end, ref int drawingPos, int targetVisualPosX)
	{
		if (start == end)
		{
			return -1;
		}
		int tabIndent = Document.TextEditorProperties.TabIndent;
		FontContainer fontContainer = TextEditorProperties.FontContainer;
		List<TextWord> words = line.Words;
		if (words == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < words.Count; i++)
		{
			TextWord textWord = words[i];
			if (num >= end)
			{
				return -1;
			}
			if (num + textWord.Length >= start)
			{
				int num3;
				switch (textWord.Type)
				{
				case TextWordType.Space:
					num3 = drawingPos + spaceWidth;
					if (num3 >= targetVisualPosX)
					{
						if (!IsNearerToAThanB(targetVisualPosX, drawingPos, num3))
						{
							return num + 1;
						}
						return num;
					}
					break;
				case TextWordType.Tab:
					drawingPos = (drawingPos + 4) / tabIndent / WideSpaceWidth * tabIndent * WideSpaceWidth;
					num3 = drawingPos + tabIndent * WideSpaceWidth;
					if (num3 >= targetVisualPosX)
					{
						if (!IsNearerToAThanB(targetVisualPosX, drawingPos, num3))
						{
							return num + 1;
						}
						return num;
					}
					break;
				case TextWordType.Word:
				{
					int num2 = Math.Max(num, start);
					int length = Math.Min(num + textWord.Length, end) - num2;
					string text = Document.GetText(line.Offset + num2, length);
					Font font = textWord.GetFont(fontContainer) ?? fontContainer.RegularFont;
					num3 = drawingPos + MeasureStringWidth(g, text, font);
					if (num3 < targetVisualPosX)
					{
						break;
					}
					for (int j = 0; j < text.Length; j++)
					{
						num3 = drawingPos + MeasureStringWidth(g, text[j].ToString(), font);
						if (num3 >= targetVisualPosX)
						{
							if (IsNearerToAThanB(targetVisualPosX, drawingPos, num3))
							{
								return num2 + j;
							}
							return num2 + j + 1;
						}
						drawingPos = num3;
					}
					return num2 + text.Length;
				}
				default:
					throw new NotSupportedException();
				}
				drawingPos = num3;
			}
			num += textWord.Length;
		}
		return num;
	}

	private static bool IsNearerToAThanB(int num, int a, int b)
	{
		return Math.Abs(a - num) < Math.Abs(b - num);
	}

	private FoldMarker FindNextFoldedFoldingOnLineAfterColumn(int lineNumber, int column)
	{
		List<FoldMarker> foldedFoldingsWithStartAfterColumn = Document.FoldingManager.GetFoldedFoldingsWithStartAfterColumn(lineNumber, column);
		if (foldedFoldingsWithStartAfterColumn.Count != 0)
		{
			return foldedFoldingsWithStartAfterColumn[0];
		}
		return null;
	}

	private float CountColumns(ref int column, int start, int end, int logicalLine, Graphics g)
	{
		if (start > end)
		{
			throw new ArgumentException("start > end");
		}
		if (start == end)
		{
			return 0f;
		}
		float num = SpaceWidth;
		float num2 = 0f;
		int tabIndent = Document.TextEditorProperties.TabIndent;
		LineSegment lineSegment = Document.GetLineSegment(logicalLine);
		List<TextWord> words = lineSegment.Words;
		if (words == null)
		{
			return 0f;
		}
		int count = words.Count;
		int num3 = 0;
		FontContainer fontContainer = TextEditorProperties.FontContainer;
		for (int i = 0; i < count; i++)
		{
			TextWord textWord = words[i];
			if (num3 >= end)
			{
				break;
			}
			if (num3 + textWord.Length >= start)
			{
				switch (textWord.Type)
				{
				case TextWordType.Space:
					num2 += num;
					break;
				case TextWordType.Tab:
					num2 = (int)((num2 + 4f) / (float)tabIndent / (float)WideSpaceWidth) * tabIndent * WideSpaceWidth;
					num2 += (float)(tabIndent * WideSpaceWidth);
					break;
				case TextWordType.Word:
				{
					int num4 = Math.Max(num3, start);
					int length = Math.Min(num3 + textWord.Length, end) - num4;
					string text = Document.GetText(lineSegment.Offset + num4, length);
					num2 += (float)MeasureStringWidth(g, text, textWord.GetFont(fontContainer) ?? fontContainer.RegularFont);
					break;
				}
				}
			}
			num3 += textWord.Length;
		}
		for (int j = lineSegment.Length; j < end; j++)
		{
			num2 += (float)WideSpaceWidth;
		}
		column += (int)((num2 + 1f) / (float)WideSpaceWidth);
		return num2;
	}

	public int GetDrawingXPos(int logicalLine, int logicalColumn)
	{
		List<FoldMarker> topLevelFoldedFoldings = Document.FoldingManager.GetTopLevelFoldedFoldings();
		FoldMarker foldMarker = null;
		int num;
		for (num = topLevelFoldedFoldings.Count - 1; num >= 0; num--)
		{
			foldMarker = topLevelFoldedFoldings[num];
			if (foldMarker.StartLine < logicalLine || (foldMarker.StartLine == logicalLine && foldMarker.StartColumn < logicalColumn))
			{
				break;
			}
			FoldMarker foldMarker2 = topLevelFoldedFoldings[num / 2];
			if (foldMarker2.StartLine > logicalLine || (foldMarker2.StartLine == logicalLine && foldMarker2.StartColumn >= logicalColumn))
			{
				num /= 2;
			}
		}
		int num2 = 0;
		int num3 = 0;
		int column = 0;
		_ = Document.TextEditorProperties.TabIndent;
		Graphics graphics = textArea.CreateGraphics();
		float num4;
		if (foldMarker == null || (foldMarker.StartLine >= logicalLine && (foldMarker.StartLine != logicalLine || foldMarker.StartColumn >= logicalColumn)))
		{
			num4 = CountColumns(ref column, 0, logicalColumn, logicalLine, graphics);
			return (int)(num4 - (float)textArea.VirtualTop.X);
		}
		if (foldMarker.EndLine > logicalLine || (foldMarker.EndLine == logicalLine && foldMarker.EndColumn > logicalColumn))
		{
			logicalColumn = foldMarker.StartColumn;
			logicalLine = foldMarker.StartLine;
			num--;
		}
		num2 = num;
		while (num >= 0)
		{
			foldMarker = topLevelFoldedFoldings[num];
			if (foldMarker.EndLine < logicalLine)
			{
				break;
			}
			num--;
		}
		num3 = num + 1;
		if (num2 < num3)
		{
			num4 = CountColumns(ref column, 0, logicalColumn, logicalLine, graphics);
			return (int)(num4 - (float)textArea.VirtualTop.X);
		}
		int start = 0;
		num4 = 0f;
		for (num = num3; num <= num2; num++)
		{
			foldMarker = topLevelFoldedFoldings[num];
			num4 += CountColumns(ref column, start, foldMarker.StartColumn, foldMarker.StartLine, graphics);
			start = foldMarker.EndColumn;
			column += foldMarker.FoldText.Length;
			num4++;
			num4 += (float)MeasureStringWidth(graphics, foldMarker.FoldText, TextEditorProperties.FontContainer.RegularFont);
		}
		num4 += CountColumns(ref column, start, logicalColumn, logicalLine, graphics);
		graphics.Dispose();
		return (int)(num4 - (float)textArea.VirtualTop.X);
	}

	private void DrawBracketHighlight(Graphics g, Rectangle rect)
	{
		g.FillRectangle(BrushRegistry.GetBrush(Color.FromArgb(50, 0, 0, 255)), rect);
		g.DrawRectangle(Pens.Blue, rect);
	}

	private void DrawString(Graphics g, string text, Font font, Color color, int x, int y)
	{
		TextRenderer.DrawText(g, text, font, new Point(x, y), color, TextFormatFlags.NoPrefix | TextFormatFlags.PreserveGraphicsClipping | TextFormatFlags.NoPadding);
	}

	private void DrawInvalidLineMarker(Graphics g, int x, int y)
	{
		HighlightColor colorFor = textArea.Document.HighlightingStrategy.GetColorFor("InvalidLines");
		DrawString(g, "~", colorFor.GetFont(TextEditorProperties.FontContainer), colorFor.Color, x, y);
	}

	private void DrawSpaceMarker(Graphics g, Color color, int x, int y)
	{
		HighlightColor colorFor = textArea.Document.HighlightingStrategy.GetColorFor("SpaceMarkers");
		DrawString(g, "·", colorFor.GetFont(TextEditorProperties.FontContainer), color, x, y);
	}

	private void DrawTabMarker(Graphics g, Color color, int x, int y)
	{
		HighlightColor colorFor = textArea.Document.HighlightingStrategy.GetColorFor("TabMarkers");
		DrawString(g, "»", colorFor.GetFont(TextEditorProperties.FontContainer), color, x, y);
	}

	private int DrawEOLMarker(Graphics g, Color color, Brush backBrush, int x, int y)
	{
		HighlightColor colorFor = textArea.Document.HighlightingStrategy.GetColorFor("EOLMarkers");
		int width = GetWidth('¶', colorFor.GetFont(TextEditorProperties.FontContainer));
		g.FillRectangle(backBrush, new RectangleF(x, y, width, fontHeight));
		DrawString(g, "¶", colorFor.GetFont(TextEditorProperties.FontContainer), color, x, y);
		return width;
	}

	private void DrawVerticalRuler(Graphics g, Rectangle lineRectangle)
	{
		int num = WideSpaceWidth * TextEditorProperties.VerticalRulerRow - textArea.VirtualTop.X;
		if (num > 0)
		{
			HighlightColor colorFor = textArea.Document.HighlightingStrategy.GetColorFor("VRuler");
			g.DrawLine(BrushRegistry.GetPen(colorFor.Color), drawingPosition.Left + num, lineRectangle.Top, drawingPosition.Left + num, lineRectangle.Bottom);
		}
	}
}
