using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace _3DTools;

public class Interactive3DDecorator : Viewport3DDecorator
{
	public class DebugEdgesAdorner : Adorner
	{
		private List<HitTestEdge> _edges;

		public DebugEdgesAdorner(UIElement adornedElement, List<HitTestEdge> edges)
			: base(adornedElement)
		{
			_edges = edges;
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			Pen renderPen = new Pen(new SolidColorBrush(Colors.Navy), 1.5);
			for (int i = 0; i < _edges.Count; i++)
			{
				drawingContext.DrawLine(renderPen, _edges[i]._p1Transformed, _edges[i]._p2Transformed);
			}
		}
	}

	private class ClosestIntersectionInfo
	{
		private Point _pointHit;

		private UIElement _uiElemHit;

		private InteractiveVisual3D _imv3DHit;

		public Point PointHit
		{
			get
			{
				return _pointHit;
			}
			set
			{
				_pointHit = value;
			}
		}

		public UIElement UIElementHit
		{
			get
			{
				return _uiElemHit;
			}
			set
			{
				_uiElemHit = value;
			}
		}

		public InteractiveVisual3D InteractiveModelVisual3DHit
		{
			get
			{
				return _imv3DHit;
			}
			set
			{
				_imv3DHit = value;
			}
		}

		public ClosestIntersectionInfo(Point p, UIElement v, InteractiveVisual3D iv3D)
		{
			_pointHit = p;
			_uiElemHit = v;
			_imv3DHit = iv3D;
		}
	}

	private const double BUFFER_SIZE = 2.0;

	public static readonly DependencyProperty DebugProperty = DependencyProperty.Register("Debug", typeof(bool), typeof(Interactive3DDecorator), new PropertyMetadata(false, OnDebugPropertyChanged));

	public static readonly DependencyProperty ContainsInkProperty = DependencyProperty.Register("ContainsInk", typeof(bool), typeof(Interactive3DDecorator), new PropertyMetadata(false));

	private Decorator _hiddenVisual;

	private Decorator _oldHiddenVisual;

	private Decorator _oldKeyboardFocusVisual;

	private TranslateTransform _hiddenVisTranslate;

	private ScaleTransform _hiddenVisScale;

	private TransformGroup _hiddenVisTransform;

	private bool _mouseCaptureInHiddenVisual;

	private double _offsetX;

	private double _offsetY;

	private double _scale;

	private ClosestIntersectionInfo _closestIntersectInfo;

	private ClosestIntersectionInfo _lastValidClosestIntersectInfo;

	private DebugEdgesAdorner _DEBUGadorner;

	private bool _isInPosition;

	public bool Debug
	{
		get
		{
			return (bool)GetValue(DebugProperty);
		}
		set
		{
			SetValue(DebugProperty, value);
		}
	}

	public bool ContainsInk
	{
		get
		{
			return (bool)GetValue(ContainsInkProperty);
		}
		set
		{
			SetValue(ContainsInkProperty, value);
		}
	}

	public Interactive3DDecorator()
	{
		ClipToBounds = true;
		_offsetX = (_offsetY = 0.0);
		_scale = 1.0;
		_hiddenVisTranslate = new TranslateTransform(_offsetX, _offsetY);
		_hiddenVisScale = new ScaleTransform(_scale, _scale);
		_hiddenVisTransform = new TransformGroup();
		_hiddenVisTransform.Children.Add(_hiddenVisScale);
		_hiddenVisTransform.Children.Add(_hiddenVisTranslate);
		_hiddenVisual = new Decorator();
		_hiddenVisual.Opacity = 0.0;
		_hiddenVisual.RenderTransform = _hiddenVisTransform;
		_oldHiddenVisual = new Decorator();
		_oldHiddenVisual.Opacity = 0.0;
		_oldKeyboardFocusVisual = new Decorator();
		_oldKeyboardFocusVisual.Opacity = 0.0;
		PostViewportChildren.Add(_oldHiddenVisual);
		PostViewportChildren.Add(_oldKeyboardFocusVisual);
		PostViewportChildren.Add(_hiddenVisual);
		_closestIntersectInfo = null;
		_lastValidClosestIntersectInfo = null;
		AllowDrop = true;
	}

	protected override void MeasurePostViewportChildren(Size constraint)
	{
		Size noConstraintSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
		foreach (UIElement uiElem in PostViewportChildren)
		{
			uiElem.Measure(noConstraintSize);
		}
	}

	protected override void ArrangePostViewportChildren(Size arrangeSize)
	{
		foreach (UIElement uiElem in PostViewportChildren)
		{
			uiElem.Arrange(new Rect(uiElem.DesiredSize));
		}
	}

