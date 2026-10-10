using System.IO;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

internal struct Mat4x3 : IRW4Struct
{
	public float[] m;

	public uint Size()
	{
		return 48u;
	}

	public void Read(Stream s)
	{
		m = new float[12];
		for (int i = 0; i < m.Length; i++)
		{
			m[i] = s.ReadF32();
		}
	}

	public void Write(Stream s)
	{
		float[] array = m;
		foreach (float f in array)
		{
			s.WriteF32(f);
		}
	}
}
