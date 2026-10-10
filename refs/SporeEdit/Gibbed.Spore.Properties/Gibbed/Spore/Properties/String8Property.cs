using System.IO;
using System.Text;
using System.Xml;
using Gibbed.Spore.Helpers;

namespace Gibbed.Spore.Properties
{
	[PropertyDefinition("string8", "string8s", 18)]
	public class String8Property : Property
	{
		public string Value;

		public override void ReadProp(Stream input, bool array)
		{
			uint num = input.ReadU32BE();
			byte[] array2 = new byte[num];
			input.Read(array2, 0, array2.Length);
			Value = Encoding.UTF8.GetString(array2);
		}

		public override void WriteProp(Stream output, bool array)
		{
			output.WriteS32BE(Value.Length);
			byte[] bytes = Encoding.UTF8.GetBytes(Value);
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
