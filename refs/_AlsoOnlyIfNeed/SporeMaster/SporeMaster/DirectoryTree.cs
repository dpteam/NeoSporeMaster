using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using VTreeView;

namespace SporeMaster;

internal class DirectoryTree : TreeNode
{
	private class ChildComparer : IComparer<string>
	{
		public int Compare(string x, string y)
		{
			bool xs = x.Length > 0 && x[0] == '#';
			bool ys = y.Length > 0 && y[0] == '#';
			if (xs && !ys)
			{
				return 1;
			}
			if (ys && !xs)
			{
				return -1;
			}
			return x.CompareTo(y);
		}
	}

	public static DirectoryTree Tree = new DirectoryTree();

	private DirectoryTree parent;

	private string fullname;

	private string fullnamelower;

	private bool visible = true;

	private SortedList<string, DirectoryTree> children;

	private int leftPresent;

	private int rightPresent;

	private FullTextIndex leftFullText;

	private FullTextIndex rightFullText;

	private static Collection<TreeNode> noTreeNodes = new Collection<TreeNode>();

	public string FullName => fullname;

	public string BaseName => fullname.Split(new char[1] { '.' }, 2)[0];

	public string FileType
	{
		get
		{
			if (!fullname.Contains('.'))
			{
				return "";
			}
			return fullname.Split(new char[1] { '.' }, 2)[1];
		}
	}

	public Brush LeftBrush
	{
		get
		{
			if (getPresent(0) == 0)
			{
				return Brushes.LightGray;
			}
			return Brushes.Black;
		}
	}

	public Brush RightBrush
	{
		get
		{
			if (getPresent(1) == 0)
			{
				return Brushes.LightGray;
			}
			return Brushes.Black;
		}
	}

	public bool LeftPresent => leftPresent != 0;

	public bool RightPresent => rightPresent != 0;

	public bool IsFolder => children != null;

	public DirectoryTree Parent => parent;

	public string Path
	{
		get
		{
			if (parent == null || parent.parent == null)
			{
				return fullname;
			}
			return parent.Path + "\\" + fullname;
		}
	}

	public override ICollection<TreeNode> Children
	{
		get
		{
			if (children == null)
			{
				return noTreeNodes;
			}
			Collection<TreeNode> c = new Collection<TreeNode>();
			foreach (DirectoryTree i in children.Values)
			{
				if (i.visible && i.leftPresent + i.rightPresent != 0)
				{
					c.Add(i);
				}
			}
			return c;
		}
	}

	public DirectoryTree()
	{
		fullname = "";
		fullnamelower = "";
		children = new SortedList<string, DirectoryTree>(new ChildComparer());
		IsExpanded = true;
	}

	private DirectoryTree(DirectoryTree parent, string fullname)
	{
		this.parent = parent;
		this.fullname = fullname;
		fullnamelower = fullname.ToLower();
	}

	public int getPresent(int side)
	{
		if (side != 1)
		{
			return leftPresent;
		}
		return rightPresent;
	}

	public void incPresent(int side)
	{
		if (side == 1)
		{
			rightPresent++;
		}
		else
		{
			leftPresent++;
		}
		if (parent != null)
		{
			parent.incPresent(side);
		}
	}

	public void decPresent(int side)
	{
		if (getPresent(side) == 0)
		{
			return;
		}
		if (children != null)
		{
			foreach (DirectoryTree c in children.Values)
			{
				c.decPresent(side);
			}
		}
		if (getPresent(side) != 1)
		{
			return;
		}
		for (DirectoryTree p = this; p != null; p = p.parent)
		{
			if (side == 1)
			{
				if (--p.rightPresent != 0)
				{
					break;
				}
			}
			else if (--p.leftPresent != 0)
			{
				break;
			}
		}
	}

	public DirectoryTree getFile(string relativePath, bool create)
	{
		if (relativePath == "")
		{
			return this;
		}
		string[] pathSplit = relativePath.Split(new char[1] { '\\' }, 2);
		string pathNext = pathSplit[0];
		string pathRest = ((pathSplit.Length == 2) ? pathSplit[1] : "");
		if (children.TryGetValue(pathNext, out var t))
		{
			return t.getFile(pathRest, create);
		}
		if (!create)
		{
			return null;
		}
		t = new DirectoryTree(this, pathNext);
		if (pathRest != "")
		{
			t.children = new SortedList<string, DirectoryTree>(new ChildComparer());
		}
		children.Add(pathNext, t);
		return t.getFile(pathRest, create);
	}

	public void SearchRightPresent()
	{
		visible = visible && RightPresent;
		if (children == null)
		{
			return;
		}
		foreach (DirectoryTree c in children.Values)
		{
			c.SearchRightPresent();
		}
	}

	private void makeChildrenVisible()
	{
		visible = true;
		if (children == null)
		{
			return;
		}
		foreach (DirectoryTree c in children.Values)
		{
			c.makeChildrenVisible();
		}
	}

	public bool Search(SearchSpec search)
	{
		makeChildrenVisible();
		SearchSpec.Sequence[] require_all = search.require_all;
		foreach (SearchSpec.Sequence word in require_all)
		{
			SearchInternal(word);
		}
		return visible;
	}

	public bool LeftContains(byte[] bytes)
	{
		if (leftFullText != null)
		{
			return leftFullText.Contains(bytes);
		}
		return false;
	}

	public bool RightContains(byte[] bytes)
	{
		if (rightFullText != null)
		{
			return rightFullText.Contains(bytes);
		}
		return false;
	}

	private bool SearchInternal(SearchSpec.Sequence seq)
	{
		if (!visible)
		{
			return false;
		}
		if (fullnamelower.Contains(seq.as_lower) || LeftContains(seq.as_utf8) || RightContains(seq.as_utf8))
		{
			return true;
		}
		bool found = false;
		if (children != null)
		{
			foreach (DirectoryTree c in children.Values)
			{
				if (c.SearchInternal(seq))
				{
					found = true;
				}
			}
		}
		if (!found)
		{
			visible = false;
		}
		return found;
	}

	public void LoadFullText(int side, string path)
	{
		FullTextIndex i = new FullTextIndex(path);
		if (side == 1)
		{
			rightFullText = i;
		}
		else
		{
			leftFullText = i;
		}
	}
}
