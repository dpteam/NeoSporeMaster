using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Gibbed.Spore.Helpers;

namespace SporeMaster;

public partial class NameEditor : UserControl, IComponentConnector
{
	private Brush LightGreen = new SolidColorBrush(new Color
	{
		R = 216,
		G = byte.MaxValue,
		B = 216,
		A = byte.MaxValue
	});

	private HashGuesser guesser { get; set; }

	public NameEditor()
	{
		InitializeComponent();
		if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
		{
			guesser = new HashGuesser();
			Width = double.NaN;
			Height = double.NaN;
			GuessPanel.DataContext = guesser;
		}
	}

	public void EditName(string name, uint? hash)
	{
		AliasName.Text = ((name != null) ? name : "");
		AliasHash.Text = (hash.HasValue ? hash.Value.ToString("X8") : "");
	}

	private void AliasName_TextChanged(object sender, TextChangedEventArgs e)
	{
		update();
	}

	private uint parseHash(string hash, out bool okSoFar, out bool done)
	{
		bool parses = uint.TryParse(hash, NumberStyles.AllowHexSpecifier, null, out var h);
		okSoFar = hash == "" || parses;
		done = parses && hash.Length == 8;
		return h;
	}

	private void AliasHash_TextChanged(object sender, TextChangedEventArgs e)
	{
		GuessHash.Text = AliasHash.Text;
		parseHash(AliasHash.Text, out var okSoFar, out var done);
		TextBox aliasHash = AliasHash;
		Brush background;
		if (done)
		{
			background = LightGreen;
		}
		else
		{
			background = (okSoFar ? Brushes.White : Brushes.Yellow);
		}
		aliasHash.Background = background;
		update();
	}

	private void update()
	{
		if (guesser == null)
		{
			return;
		}
		string name = AliasName.Text;
		uint hash = name.FNV();
		string altname = NameRegistry.Files.toName(hash);
		SaveName.IsEnabled = name.Length > 0;
		if (name.EndsWith("~"))
		{
			if (NameRegistry.Files.checkName(name, out hash))
			{
				AliasNameStatus.Text = "Known alias.";
			}
			else
			{
				AliasNameStatus.Text = "Unknown alias.";
			}
			SaveName.IsEnabled = false;
		}
		else if (altname == name)
		{
			AliasNameStatus.Text = "Already known.";
		}
		else if (!altname.StartsWith("#"))
		{
			AliasNameStatus.Text = "Already defined as '" + altname + "'.";
		}
		else if (IsHashUsed(hash))
		{
			AliasNameStatus.Inlines.Clear();
			AliasNameStatus.Inlines.Add(AliasNameStatusLink);
			AliasNameStatusLink.Inlines.Clear();
			AliasNameStatusLink.Inlines.Add("Used by Spore!");
		}
		else
		{
			AliasNameStatus.Text = "Not used by Spore.";
		}
		AliasNameHashesTo.Text = hash.ToString("X8");
		if (!HashColumn.IsEnabled)
		{
			return;
		}
		if (AliasHash.Text.Length != 8 || !uint.TryParse(AliasHash.Text, NumberStyles.AllowHexSpecifier, null, out hash))
		{
			AliasHashStatus.Text = "Not a valid hash!";
			SaveAlias.IsEnabled = false;
			return;
		}
		SaveAlias.IsEnabled = AliasName.Text.EndsWith("~") && !NameRegistry.Files.checkName(AliasName.Text, out var _);
		altname = NameRegistry.Files.toName(hash);
		if (!altname.StartsWith("#"))
		{
			AliasHashStatus.Text = "Already defined as '" + altname + "'.";
			if (altname.FNV() == hash)
			{
				SaveAlias.IsEnabled = false;
			}
		}
		else if (IsHashUsed(hash))
		{
			AliasHashStatus.Inlines.Clear();
			AliasHashStatus.Inlines.Add(AliasHashStatusLink);
			AliasHashStatusLink.Inlines.Clear();
			AliasHashStatusLink.Inlines.Add("Used by Spore!");
		}
		else
		{
			AliasHashStatus.Text = "Not used by Spore.";
		}
		if (hash == AliasName.Text.FNV())
		{
			SaveAlias.IsEnabled = false;
		}
	}

	private bool IsHashUsed(uint hash)
	{
		return DirectoryTree.Tree.Search(new SearchSpec("#" + hash.ToString("X8")));
	}

	private void AliasNameStatusLink_Click(object sender, RoutedEventArgs e)
	{
		MainWindow.Instance.SearchFiles(AliasName.Text.FNV().ToString("X8"));
	}

	private void AliasHashStatusLink_Click(object sender, RoutedEventArgs e)
	{
		if (uint.TryParse(AliasHash.Text, NumberStyles.AllowHexSpecifier, null, out var hash))
		{
			MainWindow.Instance.SearchFiles(hash.ToString("X8"));
		}
	}

	private void SaveName_Click(object sender, RoutedEventArgs e)
	{
		uint hash = AliasName.Text.FNV();
		string oldName = NameRegistry.Files.toName(hash);
		NameRegistry.Files.addName(AliasName.Text, hash, overrideExisting: true);
		NameRegistry.Files.save();
		rename(new uint[1] { hash }, new string[1] { oldName });
		update();
	}

	private void SaveAlias_Click(object sender, RoutedEventArgs e)
	{
		if (uint.TryParse(AliasHash.Text, NumberStyles.AllowHexSpecifier, null, out var hash) && AliasName.Text.EndsWith("~"))
		{
			string oldName = NameRegistry.Files.toName(hash);
			NameRegistry.Files.addName(AliasName.Text, hash, overrideExisting: true);
			NameRegistry.Files.save();
			rename(new uint[1] { hash }, new string[1] { oldName });
			update();
		}
	}

	private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (IsVisible)
		{
			update();
		}
	}

	private void rename(uint[] hashes, string[] oldName)
	{
		new PleaseWait(MainWindow.Instance, "Renaming", (PleaseWait progress) =>
		{
			Renamer.rename(hashes, oldName, progress);
		}).ShowDialog();
	}

	private void GuessHash_TextChanged(object sender, TextChangedEventArgs e)
	{
		GuessHash.Text = AliasHash.Text;
		uint h = parseHash(GuessHash.Text, out var okSoFar, out var done);
		if (done)
		{
			guesser.HashCode = h;
		}
		else
		{
			guesser.HashCode = null;
		}
		TextBox guessHash = GuessHash;
		Brush background;
		if (done)
		{
			background = LightGreen;
		}
		else
		{
			background = (okSoFar ? Brushes.White : Brushes.Yellow);
		}
		guessHash.Background = background;
	}

	private void GuessResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (GuessResults.SelectedItem is HashGuesser.Result r)
		{
			AliasName.Text = r.Text;
		}
	}

	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocater = new Uri("/SporeMaster;component/nameeditor.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocater);
		}
	}
}
