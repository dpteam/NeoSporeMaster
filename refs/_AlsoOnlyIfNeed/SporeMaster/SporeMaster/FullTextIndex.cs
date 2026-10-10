using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SporeMaster;

internal class FullTextIndex
{
	private class DirCache
	{
		public Dictionary<string, FullTextIndex> cache = new Dictionary<string, FullTextIndex>();

		private BinaryWriter writer;

		private string filename;

		private int totalsize;

		private int storedsize;

		public void Dispose()
		{
			if (writer != null)
			{
				writer.Close();
			}
			writer = null;
			cache = null;
		}

		public void read(string filename)
		{
			this.filename = filename;
			if (!File.Exists(filename))
			{
				return;
			}
			try
			{
				using FileStream file = File.OpenRead(filename);
				BinaryReader reader = new BinaryReader(file);
				int fileLength = (int)reader.BaseStream.Length;
				int pos = 0;
				while (pos < fileLength)
				{
					string name = reader.ReadString();
					pos += ((name.Length >= 16384) ? 3 : ((name.Length < 128) ? 1 : 2));
					pos += name.Length;
					int len = reader.ReadInt32();
					if (len < 0 || (ulong)((long)pos + 12L + (long)len * 5L) > (ulong)fileLength)
					{
						throw new Exception("Bad length in index file.");
					}
					pos += 4;
					DateTime lastModified = DateTime.FromFileTimeUtc(reader.ReadInt64());
					pos += 8;
					byte[] data = new byte[len];
					reader.Read(data, 0, len);
					pos += len;
					int[] suffix_array = new int[len];
					for (int i = 0; i < len; i++)
					{
						suffix_array[i] = reader.ReadInt32();
						if (suffix_array[i] < 0 || suffix_array[i] >= len)
						{
							throw new Exception("Out of bounds data in suffix array.");
						}
					}
					pos += len * 4;
					cache[name] = new FullTextIndex
					{
						data = data,
						suffix_array = suffix_array,
						lastModified = lastModified
					};
					totalsize += cache[name].data.Length;
				}
			}
			catch (Exception)
			{
				File.Delete(filename);
				storedsize = 0;
				cache = new Dictionary<string, FullTextIndex>();
				totalsize = 0;
			}
		}

		public void Add(FullTextIndex fti)
		{
			string fname = Path.GetFileName(fti.fullpath);
			if (cache.ContainsKey(fname))
			{
				totalsize -= cache[fname].data.Length;
			}
			cache[fname] = fti;
			int oldtotalsize = totalsize;
			totalsize += fti.data.Length;
			storedsize += fti.data.Length;
			if (storedsize > totalsize * 2 && storedsize > totalsize + 16384)
			{
				storedsize = totalsize;
				writer.Close();
				writer = null;
				File.Delete(filename);
				foreach (FullTextIndex i in cache.Values)
				{
					write(i);
				}
				writer.Flush();
			}
			else
			{
				write(fti);
				if (totalsize / 1000000 > oldtotalsize / 1000000)
				{
					writer.Flush();
				}
			}
		}

		private void write(FullTextIndex fti)
		{
			if (writer == null)
			{
				writer = new BinaryWriter(File.Create(filename));
			}
			writer.Write(Path.GetFileName(fti.fullpath));
			writer.Write(fti.data.Length);
			writer.Write(fti.lastModified.ToFileTimeUtc());
			writer.Write(fti.data, 0, fti.data.Length);
			for (int i = 0; i < fti.data.Length; i++)
			{
				writer.Write(fti.suffix_array[i]);
			}
		}
	}

	private string fullpath;

	private byte[] data;

	private int[] suffix_array;

	private DateTime lastModified;

	private static Dictionary<string, DirCache> dirCaches = new Dictionary<string, DirCache>();

	public static void ResetPath(string path)
	{
		string[] array = dirCaches.Keys.ToArray();
		foreach (string d in array)
		{
			if (d == path || d.StartsWith(path + "\\"))
			{
				dirCaches[d].Dispose();
				dirCaches.Remove(d);
			}
		}
	}

	private FullTextIndex()
	{
	}

	public FullTextIndex(string path)
	{
		fullpath = path;
		string dir = Path.GetDirectoryName(path);
		string fname = Path.GetFileName(path);
		lastModified = File.GetLastWriteTimeUtc(path);
		if (!dirCaches.TryGetValue(dir, out var dcache))
		{
			dcache = new DirCache();
			dcache.read(dir + ".search_index");
			dirCaches[dir] = dcache;
		}
		if (dcache.cache.ContainsKey(fname))
		{
			FullTextIndex fti = dcache.cache[fname];
			if (fti.lastModified == lastModified)
			{
				data = fti.data;
				suffix_array = fti.suffix_array;
				return;
			}
		}
		try
		{
			data = Encoding.UTF8.GetBytes(File.ReadAllText(path).ToLowerInvariant());
		}
		catch (IOException)
		{
			return;
		}
		catch (UnauthorizedAccessException)
		{
			return;
		}
		if (suffix_array == null)
		{
			int[] temp = new int[data.Length + 3];
			for (int i = 0; i < data.Length; i++)
			{
				temp[i] = data[i];
			}
			temp[data.Length] = (temp[data.Length + 1] = (temp[data.Length + 2] = 0));
			suffix_array = new int[data.Length];
			for (int j = 0; j < data.Length; j++)
			{
				suffix_array[j] = j;
			}
			suffixArray(temp, suffix_array, data.Length, 255);
			dcache.Add(this);
		}
	}

