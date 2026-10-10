using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Util;

namespace ICSharpCode.TextEditor;

[ToolboxItem(false)]
public class TextAreaControl : Panel
{
	private const int LineLengthCacheAdditionalSize = 100;

	private TextEditorControl motherTextEditorControl;

	private HRuler hRuler;

	private VScrollBar vScrollBar = new VScrollBar();

	private HScrollBar hScrollBar = new HScrollBar();

	private TextArea textArea;

	private bool doHandleMousewheel = true;

	private bool disposed;

	private bool adjustScrollBarsOnNextUpdate;

	private Point scrollToPosOnNextUpdate;

	private int[] lineLengthCache;

	private MouseWheelHandler mouseWheelHandler = new MouseWheelHandler();

	private int scrollMarginHeight = 3;

	public TextArea TextArea => textArea;

	public SelectionManager SelectionManager => textArea.SelectionManager;

	public Caret Caret => textArea.Caret;

	[Browsable(false)]
	public IDocument Document
	{
		get
		{
			if (motherTextEditorControl != null)
			{
				return motherTextEditorControl.Document;
			}
			return null;
		}
	}

	public ITextEditorProperties TextEditorProperties
	{
		get
		{
			if (motherTextEditorControl != null)
			{
				return motherTextEditorControl.TextEditorProperties;
			}
			return null;
		}
	}

	public VScrollBar VScrollBar => vScrollBar;

	public HScrollBar HScrollBar => hScrollBar;

	public bool DoHandleMousewheel
	{
		get
		{
			return doHandleMousewheel;
		}
		set
		{
			doHandleMousewheel = value;
		}
	}

	public event MouseEventHandler ShowContextMenu;

