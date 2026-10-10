using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace _3DTools;

public class ScreenSpaceLines3D : ModelVisual3D
{
	public static readonly DependencyProperty ColorProperty = DependencyProperty.Register("Color", typeof(Color), typeof(ScreenSpaceLines3D), new PropertyMetadata(Colors.White, OnColorChanged));

	public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register("Thickness", typeof(double), typeof(ScreenSpaceLines3D), new PropertyMetadata(1.0, OnThicknessChanged));

	public static readonly DependencyProperty PointsProperty = DependencyProperty.Register("Points", typeof(Point3DCollection), typeof(ScreenSpaceLines3D), new PropertyMetadata(null, OnPointsChanged));

	private Matrix3D _visualToScreen;

	private Matrix3D _screenToVisual;

	private readonly GeometryModel3D _model;

	private readonly MeshGeometry3D _mesh;

	public Color Color
	{
		get
		{
			return (Color)GetValue(ColorProperty);
		}
		set
		{
			SetValue(ColorProperty, value);
		}
	}

	public double Thickness
	{
		get
		{
			return (double)GetValue(ThicknessProperty);
		}
		set
		{
			SetValue(ThicknessProperty, value);
		}
	}

	public Point3DCollection Points
	{
		get
		{
			return (Point3DCollection)GetValue(PointsProperty);
		}
		set
		{
			SetValue(PointsProperty, value);
		}
	}

	public ScreenSpaceLines3D()
	{
		_mesh = new MeshGeometry3D();
		_model = new GeometryModel3D();
		_model.Geometry = _mesh;
		SetColor(Color);
		Content = _model;
		Points = new Point3DCollection();
		CompositionTarget.Rendering += OnRender;
	}

