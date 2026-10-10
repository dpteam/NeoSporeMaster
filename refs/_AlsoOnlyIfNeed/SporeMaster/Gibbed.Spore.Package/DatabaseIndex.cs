using System.IO;

namespace Gibbed.Spore.Package;

public class DatabaseIndex
{
	public bool Compressed;

	public uint TypeId;

	public uint GroupId;

	public uint InstanceId;

	public long Offset;

	public uint CompressedSize;

	public uint DecompressedSize;

	public short CompressedFlags;

	public ushort Flags;

	public uint Unknown;

	public void CheckCompressed()
	{
		CompressedSize &= 2147483647u;
		if (CompressedFlags != 0 && CompressedFlags != -1)
		{
			throw new InvalidDataException("compressed flags");
		}
		Compressed = CompressedFlags == -1;
	}

	public override string ToString()
	{
		return base.ToString() + ": " + TypeId.ToString("X8") + ", " + GroupId.ToString("X8") + " @ " + InstanceId.ToString("X8");
	}
}