	protected override void OnPreviewDragOver(DragEventArgs e)
	{
		base.OnPreviewDragOver(e);
		if (Viewport3D != null)
		{
			Point mousePosition = e.GetPosition(Viewport3D);
			ArrangeHiddenVisual(mousePosition, scaleHiddenVisual: true);
		}
	}

	protected override void OnDragOver(DragEventArgs e)
	{
		base.OnDragOver(e);
		e.Effects = DragDropEffects.None;
		e.Handled = true;
	}

	protected override void OnDragEnter(DragEventArgs e)
	{
		base.OnDragEnter(e);
		e.Effects = DragDropEffects.None;
		e.Handled = true;
	}

	protected override void OnPreviewMouseMove(MouseEventArgs e)
	{
		base.OnPreviewMouseMove(e);
		if (_isInPosition)
		{
			_isInPosition = false;
		}
		else if (Viewport3D != null && ArrangeHiddenVisual(e.GetPosition(Viewport3D), scaleHiddenVisual: false))
		{
			e.Handled = true;
			_isInPosition = true;
			if (ContainsInk)
			{
				InvalidateArrange();
			}
			Mouse.Synchronize();
		}
	}

	private bool ArrangeHiddenVisual(Point mouseposition, bool scaleHiddenVisual)
	{
		bool needMouseResync = false;
		Viewport3D viewport3D = Viewport3D;
		if (viewport3D != null)
		{
			PointHitTestParameters pointparams = new PointHitTestParameters(mouseposition);
			_closestIntersectInfo = null;
			_mouseCaptureInHiddenVisual = _hiddenVisual.IsMouseCaptureWithin;
			VisualTreeHelper.HitTest(viewport3D, InteractiveMV3DFilter, HTResult, pointparams);
			if (_closestIntersectInfo == null && _mouseCaptureInHiddenVisual && _lastValidClosestIntersectInfo != null)
			{
				HandleMouseCaptureButOffMesh(_lastValidClosestIntersectInfo.InteractiveModelVisual3DHit, mouseposition);
			}
			else if (_closestIntersectInfo != null)
			{
				_lastValidClosestIntersectInfo = _closestIntersectInfo;
			}
			needMouseResync = UpdateHiddenVisual(_closestIntersectInfo, mouseposition, scaleHiddenVisual);
		}
		return needMouseResync;
	}

	private void HandleMouseCaptureButOffMesh(InteractiveVisual3D imv3DHit, Point mousePos)
	{
		UIElement uie = (UIElement)Mouse.Captured;
		Rect contBounds = VisualTreeHelper.GetDescendantBounds(uie);
		GeneralTransform gt = uie.TransformToAncestor(_hiddenVisual);
		Point[] visCorners = new Point[4]
		{
			gt.Transform(new Point(contBounds.Left, contBounds.Top)),
			gt.Transform(new Point(contBounds.Right, contBounds.Top)),
			gt.Transform(new Point(contBounds.Right, contBounds.Bottom)),
			gt.Transform(new Point(contBounds.Left, contBounds.Bottom))
		};
		Point[] texCoordsOfInterest = new Point[4];
		for (int i = 0; i < visCorners.Length; i++)
		{
			ref Point reference = ref texCoordsOfInterest[i];
			reference = VisualCoordsToTextureCoords(visCorners[i], _hiddenVisual);
		}
		List<HitTestEdge> edges = imv3DHit.GetVisualEdges(texCoordsOfInterest);
		if (Debug)
		{
			AdornerLayer myAdornerLayer = AdornerLayer.GetAdornerLayer(this);
			if (_DEBUGadorner == null)
			{
				_DEBUGadorner = new DebugEdgesAdorner(this, edges);
				myAdornerLayer.Add(_DEBUGadorner);
			}
			else
			{
				myAdornerLayer.Remove(_DEBUGadorner);
				_DEBUGadorner = new DebugEdgesAdorner(this, edges);
				myAdornerLayer.Add(_DEBUGadorner);
			}
		}
		FindClosestIntersection(mousePos, edges, imv3DHit);
	}

