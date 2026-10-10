using System;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class ClearAllBookmarks : AbstractEditAction
{
	private Predicate<Bookmark> predicate;

	public ClearAllBookmarks(Predicate<Bookmark> predicate)
	{
		this.predicate = predicate;
	}

	public override void Execute(TextArea textArea)
	{
		textArea.Document.BookmarkManager.RemoveMarks(predicate);
		textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
		textArea.Document.CommitUpdate();
	}
}
