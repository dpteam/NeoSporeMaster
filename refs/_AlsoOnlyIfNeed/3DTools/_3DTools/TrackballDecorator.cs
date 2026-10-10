using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace _3DTools;

public class TrackballDecorator : Viewport3DDecorator
{
	private Point _previousPosition2D;

	private Vector3D _previousPosition3D = new Vector3D(0.0, 0.0, 1.0);

	private Transform3DGroup _transform;

	private ScaleTransform3D _scale = new ScaleTransform3D();

	private AxisAngleRotation3D _rotation = new AxisAngleRotation3D();

	private Border _eventSource;

	public Transform3D Transform => _transform;

	public TrackballDecorator()
	{
		_transform = new Transform3DGroup();
		_transform.Children.Add(_scale);
		_transform.Children.Add(new RotateTransform3D(_rotation));
		_eventSource = new Border();
		_eventSource.Background = Brushes.Transparent;
		PreViewportChildren.Add(_eventSource);
	}

	protected override void OnMouseDown(MouseButtonEventArgs e)
	{
		base.OnMouseDown(e);
		_previousPosition2D = e.GetPosition(this);
		_previousPosition3D = ProjectToTrackball(ActualWidth, ActualHeight, _previousPosition2D);
		if (Mouse.Captured == null)
		{
			Mouse.Capture(this, CaptureMode.Element);
		}
	}

	protected override void OnMouseUp(MouseButtonEventArgs e)
	{
		base.OnMouseUp(e);
		if (IsMouseCaptured)
		{
			Mouse.Capture(this, CaptureMode.None);
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (!IsMouseCaptured)
		{
			return;
		}
		Point currentPosition = e.GetPosition(this);
		if (currentPosition == _previousPosition2D)
		{
			return;
		}
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			Track(currentPosition);
		}
		else if (e.RightButton == MouseButtonState.Pressed)
		{
			Zoom(currentPosition);
		}
		_previousPosition2D = currentPosition;
		Viewport3D viewport3D = Viewport3D;
		if (viewport3D != null && viewport3D.Camera != null)
		{
			if (viewport3D.Camera.IsFrozen)
			{
				viewport3D.Camera = viewport3D.Camera.Clone();
			}
			if (viewport3D.Camera.Transform != _transform)
			{
				viewport3D.Camera.Transform = _transform;
			}
		}
	}

	private void Track(Point currentPosition)
	{
		Vector3D currentPosition3D = ProjectToTrackball(ActualWidth, ActualHeight, currentPosition);
		Vector3D axis = Vector3D.CrossProduct(_previousPosition3D, currentPosition3D);
		double angle = Vector3D.AngleBetween(_previousPosition3D, currentPosition3D);
		if (axis.Length != 0.0)
		{
			Quaternion delta = new Quaternion(axis, 0.0 - angle);
			Quaternion q = new Quaternion(_rotation.Axis, _rotation.Angle);
			q *= delta;
			_rotation.Axis = q.Axis;
			_rotation.Angle = q.Angle;
			_previousPosition3D = currentPosition3D;
		}
	}

	private Vector3D ProjectToTrackball(double width, double height, Point point)
	{
		double x = point.X / (width / 2.0);
		double y = point.Y / (height / 2.0);
		x--;
		y = 1.0 - y;
		double z2 = 1.0 - x * x - y * y;
		double z3 = ((z2 > 0.0) ? Math.Sqrt(z2) : 0.0);
		return new Vector3D(x, y, z3);
	}

	private void Zoom(Point currentPosition)
	{
		double yDelta = currentPosition.Y - _previousPosition2D.Y;
		double scale = Math.Exp(yDelta / 100.0);
		_scale.ScaleX *= scale;
		_scale.ScaleY *= scale;
		_scale.ScaleZ *= scale;
	}
}