	private static void OnColorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		((ScreenSpaceLines3D)sender).SetColor((Color)args.NewValue);
	}

	private void SetColor(Color color)
	{
		MaterialGroup unlitMaterial = new MaterialGroup();
		unlitMaterial.Children.Add(new DiffuseMaterial(new SolidColorBrush(Colors.Black)));
		unlitMaterial.Children.Add(new EmissiveMaterial(new SolidColorBrush(color)));
		unlitMaterial.Freeze();
		_model.Material = unlitMaterial;
		_model.BackMaterial = unlitMaterial;
	}

	private static void OnThicknessChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		((ScreenSpaceLines3D)sender).GeometryDirty();
	}

	private static void OnPointsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		((ScreenSpaceLines3D)sender).GeometryDirty();
	}

	private void OnRender(object sender, EventArgs e)
	{
		if ((Points.Count != 0 || _mesh.Positions.Count != 0) && UpdateTransforms())
		{
			RebuildGeometry();
		}
	}

	private void GeometryDirty()
	{
		_visualToScreen = MathUtils.ZeroMatrix;
	}

	private void RebuildGeometry()
	{
		double halfThickness = Thickness / 2.0;
		int numLines = Points.Count / 2;
		Point3DCollection positions = new Point3DCollection(numLines * 4);
		for (int i = 0; i < numLines; i++)
		{
			int startIndex = i * 2;
			Point3D startPoint = Points[startIndex];
			Point3D endPoint = Points[startIndex + 1];
			AddSegment(positions, startPoint, endPoint, halfThickness);
		}
		positions.Freeze();
		_mesh.Positions = positions;
		Int32Collection indices = new Int32Collection(Points.Count * 3);
		for (int j = 0; j < Points.Count / 2; j++)
		{
			indices.Add(j * 4 + 2);
			indices.Add(j * 4 + 1);
			indices.Add(j * 4);
			indices.Add(j * 4 + 2);
			indices.Add(j * 4 + 3);
			indices.Add(j * 4 + 1);
		}
		indices.Freeze();
		_mesh.TriangleIndices = indices;
	}

	private void AddSegment(Point3DCollection positions, Point3D startPoint, Point3D endPoint, double halfThickness)
	{
		Vector3D lineDirection = endPoint * _visualToScreen - startPoint * _visualToScreen;
		lineDirection.Z = 0.0;
		lineDirection.Normalize();
		Vector delta = new Vector(0.0 - lineDirection.Y, lineDirection.X);
		delta *= halfThickness;
		Widen(startPoint, delta, out var pOut1, out var pOut2);
		positions.Add(pOut1);
		positions.Add(pOut2);
		Widen(endPoint, delta, out pOut1, out pOut2);
		positions.Add(pOut1);
		positions.Add(pOut2);
	}

	private void Widen(Point3D pIn, Vector delta, out Point3D pOut1, out Point3D pOut2)
	{
		Point4D pIn4 = (Point4D)pIn;
		Point4D pOut41 = pIn4 * _visualToScreen;
		Point4D pOut42 = pOut41;
		pOut41.X += delta.X * pOut41.W;
		pOut41.Y += delta.Y * pOut41.W;
		pOut42.X -= delta.X * pOut42.W;
		pOut42.Y -= delta.Y * pOut42.W;
		pOut41 *= _screenToVisual;
		pOut42 *= _screenToVisual;
		pOut1 = new Point3D(pOut41.X / pOut41.W, pOut41.Y / pOut41.W, pOut41.Z / pOut41.W);
		pOut2 = new Point3D(pOut42.X / pOut42.W, pOut42.Y / pOut42.W, pOut42.Z / pOut42.W);
	}

	private bool UpdateTransforms()
	{
		Matrix3D visualToScreen = MathUtils.TryTransformTo2DAncestor(this, out var _, out var success);
		if (!success || !visualToScreen.HasInverse)
		{
			_mesh.Positions = null;
			return false;
		}
		if (visualToScreen == _visualToScreen)
		{
			return false;
		}
		_visualToScreen = (_screenToVisual = visualToScreen);
		_screenToVisual.Invert();
		return true;
	}

	public void MakeWireframe(Model3D model)
	{
		Points.Clear();
		if (model != null)
		{
			Matrix3DStack transform = new Matrix3DStack();
			transform.Push(Matrix3D.Identity);
			WireframeHelper(model, transform);
		}
	}

	private void WireframeHelper(Model3D model, Matrix3DStack matrixStack)
	{
		Transform3D transform = model.Transform;
		if (transform != null && transform != Transform3D.Identity)
		{
			matrixStack.Prepend(model.Transform.Value);
		}
		try
		{
			if (model is Model3DGroup group)
			{
				WireframeHelper(group, matrixStack);
			}
			else if (model is GeometryModel3D geometry)
			{
				WireframeHelper(geometry, matrixStack);
			}
		}
		finally
		{
			if (transform != null && transform != Transform3D.Identity)
			{
				matrixStack.Pop();
			}
		}
	}

	private void WireframeHelper(Model3DGroup group, Matrix3DStack matrixStack)
	{
		foreach (Model3D child in group.Children)
		{
			WireframeHelper(child, matrixStack);
		}
	}

	private void WireframeHelper(GeometryModel3D model, Matrix3DStack matrixStack)
	{
		Geometry3D geometry = model.Geometry;
		if (!(geometry is MeshGeometry3D mesh))
		{
			return;
		}
		Point3D[] positions = new Point3D[mesh.Positions.Count];
		mesh.Positions.CopyTo(positions, 0);
		matrixStack.Peek().Transform(positions);
		Int32Collection indices = mesh.TriangleIndices;
		if (indices.Count > 0)
		{
			int limit = positions.Length - 1;
			int i = 2;
			for (int count = indices.Count; i < count; i += 3)
			{
				int i2 = indices[i - 2];
				int i3 = indices[i - 1];
				int i4 = indices[i];
				if (0 > i2 || i2 > limit || 0 > i3 || i3 > limit || 0 > i4 || i4 > limit)
				{
					break;
				}
				AddTriangle(positions, i2, i3, i4);
			}
		}
		else
		{
			int j = 2;
			for (int count2 = positions.Length; j < count2; j += 3)
			{
				int i5 = j - 2;
				int i6 = j - 1;
				int i7 = j;
				AddTriangle(positions, i5, i6, i7);
			}
		}
	}

	private void AddTriangle(Point3D[] positions, int i0, int i1, int i2)
	{
		Points.Add(positions[i0]);
		Points.Add(positions[i1]);
		Points.Add(positions[i1]);
		Points.Add(positions[i2]);
		Points.Add(positions[i2]);
		Points.Add(positions[i0]);
	}
}
