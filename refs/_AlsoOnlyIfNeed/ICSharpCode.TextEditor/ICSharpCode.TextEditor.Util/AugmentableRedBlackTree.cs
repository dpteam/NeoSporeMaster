using System;
using System.Collections;
using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Util;

internal sealed class AugmentableRedBlackTree<T, Host> : ICollection<T>, IEnumerable<T>, IEnumerable where Host : IRedBlackTreeHost<T>
{
	private const bool RED = true;

	private const bool BLACK = false;

	private readonly Host host;

	private int count;

	internal RedBlackTreeNode<T> root;

	public int Count => count;

	bool ICollection<T>.IsReadOnly => false;

	public AugmentableRedBlackTree(Host host)
	{
		if (host == null)
		{
			throw new ArgumentNullException("host");
		}
		this.host = host;
	}

	public void Clear()
	{
		root = null;
		count = 0;
	}

	public void Add(T item)
	{
		AddInternal(new RedBlackTreeNode<T>(item));
	}

	private void AddInternal(RedBlackTreeNode<T> newNode)
	{
		if (root == null)
		{
			count = 1;
			root = newNode;
			return;
		}
		RedBlackTreeNode<T> redBlackTreeNode = root;
		while (true)
		{
			if (host.Compare(newNode.val, redBlackTreeNode.val) <= 0)
			{
				if (redBlackTreeNode.left == null)
				{
					InsertAsLeft(redBlackTreeNode, newNode);
					return;
				}
				redBlackTreeNode = redBlackTreeNode.left;
			}
			else
			{
				if (redBlackTreeNode.right == null)
				{
					break;
				}
				redBlackTreeNode = redBlackTreeNode.right;
			}
		}
		InsertAsRight(redBlackTreeNode, newNode);
	}

	internal void InsertAsLeft(RedBlackTreeNode<T> parentNode, RedBlackTreeNode<T> newNode)
	{
		parentNode.left = newNode;
		newNode.parent = parentNode;
		newNode.color = true;
		host.UpdateAfterChildrenChange(parentNode);
		FixTreeOnInsert(newNode);
		count++;
	}

	internal void InsertAsRight(RedBlackTreeNode<T> parentNode, RedBlackTreeNode<T> newNode)
	{
		parentNode.right = newNode;
		newNode.parent = parentNode;
		newNode.color = true;
		host.UpdateAfterChildrenChange(parentNode);
		FixTreeOnInsert(newNode);
		count++;
	}

	private void FixTreeOnInsert(RedBlackTreeNode<T> node)
	{
		RedBlackTreeNode<T> parent = node.parent;
		if (parent == null)
		{
			node.color = false;
		}
		else
		{
			if (!parent.color)
			{
				return;
			}
			RedBlackTreeNode<T> parent2 = parent.parent;
			RedBlackTreeNode<T> redBlackTreeNode = Sibling(parent);
			if (redBlackTreeNode != null && redBlackTreeNode.color)
			{
				parent.color = false;
				redBlackTreeNode.color = false;
				parent2.color = true;
				FixTreeOnInsert(parent2);
				return;
			}
			if (node == parent.right && parent == parent2.left)
			{
				RotateLeft(parent);
				node = node.left;
			}
			else if (node == parent.left && parent == parent2.right)
			{
				RotateRight(parent);
				node = node.right;
			}
			parent = node.parent;
			parent2 = parent.parent;
			parent.color = false;
			parent2.color = true;
			if (node == parent.left && parent == parent2.left)
			{
				RotateRight(parent2);
			}
			else
			{
				RotateLeft(parent2);
			}
		}
	}

	private void ReplaceNode(RedBlackTreeNode<T> replacedNode, RedBlackTreeNode<T> newNode)
	{
		if (replacedNode.parent == null)
		{
			root = newNode;
		}
		else if (replacedNode.parent.left == replacedNode)
		{
			replacedNode.parent.left = newNode;
		}
		else
		{
			replacedNode.parent.right = newNode;
		}
		if (newNode != null)
		{
			newNode.parent = replacedNode.parent;
		}
		replacedNode.parent = null;
	}

