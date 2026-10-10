using System.Collections.Generic;
using System.Drawing;
using System.Text;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Util;

public class RtfWriter
{
	private static Dictionary<string, int> colors;

	private static int colorNum;

	private static StringBuilder colorString;

	public static string GenerateRtf(TextArea textArea)
	{
		colors = new Dictionary<string, int>();
		colorNum = 0;
		colorString = new StringBuilder();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("{\\rtf1\\ansi\\ansicpg1252\\deff0\\deflang1031");
		BuildFontTable(textArea.Document, stringBuilder);
		stringBuilder.Append('\n');
		string value = BuildFileContent(textArea);
		BuildColorTable(textArea.Document, stringBuilder);
		stringBuilder.Append('\n');
		stringBuilder.Append("\\viewkind4\\uc1\\pard");
		stringBuilder.Append(value);
		stringBuilder.Append("}");
		return stringBuilder.ToString();
	}

	private static void BuildColorTable(IDocument doc, StringBuilder rtf)
	{
		rtf.Append("{\\colortbl ;");
		rtf.Append(colorString.ToString());
		rtf.Append("}");
	}

	private static void BuildFontTable(IDocument doc, StringBuilder rtf)
	{
		rtf.Append("{\\fonttbl");
		rtf.Append("{\\f0\\fmodern\\fprq1\\fcharset0 " + doc.TextEditorProperties.Font.Name + ";}");
		rtf.Append("}");
	}

	private static string BuildFileContent(TextArea textArea)
	{
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = true;
		Color color = Color.Black;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		foreach (ISelection item in textArea.SelectionManager.SelectionCollection)
		{
			int num = textArea.Document.PositionToOffset(item.StartPosition);
			int num2 = textArea.Document.PositionToOffset(item.EndPosition);
			for (int i = item.StartPosition.Y; i <= item.EndPosition.Y; i++)
			{
				LineSegment lineSegment = textArea.Document.GetLineSegment(i);
				int num3 = lineSegment.Offset;
				if (lineSegment.Words == null)
				{
					continue;
				}
				foreach (TextWord word in lineSegment.Words)
				{
					switch (word.Type)
					{
					case TextWordType.Space:
						if (item.ContainsOffset(num3))
						{
							stringBuilder.Append(' ');
						}
						num3++;
						break;
					case TextWordType.Tab:
						if (item.ContainsOffset(num3))
						{
							stringBuilder.Append("\\tab");
						}
						num3++;
						flag4 = true;
						break;
					case TextWordType.Word:
					{
						Color color2 = word.Color;
						if (num3 + word.Word.Length > num && num3 < num2)
						{
							string key = color2.R + ", " + color2.G + ", " + color2.B;
							if (!colors.ContainsKey(key))
							{
								colors[key] = ++colorNum;
								colorString.Append("\\red" + color2.R + "\\green" + color2.G + "\\blue" + color2.B + ";");
							}
							if (color2 != color || flag)
							{
								stringBuilder.Append("\\cf" + colors[key]);
								color = color2;
								flag4 = true;
							}
							if (flag2 != word.Italic)
							{
								if (word.Italic)
								{
									stringBuilder.Append("\\i");
								}
								else
								{
									stringBuilder.Append("\\i0");
								}
								flag2 = word.Italic;
								flag4 = true;
							}
							if (flag3 != word.Bold)
							{
								if (word.Bold)
								{
									stringBuilder.Append("\\b");
								}
								else
								{
									stringBuilder.Append("\\b0");
								}
								flag3 = word.Bold;
								flag4 = true;
							}
							if (flag)
							{
								stringBuilder.Append("\\f0\\fs" + textArea.TextEditorProperties.Font.Size * 2f);
								flag = false;
							}
							if (flag4)
							{
								stringBuilder.Append(' ');
								flag4 = false;
							}
							string text;
							if (num3 < num)
							{
								text = word.Word.Substring(num - num3);
							}
							else
							{
								text = ((num3 + word.Word.Length <= num2) ? word.Word : word.Word.Substring(0, num3 + word.Word.Length - num2));
							}
							stringBuilder.Append(text.Replace("\\", "\\\\").Replace("{", "\\{").Replace("}", "\\}"));
						}
						num3 += word.Length;
						break;
					}
					}
				}
				if (num3 < num2)
				{
					stringBuilder.Append("\\par");
				}
				stringBuilder.Append('\n');
			}
		}
		return stringBuilder.ToString();
	}
}
