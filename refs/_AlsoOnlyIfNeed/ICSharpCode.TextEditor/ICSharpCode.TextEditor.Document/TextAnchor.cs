using System;

namespace ICSharpCode.TextEditor.Document;

public sealed class TextAnchor
{
	private LineSegment lineSegment;

	private int columnNumber;

	public LineSegment Line
	{
		get
		{
			if (lineSegment == null)
			{
				throw AnchorDeletedError();
			}
			return lineSegment;
		}
		internal set
		{
			lineSegment = value;
		}
	}

	public bool IsDeleted => lineSegment == null;

	public int LineNumber => Line.LineNumber;

	public int ColumnNumber
	{
		get
		{
			if (lineSegment == null)
			{
				throw AnchorDeletedError();
			}
			return columnNumber;
		}
		internal set
		{
			columnNumber = value;
		}
	}

	public TextLocation Location => new TextLocation(ColumnNumber, LineNumber);

	public int Offset => Line.Offset + columnNumber;

	public AnchorMovementType MovementType { get; set; }

	public event EventHandler Deleted;

	private static Exception AnchorDeletedError()
	{
		return new InvalidOperationException("The text containing the anchor was deleted");
	}

	internal void Delete(ref DeferredEventList deferredEventList)
	{
		lineSegment = null;
		deferredEventList.AddDeletedAnchor(this);
	}

	internal void RaiseDeleted()
	{
		if (Deleted != null)
		{
			Deleted(this, EventArgs.Empty);
		}
	}

	internal TextAnchor(LineSegment lineSegment, int columnNumber)
	{
		this.lineSegment = lineSegment;
		this.columnNumber = columnNumber;
	}

	public override string ToString()
	{
		if (IsDeleted)
		{
			return "[TextAnchor (deleted)]";
		}
		return "[TextAnchor " + Location.ToString() + "]";
	}
}
