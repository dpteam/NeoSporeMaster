using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace _3DTools;

public class Matrix3DStack : IEnumerable<Matrix3D>, ICollection, IEnumerable
{
	private readonly List<Matrix3D> _storage = new List<Matrix3D>();

	public int Count => _storage.Count;

	bool ICollection.IsSynchronized => ((ICollection)_storage).IsSynchronized;

	object ICollection.SyncRoot => ((ICollection)_storage).SyncRoot;

	public Matrix3D Peek()
	{
		return _storage[_storage.Count - 1];
	}

	public void Push(Matrix3D item)
	{
		_storage.Add(item);
	}

	public void Append(Matrix3D item)
	{
		if (Count > 0)
		{
			Matrix3D top = Peek();
			top.Append(item);
			Push(top);
		}
		else
		{
			Push(item);
		}
	}

	public void Prepend(Matrix3D item)
	{
		if (Count > 0)
		{
			Matrix3D top = Peek();
			top.Prepend(item);
			Push(top);
		}
		else
		{
			Push(item);
		}
	}

	public Matrix3D Pop()
	{
		Matrix3D result = Peek();
		_storage.RemoveAt(_storage.Count - 1);
		return result;
	}

	private void Clear()
	{
		_storage.Clear();
	}

	private bool Contains(Matrix3D item)
	{
		return _storage.Contains(item);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		((ICollection)_storage).CopyTo(array, index);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable<Matrix3D>)this).GetEnumerator();
	}

	IEnumerator<Matrix3D> IEnumerable<Matrix3D>.GetEnumerator()
	{
		for (int i = _storage.Count - 1; i >= 0; i--)
		{
			yield return _storage[i];
		}
	}
}
