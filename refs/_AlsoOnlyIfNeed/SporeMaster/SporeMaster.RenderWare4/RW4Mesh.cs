using System.Collections.Generic;
using System.IO;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

internal class RW4Mesh : RW4Object
{
	public const int type_code = 131081;

	public RW4VertexArray vertices;

	public RW4TriangleArray triangles;

	private uint vert_count;

	private uint tri_count;

	public override void Read(RW4Model m, RW4Section s, Stream r)
	{
		r.expect(40u, "ME001");
		r.expect(4u, "ME002");
		int tri_section = r.ReadS32();
		tri_count = r.ReadU32();
		r.expect(1u, "ME003");
		r.expect(0u, "ME004");
		r.expect(tri_count * 3, "ME005");
		r.expect(0u, "ME006");
		vert_count = r.ReadU32();
		int vert_section = r.ReadS32();
		m.Sections[tri_section].GetObject<RW4TriangleArray>(m, r, out triangles);
		if (triangles.triangles.Length != tri_count)
		{
			throw new ModelFormatException(r, "ME100 Triangle count mismatch", new KeyValuePair<uint, int>(tri_count, triangles.triangles.Length));
		}
		if (vert_section != 4194304)
		{
			m.Sections[vert_section].GetObject<RW4VertexArray>(m, r, out vertices);
			if (vertices.vertices.Length != vert_count)
			{
				throw new ModelFormatException(r, "ME200 Vertex count mismatch", new KeyValuePair<uint, int>(vert_count, vertices.vertices.Length));
			}
		}
	}

	public override void Write(RW4Model m, RW4Section s, Stream w)
	{
		if (vertices != null)
		{
			vert_count = (uint)vertices.vertices.Length;
		}
		if (triangles != null)
		{
			tri_count = (uint)triangles.triangles.Length;
		}
		w.WriteU32(40u);
		w.WriteU32(4u);
		w.WriteU32(triangles.section.number);
		w.WriteU32(tri_count);
		w.WriteU32(1u);
		w.WriteU32(0u);
		w.WriteU32(tri_count * 3);
		w.WriteU32(0u);
		w.WriteU32(vert_count);
		w.WriteU32(vertices.section.number);
	}

	public override int ComputeSize()
	{
		return 40;
	}
}
