using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

namespace ICSharpCode.TextEditor.Document;

public class ResourceSyntaxModeProvider : ISyntaxModeFileProvider
{
	private List<SyntaxMode> syntaxModes;

	public ICollection<SyntaxMode> SyntaxModes => syntaxModes;

	public ResourceSyntaxModeProvider()
	{
		Assembly assembly = typeof(SyntaxMode).Assembly;
		Stream manifestResourceStream = assembly.GetManifestResourceStream("ICSharpCode.TextEditor.Resources.SyntaxModes.xml");
		if (manifestResourceStream != null)
		{
			syntaxModes = SyntaxMode.GetSyntaxModes(manifestResourceStream);
		}
		else
		{
			syntaxModes = new List<SyntaxMode>();
		}
	}

	public XmlTextReader GetSyntaxModeFile(SyntaxMode syntaxMode)
	{
		Assembly assembly = typeof(SyntaxMode).Assembly;
		return new XmlTextReader(assembly.GetManifestResourceStream("ICSharpCode.TextEditor.Resources." + syntaxMode.FileName));
	}

	public void UpdateSyntaxModeList()
	{
	}
}
