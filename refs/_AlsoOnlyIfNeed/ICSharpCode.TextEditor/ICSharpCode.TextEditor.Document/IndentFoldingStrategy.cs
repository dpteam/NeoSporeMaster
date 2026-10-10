using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Document;

public class IndentFoldingStrategy : IFoldingStrategy
{
	public List<FoldMarker> GenerateFoldMarkers(IDocument document, string fileName, object parseInformation)
	{
		List<FoldMarker> result = new List<FoldMarker>();
		new Stack<int>();
		new Stack<string>();
		return result;
	}

	private int GetLevel(IDocument document, int offset)
	{
		int i = 0;
		int num = 0;
		for (int j = offset; j < document.TextLength; num = 0, i++, j++)
		{
			switch (document.GetCharAt(j))
			{
			case ' ':
				if (++num == 4)
				{
					continue;
				}
				break;
			case '\t':
				continue;
			}
			break;
		}
		return i;
	}
}
