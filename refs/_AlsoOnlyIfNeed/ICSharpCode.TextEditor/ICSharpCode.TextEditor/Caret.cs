using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor;

public class Caret : IDisposable
{
	private abstract class CaretImplementation : IDisposable
	{
		public bool RequireRedrawOnPositionChange;

		public abstract bool Create(int width, int height);

		public abstract void Hide();

		public abstract void Show();

		public abstract bool SetPosition(int x, int y);

		public abstract void PaintCaret(Graphics g);

		public abstract void Destroy();

		public virtual void Dispose()
		{
			Destroy();
		}
	}

	private class ManagedCaret : CaretImplementation
	{
		private Timer timer = new Timer
		{
			Interval = 300
		};

		private bool visible;

		private bool blink = true;

		private int x;

		private int y;

		private int width;

		private int height;

		private TextArea textArea;

		private Caret parentCaret;

		public ManagedCaret(Caret caret)
		{
			RequireRedrawOnPositionChange = true;
			textArea = caret.textArea;
			parentCaret = caret;
			timer.Tick += CaretTimerTick;
		}

		private void CaretTimerTick(object sender, EventArgs e)
		{
			blink = !blink;
			if (visible)
			{
				textArea.UpdateLine(parentCaret.Line);
			}
		}

		public override bool Create(int width, int height)
		{
			visible = true;
			this.width = width - 2;
			this.height = height;
			timer.Enabled = true;
			return true;
		}

		public override void Hide()
		{
			visible = false;
		}

		public override void Show()
		{
			visible = true;
		}

		public override bool SetPosition(int x, int y)
		{
			this.x = x - 1;
			this.y = y;
			return true;
		}

		public override void PaintCaret(Graphics g)
		{
			if (visible && blink)
			{
				g.DrawRectangle(Pens.Gray, x, y, width, height);
			}
		}

		public override void Destroy()
		{
			visible = false;
			timer.Enabled = false;
		}

		public override void Dispose()
		{
			base.Dispose();
			timer.Dispose();
		}
	}

	private class Win32Caret : CaretImplementation
	{
		private TextArea textArea;

		[DllImport("User32.dll")]
		private static extern bool CreateCaret(IntPtr hWnd, int hBitmap, int nWidth, int nHeight);

		[DllImport("User32.dll")]
		private static extern bool SetCaretPos(int x, int y);

		[DllImport("User32.dll")]
		private static extern bool DestroyCaret();

		[DllImport("User32.dll")]
		private static extern bool ShowCaret(IntPtr hWnd);

		[DllImport("User32.dll")]
		private static extern bool HideCaret(IntPtr hWnd);

		public Win32Caret(Caret caret)
		{
			textArea = caret.textArea;
		}

		public override bool Create(int width, int height)
		{
			return CreateCaret(textArea.Handle, 0, width, height);
		}

		public override void Hide()
		{
			HideCaret(textArea.Handle);
		}

		public override void Show()
		{
			ShowCaret(textArea.Handle);
		}

		public override bool SetPosition(int x, int y)
		{
			return SetCaretPos(x, y);
		}

		public override void PaintCaret(Graphics g)
		{
		}

		public override void Destroy()
		{
			DestroyCaret();
		}
	}

	private int line;

	private int column;

	private int desiredXPos;

	private CaretMode caretMode;

	private static bool caretCreated;

	private bool hidden = true;

	private TextArea textArea;

	private Point currentPos = new Point(-1, -1);

	private Ime ime;

	private CaretImplementation caretImplementation;

	private int oldLine = -1;

	private bool outstandingUpdate;

	private bool firePositionChangedAfterUpdateEnd;

	public int DesiredColumn
	{
		get
		{
			return desiredXPos;
		}
		set
		{
			desiredXPos = value;
		}
	}

	public CaretMode CaretMode
	{
		get
		{
			return caretMode;
		}
		set
		{
			caretMode = value;
			OnCaretModeChanged(EventArgs.Empty);
		}
	}