	private void FindClosestIntersection(Point mousePos, List<HitTestEdge> edges, InteractiveVisual3D imv3DHit)
	{
		double closestDistance = double.MaxValue;
		Point closestIntersection = default;
		for (int i = 0; i < edges.Count; i++)
		{
			Vector v1 = mousePos - edges[i]._p1Transformed;
			Vector v2 = edges[i]._p2Transformed - edges[i]._p1Transformed;
			double denom = v2 * v2;
			Point currClosest;
			double distance;
			if (denom == 0.0)
			{
				currClosest = edges[i]._p1Transformed;
				distance = v1.Length;
			}
			else
			{
				double numer = v2 * v1;
				if (numer < 0.0)
				{
					currClosest = edges[i]._p1Transformed;
				}
				else
				{
					currClosest = ((!(numer > denom)) ? (edges[i]._p1Transformed + numer / denom * v2) : edges[i]._p2Transformed);
				}
				distance = (mousePos - currClosest).Length;
			}
			if (distance < closestDistance)
			{
				closestDistance = distance;
				closestIntersection = ((denom == 0.0) ? edges[i]._uv1 : ((currClosest - edges[i]._p1Transformed).Length / Math.Sqrt(denom) * (edges[i]._uv2 - edges[i]._uv1) + edges[i]._uv1));
			}
		}
		if (closestDistance != double.MaxValue)
		{
			UIElement uiElemWCapture = (UIElement)Mouse.Captured;
			UIElement uiElemOnMesh = imv3DHit.InternalVisual;
			Rect contBounds = VisualTreeHelper.GetDescendantBounds(uiElemWCapture);
			Point ptOnVisual = TextureCoordsToVisualCoords(closestIntersection, uiElemOnMesh);
			Point ptRelToCapture = uiElemOnMesh.TransformToDescendant(uiElemWCapture).Transform(ptOnVisual);
			if (ptRelToCapture.X <= contBounds.Left + 1.0)
			{
				ptRelToCapture.X -= 2.0;
			}
			if (ptRelToCapture.Y <= contBounds.Top + 1.0)
			{
				ptRelToCapture.Y -= 2.0;
			}
			if (ptRelToCapture.X >= contBounds.Right - 1.0)
			{
				ptRelToCapture.X += 2.0;
			}
			if (ptRelToCapture.Y >= contBounds.Bottom - 1.0)
			{
				ptRelToCapture.Y += 2.0;
			}
			Point finalVisualPoint = uiElemWCapture.TransformToAncestor(uiElemOnMesh).Transform(ptRelToCapture);
			_closestIntersectInfo = new ClosestIntersectionInfo(VisualCoordsToTextureCoords(finalVisualPoint, uiElemOnMesh), imv3DHit.InternalVisual, imv3DHit);
		}
	}

	public HitTestFilterBehavior InteractiveMV3DFilter(DependencyObject o)
	{
		HitTestFilterBehavior behavior = HitTestFilterBehavior.Continue;
		if (o is Visual3D && _mouseCaptureInHiddenVisual)
		{
			if (o is InteractiveVisual3D)
			{
				InteractiveVisual3D imv3D = (InteractiveVisual3D)o;
				if (imv3D.InternalVisual != _hiddenVisual.Child)
				{
					behavior = HitTestFilterBehavior.ContinueSkipSelf;
				}
			}
			else
			{
				behavior = HitTestFilterBehavior.ContinueSkipSelf;
			}
		}
		return behavior;
	}

	private bool UpdateHiddenVisual(ClosestIntersectionInfo isectInfo, Point mousePos, bool scaleHiddenVisual)
	{
		bool needsMouseReSync = false;
		double newOffsetX;
		double newOffsetY;
		if (isectInfo != null)
		{
			UIElement uiElem = isectInfo.UIElementHit;
			if (_hiddenVisual.Child != uiElem)
			{
				UIElement prevVisual = _hiddenVisual.Child;
				if (_oldHiddenVisual.Child == uiElem)
				{
					_oldHiddenVisual.Child = null;
				}
				if (_oldKeyboardFocusVisual.Child == uiElem)
				{
					_oldKeyboardFocusVisual.Child = null;
				}
				if (_oldHiddenVisual.Child == prevVisual)
				{
					_oldHiddenVisual.Child = null;
				}
				if (_oldKeyboardFocusVisual.Child == prevVisual)
				{
					_oldKeyboardFocusVisual.Child = null;
				}
				Decorator _oldVisToUse = null;
				_oldVisToUse = ((prevVisual == null || !prevVisual.IsKeyboardFocusWithin) ? _oldHiddenVisual : _oldKeyboardFocusVisual);
				_hiddenVisual.Child = uiElem;
				_oldVisToUse.Child = prevVisual;
				needsMouseReSync = true;
			}
			Point ptOnVisual = TextureCoordsToVisualCoords(isectInfo.PointHit, _hiddenVisual);
			newOffsetX = mousePos.X - ptOnVisual.X;
			newOffsetY = mousePos.Y - ptOnVisual.Y;
		}
		else
		{
			newOffsetX = ActualWidth + 1.0;
			newOffsetY = ActualHeight + 1.0;
		}
		double newScale = ((!scaleHiddenVisual) ? 1.0 : Math.Max(Viewport3D.RenderSize.Width, Viewport3D.RenderSize.Height));
		return needsMouseReSync | PositionHiddenVisual(newOffsetX, newOffsetY, newScale, mousePos);
	}

