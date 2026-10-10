using System.IO;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

internal struct Triangle : IRW4Struct
{
	public uint i;

	public uint j;

	public uint k;

	public byte unk1;

	public void Read(Stream r)
	{
		i = r.ReadU16();
		j = r.ReadU16();
		k = r.ReadU16();
	}

	public void Write(Stream w)
	{
		w.WriteU16((ushort)i);
		w.WriteU16((ushort)j);
		w.WriteU16((ushort)k);
	}

	public uint Size()
	{
		return 6u;
	}
}
