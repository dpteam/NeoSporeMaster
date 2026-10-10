using System;
using System.Text;

namespace ICSharpCode.TextEditor.Document;

public class DefaultFormattingStrategy : IFormattingStrategy
{
	private static readonly char[] whitespaceChars = new char[2] { ' ', '\t' };

	protected string GetIndentation(TextArea textArea, int lineNumber)
	{
		if (lineNumber < 0 || lineNumber > textArea.Document.TotalNumberOfLines)
		{
			throw new ArgumentOutOfRangeException("lineNumber");
		}
		string lineAsString = TextUtilities.GetLineAsString(textArea.Document, lineNumber);
		StringBuilder stringBuilder = new StringBuilder();
		string text = lineAsString;
		foreach (char c in text)
		{
			if (!char.IsWhiteSpace(c))
			{
				break;
			}
			stringBuilder.Append(c);
		}
		return stringBuilder.ToString();
	}

	protected virtual int AutoIndentLine(TextArea textArea, int lineNumber)
	{
		string text = ((lineNumber != 0) ? GetIndentation(textArea, lineNumber - 1) : "");
		if (text.Length > 0)
		{
			string newLineText = text + TextUtilities.GetLineAsString(textArea.Document, lineNumber).Trim();
			LineSegment lineSegment = textArea.Document.GetLineSegment(lineNumber);
			SmartReplaceLine(textArea.Document, lineSegment, newLineText);
		}
		return text.Length;
	}

	public static void SmartReplaceLine(IDocument document, LineSegment line, string newLineText)
	{
		if (document == null)
		{
			throw new ArgumentNullException("document");
		}
		if (line == null)
		{
			throw new ArgumentNullException("line");
		}
		if (newLineText == null)
		{
			throw new ArgumentNullException("newLineText");
		}
		string text = newLineText.Trim(whitespaceChars);
		string text2 = document.GetText(line);
		if (text2 == newLineText)
		{
			return;
		}
		int num = text2.IndexOf(text);
		if (text.Length > 0 && num >= 0)
		{
			document.UndoStack.StartUndoGroup();
			try
			{
				int i;
				for (i = 0; i < newLineText.Length; i++)
				{
					char c = newLineText[i];
					if (c != ' ' && c != '\t')
					{
						break;
					}
				}
				int num2 = newLineText.Length - text.Length - i;
				int offset = line.Offset;
				document.Replace(offset + num + text.Length, line.Length - num - text.Length, newLineText.Substring(newLineText.Length - num2));
				document.Replace(offset, num, newLineText.Substring(0, i));
				return;
			}
			finally
			{
				document.UndoStack.EndUndoGroup();
			}
		}
		document.Replace(line.Offset, line.Length, newLineText);
	}

	protected virtual int SmartIndentLine(TextArea textArea, int line)
	{
		return AutoIndentLine(textArea, line);
	}

	public virtual void FormatLine(TextArea textArea, int line, int cursorOffset, char ch)
	{
		if (ch == '\n')
		{
			textArea.Caret.Column = IndentLine(textArea, line);
		}
	}

	public int IndentLine(TextArea textArea, int line)
	{
		textArea.Document.UndoStack.StartUndoGroup();
		int result = textArea.Document.TextEditorProperties.IndentStyle switch
		{
			IndentStyle.None => 0, 
			IndentStyle.Auto => AutoIndentLine(textArea, line), 
			IndentStyle.Smart => SmartIndentLine(textArea, line), 
			_ => throw new NotSupportedException("Unsupported value for IndentStyle: " + textArea.Document.TextEditorProperties.IndentStyle), 
		};
		textArea.Document.UndoStack.EndUndoGroup();
		return result;
	}

	public virtual void IndentLines(TextArea textArea, int begin, int end)
	{
		textArea.Document.UndoStack.StartUndoGroup();
		for (int i = begin; i <= end; i++)
		{
			IndentLine(textArea, i);
		}
		textArea.Document.UndoStack.EndUndoGroup();
	}

	public virtual int SearchBracketBackward(IDocument document, int offset, char openBracket, char closingBracket)
	{
		int num = -1;
		for (int num2 = offset; num2 >= 0; num2--)
		{
			char charAt = document.GetCharAt(num2);
			if (charAt == openBracket)
			{
				num++;
				if (num == 0)
				{
					return num2;
				}
				continue;
			}
			if (charAt == closingBracket)
			{
				num--;
				continue;
			}
			switch (charAt)
			{
			case '/':
				if (num2 <= 0 || (document.GetCharAt(num2 - 1) != '/' && document.GetCharAt(num2 - 1) != '*'))
				{
					continue;
				}
				break;
			default:
				continue;
			case '"':
			case '\'':
				break;
			}
			break;
		}
		return -1;
	}

	public virtual int SearchBracketForward(IDocument document, int offset, char openBracket, char closingBracket)
	{
		int num = 1;
		for (int i = offset; i < document.TextLength; i++)
		{
			char charAt = document.GetCharAt(i);
			if (charAt == openBracket)
			{
				num++;
				continue;
			}
			if (charAt == closingBracket)
			{
				num--;
				if (num == 0)
				{
					return i;
				}
				continue;
			}
			if (charAt == '"' || charAt == '\'')
			{
				break;
			}
			if (charAt == '/' && i > 0)
			{
				if (document.GetCharAt(i - 1) == '/')
				{
					break;
				}
			}
			else if (charAt == '*' && i > 0 && document.GetCharAt(i - 1) == '/')
			{
				break;
			}
		}
		return -1;
	}
}
