using System.IO;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

internal class RW4Material : RW4Object
{
	public const int type_code = 458763;

	public RW4TexMetadata tex;

	public RW4HierarchyInfo names;

	public uint unk1;

	public override void Read(RW4Model m, RW4Section s, Stream r)
	{
		if (s.type_code != 458763)
		{
			throw new ModelFormatException(r, "MT000", s.type_code);
		}
		r.expect(4194304u, "MT001");
		r.expect(unk1 = 6553520u, "MT002");
		r.expect((uint)((int)r.Position + 16), "MT003");
		r.expect(4194304u, "MT004");
		int s2 = r.ReadS32();
		r.expect(1u, "MT005");
		int s3 = r.ReadS32();
		r.expect(0u, "MT006");
		m.Sections[s2].GetObject<RW4HierarchyInfo>(m, r, out names);
		m.Sections[s3].GetObject<RW4TexMetadata>(m, r, out tex);
		if (names.items.Length != 1)
		{
			throw new ModelFormatException(r, "MT100", names.items.Length);
		}
	}

	public override void Write(RW4Model m, RW4Section s, Stream w)
	{
		w.WriteU32(4194304u);
		w.WriteU32(unk1);
		w.WriteU32((uint)((int)w.Position + 16));
		w.WriteU32(4194304u);
		w.WriteU32(names.section.number);
		w.WriteU32(1u);
		w.WriteU32(tex.section.number);
		w.WriteS32(0);
	}

	public override int ComputeSize()
	{
		return 32;
	}
}
