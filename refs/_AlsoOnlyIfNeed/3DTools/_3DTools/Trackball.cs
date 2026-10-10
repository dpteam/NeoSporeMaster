using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace _3DTools;

public class Trackball
{
	private FrameworkElement _eventSource;

	private Point _previousPosition2D;

	private Vector3D _previousPosition3D = new Vector3D(0.0, 0.0, 1.0);

	private Transform3DGroup _transform;

	private ScaleTransform3D _scale = new ScaleTransform3D();

	private AxisAngleRotation3D _rotation = new AxisAngleRotation3D();

	public Transform3D Transform => _transform;

	public FrameworkElement EventSource
	{
		get
		{
			return _eventSource;
		}
		set
		{
			if (_eventSource != null)
			{
				_eventSource.MouseDown -= OnMouseDown;
				_eventSource.MouseUp -= OnMouseUp;
				_eventSource.MouseMove -= OnMouseMove;
			}
			_eventSource = value;
			_eventSource.MouseDown += OnMouseDown;
			_eventSource.MouseUp += OnMouseUp;
			_eventSource.MouseMove += OnMouseMove;
		}
	}

	public Trackball()
	{
		_transform = new Transform3DGroup();
		_transform.Children.Add(_scale);
		_transform.Children.Add(new RotateTransform3D(_rotation));
	}

	private void OnMouseDown(object sender, MouseEventArgs e)
	{
		Mouse.Capture(EventSource, CaptureMode.Element);
		_previousPosition2D = e.GetPosition(EventSource);
		_previousPosition3D = ProjectToTrackball(EventSource.ActualWidth, EventSource.ActualHeight, _previousPosition2D);
	}

	private void OnMouseUp(object sender, MouseEventArgs e)
	{
		Mouse.Capture(EventSource, CaptureMode.None);
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		Point currentPosition = e.GetPosition(EventSource);
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			Track(currentPosition);
		}
		else if (e.RightButton == MouseButtonState.Pressed)
		{
			Zoom(currentPosition);
		}
		_previousPosition2D = currentPosition;
	}

	private void Track(Point currentPosition)
	{
		Vector3D currentPosition3D = ProjectToTrackball(EventSource.ActualWidth, EventSource.ActualHeight, currentPosition);
		Vector3D axis = Vector3D.CrossProduct(_previousPosition3D, currentPosition3D);
		double angle = Vector3D.AngleBetween(_previousPosition3D, currentPosition3D);
		Quaternion delta = new Quaternion(axis, 0.0 - angle);
		Quaternion q = new Quaternion(_rotation.Axis, _rotation.Angle);
		q *= delta;
		_rotation.Axis = q.Axis;
		_rotation.Angle = q.Angle;
		_previousPosition3D = currentPosition3D;
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
