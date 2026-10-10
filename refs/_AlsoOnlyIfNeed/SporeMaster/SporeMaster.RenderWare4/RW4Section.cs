using System.Collections.Generic;
using System.IO;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

public class RW4Section
{
	public uint number;

	public uint pos;

	public uint size;

	public uint alignment;

	public uint type_code;

	public uint type_code_indirect;

	public RW4Object obj;

	public List<uint> fixup_offsets = new List<uint>();

	public void LoadHeader(Stream r, uint number, uint index_end)
	{
		this.number = number;
		pos = r.ReadU32();
		r.expect(0u, "H201");
		size = r.ReadU32();
		alignment = r.ReadU32();
		type_code_indirect = r.ReadU32();
		type_code = r.ReadU32();
		if (type_code == 65584)
		{
			pos += index_end;
		}
	}

	public void WriteHeader(Stream w, uint index_end)
	{
		w.WriteU32(pos - ((type_code == 65584) ? index_end : 0));
		w.WriteU32(0u);
		w.WriteU32(size);
		w.WriteU32(alignment);
		w.WriteU32(type_code_indirect);
		w.WriteU32(type_code);
	}

	public void GetObject<T>(RW4Model model, Stream r, out T o) where T : RW4Object, new()
	{
		if (obj != null)
		{
			o = (T)obj;
			return;
		}
		o = new T();
		LoadObject(model, o, r);
	}

	public void LoadObject(RW4Model model, RW4Object o, Stream r)
	{
		if (obj != null)
		{
			throw new ModelFormatException(r, "Attempt to decode section twice.", number);
		}
		long start = r.Position;
		obj = o;
		o.section = this;
		r.Seek(pos, SeekOrigin.Begin);
		obj.Read(model, this, r);
		if (r.Position != pos + size)
		{
			throw new ModelFormatException(r, "Section incompletely read.", number);
		}
		int cs = obj.ComputeSize();
		if (cs != -1 && cs != size)
		{
			throw new ModelFormatException(r, "Section size doesn't match computed size.", number);
		}
		r.Seek(start, SeekOrigin.Begin);
	}

	public void Write(Stream w, RW4Model model)
	{
		w.WritePadding(pos - w.Position);
		obj.Write(model, this, w);
		if (w.Position != pos + size)
		{
			throw new ModelFormatException(w, "Section incompletely written.", number);
		}
	}
}
