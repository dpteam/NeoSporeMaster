using System.Drawing;

namespace ICSharpCode.TextEditor.Document;

public class TextWord
{
	public sealed class SpaceTextWord : TextWord
	{
		public override TextWordType Type => TextWordType.Space;

		public override bool IsWhiteSpace => true;

		public SpaceTextWord()
		{
			length = 1;
		}

		public SpaceTextWord(HighlightColor color)
		{
			length = 1;
			SyntaxColor = color;
		}

		public override Font GetFont(FontContainer fontContainer)
		{
			return null;
		}
	}

	public sealed class TabTextWord : TextWord
	{
		public override TextWordType Type => TextWordType.Tab;

		public override bool IsWhiteSpace => true;

		public TabTextWord()
		{
			length = 1;
		}

		public TabTextWord(HighlightColor color)
		{
			length = 1;
			SyntaxColor = color;
		}

		public override Font GetFont(FontContainer fontContainer)
		{
			return null;
		}
	}

	private HighlightColor color;

	private LineSegment line;

	private IDocument document;

	private int offset;

	private int length;

	private static TextWord spaceWord = new SpaceTextWord();

	private static TextWord tabWord = new TabTextWord();

	private bool hasDefaultColor;

	public static TextWord Space => spaceWord;

	public static TextWord Tab => tabWord;

	public int Offset => offset;

	public int Length => length;

	public bool HasDefaultColor => hasDefaultColor;

	public virtual TextWordType Type => TextWordType.Word;

	public string Word
	{
		get
		{
			if (document == null)
			{
				return string.Empty;
			}
			return document.GetText(line.Offset + offset, length);
		}
	}

	public Color Color
	{
		get
		{
			if (color == null)
			{
				return Color.Black;
			}
			return color.Color;
		}
	}

	public bool Bold
	{
		get
		{
			if (color == null)
			{
				return false;
			}
			return color.Bold;
		}
	}

	public bool Italic
	{
		get
		{
			if (color == null)
			{
				return false;
			}
			return color.Italic;
		}
	}

	public HighlightColor SyntaxColor
	{
		get
		{
			return color;
		}
		set
		{
			color = value;
		}
	}

	public virtual bool IsWhiteSpace => false;

	public static TextWord Split(ref TextWord word, int pos)
	{
		TextWord result = new TextWord(word.document, word.line, word.offset + pos, word.length - pos, word.color, word.hasDefaultColor);
		word = new TextWord(word.document, word.line, word.offset, pos, word.color, word.hasDefaultColor);
		return result;
	}

	public virtual Font GetFont(FontContainer fontContainer)
	{
		return color.GetFont(fontContainer);
	}

	protected TextWord()
	{
	}

	public TextWord(IDocument document, LineSegment line, int offset, int length, HighlightColor color, bool hasDefaultColor)
	{
		this.document = document;
		this.line = line;
		this.offset = offset;
		this.length = length;
		this.color = color;
		this.hasDefaultColor = hasDefaultColor;
	}

	public override string ToString()
	{
		return string.Concat("[TextWord: Word = ", Word, ", Color = ", Color, "]");
	}
}
