using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace ICSharpCode.TextEditor.Util;

internal sealed class CheckedList<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable
{
	private readonly int threadID;

	private readonly IList<T> baseList;

	private int enumeratorCount;

	public T this[int index]
	{
		get
		{
			CheckRead();
			return baseList[index];
		}
		set
		{
			CheckWrite();
			baseList[index] = value;
		}
	}

	public int Count
	{
		get
		{
			CheckRead();
			return baseList.Count;
		}
	}

	public bool IsReadOnly
	{
		get
		{
			CheckRead();
			return baseList.IsReadOnly;
		}
	}

	public CheckedList()
		: this((IList<T>)new List<T>())
	{
	}

	public CheckedList(IList<T> baseList)
	{
		if (baseList == null)
		{
			throw new ArgumentNullException("baseList");
		}
		this.baseList = baseList;
		threadID = Thread.CurrentThread.ManagedThreadId;
	}

	private void CheckRead()
	{
		if (Thread.CurrentThread.ManagedThreadId != threadID)
		{
			throw new InvalidOperationException("CheckList cannot be accessed from this thread!");
		}
	}

	private void CheckWrite()
	{
		if (Thread.CurrentThread.ManagedThreadId != threadID)
		{
			throw new InvalidOperationException("CheckList cannot be accessed from this thread!");
		}
		if (enumeratorCount != 0)
		{
			throw new InvalidOperationException("CheckList cannot be written to while enumerators are active!");
		}
	}

	public int IndexOf(T item)
	{
		CheckRead();
		return baseList.IndexOf(item);
	}

	public void Insert(int index, T item)
	{
		CheckWrite();
		baseList.Insert(index, item);
	}

	public void RemoveAt(int index)
	{
		CheckWrite();
		baseList.RemoveAt(index);
	}

	public void Add(T item)
	{
		CheckWrite();
		baseList.Add(item);
	}

	public void Clear()
	{
		CheckWrite();
		baseList.Clear();
	}

	public bool Contains(T item)
	{
		CheckRead();
		return baseList.Contains(item);
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		CheckRead();
		baseList.CopyTo(array, arrayIndex);
	}

	public bool Remove(T item)
	{
		CheckWrite();
		return baseList.Remove(item);
	}

	public IEnumerator<T> GetEnumerator()
	{
		CheckRead();
		return Enumerate();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		CheckRead();
		return Enumerate();
	}

	private IEnumerator<T> Enumerate()
	{
		CheckRead();
		try
		{
			enumeratorCount++;
			foreach (T @base in baseList)
			{
				yield return @base;
				CheckRead();
			}
		}
		finally
		{
			enumeratorCount--;
			CheckRead();
		}
	}
}
