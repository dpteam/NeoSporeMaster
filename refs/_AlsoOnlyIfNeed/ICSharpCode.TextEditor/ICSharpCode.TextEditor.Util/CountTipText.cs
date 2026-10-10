using System.Drawing;

namespace ICSharpCode.TextEditor.Util;

internal class CountTipText(Graphics graphics, Font font, string text) : TipText(graphics, font, text)
{
	private float triHeight = 10f;

	private float triWidth = 10f;

	public Rectangle DrawingRectangle1;

	public Rectangle DrawingRectangle2;

	private void DrawTriangle(float x, float y, bool flipped)
	{
		Brush brush = BrushRegistry.GetBrush(Color.FromArgb(192, 192, 192));
		Graphics.FillRectangle(brush, new RectangleF(x, y, triHeight, triHeight));
		float num = triHeight / 2f;
		float num2 = triHeight / 4f;
		brush = Brushes.Black;
		if (flipped)
		{
			Graphics.FillPolygon(brush, new PointF[3]
			{
				new PointF(x, y + num - num2),
				new PointF(x + triWidth / 2f, y + num + num2),
				new PointF(x + triWidth, y + num - num2)
			});
		}
		else
		{
			Graphics.FillPolygon(brush, new PointF[3]
			{
				new PointF(x, y + num + num2),
				new PointF(x + triWidth / 2f, y + num - num2),
				new PointF(x + triWidth, y + num + num2)
			});
		}
	}

	public override void Draw(PointF location)
	{
		if (tipText != null && tipText.Length > 0)
		{
			base.Draw(new PointF(location.X + triWidth + 4f, location.Y));
			DrawingRectangle1 = new Rectangle((int)location.X + 2, (int)location.Y + 2, (int)triWidth, (int)triHeight);
			DrawingRectangle2 = new Rectangle((int)(location.X + AllocatedSize.Width - triWidth - 2f), (int)location.Y + 2, (int)triWidth, (int)triHeight);
			DrawTriangle(location.X + 2f, location.Y + 2f, flipped: false);
			DrawTriangle(location.X + AllocatedSize.Width - triWidth - 2f, location.Y + 2f, flipped: true);
		}
	}

	protected override void OnMaximumSizeChanged()
	{
		if (IsTextVisible())
		{
			SizeF requiredSize = Graphics.MeasureString(tipText, tipFont, MaximumSize, GetInternalStringFormat());
			requiredSize.Width += triWidth * 2f + 8f;
			SetRequiredSize(requiredSize);
		}
		else
		{
			SetRequiredSize(SizeF.Empty);
		}
	}
}
