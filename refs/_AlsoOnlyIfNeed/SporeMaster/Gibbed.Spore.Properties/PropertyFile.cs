using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Gibbed.Spore.Helpers;
using SporeMaster;

namespace Gibbed.Spore.Properties;

public class PropertyFile
{
	public Dictionary<uint, Property> Values = new Dictionary<uint, Property>();

	private PropertyFactory PropTypes = PropertyFactory.Singleton;

	public void Read(Stream input)
	{
		uint count = input.ReadU32BE();
		uint lasthash = 0u;
		for (uint i = 0u; i < count; i++)
		{
			uint hash = input.ReadU32BE();
			ushort fileType = input.ReadU16BE();
			ushort flags = input.ReadU16BE();
			if (i != 0 && hash < lasthash)
			{
				throw new Exception("property file has keys out of order: " + lasthash.ToString("X8") + ", " + hash.ToString("X8"));
			}
			lasthash = hash;
			if (Values.ContainsKey(hash))
			{
				throw new Exception("property file already has " + hash.ToString("X8") + " defined");
			}
			Property property = null;
			Type type = PropTypes.GetTypeFromFileType(fileType);
			if (type == null)
			{
				throw new Exception("invalid type " + fileType);
			}
			if ((flags & 0x30) == 0)
			{
				property = Activator.CreateInstance(type) as Property;
				property.ReadProp(input, array: false);
				Values[hash] = property;
			}
			else
			{
				if ((flags & 0x40) != 0)
				{
					continue;
				}
				ArrayProperty array = new ArrayProperty();
				array.PropertyType = type;
				int arrayCount = input.ReadS32BE();
				int arrayItemSize = input.ReadS32BE();
				for (uint j = 0u; j < arrayCount; j++)
				{
					Property subproperty = Activator.CreateInstance(type) as Property;
					if (subproperty is ComplexProperty)
					{
						MemoryStream memory = new MemoryStream();
						byte[] data = new byte[arrayItemSize];
						input.Read(data, 0, arrayItemSize);
						memory.Write(data, 0, arrayItemSize);
						memory.Seek(0L, SeekOrigin.Begin);
						subproperty.ReadProp(memory, array: true);
					}
					else
					{
						subproperty.ReadProp(input, array: true);
					}
					array.Values.Add(subproperty);
				}
				property = array;
				Values[hash] = property;
			}
		}
	}

	public void Write(Stream output)
	{
		output.WriteS32BE(Values.Count);
		IOrderedEnumerable<uint> keys = Values.Keys.OrderBy((uint k) => k);
		foreach (uint hash in keys)
		{
			Property property = Values[hash];
			output.WriteU32BE(hash);
			if (property is ArrayProperty)
			{
				ArrayProperty array = (ArrayProperty)property;
				output.WriteU16BE(PropTypes.FindPropertyType(array.PropertyType).FileType);
				output.WriteU16BE(48);
			}
			else if (property is ComplexProperty)
			{
				output.WriteU16BE(PropTypes.FindPropertyType(Values[hash].GetType()).FileType);
				output.WriteU16BE(48);
			}
			else
			{
				output.WriteU16BE(PropTypes.FindPropertyType(Values[hash].GetType()).FileType);
				output.WriteU16BE(0);
			}
			if (property is ArrayProperty)
			{
				ArrayProperty array2 = (ArrayProperty)property;
				MemoryStream[] memories = new MemoryStream[array2.Values.Count];
				int index = 0;
				uint maxSize = 0u;
				foreach (Property subproperty in array2.Values)
				{
					memories[index] = new MemoryStream();
					subproperty.WriteProp(memories[index], array: true);
					if (memories[index].Length > maxSize)
					{
						maxSize = (uint)memories[index].Length;
					}
					index++;
				}
				output.WriteU32BE((uint)memories.Length);
				output.WriteU32BE(maxSize);
				for (int i = 0; i < memories.Length; i++)
				{
					byte[] data = new byte[maxSize];
					memories[i].Seek(0L, SeekOrigin.Begin);
					memories[i].Read(data, 0, (int)memories[i].Length);
					output.Write(data, 0, data.Length);
				}
			}
			else
			{
				MemoryStream memory = new MemoryStream();
				property.WriteProp(memory, array: false);
				memory.Seek(0L, SeekOrigin.Begin);
				if (property is ComplexProperty)
				{
					output.WriteU32BE(1u);
					output.WriteU32BE((uint)memory.Length);
				}
				byte[] data2 = new byte[memory.Length];
				memory.Read(data2, 0, (int)memory.Length);
				output.Write(data2, 0, data2.Length);
			}
		}
	}

	public void ReadXML(XmlReader reader)
	{
		reader.ReadToFollowing("properties");
		while (reader.Read())
		{
			if (!reader.IsStartElement())
			{
				continue;
			}
			string typeName = reader.Name;
			uint hash = reader.GetAttribute("id")?.GetHexNumber() ?? NameRegistry.Properties.toHash(reader.GetAttribute("name"));
			PropertyLookup lookup = PropTypes.FindPropertyType(typeName);
			if (lookup == null)
			{
				throw new Exception("unknown type " + typeName);
			}
			Property property = null;
			if (lookup.Definition.Name == typeName)
			{
				property = Activator.CreateInstance(lookup.Type) as Property;
				property.ReadXML(reader);
			}
			else if (lookup.Definition.PluralName == typeName)
			{
				ArrayProperty array = new ArrayProperty();
				array.PropertyType = lookup.Type;
				XmlReader subtree = reader.ReadSubtree();
				subtree.ReadToFollowing(typeName);
				while (subtree.Read())
				{
					if (subtree.IsStartElement())
					{
						if (subtree.Name != lookup.Definition.Name)
						{
							throw new Exception("array element for " + typeName + " is not the right type");
						}
						Property subproperty = Activator.CreateInstance(lookup.Type) as Property;
						subproperty.ReadXML(subtree);
						array.Values.Add(subproperty);
					}
				}
				property = array;
			}
			Values[hash] = property;
		}
	}

	public void WriteXML(XmlWriter writer)
	{
		writer.WriteStartDocument();
		writer.WriteStartElement("properties");
		IEnumerable<uint> keys = from _003C_003Eh__TransparentIdentifier2 in Values.Keys.Select((uint k) => new
			{
				k = k,
				n = NameRegistry.Properties.toName(k).ToLowerInvariant()
			}).OrderBy(_003C_003Eh__TransparentIdentifier2 =>
			{
				string result = ((_003C_003Eh__TransparentIdentifier2.k != 11717834) ? ((_003C_003Eh__TransparentIdentifier2.k != 11717835) ? (_003C_003Eh__TransparentIdentifier2.n.StartsWith("#") ? ("3" + _003C_003Eh__TransparentIdentifier2.n) : ("2" + _003C_003Eh__TransparentIdentifier2.n)) : "1") : "0");
				return result;
			})
			select _003C_003Eh__TransparentIdentifier2.k;
		foreach (uint hash in keys)
		{
			Property property = Values[hash];
			if (!(property is ArrayProperty))
			{
				writer.WriteStartElement(property.GetType().GetSingularName());
			}
			else
			{
				ArrayProperty array = (ArrayProperty)property;
				writer.WriteStartElement(array.PropertyType.GetPluralName());
			}
			string name = NameRegistry.Properties.toName(hash);
			writer.WriteAttributeString("name", name);
			property.WriteXML(writer);
			writer.WriteEndElement();
		}
		writer.WriteEndDocument();
	}
}
