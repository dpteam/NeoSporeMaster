using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace _3DTools;

public class InteractiveVisual3D : ModelVisual3D
{
	private struct Edge(Point3D s, Point3D e)
	{
		public Point3D _start = s;

		public Point3D _end = e;
	}

	private class EdgeInfo
	{
		public bool hasFrontFace;

		public bool hasBackFace;

		public Point uv1;

		public Point uv2;

		public int numSharing;

		public EdgeInfo()
		{
			hasFrontFace = (hasBackFace = false);
			numSharing = 0;
		}
	}

	private enum PolygonSide
	{
		FRONT,
		BACK
	}

	private static DependencyProperty VisualProperty;

	private static readonly DependencyProperty IsBackVisibleProperty;

	private static readonly DiffuseMaterial _defaultMaterialPropertyValue;

	public static readonly DependencyProperty MaterialProperty;

	public static readonly DependencyProperty GeometryProperty;

	public static readonly DependencyProperty IsInteractiveMaterialProperty;

	internal readonly GeometryModel3D _content;

	private Point[] _lastVisCorners;

	private List<HitTestEdge> _lastEdges;

	private Matrix3D _lastMatrix3D;

	private UIElement _internalVisual;

	private VisualBrush _visualBrush;

	public Visual Visual
	{
		get
		{
			return (Visual)GetValue(VisualProperty);
		}
		set
		{
			SetValue(VisualProperty, value);
		}
	}

	internal UIElement InternalVisual => _internalVisual;

	private VisualBrush InternalVisualBrush
	{
		get
		{
			return _visualBrush;
		}
		set
		{
			_visualBrush = value;
		}
	}

	public bool IsBackVisible
	{
		get
		{
			return (bool)GetValue(IsBackVisibleProperty);
		}
		set
		{
			SetValue(IsBackVisibleProperty, value);
		}
	}

	public Material Material
	{
		get
		{
			return (Material)GetValue(MaterialProperty);
		}
		set
		{
			SetValue(MaterialProperty, value);
		}
	}

