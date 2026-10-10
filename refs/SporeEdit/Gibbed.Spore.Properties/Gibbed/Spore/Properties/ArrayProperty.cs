using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Gibbed.Spore.Properties
{
	public class ArrayProperty : Property
	{
		public Type PropertyType;

		public List<Property> Values = new List<Property>();

		public override void ReadProp(Stream input, bool array)
		{
			throw new NotImplementedException();
		}

		public override void WriteProp(Stream output, bool array)
		{
			throw new NotImplementedException();
		}

		public override void WriteXML(XmlWriter output)
		{
			foreach (Property value in Values)
			{
				output.WriteStartElement(value.GetType().GetSingularName());
				value.WriteXML(output);
				output.WriteEndElement();
			}
		}

		public override void ReadXML(XmlReader input)
		{
			throw new NotImplementedException();
		}
	}
}
