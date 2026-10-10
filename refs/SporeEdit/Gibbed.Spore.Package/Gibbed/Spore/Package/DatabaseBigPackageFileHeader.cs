using System.Runtime.InteropServices;

namespace Gibbed.Spore.Package
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct DatabaseBigPackageFileHeader
	{
		public int MajorVersion;

		public int MinorVersion;

		public uint Unknown0C;

		public uint Unknown10;

		public uint Unknown14;

		public uint Unknown18;

		public uint Unknown1C;

		public uint Unknown20;

		public int IndexCount;

		public long IndexSize;

		public uint Unknown30;

		public uint Always3;

		public long IndexOffset;

		public uint Unknown40;

		public uint Unknown44;

		public uint Unknown48;

		public uint Unknown4C;

		public uint Unknown50;

		public uint Unknown54;

		public uint Unknown58;

		public uint Unknown5C;

		public uint Unknown60;

		public uint Unknown64;

		public uint Unknown68;

		public uint Unknown6C;

		public uint Unknown70;

		public uint Unknown74;
	}
}
