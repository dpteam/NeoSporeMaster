using System;

namespace ICSharpCode.TextEditor.Document;

public class AbstractSegment : ISegment
{
	[CLSCompliant(false)]
	protected int offset = -1;

	[CLSCompliant(false)]
	protected int length = -1;

	public virtual int Offset
	{
		get
		{
			return offset;
		}
		set
		{
			offset = value;
		}
	}

	public virtual int Length
	{
		get
		{
			return length;
		}
		set
		{
			length = value;
		}
	}

	public override string ToString()
	{
		return $"[AbstractSegment: Offset = {Offset}, Length = {Length}]";
	}
}
