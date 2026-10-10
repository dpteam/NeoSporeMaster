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

internal class PackagePack
{
	public PackagePack(Stream output, string sourceFolder, PleaseWait progress)
	{
		NameRegistry.Files.UsedHashes = new List<uint>();
		string[] group_dirs = Directory.GetDirectories(sourceFolder);
		Func<string, bool> predicate = (string d) => d != sourceFolder + "\\sporemaster\\";
		List<string> files = (from d in group_dirs.Where(predicate)
			from f2 in Directory.GetFileSystemEntries(d)
			let f = f2
			where !f.EndsWith(".search_index")
			select f).ToList();
		files.Add(sourceFolder + "\\sporemaster\\names.txt");
		progress?.beginTask(1.0, files.Count);
		DatabasePackedFile dbf = new DatabasePackedFile();
		dbf.Version = new Version(2, 0);
		dbf.WriteHeader(output, 0, 0);
		uint rw4_hash = NameRegistry.Types.toHash("rw4");
		uint start = (uint)output.Position;
		uint size;
		foreach (string f in files)
		{
			string relativePath = f.Substring(sourceFolder.Length + 1);
			byte[] autoLocale = null;
			bool additionalOutputFiles;
			do
			{
				additionalOutputFiles = false;
				string[] parts = relativePath.Split('\\');
				if (parts.Length != 2)
				{
					continue;
				}
				string group = parts[0];
				parts = parts[1].Split(new char[1] { '.' }, 2);
				string instance = parts[0];
				string extension = parts[1];
				DatabaseIndex index = new DatabaseIndex
				{
					GroupId = NameRegistry.Groups.toHash(group),
					InstanceId = NameRegistry.Files.toHash(instance)
				};
				try
				{
					if (relativePath == "sporemaster\\names.txt")
					{
						writeNamesFile(output);
					}
					else if (autoLocale != null)
					{
						output.Write(autoLocale, 0, autoLocale.Length);
					}
					else if (extension == "prop.xml")
					{
						extension = "prop";
						writePropFile(group, instance, f, output, out autoLocale);
						if (autoLocale.Length != 0)
						{
							additionalOutputFiles = true;
							relativePath = "locale~\\auto_" + group + "_" + instance + ".locale";
						}
					}
					else if (NameRegistry.Types.toHash(extension) == rw4_hash && Directory.Exists(f))
					{
						writeRW4File(f, output);
					}
					else
					{
						writeBinaryFile(f, output);
					}
				}
				catch (Exception innerException)
				{
					throw new Exception("Error packing file '" + relativePath + "'.", innerException);
				}
				size = (uint)(int)output.Position - start;
				index.TypeId = NameRegistry.Types.toHash(extension);
				index.Compressed = false;
				index.Flags = 1;
				index.DecompressedSize = size;
				index.CompressedSize = size | 0x80000000u;
				index.Offset = start;
				dbf.Indices.Add(index);
				start += size;
			}
			while (additionalOutputFiles);
			progress.addProgress(1.0);
		}
		dbf.WriteIndex(output);
		size = (uint)(int)output.Position - start;
		output.Seek(0L, SeekOrigin.Begin);
		dbf.WriteHeader(output, (int)start, (int)size);
		output.Close();
		progress?.endTask();
	}

	private void writeRW4File(string inputDirName, Stream output)
	{
		string[] files = Directory.GetFiles(inputDirName);
		if (files.Contains(inputDirName + "\\model.mesh.xml"))
		{
			using (MemoryStream stream = new MemoryStream())
			{
				new ModelPack(inputDirName + "\\model.mesh.xml", stream);
				byte[] data = stream.GetBuffer();
				output.Write(data, 0, (int)stream.Length);
				return;
			}
		}
		if (files.Contains(inputDirName + "\\texture.dds"))
		{
			using (MemoryStream stream2 = new MemoryStream())
			{
				new DDSPack(inputDirName + "\\texture.dds", stream2);
				byte[] data2 = stream2.GetBuffer();
				output.Write(data2, 0, (int)stream2.Length);
				return;
			}
		}
		if (files.Contains(inputDirName + "\\raw.rw4"))
		{
			writeBinaryFile(inputDirName + "\\raw.rw4", output);
			return;
		}
		throw new Exception("Nothing present from which I can build a rw4 file.");
	}

	private void writePropFile(string groupName, string instanceName, string inputFileName, Stream output, out byte[] locale)
	{
		XmlReader reader = XmlReader.Create(File.OpenText(inputFileName));
		PropertyFile file = new PropertyFile();
		file.ReadXML(reader);
		reader.Close();
		locale = generateLocale(file, groupName, instanceName);
		file.Write(output);
	}

	private byte[] generateLocale(PropertyFile file, string group, string instance)
	{
		StringBuilder locale = new StringBuilder();
		uint locale_index = 0u;
		string locale_name = "auto_" + group + "_" + instance;
		foreach (Property prop1 in file.Values.Values)
		{
			if (!(prop1 is ArrayProperty arr))
			{
				continue;
			}
			foreach (Property prop2 in arr.Values)
			{
				if (prop2 is TextProperty { InstanceId: 0u, TableId: 0u } text)
				{
					if (locale_index == 0)
					{
						locale.Append("# This file generated from " + group + "/" + instance + ".prop.xml by SporeMaster.\n");
					}
					text.TableId = NameRegistry.Files.toHash(locale_name);
					locale_index = (text.InstanceId = locale_index + 1);
					locale.AppendFormat("0x{0:X8} {1}\n", text.InstanceId, text.PlaceholderText);
				}
			}
		}
		return Encoding.UTF8.GetBytes(locale.ToString());
	}

	private void writeBinaryFile(string inputFileName, Stream output)
	{
		byte[] data = File.ReadAllBytes(inputFileName);
		output.Write(data, 0, data.Length);
	}

	private void writeNamesFile(Stream output)
	{
		foreach (uint h in NameRegistry.Files.UsedHashes)
		{
			string name = NameRegistry.Files.toName(h);
			if (!name.StartsWith("#"))
			{
				string line = ((name.FNV() != h) ? (name + "\t0x" + h.ToString("X8")) : name);
				byte[] bytes = Encoding.UTF8.GetBytes(line + "\r\n");
				output.Write(bytes, 0, bytes.Length);
			}
		}
	}
}
