using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace _3DTools;

public abstract class MeshTextureCoordinateConverter : MeshConverter<PointCollection>
{
	public MeshTextureCoordinateConverter()
	{
	}

	public override object Convert(MeshGeometry3D mesh, object parameter)
	{
		string paramAsString = parameter as string;
		if (parameter != null && paramAsString == null)
		{
			throw new ArgumentException("Parameter must be a string.");
		}
		Vector3D dir = MathUtils.YAxis;
		if (paramAsString != null)
		{
			dir = Vector3D.Parse(paramAsString);
			MathUtils.TryNormalize(ref dir);
		}
		return Convert(mesh, dir);
	}

	public abstract object Convert(MeshGeometry3D mesh, Vector3D dir);
}
