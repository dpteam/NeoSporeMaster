using System.Collections.Generic;
using System.Xml;

namespace ICSharpCode.TextEditor.Document;

public class BookmarkManagerMemento
{
	private List<int> bookmarks = new List<int>();

	public List<int> Bookmarks
	{
		get
		{
			return bookmarks;
		}
		set
		{
			bookmarks = value;
		}
	}

	public void CheckMemento(IDocument document)
	{
		for (int i = 0; i < bookmarks.Count; i++)
		{
			int num = bookmarks[i];
			if (num < 0 || num >= document.TotalNumberOfLines)
			{
				bookmarks.RemoveAt(i);
				i--;
			}
		}
	}

	public BookmarkManagerMemento()
	{
	}

	public BookmarkManagerMemento(XmlElement element)
	{
		foreach (XmlElement childNode in element.ChildNodes)
		{
			bookmarks.Add(int.Parse(childNode.Attributes["line"].InnerText));
		}
	}

	public BookmarkManagerMemento(List<int> bookmarks)
	{
		this.bookmarks = bookmarks;
	}

	public object FromXmlElement(XmlElement element)
	{
		return new BookmarkManagerMemento(element);
	}

	public XmlElement ToXmlElement(XmlDocument doc)
	{
		XmlElement xmlElement = doc.CreateElement("Bookmarks");
		foreach (int bookmark in bookmarks)
		{
			XmlElement xmlElement2 = doc.CreateElement("Mark");
			XmlAttribute xmlAttribute = doc.CreateAttribute("line");
			xmlAttribute.InnerText = bookmark.ToString();
			xmlElement2.Attributes.Append(xmlAttribute);
			xmlElement.AppendChild(xmlElement2);
		}
		return xmlElement;
	}
}
