using System;
using System.Drawing;
using System.Windows.Forms;

namespace ICSharpCode.TextEditor.Gui.CompletionWindow;

public abstract class AbstractCompletionWindow : Form
{
	protected TextEditorControl control;

	protected Size drawingSize;

	private Rectangle workingScreen;

	private Form parentForm;

	private static int shadowStatus;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams result = base.CreateParams;
			AddShadowToWindow(result);
			return result;
		}
	}

	protected override bool ShowWithoutActivation => true;

	protected AbstractCompletionWindow(Form parentForm, TextEditorControl control)
	{
		workingScreen = Screen.GetWorkingArea(parentForm);
		this.parentForm = parentForm;
		this.control = control;
		SetLocation();
		StartPosition = FormStartPosition.Manual;
		FormBorderStyle = FormBorderStyle.None;
		ShowInTaskbar = false;
		MinimumSize = new Size(1, 1);
		Size = new Size(1, 1);
	}

	protected virtual void SetLocation()
	{
		TextArea textArea = control.ActiveTextAreaControl.TextArea;
		TextLocation position = textArea.Caret.Position;
		int drawingXPos = textArea.TextView.GetDrawingXPos(position.Y, position.X);
		int num = (textArea.TextEditorProperties.ShowHorizontalRuler ? textArea.TextView.FontHeight : 0);
		Point p = new Point(textArea.TextView.DrawingPosition.X + drawingXPos, textArea.TextView.DrawingPosition.Y + textArea.Document.GetVisibleLine(position.Y) * textArea.TextView.FontHeight - textArea.TextView.TextArea.VirtualTop.Y + textArea.TextView.FontHeight + num);
		Point location = control.ActiveTextAreaControl.PointToScreen(p);
		Rectangle rectangle = new Rectangle(location, drawingSize);
		if (!workingScreen.Contains(rectangle))
		{
			if (rectangle.Right > workingScreen.Right)
			{
				rectangle.X = workingScreen.Right - rectangle.Width;
			}
			if (rectangle.Left < workingScreen.Left)
			{
				rectangle.X = workingScreen.Left;
			}
			if (rectangle.Top < workingScreen.Top)
			{
				rectangle.Y = workingScreen.Top;
			}
			if (rectangle.Bottom > workingScreen.Bottom)
			{
				rectangle.Y = rectangle.Y - rectangle.Height - control.ActiveTextAreaControl.TextArea.TextView.FontHeight;
				if (rectangle.Bottom > workingScreen.Bottom)
				{
					rectangle.Y = workingScreen.Bottom - rectangle.Height;
				}
			}
		}
		Bounds = rectangle;
	}

	public static void AddShadowToWindow(CreateParams createParams)
	{
		if (shadowStatus == 0)
		{
			shadowStatus = -1;
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				Version version = Environment.OSVersion.Version;
				if (version.Major > 5 || (version.Major == 5 && version.Minor >= 1))
				{
					shadowStatus = 1;
				}
			}
		}
		if (shadowStatus == 1)
		{
			createParams.ClassStyle |= 131072;
		}
	}

	protected void ShowCompletionWindow()
	{
		Owner = parentForm;
		Enabled = true;
		Show();
		this.control.Focus();
		if (parentForm != null)
		{
			parentForm.LocationChanged += ParentFormLocationChanged;
		}
		this.control.ActiveTextAreaControl.VScrollBar.ValueChanged += ParentFormLocationChanged;
		this.control.ActiveTextAreaControl.HScrollBar.ValueChanged += ParentFormLocationChanged;
		this.control.ActiveTextAreaControl.TextArea.DoProcessDialogKey += ProcessTextAreaKey;
		this.control.ActiveTextAreaControl.Caret.PositionChanged += CaretOffsetChanged;
		this.control.ActiveTextAreaControl.TextArea.LostFocus += TextEditorLostFocus;
		this.control.Resize += ParentFormLocationChanged;
		foreach (Control control in Controls)
		{
			control.MouseMove += ControlMouseMove;
		}
	}

	private void ParentFormLocationChanged(object sender, EventArgs e)
	{
		SetLocation();
	}

	public virtual bool ProcessKeyEvent(char ch)
	{
		return false;
	}

	protected virtual bool ProcessTextAreaKey(Keys keyData)
	{
		if (!Visible)
		{
			return false;
		}
		if (keyData == Keys.Escape)
		{
			Close();
			return true;
		}
		return false;
	}

	protected virtual void CaretOffsetChanged(object sender, EventArgs e)
	{
	}

	protected void TextEditorLostFocus(object sender, EventArgs e)
	{
		if (!control.ActiveTextAreaControl.TextArea.Focused && !ContainsFocus)
		{
			Close();
		}
	}

	protected override void OnClosed(EventArgs e)
	{
		base.OnClosed(e);
		parentForm.LocationChanged -= ParentFormLocationChanged;
		foreach (Control control in Controls)
		{
			control.MouseMove -= ControlMouseMove;
		}
		if (this.control.ActiveTextAreaControl.VScrollBar != null)
		{
			this.control.ActiveTextAreaControl.VScrollBar.ValueChanged -= ParentFormLocationChanged;
		}
		if (this.control.ActiveTextAreaControl.HScrollBar != null)
		{
			this.control.ActiveTextAreaControl.HScrollBar.ValueChanged -= ParentFormLocationChanged;
		}
		this.control.ActiveTextAreaControl.TextArea.LostFocus -= TextEditorLostFocus;
		this.control.ActiveTextAreaControl.Caret.PositionChanged -= CaretOffsetChanged;
		this.control.ActiveTextAreaControl.TextArea.DoProcessDialogKey -= ProcessTextAreaKey;
		this.control.Resize -= ParentFormLocationChanged;
		Dispose();
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		ControlMouseMove(this, e);
	}

	protected void ControlMouseMove(object sender, MouseEventArgs e)
	{
		control.ActiveTextAreaControl.TextArea.ShowHiddenCursor(forceShow: false);
	}
}
