using System.IO;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

internal class RW4TexMetadata : RW4Object
{
	public const int type_code = 131083;

	public Texture texture;

	public byte[] unk_data_1;

	public int[] unk_data_2 = new int[36];

	public override void Read(RW4Model m, RW4Section s, Stream r)
	{
		if (s.type_code != 131083)
		{
			throw new ModelFormatException(r, "TM000", s.type_code);
		}
		long p = s.size - unk_data_2.Length * 4 - 4;
		if (p < 0)
		{
			throw new ModelFormatException(r, "TM001", p);
		}
		unk_data_1 = r.ReadBytes((int)p);
		int sn = r.ReadS32();
		for (int i = 0; i < unk_data_2.Length; i++)
		{
			unk_data_2[i] = r.ReadS32();
		}
		m.Sections[sn].GetObject<Texture>(m, r, out texture);
	}

	public override void Write(RW4Model m, RW4Section s, Stream w)
	{
		w.Write(unk_data_1, 0, unk_data_1.Length);
		w.WriteU32(texture.section.number);
		for (int i = 0; i < unk_data_2.Length; i++)
		{
			w.WriteS32(unk_data_2[i]);
		}
	}

	public override int ComputeSize()
	{
		return unk_data_1.Length + 4 + unk_data_2.Length * 4;
	}
}
