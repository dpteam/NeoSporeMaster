using System.Drawing;
using System.Windows.Forms;

namespace ICSharpCode.TextEditor.Util;

internal class TipPainterTools
{
	private const int SpacerSize = 4;

	public static Rectangle DrawingRectangle1;

	public static Rectangle DrawingRectangle2;

	private TipPainterTools()
	{
	}

	public static Size GetDrawingSizeHelpTipFromCombinedDescription(Control control, Graphics graphics, Font font, string countMessage, string description)
	{
		string basicDescription = null;
		string documentation = null;
		if (IsVisibleText(description))
		{
			string[] array = description.Split(new char[1] { '\n' }, 2);
			if (array.Length > 0)
			{
				basicDescription = array[0];
				if (array.Length > 1)
				{
					documentation = array[1].Trim();
				}
			}
		}
		return GetDrawingSizeDrawHelpTip(control, graphics, font, countMessage, basicDescription, documentation);
	}

	public static Size DrawHelpTipFromCombinedDescription(Control control, Graphics graphics, Font font, string countMessage, string description)
	{
		string basicDescription = null;
		string documentation = null;
		if (IsVisibleText(description))
		{
			string[] array = description.Split(new char[1] { '\n' }, 2);
			if (array.Length > 0)
			{
				basicDescription = array[0];
				if (array.Length > 1)
				{
					documentation = array[1].Trim();
				}
			}
		}
		return DrawHelpTip(control, graphics, font, countMessage, basicDescription, documentation);
	}

	public static Size GetDrawingSizeDrawHelpTip(Control control, Graphics graphics, Font font, string countMessage, string basicDescription, string documentation)
	{
		if (IsVisibleText(countMessage) || IsVisibleText(basicDescription) || IsVisibleText(documentation))
		{
			CountTipText countTipText = new CountTipText(graphics, font, countMessage);
			TipSpacer tipSpacer = new TipSpacer(graphics, new SizeF(IsVisibleText(countMessage) ? 4 : 0, 0f));
			TipText tipText = new TipText(graphics, font, basicDescription);
			TipSpacer tipSpacer2 = new TipSpacer(graphics, new SizeF(0f, IsVisibleText(documentation) ? 4 : 0));
			TipText tipText2 = new TipText(graphics, font, documentation);
			TipSplitter tipSplitter = new TipSplitter(graphics, false, tipText, tipSpacer2);
			TipSplitter tipSplitter2 = new TipSplitter(graphics, true, countTipText, tipSpacer, tipSplitter);
			TipSplitter tipData = new TipSplitter(graphics, false, tipSplitter2, tipText2);
			Size tipSize = TipPainter.GetTipSize(control, graphics, tipData);
			DrawingRectangle1 = countTipText.DrawingRectangle1;
			DrawingRectangle2 = countTipText.DrawingRectangle2;
			return tipSize;
		}
		return Size.Empty;
	}

	public static Size DrawHelpTip(Control control, Graphics graphics, Font font, string countMessage, string basicDescription, string documentation)
	{
		if (IsVisibleText(countMessage) || IsVisibleText(basicDescription) || IsVisibleText(documentation))
		{
			CountTipText countTipText = new CountTipText(graphics, font, countMessage);
			TipSpacer tipSpacer = new TipSpacer(graphics, new SizeF(IsVisibleText(countMessage) ? 4 : 0, 0f));
			TipText tipText = new TipText(graphics, font, basicDescription);
			TipSpacer tipSpacer2 = new TipSpacer(graphics, new SizeF(0f, IsVisibleText(documentation) ? 4 : 0));
			TipText tipText2 = new TipText(graphics, font, documentation);
			TipSplitter tipSplitter = new TipSplitter(graphics, false, tipText, tipSpacer2);
			TipSplitter tipSplitter2 = new TipSplitter(graphics, true, countTipText, tipSpacer, tipSplitter);
			TipSplitter tipData = new TipSplitter(graphics, false, tipSplitter2, tipText2);
			Size result = TipPainter.DrawTip(control, graphics, tipData);
			DrawingRectangle1 = countTipText.DrawingRectangle1;
			DrawingRectangle2 = countTipText.DrawingRectangle2;
			return result;
		}
		return Size.Empty;
	}

	private static bool IsVisibleText(string text)
	{
		if (text != null)
		{
			return text.Length > 0;
		}
		return false;
	}
}
