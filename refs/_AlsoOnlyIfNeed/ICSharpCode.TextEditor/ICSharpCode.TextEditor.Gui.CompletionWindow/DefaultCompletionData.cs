using System;

namespace ICSharpCode.TextEditor.Gui.CompletionWindow;

public class DefaultCompletionData : ICompletionData
{
	private string text;

	private string description;

	private int imageIndex;

	private double priority;

	public int ImageIndex => imageIndex;

	public string Text
	{
		get
		{
			return text;
		}
		set
		{
			text = value;
		}
	}

	public string Description => description;

	public double Priority
	{
		get
		{
			return priority;
		}
		set
		{
			priority = value;
		}
	}

	public virtual bool InsertAction(TextArea textArea, char ch)
	{
		textArea.InsertString(text);
		return false;
	}

	public DefaultCompletionData(string text, string description, int imageIndex)
	{
		this.text = text;
		this.description = description;
		this.imageIndex = imageIndex;
	}

	public static int Compare(ICompletionData a, ICompletionData b)
	{
		if (a == null)
		{
			throw new ArgumentNullException("a");
		}
		if (b == null)
		{
			throw new ArgumentNullException("b");
		}
		return string.Compare(a.Text, b.Text, StringComparison.InvariantCultureIgnoreCase);
	}
}
