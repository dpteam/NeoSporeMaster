using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Gibbed.Spore.Helpers;

namespace SporeMaster;

internal class NameRegistry
{
	private Dictionary<uint, string> hash_name = new Dictionary<uint, string>();

	private Dictionary<string, uint> name_hash = new Dictionary<string, uint>();

	private HashSet<uint> usedHashes = new HashSet<uint>();

	private string filename;

	public static NameRegistry Types;

	public static NameRegistry Files;

	public static NameRegistry Groups;

	public static NameRegistry Properties;

	public IEnumerable<uint> UsedHashes
	{
		get
		{
			return usedHashes.AsEnumerable();
		}
		set
		{
			usedHashes.Clear();
			usedHashes.UnionWith(value);
		}
	}

	public bool IsHashUsed(uint hash)
	{
		return usedHashes.Contains(hash);
	}

	public static string getFileName(uint GroupId, uint InstanceId, uint TypeId)
	{
		return Groups.toName(GroupId) + "/" + Files.toName(InstanceId) + "." + Types.toName(TypeId);
	}

	public static void parseFileName(string filename, out uint GroupId, out uint InstanceId, out uint TypeId)
	{
		Match m = Regex.Match(filename, "([^/\\]*)[/\\]([^.]*).(*)");
		if (!m.Success)
		{
			throw new ArgumentException("Invalid file reference.");
		}
		GroupId = Groups.toHash(m.Groups[1].ToString());
		InstanceId = Files.toHash(m.Groups[2].ToString());
		TypeId = Types.toHash(m.Groups[3].ToString());
	}

	public NameRegistry()
	{
	}

	public NameRegistry(string registryFile)
	{
		filename = registryFile;
		readRegistryFile(registryFile, overrideExisting: true);
	}

	public void save()
	{
		writeRegistryFile(filename);
	}

	public void readRegistryFile(string registryFile, bool overrideExisting)
	{
		readRegistry(new StreamReader(registryFile), overrideExisting, null);
	}

	public void readRegistry(TextReader reader, bool overrideExisting, List<uint> outNewHashes)
	{
		while (true)
		{
			string line = reader.ReadLine();
			if (line == null)
			{
				break;
			}
			if (!line.StartsWith("#"))
			{
				string name;
				uint hash;
				if (line.Contains("\t"))
				{
					string[] s = line.Split('\t');
					name = s[0];
					hash = ((!s[1].StartsWith("0x")) ? uint.Parse(s[1]) : uint.Parse(s[1].Substring(2), NumberStyles.AllowHexSpecifier));
				}
				else
				{
					name = line;
					hash = name.FNV();
				}
				if ((overrideExisting || !name_hash.ContainsKey(name)) && addName(name, hash, overrideExisting))
				{
					outNewHashes?.Add(hash);
				}
			}
		}
		reader.Close();
	}

	public void writeRegistryFile(string registryFile)
	{
		List<string> names = new List<string>();
		foreach (KeyValuePair<string, uint> i in name_hash)
		{
			if (hash_name[i.Value] != i.Key)
			{
				names.Add(i.Key);
			}
		}
		names.Add("### Previous items are 'obsolete' ###");
		names.AddRange(hash_name.Values);
		FileStream output = File.Create(registryFile + ".part");
		foreach (string name in names)
		{
			string line = ((!name_hash.ContainsKey(name) || name_hash[name] == name.FNV()) ? (name + "\r\n") : (name + "\t0x" + name_hash[name].ToString("X8") + "\r\n"));
			byte[] b = Encoding.UTF8.GetBytes(line);
			output.Write(b, 0, b.Length);
		}
		output.Close();
		File.Replace(registryFile + ".part", registryFile, registryFile + ".backup");
		File.Delete(registryFile + ".backup");
	}

	public bool addName(string name, uint hash, bool overrideExisting)
	{
		if (hash != name.FNV())
		{
			name_hash[name] = hash;
		}
		else if (name_hash.ContainsKey(name))
		{
			name_hash.Remove(name);
		}
		if (!hash_name.ContainsKey(hash) || (overrideExisting && hash_name[hash].FNV() != hash))
		{
			hash_name[hash] = name;
			return true;
		}
		return false;
	}

	public string toName(uint hash)
	{
		if (hash_name.ContainsKey(hash))
		{
			return hash_name[hash];
		}
		return "#" + hash.ToString("X8");
	}

	public uint toHash(string name)
	{
		uint hash = toHashImpl(name);
		usedHashes.Add(hash);
		return hash;
	}

	private uint toHashImpl(string name)
	{
		if (name == null)
		{
			return 0u;
		}
		if (checkName(name, out var hash))
		{
			return hash;
		}
		if (name.EndsWith("~"))
		{
			throw new Exception("Cannot hash name '" + name + "': ~ is reserved for aliases");
		}
		hash = name.FNV();
		hash_name[hash] = name;
		return hash;
	}

	public bool checkName(string name, out uint hash)
	{
		if (name == null)
		{
			hash = 0u;
			return false;
		}
		if (name.StartsWith("#"))
		{
			hash = uint.Parse(name.Substring(1), NumberStyles.AllowHexSpecifier);
			return true;
		}
		if (name.StartsWith("0x"))
		{
			hash = uint.Parse(name.Substring(2), NumberStyles.AllowHexSpecifier);
			return true;
		}
		if (name_hash.ContainsKey(name))
		{
			hash = name_hash[name];
			return true;
		}
		hash = 0u;
		return false;
	}
}