	private bool PositionHiddenVisual(double newOffsetX, double newOffsetY, double newScale, Point mousePosition)
	{
		bool positionChanged = false;
		if (newOffsetX != _offsetX || newOffsetY != _offsetY || _scale != newScale)
		{
			_offsetX = newOffsetX;
			_offsetY = newOffsetY;
			_scale = newScale;
			_hiddenVisTranslate.X = _scale * (_offsetX - mousePosition.X) + mousePosition.X;
			_hiddenVisTranslate.Y = _scale * (_offsetY - mousePosition.Y) + mousePosition.Y;
			_hiddenVisScale.ScaleX = _scale;
			_hiddenVisScale.ScaleY = _scale;
			positionChanged = true;
		}
		return positionChanged;
	}

	private static Point TextureCoordsToVisualCoords(Point uv, UIElement uiElem)
	{
		Rect descBounds = VisualTreeHelper.GetDescendantBounds(uiElem);
		return new Point(uv.X * descBounds.Width + descBounds.Left, uv.Y * descBounds.Height + descBounds.Top);
	}

	private static Point VisualCoordsToTextureCoords(Point pt, UIElement uiElem)
	{
		Rect descBounds = VisualTreeHelper.GetDescendantBounds(uiElem);
		return new Point((pt.X - descBounds.Left) / (descBounds.Right - descBounds.Left), (pt.Y - descBounds.Top) / (descBounds.Bottom - descBounds.Top));
	}

	protected override void OnRenderSizeChanged(SizeChangedInfo info)
	{
		base.OnRenderSizeChanged(info);
		TranslateTransform tt = new TranslateTransform(info.NewSize.Width + 1.0, 0.0);
		_oldKeyboardFocusVisual.RenderTransform = tt;
		_oldHiddenVisual.RenderTransform = tt;
	}

	private HitTestResultBehavior HTResult(HitTestResult rawresult)
	{
		RayHitTestResult rayResult = rawresult as RayHitTestResult;
		HitTestResultBehavior hitTestResultBehavior = HitTestResultBehavior.Continue;
		if (rayResult != null)
		{
			_closestIntersectInfo = GetIntersectionInfo(rayResult);
			hitTestResultBehavior = HitTestResultBehavior.Stop;
		}
		return hitTestResultBehavior;
	}

	private ClosestIntersectionInfo GetIntersectionInfo(RayHitTestResult rayHitResult)
	{
		ClosestIntersectionInfo isectInfo = null;
		if (rayHitResult is RayMeshGeometry3DHitTestResult { VisualHit: InteractiveVisual3D imv3D, MeshHit: var geom } rayMeshResult)
		{
			UIElement uiElem = imv3D.InternalVisual;
			if (uiElem != null)
			{
				double vertexWeight1 = rayMeshResult.VertexWeight1;
				double vertexWeight2 = rayMeshResult.VertexWeight2;
				double vertexWeight3 = rayMeshResult.VertexWeight3;
				int index1 = rayMeshResult.VertexIndex1;
				int index2 = rayMeshResult.VertexIndex2;
				int index3 = rayMeshResult.VertexIndex3;
				if (geom.TextureCoordinates != null && index1 < geom.TextureCoordinates.Count && index2 < geom.TextureCoordinates.Count && index3 < geom.TextureCoordinates.Count)
				{
					Point texCoord1 = geom.TextureCoordinates[index1];
					Point texCoord2 = geom.TextureCoordinates[index2];
					Point texCoord3 = geom.TextureCoordinates[index3];
					Point finalPoint = new Point(texCoord1.X * vertexWeight1 + texCoord2.X * vertexWeight2 + texCoord3.X * vertexWeight3, texCoord1.Y * vertexWeight1 + texCoord2.Y * vertexWeight2 + texCoord3.Y * vertexWeight3);
					isectInfo = new ClosestIntersectionInfo(finalPoint, uiElem, imv3D);
				}
			}
		}
		return isectInfo;
	}

	internal static void OnDebugPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		Interactive3DDecorator iv3D = (Interactive3DDecorator)sender;
		if ((bool)e.NewValue)
		{
			iv3D._hiddenVisual.Opacity = 0.2;
			return;
		}
		iv3D._hiddenVisual.Opacity = 0.0;
		if (iv3D._DEBUGadorner != null)
		{
			AdornerLayer myAdornerLayer = AdornerLayer.GetAdornerLayer(iv3D);
			myAdornerLayer.Remove(iv3D._DEBUGadorner);
			iv3D._DEBUGadorner = null;
		}
	}
}
