using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace _3DTools;

public partial class Trackport3D : UserControl, IComponentConnector
{
	private Trackball _trackball = new Trackball();

	private readonly ScreenSpaceLines3D Wireframe = new ScreenSpaceLines3D();

	private ViewMode _viewMode;

	private Model3D _model;

	public Color HeadlightColor
	{
		get
		{
			return Headlight.Color;
		}
		set
		{
			Headlight.Color = value;
		}
	}

	public Color AmbientLightColor
	{
		get
		{
			return AmbientLight.Color;
		}
		set
		{
			AmbientLight.Color = value;
		}
	}

	public ViewMode ViewMode
	{
		get
		{
			return _viewMode;
		}
		set
		{
			_viewMode = value;
			SetupScene();
		}
	}

	public Trackport3D()
	{
		InitializeComponent();
		Viewport.Children.Add(Wireframe);
		Camera.Transform = _trackball.Transform;
		Headlight.Transform = _trackball.Transform;
	}

	public void LoadModel(Stream fileStream)
	{
		_model = (Model3D)XamlReader.Load(fileStream);
		SetupScene();
	}

	private void SetupScene()
	{
		switch (ViewMode)
		{
		case ViewMode.Solid:
			Root.Content = _model;
			Wireframe.Points.Clear();
			break;
		case ViewMode.Wireframe:
			Root.Content = null;
			Wireframe.MakeWireframe(_model);
			break;
		}
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		_trackball.EventSource = CaptureBorder;
	}

	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocater = new Uri("/3DTools;component/trackport3d.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocater);
		}
	}
}
