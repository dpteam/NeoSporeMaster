using System;
using System.IO;
using System.Xml;
using Gibbed.Spore.Helpers;

namespace Gibbed.Spore.Properties
{
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
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}
	}
}
