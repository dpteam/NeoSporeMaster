using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace _3DTools;

public static class MeshUtils
{
	public static PointCollection GenerateCylindricalTextureCoordinates(MeshGeometry3D mesh, Vector3D dir)
	{
		if (mesh == null)
		{
			return null;
		}
		Rect3D bounds = mesh.Bounds;
		int count = mesh.Positions.Count;
		PointCollection texcoords = new PointCollection(count);
		IEnumerable<Point3D> positions = TransformPoints(ref bounds, mesh.Positions, ref dir);
		foreach (Point3D vertex in positions)
		{
			texcoords.Add(new Point(GetUnitCircleCoordinate(0.0 - vertex.Z, vertex.X), 1.0 - GetPlanarCoordinate(vertex.Y, bounds.Y, bounds.SizeY)));
		}
		return texcoords;
	}

	public static PointCollection GenerateSphericalTextureCoordinates(MeshGeometry3D mesh, Vector3D dir)
	{
		if (mesh == null)
		{
			return null;
		}
		Rect3D bounds = mesh.Bounds;
		int count = mesh.Positions.Count;
		PointCollection texcoords = new PointCollection(count);
		IEnumerable<Point3D> positions = TransformPoints(ref bounds, mesh.Positions, ref dir);
		foreach (Point3D vertex in positions)
		{
			Vector3D radius = new Vector3D(vertex.X, vertex.Y, vertex.Z);
			MathUtils.TryNormalize(ref radius);
			texcoords.Add(new Point(GetUnitCircleCoordinate(0.0 - radius.Z, radius.X), 1.0 - (Math.Asin(radius.Y) / Math.PI + 0.5)));
		}
		return texcoords;
	}

	public static PointCollection GeneratePlanarTextureCoordinates(MeshGeometry3D mesh, Vector3D dir)
	{
		if (mesh == null)
		{
			return null;
		}
		Rect3D bounds = mesh.Bounds;
		int count = mesh.Positions.Count;
		PointCollection texcoords = new PointCollection(count);
		IEnumerable<Point3D> positions = TransformPoints(ref bounds, mesh.Positions, ref dir);
		foreach (Point3D vertex in positions)
		{
			texcoords.Add(new Point(GetPlanarCoordinate(vertex.X, bounds.X, bounds.SizeX), GetPlanarCoordinate(vertex.Z, bounds.Z, bounds.SizeZ)));
		}
		return texcoords;
	}

	internal static double GetPlanarCoordinate(double end, double start, double width)
	{
		return (end - start) / width;
	}

	internal static double GetUnitCircleCoordinate(double y, double x)
	{
		return Math.Atan2(y, x) / (Math.PI * 2.0) + 0.5;
	}

	internal static IEnumerable<Point3D> TransformPoints(ref Rect3D bounds, Point3DCollection points, ref Vector3D dir)
	{
		if (dir == MathUtils.YAxis)
		{
			return points;
		}
		Vector3D rotAxis = Vector3D.CrossProduct(dir, MathUtils.YAxis);
		double rotAngle = Vector3D.AngleBetween(dir, MathUtils.YAxis);
		Quaternion q = ((rotAxis.X == 0.0 && rotAxis.Y == 0.0 && rotAxis.Z == 0.0) ? new Quaternion(MathUtils.XAxis, rotAngle) : new Quaternion(rotAxis, rotAngle));
		Vector3D center = new Vector3D(bounds.X + bounds.SizeX / 2.0, bounds.Y + bounds.SizeY / 2.0, bounds.Z + bounds.SizeZ / 2.0);
		Matrix3D t = Matrix3D.Identity;
		t.Translate(-center);
		t.Rotate(q);
		int count = points.Count;
		Point3D[] transformedPoints = new Point3D[count];
		for (int i = 0; i < count; i++)
		{
			ref Point3D reference = ref transformedPoints[i];
			reference = t.Transform(points[i]);
		}
		bounds = MathUtils.TransformBounds(bounds, t);
		return transformedPoints;
	}
}
