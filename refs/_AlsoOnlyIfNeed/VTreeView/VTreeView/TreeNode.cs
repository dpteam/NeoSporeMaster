using System.Collections.Generic;
using System.ComponentModel;

namespace VTreeView;

public abstract class TreeNode : INotifyPropertyChanged
{
	private int level;

	private bool _isSelected;

	private bool isExpanded;

	public int Level
	{
		get
		{
			return level;
		}
		set
		{
			level = value;
		}
	}

	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChanged("IsSelected");
			}
		}
	}

	public virtual bool IsExpanded
	{
		get
		{
			return isExpanded;
		}
		set
		{
			if (value != isExpanded)
			{
				isExpanded = value;
				OnPropertyChanged("IsExpanded");
			}
		}
	}

	public abstract ICollection<TreeNode> Children { get; }

	public virtual bool HasChildren => Children.Count > 0;

	public virtual string Name
	{
		get
		{
			return ToString();
		}
		set
		{
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public TreeNode()
	{
	}

	protected void OnPropertyChanged(string propertyName)
	{
		if (PropertyChanged != null)
		{
			PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public virtual void UpdateChildren()
	{
		OnPropertyChanged("HasChildren");
	}
}
