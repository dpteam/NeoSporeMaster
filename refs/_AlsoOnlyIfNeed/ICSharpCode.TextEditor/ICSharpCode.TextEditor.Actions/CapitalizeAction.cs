using System.Text;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions;

public class CapitalizeAction : AbstractSelectionFormatAction
{
	protected override void Convert(IDocument document, int startOffset, int length)
	{
		StringBuilder stringBuilder = new StringBuilder(document.GetText(startOffset, length));
		for (int i = 0; i < stringBuilder.Length; i++)
		{
			if (!char.IsLetter(stringBuilder[i]) && i < stringBuilder.Length - 1)
			{
				stringBuilder[i + 1] = char.ToUpper(stringBuilder[i + 1]);
			}
		}
		document.Replace(startOffset, length, stringBuilder.ToString());
	}
}
