using System;
using System.Runtime.Serialization;

namespace ICSharpCode.TextEditor.Document;

[Serializable]
public class HighlightingColorNotFoundException : Exception
{
	public HighlightingColorNotFoundException()
	{
	}

	public HighlightingColorNotFoundException(string message)
		: base(message)
	{
	}

	public HighlightingColorNotFoundException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	protected HighlightingColorNotFoundException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
