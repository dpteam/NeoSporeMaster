using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Actions;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Gui.CompletionWindow;

namespace ICSharpCode.TextEditor;

[ToolboxItem(false)]
public class TextArea : Control
{
	private bool hiddenMouseCursor;

	private Point mouseCursorHidePosition;

	private Point virtualTop = new Point(0, 0);

	private TextAreaControl motherTextAreaControl;

	private TextEditorControl motherTextEditorControl;

	private List<BracketHighlightingSheme> bracketshemes = new List<BracketHighlightingSheme>();

	private TextAreaClipboardHandler textAreaClipboardHandler;

	private bool autoClearSelection;

	private List<AbstractMargin> leftMargins = new List<AbstractMargin>();

	private TextView textView;

	private GutterMargin gutterMargin;

	private FoldMargin foldMargin;

	private IconBarMargin iconBarMargin;

	private SelectionManager selectionManager;

	private Caret caret;

	internal Point mousepos = new Point(0, 0);

	private bool disposed;

	private AbstractMargin lastMouseInMargin;

	private static DeclarationViewWindow toolTip;

	private static string oldToolTip;

	private bool toolTipActive;

	private Rectangle toolTipRectangle;

	private AbstractMargin updateMargin;

	[Browsable(false)]
	public IList<AbstractMargin> LeftMargins => leftMargins.AsReadOnly();

	public TextEditorControl MotherTextEditorControl => motherTextEditorControl;

	public TextAreaControl MotherTextAreaControl => motherTextAreaControl;

	public SelectionManager SelectionManager => selectionManager;

	public Caret Caret => caret;

	public TextView TextView => textView;

	public GutterMargin GutterMargin => gutterMargin;

	public FoldMargin FoldMargin => foldMargin;

	public IconBarMargin IconBarMargin => iconBarMargin;

	public Encoding Encoding => motherTextEditorControl.Encoding;

	public int MaxVScrollValue => (Document.GetVisibleLine(Document.TotalNumberOfLines - 1) + 1 + TextView.VisibleLineCount * 2 / 3) * TextView.FontHeight;

	public Point VirtualTop
	{
		get
		{
			return virtualTop;
		}
		set
		{
			Point point = new Point(value.X, Math.Min(MaxVScrollValue, Math.Max(0, value.Y)));
			if (virtualTop != point)
			{
				virtualTop = point;
				motherTextAreaControl.VScrollBar.Value = virtualTop.Y;
				Invalidate();
			}
			caret.UpdateCaretPosition();
		}
	}

	public bool AutoClearSelection
	{
		get
		{
			return autoClearSelection;
		}
		set
		{
			autoClearSelection = value;
		}
	}

	[Browsable(false)]
	public IDocument Document => motherTextEditorControl.Document;

	public TextAreaClipboardHandler ClipboardHandler => textAreaClipboardHandler;

	public ITextEditorProperties TextEditorProperties => motherTextEditorControl.TextEditorProperties;

	public bool EnableCutOrPaste
	{
		get
		{
			if (motherTextAreaControl == null)
			{
				return false;
			}
			if (SelectionManager.HasSomethingSelected)
			{
				return !SelectionManager.SelectionIsReadonly;
			}
			return !IsReadOnly(Caret.Offset);
		}
	}

	private int FirstPhysicalLine => VirtualTop.Y / textView.FontHeight;

	public event ToolTipRequestEventHandler ToolTipRequest;

	public event KeyEventHandler KeyEventHandler;

	public event DialogKeyProcessor DoProcessDialogKey;

	public void InsertLeftMargin(int index, AbstractMargin margin)
	{
		leftMargins.Insert(index, margin);
		Refresh();
	}

