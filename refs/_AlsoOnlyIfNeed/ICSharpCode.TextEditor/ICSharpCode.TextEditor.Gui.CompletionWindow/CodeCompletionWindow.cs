using System;
using System.Drawing;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Util;

namespace ICSharpCode.TextEditor.Gui.CompletionWindow;

public class CodeCompletionWindow : AbstractCompletionWindow
{
	private const int ScrollbarWidth = 16;

	private const int MaxListLength = 10;

	private ICompletionData[] completionData;

	private CodeCompletionListView codeCompletionListView;

	private VScrollBar vScrollBar = new VScrollBar();

	private ICompletionDataProvider dataProvider;

	private IDocument document;

	private bool showDeclarationWindow = true;

	private bool fixedListViewWidth = true;

	private int startOffset;

	private int endOffset;

	private DeclarationViewWindow declarationViewWindow;

	private Rectangle workingScreen;

	private bool inScrollUpdate;

	private MouseWheelHandler mouseWheelHandler = new MouseWheelHandler();

	public bool CloseWhenCaretAtBeginning { get; set; }

	public static CodeCompletionWindow ShowCompletionWindow(Form parent, TextEditorControl control, string fileName, ICompletionDataProvider completionDataProvider, char firstChar)
	{
		return ShowCompletionWindow(parent, control, fileName, completionDataProvider, firstChar, showDeclarationWindow: true, fixedListViewWidth: true);
	}

	public static CodeCompletionWindow ShowCompletionWindow(Form parent, TextEditorControl control, string fileName, ICompletionDataProvider completionDataProvider, char firstChar, bool showDeclarationWindow, bool fixedListViewWidth)
	{
		ICompletionData[] array = completionDataProvider.GenerateCompletionData(fileName, control.ActiveTextAreaControl.TextArea, firstChar);
		if (array == null || array.Length == 0)
		{
			return null;
		}
		CodeCompletionWindow codeCompletionWindow = new CodeCompletionWindow(completionDataProvider, array, parent, control, showDeclarationWindow, fixedListViewWidth);
		codeCompletionWindow.CloseWhenCaretAtBeginning = firstChar == '\0';
		codeCompletionWindow.ShowCompletionWindow();
		return codeCompletionWindow;
	}

	private CodeCompletionWindow(ICompletionDataProvider completionDataProvider, ICompletionData[] completionData, Form parentForm, TextEditorControl control, bool showDeclarationWindow, bool fixedListViewWidth)
		: base(parentForm, control)
	{
		dataProvider = completionDataProvider;
		this.completionData = completionData;
		document = control.Document;
		this.showDeclarationWindow = showDeclarationWindow;
		this.fixedListViewWidth = fixedListViewWidth;
		workingScreen = Screen.GetWorkingArea(Location);
		startOffset = control.ActiveTextAreaControl.Caret.Offset + 1;
		endOffset = startOffset;
		if (completionDataProvider.PreSelection != null)
		{
			startOffset -= completionDataProvider.PreSelection.Length + 1;
			endOffset--;
		}
		codeCompletionListView = new CodeCompletionListView(completionData);
		codeCompletionListView.ImageList = completionDataProvider.ImageList;
		codeCompletionListView.Dock = DockStyle.Fill;
		codeCompletionListView.SelectedItemChanged += CodeCompletionListViewSelectedItemChanged;
		codeCompletionListView.DoubleClick += CodeCompletionListViewDoubleClick;
		codeCompletionListView.Click += CodeCompletionListViewClick;
		Controls.Add(codeCompletionListView);
		if (completionData.Length > 10)
		{
			vScrollBar.Dock = DockStyle.Right;
			vScrollBar.Minimum = 0;
			vScrollBar.Maximum = completionData.Length - 1;
			vScrollBar.SmallChange = 1;
			vScrollBar.LargeChange = 10;
			codeCompletionListView.FirstItemChanged += CodeCompletionListViewFirstItemChanged;
			Controls.Add(vScrollBar);
		}
		drawingSize = GetListViewSize();
		SetLocation();
		if (declarationViewWindow == null)
		{
			declarationViewWindow = new DeclarationViewWindow(parentForm);
		}
		SetDeclarationViewLocation();
		declarationViewWindow.ShowDeclarationViewWindow();
		declarationViewWindow.MouseMove += ControlMouseMove;
		control.Focus();
		CodeCompletionListViewSelectedItemChanged(this, EventArgs.Empty);
		if (completionDataProvider.DefaultIndex >= 0)
		{
			codeCompletionListView.SelectIndex(completionDataProvider.DefaultIndex);
		}
		if (completionDataProvider.PreSelection != null)
		{
			CaretOffsetChanged(this, EventArgs.Empty);
		}
		vScrollBar.ValueChanged += VScrollBarValueChanged;
		document.DocumentAboutToBeChanged += DocumentAboutToBeChanged;
	}

