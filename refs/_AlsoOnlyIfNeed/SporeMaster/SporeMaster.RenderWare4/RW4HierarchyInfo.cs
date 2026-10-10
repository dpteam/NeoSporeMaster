using System.IO;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

internal class RW4HierarchyInfo : RW4Object
{
	public class Item
	{
		public int index;

		public uint name_fnv;

		public uint flags;

		public Item parent;
	}

	public const int type_code = 458754;

	public Item[] items;

	public uint id;

	public override void Read(RW4Model m, RW4Section s, Stream r)
	{
		if (s.type_code != 458754)
		{
			throw new ModelFormatException(r, "HI000", s.type_code);
		}
		uint p2 = r.ReadU32();
		uint p3 = r.ReadU32();
		uint p4 = r.ReadU32();
		uint c1 = r.ReadU32();
		id = r.ReadU32();
		r.expect(c1, "HI001");
		if (p4 != r.Position)
		{
			throw new ModelFormatException(r, "HI010", p4);
		}
		if (p2 != p4 + 4 * c1)
		{
			throw new ModelFormatException(r, "HI011", p2);
		}
		if (p3 != p4 + 8 * c1)
		{
			throw new ModelFormatException(r, "HI012", p3);
		}
		items = new Item[c1];
		for (int i = 0; i < c1; i++)
		{
			items[i] = new Item
			{
				index = i,
				name_fnv = r.ReadU32()
			};
		}
		for (int j = 0; j < c1; j++)
		{
			items[j].flags = r.ReadU32();
		}
		for (int k = 0; k < c1; k++)
		{
			int pind = r.ReadS32();
			if (pind == -1)
			{
				items[k].parent = null;
			}
			else
			{
				items[k].parent = items[pind];
			}
		}
	}

	public override void Write(RW4Model m, RW4Section s, Stream w)
	{
		uint c1 = (uint)items.Length;
		uint p1 = (uint)((int)w.Position + 24);
		w.WriteU32(p1 + 4 * c1);
		w.WriteU32(p1 + 8 * c1);
		w.WriteU32(p1);
		w.WriteU32(c1);
		w.WriteU32(id);
		w.WriteU32(c1);
		for (int i = 0; i < c1; i++)
		{
			w.WriteU32(items[i].name_fnv);
		}
		for (int j = 0; j < c1; j++)
		{
			w.WriteU32(items[j].flags);
		}
		for (int k = 0; k < c1; k++)
		{
			w.WriteS32((items[k].parent == null) ? (-1) : items[k].parent.index);
		}
	}

	public override int ComputeSize()
	{
		return 24 + 12 * items.Length;
	}
}
