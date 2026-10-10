using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Gibbed.Spore.Helpers;

namespace Gibbed.Spore.Properties
{
	public class PropertyFile
	{
		public Dictionary<uint, Property> Values = new Dictionary<uint, Property>();

		private Dictionary<ushort, PropertyLookup> PropertyTypes;

		public PropertyFile()
		{
			BuildFileTypes();
		}

		private void BuildFileTypes()
		{
			PropertyTypes = new Dictionary<ushort, PropertyLookup>();
			Type[] types = Assembly.GetAssembly(GetType()).GetTypes();
			foreach (Type type in types)
			{
				if (!type.IsSubclassOf(typeof(Property)))
				{
					continue;
				}
				object[] customAttributes = type.GetCustomAttributes(typeof(PropertyDefinitionAttribute), inherit: false);
				if (customAttributes.Length > 0)
				{
					PropertyDefinitionAttribute propertyDefinitionAttribute = (PropertyDefinitionAttribute)customAttributes[0];
					if (PropertyTypes.ContainsKey(propertyDefinitionAttribute.FileType))
					{
						throw new Exception("duplicate property type id " + propertyDefinitionAttribute.FileType);
					}
					PropertyTypes[propertyDefinitionAttribute.FileType] = new PropertyLookup();
					PropertyTypes[propertyDefinitionAttribute.FileType].Type = type;
					PropertyTypes[propertyDefinitionAttribute.FileType].Definition = propertyDefinitionAttribute;
				}
			}
		}

		public PropertyDefinitionAttribute FindPropertyType(Type type)
		{
			foreach (PropertyLookup value in PropertyTypes.Values)
			{
				if (value.Type == type)
				{
					return value.Definition;
				}
			}
			return null;
		}

		public PropertyLookup FindPropertyType(string name)
		{
			foreach (PropertyLookup value in PropertyTypes.Values)
			{
				if (value.Definition.Name == name || value.Definition.PluralName == name)
				{
					return value;
				}
			}
			return null;
		}

		private Type GetTypeFromFileType(ushort dataType)
		{
			if (PropertyTypes.ContainsKey(dataType))
			{
				return PropertyTypes[dataType].Type;
			}
			return null;
		}

		public void Read(Stream input)
		{
			uint num = input.ReadU32BE();
			for (uint num2 = 0u; num2 < num; num2++)
			{
				uint key = input.ReadU32BE();
				ushort dataType = input.ReadU16BE();
				ushort num3 = input.ReadU16BE();
				if (Values.ContainsKey(key))
				{
					throw new Exception("property file already has " + key.ToString("X8") + " defined");
				}
				Property property = null;
				Type typeFromFileType = GetTypeFromFileType(dataType);
				if (typeFromFileType == null)
				{
					throw new Exception("invalid type " + dataType);
				}
				if ((num3 & 0x30) == 0)
				{
					property = Activator.CreateInstance(typeFromFileType) as Property;
					property.ReadProp(input, array: false);
					Values[key] = property;
				}
				else
				{
					if ((num3 & 0x40u) != 0)
					{
						continue;
					}
					ArrayProperty arrayProperty = new ArrayProperty();
					arrayProperty.PropertyType = typeFromFileType;
					int num4 = input.ReadS32BE();
					int num5 = input.ReadS32BE();
					for (uint num6 = 0u; num6 < num4; num6++)
					{
						Property property2 = Activator.CreateInstance(typeFromFileType) as Property;
						if (property2 is ComplexProperty)
						{
							MemoryStream memoryStream = new MemoryStream();
							byte[] buffer = new byte[num5];
							input.Read(buffer, 0, num5);
							memoryStream.Write(buffer, 0, num5);
							memoryStream.Seek(0L, SeekOrigin.Begin);
							property2.ReadProp(memoryStream, array: true);
						}
						else
						{
							property2.ReadProp(input, array: true);
						}
						arrayProperty.Values.Add(property2);
					}
					property = arrayProperty;
					Values[key] = property;
				}
			}
		}

		public void Write(Stream output)
		{
			output.WriteS32BE(Values.Count);
			foreach (uint key in Values.Keys)
			{
				Property property = Values[key];
				output.WriteU32BE(key);
				if (property is ArrayProperty)
				{
					ArrayProperty arrayProperty = (ArrayProperty)property;
					output.WriteU16BE(FindPropertyType(arrayProperty.PropertyType).FileType);
					output.WriteU16BE(48);
				}
				else if (property is ComplexProperty)
				{
					output.WriteU16BE(FindPropertyType(Values[key].GetType()).FileType);
					output.WriteU16BE(48);
				}
				else
				{
					output.WriteU16BE(FindPropertyType(Values[key].GetType()).FileType);
					output.WriteU16BE(0);
				}
				if (property is ArrayProperty)
				{
					ArrayProperty arrayProperty2 = (ArrayProperty)property;
					MemoryStream[] array = new MemoryStream[arrayProperty2.Values.Count];
					int num = 0;
					uint num2 = 0u;
					foreach (Property value in arrayProperty2.Values)
					{
						array[num] = new MemoryStream();
						value.WriteProp(array[num], array: true);
						if (array[num].Length > num2)
						{
							num2 = (uint)array[num].Length;
						}
						num++;
					}
					output.WriteU32BE((uint)array.Length);
					output.WriteU32BE(num2);
					for (int i = 0; i < array.Length; i++)
					{
						byte[] array2 = new byte[num2];
						array[i].Seek(0L, SeekOrigin.Begin);
						array[i].Read(array2, 0, (int)array[i].Length);
						output.Write(array2, 0, array2.Length);
					}
				}
				else
				{
					MemoryStream memoryStream = new MemoryStream();
					property.WriteProp(memoryStream, array: false);
					memoryStream.Seek(0L, SeekOrigin.Begin);
					if (property is ComplexProperty)
					{
						output.WriteU32BE(1u);
						output.WriteU32BE((uint)memoryStream.Length);
					}
					byte[] array3 = new byte[memoryStream.Length];
					memoryStream.Read(array3, 0, (int)memoryStream.Length);
					output.Write(array3, 0, array3.Length);
				}
			}
		}
	}
}
