using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace VTreeView;

public class VTreeView : ListBox
{
	private TreeData _data;

	private bool doubleClickExpand = true;

	private ScrollViewer m_scrollViewer;

	public TreeData Data => _data;

	public bool DoubleClickExpand
	{
		get
		{
			return doubleClickExpand;
		}
		set
		{
			doubleClickExpand = value;
		}
	}

	public ScrollViewer ScrollViewer
	{
		get
		{
			if (m_scrollViewer == null)
			{
				DependencyObject border = VisualTreeHelper.GetChild(this, 0);
				if (border != null)
				{
					m_scrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
				}
			}
			return m_scrollViewer;
		}
	}

	public VTreeView()
	{
		MouseDoubleClick += VTreeView_MouseDoubleClick;
		_data = new TreeData();
		ItemsSource = _data.Items;
	}

	static VTreeView()
	{
		FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(VTreeView), new FrameworkPropertyMetadata(typeof(VTreeView)));
	}

	private void VTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (DoubleClickExpand && e.OriginalSource is FrameworkElement fe)
		{
			TreeNode TN = (TreeNode)fe.DataContext;
			if (TN != null)
			{
				TN.IsExpanded = !TN.IsExpanded;
				e.Handled = true;
			}
		}
	}
}
