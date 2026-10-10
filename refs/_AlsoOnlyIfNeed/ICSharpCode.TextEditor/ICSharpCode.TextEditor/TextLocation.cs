using System;

namespace ICSharpCode.TextEditor;

public struct TextLocation(int column, int line) : IComparable<TextLocation>, IEquatable<TextLocation>
{
	public static readonly TextLocation Empty = new TextLocation(-1, -1);

	private int x = column;

	private int y = line;

	public int X
	{
		get
		{
			return x;
		}
		set
		{
			x = value;
		}
	}

	public int Y
	{
		get
		{
			return y;
		}
		set
		{
			y = value;
		}
	}

	public int Line
	{
		get
		{
			return y;
		}
		set
		{
			y = value;
		}
	}

	public int Column
	{
		get
		{
			return x;
		}
		set
		{
			x = value;
		}
	}

	public bool IsEmpty
	{
		get
		{
			if (x <= 0)
			{
				return y <= 0;
			}
			return false;
		}
	}

	public override string ToString()
	{
		return string.Format("(Line {1}, Col {0})", x, y);
	}

	public override int GetHashCode()
	{
		return (87 * x.GetHashCode()) ^ y.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (!(obj is TextLocation))
		{
			return false;
		}
		return (TextLocation)obj == this;
	}

	public bool Equals(TextLocation other)
	{
		return this == other;
	}

	public static bool operator ==(TextLocation a, TextLocation b)
	{
		if (a.x == b.x)
		{
			return a.y == b.y;
		}
		return false;
	}

	public static bool operator !=(TextLocation a, TextLocation b)
	{
		if (a.x == b.x)
		{
			return a.y != b.y;
		}
		return true;
	}

	public static bool operator <(TextLocation a, TextLocation b)
	{
		if (a.y < b.y)
		{
			return true;
		}
		if (a.y == b.y)
		{
			return a.x < b.x;
		}
		return false;
	}

	public static bool operator >(TextLocation a, TextLocation b)
	{
		if (a.y > b.y)
		{
			return true;
		}
		if (a.y == b.y)
		{
			return a.x > b.x;
		}
		return false;
	}

	public static bool operator <=(TextLocation a, TextLocation b)
	{
		return !(a > b);
	}

	public static bool operator >=(TextLocation a, TextLocation b)
	{
		return !(a < b);
	}

	public int CompareTo(TextLocation other)
	{
		if (this == other)
		{
			return 0;
		}
		if (this < other)
		{
			return -1;
		}
		return 1;
	}
}
