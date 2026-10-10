using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Gibbed.Spore.Helpers;
using Gibbed.Spore.Package;
using Gibbed.Spore.Properties;
using SporeMaster.RenderWare4;

namespace SporeMaster;

internal class PackageUnpack
{
	public PackageUnpack(Stream[] packageFiles, string destinationFolder, PleaseWait progress)
	{
		double e = 0.0;
		FullTextIndex.ResetPath(destinationFolder);
		if (Directory.Exists(destinationFolder))
		{
			progress?.beginTask(e = 0.25, 1.0);
			Directory.Delete(destinationFolder, recursive: true);
			progress?.endTask();
		}
		Directory.CreateDirectory(destinationFolder);
		double totalSize = 0.0;
		for (int i = 0; i < packageFiles.Length; i++)
		{
			totalSize += (double)packageFiles[i].Length;
		}
		progress?.beginTask(1.0 - e, totalSize);
		foreach (Stream filestream in packageFiles)
		{
			DatabasePackedFile db = new DatabasePackedFile();
			db.Read(filestream);
			DatabaseIndex[] DatabaseFiles = db.Indices.ToArray();
			uint group_sporemaster = "sporemaster".FNV();
			uint instance_names = "names".FNV();
			DatabaseIndex[] array = DatabaseFiles;
			foreach (DatabaseIndex dbf in array)
			{
				if (dbf.GroupId == group_sporemaster && dbf.InstanceId == instance_names)
				{
					byte[] data = unpack(filestream, dbf);
					readNamesFile(data);
				}
			}
			uint locale_type = NameRegistry.Types.toHash("locale");
			uint prop_type = NameRegistry.Types.toHash("prop");
			uint rw4_type = NameRegistry.Types.toHash("rw4");
			DatabaseIndex[] array2 = DatabaseFiles;
			foreach (DatabaseIndex dbf2 in array2)
			{
				if (dbf2.TypeId != locale_type || (!NameRegistry.Groups.toName(dbf2.InstanceId).StartsWith("auto_") && !NameRegistry.Groups.toName(dbf2.InstanceId).EndsWith("_auto")))
				{
					string fn = Path.Combine(destinationFolder, NameRegistry.getFileName(dbf2.GroupId, dbf2.InstanceId, dbf2.TypeId));
					Directory.CreateDirectory(Path.GetDirectoryName(fn));
					byte[] data2 = unpack(filestream, dbf2);
					if (dbf2.TypeId == prop_type)
					{
						writePropFile(data2, fn);
					}
					else if (dbf2.TypeId == rw4_type)
					{
						writeRW4File(data2, fn);
					}
					else
					{
						writeBinaryFile(data2, fn);
					}
				}
				progress?.addProgress(dbf2.CompressedSize);
			}
			filestream.Close();
		}
		progress?.endTask();
	}

	private void readNamesFile(byte[] data)
	{
		List<uint> newHashes = new List<uint>();
		NameRegistry.Files.readRegistry(new StreamReader(new MemoryStream(data)), overrideExisting: false, newHashes);
		if (newHashes.Count != 0)
		{
			Renamer.rename(newHashes.ToArray(), newHashes.Select((uint h) => "#" + h.ToString("X8")).ToArray(), null);
		}
		NameRegistry.Files.save();
	}

	private void writeBinaryFile(byte[] data, string fn)
	{
		using FileStream output = File.Create(fn);
		output.Write(data, 0, data.Length);
	}

	private void writePropFile(byte[] data, string fn)
	{
		PropertyFile file = new PropertyFile();
		file.Read(new MemoryStream(data));
		stripGeneratedLocaleReferences(file);
		FileStream output = File.Create(fn + ".xml");
		XmlTextWriter writer = new XmlTextWriter(output, Encoding.UTF8);
		writer.Formatting = Formatting.Indented;
		file.WriteXML(writer);
		writer.Flush();
		output.Close();
	}

	private void writeRW4File(byte[] data, string fn)
	{
		Directory.CreateDirectory(fn);
		writeBinaryFile(data, fn + "\\raw.rw4");
		try
		{
			using MemoryStream stream = new MemoryStream(data);
			new ModelUnpack(stream, fn);
		}
		catch (Exception ex)
		{
			File.WriteAllText(fn + "\\conversion_error.txt", ex.ToString());
		}
	}

	private void stripGeneratedLocaleReferences(PropertyFile file)
	{
		foreach (Property prop1 in file.Values.Values)
		{
			if (!(prop1 is ArrayProperty arr))
			{
				continue;
			}
			foreach (Property prop2 in arr.Values)
			{
				if (prop2 is TextProperty text && (NameRegistry.Files.toName(text.TableId).StartsWith("auto_") || NameRegistry.Files.toName(text.TableId).EndsWith("_auto")))
				{
					text.InstanceId = 0u;
					text.TableId = 0u;
				}
			}
		}
	}

	private byte[] unpack(Stream archive, DatabaseIndex index)
	{
		if (index.Compressed)
		{
			archive.Seek(index.Offset, SeekOrigin.Begin);
			return archive.RefPackDecompress(index.CompressedSize, index.DecompressedSize);
		}
		archive.Seek(index.Offset, SeekOrigin.Begin);
		byte[] d = new byte[index.DecompressedSize];
		archive.Read(d, 0, d.Length);
		return d;
	}
}
