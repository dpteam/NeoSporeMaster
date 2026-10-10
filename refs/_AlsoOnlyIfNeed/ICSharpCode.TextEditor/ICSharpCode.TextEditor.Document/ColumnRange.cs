namespace ICSharpCode.TextEditor.Document;

public class ColumnRange
{
	public static readonly ColumnRange NoColumn = new ColumnRange(-2, -2);

	public static readonly ColumnRange WholeColumn = new ColumnRange(-1, -1);

	private int startColumn;

	private int endColumn;

	public int StartColumn
	{
		get
		{
			return startColumn;
		}
		set
		{
			startColumn = value;
		}
	}

	public int EndColumn
	{
		get
		{
			return endColumn;
		}
		set
		{
			endColumn = value;
		}
	}

	public ColumnRange(int startColumn, int endColumn)
	{
		this.startColumn = startColumn;
		this.endColumn = endColumn;
	}

	public override int GetHashCode()
	{
		return startColumn + (endColumn << 16);
	}

	public override bool Equals(object obj)
	{
		if (obj is ColumnRange)
		{
			if (((ColumnRange)obj).startColumn == startColumn)
			{
				return ((ColumnRange)obj).endColumn == endColumn;
			}
			return false;
		}
		return false;
	}

	public override string ToString()
	{
		return $"[ColumnRange: StartColumn={startColumn}, EndColumn={endColumn}]";
	}
}
