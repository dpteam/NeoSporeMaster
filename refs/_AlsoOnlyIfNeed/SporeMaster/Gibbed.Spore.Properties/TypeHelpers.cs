using System;

namespace Gibbed.Spore.Properties;

public static class TypeHelpers
{
	private static PropertyDefinitionAttribute GetPropertyDefinition(this Type type)
	{
		if (!type.IsSubclassOf(typeof(Property)))
		{
			return null;
		}
		object[] attributes = type.GetCustomAttributes(typeof(PropertyDefinitionAttribute), inherit: false);
		if (attributes.Length == 0)
		{
			return null;
		}
		return (PropertyDefinitionAttribute)attributes[0];
	}

	public static string GetSingularName(this Type type)
	{
		return type.GetPropertyDefinition()?.Name;
	}

	public static string GetPluralName(this Type type)
	{
		return type.GetPropertyDefinition()?.PluralName;
	}
}
