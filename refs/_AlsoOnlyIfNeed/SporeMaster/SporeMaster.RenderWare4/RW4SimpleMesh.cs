using System.IO;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

internal class RW4SimpleMesh : RW4Object
{
	public const int type_code = 524291;

	private RW4BBox bbox = new RW4BBox();

	private uint unk1;

	public Vertex[] vertices;

	public Triangle[] triangles;

	private uint[] unknown_data_2;

	public override void Read(RW4Model m, RW4Section s, Stream r)
	{
		_ = r.Position;
		bbox.Read(m, null, r);
		r.expect(13996552u, "SM001");
		unk1 = r.ReadU32();
		uint tri_count = r.ReadU32();
		r.expect(0u, "SM002");
		uint vertexcount = r.ReadU32();
		uint p2 = r.ReadU32();
		uint p3 = r.ReadU32();
		uint p4 = r.ReadU32();
		uint p5 = r.ReadU32();
		if (p3 != (uint)((r.Position + 15) & -16))
		{
			throw new ModelFormatException(r, "SM010", null);
		}
		r.ReadPadding(p3 - r.Position);
		if (p2 != p3 + vertexcount * 16)
		{
			throw new ModelFormatException(r, "SM101", null);
		}
		if (p5 != p2 + tri_count * 16)
		{
			throw new ModelFormatException(r, "SM102", null);
		}
		if (p4 != p5 + ((tri_count / 2 + 15) & -16))
		{
			throw new ModelFormatException(r, "SM103", null);
		}
		vertices = new Vertex[vertexcount];
		for (int i = 0; i < vertexcount; i++)
		{
			vertices[i].Read4(r);
		}
		triangles = new Triangle[tri_count];
		for (int t = 0; t < tri_count; t++)
		{
			uint index = r.ReadU32();
			if (index >= vertexcount)
			{
				throw new ModelFormatException(r, "SM200", t);
			}
			triangles[t].i = (ushort)index;
			index = r.ReadU32();
			if (index >= vertexcount)
			{
				throw new ModelFormatException(r, "SM200", t);
			}
			triangles[t].j = (ushort)index;
			index = r.ReadU32();
			if (index >= vertexcount)
			{
				throw new ModelFormatException(r, "SM200", t);
			}
			triangles[t].k = (ushort)index;
			r.expect(0u, "SM201");
		}
		uint x = 0u;
		for (int j = 0; j < tri_count; j++)
		{
			if ((j & 7) == 0)
			{
				x = r.ReadU32();
			}
			triangles[j].unk1 = (byte)((x >> (j & 7) * 4) & 0xF);
		}
		for (int k = (int)tri_count; k < ((tri_count + 7) & -8); k++)
		{
			if ((byte)((x >> (k & 7) * 4) & 0xF) != 15)
			{
				throw new ModelFormatException(r, "SM210", k);
			}
		}
		r.ReadPadding(p4 - r.Position);
		r.expect(p3 - 32, "SM301");
		uint u2_count = r.ReadU32();
		r.expect(tri_count, "SM302");
		r.expect(0u, "SM303");
		RW4BBox bbox2 = new RW4BBox();
		bbox2.Read(m, null, r);
		if (!bbox.IsIdentical(bbox2))
		{
			throw new ModelFormatException(r, "SM310", bbox2);
		}
		unknown_data_2 = new uint[u2_count * 8];
		for (int l = 0; l < unknown_data_2.Length; l++)
		{
			unknown_data_2[l] = r.ReadU32();
		}
	}

	public override void Write(RW4Model m, RW4Section s, Stream w)
	{
		int tri_count = triangles.Length;
		bbox.Write(m, null, w);
		w.WriteU32(13996552u);
		w.WriteU32(unk1);
		w.WriteS32(tri_count);
		w.WriteU32(0u);
		w.WriteS32(vertices.Length);
		long p1 = (w.Position + 16 + 15) & -16;
		long p4 = p1 + vertices.Length * 16 + tri_count * 16 + ((tri_count / 2 + 15) & -16);
		w.WriteU32((uint)(p1 + vertices.Length * 16));
		w.WriteU32((uint)p1);
		w.WriteU32((uint)p4);
		w.WriteU32((uint)(p1 + vertices.Length * 16 + tri_count * 16));
		w.WritePadding(p1 - w.Position);
		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i].Write4(w);
		}
		for (int t = 0; t < tri_count; t++)
		{
			w.WriteU32(triangles[t].i);
			w.WriteU32(triangles[t].j);
			w.WriteU32(triangles[t].k);
			w.WriteU32(0u);
		}
		uint pack = 0u;
		for (int j = 0; j < tri_count; j++)
		{
			pack |= (uint)(triangles[j].unk1 << (j & 7) * 4);
			if ((j & 7) == 7)
			{
				w.WriteU32(pack);
				pack = 0u;
			}
		}
		for (int k = tri_count; k < ((tri_count + 7) & -8); k++)
		{
			pack |= (uint)(15 << (k & 7) * 4);
			if ((k & 7) == 7)
			{
				w.WriteU32(pack);
			}
		}
		w.WritePadding(p4 - w.Position);
		w.WriteU32((uint)(p1 - 32));
		w.WriteU32((uint)(unknown_data_2.Length / 8));
		w.WriteU32((uint)tri_count);
		w.WriteU32(0u);
		bbox.Write(m, null, w);
		w.WriteU32s(unknown_data_2);
	}
}
