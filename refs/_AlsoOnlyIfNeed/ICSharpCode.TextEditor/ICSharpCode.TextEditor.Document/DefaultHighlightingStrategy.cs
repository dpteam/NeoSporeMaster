using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace ICSharpCode.TextEditor.Document;

public class DefaultHighlightingStrategy : IHighlightingStrategyUsingRuleSets, IHighlightingStrategy
{
	private string name;

	private List<HighlightRuleSet> rules = new List<HighlightRuleSet>();

	private Dictionary<string, HighlightColor> environmentColors = new Dictionary<string, HighlightColor>();

	private Dictionary<string, string> properties = new Dictionary<string, string>();

	private string[] extensions;

	private HighlightColor digitColor;

	private HighlightRuleSet defaultRuleSet;

	private HighlightColor defaultTextColor;

	protected LineSegment currentLine;

	protected int currentLineNumber;

	protected SpanStack currentSpanStack;

	protected bool inSpan;

	protected Span activeSpan;

	protected HighlightRuleSet activeRuleSet;

	protected int currentOffset;

	protected int currentLength;

	public HighlightColor DigitColor
	{
		get
		{
			return digitColor;
		}
		set
		{
			digitColor = value;
		}
	}

	public IEnumerable<KeyValuePair<string, HighlightColor>> EnvironmentColors => environmentColors;

	public Dictionary<string, string> Properties => properties;

	public string Name => name;

	public string[] Extensions
	{
		get
		{
			return extensions;
		}
		set
		{
			extensions = value;
		}
	}

	public List<HighlightRuleSet> Rules => rules;

	public HighlightColor DefaultTextColor => defaultTextColor;

	protected void ImportSettingsFrom(DefaultHighlightingStrategy source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		properties = source.properties;
		extensions = source.extensions;
		digitColor = source.digitColor;
		defaultRuleSet = source.defaultRuleSet;
		name = source.name;
		rules = source.rules;
		environmentColors = source.environmentColors;
		defaultTextColor = source.defaultTextColor;
	}

	public DefaultHighlightingStrategy()
		: this("Default")
	{
	}

	public DefaultHighlightingStrategy(string name)
	{
		this.name = name;
		digitColor = new HighlightColor(SystemColors.WindowText, bold: false, italic: false);
		defaultTextColor = new HighlightColor(SystemColors.WindowText, bold: false, italic: false);
		environmentColors["Default"] = new HighlightBackground("WindowText", "Window", bold: false, italic: false);
		environmentColors["Selection"] = new HighlightColor("HighlightText", "Highlight", bold: false, italic: false);
		environmentColors["VRuler"] = new HighlightColor("ControlLight", "Window", bold: false, italic: false);
		environmentColors["InvalidLines"] = new HighlightColor(Color.Red, bold: false, italic: false);
		environmentColors["CaretMarker"] = new HighlightColor(Color.Yellow, bold: false, italic: false);
		environmentColors["LineNumbers"] = new HighlightBackground("ControlDark", "Window", bold: false, italic: false);
		environmentColors["FoldLine"] = new HighlightColor(Color.FromArgb(128, 128, 128), Color.Black, bold: false, italic: false);
		environmentColors["FoldMarker"] = new HighlightColor(Color.FromArgb(128, 128, 128), Color.White, bold: false, italic: false);
		environmentColors["SelectedFoldLine"] = new HighlightColor(Color.Black, bold: false, italic: false);
		environmentColors["EOLMarkers"] = new HighlightColor("ControlLight", "Window", bold: false, italic: false);
		environmentColors["SpaceMarkers"] = new HighlightColor("ControlLight", "Window", bold: false, italic: false);
		environmentColors["TabMarkers"] = new HighlightColor("ControlLight", "Window", bold: false, italic: false);
	}

	public HighlightRuleSet FindHighlightRuleSet(string name)
	{
		foreach (HighlightRuleSet rule in rules)
		{
			if (rule.Name == name)
			{
				return rule;
			}
		}
		return null;
	}

