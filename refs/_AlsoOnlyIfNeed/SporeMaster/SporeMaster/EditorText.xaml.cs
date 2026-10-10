using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

namespace SporeMaster;

public partial class EditorText : UserControl, IEditor, IComponentConnector
{
	private string editing;

	private int editorSaveUndoCount;

	public EditorText()
	{
		InitializeComponent();
		if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
		{
			Width = double.NaN;
			Height = double.NaN;
			Editor.Document.HighlightingStrategy = HighlightingStrategyFactory.CreateHighlightingStrategyForFile("foo.xml");
			Editor.IndentStyle = IndentStyle.Smart;
			Editor.TabIndent = 2;
			Editor.Document.TextEditorProperties.IndentationSize = 2;
			Editor.ConvertTabsToSpaces = true;
		}
	}

	public void Open(string path, bool read_only)
	{
		if (path.EndsWith("\\"))
		{
			path = null;
		}
		if (editing != path)
		{
			Save();
			editing = null;
			Editor.Document.UndoStack.ClearAll();
			editorSaveUndoCount = Editor.Document.UndoStack.UndoItemCount;
			if (path != null)
			{
				try
				{
					Editor.Document.TextContent = File.ReadAllText(path);
				}
				catch (Exception ex)
				{
					Editor.Document.TextContent = "Unable to load file: " + path + ".\n\n" + ex.ToString();
					Editor.IsReadOnly = true;
					Editor.BackColor = Color.Yellow;
					Editor.Refresh();
					return;
				}
				if (!read_only)
				{
					editing = path;
				}
			}
			else
			{
				Editor.Document.TextContent = "";
			}
			Editor.Refresh();
		}
		Editor.IsReadOnly = read_only;
		Editor.BackColor = (read_only ? Color.LightGray : Color.White);
	}

	public void Search(SearchSpec search)
	{
		Editor.Document.MarkerStrategy.RemoveAll(delegate
		{
			return true;
		});
		if (Editor.Document.TextLength != 0)
		{
			bool anyvisible = false;
			int firstmatch = int.MaxValue;
			string lower = Editor.Document.TextContent.ToLowerInvariant();
			SearchSpec.Sequence[] require_all = search.require_all;
			for (int num = 0; num < require_all.Length; num++)
			{
				SearchSpec.Sequence word = require_all[num];
				int pos = 0;
				TextView view = Editor.ActiveTextAreaControl.TextArea.TextView;
				int topRow = view.FirstVisibleLine;
				int bottomRow = topRow + view.VisibleLineCount;
				int leftColumn = Editor.ActiveTextAreaControl.HScrollBar.Value - Editor.ActiveTextAreaControl.HScrollBar.Minimum;
				int rightColumn = leftColumn + view.VisibleColumnCount;
				while ((pos = lower.IndexOf(word.as_lower, pos)) != -1)
				{
					Editor.Document.MarkerStrategy.AddMarker(new TextMarker(pos, word.as_lower.Length, TextMarkerType.SolidBlock, Color.Red, Color.White));
					firstmatch = Math.Min(firstmatch, pos);
					if (!anyvisible)
					{
						int line = Editor.Document.GetLineNumberForOffset(pos);
						int col = pos - Editor.Document.GetLineSegmentForOffset(pos).Offset;
						anyvisible = line >= topRow && line < bottomRow && col >= leftColumn && col < rightColumn;
					}
					pos += word.as_lower.Length;
				}
			}
			if (!anyvisible && firstmatch != int.MaxValue)
			{
				int line2 = Editor.Document.GetLineNumberForOffset(firstmatch);
				int col2 = firstmatch - Editor.Document.GetLineSegmentForOffset(firstmatch).Offset;
				Editor.ActiveTextAreaControl.ScrollTo(line2, col2);
			}
		}
		Editor.Refresh();
	}

	public void Save()
	{
		if (editing != null && Editor.Document.UndoStack.UndoItemCount != editorSaveUndoCount)
		{
			editorSaveUndoCount = Editor.Document.UndoStack.UndoItemCount;
			File.WriteAllText(editing, Editor.Document.TextContent);
		}
	}

	public string GetSelectedText()
	{
		if (!Editor.ActiveTextAreaControl.SelectionManager.HasSomethingSelected)
		{
			return null;
		}
		return Editor.ActiveTextAreaControl.SelectionManager.SelectedText;
	}

	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocater = new Uri("/SporeMaster;component/editortext.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocater);
		}
	}
}