	private void RotateLeft(RedBlackTreeNode<T> p)
	{
		RedBlackTreeNode<T> right = p.right;
		ReplaceNode(p, right);
		p.right = right.left;
		if (p.right != null)
		{
			p.right.parent = p;
		}
		right.left = p;
		p.parent = right;
		host.UpdateAfterRotateLeft(p);
	}

	private void RotateRight(RedBlackTreeNode<T> p)
	{
		RedBlackTreeNode<T> left = p.left;
		ReplaceNode(p, left);
		p.left = left.right;
		if (p.left != null)
		{
			p.left.parent = p;
		}
		left.right = p;
		p.parent = left;
		host.UpdateAfterRotateRight(p);
	}

	private RedBlackTreeNode<T> Sibling(RedBlackTreeNode<T> node)
	{
		if (node == node.parent.left)
		{
			return node.parent.right;
		}
		return node.parent.left;
	}

	public void RemoveAt(RedBlackTreeIterator<T> iterator)
	{
		RedBlackTreeNode<T> redBlackTreeNode = iterator.node;
		if (redBlackTreeNode == null)
		{
			throw new ArgumentException("Invalid iterator");
		}
		while (redBlackTreeNode.parent != null)
		{
			redBlackTreeNode = redBlackTreeNode.parent;
		}
		if (redBlackTreeNode != root)
		{
			throw new ArgumentException("Iterator does not belong to this tree");
		}
		RemoveNode(iterator.node);
	}

	internal void RemoveNode(RedBlackTreeNode<T> removedNode)
	{
		if (removedNode.left != null && removedNode.right != null)
		{
			RedBlackTreeNode<T> leftMost = removedNode.right.LeftMost;
			RemoveNode(leftMost);
			ReplaceNode(removedNode, leftMost);
			leftMost.left = removedNode.left;
			if (leftMost.left != null)
			{
				leftMost.left.parent = leftMost;
			}
			leftMost.right = removedNode.right;
			if (leftMost.right != null)
			{
				leftMost.right.parent = leftMost;
			}
			leftMost.color = removedNode.color;
			host.UpdateAfterChildrenChange(leftMost);
			if (leftMost.parent != null)
			{
				host.UpdateAfterChildrenChange(leftMost.parent);
			}
			return;
		}
		count--;
		RedBlackTreeNode<T> parent = removedNode.parent;
		RedBlackTreeNode<T> redBlackTreeNode = removedNode.left ?? removedNode.right;
		ReplaceNode(removedNode, redBlackTreeNode);
		if (parent != null)
		{
			host.UpdateAfterChildrenChange(parent);
		}
		if (!removedNode.color)
		{
			if (redBlackTreeNode != null && redBlackTreeNode.color)
			{
				redBlackTreeNode.color = false;
			}
			else
			{
				FixTreeOnDelete(redBlackTreeNode, parent);
			}
		}
	}

	private static RedBlackTreeNode<T> Sibling(RedBlackTreeNode<T> node, RedBlackTreeNode<T> parentNode)
	{
		if (node == parentNode.left)
		{
			return parentNode.right;
		}
		return parentNode.left;
	}

	private static bool GetColor(RedBlackTreeNode<T> node)
	{
		return node?.color ?? false;
	}

