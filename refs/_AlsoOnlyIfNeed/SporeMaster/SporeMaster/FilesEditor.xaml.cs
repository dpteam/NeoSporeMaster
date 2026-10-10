using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using VTreeView;

namespace SporeMaster;

public partial class FilesEditor : UserControl, IComponentConnector
{
	public HashSet<string> fullTextExtensions;

	public string WinMergeEXE;

	private DirectoryTree files = DirectoryTree.Tree;

	private DirectoryTreeWatcher[] watcher = new DirectoryTreeWatcher[2];

	private DirectoryTree lastSelectedFile;

	private string[] _Path = new string[2];

	private IEditor currentEditor;

	private string LeftPath => _Path[0];

	private string RightPath => _Path[1];

	private Window Window => MainWindow.Instance;

	public FilesEditor()
	{
		InitializeComponent();
		if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
		{
			Width = double.NaN;
			Height = double.NaN;
		}
	}

	public void SetPath(int side, string value, PleaseWait progress)
	{
		if (!(_Path[side] == value))
		{
			_Path[side] = value;
			if (watcher[side] != null)
			{
				watcher[side].Dispose();
			}
			if (_Path[side] != null && Directory.Exists(_Path[side]))
			{
				watcher[side] = new DirectoryTreeWatcher(files, side, _Path[side], Dispatcher, update, fullTextExtensions, progress);
			}
		}
	}

	public void SearchFiles(string search)
	{
		FileSearch.Text = search;
		update();
	}

	public string GetEditorSelection()
	{
		if (currentEditor != null)
		{
			return currentEditor.GetSelectedText();
		}
		return "";
	}

	private void update()
	{
		files.Search(new SearchSpec(FileSearch.Text));
		if (ShowRightOnly.IsChecked == true)
		{
			files.SearchRightPresent();
		}
		DirTree.Data.ClearAll();
		DirTree.Data.AddRootItem(files);
		if (IsVisible)
		{
			if (lastSelectedFile != null && lastSelectedFile.Parent != null && !lastSelectedFile.LeftPresent && !lastSelectedFile.RightPresent)
			{
				uint instance = NameRegistry.Files.toHash(lastSelectedFile.BaseName);
				uint group = NameRegistry.Groups.toHash(lastSelectedFile.Parent.Path);
				uint ext = NameRegistry.Types.toHash(lastSelectedFile.FileType);
				string newpath = NameRegistry.Groups.toName(group) + "\\" + NameRegistry.Files.toName(instance) + "." + NameRegistry.Types.toName(ext);
				lastSelectedFile = files.getFile(newpath, create: false);
			}
			DirTree.SelectedItem = lastSelectedFile;
			if (DirTree.SelectedItem != null)
			{
				DirTree.ScrollIntoView(DirTree.SelectedItem);
			}
			updateEditorSearch();
		}
	}

	private void FileSearch_TextChanged(object sender, TextChangedEventArgs e)
	{
		update();
	}

	private void editDocument(string path, bool read_only)
	{
		if (path != null && path.EndsWith(".png"))
		{
			currentEditor = ImageEditor;
			TabImageEditor.IsSelected = true;
		}
		else if (path != null && path.EndsWith(".rw4\\") && File.Exists(path + "model.mesh.xml"))
		{
			currentEditor = ModelEditor;
			TabModelEditor.IsSelected = true;
		}
		else
		{
			currentEditor = TextEditor;
			TabTextEditor.IsSelected = true;
		}
		currentEditor.Open(path, read_only);
		updateEditorSearch();
	}

	private void Tree_SelectedItemChanged(object sender, SelectionChangedEventArgs e)
	{
		DirectoryTree s = (sender as global::VTreeView.VTreeView).SelectedItem as DirectoryTree;
		SelectedFilePanel.IsEnabled = s != null;
		if (s != null)
		{
			lastSelectedFile = s;
			SelectedFileLabel.Text = s.Path;
			SelectedFile_Open.IsEnabled = s.LeftPresent;
			SelectedFile_Erase.IsEnabled = s.RightPresent;
			DirectoryTree f = (s.IsFolder ? s : s.Parent);
			SelectedFile_ExploreLeft.IsEnabled = f.LeftPresent;
			SelectedFile_ExploreRight.IsEnabled = f.RightPresent;
			SelectedFile_Save.IsEnabled = false;
			if (!s.LeftPresent && !s.RightPresent)
			{
				editDocument(null, read_only: true);
			}
			else if (s.RightPresent)
			{
				editDocument(RightPath + "\\" + s.Path + (s.IsFolder ? "\\" : ""), read_only: false);
			}
			else
			{
				editDocument(LeftPath + "\\" + s.Path + (s.IsFolder ? "\\" : ""), read_only: true);
				if (!s.IsFolder)
				{
					SelectedFile_Save.IsEnabled = true;
				}
			}
			DirTree.ScrollIntoView(s);
		}
		else if (!lastSelectedFile.IsSelected)
		{
			lastSelectedFile = null;
		}
	}