	public int Line
	{
		get
		{
			return line;
		}
		set
		{
			line = value;
			ValidateCaretPos();
			UpdateCaretPosition();
			OnPositionChanged(EventArgs.Empty);
		}
	}

	public int Column
	{
		get
		{
			return column;
		}
		set
		{
			column = value;
			ValidateCaretPos();
			UpdateCaretPosition();
			OnPositionChanged(EventArgs.Empty);
		}
	}

	public TextLocation Position
	{
		get
		{
			return new TextLocation(column, line);
		}
		set
		{
			line = value.Y;
			column = value.X;
			ValidateCaretPos();
			UpdateCaretPosition();
			OnPositionChanged(EventArgs.Empty);
		}
	}

	public int Offset => textArea.Document.PositionToOffset(Position);

	public Point ScreenPosition
	{
		get
		{
			int drawingXPos = textArea.TextView.GetDrawingXPos(line, column);
			return new Point(textArea.TextView.DrawingPosition.X + drawingXPos, textArea.TextView.DrawingPosition.Y + textArea.Document.GetVisibleLine(line) * textArea.TextView.FontHeight - textArea.TextView.TextArea.VirtualTop.Y);
		}
	}

	public event EventHandler PositionChanged;

	public event EventHandler CaretModeChanged;

	public Caret(TextArea textArea)
	{
		this.textArea = textArea;
		textArea.GotFocus += GotFocus;
		textArea.LostFocus += LostFocus;
		if (Environment.OSVersion.Platform == PlatformID.Unix)
		{
			caretImplementation = new ManagedCaret(this);
		}
		else
		{
			caretImplementation = new Win32Caret(this);
		}
	}

	public void Dispose()
	{
		textArea.GotFocus -= GotFocus;
		textArea.LostFocus -= LostFocus;
		textArea = null;
		caretImplementation.Dispose();
	}

	public TextLocation ValidatePosition(TextLocation pos)
	{
		int lineNumber = Math.Max(0, Math.Min(textArea.Document.TotalNumberOfLines - 1, pos.Y));
		int num = Math.Max(0, pos.X);
		if (num == int.MaxValue || !textArea.TextEditorProperties.AllowCaretBeyondEOL)
		{
			LineSegment lineSegment = textArea.Document.GetLineSegment(lineNumber);
			num = Math.Min(num, lineSegment.Length);
		}
		return new TextLocation(num, lineNumber);
	}

	public void ValidateCaretPos()
	{
		line = Math.Max(0, Math.Min(textArea.Document.TotalNumberOfLines - 1, line));
		column = Math.Max(0, column);
		if (column == int.MaxValue || !textArea.TextEditorProperties.AllowCaretBeyondEOL)
		{
			LineSegment lineSegment = textArea.Document.GetLineSegment(line);
			column = Math.Min(column, lineSegment.Length);
		}
	}

	private void CreateCaret()
	{
		while (!caretCreated)
		{
			switch (caretMode)
			{
			case CaretMode.InsertMode:
				caretCreated = caretImplementation.Create(2, textArea.TextView.FontHeight);
				break;
			case CaretMode.OverwriteMode:
				caretCreated = caretImplementation.Create(textArea.TextView.SpaceWidth, textArea.TextView.FontHeight);
				break;
			}
		}
		if (currentPos.X < 0)
		{
			ValidateCaretPos();
			currentPos = ScreenPosition;
		}
		caretImplementation.SetPosition(currentPos.X, currentPos.Y);
		caretImplementation.Show();
	}

	public void RecreateCaret()
	{
		DisposeCaret();
		if (!hidden)
		{
			CreateCaret();
		}
	}

	private void DisposeCaret()
	{
		if (caretCreated)
		{
			caretCreated = false;
			caretImplementation.Hide();
			caretImplementation.Destroy();
		}
	}