	private void FixTreeOnDelete(RedBlackTreeNode<T> node, RedBlackTreeNode<T> parentNode)
	{
		if (parentNode == null)
		{
			return;
		}
		RedBlackTreeNode<T> redBlackTreeNode = Sibling(node, parentNode);
		if (redBlackTreeNode.color)
		{
			parentNode.color = true;
			redBlackTreeNode.color = false;
			if (node == parentNode.left)
			{
				RotateLeft(parentNode);
			}
			else
			{
				RotateRight(parentNode);
			}
			redBlackTreeNode = Sibling(node, parentNode);
		}
		if (!parentNode.color && !redBlackTreeNode.color && !GetColor(redBlackTreeNode.left) && !GetColor(redBlackTreeNode.right))
		{
			redBlackTreeNode.color = true;
			FixTreeOnDelete(parentNode, parentNode.parent);
			return;
		}
		if (parentNode.color && !redBlackTreeNode.color && !GetColor(redBlackTreeNode.left) && !GetColor(redBlackTreeNode.right))
		{
			redBlackTreeNode.color = true;
			parentNode.color = false;
			return;
		}
		if (node == parentNode.left && !redBlackTreeNode.color && GetColor(redBlackTreeNode.left) && !GetColor(redBlackTreeNode.right))
		{
			redBlackTreeNode.color = true;
			redBlackTreeNode.left.color = false;
			RotateRight(redBlackTreeNode);
		}
		else if (node == parentNode.right && !redBlackTreeNode.color && GetColor(redBlackTreeNode.right) && !GetColor(redBlackTreeNode.left))
		{
			redBlackTreeNode.color = true;
			redBlackTreeNode.right.color = false;
			RotateLeft(redBlackTreeNode);
		}
		redBlackTreeNode = Sibling(node, parentNode);
		redBlackTreeNode.color = parentNode.color;
		parentNode.color = false;
		if (node == parentNode.left)
		{
			if (redBlackTreeNode.right != null)
			{
				redBlackTreeNode.right.color = false;
			}
			RotateLeft(parentNode);
		}
		else
		{
			if (redBlackTreeNode.left != null)
			{
				redBlackTreeNode.left.color = false;
			}
			RotateRight(parentNode);
		}
	}

	public RedBlackTreeIterator<T> Find(T item)
	{
		RedBlackTreeIterator<T> result = LowerBound(item);
		while (result.IsValid && host.Compare(result.Current, item) == 0)
		{
			if (host.Equals(result.Current, item))
			{
				return result;
			}
			result.MoveNext();
		}
		return default;
	}

	public RedBlackTreeIterator<T> LowerBound(T item)
	{
		RedBlackTreeNode<T> redBlackTreeNode = root;
		RedBlackTreeNode<T> node = null;
		while (redBlackTreeNode != null)
		{
			if (host.Compare(redBlackTreeNode.val, item) < 0)
			{
				redBlackTreeNode = redBlackTreeNode.right;
				continue;
			}
			node = redBlackTreeNode;
			redBlackTreeNode = redBlackTreeNode.left;
		}
		return new RedBlackTreeIterator<T>(node);
	}

	public RedBlackTreeIterator<T> UpperBound(T item)
	{
		RedBlackTreeIterator<T> result = LowerBound(item);
		while (result.IsValid && host.Compare(result.Current, item) == 0)
		{
			result.MoveNext();
		}
		return result;
	}

	public RedBlackTreeIterator<T> Begin()
	{
		if (root == null)
		{
			return default;
		}
		return new RedBlackTreeIterator<T>(root.LeftMost);
	}

	public RedBlackTreeIterator<T> GetEnumerator()
	{
		if (root == null)
		{
			return default;
		}
		RedBlackTreeNode<T> redBlackTreeNode = new RedBlackTreeNode<T>(default);
		redBlackTreeNode.right = root;
		return new RedBlackTreeIterator<T>(redBlackTreeNode);
	}

	public bool Contains(T item)
	{
		return Find(item).IsValid;
	}

	public bool Remove(T item)
	{
		RedBlackTreeIterator<T> iterator = Find(item);
		if (!iterator.IsValid)
		{
			return false;
		}
		RemoveAt(iterator);
		return true;
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		using RedBlackTreeIterator<T> redBlackTreeIterator = GetEnumerator();
		while (redBlackTreeIterator.MoveNext())
		{
			T current = redBlackTreeIterator.Current;
			array[arrayIndex++] = current;
		}
	}
}
