using System.IO;
using System.Text;
using System.Xml;
using Gibbed.Spore.Helpers;

namespace Gibbed.Spore.Properties
{
	[PropertyDefinition("string16", "string16s", 19)]
	public class String16Property : Property
	{
		public string Value;

		public override void ReadProp(Stream input, bool array)
		{
			int num = input.ReadS32BE();
			byte[] array2 = new byte[num * 2];
			input.Read(array2, 0, num * 2);
			Value = Encoding.Unicode.GetString(array2);
		}

		public override void WriteProp(Stream output, bool array)
		{
			output.WriteS32BE(Value.Length);
			byte[] bytes = Encoding.Unicode.GetBytes(Value);
			output.Write(bytes, 0, bytes.Length);
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
}
