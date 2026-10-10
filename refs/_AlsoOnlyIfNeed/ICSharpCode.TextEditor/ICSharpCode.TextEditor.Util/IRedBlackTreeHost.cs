using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Util;

internal interface IRedBlackTreeHost<T> : IComparer<T>
{
	bool Equals(T a, T b);

	void UpdateAfterChildrenChange(RedBlackTreeNode<T> node);

	void UpdateAfterRotateLeft(RedBlackTreeNode<T> node);

	void UpdateAfterRotateRight(RedBlackTreeNode<T> node);
}
