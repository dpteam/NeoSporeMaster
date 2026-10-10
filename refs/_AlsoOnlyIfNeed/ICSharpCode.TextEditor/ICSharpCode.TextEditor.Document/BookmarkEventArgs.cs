using System;

namespace ICSharpCode.TextEditor.Document;

public class BookmarkEventArgs : EventArgs
{
	private Bookmark bookmark;

	public Bookmark Bookmark => bookmark;

	public BookmarkEventArgs(Bookmark bookmark)
	{
		this.bookmark = bookmark;
	}
}
