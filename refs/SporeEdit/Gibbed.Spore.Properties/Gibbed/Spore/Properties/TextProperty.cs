using System;
using System.IO;
using System.Text;
using System.Xml;
using Gibbed.Spore.Helpers;

namespace Gibbed.Spore.Properties
{
	[PropertyDefinition("text", "texts", 34)]
	public class TextProperty : ComplexProperty
	{
		public uint TableId;

		public uint InstanceId;

		public string PlaceholderText;

		public override void ReadProp(Stream input, bool array)
		{
			if (array)
			{
				TableId = input.ReadU32();
				InstanceId = input.ReadU32();
				int num = (int)input.Length - 8;
				byte[] array2 = new byte[num];
				input.Read(array2, 0, num);
				if ((num - 8) % 2 != 0)
				{
					throw new Exception("array size is not a multiple of two");
				}
				int count = 0;
				for (int i = 0; i < num - 8; i += 2)
				{
					if (array2[i] == 0 && array2[i + 1] == 0)
					{
						count = i;
						break;
					}
				}
				PlaceholderText = Encoding.Unicode.GetString(array2, 0, count);
				return;
			}
			throw new NotImplementedException();
		}

		public override void WriteProp(Stream output, bool array)
		{
			if (array)
			{
				output.WriteU32(TableId);
				output.WriteU32(InstanceId);
				byte[] bytes = Encoding.Unicode.GetBytes(PlaceholderText);
				output.Write(bytes, 0, bytes.Length);
				if (bytes.Length < 512)
				{
					bytes = new byte[512 - bytes.Length];
					output.Write(bytes, 0, bytes.Length);
				}
				return;
			}
			throw new NotImplementedException();
		}

		public override void WriteXML(XmlWriter output)
		{
			output.WriteAttributeString("tableid", "0x" + TableId.ToString("X8"));
			output.WriteAttributeString("instanceid", "0x" + InstanceId.ToString("X8"));
			output.WriteValue(PlaceholderText);
		}

		public override void ReadXML(XmlReader input)
		{
			TableId = input.GetAttribute("tableid").GetHexNumber();
			InstanceId = input.GetAttribute("instanceid").GetHexNumber();
			PlaceholderText = input.ReadString();
		}
	}
}
