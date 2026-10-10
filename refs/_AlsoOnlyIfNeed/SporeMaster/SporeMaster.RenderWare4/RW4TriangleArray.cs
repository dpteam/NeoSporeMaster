using System.IO;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

internal class RW4TriangleArray : RW4Object
{
	public const int type_code = 131079;

	public Buffer<Triangle> triangles;

	public uint unk1;

	public override int ComputeSize()
	{
		return 28;
	}

	public override void Read(RW4Model m, RW4Section s, Stream r)
	{
		if (s.type_code != 131079)
		{
			throw new ModelFormatException(r, "TA000 Bad type code", s.type_code);
		}
		unk1 = r.ReadU32();
		r.expect(0u, "TA001");
		uint ind_count = r.ReadU32();
		r.expect(8u, "TA002");
		r.expect(101u, "TA003");
		r.expect(4u, "TA004");
		int section_number = r.ReadS32();
		if (ind_count % 3 != 0)
		{
			throw new ModelFormatException(r, "TA010", ind_count);
		}
		uint tri_count = ind_count / 3;
		RW4Section section = m.Sections[section_number];
		section.LoadObject(m, triangles = new Buffer<Triangle>((int)tri_count), r);
	}

	public override void Write(RW4Model m, RW4Section s, Stream w)
	{
		w.WriteU32(unk1);
		w.WriteU32(0u);
		w.WriteU32((uint)(triangles.Length * 3));
		w.WriteU32(8u);
		w.WriteU32(101u);
		w.WriteU32(4u);
		w.WriteU32(triangles.section.number);
	}
}
