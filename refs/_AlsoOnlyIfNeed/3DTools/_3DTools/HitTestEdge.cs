using System.Windows;
using System.Windows.Media.Media3D;

namespace _3DTools;

public class HitTestEdge
{
	public Point3D _p1;

	public Point3D _p2;

	public Point _uv1;

	public Point _uv2;

	public Point _p1Transformed;

	public Point _p2Transformed;

	public HitTestEdge(Point3D p1, Point3D p2, Point uv1, Point uv2)
	{
		_p1 = p1;
		_p2 = p2;
		_uv1 = uv1;
		_uv2 = uv2;
	}

	public void Project(Matrix3D objectToViewportTransform)
	{
		Point3D projPoint1 = objectToViewportTransform.Transform(_p1);
		Point3D projPoint2 = objectToViewportTransform.Transform(_p2);
		_p1Transformed = new Point(projPoint1.X, projPoint1.Y);
		_p2Transformed = new Point(projPoint2.X, projPoint2.Y);
	}
}