	public bool Contains(byte[] search)
	{
		if (data == null)
		{
			return false;
		}
		return Contains_Suffix(search);
	}

	private bool Contains_Suffix(byte[] search)
	{
		int begin = 0;
		int end = suffix_array.Length;
		while (begin != end)
		{
			int mid = begin + end >> 1;
			int p = suffix_array[mid];
			int i = 0;
			while (true)
			{
				if (i < search.Length)
				{
					if (p + i >= data.Length || search[i] > data[p + i])
					{
						begin = mid + 1;
						break;
					}
					if (search[i] < data[p + i])
					{
						end = mid;
						break;
					}
					i++;
					continue;
				}
				return true;
			}
		}
		return false;
	}

	private bool Contains_Linear(byte[] search)
	{
		int end = data.Length - search.Length;
		for (int i = 0; i < end; i++)
		{
			int j = 0;
			while (true)
			{
				if (j < search.Length)
				{
					if (data[i + j] != search[j])
					{
						break;
					}
					j++;
					continue;
				}
				return true;
			}
		}
		return false;
	}

	private bool leq(int a1, int a2, int b1, int b2)
	{
		if (a1 >= b1)
		{
			if (a1 == b1)
			{
				return a2 <= b2;
			}
			return false;
		}
		return true;
	}

	private bool leq(int a1, int a2, int a3, int b1, int b2, int b3)
	{
		if (a1 >= b1)
		{
			if (a1 == b1)
			{
				return leq(a2, a3, b2, b3);
			}
			return false;
		}
		return true;
	}

	private static void radixPass(int[] a, int[] b, int[] r, int ri, int n, int K)
	{
		int[] c = new int[K + 1];
		for (int i = 0; i <= K; i++)
		{
			c[i] = 0;
		}
		for (int j = 0; j < n; j++)
		{
			c[r[ri + a[j]]]++;
		}
		int k = 0;
		int sum = 0;
		for (; k <= K; k++)
		{
			int t = c[k];
			c[k] = sum;
			sum += t;
		}
		for (int l = 0; l < n; l++)
		{
			b[c[r[ri + a[l]]]++] = a[l];
		}
	}

	private void suffixArray(int[] s, int[] SA, int n, int K)
	{
		int n2 = (n + 2) / 3;
		int n3 = (n + 1) / 3;
		int n4 = n / 3;
		int n5 = n2 + n4;
		int[] s12 = new int[n5 + 3];
		s12[n5] = (s12[n5 + 1] = (s12[n5 + 2] = 0));
		int[] SA12 = new int[n5 + 3];
		SA12[n5] = (SA12[n5 + 1] = (SA12[n5 + 2] = 0));
		int[] s13 = new int[n2];
		int[] SA13 = new int[n2];
		int i = 0;
		int j = 0;
		for (; i < n + (n2 - n3); i++)
		{
			if (i % 3 != 0)
			{
				s12[j++] = i;
			}
		}
		radixPass(s12, SA12, s, 2, n5, K);
		radixPass(SA12, s12, s, 1, n5, K);
		radixPass(s12, SA12, s, 0, n5, K);
		int name = 0;
		int c0 = -1;
		int c1 = -1;
		int c2 = -1;
		for (int k = 0; k < n5; k++)
		{
			if (s[SA12[k]] != c0 || s[SA12[k] + 1] != c1 || s[SA12[k] + 2] != c2)
			{
				name++;
				c0 = s[SA12[k]];
				c1 = s[SA12[k] + 1];
				c2 = s[SA12[k] + 2];
			}
			if (SA12[k] % 3 == 1)
			{
				s12[SA12[k] / 3] = name;
			}
			else
			{
				s12[SA12[k] / 3 + n2] = name;
			}
		}
		if (name < n5)
		{
			suffixArray(s12, SA12, n5, name);
			for (int l = 0; l < n5; l++)
			{
				s12[SA12[l]] = l + 1;
			}
		}
		else
		{
			for (int m = 0; m < n5; m++)
			{
				SA12[s12[m] - 1] = m;
			}
		}
		int num = 0;
		int j2 = 0;
		for (; num < n5; num++)
		{
			if (SA12[num] < n2)
			{
				s13[j2++] = 3 * SA12[num];
			}
		}
		radixPass(s13, SA13, s, 0, n2, K);
		int p = 0;
		int t = n2 - n3;
		for (int num2 = 0; num2 < n; num2++)
		{
			int i2 = ((SA12[t] < n2) ? (SA12[t] * 3 + 1) : ((SA12[t] - n2) * 3 + 2));
			int j3 = SA13[p];
			if ((SA12[t] < n2) ? leq(s[i2], s12[SA12[t] + n2], s[j3], s12[j3 / 3]) : leq(s[i2], s[i2 + 1], s12[SA12[t] - n2 + 1], s[j3], s[j3 + 1], s12[j3 / 3 + n2]))
			{
				SA[num2] = i2;
				t++;
				if (t == n5)
				{
					num2++;
					while (p < n2)
					{
						SA[num2] = SA13[p];
						p++;
						num2++;
					}
				}
				continue;
			}
			SA[num2] = j3;
			p++;
			if (p == n2)
			{
				num2++;
				while (t < n5)
				{
					SA[num2] = ((SA12[t] < n2) ? (SA12[t] * 3 + 1) : ((SA12[t] - n2) * 3 + 2));
					t++;
					num2++;
				}
			}
		}
	}
}
