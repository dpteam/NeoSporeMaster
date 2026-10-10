using System.IO;
using System.Xml;
using Gibbed.Spore.Helpers;

namespace Gibbed.Spore.Properties
{
	[PropertyDefinition("float", "floats", 13)]
	public class FloatProperty : Property
	{
		public float Value;

		public override void ReadProp(Stream input, bool array)
		{
			Value = input.ReadF32BE();
		}

		public override void WriteProp(Stream output, bool array)
		{
			output.WriteF32BE(Value);
		}

		public override void WriteXML(XmlWriter output)
		{
			output.WriteValue(Value);
		}

		public override void ReadXML(XmlReader input)
		{
			Value = float.Parse(input.ReadString());
		}
	}
}
