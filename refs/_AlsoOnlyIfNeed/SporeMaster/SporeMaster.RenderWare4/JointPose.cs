using System.IO;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

internal struct JointPose : IRW4Struct
{
	public const uint size = 48u;

	public float qx;

	public float qy;

	public float qz;

	public float qs;

	public float tx;

	public float ty;

	public float tz;

	public float sx;

	public float sy;

	public float sz;

	public float time;

	public uint Size()
	{
		return 48u;
	}

	public void Read(Stream s)
	{
		qx = s.ReadF32();
		qy = s.ReadF32();
		qz = s.ReadF32();
		qs = s.ReadF32();
		tx = s.ReadF32();
		ty = s.ReadF32();
		tz = s.ReadF32();
		sx = s.ReadF32();
		sy = s.ReadF32();
		sz = s.ReadF32();
		s.expect(0u, "JP001");
		time = s.ReadF32();
	}

	public void Write(Stream s)
	{
		s.WriteF32(qx);
		s.WriteF32(qy);
		s.WriteF32(qz);
		s.WriteF32(qs);
		s.WriteF32(tx);
		s.WriteF32(ty);
		s.WriteF32(tz);
		s.WriteF32(sx);
		s.WriteF32(sy);
		s.WriteF32(sz);
		s.WriteU32(0u);
		s.WriteF32(time);
	}
}
