using System;
using System.IO;
using System.Xml;
using Gibbed.Spore.Helpers;

namespace Gibbed.Spore.Properties;

[PropertyDefinition("bbox", "bboxes", 57)]
internal class BoundingBoxProperty : ComplexProperty
{
	public float MinX;

	public float MinY;

	public float MinZ;

	public float MaxX;

	public float MaxY;

	public float MaxZ;

	public override void ReadProp(Stream input, bool array)
	{
		MinX = input.ReadF32();
		MinY = input.ReadF32();
		MinZ = input.ReadF32();
		MaxX = input.ReadF32();
		MaxY = input.ReadF32();
		MaxZ = input.ReadF32();
	}

	public override void WriteProp(Stream output, bool array)
	{
		output.WriteF32(MinX);
		output.WriteF32(MinY);
		output.WriteF32(MinZ);
		output.WriteF32(MaxX);
		output.WriteF32(MaxY);
		output.WriteF32(MaxZ);
	}

	public override void WriteXML(XmlWriter output)
	{
		output.WriteStartElement("min");
		output.WriteValue($"{MinX}, {MinY}, {MinZ}");
		output.WriteEndElement();
		output.WriteStartElement("max");
		output.WriteValue($"{MaxX}, {MaxY}, {MaxZ}");
		output.WriteEndElement();
	}

	public override void ReadXML(XmlReader input)
	{
		input.ReadToDescendant("min");
		string[] s = input.ReadString().Split(',');
		if (s.Length != 3)
		{
			throw new FormatException("Bounding box vector value formatted incorrectly.");
		}
		MinX = float.Parse(s[0].Trim());
		MinY = float.Parse(s[1].Trim());
		MinZ = float.Parse(s[2].Trim());
		input.ReadToNextSibling("max");
		s = input.ReadString().Split(',');
		if (s.Length != 3)
		{
			throw new FormatException("Bounding box vector value formatted incorrectly.");
		}
		MaxX = float.Parse(s[0].Trim());
		MaxY = float.Parse(s[1].Trim());
		MaxZ = float.Parse(s[2].Trim());
	}
}
