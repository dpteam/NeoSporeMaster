using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ICSharpCode.TextEditor;

public class BrushRegistry
{
	private static Hashtable brushes = new Hashtable();

	private static Hashtable pens = new Hashtable();

	private static Hashtable dotPens = new Hashtable();

	public static Brush GetBrush(Color color)
	{
		if (!brushes.Contains(color))
		{
			Brush brush = new SolidBrush(color);
			brushes.Add(color, brush);
			return brush;
		}
		return brushes[color] as Brush;
	}

	public static Pen GetPen(Color color)
	{
		if (!pens.Contains(color))
		{
			Pen pen = new Pen(color);
			pens.Add(color, pen);
			return pen;
		}
		return pens[color] as Pen;
	}

	public static Pen GetDotPen(Color bgColor, Color fgColor)
	{
		bool flag = dotPens.Contains(bgColor);
		if (!flag || !((Hashtable)dotPens[bgColor]).Contains(fgColor))
		{
			if (!flag)
			{
				dotPens[bgColor] = new Hashtable();
			}
			HatchBrush brush = new HatchBrush(HatchStyle.Percent50, bgColor, fgColor);
			Pen pen = new Pen(brush);
			((Hashtable)dotPens[bgColor])[fgColor] = pen;
			return pen;
		}
		return ((Hashtable)dotPens[bgColor])[fgColor] as Pen;
	}
}
