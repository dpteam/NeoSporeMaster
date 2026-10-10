using System.IO;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

internal class RW4Skeleton : RW4Object
{
	public const int type_code = 458764;

	public Matrices4x3 mat3;

	public Matrices4x4 mat4;

	public RW4HierarchyInfo jointInfo;

	public uint unk1;

	public override void Read(RW4Model m, RW4Section s, Stream r)
	{
		if (s.type_code != 458764)
		{
			throw new ModelFormatException(r, "SK000", s.type_code);
		}
		r.expect(4194304u, "SK001");
		r.expect(unk1 = 9268640u, "SK002");
		int sn1 = r.ReadS32();
		int sn2 = r.ReadS32();
		int sn3 = r.ReadS32();
		m.Sections[sn1].GetObject<Matrices4x3>(m, r, out mat3);
		m.Sections[sn2].GetObject<RW4HierarchyInfo>(m, r, out jointInfo);
		m.Sections[sn3].GetObject<Matrices4x4>(m, r, out mat4);
	}

	public override void Write(RW4Model m, RW4Section s, Stream w)
	{
		w.WriteU32(4194304u);
		w.WriteU32(unk1);
		w.WriteU32(mat3.section.number);
		w.WriteU32(jointInfo.section.number);
		w.WriteU32(mat4.section.number);
	}

	public override int ComputeSize()
	{
		return 20;
	}
}
