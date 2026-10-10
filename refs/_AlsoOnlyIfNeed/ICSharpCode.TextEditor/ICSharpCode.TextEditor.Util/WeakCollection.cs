using System;
using System.Collections;
using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Util;

public class WeakCollection<T> : IEnumerable<T>, IEnumerable where T : class
{
	private readonly List<WeakReference> innerList = new List<WeakReference>();

	private bool hasEnumerator;

	public void Add(T item)
	{
		if (item == null)
		{
			throw new ArgumentNullException("item");
		}
		CheckNoEnumerator();
		if (innerList.Count == innerList.Capacity || innerList.Count % 32 == 31)
		{
			innerList.RemoveAll((WeakReference r) => !r.IsAlive);
		}
		innerList.Add(new WeakReference(item));
	}

	public void Clear()
	{
		innerList.Clear();
		CheckNoEnumerator();
	}

	public bool Contains(T item)
	{
		if (item == null)
		{
			throw new ArgumentNullException("item");
		}
		CheckNoEnumerator();
		using (IEnumerator<T> enumerator = GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				T current = enumerator.Current;
				if (item.Equals(current))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool Remove(T item)
	{
		if (item == null)
		{
			throw new ArgumentNullException("item");
		}
		CheckNoEnumerator();
		int num = 0;
		while (num < innerList.Count)
		{
			T val = (T)innerList[num].Target;
			if (val == null)
			{
				RemoveAt(num);
				continue;
			}
			if (val == item)
			{
				RemoveAt(num);
				return true;
			}
			num++;
		}
		return false;
	}

	private void RemoveAt(int i)
	{
		int index = innerList.Count - 1;
		innerList[i] = innerList[index];
		innerList.RemoveAt(index);
	}

	private void CheckNoEnumerator()
	{
		if (hasEnumerator)
		{
			throw new InvalidOperationException("The WeakCollection is already being enumerated, it cannot be modified at the same time. Ensure you dispose the first enumerator before modifying the WeakCollection.");
		}
	}

	public IEnumerator<T> GetEnumerator()
	{
		if (hasEnumerator)
		{
			throw new InvalidOperationException("The WeakCollection is already being enumerated, it cannot be enumerated twice at the same time. Ensure you dispose the first enumerator before using another enumerator.");
		}
		try
		{
			hasEnumerator = true;
			int i = 0;
			while (i < innerList.Count)
			{
				T element = (T)innerList[i].Target;
				if (element == null)
				{
					RemoveAt(i);
					continue;
				}
				yield return element;
				i++;
			}
		}
		finally
		{
			hasEnumerator = false;
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
