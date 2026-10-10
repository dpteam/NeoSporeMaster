namespace ICSharpCode.TextEditor;

public class TextAreaUpdate
{
	private TextLocation position;

	private TextAreaUpdateType type;

	public TextAreaUpdateType TextAreaUpdateType => type;

	public TextLocation Position => position;

	public TextAreaUpdate(TextAreaUpdateType type)
	{
		this.type = type;
	}

	public TextAreaUpdate(TextAreaUpdateType type, TextLocation position)
	{
		this.type = type;
		this.position = position;
	}

	public TextAreaUpdate(TextAreaUpdateType type, int startLine, int endLine)
	{
		this.type = type;
		position = new TextLocation(startLine, endLine);
	}

	public TextAreaUpdate(TextAreaUpdateType type, int singleLine)
	{
		this.type = type;
		position = new TextLocation(0, singleLine);
	}

	public override string ToString()
	{
		return $"[TextAreaUpdate: Type={type}, Position={position}]";
	}
}