	public TextAreaControl(TextEditorControl motherTextEditorControl)
	{
		this.motherTextEditorControl = motherTextEditorControl;
		textArea = new TextArea(motherTextEditorControl, this);
		Controls.Add(textArea);
		vScrollBar.ValueChanged += VScrollBarValueChanged;
		Controls.Add(vScrollBar);
		hScrollBar.ValueChanged += HScrollBarValueChanged;
		Controls.Add(hScrollBar);
		ResizeRedraw = true;
		Document.TextContentChanged += DocumentTextContentChanged;
		Document.DocumentChanged += AdjustScrollBarsOnDocumentChange;
		Document.UpdateCommited += DocumentUpdateCommitted;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && !disposed)
		{
			disposed = true;
			Document.TextContentChanged -= DocumentTextContentChanged;
			Document.DocumentChanged -= AdjustScrollBarsOnDocumentChange;
			Document.UpdateCommited -= DocumentUpdateCommitted;
			motherTextEditorControl = null;
			if (vScrollBar != null)
			{
				vScrollBar.Dispose();
				vScrollBar = null;
			}
			if (hScrollBar != null)
			{
				hScrollBar.Dispose();
				hScrollBar = null;
			}
			if (hRuler != null)
			{
				hRuler.Dispose();
				hRuler = null;
			}
		}
		base.Dispose(disposing);
	}

	private void DocumentTextContentChanged(object sender, EventArgs e)
	{
		Caret.ValidateCaretPos();
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		ResizeTextArea();
	}

	public void ResizeTextArea()
	{
		int num = 0;
		int num2 = 0;
		if (hRuler != null)
		{
			hRuler.Bounds = new Rectangle(0, 0, Width - SystemInformation.HorizontalScrollBarArrowWidth, textArea.TextView.FontHeight);
			num = hRuler.Bounds.Bottom;
			num2 = hRuler.Bounds.Height;
		}
		textArea.Bounds = new Rectangle(0, num, Width - SystemInformation.HorizontalScrollBarArrowWidth, Height - SystemInformation.VerticalScrollBarArrowHeight - num2);
		SetScrollBarBounds();
	}

	public void SetScrollBarBounds()
	{
		vScrollBar.Bounds = new Rectangle(textArea.Bounds.Right, 0, SystemInformation.HorizontalScrollBarArrowWidth, Height - SystemInformation.VerticalScrollBarArrowHeight);
		hScrollBar.Bounds = new Rectangle(0, textArea.Bounds.Bottom, Width - SystemInformation.HorizontalScrollBarArrowWidth, SystemInformation.VerticalScrollBarArrowHeight);
	}

	private void AdjustScrollBarsOnDocumentChange(object sender, DocumentEventArgs e)
	{
		if (!motherTextEditorControl.IsInUpdate)
		{
			AdjustScrollBarsClearCache();
			AdjustScrollBars();
		}
		else
		{
			adjustScrollBarsOnNextUpdate = true;
		}
	}

	private void DocumentUpdateCommitted(object sender, EventArgs e)
	{
		if (!motherTextEditorControl.IsInUpdate)
		{
			Caret.ValidateCaretPos();
			if (!scrollToPosOnNextUpdate.IsEmpty)
			{
				ScrollTo(scrollToPosOnNextUpdate.Y, scrollToPosOnNextUpdate.X);
			}
			if (adjustScrollBarsOnNextUpdate)
			{
				AdjustScrollBarsClearCache();
				AdjustScrollBars();
			}
		}
	}

	private void AdjustScrollBarsClearCache()
	{
		if (lineLengthCache != null)
		{
			if (lineLengthCache.Length < Document.TotalNumberOfLines + 200)
			{
				lineLengthCache = null;
			}
			else
			{
				Array.Clear(lineLengthCache, 0, lineLengthCache.Length);
			}
		}
	}

	public void AdjustScrollBars()
	{
		adjustScrollBarsOnNextUpdate = false;
		vScrollBar.Minimum = 0;
		vScrollBar.Maximum = textArea.MaxVScrollValue;
		int num = 0;
		int firstVisibleLine = textArea.TextView.FirstVisibleLine;
		int num2 = Document.GetFirstLogicalLine(textArea.TextView.FirstPhysicalLine + textArea.TextView.VisibleLineCount);
		if (num2 >= Document.TotalNumberOfLines)
		{
			num2 = Document.TotalNumberOfLines - 1;
		}
		if (lineLengthCache == null || lineLengthCache.Length <= num2)
		{
			lineLengthCache = new int[num2 + 100];
		}
		for (int i = firstVisibleLine; i <= num2; i++)
		{
			LineSegment lineSegment = Document.GetLineSegment(i);
			if (Document.FoldingManager.IsLineVisible(i))
			{
				if (lineLengthCache[i] > 0)
				{
					num = Math.Max(num, lineLengthCache[i]);
					continue;
				}
				int visualColumnFast = textArea.TextView.GetVisualColumnFast(lineSegment, lineSegment.Length);
				lineLengthCache[i] = Math.Max(1, visualColumnFast);
				num = Math.Max(num, visualColumnFast);
			}
		}
		hScrollBar.Minimum = 0;
		hScrollBar.Maximum = Math.Max(num + 20, textArea.TextView.VisibleColumnCount - 1);
		vScrollBar.LargeChange = Math.Max(0, textArea.TextView.DrawingPosition.Height);
		vScrollBar.SmallChange = Math.Max(0, textArea.TextView.FontHeight);
		hScrollBar.LargeChange = Math.Max(0, textArea.TextView.VisibleColumnCount - 1);
		hScrollBar.SmallChange = Math.Max(0, textArea.TextView.SpaceWidth);
	}

	public void OptionsChanged()
	{
		textArea.OptionsChanged();
		if (textArea.TextEditorProperties.ShowHorizontalRuler)
		{
			if (hRuler == null)
			{
				hRuler = new HRuler(textArea);
				Controls.Add(hRuler);
				ResizeTextArea();
			}
			else
			{
				hRuler.Invalidate();
			}
		}
		else if (hRuler != null)
		{
			Controls.Remove(hRuler);
			hRuler.Dispose();
			hRuler = null;
			ResizeTextArea();
		}
		AdjustScrollBars();
	}

	private void VScrollBarValueChanged(object sender, EventArgs e)
	{
		textArea.VirtualTop = new Point(textArea.VirtualTop.X, vScrollBar.Value);
		textArea.Invalidate();
		AdjustScrollBars();
	}

	private void HScrollBarValueChanged(object sender, EventArgs e)
	{
		textArea.VirtualTop = new Point(hScrollBar.Value * textArea.TextView.WideSpaceWidth, textArea.VirtualTop.Y);
		textArea.Invalidate();
	}

	public void HandleMouseWheel(MouseEventArgs e)
	{
		int num = mouseWheelHandler.GetScrollAmount(e);
		if (num == 0)
		{
			return;
		}
		if ((Control.ModifierKeys & Keys.Control) != 0 && TextEditorProperties.MouseWheelTextZoom)
		{
			if (num > 0)
			{
				motherTextEditorControl.Font = new Font(motherTextEditorControl.Font.Name, motherTextEditorControl.Font.Size + 1f);
			}
			else
			{
				motherTextEditorControl.Font = new Font(motherTextEditorControl.Font.Name, Math.Max(6f, motherTextEditorControl.Font.Size - 1f));
			}
			return;
		}
		if (TextEditorProperties.MouseWheelScrollDown)
		{
			num = -num;
		}
		int val = vScrollBar.Value + vScrollBar.SmallChange * num;
		vScrollBar.Value = Math.Max(vScrollBar.Minimum, Math.Min(vScrollBar.Maximum - vScrollBar.LargeChange + 1, val));
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		base.OnMouseWheel(e);
		if (DoHandleMousewheel)
		{
			HandleMouseWheel(e);
		}
	}

	public void ScrollToCaret()
	{
		ScrollTo(textArea.Caret.Line, textArea.Caret.Column);
	}

	public void ScrollTo(int line, int column)
	{
		if (motherTextEditorControl.IsInUpdate)
		{
			scrollToPosOnNextUpdate = new Point(column, line);
			return;
		}
		scrollToPosOnNextUpdate = Point.Empty;
		ScrollTo(line);
		int num = hScrollBar.Value - hScrollBar.Minimum;
		int num2 = num + textArea.TextView.VisibleColumnCount;
		int visualColumn = textArea.TextView.GetVisualColumn(line, column);
		if (textArea.TextView.VisibleColumnCount < 0)
		{
			hScrollBar.Value = 0;
		}
		else if (visualColumn < num)
		{
			hScrollBar.Value = Math.Max(0, visualColumn - scrollMarginHeight);
		}
		else if (visualColumn > num2)
		{
			hScrollBar.Value = Math.Max(0, Math.Min(hScrollBar.Maximum, visualColumn - textArea.TextView.VisibleColumnCount + scrollMarginHeight));
		}
	}

	public void ScrollTo(int line)
	{
		line = Math.Max(0, Math.Min(Document.TotalNumberOfLines - 1, line));
		line = Document.GetVisibleLine(line);
		int num = textArea.TextView.FirstPhysicalLine;
		if (textArea.TextView.LineHeightRemainder > 0)
		{
			num++;
		}
		if (line - scrollMarginHeight + 3 < num)
		{
			vScrollBar.Value = Math.Max(0, Math.Min(vScrollBar.Maximum, (line - scrollMarginHeight + 3) * textArea.TextView.FontHeight));
			VScrollBarValueChanged(this, EventArgs.Empty);
			return;
		}
		int num2 = num + textArea.TextView.VisibleLineCount;
		if (line + scrollMarginHeight - 1 > num2)
		{
			if (textArea.TextView.VisibleLineCount == 1)
			{
				vScrollBar.Value = Math.Max(0, Math.Min(vScrollBar.Maximum, (line - scrollMarginHeight - 1) * textArea.TextView.FontHeight));
			}
			else
			{
				vScrollBar.Value = Math.Min(vScrollBar.Maximum, (line - textArea.TextView.VisibleLineCount + scrollMarginHeight - 1) * textArea.TextView.FontHeight);
			}
			VScrollBarValueChanged(this, EventArgs.Empty);
		}
	}

	public void CenterViewOn(int line, int treshold)
	{
		line = Math.Max(0, Math.Min(Document.TotalNumberOfLines - 1, line));
		line = Document.GetVisibleLine(line);
		line -= textArea.TextView.VisibleLineCount / 2;
		int num = textArea.TextView.FirstPhysicalLine;
		if (textArea.TextView.LineHeightRemainder > 0)
		{
			num++;
		}
		if (Math.Abs(num - line) > treshold)
		{
			vScrollBar.Value = Math.Max(0, Math.Min(vScrollBar.Maximum, (line - scrollMarginHeight + 3) * textArea.TextView.FontHeight));
			VScrollBarValueChanged(this, EventArgs.Empty);
		}
	}

	public void JumpTo(int line)
	{
		line = Math.Min(line, Document.TotalNumberOfLines - 1);
		string text = Document.GetText(Document.GetLineSegment(line));
		JumpTo(line, text.Length - text.TrimStart().Length);
	}

	public void JumpTo(int line, int column)
	{
		textArea.Focus();
		textArea.SelectionManager.ClearSelection();
		textArea.Caret.Position = new TextLocation(column, line);
		textArea.SetDesiredColumn();
		ScrollToCaret();
	}

	protected override void WndProc(ref Message m)
	{
		if (m.Msg == 123 && ShowContextMenu != null)
		{
			long num = m.LParam.ToInt64();
			int num2 = (short)(num & 0xFFFF);
			int num3 = (short)((num & 0xFFFF0000u) >> 16);
			if (num2 == -1 && num3 == -1)
			{
				Point screenPosition = Caret.ScreenPosition;
				ShowContextMenu(this, new MouseEventArgs(MouseButtons.None, 0, screenPosition.X, screenPosition.Y + textArea.TextView.FontHeight, 0));
			}
			else
			{
				Point point = PointToClient(new Point(num2, num3));
				ShowContextMenu(this, new MouseEventArgs(MouseButtons.Right, 1, point.X, point.Y, 0));
			}
		}
		base.WndProc(ref m);
	}

	protected override void OnEnter(EventArgs e)
	{
		Caret.ValidateCaretPos();
		base.OnEnter(e);
	}
}
