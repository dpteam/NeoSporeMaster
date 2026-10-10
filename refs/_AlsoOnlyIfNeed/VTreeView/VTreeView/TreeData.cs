using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace VTreeView;

public class TreeData : INotifyPropertyChanged
{
	public delegate void NodeExpandedChanagedEventHandler(object sender, NodeExpandedChanagedEventArgs e);

	public class NodeExpandedChanagedEventArgs : EventArgs
	{
		private TreeNode _tN;

		public TreeNode TN => _tN;

		public NodeExpandedChanagedEventArgs(TreeNode tN)
		{
			_tN = tN;
		}
	}

	private ObservableCollection<TreeNode> items = new ObservableCollection<TreeNode>();

	public ObservableCollection<TreeNode> Items => items;

	public event PropertyChangedEventHandler PropertyChanged;

	public event NodeExpandedChanagedEventHandler NodeExpandedChanaged;

	public void AddRootItem(TreeNode TN)
	{
		TN.Level = 0;
		TN.PropertyChanged += TN_PropertyChanged;
		items.Add(TN);
		if (TN.IsExpanded)
		{
			PopulateChildren(TN);
		}
	}

	public void AddRootItems(IEnumerable<TreeNode> TNs)
	{
		IEnumerator<TreeNode> myEnumerator = TNs.GetEnumerator();
		while (myEnumerator.MoveNext())
		{
			AddRootItem(myEnumerator.Current);
		}
	}

	public void ClearAll()
	{
		items.Clear();
	}

	private void PopulateChildren(TreeNode TN)
	{
		if (!items.Contains(TN))
		{
			return;
		}
		int index = items.IndexOf(TN);
		if (index != items.Count - 1 && items[index + 1].Level > TN.Level)
		{
			return;
		}
		IEnumerable myList = TN.Children;
		int offset = 0;
		foreach (TreeNode TN2 in myList)
		{
			TN2.PropertyChanged += TN_PropertyChanged;
			TN2.Level = TN.Level + 1;
			items.Insert(index + offset + 1, TN2);
			offset++;
		}
		foreach (TreeNode TN3 in myList)
		{
			if (TN3.IsExpanded && TN.HasChildren)
			{
				PopulateChildren(TN3);
			}
		}
	}

	private void TN_PropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "IsExpanded")
		{
			TreeNode TN = (TreeNode)sender;
			if (TN.IsExpanded)
			{
				PopulateChildren(TN);
			}
			else
			{
				ClearChildren(TN);
			}
			if (NodeExpandedChanaged != null)
			{
				NodeExpandedChanaged(this, new NodeExpandedChanagedEventArgs(TN));
			}
		}
	}

	private void ClearChildren(TreeNode TN)
	{
		if (items.Contains(TN))
		{
			int indexToRemove = items.IndexOf(TN) + 1;
			while (indexToRemove < items.Count && items[indexToRemove].Level > TN.Level)
			{
				items[indexToRemove].PropertyChanged -= TN_PropertyChanged;
				items.RemoveAt(indexToRemove);
			}
		}
	}

	private void OnPropertyChanged(string propertyName)
	{
		if (PropertyChanged != null)
		{
			PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public void RemoveNode(TreeNode TN)
	{
		ClearChildren(TN);
		if (items.Contains(TN))
		{
			items.Remove(TN);
		}
	}

	public void UpdateChildren(TreeNode TN)
	{
		TN.UpdateChildren();
		if (!TN.HasChildren)
		{
			TN.IsExpanded = false;
		}
		else if (TN.IsExpanded && items.Contains(TN))
		{
			ClearChildren(TN);
			PopulateChildren(TN);
		}
	}
}