	public void AddRuleSet(HighlightRuleSet aRuleSet)
	{
		HighlightRuleSet highlightRuleSet = FindHighlightRuleSet(aRuleSet.Name);
		if (highlightRuleSet != null)
		{
			highlightRuleSet.MergeFrom(aRuleSet);
		}
		else
		{
			rules.Add(aRuleSet);
		}
	}

	public void ResolveReferences()
	{
		ResolveRuleSetReferences();
		ResolveExternalReferences();
	}

	private void ResolveRuleSetReferences()
	{
		foreach (HighlightRuleSet rule in Rules)
		{
			if (rule.Name == null)
			{
				defaultRuleSet = rule;
			}
			foreach (Span span in rule.Spans)
			{
				if (span.Rule != null)
				{
					bool flag = false;
					foreach (HighlightRuleSet rule2 in Rules)
					{
						if (rule2.Name == span.Rule)
						{
							flag = true;
							span.RuleSet = rule2;
							break;
						}
					}
					if (!flag)
					{
						span.RuleSet = null;
						throw new HighlightingDefinitionInvalidException("The RuleSet " + span.Rule + " could not be found in mode definition " + Name);
					}
				}
				else
				{
					span.RuleSet = null;
				}
			}
		}
		if (defaultRuleSet == null)
		{
			throw new HighlightingDefinitionInvalidException("No default RuleSet is defined for mode definition " + Name);
		}
	}

	private void ResolveExternalReferences()
	{
		foreach (HighlightRuleSet rule in Rules)
		{
			rule.Highlighter = this;
			if (rule.Reference != null)
			{
				IHighlightingStrategy highlightingStrategy = HighlightingManager.Manager.FindHighlighter(rule.Reference);
				if (highlightingStrategy == null)
				{
					throw new HighlightingDefinitionInvalidException("The mode defintion " + rule.Reference + " which is refered from the " + Name + " mode definition could not be found");
				}
				if (!(highlightingStrategy is IHighlightingStrategyUsingRuleSets))
				{
					throw new HighlightingDefinitionInvalidException("The mode defintion " + rule.Reference + " which is refered from the " + Name + " mode definition does not implement IHighlightingStrategyUsingRuleSets");
				}
				rule.Highlighter = (IHighlightingStrategyUsingRuleSets)highlightingStrategy;
			}
		}
	}

	public void SetColorFor(string name, HighlightColor color)
	{
		if (name == "Default")
		{
			defaultTextColor = new HighlightColor(color.Color, color.Bold, color.Italic);
		}
		environmentColors[name] = color;
	}

	public HighlightColor GetColorFor(string name)
	{
		if (environmentColors.TryGetValue(name, out var value))
		{
			return value;
		}
		return defaultTextColor;
	}

	public HighlightColor GetColor(IDocument document, LineSegment currentSegment, int currentOffset, int currentLength)
	{
		return GetColor(defaultRuleSet, document, currentSegment, currentOffset, currentLength);
	}

	protected virtual HighlightColor GetColor(HighlightRuleSet ruleSet, IDocument document, LineSegment currentSegment, int currentOffset, int currentLength)
	{
		if (ruleSet != null)
		{
			if (ruleSet.Reference != null)
			{
				return ruleSet.Highlighter.GetColor(document, currentSegment, currentOffset, currentLength);
			}
			return (HighlightColor)ruleSet.KeyWords[document, currentSegment, currentOffset, currentLength];
		}
		return null;
	}

	public HighlightRuleSet GetRuleSet(Span aSpan)
	{
		if (aSpan == null)
		{
			return defaultRuleSet;
		}
		if (aSpan.RuleSet != null)
		{
			if (aSpan.RuleSet.Reference != null)
			{
				return aSpan.RuleSet.Highlighter.GetRuleSet(null);
			}
			return aSpan.RuleSet;
		}
		return null;
	}

