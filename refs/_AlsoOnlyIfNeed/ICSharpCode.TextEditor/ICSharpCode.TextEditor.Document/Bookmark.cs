using System;
using System.Drawing;
using System.Windows.Forms;

namespace ICSharpCode.TextEditor.Document;

public class Bookmark
{
	private IDocument document;

	private TextAnchor anchor;

	private TextLocation location;

	private bool isEnabled = true;

	public IDocument Document
	{
		get
		{
			return document;
		}
		set
		{
			if (document != value)
			{
				if (anchor != null)
				{
					location = anchor.Location;
					anchor = null;
				}
				document = value;
				CreateAnchor();
				OnDocumentChanged(EventArgs.Empty);
			}
		}
	}

	public TextAnchor Anchor => anchor;

	public TextLocation Location
	{
		get
		{
			if (anchor != null)
			{
				return anchor.Location;
			}
			return location;
		}
		set
		{
			location = value;
			CreateAnchor();
		}
	}

	public bool IsEnabled
	{
		get
		{
			return isEnabled;
		}
		set
		{
			if (isEnabled != value)
			{
				isEnabled = value;
				if (document != null)
				{
					document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, LineNumber));
					document.CommitUpdate();
				}
				OnIsEnabledChanged(EventArgs.Empty);
			}
		}
	}

	public int LineNumber
	{
		get
		{
			if (anchor != null)
			{
				return anchor.LineNumber;
			}
			return location.Line;
		}
	}

	public int ColumnNumber
	{
		get
		{
			if (anchor != null)
			{
				return anchor.ColumnNumber;
			}
			return location.Column;
		}
	}

	public virtual bool CanToggle => true;

	public event EventHandler DocumentChanged;

	public event EventHandler IsEnabledChanged;

	private void CreateAnchor()
	{
		if (document != null)
		{
			LineSegment lineSegment = document.GetLineSegment(Math.Max(0, Math.Min(location.Line, document.TotalNumberOfLines - 1)));
			anchor = lineSegment.CreateAnchor(Math.Max(0, Math.Min(location.Column, lineSegment.Length)));
			anchor.MovementType = AnchorMovementType.AfterInsertion;
			anchor.Deleted += AnchorDeleted;
		}
	}

	private void AnchorDeleted(object sender, EventArgs e)
	{
		document.BookmarkManager.RemoveMark(this);
	}

	protected virtual void OnDocumentChanged(EventArgs e)
	{
		if (DocumentChanged != null)
		{
			DocumentChanged(this, e);
		}
	}

	protected virtual void OnIsEnabledChanged(EventArgs e)
	{
		if (IsEnabledChanged != null)
		{
			IsEnabledChanged(this, e);
		}
	}

	public Bookmark(IDocument document, TextLocation location)
		: this(document, location, isEnabled: true)
	{
	}

	public Bookmark(IDocument document, TextLocation location, bool isEnabled)
	{
		this.document = document;
		this.isEnabled = isEnabled;
		Location = location;
	}

	public virtual bool Click(Control parent, MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left && CanToggle)
		{
			document.BookmarkManager.RemoveMark(this);
			return true;
		}
		return false;
	}

	public virtual void Draw(IconBarMargin margin, Graphics g, Point p)
	{
		margin.DrawBookmark(g, p.Y, isEnabled);
	}
}
