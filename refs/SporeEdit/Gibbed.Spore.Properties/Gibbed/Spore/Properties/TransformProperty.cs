using System;
using System.IO;
using System.Xml;
using Gibbed.Spore.Helpers;

namespace Gibbed.Spore.Properties
{
	[PropertyDefinition("transform", "transforms", 56)]
	public class TransformProperty : ComplexProperty
	{
		public uint Unknown01;

		public float Unknown02;

		public float Unknown03;

		public float Unknown04;

		public float Unknown05;

		public float Unknown06;

		public float Unknown07;

		public float Unknown08;

		public float Unknown09;

		public float Unknown10;

		public float Unknown11;

		public float Unknown12;

		public float Unknown13;

		public override void ReadProp(Stream input, bool array)
		{
			Unknown01 = input.ReadU32BE();
			Unknown02 = input.ReadF32();
			Unknown03 = input.ReadF32();
			Unknown04 = input.ReadF32();
			Unknown05 = input.ReadF32();
			Unknown06 = input.ReadF32();
			Unknown07 = input.ReadF32();
			Unknown08 = input.ReadF32();
			Unknown09 = input.ReadF32();
			Unknown10 = input.ReadF32();
			Unknown11 = input.ReadF32();
			Unknown12 = input.ReadF32();
			Unknown13 = input.ReadF32();
		}

		public override void WriteProp(Stream output, bool array)
		{
			throw new NotImplementedException();
		}

		public override void WriteXML(XmlWriter output)
		{
			output.WriteValue($"{Unknown01} {Unknown02} {Unknown03} {Unknown04} {Unknown05} {Unknown06} {Unknown07} {Unknown08} {Unknown09} {Unknown10} {Unknown11} {Unknown12} {Unknown13}");
		}

		public override void ReadXML(XmlReader input)
		{
			throw new NotImplementedException();
		}
	}
}
