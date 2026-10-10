using System.Windows.Media.Media3D;

namespace _3DTools;

public class CylindricalTextureCoordinateGenerator : MeshTextureCoordinateConverter
{
	public override object Convert(MeshGeometry3D mesh, Vector3D dir)
	{
		return MeshUtils.GenerateCylindricalTextureCoordinates(mesh, dir);
	}
}
