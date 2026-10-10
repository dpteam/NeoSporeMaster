using System.IO;
using System.Xml;
using Gibbed.Spore.Helpers;

namespace Gibbed.Spore.Properties
{
	[PropertyDefinition("vector3", "vector3s", 49)]
	public class Vector3Property : Property
	{
		public float X;

		public float Y;

		public float Z;

		public override void ReadProp(Stream input, bool array)
		{
			X = input.ReadF32();
			Y = input.ReadF32();
			Z = input.ReadF32();
			if (!array)
			{
				input.Seek(4L, SeekOrigin.Current);
			}
		}

		public override void WriteProp(Stream output, bool array)
		{
			output.WriteF32(X);
			output.WriteF32(Y);
			output.WriteF32(Z);
			if (!array)
			{
				output.WriteU32(0u);
			}
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
		}

		public override void ReadXML(XmlReader input)
		{
			XmlReader xmlReader = input.ReadSubtree();
			xmlReader.ReadToFollowing("x");
			X = xmlReader.ReadElementContentAsFloat("x", "");
			xmlReader.ReadToFollowing("y");
			Y = xmlReader.ReadElementContentAsFloat("y", "");
			xmlReader.ReadToFollowing("z");
			Z = xmlReader.ReadElementContentAsFloat("z", "");
		}
	}
}
