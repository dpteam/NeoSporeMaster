using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Gibbed.Spore.Helpers;
using Gibbed.Spore.Properties;
using Microsoft.VisualBasic.CompilerServices;

namespace SporeEdit_v1._8.Spore
{
	internal class XMLConverter
	{
		[DebuggerNonUserCode]
		public XMLConverter()
		{
		}

		public void Convert(Stream input, Stream output)
		{
			PropertyFile propertyFile = new PropertyFile();
			XmlTextReader xmlTextReader = new XmlTextReader(input);
			xmlTextReader.ReadToFollowing("properties");
			while (xmlTextReader.Read())
			{
				if (!xmlTextReader.IsStartElement())
				{
					continue;
				}
				string name = xmlTextReader.Name;
				string attribute = xmlTextReader.GetAttribute("id");
				uint hexNumber = attribute.GetHexNumber();
				PropertyLookup propertyLookup = propertyFile.FindPropertyType(name);
				if (propertyLookup == null)
				{
					throw new Exception("unknown type " + name);
				}
				Property property = null;
				if (Operators.CompareString(propertyLookup.Definition.Name, name, TextCompare: false) == 0)
				{
					property = Activator.CreateInstance(propertyLookup.Type) as Property;
					property.ReadXML(xmlTextReader);
				}
				else if (Operators.CompareString(propertyLookup.Definition.PluralName, name, TextCompare: false) == 0)
				{
					ArrayProperty arrayProperty = new ArrayProperty();
					arrayProperty.PropertyType = propertyLookup.Type;
					XmlReader xmlReader = xmlTextReader.ReadSubtree();
					xmlReader.ReadToFollowing(name);
					while (xmlReader.Read())
					{
						if (xmlReader.IsStartElement())
						{
							if (Operators.CompareString(xmlReader.Name, propertyLookup.Definition.Name, TextCompare: false) != 0)
							{
								throw new Exception("array element for " + name + " is not the right type");
							}
							Property property2 = Activator.CreateInstance(propertyLookup.Type) as Property;
							property2.ReadXML(xmlReader);
							arrayProperty.Values.Add(property2);
						}
					}
					property = arrayProperty;
				}
				propertyFile.Values[hexNumber] = property;
			}
			propertyFile.Write(output);
		}
	}
}
