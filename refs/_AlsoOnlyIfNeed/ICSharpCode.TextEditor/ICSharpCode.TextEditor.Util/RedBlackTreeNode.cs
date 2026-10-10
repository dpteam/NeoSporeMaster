namespace ICSharpCode.TextEditor.Util;

internal sealed class RedBlackTreeNode<T>
{
	internal RedBlackTreeNode<T> left;

	internal RedBlackTreeNode<T> right;

	internal RedBlackTreeNode<T> parent;

	internal T val;

	internal bool color;

	internal RedBlackTreeNode<T> LeftMost
	{
		get
		{
			RedBlackTreeNode<T> redBlackTreeNode = this;
			while (redBlackTreeNode.left != null)
			{
				redBlackTreeNode = redBlackTreeNode.left;
			}
			return redBlackTreeNode;
		}
	}

	internal RedBlackTreeNode<T> RightMost
	{
		get
		{
			RedBlackTreeNode<T> redBlackTreeNode = this;
			while (redBlackTreeNode.right != null)
			{
				redBlackTreeNode = redBlackTreeNode.right;
			}
			return redBlackTreeNode;
		}
	}

	internal RedBlackTreeNode(T val)
	{
		this.val = val;
	}
}
