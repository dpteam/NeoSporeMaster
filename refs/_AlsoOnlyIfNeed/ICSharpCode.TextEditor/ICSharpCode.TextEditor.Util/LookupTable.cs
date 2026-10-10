using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Util;

public class LookupTable
{
	private class Node
	{
		public string word;

		public object color;

		public Node[] leaf = new Node[256];

		public Node(object color, string word)
		{
			this.word = word;
			this.color = color;
		}
	}

	private Node root = new Node(null, null);

	private bool casesensitive;

	private int length;

	public int Count => length;

	public object this[IDocument document, LineSegment line, int offset, int length]
	{
		get
		{
			if (length == 0)
			{
				return null;
			}
			Node node = root;
			int num = line.Offset + offset;
			if (casesensitive)
			{
				for (int i = 0; i < length; i++)
				{
					int num2 = document.GetCharAt(num + i) % 256;
					node = node.leaf[num2];
					if (node == null)
					{
						return null;
					}
					if (node.color != null && TextUtility.RegionMatches(document, num, length, node.word))
					{
						return node.color;
					}
				}
			}
			else
			{
				for (int j = 0; j < length; j++)
				{
					int num3 = char.ToUpper(document.GetCharAt(num + j)) % 256;
					node = node.leaf[num3];
					if (node == null)
					{
						return null;
					}
					if (node.color != null && TextUtility.RegionMatches(document, casesensitive, num, length, node.word))
					{
						return node.color;
					}
				}
			}
			return null;
		}
	}

	public object this[string keyword]
	{
		set
		{
			Node node = root;
			Node node2 = root;
			if (!casesensitive)
			{
				keyword = keyword.ToUpper();
			}
			length++;
			for (int i = 0; i < keyword.Length; i++)
			{
				int num = keyword[i] % 256;
				_ = keyword[i];
				node2 = node2.leaf[num];
				if (node2 == null)
				{
					node.leaf[num] = new Node(value, keyword);
					break;
				}
				if (node2.word != null && node2.word.Length != i)
				{
					string word = node2.word;
					object color = node2.color;
					node2.color = (node2.word = null);
					this[word] = color;
				}
				if (i == keyword.Length - 1)
				{
					node2.word = keyword;
					node2.color = value;
					break;
				}
				node = node2;
			}
		}
	}

	public LookupTable(bool casesensitive)
	{
		this.casesensitive = casesensitive;
	}
}
