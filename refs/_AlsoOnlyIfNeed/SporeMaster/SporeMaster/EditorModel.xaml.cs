using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Media3D;
using SporeMaster.RenderWare4;

namespace SporeMaster;

public partial class EditorModel : UserControl, IEditor, IComponentConnector
{
	public EditorModel()
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
		Errors.Text = "";
		meshMain.TriangleIndices.Clear();
		meshMain.Positions.Clear();
		meshMain.Normals.Clear();
		wireframe.Points.Clear();
		if (filename == null)
		{
			return;
		}
		try
		{
			OgreXmlReader imp = new OgreXmlReader(filename + "model.mesh.xml");
			foreach (Vertex v in imp.vertices)
			{
				meshMain.Positions.Add(new Point3D(v.x, v.y, v.z));
				meshMain.Normals.Add(new Vector3D(Vertex.UnpackNormal(v.normal, 0), Vertex.UnpackNormal(v.normal, 1), Vertex.UnpackNormal(v.normal, 2)));
			}
			foreach (Triangle t in imp.triangles)
			{
				meshMain.TriangleIndices.Add((int)t.i);
				meshMain.TriangleIndices.Add((int)t.j);
				meshMain.TriangleIndices.Add((int)t.k);
			}
			wireframe.MakeWireframe(Model);
		}
		catch (Exception ex)
		{
			Errors.Text = ex.ToString();
		}
	}

	public void Save()
	{
	}

	public void Search(SearchSpec search_string)
	{
	}

	public string GetSelectedText()
	{
		return "";
	}

	private void OnViewportMouseUp(object sender, MouseButtonEventArgs e)
	{
	}

	private void OnViewportMouseDown(object sender, MouseButtonEventArgs e)
	{
	}

	private void OnViewportMouseMove(object sender, MouseEventArgs e)
	{
	}

	private void Zoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		double s = 0.0 - Math.Pow(2.0, 0.0 - Zoom.Value);
		camMain.Position = new Point3D(camMain.LookDirection.X * s, camMain.LookDirection.Y * s, camMain.LookDirection.Z * s);
	}

	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocater = new Uri("/SporeMaster;component/editormodel.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocater);
		}
	}
}
