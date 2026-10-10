using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using ICSharpCode.TextEditor.Util;

namespace ICSharpCode.TextEditor.Gui.InsightWindow;

public class InsightWindow : AbstractCompletionWindow
{
	private class InsightDataProviderStackElement
	{
		public int currentData;

		public IInsightDataProvider dataProvider;

		public InsightDataProviderStackElement(IInsightDataProvider dataProvider)
		{
			currentData = Math.Max(dataProvider.DefaultIndex, 0);
			this.dataProvider = dataProvider;
		}
	}

	private MouseWheelHandler mouseWheelHandler = new MouseWheelHandler();

	private Stack<InsightDataProviderStackElement> insightDataProviderStack = new Stack<InsightDataProviderStackElement>();

	private int CurrentData
	{
		get
		{
			return insightDataProviderStack.Peek().currentData;
		}
		set
		{
			insightDataProviderStack.Peek().currentData = value;
		}
	}

	private IInsightDataProvider DataProvider
	{
		get
		{
			if (insightDataProviderStack.Count == 0)
			{
				return null;
			}
			return insightDataProviderStack.Peek().dataProvider;
		}
	}

	public InsightWindow(Form parentForm, TextEditorControl control)
		: base(parentForm, control)
	{
		SetStyle(ControlStyles.UserPaint, value: true);
		SetStyle(ControlStyles.OptimizedDoubleBuffer, value: true);
	}

	public void ShowInsightWindow()
	{
		if (!Visible)
		{
			if (insightDataProviderStack.Count > 0)
			{
				ShowCompletionWindow();
			}
		}
		else
		{
			Refresh();
		}
	}

	protected override bool ProcessTextAreaKey(Keys keyData)
	{
		if (!Visible)
		{
			return false;
		}
		switch (keyData)
		{
		case Keys.Down:
			if (DataProvider != null && DataProvider.InsightDataCount > 0)
			{
				CurrentData = (CurrentData + 1) % DataProvider.InsightDataCount;
				Refresh();
			}
			return true;
		case Keys.Up:
			if (DataProvider != null && DataProvider.InsightDataCount > 0)
			{
				CurrentData = (CurrentData + DataProvider.InsightDataCount - 1) % DataProvider.InsightDataCount;
				Refresh();
			}
			return true;
		default:
			return base.ProcessTextAreaKey(keyData);
		}
	}

	protected override void CaretOffsetChanged(object sender, EventArgs e)
	{
		TextLocation position = control.ActiveTextAreaControl.Caret.Position;
		_ = position.Y;
		_ = control.ActiveTextAreaControl.TextArea.TextView.FontHeight;
		_ = control.ActiveTextAreaControl.TextArea.VirtualTop.Y;
		_ = control.ActiveTextAreaControl.TextArea.TextView.DrawingPosition.Y;
		int drawingXPos = control.ActiveTextAreaControl.TextArea.TextView.GetDrawingXPos(position.Y, position.X);
		int num = (control.ActiveTextAreaControl.Document.GetVisibleLine(position.Y) + 1) * control.ActiveTextAreaControl.TextArea.TextView.FontHeight - control.ActiveTextAreaControl.TextArea.VirtualTop.Y;
		int num2 = (control.TextEditorProperties.ShowHorizontalRuler ? control.ActiveTextAreaControl.TextArea.TextView.FontHeight : 0);
		Point location = control.ActiveTextAreaControl.PointToScreen(new Point(drawingXPos, num + num2));
		if (location.Y != Location.Y)
		{
			Location = location;
		}
		while (DataProvider != null && DataProvider.CaretOffsetChanged())
		{
			CloseCurrentDataProvider();
		}
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		control.ActiveTextAreaControl.TextArea.Focus();
		if (TipPainterTools.DrawingRectangle1.Contains(e.X, e.Y))
		{
			CurrentData = (CurrentData + DataProvider.InsightDataCount - 1) % DataProvider.InsightDataCount;
			Refresh();
		}
		if (TipPainterTools.DrawingRectangle2.Contains(e.X, e.Y))
		{
			CurrentData = (CurrentData + 1) % DataProvider.InsightDataCount;
			Refresh();
		}
	}

	public void HandleMouseWheel(MouseEventArgs e)
	{
		if (DataProvider != null && DataProvider.InsightDataCount > 0)
		{
			int num = mouseWheelHandler.GetScrollAmount(e);
			if (control.TextEditorProperties.MouseWheelScrollDown)
			{
				num = -num;
			}
			if (num > 0)
			{
				CurrentData = (CurrentData + 1) % DataProvider.InsightDataCount;
			}
			else if (num < 0)
			{
				CurrentData = (CurrentData + DataProvider.InsightDataCount - 1) % DataProvider.InsightDataCount;
			}
			Refresh();
		}
	}

	protected override void OnPaint(PaintEventArgs pe)
	{
		string countMessage = null;
		string description;
		if (DataProvider == null || DataProvider.InsightDataCount < 1)
		{
			description = "Unknown Method";
		}
		else
		{
			if (DataProvider.InsightDataCount > 1)
			{
				countMessage = control.GetRangeDescription(CurrentData + 1, DataProvider.InsightDataCount);
			}
			description = DataProvider.GetInsightData(CurrentData);
		}
		drawingSize = TipPainterTools.GetDrawingSizeHelpTipFromCombinedDescription(this, pe.Graphics, Font, countMessage, description);
		if (drawingSize != Size)
		{
			SetLocation();
		}
		else
		{
			TipPainterTools.DrawHelpTipFromCombinedDescription(this, pe.Graphics, Font, countMessage, description);
		}
	}

	protected override void OnPaintBackground(PaintEventArgs pe)
	{
		pe.Graphics.FillRectangle(SystemBrushes.Info, pe.ClipRectangle);
	}

	public void AddInsightDataProvider(IInsightDataProvider provider, string fileName)
	{
		provider.SetupDataProvider(fileName, control.ActiveTextAreaControl.TextArea);
		if (provider.InsightDataCount > 0)
		{
			insightDataProviderStack.Push(new InsightDataProviderStackElement(provider));
		}
	}

	private void CloseCurrentDataProvider()
	{
		insightDataProviderStack.Pop();
		if (insightDataProviderStack.Count == 0)
		{
			Close();
		}
		else
		{
			Refresh();
		}
	}
}
