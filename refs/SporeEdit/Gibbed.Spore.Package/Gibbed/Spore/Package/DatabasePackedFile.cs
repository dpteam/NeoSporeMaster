using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Gibbed.Spore.Helpers;

namespace Gibbed.Spore.Package
{
	public class DatabasePackedFile
	{
		public Version Version = new Version();

		public List<DatabaseIndex> Indices = new List<DatabaseIndex>();

		public void Read(Stream stream)
		{
			bool flag = false;
			uint num = stream.ReadU32();
			if (num != 1179664964 && num != 1178747460)
			{
				throw new NotAPackageException();
			}
			long num2;
			long offset;
			if (num == 1178747460)
			{
				flag = true;
				byte[] array = new byte[Marshal.SizeOf(typeof(DatabaseBigPackageFileHeader))];
				if (array.Length != 116)
				{
					throw new Exception("DatabaseBigPackageFileHeader is wrong size (" + array.Length + ")");
				}
				stream.Read(array, 0, array.Length);
				DatabaseBigPackageFileHeader databaseBigPackageFileHeader = (DatabaseBigPackageFileHeader)array.BytesToStructure(typeof(DatabaseBigPackageFileHeader));
				if (databaseBigPackageFileHeader.Always3 != 3)
				{
					throw new Exception("the value in the DBBF header that is always 3 was not 3");
				}
				Version = new Version(databaseBigPackageFileHeader.MajorVersion, databaseBigPackageFileHeader.MinorVersion);
				num2 = databaseBigPackageFileHeader.IndexCount;
				offset = databaseBigPackageFileHeader.IndexOffset;
			}
			else
			{
				flag = false;
				byte[] array2 = new byte[Marshal.SizeOf(typeof(DatabasePackedFileHeader))];
				if (array2.Length != 92)
				{
					throw new Exception("DatabasePackageFileHeader is wrong size (" + array2.Length + ")");
				}
				stream.Read(array2, 0, array2.Length);
				DatabasePackedFileHeader databasePackedFileHeader = (DatabasePackedFileHeader)array2.BytesToStructure(typeof(DatabasePackedFileHeader));
				if (databasePackedFileHeader.Always3 != 3)
				{
					throw new Exception("the value in the DBPF header that is always 3 was not 3");
				}
				Version = new Version(databasePackedFileHeader.MajorVersion, databasePackedFileHeader.MinorVersion);
				num2 = databasePackedFileHeader.IndexCount;
				offset = databasePackedFileHeader.IndexOffset;
			}
			Indices = new List<DatabaseIndex>();
			if (num2 <= 0)
			{
				return;
			}
			stream.Seek(offset, SeekOrigin.Begin);
			int num3 = stream.ReadS32();
			if (num3 < 4 || num3 > 7)
			{
				throw new InvalidDataException("don't know how to handle this index data");
			}
			uint typeId = 3405691582u;
			if ((num3 & 1) == 1)
			{
				typeId = stream.ReadU32();
			}
			uint groupId = 3405691582u;
			if ((num3 & 2) == 2)
			{
				groupId = stream.ReadU32();
			}
			uint unknown = 3405691582u;
			if ((num3 & 4) == 4)
			{
				unknown = stream.ReadU32();
			}
			for (int i = 0; i < num2; i++)
			{
				DatabaseIndex databaseIndex = new DatabaseIndex();
				if ((num3 & 1) == 1)
				{
					databaseIndex.TypeId = typeId;
				}
				else
				{
					databaseIndex.TypeId = stream.ReadU32();
				}
				if ((num3 & 2) == 2)
				{
					databaseIndex.GroupId = groupId;
				}
				else
				{
					databaseIndex.GroupId = stream.ReadU32();
				}
				if ((num3 & 4) == 4)
				{
					databaseIndex.Unknown = unknown;
				}
				else
				{
					databaseIndex.Unknown = stream.ReadU32();
				}
				databaseIndex.InstanceId = stream.ReadU32();
				if (flag)
				{
					databaseIndex.Offset = stream.ReadS64();
				}
				else
				{
					databaseIndex.Offset = stream.ReadS32();
				}
				databaseIndex.CompressedSize = stream.ReadU32();
				databaseIndex.DecompressedSize = stream.ReadU32();
				databaseIndex.CompressedFlags = stream.ReadS16();
				databaseIndex.Flags = stream.ReadU16();
				databaseIndex.CheckCompressed();
				Indices.Add(databaseIndex);
			}
		}

		private static byte[] StructureToBytes(object structure)
		{
			int num = Marshal.SizeOf(structure.GetType());
			byte[] array = new byte[num];
			GCHandle gCHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
			Marshal.StructureToPtr(structure, gCHandle.AddrOfPinnedObject(), fDeleteOld: false);
			gCHandle.Free();
			return array;
		}

		public void WriteHeader(Stream output, int indexOffset, int indexSize)
		{
			output.WriteASCII("DBPF");
			DatabasePackedFileHeader databasePackedFileHeader = default(DatabasePackedFileHeader);
			databasePackedFileHeader.MajorVersion = Version.Major;
			databasePackedFileHeader.MinorVersion = Version.Minor;
			databasePackedFileHeader.Always3 = 3u;
			databasePackedFileHeader.IndexCount = Indices.Count;
			databasePackedFileHeader.IndexOffset = indexOffset;
			databasePackedFileHeader.IndexSize = indexSize;
			byte[] array = StructureToBytes(databasePackedFileHeader);
			output.Write(array, 0, array.Length);
		}

		public void WriteIndex(Stream output)
		{
			output.WriteU32(4u);
			output.WriteU32(0u);
			foreach (DatabaseIndex index in Indices)
			{
				output.WriteU32(index.TypeId);
				output.WriteU32(index.GroupId);
				output.WriteU32(index.InstanceId);
				output.WriteS32((int)index.Offset);
				output.WriteU32(index.CompressedSize);
				output.WriteU32(index.DecompressedSize);
				output.WriteS16(index.CompressedFlags);
				output.WriteU16(index.Flags);
			}
		}
	}
}
