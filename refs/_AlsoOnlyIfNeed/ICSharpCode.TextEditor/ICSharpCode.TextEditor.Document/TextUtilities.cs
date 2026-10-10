using System.Text;

namespace ICSharpCode.TextEditor.Document;

public sealed class TextUtilities
{
	public enum CharacterType
	{
		LetterDigitOrUnderscore,
		WhiteSpace,
		Other
	}

	public static string LeadingWhiteSpaceToTabs(string line, int tabIndent)
	{
		StringBuilder stringBuilder = new StringBuilder(line.Length);
		int num = 0;
		int num2 = 0;
		for (num2 = 0; num2 < line.Length; num2++)
		{
			if (line[num2] == ' ')
			{
				num++;
				if (num == tabIndent)
				{
					stringBuilder.Append('\t');
					num = 0;
				}
			}
			else
			{
				if (line[num2] != '\t')
				{
					break;
				}
				stringBuilder.Append('\t');
				num = 0;
			}
		}
		if (num2 < line.Length)
		{
			stringBuilder.Append(line.Substring(num2 - num));
		}
		return stringBuilder.ToString();
	}

	public static bool IsLetterDigitOrUnderscore(char c)
	{
		if (!char.IsLetterOrDigit(c))
		{
			return c == '_';
		}
		return true;
	}

	public static string GetExpressionBeforeOffset(TextArea textArea, int initialOffset)
	{
		IDocument document = textArea.Document;
		int num = initialOffset;
		while (num - 1 > 0)
		{
			switch (document.GetCharAt(num - 1))
			{
			case ']':
				num = SearchBracketBackward(document, num - 2, '[', ']');
				continue;
			case ')':
				num = SearchBracketBackward(document, num - 2, '(', ')');
				continue;
			case '.':
				num--;
				continue;
			case '"':
				if (num < initialOffset - 1)
				{
					return null;
				}
				return "\"\"";
			case '\'':
				if (num < initialOffset - 1)
				{
					return null;
				}
				return "'a'";
			case '>':
				if (document.GetCharAt(num - 2) == '-')
				{
					num -= 2;
					continue;
				}
				break;
			default:
			{
				if (char.IsWhiteSpace(document.GetCharAt(num - 1)))
				{
					num--;
					continue;
				}
				int num2 = num - 1;
				if (!IsLetterDigitOrUnderscore(document.GetCharAt(num2)))
				{
					break;
				}
				while (num2 > 0 && IsLetterDigitOrUnderscore(document.GetCharAt(num2 - 1)))
				{
					num2--;
				}
				string text = document.GetText(num2, num - num2).Trim();
				switch (text)
				{
				default:
					if (text.Length <= 0 || IsLetterDigitOrUnderscore(text[0]))
					{
						num = num2;
						continue;
					}
					break;
				case "ref":
				case "out":
				case "in":
				case "return":
				case "throw":
				case "case":
					break;
				}
				break;
			}
			case '\n':
			case '\r':
			case '}':
				break;
			}
			break;
		}
		if (num < 0)
		{
			return string.Empty;
		}
		string text2 = document.GetText(num, textArea.Caret.Offset - num).Trim();
		int num3 = text2.LastIndexOf('\n');
		if (num3 >= 0)
		{
			num += num3 + 1;
		}
		return document.GetText(num, textArea.Caret.Offset - num).Trim();
	}

	public static CharacterType GetCharacterType(char c)
	{
		if (IsLetterDigitOrUnderscore(c))
		{
			return CharacterType.LetterDigitOrUnderscore;
		}
		if (char.IsWhiteSpace(c))
		{
			return CharacterType.WhiteSpace;
		}
		return CharacterType.Other;
	}

	public static int GetFirstNonWSChar(IDocument document, int offset)
	{
		while (offset < document.TextLength && char.IsWhiteSpace(document.GetCharAt(offset)))
		{
			offset++;
		}
		return offset;
	}