	private void updateEditorSearch()
	{
		if (currentEditor != null)
		{
			currentEditor.Search(new SearchSpec(FileSearch.Text));
		}
	}

	private void SelectedFile_Open_Click(object sender, RoutedEventArgs e)
	{
		if (currentEditor != null)
		{
			currentEditor.Save();
		}
		DirectoryTree n = DirTree.SelectedItem as DirectoryTree;
		string l = watcher[0].Path + "\\" + n.Path;
		string r = watcher[1].Path + "\\" + n.Path;
		if (!n.RightPresent)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(r));
			Process.Start(WinMergeEXE, "/u /wl /s \"" + l + "\" \"" + l + "\" \"" + r + "\"");
		}
		else if (!n.LeftPresent)
		{
			l = "Not Present in Spore";
			Process.Start(WinMergeEXE, "/u /wl /s \"" + l + "\" \"" + r + "\"");
		}
		else
		{
			Process.Start(WinMergeEXE, "/u /wl /s \"" + l + "\" \"" + r + "\"");
		}
	}

	private void SelectedFile_Save_Click(object sender, RoutedEventArgs e)
	{
		if (currentEditor != null)
		{
			currentEditor.Save();
		}
		DirectoryTree n = DirTree.SelectedItem as DirectoryTree;
		if (n.LeftPresent && !n.RightPresent && !n.IsFolder)
		{
			string l = watcher[0].Path + "\\" + n.Path;
			string r = watcher[1].Path + "\\" + n.Path;
			Directory.CreateDirectory(Path.GetDirectoryName(r));
			File.Copy(l, r);
			editDocument(r, read_only: false);
		}
	}

	private void SelectedFile_Erase_Click(object sender, RoutedEventArgs e)
	{
		DirectoryTree n = DirTree.SelectedItem as DirectoryTree;
		string r = watcher[1].Path + "\\" + n.Path;
		if (MessageBox.Show(Window, "Are you sure you want to delete " + r + "?", "Delete", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
		{
			return;
		}
		if (n.IsFolder)
		{
			Directory.Delete(r, recursive: true);
			return;
		}
		File.Delete(r);
		if (File.Exists(r + ".search_index"))
		{
			File.Delete(r + ".search_index");
		}
	}

	private void ShowRightOnly_Changed(object sender, RoutedEventArgs e)
	{
		update();
	}

	private void SelectedFile_Explorer_Click(object sender, RoutedEventArgs e)
	{
		if (currentEditor != null)
		{
			currentEditor.Save();
		}
		DirectoryTree n = DirTree.SelectedItem as DirectoryTree;
		int side = ((sender != SelectedFile_ExploreLeft) ? 1 : 0);
		string p = watcher[side].Path + "\\" + n.Path;
		if (!n.IsFolder)
		{
			p = Path.GetDirectoryName(p);
		}
		Process.Start(p);
	}

	private void DirTree_KeyDown(object sender, KeyEventArgs e)
	{
		DirectoryTree n = DirTree.SelectedItem as DirectoryTree;
		if (e.Key == Key.Up)
		{
			e.Handled = true;
			if (DirTree.SelectedIndex > 0)
			{
				DirTree.SelectedIndex -= 1;
			}
		}
		if (e.Key == Key.Down)
		{
			e.Handled = true;
			DirTree.SelectedIndex += 1;
		}
		if (e.Key == Key.Right)
		{
			if (n != null && n.IsFolder && !n.IsExpanded)
			{
				n.IsExpanded = true;
			}
			e.Handled = true;
		}
		if (e.Key != Key.Left)
		{
			return;
		}
		if (n != null)
		{
			if (n.IsExpanded)
			{
				n.IsExpanded = false;
			}
			else if (n.Parent != null)
			{
				if (n.Parent.Parent != null)
				{
					n.Parent.IsExpanded = false;
				}
				DirTree.SelectedItem = n.Parent;
			}
		}
		e.Handled = true;
	}

	private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		update();
		if (currentEditor != null)
		{
			currentEditor.Save();
		}
	}

	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocater = new Uri("/SporeMaster;component/fileseditor.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocater);
		}
	}

	[DebuggerNonUserCode]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}
}
