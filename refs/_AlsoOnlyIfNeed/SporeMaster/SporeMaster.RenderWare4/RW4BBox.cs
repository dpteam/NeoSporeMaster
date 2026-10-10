using System.IO;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

internal class RW4BBox : RW4Object
{
	public const int type_code = 524293;

	public float minx;

	public float miny;

	public float minz;

	public float maxx;

	public float maxy;

	public float maxz;

	public uint unk1;

	public uint unk2;

	public bool IsIdentical(RW4BBox b)
	{
		if (minx == b.minx && miny == b.miny && minz == b.minz && maxx == b.maxx && maxy == b.maxy && maxz == b.maxz && unk1 == b.unk1)
		{
			return unk2 == b.unk2;
		}
		return false;
	}

	public override void Read(RW4Model m, RW4Section s, Stream r)
	{
		minx = r.ReadF32();
		miny = r.ReadF32();
		minz = r.ReadF32();
		unk1 = r.ReadU32();
		maxx = r.ReadF32();
		maxy = r.ReadF32();
		maxz = r.ReadF32();
		unk2 = r.ReadU32();
		if (minx > maxx || miny > maxy || minz > maxz)
		{
			throw new ModelFormatException(r, "BBOX011", null);
		}
	}

	public override void Write(RW4Model m, RW4Section s, Stream w)
	{
		w.WriteF32(minx);
		w.WriteF32(miny);
		w.WriteF32(minz);
		w.WriteU32(unk1);
		w.WriteF32(maxx);
		w.WriteF32(maxy);
		w.WriteF32(maxz);
		w.WriteU32(unk2);
	}

	public override int ComputeSize()
	{
		return 32;
	}
}
