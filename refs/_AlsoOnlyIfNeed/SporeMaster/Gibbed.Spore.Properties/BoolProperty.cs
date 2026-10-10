using System.IO;
using System.Xml;
using Gibbed.Spore.Helpers;

namespace Gibbed.Spore.Properties;

[PropertyDefinition("bool", "bools", 1)]
public class BoolProperty : Property
{
	public bool Value;

	public override void ReadProp(Stream input, bool array)
	{
		Value = input.ReadBoolean();
	}

	public override void WriteProp(Stream output, bool array)
	{
		output.WriteBoolean(Value);
	}

	public override void WriteXML(XmlWriter output)
	{
		output.WriteValue(Value);
	}

	public override void ReadXML(XmlReader input)
	{
		Value = bool.Parse(input.ReadString());
	}
}
