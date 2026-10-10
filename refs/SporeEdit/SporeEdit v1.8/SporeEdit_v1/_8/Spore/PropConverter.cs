using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using Gibbed.Spore.Helpers;
using Gibbed.Spore.Properties;

namespace SporeEdit_v1._8.Spore
{
	public class PropConverter
	{
		private Dictionary<uint, string> PropertyNames;

		public PropConverter()
		{
			PropertyNames = new Dictionary<uint, string>();
			LoadPropertyNames(Path.Combine(Application.StartupPath, "data\\property_names.xml"));
		}

		private void LoadPropertyNames(string path)
		{
			LoadPropertyNames(path, clear: true);
		}

		private void LoadPropertyNames(string path, bool clear)
		{
			if (clear)
			{
				PropertyNames.Clear();
			}
			if (!File.Exists(path))
			{
				return;
			}
			XPathDocument xPathDocument = new XPathDocument(path);
			XPathNavigator xPathNavigator = xPathDocument.CreateNavigator();
			XPathNodeIterator xPathNodeIterator = xPathNavigator.Select("/names/name");
			while (xPathNodeIterator.MoveNext())
			{
				string attribute = xPathNodeIterator.Current.GetAttribute("id", "");
				string value = xPathNodeIterator.Current.Value;
				uint key;
				if (attribute.StartsWith("(hash(") && attribute.EndsWith("))"))
				{
					string input = attribute.Substring(6, checked(attribute.Length - 8));
					key = input.FNV();
				}
				else
				{
					key = attribute.GetHexNumber();
				}
				PropertyNames[key] = value;
			}
		}

		public void Convert(Stream input, Stream output)
		{
			PropertyFile propertyFile = new PropertyFile();
			propertyFile.Read(input);
			XmlTextWriter xmlTextWriter = new XmlTextWriter(output, Encoding.UTF8);
			xmlTextWriter.Formatting = Formatting.Indented;
			xmlTextWriter.WriteStartDocument();
			xmlTextWriter.WriteStartElement("properties");
			foreach (uint key in propertyFile.Values.Keys)
			{
				Property property = propertyFile.Values[key];
				if (!(property is ArrayProperty))
				{
					xmlTextWriter.WriteStartElement(property.GetType().GetSingularName());
				}
				else
				{
					ArrayProperty arrayProperty = (ArrayProperty)property;
					xmlTextWriter.WriteStartElement(arrayProperty.PropertyType.GetPluralName());
				}
				if (PropertyNames.ContainsKey(key))
				{
					xmlTextWriter.WriteAttributeString("name", PropertyNames[key]);
				}
				xmlTextWriter.WriteAttributeString("id", "0x" + key.ToString("X8"));
				property.WriteXML(xmlTextWriter);
				xmlTextWriter.WriteEndElement();
			}
			xmlTextWriter.WriteEndDocument();
			xmlTextWriter.Flush();
		}
	}
}
