using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor;

[ToolboxBitmap("ICSharpCode.TextEditor.Resources.TextEditorControl.bmp")]
[ToolboxItem(true)]
public class TextEditorControl : TextEditorControlBase
{
	protected Panel textAreaPanel = new Panel();

	private TextAreaControl primaryTextArea;

	private Splitter textAreaSplitter;

	private TextAreaControl secondaryTextArea;

	private PrintDocument printDocument;

	private TextAreaControl activeTextAreaControl;

	private int curLineNr;

	private float curTabIndent;

	private StringFormat printingStringFormat;

	[Browsable(false)]
	public PrintDocument PrintDocument
	{
		get
		{
			if (printDocument == null)
			{
				printDocument = new PrintDocument();
				printDocument.BeginPrint += BeginPrint;
				printDocument.PrintPage += PrintPage;
			}
			return printDocument;
		}
	}

	public override TextAreaControl ActiveTextAreaControl => activeTextAreaControl;

	[Browsable(false)]
	public bool EnableUndo => Document.UndoStack.CanUndo;

	[Browsable(false)]
	public bool EnableRedo => Document.UndoStack.CanRedo;

	public event EventHandler ActiveTextAreaControlChanged;

	protected void SetActiveTextAreaControl(TextAreaControl value)
	{
		if (activeTextAreaControl != value)
		{
			activeTextAreaControl = value;
			if (ActiveTextAreaControlChanged != null)
			{
				ActiveTextAreaControlChanged(this, EventArgs.Empty);
			}
		}
	}

	public TextEditorControl()
	{
		SetStyle(ControlStyles.ContainerControl, value: true);
		textAreaPanel.Dock = DockStyle.Fill;
		Document = new DocumentFactory().CreateDocument();
		Document.HighlightingStrategy = HighlightingStrategyFactory.CreateHighlightingStrategy();
		primaryTextArea = new TextAreaControl(this);
		activeTextAreaControl = primaryTextArea;
		TextArea textArea = primaryTextArea.TextArea;
		EventHandler value = delegate
		{
			SetActiveTextAreaControl(primaryTextArea);
		};
		textArea.GotFocus += value;
		primaryTextArea.Dock = DockStyle.Fill;
		textAreaPanel.Controls.Add(primaryTextArea);
		InitializeTextAreaControl(primaryTextArea);
		Controls.Add(textAreaPanel);
		ResizeRedraw = true;
		Document.UpdateCommited += CommitUpdateRequested;
		OptionsChanged();
	}

	protected virtual void InitializeTextAreaControl(TextAreaControl newControl)
	{
	}

	public override void OptionsChanged()
	{
		primaryTextArea.OptionsChanged();
		if (secondaryTextArea != null)
		{
			secondaryTextArea.OptionsChanged();
		}
	}

	public void Split()
	{
		if (secondaryTextArea == null)
		{
			secondaryTextArea = new TextAreaControl(this);
			secondaryTextArea.Dock = DockStyle.Bottom;
			secondaryTextArea.Height = Height / 2;
			secondaryTextArea.TextArea.GotFocus += delegate
			{
				SetActiveTextAreaControl(secondaryTextArea);
			};
			textAreaSplitter = new Splitter();
			textAreaSplitter.BorderStyle = BorderStyle.FixedSingle;
			textAreaSplitter.Height = 8;
			textAreaSplitter.Dock = DockStyle.Bottom;
			textAreaPanel.Controls.Add(textAreaSplitter);
			textAreaPanel.Controls.Add(secondaryTextArea);
			InitializeTextAreaControl(secondaryTextArea);
			secondaryTextArea.OptionsChanged();
		}
		else
		{
			SetActiveTextAreaControl(primaryTextArea);
			textAreaPanel.Controls.Remove(secondaryTextArea);
			textAreaPanel.Controls.Remove(textAreaSplitter);
			secondaryTextArea.Dispose();
			textAreaSplitter.Dispose();
			secondaryTextArea = null;
			textAreaSplitter = null;
		}
	}

	public void Undo()
	{
		if (!Document.ReadOnly && Document.UndoStack.CanUndo)
		{
			BeginUpdate();
			Document.UndoStack.Undo();
			Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
			primaryTextArea.TextArea.UpdateMatchingBracket();
			if (secondaryTextArea != null)
			{
				secondaryTextArea.TextArea.UpdateMatchingBracket();
			}
			EndUpdate();
		}
	}

	public void Redo()
	{
		if (!Document.ReadOnly && Document.UndoStack.CanRedo)
		{
			BeginUpdate();
			Document.UndoStack.Redo();
			Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
			primaryTextArea.TextArea.UpdateMatchingBracket();
			if (secondaryTextArea != null)
			{
				secondaryTextArea.TextArea.UpdateMatchingBracket();
			}
			EndUpdate();
		}
	}

