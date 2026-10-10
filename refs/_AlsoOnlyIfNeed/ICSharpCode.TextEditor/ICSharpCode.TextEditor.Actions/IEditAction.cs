using System.Windows.Forms;

namespace ICSharpCode.TextEditor.Actions;

public interface IEditAction
{
	Keys[] Keys { get; set; }

	void Execute(TextArea textArea);
}
