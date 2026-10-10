using System;
using System.IO;
using System.Xml;
using Gibbed.Spore.Helpers;
using SporeMaster;

namespace Gibbed.Spore.Properties;

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
			output.WriteAttributeString("groupid", NameRegistry.Groups.toName(GroupId));
		}
		output.WriteAttributeString("instanceid", NameRegistry.Files.toName(InstanceId));
		if (TypeId != 0)
		{
			output.WriteAttributeString("typeid", NameRegistry.Types.toName(TypeId));
		}
	}

	public override void ReadXML(XmlReader input)
	{
		string instanceText = input.GetAttribute("instanceid");
		if (instanceText == null)
		{
			throw new Exception("instanceid cannot be null for key");
		}
		GroupId = NameRegistry.Groups.toHash(input.GetAttribute("groupid"));
		InstanceId = NameRegistry.Files.toHash(instanceText);
		TypeId = NameRegistry.Types.toHash(input.GetAttribute("typeid"));
	}
}
