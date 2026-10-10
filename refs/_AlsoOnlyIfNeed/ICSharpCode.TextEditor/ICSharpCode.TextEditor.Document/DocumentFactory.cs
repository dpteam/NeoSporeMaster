using System.Text;
using ICSharpCode.TextEditor.Util;

namespace ICSharpCode.TextEditor.Document;

public class DocumentFactory
{
	public IDocument CreateDocument()
	{
		DefaultDocument defaultDocument = new DefaultDocument();
		defaultDocument.TextBufferStrategy = new GapTextBufferStrategy();
		defaultDocument.FormattingStrategy = new DefaultFormattingStrategy();
		defaultDocument.LineManager = new LineManager(defaultDocument, null);
		defaultDocument.FoldingManager = new FoldingManager(defaultDocument, defaultDocument.LineManager);
		defaultDocument.FoldingManager.FoldingStrategy = null;
		defaultDocument.MarkerStrategy = new MarkerStrategy(defaultDocument);
		defaultDocument.BookmarkManager = new BookmarkManager(defaultDocument, defaultDocument.LineManager);
		return defaultDocument;
	}

	public IDocument CreateFromTextBuffer(ITextBufferStrategy textBuffer)
	{
		DefaultDocument defaultDocument = (DefaultDocument)CreateDocument();
		defaultDocument.TextContent = textBuffer.GetText(0, textBuffer.Length);
		defaultDocument.TextBufferStrategy = textBuffer;
		return defaultDocument;
	}

	public IDocument CreateFromFile(string fileName)
	{
		IDocument document = CreateDocument();
		document.TextContent = FileReader.ReadFileContent(fileName, Encoding.Default);
		return document;
	}
}