	public void SetHighlighting(string name)
	{
		Document.HighlightingStrategy = HighlightingStrategyFactory.CreateHighlightingStrategy(name);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (printDocument != null)
			{
				printDocument.BeginPrint -= BeginPrint;
				printDocument.PrintPage -= PrintPage;
				printDocument = null;
			}
			Document.UndoStack.ClearAll();
			Document.UpdateCommited -= CommitUpdateRequested;
			if (textAreaPanel != null)
			{
				if (secondaryTextArea != null)
				{
					secondaryTextArea.Dispose();
					textAreaSplitter.Dispose();
					secondaryTextArea = null;
					textAreaSplitter = null;
				}
				if (primaryTextArea != null)
				{
					primaryTextArea.Dispose();
				}
				textAreaPanel.Dispose();
				textAreaPanel = null;
			}
		}
		base.Dispose(disposing);
	}

	public override void EndUpdate()
	{
		base.EndUpdate();
		Document.CommitUpdate();
		if (!IsInUpdate)
		{
			ActiveTextAreaControl.Caret.OnEndUpdate();
		}
	}

	private void CommitUpdateRequested(object sender, EventArgs e)
	{
		if (IsInUpdate)
		{
			return;
		}
		foreach (TextAreaUpdate item in Document.UpdateQueue)
		{
			switch (item.TextAreaUpdateType)
			{
			case TextAreaUpdateType.PositionToEnd:
				primaryTextArea.TextArea.UpdateToEnd(item.Position.Y);
				if (secondaryTextArea != null)
				{
					secondaryTextArea.TextArea.UpdateToEnd(item.Position.Y);
				}
				break;
			case TextAreaUpdateType.SingleLine:
			case TextAreaUpdateType.PositionToLineEnd:
				primaryTextArea.TextArea.UpdateLine(item.Position.Y);
				if (secondaryTextArea != null)
				{
					secondaryTextArea.TextArea.UpdateLine(item.Position.Y);
				}
				break;
			case TextAreaUpdateType.SinglePosition:
				primaryTextArea.TextArea.UpdateLine(item.Position.Y, item.Position.X, item.Position.X);
				if (secondaryTextArea != null)
				{
					secondaryTextArea.TextArea.UpdateLine(item.Position.Y, item.Position.X, item.Position.X);
				}
				break;
			case TextAreaUpdateType.LinesBetween:
				primaryTextArea.TextArea.UpdateLines(item.Position.X, item.Position.Y);
				if (secondaryTextArea != null)
				{
					secondaryTextArea.TextArea.UpdateLines(item.Position.X, item.Position.Y);
				}
				break;
			case TextAreaUpdateType.WholeTextArea:
				primaryTextArea.TextArea.Invalidate();
				if (secondaryTextArea != null)
				{
					secondaryTextArea.TextArea.Invalidate();
				}
				break;
			}
		}
		Document.UpdateQueue.Clear();
	}

	private void BeginPrint(object sender, PrintEventArgs ev)
	{
		curLineNr = 0;
		printingStringFormat = (StringFormat)StringFormat.GenericTypographic.Clone();
		float[] array = new float[100];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = TabIndent * primaryTextArea.TextArea.TextView.WideSpaceWidth;
		}
		printingStringFormat.SetTabStops(0f, array);
	}

	private void Advance(ref float x, ref float y, float maxWidth, float size, float fontHeight)
	{
		if (x + size < maxWidth)
		{
			x += size;
			return;
		}
		x = curTabIndent;
		y += fontHeight;
	}

	private float MeasurePrintingHeight(Graphics g, LineSegment line, float maxWidth)
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = Font.GetHeight(g);
		curTabIndent = 0f;
		FontContainer fontContainer = TextEditorProperties.FontContainer;
		foreach (TextWord word in line.Words)
		{
			switch (word.Type)
			{
			case TextWordType.Space:
				Advance(ref num, ref num2, maxWidth, primaryTextArea.TextArea.TextView.SpaceWidth, num3);
				break;
			case TextWordType.Tab:
				Advance(ref num, ref num2, maxWidth, TabIndent * primaryTextArea.TextArea.TextView.WideSpaceWidth, num3);
				break;
			case TextWordType.Word:
				Advance(ref num, ref num2, maxWidth, g.MeasureString(word.Word, word.GetFont(fontContainer), new SizeF(maxWidth, num3 * 100f), printingStringFormat).Width, num3);
				break;
			}
		}
		return num2 + num3;
	}

	private void DrawLine(Graphics g, LineSegment line, float yPos, RectangleF margin)
	{
		float num = 0f;
		float num2 = Font.GetHeight(g);
		curTabIndent = 0f;
		FontContainer fontContainer = TextEditorProperties.FontContainer;
		foreach (TextWord word in line.Words)
		{
			switch (word.Type)
			{
			case TextWordType.Space:
				Advance(ref num, ref yPos, margin.Width, primaryTextArea.TextArea.TextView.SpaceWidth, num2);
				break;
			case TextWordType.Tab:
				Advance(ref num, ref yPos, margin.Width, TabIndent * primaryTextArea.TextArea.TextView.WideSpaceWidth, num2);
				break;
			case TextWordType.Word:
			{
				g.DrawString(word.Word, word.GetFont(fontContainer), BrushRegistry.GetBrush(word.Color), num + margin.X, yPos);
				SizeF sizeF = g.MeasureString(word.Word, word.GetFont(fontContainer), new SizeF(margin.Width, num2 * 100f), printingStringFormat);
				Advance(ref num, ref yPos, margin.Width, sizeF.Width, num2);
				break;
			}
			}
		}
	}

	private void PrintPage(object sender, PrintPageEventArgs ev)
	{
		Graphics graphics = ev.Graphics;
		float num = ev.MarginBounds.Top;
		while (curLineNr < Document.TotalNumberOfLines)
		{
			LineSegment lineSegment = Document.GetLineSegment(curLineNr);
			if (lineSegment.Words != null)
			{
				float num2 = MeasurePrintingHeight(graphics, lineSegment, ev.MarginBounds.Width);
				if (num2 + num > (float)ev.MarginBounds.Bottom)
				{
					break;
				}
				DrawLine(graphics, lineSegment, num, ev.MarginBounds);
				num += num2;
			}
			curLineNr++;
		}
		ev.HasMorePages = curLineNr < Document.TotalNumberOfLines;
	}
}