	public TextArea(TextEditorControl motherTextEditorControl, TextAreaControl motherTextAreaControl)
	{
		this.motherTextAreaControl = motherTextAreaControl;
		this.motherTextEditorControl = motherTextEditorControl;
		caret = new Caret(this);
		selectionManager = new SelectionManager(Document, this);
		textAreaClipboardHandler = new TextAreaClipboardHandler(this);
		ResizeRedraw = true;
		SetStyle(ControlStyles.OptimizedDoubleBuffer, value: true);
		SetStyle(ControlStyles.Opaque, value: false);
		SetStyle(ControlStyles.ResizeRedraw, value: true);
		SetStyle(ControlStyles.Selectable, value: true);
		textView = new TextView(this);
		gutterMargin = new GutterMargin(this);
		foldMargin = new FoldMargin(this);
		iconBarMargin = new IconBarMargin(this);
		leftMargins.AddRange(new AbstractMargin[3] { iconBarMargin, gutterMargin, foldMargin });
		OptionsChanged();
		new TextAreaMouseHandler(this).Attach();
		new TextAreaDragDropHandler().Attach(this);
		bracketshemes.Add(new BracketHighlightingSheme('{', '}'));
		bracketshemes.Add(new BracketHighlightingSheme('(', ')'));
		bracketshemes.Add(new BracketHighlightingSheme('[', ']'));
		caret.PositionChanged += SearchMatchingBracket;
		Document.TextContentChanged += TextContentChanged;
		Document.FoldingManager.FoldingsChanged += DocumentFoldingsChanged;
	}

	public void UpdateMatchingBracket()
	{
		SearchMatchingBracket(null, null);
	}

	private void TextContentChanged(object sender, EventArgs e)
	{
		Caret.Position = new TextLocation(0, 0);
		SelectionManager.SelectionCollection.Clear();
	}

	private void SearchMatchingBracket(object sender, EventArgs e)
	{
		if (!TextEditorProperties.ShowMatchingBracket)
		{
			textView.Highlight = null;
			return;
		}
		int num = -1;
		int num2 = -1;
		if (textView.Highlight != null && textView.Highlight.OpenBrace.Y >= 0 && textView.Highlight.OpenBrace.Y < Document.TotalNumberOfLines)
		{
			num = textView.Highlight.OpenBrace.Y;
		}
		if (textView.Highlight != null && textView.Highlight.CloseBrace.Y >= 0 && textView.Highlight.CloseBrace.Y < Document.TotalNumberOfLines)
		{
			num2 = textView.Highlight.CloseBrace.Y;
		}
		textView.Highlight = FindMatchingBracketHighlight();
		if (num >= 0)
		{
			UpdateLine(num);
		}
		if (num2 >= 0 && num2 != num)
		{
			UpdateLine(num2);
		}
		if (textView.Highlight != null)
		{
			int num3 = textView.Highlight.OpenBrace.Y;
			int num4 = textView.Highlight.CloseBrace.Y;
			if (num3 != num && num3 != num2)
			{
				UpdateLine(num3);
			}
			if (num4 != num && num4 != num2 && num4 != num3)
			{
				UpdateLine(num4);
			}
		}
	}

	public Highlight FindMatchingBracketHighlight()
	{
		if (Caret.Offset == 0)
		{
			return null;
		}
		foreach (BracketHighlightingSheme bracketsheme in bracketshemes)
		{
			Highlight highlight = bracketsheme.GetHighlight(Document, Caret.Offset - 1);
			if (highlight != null)
			{
				return highlight;
			}
		}
		return null;
	}

	public void SetDesiredColumn()
	{
		Caret.DesiredColumn = TextView.GetDrawingXPos(Caret.Line, Caret.Column) + VirtualTop.X;
	}

	public void SetCaretToDesiredColumn()
	{
		Caret.Position = textView.GetLogicalColumn(Caret.Line, Caret.DesiredColumn + VirtualTop.X, out var _);
	}

	public void OptionsChanged()
	{
		UpdateMatchingBracket();
		textView.OptionsChanged();
		caret.RecreateCaret();
		caret.UpdateCaretPosition();
		Refresh();
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		Cursor = Cursors.Default;
		if (lastMouseInMargin != null)
		{
			lastMouseInMargin.HandleMouseLeave(EventArgs.Empty);
			lastMouseInMargin = null;
		}
		CloseToolTip();
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		mousepos = new Point(e.X, e.Y);
		base.OnMouseDown(e);
		CloseToolTip();
		foreach (AbstractMargin leftMargin in leftMargins)
		{
			if (leftMargin.DrawingPosition.Contains(e.X, e.Y))
			{
				leftMargin.HandleMouseDown(new Point(e.X, e.Y), e.Button);
			}
		}
	}

