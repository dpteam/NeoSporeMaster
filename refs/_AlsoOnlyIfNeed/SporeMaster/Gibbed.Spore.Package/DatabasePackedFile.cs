using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Gibbed.Spore.Helpers;

namespace Gibbed.Spore.Package;

public class DatabasePackedFile
{
	public Version Version = new Version();

	public List<DatabaseIndex> Indices = new List<DatabaseIndex>();

	public void Read(Stream stream)
	{
		bool big = false;
		uint magic = stream.ReadU32();
		if (magic != 1179664964 && magic != 1178747460)
		{
			throw new NotAPackageException();
		}
		long indexCount;
		long indexOffset;
		if (magic == 1178747460)
		{
			big = true;
			byte[] data = new byte[Marshal.SizeOf(typeof(DatabaseBigPackageFileHeader))];
			if (data.Length != 116)
			{
				throw new Exception("DatabaseBigPackageFileHeader is wrong size (" + data.Length + ")");
			}
			stream.Read(data, 0, data.Length);
			DatabaseBigPackageFileHeader header = (DatabaseBigPackageFileHeader)data.BytesToStructure(typeof(DatabaseBigPackageFileHeader));
			if (header.Always3 != 3)
			{
				throw new Exception("the value in the DBBF header that is always 3 was not 3");
			}
			Version = new Version(header.MajorVersion, header.MinorVersion);
			indexCount = header.IndexCount;
			indexOffset = header.IndexOffset;
		}
		else
		{
			big = false;
			byte[] data2 = new byte[Marshal.SizeOf(typeof(DatabasePackedFileHeader))];
			if (data2.Length != 92)
			{
				throw new Exception("DatabasePackageFileHeader is wrong size (" + data2.Length + ")");
			}
			stream.Read(data2, 0, data2.Length);
			DatabasePackedFileHeader header2 = (DatabasePackedFileHeader)data2.BytesToStructure(typeof(DatabasePackedFileHeader));
			if (header2.Always3 != 3)
			{
				throw new Exception("the value in the DBPF header that is always 3 was not 3");
			}
			Version = new Version(header2.MajorVersion, header2.MinorVersion);
			indexCount = header2.IndexCount;
			indexOffset = header2.IndexOffset;
		}
		Indices = new List<DatabaseIndex>();
		if (indexCount <= 0)
		{
			return;
		}
		stream.Seek(indexOffset, SeekOrigin.Begin);
		int indexHeaderValues = stream.ReadS32();
		if (indexHeaderValues < 4 || indexHeaderValues > 7)
		{
			throw new InvalidDataException("don't know how to handle this index data");
		}
		uint indexTypeId = 3405691582u;
		if ((indexHeaderValues & 1) == 1)
		{
			indexTypeId = stream.ReadU32();
		}
		uint indexGroupId = 3405691582u;
		if ((indexHeaderValues & 2) == 2)
		{
			indexGroupId = stream.ReadU32();
		}
		uint indexUnknown = 3405691582u;
		if ((indexHeaderValues & 4) == 4)
		{
			indexUnknown = stream.ReadU32();
		}
		for (int i = 0; i < indexCount; i++)
		{
			DatabaseIndex index = new DatabaseIndex();
			if ((indexHeaderValues & 1) == 1)
			{
				index.TypeId = indexTypeId;
			}
			else
			{
				index.TypeId = stream.ReadU32();
			}
			if ((indexHeaderValues & 2) == 2)
			{
				index.GroupId = indexGroupId;
			}
			else
			{
				index.GroupId = stream.ReadU32();
			}
			if ((indexHeaderValues & 4) == 4)
			{
				index.Unknown = indexUnknown;
			}
			else
			{
				index.Unknown = stream.ReadU32();
			}
			index.InstanceId = stream.ReadU32();
			if (big)
			{
				index.Offset = stream.ReadS64();
			}
			else
			{
				index.Offset = stream.ReadS32();
			}
			index.CompressedSize = stream.ReadU32();
			index.DecompressedSize = stream.ReadU32();
			index.CompressedFlags = stream.ReadS16();
			index.Flags = stream.ReadU16();
			index.CheckCompressed();
			Indices.Add(index);
		}
	}

	private static byte[] StructureToBytes(object structure)
	{
		int size = Marshal.SizeOf(structure.GetType());
		byte[] data = new byte[size];
		GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
		Marshal.StructureToPtr(structure, handle.AddrOfPinnedObject(), fDeleteOld: false);
		handle.Free();
		return data;
	}

	public void WriteHeader(Stream output, int indexOffset, int indexSize)
	{
		output.WriteASCII("DBPF");
		byte[] data = StructureToBytes(new DatabasePackedFileHeader
		{
			MajorVersion = Version.Major,
			MinorVersion = Version.Minor,
			Always3 = 3u,
			IndexCount = Indices.Count,
			IndexOffset = indexOffset,
			IndexSize = indexSize
		});
		output.Write(data, 0, data.Length);
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
