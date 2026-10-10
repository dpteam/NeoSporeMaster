using System;
using System.Collections.Generic;
using System.Reflection;

namespace Gibbed.Spore.Properties;

public class PropertyFactory
{
	private static PropertyFactory instance;

	private Dictionary<ushort, PropertyLookup> PropertyTypes;

	public static PropertyFactory Singleton
	{
		get
		{
			if (instance == null)
			{
				instance = new PropertyFactory();
			}
			return instance;
		}
	}

	private PropertyFactory()
	{
		PropertyTypes = new Dictionary<ushort, PropertyLookup>();
		Type[] types = Assembly.GetAssembly(GetType()).GetTypes();
		foreach (Type type in types)
		{
			if (!type.IsSubclassOf(typeof(Property)))
			{
				continue;
			}
			object[] attributes = type.GetCustomAttributes(typeof(PropertyDefinitionAttribute), inherit: false);
			if (attributes.Length > 0)
			{
				PropertyDefinitionAttribute propDef = (PropertyDefinitionAttribute)attributes[0];
				if (PropertyTypes.ContainsKey(propDef.FileType))
				{
					throw new Exception("duplicate property type id " + propDef.FileType);
				}
				PropertyTypes[propDef.FileType] = new PropertyLookup();
				PropertyTypes[propDef.FileType].Type = type;
				PropertyTypes[propDef.FileType].Definition = propDef;
			}
		}
	}

	public PropertyDefinitionAttribute FindPropertyType(Type type)
	{
		foreach (PropertyLookup lookup in PropertyTypes.Values)
		{
			if (lookup.Type == type)
			{
				return lookup.Definition;
			}
		}
		return null;
	}

	public PropertyLookup FindPropertyType(string name)
	{
		foreach (PropertyLookup lookup in PropertyTypes.Values)
		{
			if (lookup.Definition.Name == name || lookup.Definition.PluralName == name)
			{
				return lookup;
			}
		}
		return null;
	}

	public Type GetTypeFromFileType(ushort dataType)
	{
		if (PropertyTypes.ContainsKey(dataType))
		{
			return PropertyTypes[dataType].Type;
		}
		return null;
	}
}
