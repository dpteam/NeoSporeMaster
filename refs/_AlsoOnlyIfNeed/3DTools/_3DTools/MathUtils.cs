using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace _3DTools;

public static class MathUtils
{
	public static readonly Matrix3D ZeroMatrix = new Matrix3D(0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0);

	public static readonly Vector3D XAxis = new Vector3D(1.0, 0.0, 0.0);

	public static readonly Vector3D YAxis = new Vector3D(0.0, 1.0, 0.0);

	public static readonly Vector3D ZAxis = new Vector3D(0.0, 0.0, 1.0);

	public static double GetAspectRatio(Size size)
	{
		return size.Width / size.Height;
	}

	public static double DegreesToRadians(double degrees)
	{
		return degrees * (Math.PI / 180.0);
	}

	private static Matrix3D GetViewMatrix(ProjectionCamera camera)
	{
		Vector3D zAxis = -camera.LookDirection;
		zAxis.Normalize();
		Vector3D xAxis = Vector3D.CrossProduct(camera.UpDirection, zAxis);
		xAxis.Normalize();
		Vector3D yAxis = Vector3D.CrossProduct(zAxis, xAxis);
		Vector3D position = (Vector3D)camera.Position;
		double offsetX = 0.0 - Vector3D.DotProduct(xAxis, position);
		double offsetY = 0.0 - Vector3D.DotProduct(yAxis, position);
		double offsetZ = 0.0 - Vector3D.DotProduct(zAxis, position);
		return new Matrix3D(xAxis.X, yAxis.X, zAxis.X, 0.0, xAxis.Y, yAxis.Y, zAxis.Y, 0.0, xAxis.Z, yAxis.Z, zAxis.Z, 0.0, offsetX, offsetY, offsetZ, 1.0);
	}

	public static Matrix3D GetViewMatrix(Camera camera)
	{
		if (camera == null)
		{
			throw new ArgumentNullException("camera");
		}
		if (camera is ProjectionCamera projectionCamera)
		{
			return GetViewMatrix(projectionCamera);
		}
		if (camera is MatrixCamera matrixCamera)
		{
			return matrixCamera.ViewMatrix;
		}
		throw new ArgumentException($"Unsupported camera type '{camera.GetType().FullName}'.", "camera");
	}

	private static Matrix3D GetProjectionMatrix(OrthographicCamera camera, double aspectRatio)
	{
		double w = camera.Width;
		double h = w / aspectRatio;
		double zn = camera.NearPlaneDistance;
		double zf = camera.FarPlaneDistance;
		double m33 = 1.0 / (zn - zf);
		double m43 = zn * m33;
		return new Matrix3D(2.0 / w, 0.0, 0.0, 0.0, 0.0, 2.0 / h, 0.0, 0.0, 0.0, 0.0, m33, 0.0, 0.0, 0.0, m43, 1.0);
	}

	private static Matrix3D GetProjectionMatrix(PerspectiveCamera camera, double aspectRatio)
	{
		double hFoV = DegreesToRadians(camera.FieldOfView);
		double zn = camera.NearPlaneDistance;
		double zf = camera.FarPlaneDistance;
		double xScale = 1.0 / Math.Tan(hFoV / 2.0);
		double yScale = aspectRatio * xScale;
		double m33 = ((zf == double.PositiveInfinity) ? (-1.0) : (zf / (zn - zf)));
		double m43 = zn * m33;
		return new Matrix3D(xScale, 0.0, 0.0, 0.0, 0.0, yScale, 0.0, 0.0, 0.0, 0.0, m33, -1.0, 0.0, 0.0, m43, 0.0);
	}

	public static Matrix3D GetProjectionMatrix(Camera camera, double aspectRatio)
	{
		if (camera == null)
		{
			throw new ArgumentNullException("camera");
		}
		if (camera is PerspectiveCamera perspectiveCamera)
		{
			return GetProjectionMatrix(perspectiveCamera, aspectRatio);
		}
		if (camera is OrthographicCamera orthographicCamera)
		{
			return GetProjectionMatrix(orthographicCamera, aspectRatio);
		}
		if (camera is MatrixCamera matrixCamera)
		{
			return matrixCamera.ProjectionMatrix;
		}
		throw new ArgumentException($"Unsupported camera type '{camera.GetType().FullName}'.", "camera");
	}

	private static Matrix3D GetHomogeneousToViewportTransform(Rect viewport)
	{
		double scaleX = viewport.Width / 2.0;
		double scaleY = viewport.Height / 2.0;
		double offsetX = viewport.X + scaleX;
		double offsetY = viewport.Y + scaleY;
		return new Matrix3D(scaleX, 0.0, 0.0, 0.0, 0.0, 0.0 - scaleY, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, offsetX, offsetY, 0.0, 1.0);
	}

