using System.IO;
using System.Xml;
using Gibbed.Spore.Helpers;

namespace Gibbed.Spore.Properties
{
	[PropertyDefinition("int32", "int32s", 9)]
	public class Int32Property : Property
	{
		public int Value;

		public override void ReadProp(Stream input, bool array)
		{
			Value = input.ReadS32BE();
		}

		public override void WriteProp(Stream output, bool array)
		{
			output.WriteS32BE(Value);
		}

		public override void WriteXML(XmlWriter output)
		{
			output.WriteValue(Value);
		}

		public override void ReadXML(XmlReader input)
		{
			Value = int.Parse(input.ReadString());
		}
	}
}
