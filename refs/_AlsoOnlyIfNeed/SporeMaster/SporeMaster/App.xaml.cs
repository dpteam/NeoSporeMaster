using System;
using System.Diagnostics;
using System.Windows;

namespace SporeMaster;

public partial class App : Application
{
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
			Uri resourceLocater = new Uri("/SporeMaster;component/app.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocater);
		}
	}

	[DebuggerNonUserCode]
	[STAThread]
	public static void Main()
	{
		App app = new App();
		app.InitializeComponent();
		app.Run();
	}
}
