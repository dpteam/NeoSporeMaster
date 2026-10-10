using System;
using System.IO;
using System.Xml;
using Gibbed.Spore.Helpers;

namespace Gibbed.Spore.Properties;

[PropertyDefinition("transform", "transforms", 56)]
public class TransformProperty : ComplexProperty
{
	public uint Flags;

	public float[] Matrix = new float[12];

	public override void ReadProp(Stream input, bool array)
	{
		Flags = input.ReadU32BE();
		for (int i = 0; i < 12; i++)
		{
			Matrix[i] = input.ReadF32();
		}
	}

	public override void WriteProp(Stream output, bool array)
	{
		output.WriteU32BE(Flags);
		for (int i = 0; i < 12; i++)
		{
			output.WriteF32(Matrix[i]);
		}
	}

	public override void WriteXML(XmlWriter output)
	{
		output.WriteAttributeString("flags", "0x" + Flags.ToString("X8"));
		output.WriteAttributeString("pos", $"{Matrix[0]},{Matrix[1]},{Matrix[2]}");
		output.WriteAttributeString("vx", $"{Matrix[3]},{Matrix[6]},{Matrix[9]}");
		output.WriteAttributeString("vy", $"{Matrix[4]},{Matrix[7]},{Matrix[10]}");
		output.WriteAttributeString("vz", $"{Matrix[5]},{Matrix[8]},{Matrix[11]}");
	}

	public override void ReadXML(XmlReader input)
	{
		if (!input.HasAttributes)
		{
			string[] s = input.ReadString().Split(' ');
			if (s.Length != 13)
			{
				throw new FormatException("Legacy transform format incorrect.");
			}
			Flags = s[0].GetHexNumber();
			for (int i = 0; i < 12; i++)
			{
				Matrix[i] = float.Parse(s[i + 1]);
			}
		}
		else
		{
			Flags = input.GetAttribute("flags").GetHexNumber();
			parseV(input.GetAttribute("pos"), out Matrix[0], out Matrix[1], out Matrix[2]);
			parseV(input.GetAttribute("vx"), out Matrix[3], out Matrix[6], out Matrix[9]);
			parseV(input.GetAttribute("vy"), out Matrix[4], out Matrix[7], out Matrix[10]);
			parseV(input.GetAttribute("vz"), out Matrix[5], out Matrix[8], out Matrix[11]);
		}
	}

	private static void parseV(string input, out float x, out float y, out float z)
	{
		string[] s = input.Split(',');
		if (s.Length != 3)
		{
			throw new FormatException("Bad vector format in transform property.");
		}
		x = float.Parse(s[0]);
		y = float.Parse(s[1]);
		z = float.Parse(s[2]);
	}
}
