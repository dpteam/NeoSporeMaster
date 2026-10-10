using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Gibbed.Spore.Helpers;

namespace SporeMaster;

internal class HashGuesser : INotifyPropertyChanged
{
	public class Result
	{
		public string Text { get; set; }
	}

	private uint? target_hash;

	private string prefix = "";

	private string suffix = "";

	private string contains = "";

	private bool isEnabled;

	private uint fnv_unstep = 899433627u;

	private List<string> left;

	private List<string> right;

	private Dictionary<uint, string> left_hash;

	private string left_hash_prefix;

	public List<Result> Results { get; set; }

	public uint? HashCode
	{
		get
		{
			return target_hash;
		}
		set
		{
			target_hash = value;
			guess();
		}
	}

	public string Prefix
	{
		get
		{
			return prefix;
		}
		set
		{
			prefix = value;
			guess();
		}
	}

	public string Suffix
	{
		get
		{
			return suffix;
		}
		set
		{
			suffix = value;
			guess();
		}
	}

	public string Contains
	{
		get
		{
			return contains;
		}
		set
		{
			contains = value;
			guess();
		}
	}

	public bool Enabled
	{
		get
		{
			return isEnabled;
		}
		set
		{
			isEnabled = value;
			guess();
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	private void NotifyPropertyChanged(string info)
	{
		if (PropertyChanged != null)
		{
			PropertyChanged(this, new PropertyChangedEventArgs(info));
		}
	}

	private void guess()
	{
		Results = new List<Result>();
		if (!target_hash.HasValue || !isEnabled)
		{
			NotifyPropertyChanged("results");
			return;
		}
		if (left == null)
		{
			left = read("guess_left.txt", 5000000);
		}
		if (right == null)
		{
			right = read("guess_right.txt", 5000000);
		}
		if (left_hash == null || prefix != left_hash_prefix)
		{
			left_hash_prefix = prefix;
			recalc_left();
		}
		uint th = FNV_rev(target_hash.Value, suffix);
		if (contains != "" && left_hash.ContainsKey(FNV_rev(th, contains)))
		{
			string text = left_hash[FNV_rev(th, contains)];
			char[] separator = new char[1];
			string[] array = text.Split(separator);
			foreach (string l in array)
			{
				Results.Add(new Result
				{
					Text = prefix + l + contains + suffix
				});
			}
		}
		foreach (string r in right)
		{
			uint h = FNV_rev(th, r);
			if (left_hash.ContainsKey(h))
			{
				string text2 = left_hash[h];
				char[] separator2 = new char[1];
				string[] array2 = text2.Split(separator2);
				foreach (string l2 in array2)
				{
					Results.Add(new Result
					{
						Text = prefix + l2 + r + suffix
					});
				}
			}
			if (!(contains != ""))
			{
				continue;
			}
			h = FNV_rev(h, contains);
			if (left_hash.ContainsKey(h))
			{
				string text3 = left_hash[h];
				char[] separator3 = new char[1];
				string[] array3 = text3.Split(separator3);
				foreach (string l3 in array3)
				{
					Results.Add(new Result
					{
						Text = prefix + l3 + contains + r + suffix
					});
				}
			}
		}
		Results = (from result in Results
			group result by result.Text into g
			where g.Key.Contains(contains)
			orderby g.Key.Length
			select new Result
			{
				Text = g.Key
			}).ToList();
		NotifyPropertyChanged("results");
	}

	private uint FNV_rev(uint h, string chars)
	{
		for (int i = chars.Length - 1; i >= 0; i--)
		{
			h ^= chars[i];
			h *= fnv_unstep;
		}
		return h;
	}

	private void recalc_left()
	{
		left_hash = new Dictionary<uint, string>();
		foreach (string hi in left)
		{
			string line = prefix + hi;
			uint h = line.FNV();
			if (left_hash.ContainsKey(h))
			{
				left_hash[h] = left_hash[h] + "\0" + hi;
			}
			else
			{
				left_hash.Add(h, hi);
			}
		}
	}

	private List<string> read(string name, int max)
	{
		List<string> result = new List<string>();
		using (StreamReader read_right = new StreamReader(name))
		{
			while (max-- != 0)
			{
				string line = read_right.ReadLine();
				if (line != null)
				{
					result.Add(line);
					continue;
				}
				break;
			}
		}
		return result;
	}

	public HashGuesser()
	{
		Results = new List<Result>();
	}
}
