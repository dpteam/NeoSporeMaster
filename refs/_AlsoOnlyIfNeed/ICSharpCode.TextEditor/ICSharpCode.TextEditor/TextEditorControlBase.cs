using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Text;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Actions;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Util;

namespace ICSharpCode.TextEditor;

[ToolboxItem(false)]
public abstract class TextEditorControlBase : UserControl
{
	private string currentFileName;

	private int updateLevel;

	private IDocument document;

	protected Dictionary<Keys, IEditAction> editactions = new Dictionary<Keys, IEditAction>();

	private Encoding encoding;

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false)]
	public ITextEditorProperties TextEditorProperties
	{
		get
		{
			return document.TextEditorProperties;
		}
		set
		{
			document.TextEditorProperties = value;
			OptionsChanged();
		}
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Encoding Encoding
	{
		get
		{
			if (encoding == null)
			{
				return TextEditorProperties.Encoding;
			}
			return encoding;
		}
		set
		{
			encoding = value;
		}
	}

	[Browsable(false)]
	[ReadOnly(true)]
	public string FileName
	{
		get
		{
			return currentFileName;
		}
		set
		{
			if (currentFileName != value)
			{
				currentFileName = value;
				OnFileNameChanged(EventArgs.Empty);
			}
		}
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IDocument Document
	{
		get
		{
			return document;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (document != null)
			{
				document.DocumentChanged -= OnDocumentChanged;
			}
			document = value;
			document.UndoStack.TextEditorControl = this;
			document.DocumentChanged += OnDocumentChanged;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Always)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	[Browsable(true)]
	[Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
	public override string Text
	{
		get
		{
			return Document.TextContent;
		}
		set
		{
			Document.TextContent = value;
		}
	}

	[Browsable(false)]
	public bool IsReadOnly
	{
		get
		{
			return Document.ReadOnly;
		}
		set
		{
			Document.ReadOnly = value;
		}
	}

	[Browsable(false)]
	public bool IsInUpdate => updateLevel > 0;

	protected override Size DefaultSize => new Size(100, 100);

	[Description("If true spaces are shown in the textarea")]
	[Category("Appearance")]
	[DefaultValue(false)]
	public bool ShowSpaces
	{
		get
		{
			return document.TextEditorProperties.ShowSpaces;
		}
		set
		{
			document.TextEditorProperties.ShowSpaces = value;
			OptionsChanged();
		}
	}

	[Description("Specifies the quality of text rendering (whether to use hinting and/or anti-aliasing).")]
	[Category("Appearance")]
	[DefaultValue(TextRenderingHint.SystemDefault)]
	public TextRenderingHint TextRenderingHint
	{
		get
		{
			return document.TextEditorProperties.TextRenderingHint;
		}
		set
		{
			document.TextEditorProperties.TextRenderingHint = value;
			OptionsChanged();
		}
	}

	[DefaultValue(false)]
	[Description("If true tabs are shown in the textarea")]
	[Category("Appearance")]
	public bool ShowTabs
	{
		get
		{
			return document.TextEditorProperties.ShowTabs;
		}
		set
		{
			document.TextEditorProperties.ShowTabs = value;
			OptionsChanged();
		}
	}

	[DefaultValue(false)]
	[Description("If true EOL markers are shown in the textarea")]
	[Category("Appearance")]
	public bool ShowEOLMarkers
	{
		get
		{
			return document.TextEditorProperties.ShowEOLMarker;
		}
		set
		{
			document.TextEditorProperties.ShowEOLMarker = value;
			OptionsChanged();
		}
	}

	[Category("Appearance")]
	[DefaultValue(false)]
	[Description("If true the horizontal ruler is shown in the textarea")]
	public bool ShowHRuler
	{
		get
		{
			return document.TextEditorProperties.ShowHorizontalRuler;
		}
		set
		{
			document.TextEditorProperties.ShowHorizontalRuler = value;
			OptionsChanged();
		}
	}

	[Description("If true the vertical ruler is shown in the textarea")]
	[Category("Appearance")]
	[DefaultValue(true)]
	public bool ShowVRuler
	{
		get
		{
			return document.TextEditorProperties.ShowVerticalRuler;
		}
		set
		{
			document.TextEditorProperties.ShowVerticalRuler = value;
			OptionsChanged();
		}
	}

	[Description("The row in which the vertical ruler is displayed")]
	[Category("Appearance")]
	[DefaultValue(80)]
	public int VRulerRow
	{
		get
		{
			return document.TextEditorProperties.VerticalRulerRow;
		}
		set
		{
			document.TextEditorProperties.VerticalRulerRow = value;
			OptionsChanged();
		}
	}

	[Description("If true line numbers are shown in the textarea")]
	[Category("Appearance")]
	[DefaultValue(true)]
	public bool ShowLineNumbers
	{
		get
		{
			return document.TextEditorProperties.ShowLineNumbers;
		}
		set
		{
			document.TextEditorProperties.ShowLineNumbers = value;
			OptionsChanged();
		}
	}

	[Description("If true invalid lines are marked in the textarea")]
	[DefaultValue(false)]
	[Category("Appearance")]
	public bool ShowInvalidLines
	{
		get
		{
			return document.TextEditorProperties.ShowInvalidLines;
		}
		set
		{
			document.TextEditorProperties.ShowInvalidLines = value;
			OptionsChanged();
		}
	}

	[Category("Appearance")]
	[Description("If true folding is enabled in the textarea")]
	[DefaultValue(true)]
	public bool EnableFolding
	{
		get
		{
			return document.TextEditorProperties.EnableFolding;
		}
		set
		{
			document.TextEditorProperties.EnableFolding = value;
			OptionsChanged();
		}
	}

	[DefaultValue(true)]
	[Description("If true matching brackets are highlighted")]
	[Category("Appearance")]
	public bool ShowMatchingBracket
	{
		get
		{
			return document.TextEditorProperties.ShowMatchingBracket;
		}
		set
		{
			document.TextEditorProperties.ShowMatchingBracket = value;
			OptionsChanged();
		}
	}

	[Category("Appearance")]
	[Description("If true the icon bar is displayed")]
	[DefaultValue(false)]
	public bool IsIconBarVisible
	{
		get
		{
			return document.TextEditorProperties.IsIconBarVisible;
		}
		set
		{
			document.TextEditorProperties.IsIconBarVisible = value;
			OptionsChanged();
		}
	}

	[Description("The width in spaces of a tab character")]
	[DefaultValue(4)]
	[Category("Appearance")]
	public int TabIndent
	{
		get
		{
			return document.TextEditorProperties.TabIndent;
		}
		set
		{
			document.TextEditorProperties.TabIndent = value;
			OptionsChanged();
		}
	}

	[Description("The line viewer style")]
	[Category("Appearance")]
	[DefaultValue(LineViewerStyle.None)]
	public LineViewerStyle LineViewerStyle
	{
		get
		{
			return document.TextEditorProperties.LineViewerStyle;
		}
		set
		{
			document.TextEditorProperties.LineViewerStyle = value;
			OptionsChanged();
		}
	}

	[DefaultValue(IndentStyle.Smart)]
	[Description("The indent style")]
	[Category("Behavior")]
	public IndentStyle IndentStyle
	{
		get
		{
			return document.TextEditorProperties.IndentStyle;
		}
		set
		{
			document.TextEditorProperties.IndentStyle = value;
			OptionsChanged();
		}
	}

	[Description("Converts tabs to spaces while typing")]
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool ConvertTabsToSpaces
	{
		get
		{
			return document.TextEditorProperties.ConvertTabsToSpaces;
		}
		set
		{
			document.TextEditorProperties.ConvertTabsToSpaces = value;
			OptionsChanged();
		}
	}

	[DefaultValue(false)]
	[Category("Behavior")]
	[Description("Hide the mouse cursor while typing")]
	public bool HideMouseCursor
	{
		get
		{
			return document.TextEditorProperties.HideMouseCursor;
		}
		set
		{
			document.TextEditorProperties.HideMouseCursor = value;
			OptionsChanged();
		}
	}

	[DefaultValue(false)]
	[Description("Allows the caret to be placed beyond the end of line")]
	[Category("Behavior")]
	public bool AllowCaretBeyondEOL
	{
		get
		{
			return document.TextEditorProperties.AllowCaretBeyondEOL;
		}
		set
		{
			document.TextEditorProperties.AllowCaretBeyondEOL = value;
			OptionsChanged();
		}
	}

	[Description("Specifies if the bracket matching should match the bracket before or after the caret.")]
	[Category("Behavior")]
	[DefaultValue(BracketMatchingStyle.After)]
	public BracketMatchingStyle BracketMatchingStyle
	{
		get
		{
			return document.TextEditorProperties.BracketMatchingStyle;
		}
		set
		{
			document.TextEditorProperties.BracketMatchingStyle = value;
			OptionsChanged();
		}
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	[Browsable(true)]
	[Description("The base font of the text area. No bold or italic fonts can be used because bold/italic is reserved for highlighting purposes.")]
	public override Font Font
	{
		get
		{
			return document.TextEditorProperties.Font;
		}
		set
		{
			document.TextEditorProperties.Font = value;
			OptionsChanged();
		}
	}

	public abstract TextAreaControl ActiveTextAreaControl { get; }

	[EditorBrowsable(EditorBrowsableState.Always)]
	[Browsable(true)]
	public new event EventHandler TextChanged
	{
		add
		{
			base.TextChanged += value;
		}
		remove
		{
			base.TextChanged -= value;
		}
	}

	public event EventHandler FileNameChanged;

	private void OnDocumentChanged(object sender, EventArgs e)
	{
		OnTextChanged(e);
	}

	private static Font ParseFont(string font)
	{
		string[] array = font.Split(',', '=');
		return new Font(array[1], float.Parse(array[3]));
	}

	protected TextEditorControlBase()
	{
		GenerateDefaultActions();
		HighlightingManager.Manager.ReloadSyntaxHighlighting += OnReloadHighlighting;
	}

	protected virtual void OnReloadHighlighting(object sender, EventArgs e)
	{
		if (Document.HighlightingStrategy != null)
		{
			try
			{
				Document.HighlightingStrategy = HighlightingStrategyFactory.CreateHighlightingStrategy(Document.HighlightingStrategy.Name);
			}
			catch (HighlightingDefinitionInvalidException ex)
			{
				MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
			OptionsChanged();
		}
	}

	public bool IsEditAction(Keys keyData)
	{
		return editactions.ContainsKey(keyData);
	}

	internal IEditAction GetEditAction(Keys keyData)
	{
		if (!IsEditAction(keyData))
		{
			return null;
		}
		return editactions[keyData];
	}

	private void GenerateDefaultActions()
	{
		editactions[Keys.Left] = new CaretLeft();
		editactions[Keys.Left | Keys.Shift] = new ShiftCaretLeft();
		editactions[Keys.Left | Keys.Control] = new WordLeft();
		editactions[Keys.Left | Keys.Shift | Keys.Control] = new ShiftWordLeft();
		editactions[Keys.Right] = new CaretRight();
		editactions[Keys.Right | Keys.Shift] = new ShiftCaretRight();
		editactions[Keys.Right | Keys.Control] = new WordRight();
		editactions[Keys.Right | Keys.Shift | Keys.Control] = new ShiftWordRight();
		editactions[Keys.Up] = new CaretUp();
		editactions[Keys.Up | Keys.Shift] = new ShiftCaretUp();
		editactions[Keys.Up | Keys.Control] = new ScrollLineUp();
		editactions[Keys.Down] = new CaretDown();
		editactions[Keys.Down | Keys.Shift] = new ShiftCaretDown();
		editactions[Keys.Down | Keys.Control] = new ScrollLineDown();
		editactions[Keys.Insert] = new ToggleEditMode();
		editactions[Keys.Insert | Keys.Control] = new Copy();
		editactions[Keys.Insert | Keys.Shift] = new Paste();
		editactions[Keys.Delete] = new Delete();
		editactions[Keys.Delete | Keys.Shift] = new Cut();
		editactions[Keys.Home] = new Home();
		editactions[Keys.Home | Keys.Shift] = new ShiftHome();
		editactions[Keys.Home | Keys.Control] = new MoveToStart();
		editactions[Keys.Home | Keys.Shift | Keys.Control] = new ShiftMoveToStart();
		editactions[Keys.End] = new End();
		editactions[Keys.End | Keys.Shift] = new ShiftEnd();
		editactions[Keys.End | Keys.Control] = new MoveToEnd();
		editactions[Keys.End | Keys.Shift | Keys.Control] = new ShiftMoveToEnd();
		editactions[Keys.Prior] = new MovePageUp();
		editactions[Keys.Prior | Keys.Shift] = new ShiftMovePageUp();
		editactions[Keys.Next] = new MovePageDown();
		editactions[Keys.Next | Keys.Shift] = new ShiftMovePageDown();
		editactions[Keys.Return] = new Return();
		editactions[Keys.Tab] = new Tab();
		editactions[Keys.Tab | Keys.Shift] = new ShiftTab();
		editactions[Keys.Back] = new Backspace();
		editactions[Keys.Back | Keys.Shift] = new Backspace();
		editactions[Keys.X | Keys.Control] = new Cut();
		editactions[Keys.C | Keys.Control] = new Copy();
		editactions[Keys.V | Keys.Control] = new Paste();
		editactions[Keys.A | Keys.Control] = new SelectWholeDocument();
		editactions[Keys.Escape] = new ClearAllSelections();
		editactions[Keys.Divide | Keys.Control] = new ToggleComment();
		editactions[Keys.OemQuestion | Keys.Control] = new ToggleComment();
		editactions[Keys.Back | Keys.Alt] = new ICSharpCode.TextEditor.Actions.Undo();
		editactions[Keys.Z | Keys.Control] = new ICSharpCode.TextEditor.Actions.Undo();
		editactions[Keys.Y | Keys.Control] = new Redo();
		editactions[Keys.Delete | Keys.Control] = new DeleteWord();
		editactions[Keys.Back | Keys.Control] = new WordBackspace();
		editactions[Keys.D | Keys.Control] = new DeleteLine();
		editactions[Keys.D | Keys.Shift | Keys.Control] = new DeleteToLineEnd();
		editactions[Keys.B | Keys.Control] = new GotoMatchingBrace();
	}

	public virtual void BeginUpdate()
	{
		updateLevel++;
	}

	public virtual void EndUpdate()
	{
		updateLevel = Math.Max(0, updateLevel - 1);
	}

	public void LoadFile(string fileName)
	{
		LoadFile(fileName, autoLoadHighlighting: true, autodetectEncoding: true);
	}

	public void LoadFile(string fileName, bool autoLoadHighlighting, bool autodetectEncoding)
	{
		using FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
		LoadFile(fileName, stream, autoLoadHighlighting, autodetectEncoding);
	}

	public void LoadFile(string fileName, Stream stream, bool autoLoadHighlighting, bool autodetectEncoding)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		BeginUpdate();
		document.TextContent = string.Empty;
		document.UndoStack.ClearAll();
		document.BookmarkManager.Clear();
		if (autoLoadHighlighting)
		{
			try
			{
				document.HighlightingStrategy = HighlightingStrategyFactory.CreateHighlightingStrategyForFile(fileName);
			}
			catch (HighlightingDefinitionInvalidException ex)
			{
				MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}
		if (autodetectEncoding)
		{
			Encoding encoding = Encoding;
			Document.TextContent = FileReader.ReadFileContent(stream, ref encoding);
			Encoding = encoding;
		}
		else
		{
			using StreamReader streamReader = new StreamReader(fileName, Encoding);
			Document.TextContent = streamReader.ReadToEnd();
		}
		FileName = fileName;
		OptionsChanged();
		Document.UpdateQueue.Clear();
		EndUpdate();
		Refresh();
	}

	public bool CanSaveWithCurrentEncoding()
	{
		if (encoding == null || FileReader.IsUnicode(encoding))
		{
			return true;
		}
		string textContent = document.TextContent;
		return encoding.GetString(encoding.GetBytes(textContent)) == textContent;
	}

	public void SaveFile(string fileName)
	{
		using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
		{
			SaveFile(stream);
		}
		FileName = fileName;
	}

	public void SaveFile(Stream stream)
	{
		StreamWriter streamWriter = new StreamWriter(stream, Encoding ?? Encoding.UTF8);
		foreach (LineSegment item in Document.LineSegmentCollection)
		{
			streamWriter.Write(Document.GetText(item.Offset, item.Length));
			if (item.DelimiterLength > 0)
			{
				char charAt = Document.GetCharAt(item.Offset + item.Length);
				if (charAt != '\n' && charAt != '\r')
				{
					throw new InvalidOperationException("The document cannot be saved because it is corrupted.");
				}
				streamWriter.Write(document.TextEditorProperties.LineTerminator);
			}
		}
		streamWriter.Flush();
	}

	public abstract void OptionsChanged();

	public virtual string GetRangeDescription(int selectedItem, int itemCount)
	{
		StringBuilder stringBuilder = new StringBuilder(selectedItem.ToString());
		stringBuilder.Append(" from ");
		stringBuilder.Append(itemCount.ToString());
		return stringBuilder.ToString();
	}

	public override void Refresh()
	{
		if (!IsInUpdate)
		{
			base.Refresh();
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			HighlightingManager.Manager.ReloadSyntaxHighlighting -= OnReloadHighlighting;
			document.HighlightingStrategy = null;
			document.UndoStack.TextEditorControl = null;
		}
		base.Dispose(disposing);
	}

	protected virtual void OnFileNameChanged(EventArgs e)
	{
		if (FileNameChanged != null)
		{
			FileNameChanged(this, e);
		}
	}
}