	internal void ShowHiddenCursor(bool forceShow)
	{
		if (hiddenMouseCursor && (mouseCursorHidePosition != Cursor.Position || forceShow))
		{
			Cursor.Show();
			hiddenMouseCursor = false;
		}
	}

	private void SetToolTip(string text, int lineNumber)
	{
		if (toolTip == null || toolTip.IsDisposed)
		{
			toolTip = new DeclarationViewWindow(FindForm());
		}
		if (oldToolTip == text)
		{
			return;
		}
		if (text == null)
		{
			toolTip.Hide();
		}
		else
		{
			Point mousePosition = Control.MousePosition;
			Point point = PointToClient(mousePosition);
			if (lineNumber >= 0)
			{
				lineNumber = Document.GetVisibleLine(lineNumber);
				mousePosition.Y = mousePosition.Y - point.Y + lineNumber * TextView.FontHeight - virtualTop.Y;
			}
			mousePosition.Offset(3, 3);
			toolTip.Location = mousePosition;
			toolTip.Description = text;
			toolTip.HideOnClick = true;
			toolTip.Show();
		}
		oldToolTip = text;
	}

	protected virtual void OnToolTipRequest(ToolTipRequestEventArgs e)
	{
		if (ToolTipRequest != null)
		{
			ToolTipRequest(this, e);
		}
	}

	private void CloseToolTip()
	{
		if (toolTipActive)
		{
			toolTipActive = false;
			SetToolTip(null, -1);
		}
		ResetMouseEventArgs();
	}

	protected override void OnMouseHover(EventArgs e)
	{
		base.OnMouseHover(e);
		if (Control.MouseButtons == MouseButtons.None)
		{
			RequestToolTip(PointToClient(Control.MousePosition));
		}
		else
		{
			CloseToolTip();
		}
	}

	protected void RequestToolTip(Point mousePos)
	{
		if (toolTipRectangle.Contains(mousePos))
		{
			if (!toolTipActive)
			{
				ResetMouseEventArgs();
			}
			return;
		}
		toolTipRectangle = new Rectangle(mousePos.X - 4, mousePos.Y - 4, 8, 8);
		TextLocation logicalPosition = textView.GetLogicalPosition(mousePos.X - textView.DrawingPosition.Left, mousePos.Y - textView.DrawingPosition.Top);
		bool flag = textView.DrawingPosition.Contains(mousePos) && logicalPosition.Y >= 0 && logicalPosition.Y < Document.TotalNumberOfLines;
		ToolTipRequestEventArgs e = new ToolTipRequestEventArgs(mousePos, logicalPosition, flag);
		OnToolTipRequest(e);
		if (e.ToolTipShown)
		{
			toolTipActive = true;
			SetToolTip(e.toolTipText, flag ? (logicalPosition.Y + 1) : (-1));
		}
		else
		{
			CloseToolTip();
		}
	}

	internal void RaiseMouseMove(MouseEventArgs e)
	{
		OnMouseMove(e);
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (!toolTipRectangle.Contains(e.Location))
		{
			toolTipRectangle = Rectangle.Empty;
			if (toolTipActive)
			{
				RequestToolTip(e.Location);
			}
		}
		foreach (AbstractMargin leftMargin in leftMargins)
		{
			if (!leftMargin.DrawingPosition.Contains(e.X, e.Y))
			{
				continue;
			}
			Cursor = leftMargin.Cursor;
			leftMargin.HandleMouseMove(new Point(e.X, e.Y), e.Button);
			if (lastMouseInMargin != leftMargin)
			{
				if (lastMouseInMargin != null)
				{
					lastMouseInMargin.HandleMouseLeave(EventArgs.Empty);
				}
				lastMouseInMargin = leftMargin;
			}
			return;
		}
		if (lastMouseInMargin != null)
		{
			lastMouseInMargin.HandleMouseLeave(EventArgs.Empty);
			lastMouseInMargin = null;
		}
		if (textView.DrawingPosition.Contains(e.X, e.Y))
		{
			TextLocation logicalPosition = TextView.GetLogicalPosition(e.X - TextView.DrawingPosition.X, e.Y - TextView.DrawingPosition.Y);
			if (SelectionManager.IsSelected(Document.PositionToOffset(logicalPosition)) && Control.MouseButtons == MouseButtons.None)
			{
				Cursor = Cursors.Default;
			}
			else
			{
				Cursor = textView.Cursor;
			}
		}
		else
		{
			Cursor = Cursors.Default;
		}
	}

