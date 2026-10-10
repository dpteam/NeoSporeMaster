using System;
using System.Collections;
using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Document;

public sealed class SpanStack : ICloneable, IEnumerable<Span>, IEnumerable
{
	internal sealed class StackNode
	{
		public readonly StackNode Previous;

		public readonly Span Data;

		public StackNode(StackNode previous, Span data)
		{
			Previous = previous;
			Data = data;
		}
	}

	public struct Enumerator : IEnumerator<Span>, IDisposable, IEnumerator
	{
		private StackNode c;

		public Span Current => c.Data;

		object IEnumerator.Current => c.Data;

		internal Enumerator(StackNode node)
		{
			c = node;
		}

		public void Dispose()
		{
			c = null;
		}

		public bool MoveNext()
		{
			c = c.Previous;
			return c != null;
		}

		public void Reset()
		{
			throw new NotSupportedException();
		}
	}

	private StackNode top;

	public bool IsEmpty => top == null;

	public Span Pop()
	{
		Span data = top.Data;
		top = top.Previous;
		return data;
	}

	public Span Peek()
	{
		return top.Data;
	}

	public void Push(Span s)
	{
		top = new StackNode(top, s);
	}

	public SpanStack Clone()
	{
		SpanStack spanStack = new SpanStack();
		spanStack.top = top;
		return spanStack;
	}

	object ICloneable.Clone()
	{
		return Clone();
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(new StackNode(top, null));
	}

	IEnumerator<Span> IEnumerable<Span>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
