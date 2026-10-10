using System;
using System.Drawing;

namespace ICSharpCode.TextEditor.Util;

internal abstract class TipSection
{
	private SizeF tipAllocatedSize;

	private Graphics tipGraphics;

	private SizeF tipMaxSize;

	private SizeF tipRequiredSize;

	protected Graphics Graphics => tipGraphics;

	protected SizeF AllocatedSize => tipAllocatedSize;

	protected SizeF MaximumSize => tipMaxSize;

	protected TipSection(Graphics graphics)
	{
		tipGraphics = graphics;
	}

	public abstract void Draw(PointF location);

	public SizeF GetRequiredSize()
	{
		return tipRequiredSize;
	}

	public void SetAllocatedSize(SizeF allocatedSize)
	{
		tipAllocatedSize = allocatedSize;
		OnAllocatedSizeChanged();
	}

	public void SetMaximumSize(SizeF maximumSize)
	{
		tipMaxSize = maximumSize;
		OnMaximumSizeChanged();
	}

	protected virtual void OnAllocatedSizeChanged()
	{
	}

	protected virtual void OnMaximumSizeChanged()
	{
	}

	protected void SetRequiredSize(SizeF requiredSize)
	{
		requiredSize.Width = Math.Max(0f, requiredSize.Width);
		requiredSize.Height = Math.Max(0f, requiredSize.Height);
		requiredSize.Width = Math.Min(tipMaxSize.Width, requiredSize.Width);
		requiredSize.Height = Math.Min(tipMaxSize.Height, requiredSize.Height);
		tipRequiredSize = requiredSize;
	}
}