	public void Refresh(AbstractMargin margin)
	{
		updateMargin = margin;
		Invalidate(updateMargin.DrawingPosition);
		Update();
		updateMargin = null;
	}

	protected override void OnPaintBackground(PaintEventArgs pevent)
	{
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		int num = 0;
		int num2 = 0;
		bool flag = false;
		Graphics graphics = e.Graphics;
		Rectangle clipRectangle = e.ClipRectangle;
		bool flag2 = clipRectangle.X == 0 && clipRectangle.Y == 0 && clipRectangle.Width == Width && clipRectangle.Height == Height;
		graphics.TextRenderingHint = TextEditorProperties.TextRenderingHint;
		if (updateMargin != null)
		{
			updateMargin.Paint(graphics, updateMargin.DrawingPosition);
		}
		if (clipRectangle.Width <= 0 || clipRectangle.Height <= 0)
		{
			return;
		}
		foreach (AbstractMargin leftMargin in leftMargins)
		{
			if (!leftMargin.IsVisible)
			{
				continue;
			}
			Rectangle rectangle = new Rectangle(num, num2, leftMargin.Size.Width, Height - num2);
			if (rectangle != leftMargin.DrawingPosition)
			{
				if (!flag2 && !clipRectangle.Contains(rectangle))
				{
					Invalidate();
				}
				flag = true;
				leftMargin.DrawingPosition = rectangle;
			}
			num += leftMargin.DrawingPosition.Width;
			if (clipRectangle.IntersectsWith(rectangle))
			{
				rectangle.Intersect(clipRectangle);
				if (!rectangle.IsEmpty)
				{
					leftMargin.Paint(graphics, rectangle);
				}
			}
		}
		Rectangle rectangle2 = new Rectangle(num, num2, Width - num, Height - num2);
		if (rectangle2 != textView.DrawingPosition)
		{
			flag = true;
			textView.DrawingPosition = rectangle2;
			BeginInvoke(new MethodInvoker(caret.UpdateCaretPosition));
		}
		if (clipRectangle.IntersectsWith(rectangle2))
		{
			rectangle2.Intersect(clipRectangle);
			if (!rectangle2.IsEmpty)
			{
				textView.Paint(graphics, rectangle2);
			}
		}
		if (flag)
		{
			motherTextAreaControl.AdjustScrollBars();
		}
		base.OnPaint(e);
	}

	private void DocumentFoldingsChanged(object sender, EventArgs e)
	{
		Caret.UpdateCaretPosition();
		Invalidate();
		motherTextAreaControl.AdjustScrollBars();
	}

	protected internal virtual bool HandleKeyPress(char ch)
	{
		if (KeyEventHandler != null)
		{
			return KeyEventHandler(ch);
		}
		return false;
	}

	protected override bool IsInputChar(char charCode)
	{
		return true;
	}

	internal bool IsReadOnly(int offset)
	{
		if (Document.ReadOnly)
		{
			return true;
		}
		if (TextEditorProperties.SupportReadOnlySegments)
		{
			return Document.MarkerStrategy.GetMarkers(offset).Exists((TextMarker m) => m.IsReadOnly);
		}
		return false;
	}

	internal bool IsReadOnly(int offset, int length)
	{
		if (Document.ReadOnly)
		{
			return true;
		}
		if (TextEditorProperties.SupportReadOnlySegments)
		{
			return Document.MarkerStrategy.GetMarkers(offset, length).Exists((TextMarker m) => m.IsReadOnly);
		}
		return false;
	}

