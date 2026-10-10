using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SporeMaster.RenderWare4;

internal class Buffer<T> : RW4Object, IEnumerable<T>, IEnumerable where T : IRW4Struct, new()
{
	public const uint type_code = 65584u;

	private static uint Tsize = new T().Size();

	private T[] items;

	public T this[int index]
	{
		get
		{
			return items[index];
		}
		set
		{
			items[index] = value;
		}
	}

	public int Length
	{
		get
		{
			return items.Length;
		}
		set
		{
			T[] v = new T[value];
			for (int i = 0; i < value && i < items.Length; i++)
			{
				v[i] = items[i];
			}
			items = v;
		}
	}

	public Buffer(int size)
	{
		items = new T[size];
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable<T>)this).GetEnumerator();
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		try
		{
			T[] array = items;
			for (int i = 0; i < array.Length; i++)
			{
				yield return array[i];
			}
		}
		finally
		{
		}
	}

	public void Assign(IList<T> data)
	{
		items = data.ToArray();
	}

	public override int ComputeSize()
	{
		return (int)(Tsize * items.Length);
	}

	public override void Read(RW4Model m, RW4Section s, Stream r)
	{
		if (s.type_code != 65584)
		{
			throw new ModelFormatException(r, "VB000 Bad type code", s.type_code);
		}
		for (int i = 0; i < items.Length; i++)
		{
			items[i].Read(r);
		}
	}

	public override void Write(RW4Model m, RW4Section s, Stream w)
	{
		for (int i = 0; i < items.Length; i++)
		{
			items[i].Write(w);
		}
	}
}