	public static int FindWordEnd(IDocument document, int offset)
	{
		LineSegment lineSegmentForOffset = document.GetLineSegmentForOffset(offset);
		int num = lineSegmentForOffset.Offset + lineSegmentForOffset.Length;
		while (offset < num && IsLetterDigitOrUnderscore(document.GetCharAt(offset)))
		{
			offset++;
		}
		return offset;
	}

	public static int FindWordStart(IDocument document, int offset)
	{
		LineSegment lineSegmentForOffset = document.GetLineSegmentForOffset(offset);
		int offset2 = lineSegmentForOffset.Offset;
		while (offset > offset2 && IsLetterDigitOrUnderscore(document.GetCharAt(offset - 1)))
		{
			offset--;
		}
		return offset;
	}

	public static int FindNextWordStart(IDocument document, int offset)
	{
		LineSegment lineSegmentForOffset = document.GetLineSegmentForOffset(offset);
		int num = lineSegmentForOffset.Offset + lineSegmentForOffset.Length;
		CharacterType characterType = GetCharacterType(document.GetCharAt(offset));
		while (offset < num && GetCharacterType(document.GetCharAt(offset)) == characterType)
		{
			offset++;
		}
		while (offset < num && GetCharacterType(document.GetCharAt(offset)) == CharacterType.WhiteSpace)
		{
			offset++;
		}
		return offset;
	}

	public static int FindPrevWordStart(IDocument document, int offset)
	{
		if (offset > 0)
		{
			LineSegment lineSegmentForOffset = document.GetLineSegmentForOffset(offset);
			CharacterType characterType = GetCharacterType(document.GetCharAt(offset - 1));
			while (offset > lineSegmentForOffset.Offset && GetCharacterType(document.GetCharAt(offset - 1)) == characterType)
			{
				offset--;
			}
			if (characterType == CharacterType.WhiteSpace && offset > lineSegmentForOffset.Offset)
			{
				characterType = GetCharacterType(document.GetCharAt(offset - 1));
				while (offset > lineSegmentForOffset.Offset && GetCharacterType(document.GetCharAt(offset - 1)) == characterType)
				{
					offset--;
				}
			}
		}
		return offset;
	}

	public static string GetLineAsString(IDocument document, int lineNumber)
	{
		LineSegment lineSegment = document.GetLineSegment(lineNumber);
		return document.GetText(lineSegment.Offset, lineSegment.Length);
	}

	public static int SearchBracketBackward(IDocument document, int offset, char openBracket, char closingBracket)
	{
		return document.FormattingStrategy.SearchBracketBackward(document, offset, openBracket, closingBracket);
	}

	public static int SearchBracketForward(IDocument document, int offset, char openBracket, char closingBracket)
	{
		return document.FormattingStrategy.SearchBracketForward(document, offset, openBracket, closingBracket);
	}

	public static bool IsEmptyLine(IDocument document, int lineNumber)
	{
		return IsEmptyLine(document, document.GetLineSegment(lineNumber));
	}

	public static bool IsEmptyLine(IDocument document, LineSegment line)
	{
		for (int i = line.Offset; i < line.Offset + line.Length; i++)
		{
			char charAt = document.GetCharAt(i);
			if (!char.IsWhiteSpace(charAt))
			{
				return false;
			}
		}
		return true;
	}

	private static bool IsWordPart(char ch)
	{
		if (!IsLetterDigitOrUnderscore(ch))
		{
			return ch == '.';
		}
		return true;
	}

	public static string GetWordAt(IDocument document, int offset)
	{
		if (offset < 0 || offset >= document.TextLength - 1 || !IsWordPart(document.GetCharAt(offset)))
		{
			return string.Empty;
		}
		int num = offset;
		int i = offset;
		while (num > 0 && IsWordPart(document.GetCharAt(num - 1)))
		{
			num--;
		}
		for (; i < document.TextLength - 1 && IsWordPart(document.GetCharAt(i + 1)); i++)
		{
		}
		return document.GetText(num, i - num + 1);
	}
}
