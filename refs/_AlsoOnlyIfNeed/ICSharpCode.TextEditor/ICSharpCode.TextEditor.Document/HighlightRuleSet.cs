using System.Collections;
using System.Xml;
using ICSharpCode.TextEditor.Util;

namespace ICSharpCode.TextEditor.Document;

public class HighlightRuleSet
{
	private LookupTable keyWords;

	private ArrayList spans = new ArrayList();

	private LookupTable prevMarkers;

	private LookupTable nextMarkers;

	private char escapeCharacter;

	private bool ignoreCase;

	private string name;

	private bool[] delimiters = new bool[256];

	private string reference;

	internal IHighlightingStrategyUsingRuleSets Highlighter;

	public ArrayList Spans => spans;

	public LookupTable KeyWords => keyWords;

	public LookupTable PrevMarkers => prevMarkers;

	public LookupTable NextMarkers => nextMarkers;

	public bool[] Delimiters => delimiters;

	public char EscapeCharacter => escapeCharacter;

	public bool IgnoreCase => ignoreCase;

	public string Name
	{
		get
		{
			return name;
		}
		set
		{
			name = value;
		}
	}

	public string Reference => reference;

	public HighlightRuleSet()
	{
		keyWords = new LookupTable(casesensitive: false);
		prevMarkers = new LookupTable(casesensitive: false);
		nextMarkers = new LookupTable(casesensitive: false);
	}

	public HighlightRuleSet(XmlElement el)
	{
		if (el.Attributes["name"] != null)
		{
			Name = el.Attributes["name"].InnerText;
		}
		if (el.HasAttribute("escapecharacter"))
		{
			escapeCharacter = el.GetAttribute("escapecharacter")[0];
		}
		if (el.Attributes["reference"] != null)
		{
			reference = el.Attributes["reference"].InnerText;
		}
		if (el.Attributes["ignorecase"] != null)
		{
			ignoreCase = bool.Parse(el.Attributes["ignorecase"].InnerText);
		}
		for (int i = 0; i < Delimiters.Length; i++)
		{
			delimiters[i] = false;
		}
		if (el["Delimiters"] != null)
		{
			string innerText = el["Delimiters"].InnerText;
			string text = innerText;
			foreach (char c in text)
			{
				delimiters[(uint)c] = true;
			}
		}
		keyWords = new LookupTable(!IgnoreCase);
		prevMarkers = new LookupTable(!IgnoreCase);
		nextMarkers = new LookupTable(!IgnoreCase);
		XmlNodeList elementsByTagName = el.GetElementsByTagName("KeyWords");
		foreach (XmlElement item in elementsByTagName)
		{
			HighlightColor value = new HighlightColor(item);
			XmlNodeList elementsByTagName2 = item.GetElementsByTagName("Key");
			foreach (XmlElement item2 in elementsByTagName2)
			{
				keyWords[item2.Attributes["word"].InnerText] = value;
			}
		}
		elementsByTagName = el.GetElementsByTagName("Span");
		foreach (XmlElement item3 in elementsByTagName)
		{
			Spans.Add(new Span(item3));
		}
		elementsByTagName = el.GetElementsByTagName("MarkPrevious");
		foreach (XmlElement item4 in elementsByTagName)
		{
			PrevMarker prevMarker = new PrevMarker(item4);
			prevMarkers[prevMarker.What] = prevMarker;
		}
		elementsByTagName = el.GetElementsByTagName("MarkFollowing");
		foreach (XmlElement item5 in elementsByTagName)
		{
			NextMarker nextMarker = new NextMarker(item5);
			nextMarkers[nextMarker.What] = nextMarker;
		}
	}

	public void MergeFrom(HighlightRuleSet ruleSet)
	{
		for (int i = 0; i < delimiters.Length; i++)
		{
			ref bool reference = ref delimiters[i];
			reference |= ruleSet.delimiters[i];
		}
		ArrayList c = spans;
		spans = (ArrayList)ruleSet.spans.Clone();
		spans.AddRange(c);
	}
}
