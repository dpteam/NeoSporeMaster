using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace _3DTools;

[ContentProperty("Content")]
public abstract class Viewport3DDecorator : FrameworkElement, IAddChild
{
	private UIElementCollection _preViewportChildren;

	private UIElementCollection _postViewportChildren;

	private UIElement _content;

	public UIElement Content
	{
		get
		{
			return _content;
		}
		set
		{
			if (!(value is Viewport3D) && !(value is Viewport3DDecorator))
			{
				throw new ArgumentException("Not a valid child type", "value");
			}
			if (_content != value)
			{
				UIElement oldContent = _content;
				RemoveVisualChild(oldContent);
				RemoveLogicalChild(oldContent);
				_content = value;
				AddLogicalChild(value);
				AddVisualChild(value);
				OnViewport3DDecoratorContentChange(oldContent, value);
				BindToContentsWidthHeight(value);
				InvalidateMeasure();
			}
		}
	}

	public Viewport3D Viewport3D
	{
		get
		{
			Viewport3D viewport3D = null;
			Viewport3DDecorator currEnhancer = this;
			while (true)
			{
				UIElement currContent = currEnhancer.Content;
				if (currContent == null)
				{
					break;
				}
				if (currContent is Viewport3D)
				{
					viewport3D = (Viewport3D)currContent;
					break;
				}
				currEnhancer = (Viewport3DDecorator)currContent;
			}
			return viewport3D;
		}
	}

	protected UIElementCollection PreViewportChildren => _preViewportChildren;

	protected UIElementCollection PostViewportChildren => _postViewportChildren;

	protected override int VisualChildrenCount
	{
		get
		{
			int contentCount = ((Content != null) ? 1 : 0);
			return PreViewportChildren.Count + PostViewportChildren.Count + contentCount;
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

	public Viewport3DDecorator()
	{
		_preViewportChildren = new UIElementCollection(this, this);
		_postViewportChildren = new UIElementCollection(this, this);
		_content = null;
	}

	private void BindToContentsWidthHeight(UIElement newContent)
	{
		Binding _widthBinding = new Binding("Width");
		_widthBinding.Mode = BindingMode.OneWay;
		Binding _heightBinding = new Binding("Height");
		_heightBinding.Mode = BindingMode.OneWay;
		_widthBinding.Source = newContent;
		_heightBinding.Source = newContent;
		BindingOperations.SetBinding(this, FrameworkElement.WidthProperty, _widthBinding);
		BindingOperations.SetBinding(this, FrameworkElement.HeightProperty, _heightBinding);
		Binding _maxWidthBinding = new Binding("MaxWidth");
		_maxWidthBinding.Mode = BindingMode.OneWay;
		Binding _maxHeightBinding = new Binding("MaxHeight");
		_maxHeightBinding.Mode = BindingMode.OneWay;
		_maxWidthBinding.Source = newContent;
		_maxHeightBinding.Source = newContent;
		BindingOperations.SetBinding(this, FrameworkElement.MaxWidthProperty, _maxWidthBinding);
		BindingOperations.SetBinding(this, FrameworkElement.MaxHeightProperty, _maxHeightBinding);
		Binding _minWidthBinding = new Binding("MinWidth");
		_minWidthBinding.Mode = BindingMode.OneWay;
		Binding _minHeightBinding = new Binding("MinHeight");
		_minHeightBinding.Mode = BindingMode.OneWay;
		_minWidthBinding.Source = newContent;
		_minHeightBinding.Source = newContent;
		BindingOperations.SetBinding(this, FrameworkElement.MinWidthProperty, _minWidthBinding);
		BindingOperations.SetBinding(this, FrameworkElement.MinHeightProperty, _minHeightBinding);
	}

	protected virtual void OnViewport3DDecoratorContentChange(UIElement oldContent, UIElement newContent)
	{
	}

	protected override Visual GetVisualChild(int index)
	{
		int orginalIndex = index;
		if (index < PreViewportChildren.Count)
		{
			return PreViewportChildren[index];
		}
		index -= PreViewportChildren.Count;
		if (Content != null && index == 0)
		{
			return Content;
		}
		index -= ((Content != null) ? 1 : 0);
		if (index < PostViewportChildren.Count)
		{
			return PostViewportChildren[index];
		}
		throw new ArgumentOutOfRangeException("index", orginalIndex, "Out of range visual requested");
	}

	protected override Size MeasureOverride(Size constraint)
	{
		Size finalSize = default;
		MeasurePreViewportChildren(constraint);
		if (Content != null)
		{
			Content.Measure(constraint);
			finalSize = Content.DesiredSize;
		}
		MeasurePostViewportChildren(constraint);
		return finalSize;
	}

	protected virtual void MeasurePreViewportChildren(Size constraint)
	{
		MeasureUIElementCollection(PreViewportChildren, constraint);
	}

	protected virtual void MeasurePostViewportChildren(Size constraint)
	{
		MeasureUIElementCollection(PostViewportChildren, constraint);
	}

	private void MeasureUIElementCollection(UIElementCollection collection, Size constraint)
	{
		foreach (UIElement uiElem in collection)
		{
			uiElem.Measure(constraint);
		}
	}

	protected override Size ArrangeOverride(Size arrangeSize)
	{
		ArrangePreViewportChildren(arrangeSize);
		if (Content != null)
		{
			Content.Arrange(new Rect(arrangeSize));
		}
		ArrangePostViewportChildren(arrangeSize);
		return arrangeSize;
	}

	protected virtual void ArrangePreViewportChildren(Size arrangeSize)
	{
		ArrangeUIElementCollection(PreViewportChildren, arrangeSize);
	}

	protected virtual void ArrangePostViewportChildren(Size arrangeSize)
	{
		ArrangeUIElementCollection(PostViewportChildren, arrangeSize);
	}

	private void ArrangeUIElementCollection(UIElementCollection collection, Size constraint)
	{
		foreach (UIElement uiElem in collection)
		{
			uiElem.Arrange(new Rect(constraint));
		}
	}

	void IAddChild.AddChild(object value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (Content != null)
		{
			throw new ArgumentException("Viewport3DDecorator can only have one child");
		}
		Content = (UIElement)value;
	}

	void IAddChild.AddText(string text)
	{
		for (int i = 0; i < text.Length; i++)
		{
			if (!char.IsWhiteSpace(text[i]))
			{
				throw new ArgumentException("Non whitespace in add text", text);
			}
		}
	}
}
