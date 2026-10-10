using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ICSharpCode.TextEditor.Document;

public class BookmarkManager
{
	private IDocument document;

	private List<Bookmark> bookmark = new List<Bookmark>();

	public ReadOnlyCollection<Bookmark> Marks => new ReadOnlyCollection<Bookmark>(bookmark);

	public IDocument Document => document;

	public IBookmarkFactory Factory { get; set; }

	public event BookmarkEventHandler Removed;

	public event BookmarkEventHandler Added;

	internal BookmarkManager(IDocument document, LineManager lineTracker)
	{
		this.document = document;
	}

	public void ToggleMarkAt(TextLocation location)
	{
		Bookmark bookmark = ((Factory == null) ? new Bookmark(document, location) : Factory.CreateBookmark(document, location));
		Type type = bookmark.GetType();
		for (int i = 0; i < this.bookmark.Count; i++)
		{
			Bookmark bookmark2 = this.bookmark[i];
			if (bookmark2.LineNumber == location.Line && bookmark2.CanToggle && bookmark2.GetType() == type)
			{
				this.bookmark.RemoveAt(i);
				OnRemoved(new BookmarkEventArgs(bookmark2));
				return;
			}
		}
		this.bookmark.Add(bookmark);
		OnAdded(new BookmarkEventArgs(bookmark));
	}

	public void AddMark(Bookmark mark)
	{
		bookmark.Add(mark);
		OnAdded(new BookmarkEventArgs(mark));
	}

	public void RemoveMark(Bookmark mark)
	{
		bookmark.Remove(mark);
		OnRemoved(new BookmarkEventArgs(mark));
	}

	public void RemoveMarks(Predicate<Bookmark> predicate)
	{
		for (int i = 0; i < bookmark.Count; i++)
		{
			Bookmark obj = bookmark[i];
			if (predicate(obj))
			{
				bookmark.RemoveAt(i--);
				OnRemoved(new BookmarkEventArgs(obj));
			}
		}
	}

	public bool IsMarked(int lineNr)
	{
		for (int i = 0; i < bookmark.Count; i++)
		{
			if (bookmark[i].LineNumber == lineNr)
			{
				return true;
			}
		}
		return false;
	}

	public void Clear()
	{
		foreach (Bookmark item in bookmark)
		{
			OnRemoved(new BookmarkEventArgs(item));
		}
		bookmark.Clear();
	}

	public Bookmark GetFirstMark(Predicate<Bookmark> predicate)
	{
		if (this.bookmark.Count < 1)
		{
			return null;
		}
		Bookmark bookmark = null;
		for (int i = 0; i < this.bookmark.Count; i++)
		{
			if (predicate(this.bookmark[i]) && this.bookmark[i].IsEnabled && (bookmark == null || this.bookmark[i].LineNumber < bookmark.LineNumber))
			{
				bookmark = this.bookmark[i];
			}
		}
		return bookmark;
	}

	public Bookmark GetLastMark(Predicate<Bookmark> predicate)
	{
		if (this.bookmark.Count < 1)
		{
			return null;
		}
		Bookmark bookmark = null;
		for (int i = 0; i < this.bookmark.Count; i++)
		{
			if (predicate(this.bookmark[i]) && this.bookmark[i].IsEnabled && (bookmark == null || this.bookmark[i].LineNumber > bookmark.LineNumber))
			{
				bookmark = this.bookmark[i];
			}
		}
		return bookmark;
	}

	private bool AcceptAnyMarkPredicate(Bookmark mark)
	{
		return true;
	}

	public Bookmark GetNextMark(int curLineNr)
	{
		return GetNextMark(curLineNr, AcceptAnyMarkPredicate);
	}

	public Bookmark GetNextMark(int curLineNr, Predicate<Bookmark> predicate)
	{
		if (this.bookmark.Count == 0)
		{
			return null;
		}
		Bookmark bookmark = GetFirstMark(predicate);
		foreach (Bookmark item in this.bookmark)
		{
			if (predicate(item) && item.IsEnabled && item.LineNumber > curLineNr && (item.LineNumber < bookmark.LineNumber || bookmark.LineNumber <= curLineNr))
			{
				bookmark = item;
			}
		}
		return bookmark;
	}

	public Bookmark GetPrevMark(int curLineNr)
	{
		return GetPrevMark(curLineNr, AcceptAnyMarkPredicate);
	}

	public Bookmark GetPrevMark(int curLineNr, Predicate<Bookmark> predicate)
	{
		if (this.bookmark.Count == 0)
		{
			return null;
		}
		Bookmark bookmark = GetLastMark(predicate);
		foreach (Bookmark item in this.bookmark)
		{
			if (predicate(item) && item.IsEnabled && item.LineNumber < curLineNr && (item.LineNumber > bookmark.LineNumber || bookmark.LineNumber >= curLineNr))
			{
				bookmark = item;
			}
		}
		return bookmark;
	}

	protected virtual void OnRemoved(BookmarkEventArgs e)
	{
		if (Removed != null)
		{
			Removed(this, e);
		}
	}

	protected virtual void OnAdded(BookmarkEventArgs e)
	{
		if (Added != null)
		{
			Added(this, e);
		}
	}
}
