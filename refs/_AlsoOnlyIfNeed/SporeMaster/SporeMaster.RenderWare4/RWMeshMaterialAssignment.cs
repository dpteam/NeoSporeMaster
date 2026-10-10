using System.IO;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

internal class RWMeshMaterialAssignment : RW4Object
{
	public const int type_code = 131098;

	public RW4Mesh mesh;

	public RW4TexMetadata[] mat;

	public override void Read(RW4Model m, RW4Section s, Stream r)
	{
		int sn1 = r.ReadS32();
		uint count = r.ReadU32();
		int[] sns = new int[count];
		for (int i = 0; i < count; i++)
		{
			sns[i] = r.ReadS32();
		}
		m.Sections[sn1].GetObject<RW4Mesh>(m, r, out mesh);
		mat = new RW4TexMetadata[count];
		for (int j = 0; j < count; j++)
		{
			m.Sections[sns[j]].GetObject<RW4TexMetadata>(m, r, out mat[j]);
		}
	}

	public override void Write(RW4Model m, RW4Section s, Stream w)
	{
		w.WriteU32(mesh.section.number);
		w.WriteU32((uint)mat.Length);
		RW4TexMetadata[] array = mat;
		foreach (RW4TexMetadata x in array)
		{
			w.WriteU32(x.section.number);
		}
	}

	public override int ComputeSize()
	{
		return 8 + 4 * mat.Length;
	}
}
