using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace ICSharpCode.TextEditor.Document;

public class HighlightingManager
{
	private ArrayList syntaxModeFileProviders = new ArrayList();

	private static HighlightingManager highlightingManager;

	private Hashtable highlightingDefs = new Hashtable();

	private Hashtable extensionsToName = new Hashtable();

	public Hashtable HighlightingDefinitions => highlightingDefs;

	public static HighlightingManager Manager => highlightingManager;

	public DefaultHighlightingStrategy DefaultHighlighting => (DefaultHighlightingStrategy)highlightingDefs["Default"];

	public event EventHandler ReloadSyntaxHighlighting;

	static HighlightingManager()
	{
		highlightingManager = new HighlightingManager();
		highlightingManager.AddSyntaxModeFileProvider(new ResourceSyntaxModeProvider());
	}

	public HighlightingManager()
	{
		CreateDefaultHighlightingStrategy();
	}

	public void AddSyntaxModeFileProvider(ISyntaxModeFileProvider syntaxModeFileProvider)
	{
		foreach (SyntaxMode syntaxMode in syntaxModeFileProvider.SyntaxModes)
		{
			highlightingDefs[syntaxMode.Name] = new DictionaryEntry(syntaxMode, syntaxModeFileProvider);
			string[] extensions = syntaxMode.Extensions;
			foreach (string text in extensions)
			{
				extensionsToName[text.ToUpperInvariant()] = syntaxMode.Name;
			}
		}
		if (!syntaxModeFileProviders.Contains(syntaxModeFileProvider))
		{
			syntaxModeFileProviders.Add(syntaxModeFileProvider);
		}
	}

	public void AddHighlightingStrategy(IHighlightingStrategy highlightingStrategy)
	{
		highlightingDefs[highlightingStrategy.Name] = highlightingStrategy;
		string[] extensions = highlightingStrategy.Extensions;
		foreach (string text in extensions)
		{
			extensionsToName[text.ToUpperInvariant()] = highlightingStrategy.Name;
		}
	}

	public void ReloadSyntaxModes()
	{
		highlightingDefs.Clear();
		extensionsToName.Clear();
		CreateDefaultHighlightingStrategy();
		foreach (ISyntaxModeFileProvider syntaxModeFileProvider in syntaxModeFileProviders)
		{
			syntaxModeFileProvider.UpdateSyntaxModeList();
			AddSyntaxModeFileProvider(syntaxModeFileProvider);
		}
		OnReloadSyntaxHighlighting(EventArgs.Empty);
	}

	private void CreateDefaultHighlightingStrategy()
	{
		DefaultHighlightingStrategy defaultHighlightingStrategy = new DefaultHighlightingStrategy();
		defaultHighlightingStrategy.Extensions = new string[0];
		defaultHighlightingStrategy.Rules.Add(new HighlightRuleSet());
		highlightingDefs["Default"] = defaultHighlightingStrategy;
	}

	private IHighlightingStrategy LoadDefinition(DictionaryEntry entry)
	{
		SyntaxMode syntaxMode = (SyntaxMode)entry.Key;
		ISyntaxModeFileProvider syntaxModeFileProvider = (ISyntaxModeFileProvider)entry.Value;
		DefaultHighlightingStrategy defaultHighlightingStrategy = null;
		try
		{
			XmlTextReader syntaxModeFile = syntaxModeFileProvider.GetSyntaxModeFile(syntaxMode);
			if (syntaxModeFile == null)
			{
				throw new HighlightingDefinitionInvalidException("Could not get syntax mode file for " + syntaxMode.Name);
			}
			defaultHighlightingStrategy = HighlightingDefinitionParser.Parse(syntaxMode, syntaxModeFile);
			if (defaultHighlightingStrategy.Name != syntaxMode.Name)
			{
				throw new HighlightingDefinitionInvalidException("The name specified in the .xshd '" + defaultHighlightingStrategy.Name + "' must be equal the syntax mode name '" + syntaxMode.Name + "'");
			}
		}
		finally
		{
			if (defaultHighlightingStrategy == null)
			{
				defaultHighlightingStrategy = DefaultHighlighting;
			}
			highlightingDefs[syntaxMode.Name] = defaultHighlightingStrategy;
			defaultHighlightingStrategy.ResolveReferences();
		}
		return defaultHighlightingStrategy;
	}

	internal KeyValuePair<SyntaxMode, ISyntaxModeFileProvider> FindHighlighterEntry(string name)
	{
		foreach (ISyntaxModeFileProvider syntaxModeFileProvider in syntaxModeFileProviders)
		{
			foreach (SyntaxMode syntaxMode in syntaxModeFileProvider.SyntaxModes)
			{
				if (syntaxMode.Name == name)
				{
					return new KeyValuePair<SyntaxMode, ISyntaxModeFileProvider>(syntaxMode, syntaxModeFileProvider);
				}
			}
		}
		return new KeyValuePair<SyntaxMode, ISyntaxModeFileProvider>(null, null);
	}

	public IHighlightingStrategy FindHighlighter(string name)
	{
		object obj = highlightingDefs[name];
		if (obj is DictionaryEntry)
		{
			return LoadDefinition((DictionaryEntry)obj);
		}
		if (obj != null)
		{
			return (IHighlightingStrategy)obj;
		}
		return DefaultHighlighting;
	}

	public IHighlightingStrategy FindHighlighterForFile(string fileName)
	{
		string text = (string)extensionsToName[Path.GetExtension(fileName).ToUpperInvariant()];
		if (text != null)
		{
			object obj = highlightingDefs[text];
			if (obj is DictionaryEntry)
			{
				return LoadDefinition((DictionaryEntry)obj);
			}
			if (obj != null)
			{
				return (IHighlightingStrategy)obj;
			}
			return DefaultHighlighting;
		}
		return DefaultHighlighting;
	}

	protected virtual void OnReloadSyntaxHighlighting(EventArgs e)
	{
		if (ReloadSyntaxHighlighting != null)
		{
			ReloadSyntaxHighlighting(this, e);
		}
	}
}