	private void GotFocus(object sender, EventArgs e)
	{
		hidden = false;
		if (!textArea.MotherTextEditorControl.IsInUpdate)
		{
			CreateCaret();
			UpdateCaretPosition();
		}
	}

	private void LostFocus(object sender, EventArgs e)
	{
		hidden = true;
		DisposeCaret();
	}

	internal void OnEndUpdate()
	{
		if (outstandingUpdate)
		{
			UpdateCaretPosition();
		}
	}

	public void UpdateCaretPosition()
	{
		if (caretImplementation.RequireRedrawOnPositionChange)
		{
			textArea.UpdateLine(oldLine);
			if (line != oldLine)
			{
				textArea.UpdateLine(line);
			}
		}
		else if (textArea.MotherTextAreaControl.TextEditorProperties.LineViewerStyle == LineViewerStyle.FullRow && oldLine != line)
		{
			textArea.UpdateLine(oldLine);
			textArea.UpdateLine(line);
		}
		oldLine = line;
		if (hidden || textArea.MotherTextEditorControl.IsInUpdate)
		{
			outstandingUpdate = true;
			return;
		}
		outstandingUpdate = false;
		ValidateCaretPos();
		int logicalLine = line;
		int drawingXPos = textArea.TextView.GetDrawingXPos(logicalLine, column);
		Point screenPosition = ScreenPosition;
		if (drawingXPos >= 0)
		{
			CreateCaret();
			if (!caretImplementation.SetPosition(screenPosition.X, screenPosition.Y))
			{
				caretImplementation.Destroy();
				caretCreated = false;
				UpdateCaretPosition();
			}
		}
		else
		{
			caretImplementation.Destroy();
		}
		if (ime == null)
		{
			ime = new Ime(textArea.Handle, textArea.Document.TextEditorProperties.Font);
		}
		else
		{
			ime.HWnd = textArea.Handle;
			ime.Font = textArea.Document.TextEditorProperties.Font;
		}
		ime.SetIMEWindowLocation(screenPosition.X, screenPosition.Y);
		currentPos = screenPosition;
	}

	[Conditional("DEBUG")]
	private static void Log(string text)
	{
	}

	internal void PaintCaret(Graphics g)
	{
		caretImplementation.PaintCaret(g);
	}

	private void FirePositionChangedAfterUpdateEnd(object sender, EventArgs e)
	{
		OnPositionChanged(EventArgs.Empty);
	}

	protected virtual void OnPositionChanged(EventArgs e)
	{
		if (textArea.MotherTextEditorControl.IsInUpdate)
		{
			if (!firePositionChangedAfterUpdateEnd)
			{
				firePositionChangedAfterUpdateEnd = true;
				textArea.Document.UpdateCommited += FirePositionChangedAfterUpdateEnd;
			}
			return;
		}
		if (firePositionChangedAfterUpdateEnd)
		{
			textArea.Document.UpdateCommited -= FirePositionChangedAfterUpdateEnd;
			firePositionChangedAfterUpdateEnd = false;
		}
		List<FoldMarker> foldingsFromPosition = textArea.Document.FoldingManager.GetFoldingsFromPosition(line, column);
		bool flag = false;
		foreach (FoldMarker item in foldingsFromPosition)
		{
			flag |= item.IsFolded;
			item.IsFolded = false;
		}
		if (flag)
		{
			textArea.Document.FoldingManager.NotifyFoldingsChanged(EventArgs.Empty);
		}
		if (PositionChanged != null)
		{
			PositionChanged(this, e);
		}
		textArea.ScrollToCaret();
	}

	protected virtual void OnCaretModeChanged(EventArgs e)
	{
		if (CaretModeChanged != null)
		{
			CaretModeChanged(this, e);
		}
		caretImplementation.Hide();
		caretImplementation.Destroy();
		caretCreated = false;
		CreateCaret();
		caretImplementation.Show();
	}
}
