using System.IO;
using System.Text;
using System.Xml;
using Gibbed.Spore.Helpers;

namespace Gibbed.Spore.Properties;

[PropertyDefinition("string16", "string16s", 19)]
public class String16Property : Property
{
	public string Value;

	public override void ReadProp(Stream input, bool array)
	{
		int length = input.ReadS32BE();
		byte[] data = new byte[length * 2];
		input.Read(data, 0, length * 2);
		Value = Encoding.Unicode.GetString(data);
	}

	public override void WriteProp(Stream output, bool array)
	{
		output.WriteS32BE(Value.Length);
		byte[] data = Encoding.Unicode.GetBytes(Value);
		output.Write(data, 0, data.Length);
	}

	public override void WriteXML(XmlWriter output)
	{
		output.WriteValue(Value);
	}

	public override void ReadXML(XmlReader input)
	{
		Value = input.ReadString();
	}
}
