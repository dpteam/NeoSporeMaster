using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace ICSharpCode.TextEditor.Document;

public static class HighlightingDefinitionParser
{
	public static DefaultHighlightingStrategy Parse(SyntaxMode syntaxMode, XmlReader xmlReader)
	{
		return Parse(null, syntaxMode, xmlReader);
	}

	public static DefaultHighlightingStrategy Parse(DefaultHighlightingStrategy highlighter, SyntaxMode syntaxMode, XmlReader xmlReader)
	{
		if (syntaxMode == null)
		{
			throw new ArgumentNullException("syntaxMode");
		}
		if (xmlReader == null)
		{
			throw new ArgumentNullException("xmlTextReader");
		}
		try
		{
			List<ValidationEventArgs> errors = null;
			XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
			Stream manifestResourceStream = typeof(HighlightingDefinitionParser).Assembly.GetManifestResourceStream("ICSharpCode.TextEditor.Resources.Mode.xsd");
			xmlReaderSettings.Schemas.Add("", new XmlTextReader(manifestResourceStream));
			xmlReaderSettings.Schemas.ValidationEventHandler += (object sender, ValidationEventArgs args) =>
			{
				if (errors == null)
				{
					errors = new List<ValidationEventArgs>();
				}
				errors.Add(args);
			};
			xmlReaderSettings.ValidationType = ValidationType.Schema;
			XmlReader reader = XmlReader.Create(xmlReader, xmlReaderSettings);
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(reader);
			if (highlighter == null)
			{
				highlighter = new DefaultHighlightingStrategy(xmlDocument.DocumentElement.Attributes["name"].InnerText);
			}
			if (xmlDocument.DocumentElement.HasAttribute("extends"))
			{
				KeyValuePair<SyntaxMode, ISyntaxModeFileProvider> keyValuePair = HighlightingManager.Manager.FindHighlighterEntry(xmlDocument.DocumentElement.GetAttribute("extends"));
				if (keyValuePair.Key == null)
				{
					throw new HighlightingDefinitionInvalidException("Cannot find referenced highlighting source " + xmlDocument.DocumentElement.GetAttribute("extends"));
				}
				highlighter = Parse(highlighter, keyValuePair.Key, keyValuePair.Value.GetSyntaxModeFile(keyValuePair.Key));
				if (highlighter == null)
				{
					return null;
				}
			}
			if (xmlDocument.DocumentElement.HasAttribute("extensions"))
			{
				highlighter.Extensions = xmlDocument.DocumentElement.GetAttribute("extensions").Split(';', '|');
			}
			XmlElement xmlElement = xmlDocument.DocumentElement["Environment"];
			if (xmlElement != null)
			{
				foreach (XmlNode childNode in xmlElement.ChildNodes)
				{
					if (childNode is XmlElement)
					{
						XmlElement xmlElement2 = (XmlElement)childNode;
						if (xmlElement2.Name == "Custom")
						{
							highlighter.SetColorFor(xmlElement2.GetAttribute("name"), xmlElement2.HasAttribute("bgcolor") ? new HighlightBackground(xmlElement2) : new HighlightColor(xmlElement2));
						}
						else
						{
							highlighter.SetColorFor(xmlElement2.Name, xmlElement2.HasAttribute("bgcolor") ? new HighlightBackground(xmlElement2) : new HighlightColor(xmlElement2));
						}
					}
				}
			}
			if (xmlDocument.DocumentElement["Properties"] != null)
			{
				foreach (XmlElement childNode2 in xmlDocument.DocumentElement["Properties"].ChildNodes)
				{
					highlighter.Properties[childNode2.Attributes["name"].InnerText] = childNode2.Attributes["value"].InnerText;
				}
			}
			if (xmlDocument.DocumentElement["Digits"] != null)
			{
				highlighter.DigitColor = new HighlightColor(xmlDocument.DocumentElement["Digits"]);
			}
			XmlNodeList elementsByTagName = xmlDocument.DocumentElement.GetElementsByTagName("RuleSet");
			foreach (XmlElement item in elementsByTagName)
			{
				highlighter.AddRuleSet(new HighlightRuleSet(item));
			}
			xmlReader.Close();
			if (errors != null)
			{
				StringBuilder stringBuilder = new StringBuilder();
				foreach (ValidationEventArgs item2 in errors)
				{
					stringBuilder.AppendLine(item2.Message);
				}
				throw new HighlightingDefinitionInvalidException(stringBuilder.ToString());
			}
			return highlighter;
		}
		catch (Exception innerException)
		{
			throw new HighlightingDefinitionInvalidException("Could not load mode definition file '" + syntaxMode.FileName + "'.\n", innerException);
		}
	}
}
