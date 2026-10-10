using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml;
using Microsoft.Win32;

namespace SporeMaster;

public partial class MainWindow : Window, IComponentConnector
{
	private const string ConfigFile = "SporeMaster Configuration.xml";

	public static MainWindow Instance;

	public string LeftPath = "spore.unpacked";

	public string RightPath;

	private string UnpackedFolderFormat = "{0}{1}.unpacked";

	public MainWindow()
	{
		Instance = this;
		InitializeComponent();
		loadConfiguration();
		Registry.SetValue("HKEY_CURRENT_USER\\Software\\Thingamahoochie\\WinMerge\\Backup", "EnableFile", 0u, RegistryValueKind.DWord);
		if (Directory.Exists(LeftPath))
		{
			tabControl.SelectedIndex = 1;
		}
		new PleaseWait(null, "Updating Index...", (PleaseWait progress) =>
		{
			NameRegistry.Files = (NameRegistry.Groups = new NameRegistry("reg_file.txt"));
			NameRegistry.Types = new NameRegistry("reg_type.txt");
			NameRegistry.Properties = new NameRegistry("reg_property.txt");
			if (!File.Exists("Not present in Spore"))
			{
				File.WriteAllText("Not Present in Spore", "");
				File.SetAttributes("Not Present in Spore", FileAttributes.ReadOnly);
			}
			FilesEditor.SetPath(0, LeftPath, progress);
		}).ShowDialog();
	}

	private void loadConfiguration()
	{
		if (!File.Exists("SporeMaster Configuration.xml"))
		{
			Directory.SetCurrentDirectory(Path.GetDirectoryName(typeof(MainWindow).Assembly.Location));
		}
		using XmlReader cfg = XmlReader.Create("SporeMaster Configuration.xml");
		cfg.ReadToFollowing("SporeMasterConfig");
		while (cfg.Read())
		{
			if (!cfg.IsStartElement())
			{
				continue;
			}
			if (cfg.Name == "Packages")
			{
				PackageList.Items.Clear();
				XmlReader subtree = cfg.ReadSubtree();
				while (subtree.Read())
				{
					if (subtree.IsStartElement() && subtree.Name == "Package")
					{
						PackageList.Items.Add(makeListItem(subtree.ReadString()));
					}
				}
			}
			else if (cfg.Name == "FullTextIndexExtensions")
			{
				FilesEditor.fullTextExtensions = new HashSet<string>();
				XmlReader subtree2 = cfg.ReadSubtree();
				while (subtree2.Read())
				{
					if (subtree2.IsStartElement() && subtree2.Name == "Extension")
					{
						FilesEditor.fullTextExtensions.Add(subtree2.ReadString());
					}
				}
			}
			else if (cfg.Name == "WinMerge")
			{
				FilesEditor.WinMergeEXE = cfg.ReadString();
			}
			else if (cfg.Name == "UnpackedFolderFormat")
			{
				UnpackedFolderFormat = cfg.ReadString();
			}
		}
	}

	private void saveConfiguration()
	{
		using XmlTextWriter cfg = new XmlTextWriter("SporeMaster Configuration.xml", Encoding.UTF8);
		cfg.Formatting = Formatting.Indented;
		cfg.WriteStartDocument();
		cfg.WriteStartElement("SporeMasterConfig");
		cfg.WriteStartElement("Packages");
		foreach (object p in (IEnumerable)PackageList.Items)
		{
			cfg.WriteElementString("Package", (p as ListBoxItem).Content as string);
		}
		cfg.WriteEndElement();
		cfg.WriteStartElement("FullTextIndexExtensions");
		foreach (string ext in FilesEditor.fullTextExtensions)
		{
			cfg.WriteElementString("Extension", ext);
		}
		cfg.WriteEndElement();
		cfg.WriteElementString("WinMerge", FilesEditor.WinMergeEXE);
		cfg.WriteElementString("UnpackedFolderFormat", UnpackedFolderFormat);
		cfg.WriteEndElement();
		cfg.WriteEndDocument();
	}

	public void SearchFiles(string search)
	{
		TabFilesEditor.IsSelected = true;
		FilesEditor.SearchFiles(search);
	}

