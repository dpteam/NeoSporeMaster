using System.IO;
using System.Xml;

namespace Gibbed.Spore.Properties
{
	public abstract class Property
	{
		public abstract void ReadProp(Stream input, bool array);

		public abstract void WriteProp(Stream input, bool array);

		public abstract void WriteXML(XmlWriter output);

		public abstract void ReadXML(XmlReader input);
	}
}
