using System;

namespace ICSharpCode.TextEditor.Document;

public class FoldMarker : AbstractSegment, IComparable
{
	private bool isFolded;

	private string foldText = "...";

	private FoldType foldType;

	private IDocument document;

	private int startLine = -1;

	private int startColumn;

	private int endLine = -1;

	private int endColumn;

	public FoldType FoldType
	{
		get
		{
			return foldType;
		}
		set
		{
			foldType = value;
		}
	}

	public int StartLine
	{
		get
		{
			if (startLine < 0)
			{
				GetPointForOffset(document, offset, out startLine, out startColumn);
			}
			return startLine;
		}
	}

	public int StartColumn
	{
		get
		{
			if (startLine < 0)
			{
				GetPointForOffset(document, offset, out startLine, out startColumn);
			}
			return startColumn;
		}
	}

	public int EndLine
	{
		get
		{
			if (endLine < 0)
			{
				GetPointForOffset(document, offset + length, out endLine, out endColumn);
			}
			return endLine;
		}
	}

	public int EndColumn
	{
		get
		{
			if (endLine < 0)
			{
				GetPointForOffset(document, offset + length, out endLine, out endColumn);
			}
			return endColumn;
		}
	}

	public override int Offset
	{
		get
		{
			return base.Offset;
		}
		set
		{
			base.Offset = value;
			startLine = -1;
			endLine = -1;
		}
	}

	public override int Length
	{
		get
		{
			return base.Length;
		}
		set
		{
			base.Length = value;
			endLine = -1;
		}
	}

	public bool IsFolded
	{
		get
		{
			return isFolded;
		}
		set
		{
			isFolded = value;
		}
	}

	public string FoldText => foldText;

	public string InnerText => document.GetText(offset, length);

	private static void GetPointForOffset(IDocument document, int offset, out int line, out int column)
	{
		if (offset > document.TextLength)
		{
			line = document.TotalNumberOfLines + 1;
			column = 1;
		}
		else if (offset < 0)
		{
			line = -1;
			column = -1;
		}
		else
		{
			line = document.GetLineNumberForOffset(offset);
			column = offset - document.GetLineSegment(line).Offset;
		}
	}

	public FoldMarker(IDocument document, int offset, int length, string foldText, bool isFolded)
	{
		this.document = document;
		base.offset = offset;
		base.length = length;
		this.foldText = foldText;
		this.isFolded = isFolded;
	}

	public FoldMarker(IDocument document, int startLine, int startColumn, int endLine, int endColumn)
		: this(document, startLine, startColumn, endLine, endColumn, FoldType.Unspecified)
	{
	}

	public FoldMarker(IDocument document, int startLine, int startColumn, int endLine, int endColumn, FoldType foldType)
		: this(document, startLine, startColumn, endLine, endColumn, foldType, "...")
	{
	}

	public FoldMarker(IDocument document, int startLine, int startColumn, int endLine, int endColumn, FoldType foldType, string foldText)
		: this(document, startLine, startColumn, endLine, endColumn, foldType, foldText, isFolded: false)
	{
	}

	public FoldMarker(IDocument document, int startLine, int startColumn, int endLine, int endColumn, FoldType foldType, string foldText, bool isFolded)
	{
		this.document = document;
		startLine = Math.Min(document.TotalNumberOfLines - 1, Math.Max(startLine, 0));
		ISegment lineSegment = document.GetLineSegment(startLine);
		endLine = Math.Min(document.TotalNumberOfLines - 1, Math.Max(endLine, 0));
		ISegment lineSegment2 = document.GetLineSegment(endLine);
		if (string.IsNullOrEmpty(foldText))
		{
			foldText = "...";
		}
		FoldType = foldType;
		this.foldText = foldText;
		offset = lineSegment.Offset + Math.Min(startColumn, lineSegment.Length);
		length = lineSegment2.Offset + Math.Min(endColumn, lineSegment2.Length) - offset;
		this.isFolded = isFolded;
	}

	public int CompareTo(object o)
	{
		if (!(o is FoldMarker))
		{
			throw new ArgumentException();
		}
		FoldMarker foldMarker = (FoldMarker)o;
		if (offset != foldMarker.offset)
		{
			return offset.CompareTo(foldMarker.offset);
		}
		return length.CompareTo(foldMarker.length);
	}
}