	private void PackageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		Button btn_EditPackage = Btn_EditPackage;
		bool isEnabled = (Btn_RemovePackage.IsEnabled = PackageList.SelectedIndex != -1);
		btn_EditPackage.IsEnabled = isEnabled;
		Btn_PackageUp.IsEnabled = PackageList.SelectedIndex > 0;
		Btn_PackageDown.IsEnabled = PackageList.SelectedIndex != -1 && PackageList.SelectedIndex < PackageList.Items.Count - 1;
	}

	private void Btn_NewPackage_Click(object sender, RoutedEventArgs e)
	{
		OpenFileDialog open = new OpenFileDialog();
		open.Filter = "Spore packages|*.package";
		open.Multiselect = true;
		open.RestoreDirectory = true;
		if (open.ShowDialog(this) == true)
		{
			string[] fileNames = open.FileNames;
			foreach (string f in fileNames)
			{
				PackageList.Items.Add(makeListItem(f));
			}
		}
		saveConfiguration();
	}

	private void Btn_EditPackage_Click(object sender, RoutedEventArgs e)
	{
		int i = PackageList.SelectedIndex;
		OpenFileDialog open = new OpenFileDialog();
		open.Filter = "Spore packages|*.package";
		open.FileName = PackageList.SelectedItem as string;
		open.RestoreDirectory = true;
		if (open.ShowDialog(this) == true)
		{
			PackageList.Items.RemoveAt(i);
			PackageList.Items.Insert(i, makeListItem(open.FileName));
			PackageList.SelectedIndex = i;
			PackageList.Focus();
		}
		saveConfiguration();
	}

	private void Btn_RemovePackage_Click(object sender, RoutedEventArgs e)
	{
		PackageList.Items.Remove(PackageList.SelectedItem);
		saveConfiguration();
	}

	private void Btn_PackageUp_Click(object sender, RoutedEventArgs e)
	{
		int i = PackageList.SelectedIndex;
		string f = (PackageList.Items[i] as ListViewItem).Content as string;
		PackageList.Items.RemoveAt(i);
		PackageList.Items.Insert(i - 1, makeListItem(f));
		PackageList.SelectedIndex = i - 1;
		PackageList.Focus();
		saveConfiguration();
	}

	private void Btn_PackageDown_Click(object sender, RoutedEventArgs e)
	{
		int i = PackageList.SelectedIndex;
		string f = (PackageList.Items[i] as ListViewItem).Content as string;
		PackageList.Items.RemoveAt(i);
		PackageList.Items.Insert(i + 1, makeListItem(f));
		PackageList.SelectedIndex = i + 1;
		PackageList.Focus();
		saveConfiguration();
	}

	private ListViewItem makeListItem(string filename)
	{
		ListViewItem l = new ListViewItem();
		l.Content = filename;
		return l;
	}

	private void Btn_Unpack_Click(object sender, RoutedEventArgs e)
	{
		string[] filenames = (from ListViewItem item in PackageList.Items
			select item.Content as string).ToArray();
		new PleaseWait(null, "Unpacking", (PleaseWait progress) =>
		{
			try
			{
				FilesEditor.SetPath(0, null, progress);
				Directory.CreateDirectory(LeftPath);
				Stream[] packageFiles = ((IEnumerable<string>)filenames).Select((Func<string, Stream>)((string f) => File.OpenRead(f))).ToArray();
				progress.beginTask(0.3, 1.0);
				new PackageUnpack(packageFiles, LeftPath, progress);
				progress.endTask();
				progress.beginTask(0.7, 1.0);
				FilesEditor.SetPath(0, LeftPath, progress);
				progress.endTask();
			}
			catch (Exception ex)
			{
				progress.complete();
				MessageBox.Show(ex.ToString(), "Error unpacking packages");
			}
		}).ShowDialog();
	}

	private void NewMod_Click(object sender, RoutedEventArgs e)
	{
		SaveFileDialog open = new SaveFileDialog();
		open.Filter = "Spore packages|*.package";
		open.RestoreDirectory = true;
		if (open.ShowDialog(this) == true)
		{
			ModPath.Text = open.FileName;
			Directory.CreateDirectory(buildRightPath(ModPath.Text));
			checkModReady();
		}
	}

	private void OpenMod_Click(object sender, RoutedEventArgs e)
	{
		OpenFileDialog open = new OpenFileDialog();
		open.Filter = "Spore packages|*.package";
		open.FileName = ModPath.Text;
		open.RestoreDirectory = true;
		if (open.ShowDialog(this) == true)
		{
			ModPath.Text = open.FileName;
			checkModReady();
		}
	}

	private void Pack_Mod_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			string mod = ModPath.Text;
			if (!File.Exists(mod) || MessageBox.Show("This will overwrite\n  '" + ModPath.Text + "'\nwith your changes.", "Warning", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
			{
				new PleaseWait(null, "Packing Mod", (PleaseWait progress) =>
				{
					FileStream output = File.Create(mod);
					new PackagePack(output, buildRightPath(mod), progress);
				}).ShowDialog();
				checkModReady();
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.ToString(), "Error packing mod");
		}
	}

	private void Unpack_Mod_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			string path = buildRightPath(ModPath.Text);
			if (!Directory.Exists(path) || MessageBox.Show("This will overwrite any changes you have made\nsince the last time you packed this mod!\n\nAre you sure?", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
			{
				FilesEditor.SetPath(1, null, null);
				FileStream stream = File.OpenRead(ModPath.Text);
				new PackageUnpack(new Stream[1] { stream }, path, null);
				checkModReady();
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.ToString(), "Error unpacking mod");
		}
	}

	private string buildRightPath(string modPath)
	{
		string dirname = Path.GetDirectoryName(modPath);
		if (dirname != "")
		{
			dirname += "\\";
		}
		return string.Format(UnpackedFolderFormat, dirname, Path.GetFileName(modPath));
	}

	private void checkModReady()
	{
		FilesEditor.SetPath(1, null, null);
		if (RightPath != null)
		{
			FullTextIndex.ResetPath(RightPath);
		}
		if (ModPath.Text != "")
		{
			RightPath = buildRightPath(ModPath.Text);
			Unpack_Mod.IsEnabled = File.Exists(ModPath.Text);
			if (Directory.Exists(RightPath))
			{
				FilesEditor.SetPath(1, RightPath, null);
				TabFilesEditor.IsEnabled = true;
				Pack_Mod.IsEnabled = true;
				Unpack_Mod_Doc.Text = "You have unpacked this mod.  Edit it in the Files tab.";
				Pack_Mod_Doc.Text = "You must pack the mod to see changes in Spore.";
			}
			else
			{
				TabFilesEditor.IsEnabled = false;
				Pack_Mod.IsEnabled = false;
				Unpack_Mod_Doc.Text = "You must unpack this mod before editing it.";
				Pack_Mod_Doc.Text = "";
			}
		}
		else
		{
			Unpack_Mod.IsEnabled = false;
			Pack_Mod.IsEnabled = false;
			Unpack_Mod_Doc.Text = "";
			Pack_Mod_Doc.Text = "";
			RightPath = null;
		}
	}

	private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.OriginalSource == tabControl)
		{
			int selectedIndex = tabControl.SelectedIndex;
			if (selectedIndex == 3)
			{
				Tab_NameEditor_init();
			}
		}
	}

	private void Tab_NameEditor_init()
	{
		string text = FilesEditor.GetEditorSelection();
		if (text != null)
		{
			uint hash;
			if (text.StartsWith("#"))
			{
				if (uint.TryParse(text.Substring(1), NumberStyles.AllowHexSpecifier, null, out hash))
				{
					NameEditor.EditName(null, hash);
					return;
				}
			}
			else
			{
				if (uint.TryParse(text, out hash) && hash != 0)
				{
					NameEditor.EditName(null, hash);
					return;
				}
				if (text.Length == 8 && uint.TryParse(text, NumberStyles.AllowHexSpecifier, null, out hash))
				{
					NameEditor.EditName(null, hash);
					return;
				}
				if (!text.Contains("\n") && !text.Contains(" "))
				{
					NameEditor.EditName(text, null);
					return;
				}
			}
		}
		if (FilesEditor.DirTree.SelectedItem is DirectoryTree { FullName: var fullName })
		{
			string s = fullName.Split(new char[1] { '.' }, 2)[0];
			if (s.StartsWith("#"))
			{
				NameEditor.EditName(null, NameRegistry.Files.toHash(s));
			}
			else
			{
				NameEditor.EditName(s, NameRegistry.Files.toHash(s));
			}
		}
	}

	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocater = new Uri("/SporeMaster;component/mainwindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocater);
		}
	}

	[DebuggerNonUserCode]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}
}
