namespace ICSharpCode.TextEditor.Document;

public interface IBookmarkFactory
{
	Bookmark CreateBookmark(IDocument document, TextLocation location);
}
