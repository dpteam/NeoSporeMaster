using System.IO;
using System.Xml;
using Gibbed.Spore.Helpers;

namespace Gibbed.Spore.Properties;

[PropertyDefinition("vector4", "vector4s", 51)]
internal class Vector4Property : Property
{
	public float X;

	public float Y;

	public float Z;

	public float W;

	public override void ReadProp(Stream input, bool array)
	{
		X = input.ReadF32();
		Y = input.ReadF32();
		Z = input.ReadF32();
		W = input.ReadF32();
	}

	public override void WriteProp(Stream output, bool array)
	{
		output.WriteF32(X);
		output.WriteF32(Y);
		output.WriteF32(Z);
		output.WriteF32(W);
	}

	public override void WriteXML(XmlWriter output)
	{
		output.WriteStartElement("x");
		output.WriteValue(X);
		output.WriteEndElement();
		output.WriteStartElement("y");
		output.WriteValue(Y);
		output.WriteEndElement();
		output.WriteStartElement("z");
		output.WriteValue(Z);
		output.WriteEndElement();
		output.WriteStartElement("w");
		output.WriteValue(W);
		output.WriteEndElement();
	}

	public override void ReadXML(XmlReader input)
	{
		XmlReader subtree = input.ReadSubtree();
		subtree.ReadToFollowing("x");
		X = subtree.ReadElementContentAsFloat("x", "");
		subtree.ReadToFollowing("y");
		Y = subtree.ReadElementContentAsFloat("y", "");
		subtree.ReadToFollowing("z");
		Z = subtree.ReadElementContentAsFloat("z", "");
		subtree.ReadToFollowing("w");
		W = subtree.ReadElementContentAsFloat("w", "");
	}
}
