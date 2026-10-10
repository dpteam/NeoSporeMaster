using System.Drawing;
using System.Windows.Forms;

namespace ICSharpCode.TextEditor;

public class HRuler : Control
{
	private TextArea textArea;

	public HRuler(TextArea textArea)
	{
		this.textArea = textArea;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		int num = 0;
		for (float num2 = textArea.TextView.DrawingPosition.Left; num2 < (float)textArea.TextView.DrawingPosition.Right; num2 += (float)textArea.TextView.WideSpaceWidth)
		{
			int num3 = Height * 2 / 3;
			if (num % 5 == 0)
			{
				num3 = Height * 4 / 5;
			}
			if (num % 10 == 0)
			{
				num3 = 1;
			}
			num++;
			graphics.DrawLine(Pens.Black, (int)num2, num3, (int)num2, Height - num3);
		}
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
		e.Graphics.FillRectangle(Brushes.White, new Rectangle(0, 0, Width, Height));
	}
}
