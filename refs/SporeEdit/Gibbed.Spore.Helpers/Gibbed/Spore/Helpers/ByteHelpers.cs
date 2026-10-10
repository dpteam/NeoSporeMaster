using System;
using System.Runtime.InteropServices;

namespace Gibbed.Spore.Helpers
{
	public static class ByteHelpers
	{
		public static object BytesToStructure(this byte[] data, Type type)
		{
			if (data.Length != Marshal.SizeOf(type))
			{
				throw new Exception("structure size is not the same as the data size");
			}
			GCHandle gCHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
			object result = Marshal.PtrToStructure(gCHandle.AddrOfPinnedObject(), type);
			gCHandle.Free();
			return result;
		}
	}
}
