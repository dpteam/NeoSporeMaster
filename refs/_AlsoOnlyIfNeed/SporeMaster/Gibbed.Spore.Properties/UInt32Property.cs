using System.IO;
using System.Xml;
using Gibbed.Spore.Helpers;

namespace Gibbed.Spore.Properties;

[PropertyDefinition("uint32", "uint32s", 10)]
public class UInt32Property : Property
{
	public uint Value;

	public override void ReadProp(Stream input, bool array)
	{
		Value = input.ReadU32BE();
	}

	public override void WriteProp(Stream output, bool array)
	{
		output.WriteU32BE(Value);
	}

	public override void WriteXML(XmlWriter output)
	{
		output.WriteValue(Value);
	}

	public override void ReadXML(XmlReader input)
	{
		Value = uint.Parse(input.ReadString());
	}
}
