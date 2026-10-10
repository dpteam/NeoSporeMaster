using System;
using System.Windows.Forms;

namespace ICSharpCode.TextEditor.Util;

internal class MouseWheelHandler
{
	private const int WHEEL_DELTA = 120;

	private int mouseWheelDelta;

	public int GetScrollAmount(MouseEventArgs e)
	{
		mouseWheelDelta += e.Delta;
		int num = Math.Max(SystemInformation.MouseWheelScrollLines, 1);
		int result = mouseWheelDelta * num / 120;
		mouseWheelDelta %= Math.Max(1, 120 / num);
		return result;
	}
}