	public void SimulateKeyPress(char ch)
	{
		if (SelectionManager.HasSomethingSelected)
		{
			if (SelectionManager.SelectionIsReadonly)
			{
				return;
			}
		}
		else if (IsReadOnly(Caret.Offset))
		{
			return;
		}
		if (ch < ' ')
		{
			return;
		}
		if (!hiddenMouseCursor && TextEditorProperties.HideMouseCursor && ClientRectangle.Contains(PointToClient(Cursor.Position)))
		{
			mouseCursorHidePosition = Cursor.Position;
			hiddenMouseCursor = true;
			Cursor.Hide();
		}
		CloseToolTip();
		BeginUpdate();
		Document.UndoStack.StartUndoGroup();
		try
		{
			if (!HandleKeyPress(ch))
			{
				switch (Caret.CaretMode)
				{
				case CaretMode.InsertMode:
					InsertChar(ch);
					break;
				case CaretMode.OverwriteMode:
					ReplaceChar(ch);
					break;
				}
			}
			int line = Caret.Line;
			Document.FormattingStrategy.FormatLine(this, line, Document.PositionToOffset(Caret.Position), ch);
			EndUpdate();
		}
		finally
		{
			Document.UndoStack.EndUndoGroup();
		}
	}

	protected override void OnKeyPress(KeyPressEventArgs e)
	{
		base.OnKeyPress(e);
		SimulateKeyPress(e.KeyChar);
		e.Handled = true;
	}

	public bool ExecuteDialogKey(Keys keyData)
	{
		if (DoProcessDialogKey != null && DoProcessDialogKey(keyData))
		{
			return true;
		}
		IEditAction editAction = motherTextEditorControl.GetEditAction(keyData);
		AutoClearSelection = true;
		if (editAction != null)
		{
			BeginUpdate();
			try
			{
				lock (Document)
				{
					editAction.Execute(this);
					if (SelectionManager.HasSomethingSelected && AutoClearSelection && Document.TextEditorProperties.DocumentSelectionMode == DocumentSelectionMode.Normal)
					{
						SelectionManager.ClearSelection();
					}
				}
			}
			finally
			{
				EndUpdate();
				Caret.UpdateCaretPosition();
			}
			return true;
		}
		return false;
	}

	protected override bool ProcessDialogKey(Keys keyData)
	{
		if (!ExecuteDialogKey(keyData))
		{
			return base.ProcessDialogKey(keyData);
		}
		return true;
	}

	public void ScrollToCaret()
	{
		motherTextAreaControl.ScrollToCaret();
	}

	public void ScrollTo(int line)
	{
		motherTextAreaControl.ScrollTo(line);
	}

	public void BeginUpdate()
	{
		motherTextEditorControl.BeginUpdate();
	}

	public void EndUpdate()
	{
		motherTextEditorControl.EndUpdate();
	}

	private string GenerateWhitespaceString(int length)
	{
		return new string(' ', length);
	}

	public void InsertChar(char ch)
	{
		bool isInUpdate = motherTextEditorControl.IsInUpdate;
		if (!isInUpdate)
		{
			BeginUpdate();
		}
		if (char.IsWhiteSpace(ch) && ch != '\t' && ch != '\n')
		{
			ch = ' ';
		}
		Document.UndoStack.StartUndoGroup();
		if (Document.TextEditorProperties.DocumentSelectionMode == DocumentSelectionMode.Normal && SelectionManager.SelectionCollection.Count > 0)
		{
			Caret.Position = SelectionManager.SelectionCollection[0].StartPosition;
			SelectionManager.RemoveSelectedText();
		}
		LineSegment lineSegment = Document.GetLineSegment(Caret.Line);
		int offset = Caret.Offset;
		int column = Caret.Column;
		if (lineSegment.Length < column && ch != '\n')
		{
			Document.Insert(offset, GenerateWhitespaceString(column - lineSegment.Length) + ch);
		}
		else
		{
			Document.Insert(offset, ch.ToString());
		}
		Document.UndoStack.EndUndoGroup();
		Caret.Column++;
		if (!isInUpdate)
		{
			EndUpdate();
			UpdateLineToEnd(Caret.Line, Caret.Column);
		}
	}

