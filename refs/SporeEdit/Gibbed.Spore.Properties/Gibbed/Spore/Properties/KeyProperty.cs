using System;
using System.IO;
using System.Xml;
using Gibbed.Spore.Helpers;

namespace Gibbed.Spore.Properties
{
	[PropertyDefinition("key", "keys", 32)]
	public class KeyProperty : Property
	{
		public uint TypeId;

		public uint GroupId;

		public uint InstanceId;

		public override void ReadProp(Stream input, bool array)
		{
			InstanceId = input.ReadU32();
			TypeId = input.ReadU32();
			GroupId = input.ReadU32();
			if (!array)
			{
				input.Seek(4L, SeekOrigin.Current);
			}
		}

		public override void WriteProp(Stream output, bool array)
		{
			output.WriteU32(InstanceId);
			output.WriteU32(TypeId);
			output.WriteU32(GroupId);
			if (!array)
			{
				output.WriteU32(0u);
			}
		}

		public override void WriteXML(XmlWriter output)
		{
			if (GroupId != 0)
			{
				output.WriteAttributeString("groupid", "0x" + GroupId.ToString("X8"));
			}
			output.WriteAttributeString("instanceid", "0x" + InstanceId.ToString("X8"));
			if (TypeId != 0)
			{
				output.WriteAttributeString("typeid", "0x" + TypeId.ToString("X8"));
			}
		}

		public override void ReadXML(XmlReader input)
		{
			string attribute = input.GetAttribute("groupid");
			string attribute2 = input.GetAttribute("instanceid");
			string attribute3 = input.GetAttribute("typeid");
			if (attribute == null)
			{
				GroupId = 0u;
			}
			else
			{
				GroupId = attribute.GetHexNumber();
			}
			if (attribute2 == null)
			{
				throw new Exception("instanceid cannot be null for key");
			}
			InstanceId = attribute2.GetHexNumber();
			if (attribute3 == null)
			{
				TypeId = 0u;
			}
			else
			{
				TypeId = attribute3.GetHexNumber();
			}
		}
	}
}
