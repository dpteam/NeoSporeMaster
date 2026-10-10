using System;
using System.Collections.Generic;
using System.Drawing;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor;

public class DrawableLine
{
	private class SimpleTextWord
	{
		internal TextWordType Type;

		internal string Word;

		internal bool Bold;

		internal Color Color;

		internal static readonly SimpleTextWord Space = new SimpleTextWord(TextWordType.Space, " ", Bold: false, Color.Black);

		internal static readonly SimpleTextWord Tab = new SimpleTextWord(TextWordType.Tab, "\t", Bold: false, Color.Black);

		public SimpleTextWord(TextWordType Type, string Word, bool Bold, Color Color)
		{
			this.Type = Type;
			this.Word = Word;
			this.Bold = Bold;
			this.Color = Color;
		}
	}

	private static StringFormat sf = (StringFormat)StringFormat.GenericTypographic.Clone();

	private List<SimpleTextWord> words = new List<SimpleTextWord>();

	private SizeF spaceSize;

	private Font monospacedFont;

	private Font boldMonospacedFont;

	public int LineLength
	{
		get
		{
			int num = 0;
			foreach (SimpleTextWord word in words)
			{
				num += word.Word.Length;
			}
			return num;
		}
	}

	public DrawableLine(IDocument document, LineSegment line, Font monospacedFont, Font boldMonospacedFont)
	{
		this.monospacedFont = monospacedFont;
		this.boldMonospacedFont = boldMonospacedFont;
		if (line.Words != null)
		{
			foreach (TextWord word in line.Words)
			{
				if (word.Type == TextWordType.Space)
				{
					words.Add(SimpleTextWord.Space);
				}
				else if (word.Type == TextWordType.Tab)
				{
					words.Add(SimpleTextWord.Tab);
				}
				else
				{
					words.Add(new SimpleTextWord(TextWordType.Word, word.Word, word.Bold, word.Color));
				}
			}
			return;
		}
		words.Add(new SimpleTextWord(TextWordType.Word, document.GetText(line), Bold: false, Color.Black));
	}

	public void SetBold(int startIndex, int endIndex, bool bold)
	{
		if (startIndex < 0)
		{
			throw new ArgumentException("startIndex must be >= 0");
		}
		if (startIndex > endIndex)
		{
			throw new ArgumentException("startIndex must be <= endIndex");
		}
		if (startIndex == endIndex)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < words.Count; i++)
		{
			SimpleTextWord simpleTextWord = words[i];
			if (num >= endIndex)
			{
				break;
			}
			int num2 = num + simpleTextWord.Word.Length;
			if (startIndex <= num && endIndex >= num2)
			{
				simpleTextWord.Bold = bold;
			}
			else if (startIndex <= num)
			{
				int num3 = endIndex - num;
				SimpleTextWord item = new SimpleTextWord(simpleTextWord.Type, simpleTextWord.Word.Substring(num3), simpleTextWord.Bold, simpleTextWord.Color);
				words.Insert(i + 1, item);
				simpleTextWord.Bold = bold;
				simpleTextWord.Word = simpleTextWord.Word.Substring(0, num3);
			}
			else if (startIndex < num2)
			{
				int num4 = startIndex - num;
				SimpleTextWord item2 = new SimpleTextWord(simpleTextWord.Type, simpleTextWord.Word.Substring(num4), simpleTextWord.Bold, simpleTextWord.Color);
				words.Insert(i + 1, item2);
				simpleTextWord.Word = simpleTextWord.Word.Substring(0, num4);
			}
			num = num2;
		}
	}

	public static float DrawDocumentWord(Graphics g, string word, PointF position, Font font, Color foreColor)
	{
		if (word == null || word.Length == 0)
		{
			return 0f;
		}
		SizeF sizeF = g.MeasureString(word, font, 32768, sf);
		g.DrawString(word, font, BrushRegistry.GetBrush(foreColor), position, sf);
		return sizeF.Width;
	}

	public SizeF GetSpaceSize(Graphics g)
	{
		if (spaceSize.IsEmpty)
		{
			spaceSize = g.MeasureString("-", boldMonospacedFont, new PointF(0f, 0f), sf);
		}
		return spaceSize;
	}

	public void DrawLine(Graphics g, ref float xPos, float xOffset, float yPos, Color c)
	{
		SizeF sizeF = GetSpaceSize(g);
		foreach (SimpleTextWord word in words)
		{
			switch (word.Type)
			{
			case TextWordType.Space:
				xPos += sizeF.Width;
				break;
			case TextWordType.Tab:
			{
				float num = sizeF.Width * 4f;
				xPos += num;
				xPos = (float)(int)((xPos + 2f) / num) * num;
				break;
			}
			case TextWordType.Word:
				xPos += DrawDocumentWord(g, word.Word, new PointF(xPos + xOffset, yPos), word.Bold ? boldMonospacedFont : monospacedFont, (c == Color.Empty) ? word.Color : c);
				break;
			}
		}
	}

	public void DrawLine(Graphics g, ref float xPos, float xOffset, float yPos)
	{
		DrawLine(g, ref xPos, xOffset, yPos, Color.Empty);
	}

	public float MeasureWidth(Graphics g, float xPos)
	{
		SizeF sizeF = GetSpaceSize(g);
		foreach (SimpleTextWord word in words)
		{
			switch (word.Type)
			{
			case TextWordType.Space:
				xPos += sizeF.Width;
				break;
			case TextWordType.Tab:
			{
				float num = sizeF.Width * 4f;
				xPos += num;
				xPos = (float)(int)((xPos + 2f) / num) * num;
				break;
			}
			case TextWordType.Word:
				if (word.Word != null && word.Word.Length > 0)
				{
					xPos += g.MeasureString(word.Word, word.Bold ? boldMonospacedFont : monospacedFont, 32768, sf).Width;
				}
				break;
			}
		}
		return xPos;
	}
}
