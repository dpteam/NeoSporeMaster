using System;
using System.IO;
using System.Text;
using System.Xml;
using Gibbed.Spore.Properties;

namespace SporeMaster;

internal class RebuildPropertyFile
{
	private bool failed;

	public bool Failed => failed;

	public RebuildPropertyFile(string path)
	{
		try
		{
			FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
			XmlReader reader = XmlReader.Create(stream);
			PropertyFile file = new PropertyFile();
			file.ReadXML(reader);
			reader.Close();
			stream.Close();
			stream = new FileStream(path + ".part", FileMode.Create, FileAccess.Write);
			XmlTextWriter writer = new XmlTextWriter(stream, Encoding.UTF8)
			{
				Formatting = Formatting.Indented
			};
			file.WriteXML(writer);
			writer.Close();
			stream.Close();
		}
		catch (Exception)
		{
			failed = true;
			if (File.Exists(path + ".part"))
			{
				File.Delete(path + ".part");
			}
			return;
		}
		File.Replace(path + ".part", path, path + ".backup");
		File.Delete(path + ".backup");
	}
}
