using System;
using System.Drawing;

namespace ICSharpCode.TextEditor.Util;

internal class TipText : TipSection
{
	protected StringAlignment horzAlign;

	protected StringAlignment vertAlign;

	protected Color tipColor;

	protected Font tipFont;

	protected StringFormat tipFormat;

	protected string tipText;

	public Color Color
	{
		get
		{
			return tipColor;
		}
		set
		{
			tipColor = value;
		}
	}

	public StringAlignment HorizontalAlignment
	{
		get
		{
			return horzAlign;
		}
		set
		{
			horzAlign = value;
			tipFormat = null;
		}
	}

	public StringAlignment VerticalAlignment
	{
		get
		{
			return vertAlign;
		}
		set
		{
			vertAlign = value;
			tipFormat = null;
		}
	}

	public TipText(Graphics graphics, Font font, string text)
		: base(graphics)
	{
		tipFont = font;
		tipText = text;
		if (text != null && text.Length > 32767)
		{
			throw new ArgumentException("TipText: text too long (max. is " + short.MaxValue + " characters)", "text");
		}
		Color = SystemColors.InfoText;
		HorizontalAlignment = StringAlignment.Near;
		VerticalAlignment = StringAlignment.Near;
	}

	public override void Draw(PointF location)
	{
		if (IsTextVisible())
		{
			RectangleF layoutRectangle = new RectangleF(location, AllocatedSize);
			Graphics.DrawString(tipText, tipFont, BrushRegistry.GetBrush(Color), layoutRectangle, GetInternalStringFormat());
		}
	}

	protected StringFormat GetInternalStringFormat()
	{
		if (tipFormat == null)
		{
			tipFormat = CreateTipStringFormat(horzAlign, vertAlign);
		}
		return tipFormat;
	}

	protected override void OnMaximumSizeChanged()
	{
		base.OnMaximumSizeChanged();
		if (IsTextVisible())
		{
			SizeF requiredSize = Graphics.MeasureString(tipText, tipFont, MaximumSize, GetInternalStringFormat());
			SetRequiredSize(requiredSize);
		}
		else
		{
			SetRequiredSize(SizeF.Empty);
		}
	}

	private static StringFormat CreateTipStringFormat(StringAlignment horizontalAlignment, StringAlignment verticalAlignment)
	{
		StringFormat stringFormat = (StringFormat)StringFormat.GenericTypographic.Clone();
		stringFormat.FormatFlags = StringFormatFlags.FitBlackBox | StringFormatFlags.MeasureTrailingSpaces;
		stringFormat.Alignment = horizontalAlignment;
		stringFormat.LineAlignment = verticalAlignment;
		return stringFormat;
	}

	protected bool IsTextVisible()
	{
		if (tipText != null)
		{
			return tipText.Length > 0;
		}
		return false;
	}
}