	public Geometry3D Geometry
	{
		get
		{
			return (Geometry3D)GetValue(GeometryProperty);
		}
		set
		{
			SetValue(GeometryProperty, value);
		}
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public new Model3D Content
	{
		get
		{
			return base.Content;
		}
		set
		{
			base.Content = value;
		}
	}

	public InteractiveVisual3D()
	{
		InternalVisualBrush = CreateVisualBrush();
		_content = new GeometryModel3D();
		Content = _content;
		GenerateMaterial();
	}

	static InteractiveVisual3D()
	{
		VisualProperty = DependencyProperty.Register("Visual", typeof(Visual), typeof(InteractiveVisual3D), new PropertyMetadata(null, OnVisualChanged));
		IsBackVisibleProperty = DependencyProperty.Register("IsBackVisible", typeof(bool), typeof(InteractiveVisual3D), new PropertyMetadata(false, OnIsBackVisiblePropertyChanged));
		GeometryProperty = DependencyProperty.Register("Geometry", typeof(Geometry3D), typeof(InteractiveVisual3D), new PropertyMetadata(null, OnGeometryChanged));
		IsInteractiveMaterialProperty = DependencyProperty.RegisterAttached("IsInteractiveMaterial", typeof(bool), typeof(InteractiveVisual3D), new PropertyMetadata(false));
		_defaultMaterialPropertyValue = new DiffuseMaterial();
		_defaultMaterialPropertyValue.SetValue(IsInteractiveMaterialProperty, true);
		_defaultMaterialPropertyValue.Freeze();
		MaterialProperty = DependencyProperty.Register("Material", typeof(Material), typeof(InteractiveVisual3D), new PropertyMetadata(_defaultMaterialPropertyValue, OnMaterialPropertyChanged));
	}

	protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);
		_lastVisCorners = null;
	}

	internal List<HitTestEdge> GetVisualEdges(Point[] texCoordsOfInterest)
	{
		_lastEdges = GrabValidEdges(texCoordsOfInterest);
		_lastVisCorners = texCoordsOfInterest;
		return _lastEdges;
	}

	private List<HitTestEdge> GrabValidEdges(Point[] tc)
	{
		List<HitTestEdge> hitTestEdgeList = new List<HitTestEdge>();
		Dictionary<Edge, EdgeInfo> adjInformation = new Dictionary<Edge, EdgeInfo>();
		MeshGeometry3D contentGeom = (MeshGeometry3D)_content.Geometry;
		Point3DCollection positions = contentGeom.Positions;
		PointCollection textureCoords = contentGeom.TextureCoordinates;
		Int32Collection triIndices = contentGeom.TriangleIndices;
		Transform3D contentTransform = _content.Transform;
		Matrix3D cameraToObjectTransform = MathUtils.TryTransformToCameraSpace(this, out var containingVisual, out var success);
		if (!success)
		{
			return new List<HitTestEdge>();
		}
		if (contentTransform != null)
		{
			cameraToObjectTransform.Prepend(contentTransform.Value);
		}
		Matrix3D objectToViewportTransform = MathUtils.TryTransformTo2DAncestor(this, out containingVisual, out success);
		if (!success)
		{
			return new List<HitTestEdge>();
		}
		if (contentTransform != null)
		{
			objectToViewportTransform.Prepend(contentTransform.Value);
		}
		bool sameAsBefore = _lastVisCorners != null;
		if (_lastVisCorners != null)
		{
			for (int i = 0; i < tc.Length; i++)
			{
				if (tc[i] != _lastVisCorners[i])
				{
					sameAsBefore = false;
					break;
				}
			}
			if (_lastMatrix3D != objectToViewportTransform)
			{
				sameAsBefore = false;
			}
		}
		if (sameAsBefore)
		{
			return _lastEdges;
		}
		_lastMatrix3D = objectToViewportTransform;
		try
		{
			cameraToObjectTransform.Invert();
		}
		catch (InvalidOperationException)
		{
			return new List<HitTestEdge>();
		}
		Point3D camPosObjSpace = cameraToObjectTransform.Transform(new Point3D(0.0, 0.0, 0.0));
		Rect bbox = Rect.Empty;
		for (int j = 0; j < tc.Length; j++)
		{
			bbox.Union(tc[j]);
		}
		int[] indices = new int[3];
		Point3D[] p = new Point3D[3];
		Point[] uv = new Point[3];
		for (int k = 0; k < triIndices.Count; k += 3)
		{
			Rect triBBox = Rect.Empty;
			for (int l = 0; l < 3; l++)
			{
				indices[l] = triIndices[k + l];
				ref Point3D reference = ref p[l];
				reference = positions[indices[l]];
				ref Point reference2 = ref uv[l];
				reference2 = textureCoords[indices[l]];
				triBBox.Union(uv[l]);
			}
			if (bbox.IntersectsWith(triBBox))
			{
				ProcessTriangle(p, uv, tc, hitTestEdgeList, adjInformation, camPosObjSpace);
			}
		}
		foreach (Edge edge in adjInformation.Keys)
		{
			EdgeInfo ei = adjInformation[edge];
			if (ei.hasFrontFace && ei.numSharing == 1)
			{
				HandleSilhouetteEdge(ei.uv1, ei.uv2, edge._start, edge._end, tc, hitTestEdgeList);
			}
		}
		for (int m = 0; m < hitTestEdgeList.Count; m++)
		{
			hitTestEdgeList[m].Project(objectToViewportTransform);
		}
		return hitTestEdgeList;
	}

	private void ProcessTriangle(Point3D[] p, Point[] uv, Point[] tc, List<HitTestEdge> edgeList, Dictionary<Edge, EdgeInfo> adjInformation, Point3D camPosObjSpace)
	{
		Vector3D normal = Vector3D.CrossProduct(p[1] - p[0], p[2] - p[0]);
		Vector3D dirToCamera = camPosObjSpace - p[0];
		if (normal.X != 0.0 || normal.Y != 0.0 || normal.Z != 0.0)
		{
			double dotProd = Vector3D.DotProduct(normal, dirToCamera);
			if (dotProd > 0.0)
			{
				ProcessTriangleEdges(p, uv, tc, PolygonSide.FRONT, edgeList, adjInformation);
				ProcessVisualBoundsIntersections(p, uv, tc, edgeList);
			}
			else
			{
				ProcessTriangleEdges(p, uv, tc, PolygonSide.BACK, edgeList, adjInformation);
			}
		}
	}

	private void ProcessVisualBoundsIntersections(Point3D[] p, Point[] uv, Point[] tc, List<HitTestEdge> edgeList)
	{
		List<Point3D> pointList = new List<Point3D>();
		List<Point> uvList = new List<Point>();
		for (int i = 0; i < tc.Length; i++)
		{
			Point visEdgeStart = tc[i];
			Point visEdgeEnd = tc[(i + 1) % tc.Length];
			pointList.Clear();
			uvList.Clear();
			bool skipListProcessing = false;
			for (int j = 0; j < uv.Length; j++)
			{
				Point uv2 = uv[j];
				Point uv3 = uv[(j + 1) % uv.Length];
				Point3D p3D1 = p[j];
				Point3D p3D2 = p[(j + 1) % p.Length];
				if (Math.Max(visEdgeStart.X, visEdgeEnd.X) < Math.Min(uv2.X, uv3.X) || Math.Min(visEdgeStart.X, visEdgeEnd.X) > Math.Max(uv2.X, uv3.X) || Math.Max(visEdgeStart.Y, visEdgeEnd.Y) < Math.Min(uv2.Y, uv3.Y) || Math.Min(visEdgeStart.Y, visEdgeEnd.Y) > Math.Max(uv2.Y, uv3.Y))
				{
					continue;
				}
				bool areCoincident = false;
				Vector dir = uv3 - uv2;
				double t = IntersectRayLine(uv2, dir, visEdgeStart, visEdgeEnd, out areCoincident);
				if (areCoincident)
				{
					HandleCoincidentLines(visEdgeStart, visEdgeEnd, p3D1, p3D2, uv2, uv3, edgeList);
					skipListProcessing = true;
					break;
				}
				if (t >= 0.0 && t <= 1.0)
				{
					Point intersUV = uv2 + dir * t;
					Point3D intersPoint3D = p3D1 + (p3D2 - p3D1) * t;
					double visEdgeDiff = (visEdgeStart - visEdgeEnd).Length;
					if ((intersUV - visEdgeStart).Length < visEdgeDiff && (intersUV - visEdgeEnd).Length < visEdgeDiff)
					{
						pointList.Add(intersPoint3D);
						uvList.Add(intersUV);
					}
				}
			}
			if (skipListProcessing)
			{
				continue;
			}
			Point3D outputPoint2;
			Point3D outputPoint3;
			if (pointList.Count >= 2)
			{
				edgeList.Add(new HitTestEdge(pointList[0], pointList[1], uvList[0], uvList[1]));
			}
			else if (pointList.Count == 1)
			{
				if (IsPointInTriangle(visEdgeStart, uv, p, out var outputPoint))
				{
					edgeList.Add(new HitTestEdge(pointList[0], outputPoint, uvList[0], visEdgeStart));
				}
				if (IsPointInTriangle(visEdgeEnd, uv, p, out outputPoint))
				{
					edgeList.Add(new HitTestEdge(pointList[0], outputPoint, uvList[0], visEdgeEnd));
				}
			}
			else if (IsPointInTriangle(visEdgeStart, uv, p, out outputPoint2) && IsPointInTriangle(visEdgeEnd, uv, p, out outputPoint3))
			{
				edgeList.Add(new HitTestEdge(outputPoint2, outputPoint3, visEdgeStart, visEdgeEnd));
			}
		}
	}

	private bool IsPointInTriangle(Point p, Point[] triUVVertices, Point3D[] tri3DVertices, out Point3D inters3DPoint)
	{
		double denom = 0.0;
		inters3DPoint = default;
		double A = triUVVertices[0].X - triUVVertices[2].X;
		double B = triUVVertices[1].X - triUVVertices[2].X;
		double C = triUVVertices[2].X - p.X;
		double D = triUVVertices[0].Y - triUVVertices[2].Y;
		double E = triUVVertices[1].Y - triUVVertices[2].Y;
		double F = triUVVertices[2].Y - p.Y;
		denom = A * E - B * D;
		if (denom == 0.0)
		{
			return false;
		}
		double lambda1 = (B * F - C * E) / denom;
		denom = B * D - A * E;
		if (denom == 0.0)
		{
			return false;
		}
		double lambda2 = (A * F - C * D) / denom;
		if (lambda1 < 0.0 || lambda1 > 1.0 || lambda2 < 0.0 || lambda2 > 1.0 || lambda1 + lambda2 > 1.0)
		{
			return false;
		}
		inters3DPoint = (Point3D)(lambda1 * (Vector3D)tri3DVertices[0] + lambda2 * (Vector3D)tri3DVertices[1] + (1.0 - lambda1 - lambda2) * (Vector3D)tri3DVertices[2]);
		return true;
	}

	private void HandleCoincidentLines(Point visUV1, Point visUV2, Point3D tri3D1, Point3D tri3D2, Point triUV1, Point triUV2, List<HitTestEdge> edgeList)
	{
		Point uv1;
		Point3D p1;
		Point uv2;
		Point3D p2;
		if (Math.Abs(visUV1.X - visUV2.X) > Math.Abs(visUV1.Y - visUV2.Y))
		{
			Point minVisUV;
			Point maxVisUV;
			if (visUV1.X <= visUV2.X)
			{
				minVisUV = visUV1;
				maxVisUV = visUV2;
			}
			else
			{
				minVisUV = visUV2;
				maxVisUV = visUV1;
			}
			Point minTriUV;
			Point3D minTri3D;
			Point maxTriUV;
			Point3D maxTri3D;
			if (triUV1.X <= triUV2.X)
			{
				minTriUV = triUV1;
				minTri3D = tri3D1;
				maxTriUV = triUV2;
				maxTri3D = tri3D2;
			}
			else
			{
				minTriUV = triUV2;
				minTri3D = tri3D2;
				maxTriUV = triUV1;
				maxTri3D = tri3D1;
			}
			if (minVisUV.X < minTriUV.X)
			{
				uv1 = minTriUV;
				p1 = minTri3D;
			}
			else
			{
				uv1 = minVisUV;
				p1 = minTri3D + (minVisUV.X - minTriUV.X) / (maxTriUV.X - minTriUV.X) * (maxTri3D - minTri3D);
			}
			if (maxVisUV.X > maxTriUV.X)
			{
				uv2 = maxTriUV;
				p2 = maxTri3D;
			}
			else
			{
				uv2 = maxVisUV;
				p2 = minTri3D + (maxVisUV.X - minTriUV.X) / (maxTriUV.X - minTriUV.X) * (maxTri3D - minTri3D);
			}
		}
		else
		{
			Point minVisUV;
			Point maxVisUV;
			if (visUV1.Y <= visUV2.Y)
			{
				minVisUV = visUV1;
				maxVisUV = visUV2;
			}
			else
			{
				minVisUV = visUV2;
				maxVisUV = visUV1;
			}
			Point minTriUV;
			Point3D minTri3D;
			Point maxTriUV;
			Point3D maxTri3D;
			if (triUV1.Y <= triUV2.Y)
			{
				minTriUV = triUV1;
				minTri3D = tri3D1;
				maxTriUV = triUV2;
				maxTri3D = tri3D2;
			}
			else
			{
				minTriUV = triUV2;
				minTri3D = tri3D2;
				maxTriUV = triUV1;
				maxTri3D = tri3D1;
			}
			if (minVisUV.Y < minTriUV.Y)
			{
				uv1 = minTriUV;
				p1 = minTri3D;
			}
			else
			{
				uv1 = minVisUV;
				p1 = minTri3D + (minVisUV.Y - minTriUV.Y) / (maxTriUV.Y - minTriUV.Y) * (maxTri3D - minTri3D);
			}
			if (maxVisUV.Y > maxTriUV.Y)
			{
				uv2 = maxTriUV;
				p2 = maxTri3D;
			}
			else
			{
				uv2 = maxVisUV;
				p2 = minTri3D + (maxVisUV.Y - minTriUV.Y) / (maxTriUV.Y - minTriUV.Y) * (maxTri3D - minTri3D);
			}
		}
		edgeList.Add(new HitTestEdge(p1, p2, uv1, uv2));
	}

	private double IntersectRayLine(Point o, Vector d, Point p1, Point p2, out bool coinc)
	{
		coinc = false;
		double dy = p2.Y - p1.Y;
		double dx = p2.X - p1.X;
		if (dx == 0.0)
		{
			if (d.X == 0.0)
			{
				coinc = o.X == p1.X;
				return -1.0;
			}
			return (p2.X - o.X) / d.X;
		}
		double numer = (o.X - p1.X) * dy / dx - o.Y + p1.Y;
		double denom = d.Y - d.X * dy / dx;
		if (denom == 0.0)
		{
			double b0 = (0.0 - o.X) * dy / dx + o.Y;
			double b1 = (0.0 - p1.X) * dy / dx + p1.Y;
			coinc = b0 == b1;
			return -1.0;
		}
		return numer / denom;
	}

	private void ProcessTriangleEdges(Point3D[] p, Point[] uv, Point[] tc, PolygonSide polygonSide, List<HitTestEdge> edgeList, Dictionary<Edge, EdgeInfo> adjInformation)
	{
		for (int i = 0; i < p.Length; i++)
		{
			Point3D p3D1 = p[i];
			Point3D p3D2 = p[(i + 1) % p.Length];
			Edge edge;
			Point uv2;
			Point uv3;
			if (p3D1.X < p3D2.X || (p3D1.X == p3D2.X && p3D1.Y < p3D2.Y) || (p3D1.X == p3D2.X && p3D1.Y == p3D2.Y && p3D1.Z < p3D1.Z))
			{
				edge = new Edge(p3D1, p3D2);
				uv2 = uv[i];
				uv3 = uv[(i + 1) % p.Length];
			}
			else
			{
				edge = new Edge(p3D2, p3D1);
				uv3 = uv[i];
				uv2 = uv[(i + 1) % p.Length];
			}
			EdgeInfo edgeInfo = (adjInformation.ContainsKey(edge) ? adjInformation[edge] : (adjInformation[edge] = new EdgeInfo()));
			edgeInfo.numSharing++;
			bool alreadyAdded = edgeInfo.hasBackFace && edgeInfo.hasFrontFace;
			if (polygonSide == PolygonSide.FRONT)
			{
				edgeInfo.hasFrontFace = true;
				edgeInfo.uv1 = uv2;
				edgeInfo.uv2 = uv3;
			}
			else
			{
				edgeInfo.hasBackFace = true;
			}
			if (!alreadyAdded && edgeInfo.hasBackFace && edgeInfo.hasFrontFace)
			{
				HandleSilhouetteEdge(edgeInfo.uv1, edgeInfo.uv2, edge._start, edge._end, tc, edgeList);
			}
		}
	}

	private void HandleSilhouetteEdge(Point uv1, Point uv2, Point3D p3D1, Point3D p3D2, Point[] bounds, List<HitTestEdge> edgeList)
	{
		List<Point3D> pointList = new List<Point3D>();
		List<Point> uvList = new List<Point>();
		Vector dir = uv2 - uv1;
		for (int i = 0; i < bounds.Length; i++)
		{
			Point visEdgeStart = bounds[i];
			Point visEdgeEnd = bounds[(i + 1) % bounds.Length];
			if (Math.Max(visEdgeStart.X, visEdgeEnd.X) < Math.Min(uv1.X, uv2.X) || Math.Min(visEdgeStart.X, visEdgeEnd.X) > Math.Max(uv1.X, uv2.X) || Math.Max(visEdgeStart.Y, visEdgeEnd.Y) < Math.Min(uv1.Y, uv2.Y) || Math.Min(visEdgeStart.Y, visEdgeEnd.Y) > Math.Max(uv1.Y, uv2.Y))
			{
				continue;
			}
			bool areCoincident = false;
			double t = IntersectRayLine(uv1, dir, visEdgeStart, visEdgeEnd, out areCoincident);
			if (areCoincident)
			{
				return;
			}
			if (t >= 0.0 && t <= 1.0)
			{
				Point intersUV = uv1 + dir * t;
				Point3D intersPoint3D = p3D1 + (p3D2 - p3D1) * t;
				double visEdgeDiff = (visEdgeStart - visEdgeEnd).Length;
				if ((intersUV - visEdgeStart).Length < visEdgeDiff && (intersUV - visEdgeEnd).Length < visEdgeDiff)
				{
					pointList.Add(intersPoint3D);
					uvList.Add(intersUV);
				}
			}
		}
		if (pointList.Count >= 2)
		{
			edgeList.Add(new HitTestEdge(pointList[0], pointList[1], uvList[0], uvList[1]));
		}
		else if (pointList.Count == 1)
		{
			if (IsPointInPolygon(bounds, uv1))
			{
				edgeList.Add(new HitTestEdge(pointList[0], p3D1, uvList[0], uv1));
			}
			if (IsPointInPolygon(bounds, uv2))
			{
				edgeList.Add(new HitTestEdge(pointList[0], p3D2, uvList[0], uv2));
			}
		}
		else if (IsPointInPolygon(bounds, uv1) && IsPointInPolygon(bounds, uv2))
		{
			edgeList.Add(new HitTestEdge(p3D1, p3D2, uv1, uv2));
		}
	}

	private bool IsPointInPolygon(Point[] polygon, Point p)
	{
		bool sign = false;
		for (int i = 0; i < polygon.Length; i++)
		{
			double crossProduct = Vector.CrossProduct(polygon[(i + 1) % polygon.Length] - polygon[i], polygon[i] - p);
			bool currSign = crossProduct > 0.0;
			if (i == 0)
			{
				sign = currSign;
			}
			else if (sign != currSign)
			{
				return false;
			}
		}
		return true;
	}

	private void GenerateMaterial()
	{
		InternalVisualBrush.Visual = null;
		InternalVisualBrush = CreateVisualBrush();
		Material material = Material.Clone();
		_content.Material = material;
		InternalVisualBrush.Visual = InternalVisual;
		SwapInVisualBrush(material);
		if (IsBackVisible)
		{
			_content.BackMaterial = material;
		}
	}

	private VisualBrush CreateVisualBrush()
	{
		VisualBrush vb = new VisualBrush();
		RenderOptions.SetCachingHint(vb, CachingHint.Cache);
		vb.ViewportUnits = BrushMappingMode.Absolute;
		vb.TileMode = TileMode.None;
		return vb;
	}

	private void SwapInVisualBrush(Material material)
	{
		bool foundMaterialToSwap = false;
		Stack<Material> materialStack = new Stack<Material>();
		materialStack.Push(material);
		while (materialStack.Count > 0)
		{
			Material currMaterial = materialStack.Pop();
			if (currMaterial is DiffuseMaterial)
			{
				DiffuseMaterial diffMaterial = (DiffuseMaterial)currMaterial;
				if ((bool)diffMaterial.GetValue(IsInteractiveMaterialProperty))
				{
					diffMaterial.Brush = InternalVisualBrush;
					foundMaterialToSwap = true;
				}
				continue;
			}
			if (currMaterial is EmissiveMaterial)
			{
				EmissiveMaterial emmMaterial = (EmissiveMaterial)currMaterial;
				if ((bool)emmMaterial.GetValue(IsInteractiveMaterialProperty))
				{
					emmMaterial.Brush = InternalVisualBrush;
					foundMaterialToSwap = true;
				}
				continue;
			}
			if (currMaterial is SpecularMaterial)
			{
				SpecularMaterial specMaterial = (SpecularMaterial)currMaterial;
				if ((bool)specMaterial.GetValue(IsInteractiveMaterialProperty))
				{
					specMaterial.Brush = InternalVisualBrush;
					foundMaterialToSwap = true;
				}
				continue;
			}
			if (currMaterial is MaterialGroup)
			{
				MaterialGroup matGroup = (MaterialGroup)currMaterial;
				foreach (Material m in matGroup.Children)
				{
					materialStack.Push(m);
				}
				continue;
			}
			throw new ArgumentException("material needs to be either a DiffuseMaterial, EmissiveMaterial, SpecularMaterial or a MaterialGroup", "material");
		}
		if (!foundMaterialToSwap)
		{
			throw new ArgumentException("material needs to contain at least one material that has the IsInteractiveMaterial attached property", "material");
		}
	}

	internal static void OnVisualChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		InteractiveVisual3D imv3D = (InteractiveVisual3D)sender;
		AdornerDecorator ad = null;
		if (imv3D.InternalVisual != null)
		{
			ad = (AdornerDecorator)imv3D.InternalVisual;
			if (ad.Child is VisualDecorator)
			{
				VisualDecorator oldVisualDecorator = (VisualDecorator)ad.Child;
				oldVisualDecorator.Content = null;
			}
		}
		if (ad == null)
		{
			ad = new AdornerDecorator();
		}
		UIElement adornerDecoratorChild;
		if (imv3D.Visual is UIElement)
		{
			adornerDecoratorChild = (UIElement)imv3D.Visual;
		}
		else
		{
			VisualDecorator visDecorator = new VisualDecorator();
			visDecorator.Content = imv3D.Visual;
			adornerDecoratorChild = visDecorator;
		}
		ad.Child = null;
		ad.Child = adornerDecoratorChild;
		imv3D._internalVisual = ad;
		imv3D.InternalVisualBrush.Visual = imv3D.InternalVisual;
	}

	internal static void OnIsBackVisiblePropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		InteractiveVisual3D imv3D = (InteractiveVisual3D)sender;
		if (imv3D.IsBackVisible)
		{
			imv3D._content.BackMaterial = imv3D._content.Material;
		}
		else
		{
			imv3D._content.BackMaterial = null;
		}
	}

	internal static void OnMaterialPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		InteractiveVisual3D imv3D = (InteractiveVisual3D)sender;
		imv3D.GenerateMaterial();
	}

	internal static void OnGeometryChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		InteractiveVisual3D imv3D = (InteractiveVisual3D)sender;
		imv3D._content.Geometry = imv3D.Geometry;
	}

	public static void SetIsInteractiveMaterial(UIElement element, bool value)
	{
		element.SetValue(IsInteractiveMaterialProperty, value);
	}

	public static bool GetIsInteractiveMaterial(UIElement element)
	{
		return (bool)element.GetValue(IsInteractiveMaterialProperty);
	}
}
