using System;
using System.Drawing;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor;

public abstract class AbstractMargin
{
	private Cursor cursor = Cursors.Default;

	[CLSCompliant(false)]
	protected Rectangle drawingPosition = new Rectangle(0, 0, 0, 0);

	[CLSCompliant(false)]
	protected TextArea textArea;

	public Rectangle DrawingPosition
	{
		get
		{
			return drawingPosition;
		}
		set
		{
			drawingPosition = value;
		}
	}

	public TextArea TextArea => textArea;

	public IDocument Document => textArea.Document;

	public ITextEditorProperties TextEditorProperties => textArea.Document.TextEditorProperties;

	public virtual Cursor Cursor
	{
		get
		{
			return cursor;
		}
		set
		{
			cursor = value;
		}
	}

	public virtual Size Size => new Size(-1, -1);

	public virtual bool IsVisible => true;

	public event MarginPaintEventHandler Painted;

	public event MarginMouseEventHandler MouseDown;

	public event MarginMouseEventHandler MouseMove;

	public event EventHandler MouseLeave;

	protected AbstractMargin(TextArea textArea)
	{
		this.textArea = textArea;
	}

	public virtual void HandleMouseDown(Point mousepos, MouseButtons mouseButtons)
	{
		if (MouseDown != null)
		{
			MouseDown(this, mousepos, mouseButtons);
		}
	}

	public virtual void HandleMouseMove(Point mousepos, MouseButtons mouseButtons)
	{
		if (MouseMove != null)
		{
			MouseMove(this, mousepos, mouseButtons);
		}
	}

	public virtual void HandleMouseLeave(EventArgs e)
	{
		if (MouseLeave != null)
		{
			MouseLeave(this, e);
		}
	}

	public virtual void Paint(Graphics g, Rectangle rect)
	{
		if (Painted != null)
		{
			Painted(this, g, rect);
		}
	}
}