	public virtual void MarkTokens(IDocument document)
	{
		if (Rules.Count == 0)
		{
			return;
		}
		for (int i = 0; i < document.TotalNumberOfLines; i++)
		{
			LineSegment lineSegment = ((i > 0) ? document.GetLineSegment(i - 1) : null);
			if (i >= document.LineSegmentCollection.Count)
			{
				break;
			}
			currentSpanStack = ((lineSegment != null && lineSegment.HighlightSpanStack != null) ? lineSegment.HighlightSpanStack.Clone() : null);
			if (currentSpanStack != null)
			{
				while (!currentSpanStack.IsEmpty && currentSpanStack.Peek().StopEOL)
				{
					currentSpanStack.Pop();
				}
				if (currentSpanStack.IsEmpty)
				{
					currentSpanStack = null;
				}
			}
			currentLine = document.LineSegmentCollection[i];
			if (currentLine.Length == -1)
			{
				return;
			}
			currentLineNumber = i;
			List<TextWord> words = ParseLine(document);
			if (currentLine.Words != null)
			{
				currentLine.Words.Clear();
			}
			currentLine.Words = words;
			currentLine.HighlightSpanStack = ((currentSpanStack == null || currentSpanStack.IsEmpty) ? null : currentSpanStack);
		}
		document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
		document.CommitUpdate();
		currentLine = null;
	}

	private bool MarkTokensInLine(IDocument document, int lineNumber, ref bool spanChanged)
	{
		currentLineNumber = lineNumber;
		bool result = false;
		LineSegment lineSegment = ((lineNumber > 0) ? document.GetLineSegment(lineNumber - 1) : null);
		currentSpanStack = ((lineSegment != null && lineSegment.HighlightSpanStack != null) ? lineSegment.HighlightSpanStack.Clone() : null);
		if (currentSpanStack != null)
		{
			while (!currentSpanStack.IsEmpty && currentSpanStack.Peek().StopEOL)
			{
				currentSpanStack.Pop();
			}
			if (currentSpanStack.IsEmpty)
			{
				currentSpanStack = null;
			}
		}
		currentLine = document.LineSegmentCollection[lineNumber];
		if (currentLine.Length == -1)
		{
			return false;
		}
		List<TextWord> words = ParseLine(document);
		if (currentSpanStack != null && currentSpanStack.IsEmpty)
		{
			currentSpanStack = null;
		}
		if (currentLine.HighlightSpanStack != currentSpanStack)
		{
			if (currentLine.HighlightSpanStack == null)
			{
				result = false;
				foreach (Span item in currentSpanStack)
				{
					if (!item.StopEOL)
					{
						spanChanged = true;
						result = true;
						break;
					}
				}
			}
			else if (currentSpanStack == null)
			{
				result = false;
				foreach (Span item2 in currentLine.HighlightSpanStack)
				{
					if (!item2.StopEOL)
					{
						spanChanged = true;
						result = true;
						break;
					}
				}
			}
			else
			{
				SpanStack.Enumerator enumerator3 = currentSpanStack.GetEnumerator();
				SpanStack.Enumerator enumerator4 = currentLine.HighlightSpanStack.GetEnumerator();
				bool flag = false;
				while (!flag)
				{
					bool flag2 = false;
					while (enumerator3.MoveNext())
					{
						if (!enumerator3.Current.StopEOL)
						{
							flag2 = true;
							break;
						}
					}
					bool flag3 = false;
					while (enumerator4.MoveNext())
					{
						if (!enumerator4.Current.StopEOL)
						{
							flag3 = true;
							break;
						}
					}
					if (flag2 || flag3)
					{
						if (flag2 && flag3)
						{
							if (enumerator3.Current != enumerator4.Current)
							{
								flag = true;
								result = true;
								spanChanged = true;
							}
						}
						else
						{
							spanChanged = true;
							flag = true;
							result = true;
						}
					}
					else
					{
						flag = true;
						result = false;
					}
				}
			}
		}
		else
		{
			result = false;
		}
		if (currentLine.Words != null)
		{
			currentLine.Words.Clear();
		}
		currentLine.Words = words;
		currentLine.HighlightSpanStack = ((currentSpanStack != null && !currentSpanStack.IsEmpty) ? currentSpanStack : null);
		return result;
	}

