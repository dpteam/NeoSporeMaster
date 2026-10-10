using System;
using System.IO;
using System.Text;
using System.Xml;
using Gibbed.Spore.Helpers;
using SporeMaster;

namespace Gibbed.Spore.Properties;

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
			int size = (int)input.Length - 8;
			byte[] data = new byte[size];
			input.Read(data, 0, size);
			if ((size - 8) % 2 != 0)
			{
				throw new Exception("array size is not a multiple of two");
			}
			int end = 0;
			for (int i = 0; i < size - 8; i += 2)
			{
				if (data[i] == 0 && data[i + 1] == 0)
				{
					end = i;
					break;
				}
			}
			PlaceholderText = Encoding.Unicode.GetString(data, 0, end);
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
			byte[] data = Encoding.Unicode.GetBytes(PlaceholderText);
			output.Write(data, 0, data.Length);
			if (data.Length < 512)
			{
				data = new byte[512 - data.Length];
				output.Write(data, 0, data.Length);
			}
			return;
		}
		throw new NotImplementedException();
	}

	public override void WriteXML(XmlWriter output)
	{
		if (TableId != 0)
		{
			output.WriteAttributeString("tableid", NameRegistry.Files.toName(TableId));
		}
		if (InstanceId != 0)
		{
			output.WriteAttributeString("instanceid", NameRegistry.Files.toName(InstanceId));
		}
		output.WriteValue(PlaceholderText);
	}

	public override void ReadXML(XmlReader input)
	{
		string tid = input.GetAttribute("tableid");
		string iid = input.GetAttribute("instanceid");
		TableId = ((tid != null) ? NameRegistry.Files.toHash(tid) : 0u);
		InstanceId = ((iid != null) ? NameRegistry.Files.toHash(iid) : 0u);
		PlaceholderText = input.ReadString();
	}
}
