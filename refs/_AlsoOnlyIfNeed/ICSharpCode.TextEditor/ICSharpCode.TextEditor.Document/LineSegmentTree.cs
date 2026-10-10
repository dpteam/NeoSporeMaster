using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ICSharpCode.TextEditor.Util;

namespace ICSharpCode.TextEditor.Document;

internal sealed class LineSegmentTree : IList<LineSegment>, ICollection<LineSegment>, IEnumerable<LineSegment>, IEnumerable
{
	internal struct RBNode(LineSegment lineSegment)
	{
		internal LineSegment lineSegment = lineSegment;

		internal int count = 1;

		internal int totalLength = lineSegment.TotalLength;

		public override string ToString()
		{
			return "[RBNode count=" + count + " totalLength=" + totalLength + " lineSegment.LineNumber=" + lineSegment.LineNumber + " lineSegment.Offset=" + lineSegment.Offset + " lineSegment.TotalLength=" + lineSegment.TotalLength + " lineSegment.DelimiterLength=" + lineSegment.DelimiterLength + "]";
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct MyHost : IRedBlackTreeHost<RBNode>, IComparer<RBNode>
	{
		public int Compare(RBNode x, RBNode y)
		{
			throw new NotImplementedException();
		}

		public bool Equals(RBNode a, RBNode b)
		{
			throw new NotImplementedException();
		}

		public void UpdateAfterChildrenChange(RedBlackTreeNode<RBNode> node)
		{
			int num = 1;
			int num2 = node.val.lineSegment.TotalLength;
			if (node.left != null)
			{
				num += node.left.val.count;
				num2 += node.left.val.totalLength;
			}
			if (node.right != null)
			{
				num += node.right.val.count;
				num2 += node.right.val.totalLength;
			}
			if (num != node.val.count || num2 != node.val.totalLength)
			{
				node.val.count = num;
				node.val.totalLength = num2;
				if (node.parent != null)
				{
					UpdateAfterChildrenChange(node.parent);
				}
			}
		}

		public void UpdateAfterRotateLeft(RedBlackTreeNode<RBNode> node)
		{
			UpdateAfterChildrenChange(node);
			UpdateAfterChildrenChange(node.parent);
		}

		public void UpdateAfterRotateRight(RedBlackTreeNode<RBNode> node)
		{
			UpdateAfterChildrenChange(node);
			UpdateAfterChildrenChange(node.parent);
		}
	}

	public struct Enumerator : IEnumerator<LineSegment>, IDisposable, IEnumerator
	{
		public static readonly Enumerator Invalid = default;

		internal RedBlackTreeIterator<RBNode> it;

		public LineSegment Current => it.Current.lineSegment;

		public bool IsValid => it.IsValid;

		public int CurrentIndex
		{
			get
			{
				if (it.node == null)
				{
					throw new InvalidOperationException();
				}
				return GetIndexFromNode(it.node);
			}
		}

		public int CurrentOffset
		{
			get
			{
				if (it.node == null)
				{
					throw new InvalidOperationException();
				}
				return GetOffsetFromNode(it.node);
			}
		}

		object IEnumerator.Current => it.Current.lineSegment;

		internal Enumerator(RedBlackTreeIterator<RBNode> it)
		{
			this.it = it;
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			return it.MoveNext();
		}

		public bool MoveBack()
		{
			return it.MoveBack();
		}

		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	private readonly AugmentableRedBlackTree<RBNode, MyHost> tree = new AugmentableRedBlackTree<RBNode, MyHost>(default);

	public int TotalLength
	{
		get
		{
			if (tree.root == null)
			{
				return 0;
			}
			return tree.root.val.totalLength;
		}
	}

	public int Count => tree.Count;

	public LineSegment this[int index]
	{
		get
		{
			return GetNode(index).val.lineSegment;
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	bool ICollection<LineSegment>.IsReadOnly => true;

	private RedBlackTreeNode<RBNode> GetNode(int index)
	{
		if (index < 0 || index >= tree.Count)
		{
			throw new ArgumentOutOfRangeException("index", index, "index should be between 0 and " + (tree.Count - 1));
		}
		RedBlackTreeNode<RBNode> redBlackTreeNode = tree.root;
		while (true)
		{
			if (redBlackTreeNode.left != null && index < redBlackTreeNode.left.val.count)
			{
				redBlackTreeNode = redBlackTreeNode.left;
				continue;
			}
			if (redBlackTreeNode.left != null)
			{
				index -= redBlackTreeNode.left.val.count;
			}
			if (index == 0)
			{
				break;
			}
			index--;
			redBlackTreeNode = redBlackTreeNode.right;
		}
		return redBlackTreeNode;
	}

	private static int GetIndexFromNode(RedBlackTreeNode<RBNode> node)
	{
		int num = ((node.left != null) ? node.left.val.count : 0);
		while (node.parent != null)
		{
			if (node == node.parent.right)
			{
				if (node.parent.left != null)
				{
					num += node.parent.left.val.count;
				}
				num++;
			}
			node = node.parent;
		}
		return num;
	}

	private RedBlackTreeNode<RBNode> GetNodeByOffset(int offset)
	{
		if (offset < 0 || offset > TotalLength)
		{
			throw new ArgumentOutOfRangeException("offset", offset, "offset should be between 0 and " + TotalLength);
		}
		if (offset == TotalLength)
		{
			if (tree.root == null)
			{
				throw new InvalidOperationException("Cannot call GetNodeByOffset while tree is empty.");
			}
			return tree.root.RightMost;
		}
		RedBlackTreeNode<RBNode> redBlackTreeNode = tree.root;
		while (true)
		{
			if (redBlackTreeNode.left != null && offset < redBlackTreeNode.left.val.totalLength)
			{
				redBlackTreeNode = redBlackTreeNode.left;
				continue;
			}
			if (redBlackTreeNode.left != null)
			{
				offset -= redBlackTreeNode.left.val.totalLength;
			}
			offset -= redBlackTreeNode.val.lineSegment.TotalLength;
			if (offset < 0)
			{
				break;
			}
			redBlackTreeNode = redBlackTreeNode.right;
		}
		return redBlackTreeNode;
	}

	private static int GetOffsetFromNode(RedBlackTreeNode<RBNode> node)
	{
		int num = ((node.left != null) ? node.left.val.totalLength : 0);
		while (node.parent != null)
		{
			if (node == node.parent.right)
			{
				if (node.parent.left != null)
				{
					num += node.parent.left.val.totalLength;
				}
				num += node.parent.val.lineSegment.TotalLength;
			}
			node = node.parent;
		}
		return num;
	}

	public LineSegment GetByOffset(int offset)
	{
		return GetNodeByOffset(offset).val.lineSegment;
	}

	public void SetSegmentLength(LineSegment segment, int newTotalLength)
	{
		if (segment == null)
		{
			throw new ArgumentNullException("segment");
		}
		RedBlackTreeNode<RBNode> node = segment.treeEntry.it.node;
		segment.TotalLength = newTotalLength;
		default(MyHost).UpdateAfterChildrenChange(node);
	}

	public void RemoveSegment(LineSegment segment)
	{
		tree.RemoveAt(segment.treeEntry.it);
	}

	public LineSegment InsertSegmentAfter(LineSegment segment, int length)
	{
		LineSegment lineSegment = new LineSegment();
		lineSegment.TotalLength = length;
		lineSegment.DelimiterLength = segment.DelimiterLength;
		lineSegment.treeEntry = InsertAfter(segment.treeEntry.it.node, lineSegment);
		return lineSegment;
	}

	private Enumerator InsertAfter(RedBlackTreeNode<RBNode> node, LineSegment newSegment)
	{
		RedBlackTreeNode<RBNode> redBlackTreeNode = new RedBlackTreeNode<RBNode>(new RBNode(newSegment));
		if (node.right == null)
		{
			tree.InsertAsRight(node, redBlackTreeNode);
		}
		else
		{
			tree.InsertAsLeft(node.right.LeftMost, redBlackTreeNode);
		}
		return new Enumerator(new RedBlackTreeIterator<RBNode>(redBlackTreeNode));
	}

	public int IndexOf(LineSegment item)
	{
		int lineNumber = item.LineNumber;
		if (lineNumber < 0 || lineNumber >= Count)
		{
			return -1;
		}
		if (item != this[lineNumber])
		{
			return -1;
		}
		return lineNumber;
	}

	void IList<LineSegment>.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	public LineSegmentTree()
	{
		Clear();
	}

	public void Clear()
	{
		tree.Clear();
		LineSegment lineSegment = new LineSegment();
		lineSegment.TotalLength = 0;
		lineSegment.DelimiterLength = 0;
		tree.Add(new RBNode(lineSegment));
		lineSegment.treeEntry = GetEnumeratorForIndex(0);
	}

	public bool Contains(LineSegment item)
	{
		return IndexOf(item) >= 0;
	}

	public void CopyTo(LineSegment[] array, int arrayIndex)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		using Enumerator enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			LineSegment current = enumerator.Current;
			array[arrayIndex++] = current;
		}
	}

	IEnumerator<LineSegment> IEnumerable<LineSegment>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(tree.GetEnumerator());
	}

	public Enumerator GetEnumeratorForIndex(int index)
	{
		return new Enumerator(new RedBlackTreeIterator<RBNode>(GetNode(index)));
	}

	public Enumerator GetEnumeratorForOffset(int offset)
	{
		return new Enumerator(new RedBlackTreeIterator<RBNode>(GetNodeByOffset(offset)));
	}

	void IList<LineSegment>.Insert(int index, LineSegment item)
	{
		throw new NotSupportedException();
	}

	void ICollection<LineSegment>.Add(LineSegment item)
	{
		throw new NotSupportedException();
	}

	bool ICollection<LineSegment>.Remove(LineSegment item)
	{
		throw new NotSupportedException();
	}
}
