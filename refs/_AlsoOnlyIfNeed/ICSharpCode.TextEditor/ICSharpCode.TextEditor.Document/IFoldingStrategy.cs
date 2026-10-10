using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Document;

public interface IFoldingStrategy
{
	List<FoldMarker> GenerateFoldMarkers(IDocument document, string fileName, object parseInformation);
}
