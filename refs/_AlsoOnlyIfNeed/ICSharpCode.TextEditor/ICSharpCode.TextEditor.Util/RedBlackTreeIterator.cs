using System;
using System.Collections;
using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Util;

internal struct RedBlackTreeIterator<T> : IEnumerator<T>, IDisposable, IEnumerator
{
	internal RedBlackTreeNode<T> node;

	public bool IsValid => node != null;

	public T Current
	{
		get
		{
			if (node != null)
			{
				return node.val;
			}
			throw new InvalidOperationException();
		}
	}

	object IEnumerator.Current => Current;

	internal RedBlackTreeIterator(RedBlackTreeNode<T> node)
	{
		this.node = node;
	}

	void IDisposable.Dispose()
	{
	}

	void IEnumerator.Reset()
	{
		throw new NotSupportedException();
	}

	public bool MoveNext()
	{
		if (node == null)
		{
			return false;
		}
		if (node.right != null)
		{
			node = node.right.LeftMost;
		}
		else
		{
			RedBlackTreeNode<T> redBlackTreeNode;
			do
			{
				redBlackTreeNode = node;
				node = node.parent;
			}
			while (node != null && node.right == redBlackTreeNode);
		}
		return node != null;
	}

	public bool MoveBack()
	{
		if (node == null)
		{
			return false;
		}
		if (node.left != null)
		{
			node = node.left.RightMost;
		}
		else
		{
			RedBlackTreeNode<T> redBlackTreeNode;
			do
			{
				redBlackTreeNode = node;
				node = node.parent;
			}
			while (node != null && node.left == redBlackTreeNode);
		}
		return node != null;
	}
}
