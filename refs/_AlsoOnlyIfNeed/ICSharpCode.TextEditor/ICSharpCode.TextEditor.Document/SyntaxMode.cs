using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace ICSharpCode.TextEditor.Document;

public class SyntaxMode
{
	private string fileName;

	private string name;

	private string[] extensions;

	public string FileName
	{
		get
		{
			return fileName;
		}
		set
		{
			fileName = value;
		}
	}

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

	public SyntaxMode(string fileName, string name, string extensions)
	{
		this.fileName = fileName;
		this.name = name;
		this.extensions = extensions.Split(';', '|', ',');
	}

	public SyntaxMode(string fileName, string name, string[] extensions)
	{
		this.fileName = fileName;
		this.name = name;
		this.extensions = extensions;
	}

	public static List<SyntaxMode> GetSyntaxModes(Stream xmlSyntaxModeStream)
	{
		XmlTextReader xmlTextReader = new XmlTextReader(xmlSyntaxModeStream);
		List<SyntaxMode> list = new List<SyntaxMode>();
		while (xmlTextReader.Read())
		{
			XmlNodeType nodeType = xmlTextReader.NodeType;
			if (nodeType != XmlNodeType.Element)
			{
				continue;
			}
			switch (xmlTextReader.Name)
			{
			case "SyntaxModes":
			{
				string attribute = xmlTextReader.GetAttribute("version");
				if (attribute != "1.0")
				{
					throw new HighlightingDefinitionInvalidException("Unknown syntax mode file defininition with version " + attribute);
				}
				break;
			}
			case "Mode":
				list.Add(new SyntaxMode(xmlTextReader.GetAttribute("file"), xmlTextReader.GetAttribute("name"), xmlTextReader.GetAttribute("extensions")));
				break;
			default:
				throw new HighlightingDefinitionInvalidException("Unknown node in syntax mode file :" + xmlTextReader.Name);
			}
		}
		xmlTextReader.Close();
		return list;
	}

	public override string ToString()
	{
		return string.Format("[SyntaxMode: FileName={0}, Name={1}, Extensions=({2})]", fileName, name, string.Join(",", extensions));
	}
}
