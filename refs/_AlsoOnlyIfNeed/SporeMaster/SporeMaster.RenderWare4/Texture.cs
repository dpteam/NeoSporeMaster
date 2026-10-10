using System.IO;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

internal class Texture : RW4Object
{
	public const int type_code = 131075;

	public const int DXT5 = 894720068;

	public TextureBlob texData;

	public uint textureType;

	public uint unk1;

	public ushort width;

	public ushort height;

	public uint mipmapInfo;

	public override void Read(RW4Model m, RW4Section s, Stream r)
	{
		if (s.type_code != 131075)
		{
			throw new ModelFormatException(r, "T000", s.type_code);
		}
		textureType = r.ReadU32();
		r.expect(8u, "T001");
		unk1 = r.ReadU32();
		width = r.ReadU16();
		height = r.ReadU16();
		mipmapInfo = r.ReadU32();
		r.expect(0u, "T002");
		r.expect(0u, "T003");
		int sec = r.ReadS32();
		m.Sections[sec].GetObject<TextureBlob>(m, r, out texData);
	}

	public override void Write(RW4Model m, RW4Section s, Stream w)
	{
		w.WriteU32(textureType);
		w.WriteU32(8u);
		w.WriteU32(unk1);
		w.WriteU16(width);
		w.WriteU16(height);
		w.WriteU32(mipmapInfo);
		w.WriteU32(0u);
		w.WriteU32(0u);
		w.WriteU32(texData.section.number);
	}

	public override int ComputeSize()
	{
		return 32;
	}
}
