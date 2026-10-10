using System.IO;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

internal struct Vertex : IRW4Struct
{
	public const int size = 36;

	public float x;

	public float y;

	public float z;

	public uint normal;

	public uint tangent;

	public float u;

	public float v;

	public uint packed_bone_indices;

	public uint packed_bone_weights;

	public uint Size()
	{
		return 36u;
	}

	public void Read(Stream r)
	{
		x = r.ReadF32();
		y = r.ReadF32();
		z = r.ReadF32();
		normal = r.ReadU32();
		tangent = r.ReadU32();
		u = r.ReadF32();
		v = r.ReadF32();
		packed_bone_indices = r.ReadU32();
		packed_bone_weights = r.ReadU32();
	}

	public void Write(Stream w)
	{
		w.WriteF32(x);
		w.WriteF32(y);
		w.WriteF32(z);
		w.WriteU32(normal);
		w.WriteU32(tangent);
		w.WriteF32(u);
		w.WriteF32(v);
		w.WriteU32(packed_bone_indices);
		w.WriteU32(packed_bone_weights);
	}

	public void Read4(Stream r)
	{
		x = r.ReadF32();
		y = r.ReadF32();
		z = r.ReadF32();
		r.expect(0u, "4V001");
	}

	public void Write4(Stream w)
	{
		w.WriteF32(x);
		w.WriteF32(y);
		w.WriteF32(z);
		w.WriteU32(0u);
	}

	public static uint PackNormal(float x, float y, float z)
	{
		float invl = 127.5f;
		byte xb = (byte)((double)(x * invl) + 127.5);
		byte yb = (byte)((double)(y * invl) + 127.5);
		byte zb = (byte)((double)(z * invl) + 127.5);
		return (uint)(xb + (yb << 8) + (zb << 16));
	}

	public static float UnpackNormal(uint packed, int dim)
	{
		byte b = (byte)((packed >> dim * 8) & 0xFF);
		return ((float)(int)b - 127.5f) / 127.5f;
	}
}