	public virtual void MarkTokens(IDocument document, List<LineSegment> inputLines)
	{
		if (Rules.Count == 0)
		{
			return;
		}
		Dictionary<LineSegment, bool> dictionary = new Dictionary<LineSegment, bool>();
		bool spanChanged = false;
		int count = document.LineSegmentCollection.Count;
		foreach (LineSegment inputLine in inputLines)
		{
			if (dictionary.ContainsKey(inputLine))
			{
				continue;
			}
			int num = inputLine.LineNumber;
			bool flag = true;
			if (num != -1)
			{
				while (flag && num < count)
				{
					flag = MarkTokensInLine(document, num, ref spanChanged);
					dictionary[currentLine] = true;
					num++;
				}
			}
		}
		if (spanChanged || inputLines.Count > 20)
		{
			document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
		}
		else
		{
			foreach (LineSegment inputLine2 in inputLines)
			{
				document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, inputLine2.LineNumber));
			}
		}
		document.CommitUpdate();
		currentLine = null;
	}

	private void UpdateSpanStateVariables()
	{
		inSpan = currentSpanStack != null && !currentSpanStack.IsEmpty;
		activeSpan = (inSpan ? currentSpanStack.Peek() : null);
		activeRuleSet = GetRuleSet(activeSpan);
	}

	private List<TextWord> ParseLine(IDocument document)
	{
		List<TextWord> list = new List<TextWord>();
		HighlightColor markNext = null;
		currentOffset = 0;
		currentLength = 0;
		UpdateSpanStateVariables();
		int length = currentLine.Length;
		int offset = currentLine.Offset;
		for (int i = 0; i < length; i++)
		{
			char charAt = document.GetCharAt(offset + i);
			switch (charAt)
			{
			case '\n':
			case '\r':
				PushCurWord(document, ref markNext, list);
				currentOffset++;
				continue;
			case ' ':
				PushCurWord(document, ref markNext, list);
				if (activeSpan != null && activeSpan.Color.HasBackground)
				{
					list.Add(new TextWord.SpaceTextWord(activeSpan.Color));
				}
				else
				{
					list.Add(TextWord.Space);
				}
				currentOffset++;
				continue;
			case '\t':
				PushCurWord(document, ref markNext, list);
				if (activeSpan != null && activeSpan.Color.HasBackground)
				{
					list.Add(new TextWord.TabTextWord(activeSpan.Color));
				}
				else
				{
					list.Add(TextWord.Tab);
				}
				currentOffset++;
				continue;
			}
			char c = '\0';
			if (activeSpan != null && activeSpan.EscapeCharacter != '\0')
			{
				c = activeSpan.EscapeCharacter;
			}
			else if (activeRuleSet != null)
			{
				c = activeRuleSet.EscapeCharacter;
			}
			if (c != '\0' && c == charAt)
			{
				if (activeSpan == null || activeSpan.End == null || activeSpan.End.Length != 1 || c != activeSpan.End[0])
				{
					currentLength++;
					if (i + 1 < length)
					{
						currentLength++;
					}
					PushCurWord(document, ref markNext, list);
					i++;
					continue;
				}
				if (i + 1 < length && document.GetCharAt(offset + i + 1) == c)
				{
					currentLength += 2;
					PushCurWord(document, ref markNext, list);
					i++;
					continue;
				}
			}
			if (!inSpan && (char.IsDigit(charAt) || (charAt == '.' && i + 1 < length && char.IsDigit(document.GetCharAt(offset + i + 1)))) && currentLength == 0)
			{
				bool flag = false;
				bool flag2 = false;
				if (charAt == '0' && i + 1 < length && char.ToUpper(document.GetCharAt(offset + i + 1)) == 'X')
				{
					currentLength++;
					i++;
					currentLength++;
					flag = true;
					while (i + 1 < length && "0123456789ABCDEF".IndexOf(char.ToUpper(document.GetCharAt(offset + i + 1))) != -1)
					{
						i++;
						currentLength++;
					}
				}
				else
				{
					currentLength++;
					while (i + 1 < length && char.IsDigit(document.GetCharAt(offset + i + 1)))
					{
						i++;
						currentLength++;
					}
				}
				if (!flag && i + 1 < length && document.GetCharAt(offset + i + 1) == '.')
				{
					flag2 = true;
					i++;
					currentLength++;
					while (i + 1 < length && char.IsDigit(document.GetCharAt(offset + i + 1)))
					{
						i++;
						currentLength++;
					}
				}
				if (i + 1 < length && char.ToUpper(document.GetCharAt(offset + i + 1)) == 'E')
				{
					flag2 = true;
					i++;
					currentLength++;
					if (i + 1 < length && (document.GetCharAt(offset + i + 1) == '+' || document.GetCharAt(currentLine.Offset + i + 1) == '-'))
					{
						i++;
						currentLength++;
					}
					while (i + 1 < currentLine.Length && char.IsDigit(document.GetCharAt(offset + i + 1)))
					{
						i++;
						currentLength++;
					}
				}
				if (i + 1 < currentLine.Length)
				{
					char c2 = char.ToUpper(document.GetCharAt(offset + i + 1));
					if (c2 == 'F' || c2 == 'M' || c2 == 'D')
					{
						flag2 = true;
						i++;
						currentLength++;
					}
				}
				if (!flag2)
				{
					bool flag3 = false;
					if (i + 1 < length && char.ToUpper(document.GetCharAt(offset + i + 1)) == 'U')
					{
						i++;
						currentLength++;
						flag3 = true;
					}
					if (i + 1 < length && char.ToUpper(document.GetCharAt(offset + i + 1)) == 'L')
					{
						i++;
						currentLength++;
						if (!flag3 && i + 1 < length && char.ToUpper(document.GetCharAt(offset + i + 1)) == 'U')
						{
							i++;
							currentLength++;
						}
					}
				}
				list.Add(new TextWord(document, currentLine, currentOffset, currentLength, DigitColor, hasDefaultColor: false));
				currentOffset += currentLength;
				currentLength = 0;
				continue;
			}
			if (inSpan && activeSpan.End != null && activeSpan.End.Length > 0 && MatchExpr(currentLine, activeSpan.End, i, document, activeSpan.IgnoreCase))
			{
				PushCurWord(document, ref markNext, list);
				string regString = GetRegString(currentLine, activeSpan.End, i, document);
				currentLength += regString.Length;
				list.Add(new TextWord(document, currentLine, currentOffset, currentLength, activeSpan.EndColor, hasDefaultColor: false));
				currentOffset += currentLength;
				currentLength = 0;
				i += regString.Length - 1;
				currentSpanStack.Pop();
				UpdateSpanStateVariables();
				continue;
			}
			if (activeRuleSet != null)
			{
				foreach (Span span in activeRuleSet.Spans)
				{
					if ((span.IsBeginSingleWord && currentLength != 0) || (span.IsBeginStartOfLine.HasValue && span.IsBeginStartOfLine.Value != (currentLength == 0 && list.TrueForAll((TextWord textWord) => textWord.Type != TextWordType.Word))) || !MatchExpr(currentLine, span.Begin, i, document, activeRuleSet.IgnoreCase))
					{
						continue;
					}
					PushCurWord(document, ref markNext, list);
					string regString2 = GetRegString(currentLine, span.Begin, i, document);
					if (!OverrideSpan(regString2, document, list, span, ref i))
					{
						currentLength += regString2.Length;
						list.Add(new TextWord(document, currentLine, currentOffset, currentLength, span.BeginColor, hasDefaultColor: false));
						currentOffset += currentLength;
						currentLength = 0;
						i += regString2.Length - 1;
						if (currentSpanStack == null)
						{
							currentSpanStack = new SpanStack();
						}
						currentSpanStack.Push(span);
						span.IgnoreCase = activeRuleSet.IgnoreCase;
						UpdateSpanStateVariables();
					}
					goto IL_08b6;
				}
			}
			if (activeRuleSet != null && charAt < 'Ā' && activeRuleSet.Delimiters[(uint)charAt])
			{
				PushCurWord(document, ref markNext, list);
				if (currentOffset + currentLength + 1 < currentLine.Length)
				{
					currentLength++;
					PushCurWord(document, ref markNext, list);
					continue;
				}
			}
			currentLength++;
			IL_08b6:;
		}
		PushCurWord(document, ref markNext, list);
		OnParsedLine(document, currentLine, list);
		return list;
	}

	protected virtual void OnParsedLine(IDocument document, LineSegment currentLine, List<TextWord> words)
	{
	}

	protected virtual bool OverrideSpan(string spanBegin, IDocument document, List<TextWord> words, Span span, ref int lineOffset)
	{
		return false;
	}

	private void PushCurWord(IDocument document, ref HighlightColor markNext, List<TextWord> words)
	{
		if (currentLength <= 0)
		{
			return;
		}
		if (words.Count > 0 && activeRuleSet != null)
		{
			TextWord textWord = null;
			for (int num = words.Count - 1; num >= 0; num--)
			{
				if (!words[num].IsWhiteSpace)
				{
					textWord = words[num];
					if (textWord.HasDefaultColor)
					{
						PrevMarker prevMarker = (PrevMarker)activeRuleSet.PrevMarkers[document, currentLine, currentOffset, currentLength];
						if (prevMarker != null)
						{
							textWord.SyntaxColor = prevMarker.Color;
						}
					}
					break;
				}
			}
		}
		if (inSpan)
		{
			HighlightColor highlightColor = null;
			bool hasDefaultColor = true;
			if (activeSpan.Rule == null)
			{
				highlightColor = activeSpan.Color;
			}
			else
			{
				highlightColor = GetColor(activeRuleSet, document, currentLine, currentOffset, currentLength);
				hasDefaultColor = false;
			}
			if (highlightColor == null)
			{
				highlightColor = activeSpan.Color;
				if (highlightColor.Color == Color.Transparent)
				{
					highlightColor = DefaultTextColor;
				}
				hasDefaultColor = true;
			}
			words.Add(new TextWord(document, currentLine, currentOffset, currentLength, (markNext != null) ? markNext : highlightColor, hasDefaultColor));
		}
		else
		{
			HighlightColor highlightColor2 = ((markNext != null) ? markNext : GetColor(activeRuleSet, document, currentLine, currentOffset, currentLength));
			if (highlightColor2 == null)
			{
				words.Add(new TextWord(document, currentLine, currentOffset, currentLength, DefaultTextColor, hasDefaultColor: true));
			}
			else
			{
				words.Add(new TextWord(document, currentLine, currentOffset, currentLength, highlightColor2, hasDefaultColor: false));
			}
		}
		if (activeRuleSet != null)
		{
			NextMarker nextMarker = (NextMarker)activeRuleSet.NextMarkers[document, currentLine, currentOffset, currentLength];
			if (nextMarker != null)
			{
				if (nextMarker.MarkMarker && words.Count > 0)
				{
					TextWord textWord2 = words[words.Count - 1];
					textWord2.SyntaxColor = nextMarker.Color;
				}
				markNext = nextMarker.Color;
			}
			else
			{
				markNext = null;
			}
		}
		currentOffset += currentLength;
		currentLength = 0;
	}

	private static string GetRegString(LineSegment lineSegment, char[] expr, int index, IDocument document)
	{
		int num = 0;
		StringBuilder stringBuilder = new StringBuilder();
		int num2 = 0;
		while (num2 < expr.Length && index + num < lineSegment.Length)
		{
			char c = expr[num2];
			if (c == '@')
			{
				num2++;
				switch (expr[num2])
				{
				case '!':
				{
					StringBuilder stringBuilder2 = new StringBuilder();
					num2++;
					while (num2 < expr.Length && expr[num2] != '@')
					{
						stringBuilder2.Append(expr[num2++]);
					}
					break;
				}
				case '@':
					stringBuilder.Append(document.GetCharAt(lineSegment.Offset + index + num));
					break;
				}
			}
			else
			{
				if (expr[num2] != document.GetCharAt(lineSegment.Offset + index + num))
				{
					return stringBuilder.ToString();
				}
				stringBuilder.Append(document.GetCharAt(lineSegment.Offset + index + num));
			}
			num2++;
			num++;
		}
		return stringBuilder.ToString();
	}

	private static bool MatchExpr(LineSegment lineSegment, char[] expr, int index, IDocument document, bool ignoreCase)
	{
		int num = 0;
		int num2 = 0;
		while (num < expr.Length)
		{
			char c = expr[num];
			if (c == '@')
			{
				num++;
				if (num < expr.Length)
				{
					switch (expr[num])
					{
					case 'C':
						if (index + num2 != lineSegment.Offset && index + num2 < lineSegment.Offset + lineSegment.Length)
						{
							char charAt = document.GetCharAt(lineSegment.Offset + index + num2);
							if (!char.IsWhiteSpace(charAt) && !char.IsPunctuation(charAt))
							{
								return false;
							}
						}
						break;
					case '!':
					{
						StringBuilder stringBuilder = new StringBuilder();
						num++;
						while (num < expr.Length && expr[num] != '@')
						{
							stringBuilder.Append(expr[num++]);
						}
						if (lineSegment.Offset + index + num2 + stringBuilder.Length >= document.TextLength)
						{
							break;
						}
						int i;
						for (i = 0; i < stringBuilder.Length; i++)
						{
							char c2 = (ignoreCase ? char.ToUpperInvariant(document.GetCharAt(lineSegment.Offset + index + num2 + i)) : document.GetCharAt(lineSegment.Offset + index + num2 + i));
							char c3 = (ignoreCase ? char.ToUpperInvariant(stringBuilder[i]) : stringBuilder[i]);
							if (c2 != c3)
							{
								break;
							}
						}
						if (i >= stringBuilder.Length)
						{
							return false;
						}
						break;
					}
					case '-':
					{
						StringBuilder stringBuilder2 = new StringBuilder();
						num++;
						while (num < expr.Length && expr[num] != '@')
						{
							stringBuilder2.Append(expr[num++]);
						}
						if (index - stringBuilder2.Length < 0)
						{
							break;
						}
						int j;
						for (j = 0; j < stringBuilder2.Length; j++)
						{
							char c4 = (ignoreCase ? char.ToUpperInvariant(document.GetCharAt(lineSegment.Offset + index - stringBuilder2.Length + j)) : document.GetCharAt(lineSegment.Offset + index - stringBuilder2.Length + j));
							char c5 = (ignoreCase ? char.ToUpperInvariant(stringBuilder2[j]) : stringBuilder2[j]);
							if (c4 != c5)
							{
								break;
							}
						}
						if (j >= stringBuilder2.Length)
						{
							return false;
						}
						break;
					}
					case '@':
						if (index + num2 >= lineSegment.Length || '@' != document.GetCharAt(lineSegment.Offset + index + num2))
						{
							return false;
						}
						break;
					}
				}
			}
			else
			{
				if (index + num2 >= lineSegment.Length)
				{
					return false;
				}
				char c6 = (ignoreCase ? char.ToUpperInvariant(document.GetCharAt(lineSegment.Offset + index + num2)) : document.GetCharAt(lineSegment.Offset + index + num2));
				char c7 = (ignoreCase ? char.ToUpperInvariant(expr[num]) : expr[num]);
				if (c6 != c7)
				{
					return false;
				}
			}
			num++;
			num2++;
		}
		return true;
	}
}
