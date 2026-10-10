using System;

namespace Gibbed.Spore.Properties;

public class PropertyDefinitionAttribute : Attribute
{
	public string Name;

	public string PluralName;

	public ushort FileType;

	public PropertyDefinitionAttribute(string name, string pluralName, ushort fileType)
	{
		if (name == pluralName)
		{
			throw new Exception("singular name cannot be the same as the plural name");
		}
		Name = name;
		PluralName = pluralName;
		FileType = fileType;
	}
}