	private void CodeCompletionListViewFirstItemChanged(object sender, EventArgs e)
	{
		if (!inScrollUpdate)
		{
			inScrollUpdate = true;
			vScrollBar.Value = Math.Min(vScrollBar.Maximum, codeCompletionListView.FirstItem);
			inScrollUpdate = false;
		}
	}

	private void VScrollBarValueChanged(object sender, EventArgs e)
	{
		if (!inScrollUpdate)
		{
			inScrollUpdate = true;
			codeCompletionListView.FirstItem = vScrollBar.Value;
			codeCompletionListView.Refresh();
			control.ActiveTextAreaControl.TextArea.Focus();
			inScrollUpdate = false;
		}
	}

	private void SetDeclarationViewLocation()
	{
		int num = Bounds.Left - workingScreen.Left;
		int num2 = workingScreen.Right - Bounds.Right;
		Point point = ((num2 * 2 <= num) ? new Point(Bounds.Left - declarationViewWindow.Width, Bounds.Top) : new Point(Bounds.Right, Bounds.Top));
		if (declarationViewWindow.Location != point)
		{
			declarationViewWindow.Location = point;
		}
	}

	protected override void SetLocation()
	{
		base.SetLocation();
		if (declarationViewWindow != null)
		{
			SetDeclarationViewLocation();
		}
	}

	public void HandleMouseWheel(MouseEventArgs e)
	{
		int num = mouseWheelHandler.GetScrollAmount(e);
		if (num != 0)
		{
			if (control.TextEditorProperties.MouseWheelScrollDown)
			{
				num = -num;
			}
			int val = vScrollBar.Value + vScrollBar.SmallChange * num;
			vScrollBar.Value = Math.Max(vScrollBar.Minimum, Math.Min(vScrollBar.Maximum - vScrollBar.LargeChange + 1, val));
		}
	}

	private void CodeCompletionListViewSelectedItemChanged(object sender, EventArgs e)
	{
		ICompletionData selectedCompletionData = codeCompletionListView.SelectedCompletionData;
		if (showDeclarationWindow && selectedCompletionData != null && selectedCompletionData.Description != null && selectedCompletionData.Description.Length > 0)
		{
			declarationViewWindow.Description = selectedCompletionData.Description;
			SetDeclarationViewLocation();
		}
		else
		{
			declarationViewWindow.Description = null;
		}
	}

	public override bool ProcessKeyEvent(char ch)
	{
		switch (dataProvider.ProcessKey(ch))
		{
		case CompletionDataProviderKeyResult.BeforeStartKey:
			startOffset++;
			endOffset++;
			return base.ProcessKeyEvent(ch);
		case CompletionDataProviderKeyResult.NormalKey:
			return base.ProcessKeyEvent(ch);
		case CompletionDataProviderKeyResult.InsertionKey:
			return InsertSelectedItem(ch);
		default:
			throw new InvalidOperationException("Invalid return value of dataProvider.ProcessKey");
		}
	}

