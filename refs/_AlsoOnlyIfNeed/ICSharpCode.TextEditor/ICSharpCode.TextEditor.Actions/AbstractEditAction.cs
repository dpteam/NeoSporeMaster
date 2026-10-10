using System.Windows.Forms;

namespace ICSharpCode.TextEditor.Actions;

public abstract class AbstractEditAction : IEditAction
{
	private Keys[] keys;

	public Keys[] Keys
	{
		get
		{
			return keys;
		}
		set
		{
			keys = value;
		}
	}

	public abstract void Execute(TextArea textArea);
}
