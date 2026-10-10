using System.IO;
using System.Xml;
using Gibbed.Spore.Helpers;

namespace Gibbed.Spore.Properties;

[PropertyDefinition("colorRGBA", "colorRGBAs", 52)]
public class ColorRGBAProperty : Property
{
	public float R;

	public float G;

	public float B;

	public float A;

	public override void ReadProp(Stream input, bool array)
	{
		R = input.ReadF32();
		G = input.ReadF32();
		B = input.ReadF32();
		A = input.ReadF32();
	}

	public override void WriteProp(Stream output, bool array)
	{
		output.WriteF32(R);
		output.WriteF32(G);
		output.WriteF32(B);
		output.WriteF32(A);
	}

	public override void WriteXML(XmlWriter output)
	{
		output.WriteStartElement("r");
		output.WriteValue(R);
		output.WriteEndElement();
		output.WriteStartElement("g");
		output.WriteValue(G);
		output.WriteEndElement();
		output.WriteStartElement("b");
		output.WriteValue(B);
		output.WriteEndElement();
		output.WriteStartElement("a");
		output.WriteValue(A);
		output.WriteEndElement();
	}

	public override void ReadXML(XmlReader input)
	{
		XmlReader subtree = input.ReadSubtree();
		subtree.ReadToFollowing("r");
		R = subtree.ReadElementContentAsFloat("r", "");
		subtree.ReadToFollowing("g");
		G = subtree.ReadElementContentAsFloat("g", "");
		subtree.ReadToFollowing("b");
		B = subtree.ReadElementContentAsFloat("b", "");
		subtree.ReadToFollowing("a");
		A = subtree.ReadElementContentAsFloat("a", "");
	}
}