	private void DocumentAboutToBeChanged(object sender, DocumentEventArgs e)
	{
		if (e.Offset >= startOffset && e.Offset <= endOffset)
		{
			if (e.Length > 0)
			{
				endOffset -= e.Length;
			}
			if (!string.IsNullOrEmpty(e.Text))
			{
				endOffset += e.Text.Length;
			}
		}
	}

	protected override void CaretOffsetChanged(object sender, EventArgs e)
	{
		int offset = control.ActiveTextAreaControl.Caret.Offset;
		if (offset == startOffset)
		{
			if (CloseWhenCaretAtBeginning)
			{
				Close();
			}
		}
		else if (offset < startOffset || offset > endOffset)
		{
			Close();
		}
		else
		{
			codeCompletionListView.SelectItemWithStart(control.Document.GetText(startOffset, offset - startOffset));
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
		case Keys.Home:
			codeCompletionListView.SelectIndex(0);
			return true;
		case Keys.End:
			codeCompletionListView.SelectIndex(completionData.Length - 1);
			return true;
		case Keys.Next:
			codeCompletionListView.PageDown();
			return true;
		case Keys.Prior:
			codeCompletionListView.PageUp();
			return true;
		case Keys.Down:
			codeCompletionListView.SelectNextItem();
			return true;
		case Keys.Up:
			codeCompletionListView.SelectPrevItem();
			return true;
		case Keys.Tab:
			InsertSelectedItem('\t');
			return true;
		case Keys.Return:
			InsertSelectedItem('\n');
			return true;
		default:
			return base.ProcessTextAreaKey(keyData);
		}
	}

	private void CodeCompletionListViewDoubleClick(object sender, EventArgs e)
	{
		InsertSelectedItem('\0');
	}

	private void CodeCompletionListViewClick(object sender, EventArgs e)
	{
		control.ActiveTextAreaControl.TextArea.Focus();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			document.DocumentAboutToBeChanged -= DocumentAboutToBeChanged;
			if (codeCompletionListView != null)
			{
				codeCompletionListView.Dispose();
				codeCompletionListView = null;
			}
			if (declarationViewWindow != null)
			{
				declarationViewWindow.Dispose();
				declarationViewWindow = null;
			}
		}
		base.Dispose(disposing);
	}

	private bool InsertSelectedItem(char ch)
	{
		document.DocumentAboutToBeChanged -= DocumentAboutToBeChanged;
		ICompletionData selectedCompletionData = codeCompletionListView.SelectedCompletionData;
		bool result = false;
		if (selectedCompletionData != null)
		{
			control.BeginUpdate();
			try
			{
				if (endOffset - startOffset > 0)
				{
					control.Document.Remove(startOffset, endOffset - startOffset);
				}
				result = dataProvider.InsertAction(selectedCompletionData, control.ActiveTextAreaControl.TextArea, startOffset, ch);
			}
			finally
			{
				control.EndUpdate();
			}
		}
		Close();
		return result;
	}

	private Size GetListViewSize()
	{
		int num = codeCompletionListView.ItemHeight * Math.Min(10, completionData.Length);
		int defaultWidth = codeCompletionListView.ItemHeight * 10;
		if (!fixedListViewWidth)
		{
			defaultWidth = GetListViewWidth(defaultWidth, num);
		}
		return new Size(defaultWidth, num);
	}

	private int GetListViewWidth(int defaultWidth, int height)
	{
		float num = defaultWidth;
		using (Graphics graphics = codeCompletionListView.CreateGraphics())
		{
			for (int i = 0; i < completionData.Length; i++)
			{
				float num2 = graphics.MeasureString(completionData[i].Text.ToString(), codeCompletionListView.Font).Width;
				if (num2 > num)
				{
					num = num2;
				}
			}
		}
		float num3 = codeCompletionListView.ItemHeight * completionData.Length;
		if (num3 > (float)height)
		{
			num += 16f;
		}
		return (int)num;
	}
}