	public void InsertString(string str)
	{
		bool isInUpdate = motherTextEditorControl.IsInUpdate;
		if (!isInUpdate)
		{
			BeginUpdate();
		}
		try
		{
			Document.UndoStack.StartUndoGroup();
			if (Document.TextEditorProperties.DocumentSelectionMode == DocumentSelectionMode.Normal && SelectionManager.SelectionCollection.Count > 0)
			{
				Caret.Position = SelectionManager.SelectionCollection[0].StartPosition;
				SelectionManager.RemoveSelectedText();
			}
			int num = Document.PositionToOffset(Caret.Position);
			int line = Caret.Line;
			LineSegment lineSegment = Document.GetLineSegment(Caret.Line);
			if (lineSegment.Length < Caret.Column)
			{
				int num2 = Caret.Column - lineSegment.Length;
				Document.Insert(num, GenerateWhitespaceString(num2) + str);
				Caret.Position = Document.OffsetToPosition(num + str.Length + num2);
			}
			else
			{
				Document.Insert(num, str);
				Caret.Position = Document.OffsetToPosition(num + str.Length);
			}
			Document.UndoStack.EndUndoGroup();
			if (line != Caret.Line)
			{
				UpdateToEnd(line);
			}
			else
			{
				UpdateLineToEnd(Caret.Line, Caret.Column);
			}
		}
		finally
		{
			if (!isInUpdate)
			{
				EndUpdate();
			}
		}
	}

	public void ReplaceChar(char ch)
	{
		bool isInUpdate = motherTextEditorControl.IsInUpdate;
		if (!isInUpdate)
		{
			BeginUpdate();
		}
		if (Document.TextEditorProperties.DocumentSelectionMode == DocumentSelectionMode.Normal && SelectionManager.SelectionCollection.Count > 0)
		{
			Caret.Position = SelectionManager.SelectionCollection[0].StartPosition;
			SelectionManager.RemoveSelectedText();
		}
		int line = Caret.Line;
		LineSegment lineSegment = Document.GetLineSegment(line);
		int num = Document.PositionToOffset(Caret.Position);
		if (num < lineSegment.Offset + lineSegment.Length)
		{
			Document.Replace(num, 1, ch.ToString());
		}
		else
		{
			Document.Insert(num, ch.ToString());
		}
		if (!isInUpdate)
		{
			EndUpdate();
			UpdateLineToEnd(line, Caret.Column);
		}
		Caret.Column++;
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (!disposing || disposed)
		{
			return;
		}
		disposed = true;
		if (caret != null)
		{
			caret.PositionChanged -= SearchMatchingBracket;
			caret.Dispose();
		}
		if (selectionManager != null)
		{
			selectionManager.Dispose();
		}
		Document.TextContentChanged -= TextContentChanged;
		Document.FoldingManager.FoldingsChanged -= DocumentFoldingsChanged;
		motherTextAreaControl = null;
		motherTextEditorControl = null;
		foreach (AbstractMargin leftMargin in leftMargins)
		{
			if (leftMargin is IDisposable)
			{
				(leftMargin as IDisposable).Dispose();
			}
		}
		textView.Dispose();
	}

	internal void UpdateLine(int line)
	{
		UpdateLines(0, line, line);
	}

	internal void UpdateLines(int lineBegin, int lineEnd)
	{
		UpdateLines(0, lineBegin, lineEnd);
	}

	internal void UpdateToEnd(int lineBegin)
	{
		lineBegin = Document.GetVisibleLine(lineBegin);
		int num = Math.Max(0, lineBegin * textView.FontHeight);
		num = Math.Max(0, num - virtualTop.Y);
		Rectangle rc = new Rectangle(0, num, Width, Height - num);
		Invalidate(rc);
	}

	internal void UpdateLineToEnd(int lineNr, int xStart)
	{
		UpdateLines(xStart, lineNr, lineNr);
	}

	internal void UpdateLine(int line, int begin, int end)
	{
		UpdateLines(line, line);
	}

	internal void UpdateLines(int xPos, int lineBegin, int lineEnd)
	{
		InvalidateLines(xPos * TextView.WideSpaceWidth, lineBegin, lineEnd);
	}

	private void InvalidateLines(int xPos, int lineBegin, int lineEnd)
	{
		lineBegin = Math.Max(Document.GetVisibleLine(lineBegin), FirstPhysicalLine);
		lineEnd = Math.Min(Document.GetVisibleLine(lineEnd), FirstPhysicalLine + textView.VisibleLineCount);
		int num = Math.Max(0, lineBegin * textView.FontHeight);
		int num2 = Math.Min(textView.DrawingPosition.Height, (1 + lineEnd - lineBegin) * (textView.FontHeight + 1));
		Rectangle rc = new Rectangle(0, num - 1 - virtualTop.Y, Width, num2 + 3);
		Invalidate(rc);
	}
}