	public static Matrix3D TryWorldToViewportTransform(Viewport3DVisual visual, out bool success)
	{
		success = false;
		Matrix3D result = TryWorldToCameraTransform(visual, out success);
		if (success)
		{
			result.Append(GetProjectionMatrix(visual.Camera, GetAspectRatio(visual.Viewport.Size)));
			result.Append(GetHomogeneousToViewportTransform(visual.Viewport));
			success = true;
		}
		return result;
	}

	public static Matrix3D TryWorldToCameraTransform(Viewport3DVisual visual, out bool success)
	{
		success = false;
		Matrix3D result = Matrix3D.Identity;
		Camera camera = visual.Camera;
		if (camera == null)
		{
			return ZeroMatrix;
		}
		Rect viewport = visual.Viewport;
		if (viewport == Rect.Empty)
		{
			return ZeroMatrix;
		}
		Transform3D cameraTransform = camera.Transform;
		if (cameraTransform != null)
		{
			Matrix3D m = cameraTransform.Value;
			if (!m.HasInverse)
			{
				return ZeroMatrix;
			}
			m.Invert();
			result.Append(m);
		}
		result.Append(GetViewMatrix(camera));
		success = true;
		return result;
	}

	private static Matrix3D GetWorldTransformationMatrix(DependencyObject visual, out Viewport3DVisual viewport)
	{
		Matrix3D worldTransform = Matrix3D.Identity;
		viewport = null;
		if (!(visual is Visual3D))
		{
			throw new ArgumentException("Must be of type Visual3D.", "visual");
		}
		while (visual != null && visual is ModelVisual3D)
		{
			Transform3D transform = (Transform3D)visual.GetValue(ModelVisual3D.TransformProperty);
			if (transform != null)
			{
				worldTransform.Append(transform.Value);
			}
			visual = VisualTreeHelper.GetParent(visual);
		}
		viewport = visual as Viewport3DVisual;
		if (viewport == null)
		{
			if (visual != null)
			{
				throw new ApplicationException($"Unsupported type: '{visual.GetType().FullName}'.  Expected tree of ModelVisual3Ds leading up to a Viewport3DVisual.");
			}
			return ZeroMatrix;
		}
		return worldTransform;
	}

	public static Matrix3D TryTransformTo2DAncestor(DependencyObject visual, out Viewport3DVisual viewport, out bool success)
	{
		Matrix3D to2D = GetWorldTransformationMatrix(visual, out viewport);
		to2D.Append(TryWorldToViewportTransform(viewport, out success));
		if (!success)
		{
			return ZeroMatrix;
		}
		return to2D;
	}

	public static Matrix3D TryTransformToCameraSpace(DependencyObject visual, out Viewport3DVisual viewport, out bool success)
	{
		Matrix3D toViewSpace = GetWorldTransformationMatrix(visual, out viewport);
		toViewSpace.Append(TryWorldToCameraTransform(viewport, out success));
		if (!success)
		{
			return ZeroMatrix;
		}
		return toViewSpace;
	}

	public static Rect3D TransformBounds(Rect3D bounds, Matrix3D transform)
	{
		double x1 = bounds.X;
		double y1 = bounds.Y;
		double z1 = bounds.Z;
		double x2 = bounds.X + bounds.SizeX;
		double y2 = bounds.Y + bounds.SizeY;
		double z2 = bounds.Z + bounds.SizeZ;
		Point3D[] points = new Point3D[8]
		{
			new Point3D(x1, y1, z1),
			new Point3D(x1, y1, z2),
			new Point3D(x1, y2, z1),
			new Point3D(x1, y2, z2),
			new Point3D(x2, y1, z1),
			new Point3D(x2, y1, z2),
			new Point3D(x2, y2, z1),
			new Point3D(x2, y2, z2)
		};
		transform.Transform(points);
		Point3D p = points[0];
		x1 = (x2 = p.X);
		y1 = (y2 = p.Y);
		z1 = (z2 = p.Z);
		for (int i = 1; i < points.Length; i++)
		{
			p = points[i];
			x1 = Math.Min(x1, p.X);
			y1 = Math.Min(y1, p.Y);
			z1 = Math.Min(z1, p.Z);
			x2 = Math.Max(x2, p.X);
			y2 = Math.Max(y2, p.Y);
			z2 = Math.Max(z2, p.Z);
		}
		return new Rect3D(x1, y1, z1, x2 - x1, y2 - y1, z2 - z1);
	}

	public static bool TryNormalize(ref Vector3D v)
	{
		double length = v.Length;
		if (length != 0.0)
		{
			v /= length;
			return true;
		}
		return false;
	}

	public static Point3D GetCenter(Rect3D box)
	{
		return new Point3D(box.X + box.SizeX / 2.0, box.Y + box.SizeY / 2.0, box.Z + box.SizeZ / 2.0);
	}
}
