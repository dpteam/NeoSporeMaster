using System;
using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Document;

public sealed class MarkerStrategy
{
	private List<TextMarker> textMarker = new List<TextMarker>();

	private IDocument document;

	private Dictionary<int, List<TextMarker>> markersTable = new Dictionary<int, List<TextMarker>>();

	public IDocument Document => document;

	public IEnumerable<TextMarker> TextMarker => textMarker.AsReadOnly();

	public void AddMarker(TextMarker item)
	{
		markersTable.Clear();
		textMarker.Add(item);
	}

	public void InsertMarker(int index, TextMarker item)
	{
		markersTable.Clear();
		textMarker.Insert(index, item);
	}

	public void RemoveMarker(TextMarker item)
	{
		markersTable.Clear();
		textMarker.Remove(item);
	}

	public void RemoveAll(Predicate<TextMarker> match)
	{
		markersTable.Clear();
		textMarker.RemoveAll(match);
	}

	public MarkerStrategy(IDocument document)
	{
		this.document = document;
		document.DocumentChanged += DocumentChanged;
	}

	public List<TextMarker> GetMarkers(int offset)
	{
		if (!markersTable.ContainsKey(offset))
		{
			List<TextMarker> list = new List<TextMarker>();
			for (int i = 0; i < this.textMarker.Count; i++)
			{
				TextMarker textMarker = this.textMarker[i];
				if (textMarker.Offset <= offset && offset <= textMarker.EndOffset)
				{
					list.Add(textMarker);
				}
			}
			markersTable[offset] = list;
		}
		return markersTable[offset];
	}

	public List<TextMarker> GetMarkers(int offset, int length)
	{
		int num = offset + length - 1;
		List<TextMarker> list = new List<TextMarker>();
		for (int i = 0; i < this.textMarker.Count; i++)
		{
			TextMarker textMarker = this.textMarker[i];
			if ((textMarker.Offset <= offset && offset <= textMarker.EndOffset) || (textMarker.Offset <= num && num <= textMarker.EndOffset) || (offset <= textMarker.Offset && textMarker.Offset <= num) || (offset <= textMarker.EndOffset && textMarker.EndOffset <= num))
			{
				list.Add(textMarker);
			}
		}
		return list;
	}

	public List<TextMarker> GetMarkers(TextLocation position)
	{
		if (position.Y >= document.TotalNumberOfLines || position.Y < 0)
		{
			return new List<TextMarker>();
		}
		LineSegment lineSegment = document.GetLineSegment(position.Y);
		return GetMarkers(lineSegment.Offset + position.X);
	}

	private void DocumentChanged(object sender, DocumentEventArgs e)
	{
		markersTable.Clear();
		document.UpdateSegmentListOnDocumentChange(textMarker, e);
	}
}
