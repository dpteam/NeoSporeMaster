using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace ICSharpCode.TextEditor.Document;

public class FileSyntaxModeProvider : ISyntaxModeFileProvider
{
	private string directory;

	private List<SyntaxMode> syntaxModes;

	public ICollection<SyntaxMode> SyntaxModes => syntaxModes;

	public FileSyntaxModeProvider(string directory)
	{
		this.directory = directory;
		UpdateSyntaxModeList();
	}

	public void UpdateSyntaxModeList()
	{
		string path = Path.Combine(directory, "SyntaxModes.xml");
		if (File.Exists(path))
		{
			Stream stream = File.OpenRead(path);
			syntaxModes = SyntaxMode.GetSyntaxModes(stream);
			stream.Close();
		}
		else
		{
			syntaxModes = ScanDirectory(directory);
		}
	}

	public XmlTextReader GetSyntaxModeFile(SyntaxMode syntaxMode)
	{
		string text = Path.Combine(directory, syntaxMode.FileName);
		if (!File.Exists(text))
		{
			throw new HighlightingDefinitionInvalidException("Can't load highlighting definition " + text + " (file not found)!");
		}
		return new XmlTextReader(File.OpenRead(text));
	}

	private List<SyntaxMode> ScanDirectory(string directory)
	{
		string[] files = Directory.GetFiles(directory);
		List<SyntaxMode> list = new List<SyntaxMode>();
		string[] array = files;
		foreach (string text in array)
		{
			if (!Path.GetExtension(text).Equals(".XSHD", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			XmlTextReader xmlTextReader = new XmlTextReader(text);
			while (xmlTextReader.Read())
			{
				if (xmlTextReader.NodeType == XmlNodeType.Element)
				{
					string name;
					if ((name = xmlTextReader.Name) != null && name == "SyntaxDefinition")
					{
						string attribute = xmlTextReader.GetAttribute("name");
						string attribute2 = xmlTextReader.GetAttribute("extensions");
						list.Add(new SyntaxMode(Path.GetFileName(text), attribute, attribute2));
						break;
					}
					throw new HighlightingDefinitionInvalidException("Unknown root node in syntax highlighting file :" + xmlTextReader.Name);
				}
			}
			xmlTextReader.Close();
		}
		return list;
	}
}
