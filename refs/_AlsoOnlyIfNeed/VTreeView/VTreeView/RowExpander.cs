using System.Windows;
using System.Windows.Controls;

namespace VTreeView;

public class RowExpander : Control
{
	static RowExpander()
	{
		FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(RowExpander), new FrameworkPropertyMetadata(typeof(RowExpander)));
	}
}
