using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SporeMaster;

public class SearchSpec
{
	public struct Sequence
	{
		public string as_lower;

		public byte[] as_utf8;

		public Sequence(string x)
		{
			as_lower = x.ToLowerInvariant();
			as_utf8 = Encoding.UTF8.GetBytes(as_lower);
		}
	}

	public Sequence[] require_all;

	public static SearchSpec all = new SearchSpec("");

	public SearchSpec(string phrase)
	{
		require_all = (from word in words(phrase)
			where word != ""
			select new Sequence(word)).ToArray();
	}

	private IEnumerable<string> words(string phrase)
	{
		string[] w = phrase.Split(' ');
		int i = 0;
		while (i < w.Length)
		{
			if (w[i].StartsWith("\""))
			{
				string s = "";
				for (; i < w.Length; i++)
				{
					if (w[i].EndsWith("\"") && (w[i] != "\"" || s != ""))
					{
						s += w[i].Substring(0, w[i].Length - 1);
						i++;
						break;
					}
					s += w[i];
					if (i + 1 < w.Length)
					{
						s += " ";
					}
				}
				yield return s.Substring(1);
			}
			else
			{
				yield return w[i++];
			}
		}
	}
}
