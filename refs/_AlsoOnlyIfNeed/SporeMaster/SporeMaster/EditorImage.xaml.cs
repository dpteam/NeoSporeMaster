using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace SporeMaster;

public partial class EditorImage : UserControl, IEditor, IComponentConnector
{
	public EditorImage()
	{
		InitializeComponent();
		if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
		{
			Width = double.NaN;
			Height = double.NaN;
		}
	}

	public void Open(string filename, bool read_only)
	{
		byte[] data = File.ReadAllBytes(filename);
		BitmapImage bitmap = new BitmapImage();
		bitmap.BeginInit();
		bitmap.StreamSource = new MemoryStream(data);
		bitmap.EndInit();
		image.Source = bitmap;
	}

	public void Save()
	{
	}

	public void Search(SearchSpec spec)
	{
	}

	public string GetSelectedText()
	{
		return "";
	}

	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocater = new Uri("/SporeMaster;component/editorimage.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocater);
		}
	}
}
