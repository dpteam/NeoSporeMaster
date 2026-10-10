using System.IO;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

internal class Matrices<T> : RW4Object where T : IRW4Struct, new()
{
	private static int Tsize = (int)new T().Size();

	public T[] items;

	public override void Read(RW4Model m, RW4Section s, Stream r)
	{
		uint p1 = r.ReadU32();
		uint count = r.ReadU32();
		r.expect(0u, "MS001");
		r.expect(0u, "MS002");
		if (p1 != r.Position)
		{
			throw new ModelFormatException(r, "MS001", p1);
		}
		items = new T[count];
		for (int i = 0; i < count; i++)
		{
			items[i].Read(r);
		}
	}

	public override void Write(RW4Model m, RW4Section s, Stream w)
	{
		uint p1 = (uint)((int)w.Position + 16);
		w.WriteU32(p1);
		w.WriteU32((uint)items.Length);
		w.WriteU32(0u);
		w.WriteU32(0u);
		T[] array = items;
		for (int i = 0; i < array.Length; i++)
		{
			T i2 = array[i];
			i2.Write(w);
		}
	}

	public override int ComputeSize()
	{
		return 16 + Tsize * items.Length;
	}
}
