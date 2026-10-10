using System;
using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Undo;

internal sealed class UndoQueue : IUndoableOperation
{
	private List<IUndoableOperation> undolist = new List<IUndoableOperation>();

	public UndoQueue(Stack<IUndoableOperation> stack, int numops)
	{
		if (stack == null)
		{
			throw new ArgumentNullException("stack");
		}
		if (numops > stack.Count)
		{
			numops = stack.Count;
		}
		for (int i = 0; i < numops; i++)
		{
			undolist.Add(stack.Pop());
		}
	}

	public void Undo()
	{
		for (int i = 0; i < undolist.Count; i++)
		{
			undolist[i].Undo();
		}
	}

	public void Redo()
	{
		for (int num = undolist.Count - 1; num >= 0; num--)
		{
			undolist[num].Redo();
		}
	}
}
