using System.IO;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

internal class RW4VertexArray : RW4Object
{
	public const uint type_code = 131077u;

	public VertexFormat format;

	public uint unk2;

	public Buffer<Vertex> vertices;

	public override void Read(RW4Model m, RW4Section s, Stream r)
	{
		if (s.type_code != 131077)
		{
			throw new ModelFormatException(r, "VA000 Bad type code", s.type_code);
		}
		int section_1 = r.ReadS32();
		unk2 = r.ReadU32();
		r.expect(0u, "VA001");
		uint vcount = r.ReadU32();
		r.expect(8u, "VA002");
		uint vertex_size = r.ReadU32();
		if (vertex_size != 36)
		{
			throw new ModelFormatException(r, "VA100 Unsupported vertex size", vertex_size);
		}
		int section_number = r.ReadS32();
		RW4Section section = m.Sections[section_number];
		section.LoadObject(m, vertices = new Buffer<Vertex>((int)vcount), r);
		section = m.Sections[section_1];
		if (section.type_code != 131076)
		{
			throw new ModelFormatException(r, "VA101 Bad section type code", section.type_code);
		}
		section.GetObject<VertexFormat>(m, r, out format);
	}

	public override void Write(RW4Model m, RW4Section s, Stream w)
	{
		w.WriteU32(format.section.number);
		w.WriteU32(unk2);
		w.WriteU32(0u);
		w.WriteU32((uint)vertices.Length);
		w.WriteU32(8u);
		w.WriteU32(36u);
		w.WriteU32(vertices.section.number);
	}

	public override int ComputeSize()
	{
		return 28;
	}
}
