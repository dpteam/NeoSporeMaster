using System;
using System.Collections;
using System.Windows;
using System.Windows.Media;

namespace _3DTools;

internal class VisualDecorator : FrameworkElement
{
	private Visual _visual;

	public Visual Content
	{
		get
		{
			return _visual;
		}
		set
		{
			if (_visual != value)
			{
				Visual oldVisual = _visual;
				RemoveVisualChild(oldVisual);
				RemoveLogicalChild(oldVisual);
				_visual = value;
				AddLogicalChild(value);
				AddVisualChild(value);
			}
		}
	}

	protected override int VisualChildrenCount
	{
		get
		{
			if (Content == null)
			{
				return 0;
			}
			return 1;
		}
	}

	protected override IEnumerator LogicalChildren
	{
		get
		{
			Visual[] logicalChildren = new Visual[VisualChildrenCount];
			for (int i = 0; i < VisualChildrenCount; i++)
			{
				logicalChildren[i] = GetVisualChild(i);
			}
			return logicalChildren.GetEnumerator();
		}
	}

	public VisualDecorator()
	{
		_visual = null;
	}

	protected override Visual GetVisualChild(int index)
	{
		if (index == 0 && Content != null)
		{
			return _visual;
		}
		throw new ArgumentOutOfRangeException("index", index, "Out of range visual requested");
	}
}
