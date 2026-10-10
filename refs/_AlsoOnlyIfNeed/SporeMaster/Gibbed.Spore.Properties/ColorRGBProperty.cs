using System.IO;
using System.Xml;
using Gibbed.Spore.Helpers;

namespace Gibbed.Spore.Properties;

[PropertyDefinition("colorRGB", "colorRGBs", 50)]
public class ColorRGBProperty : Property
{
	public float R;

	public float G;

	public float B;

	public override void ReadProp(Stream input, bool array)
	{
		R = input.ReadF32();
		G = input.ReadF32();
		B = input.ReadF32();
		if (!array)
		{
			input.Seek(4L, SeekOrigin.Current);
		}
	}

	public override void WriteProp(Stream output, bool array)
	{
		output.WriteF32(R);
		output.WriteF32(G);
		output.WriteF32(B);
		if (!array)
		{
			output.WriteU32(0u);
		}
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
	}
}
