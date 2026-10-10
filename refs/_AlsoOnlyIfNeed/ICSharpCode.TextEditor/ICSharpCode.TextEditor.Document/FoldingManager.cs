using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace ICSharpCode.TextEditor.Document;

public class FoldingManager
{
	private class StartComparer : IComparer<FoldMarker>
	{
		public static readonly StartComparer Instance = new StartComparer();

		public int Compare(FoldMarker x, FoldMarker y)
		{
			if (x.StartLine < y.StartLine)
			{
				return -1;
			}
			if (x.StartLine == y.StartLine)
			{
				return x.StartColumn.CompareTo(y.StartColumn);
			}
			return 1;
		}
	}

	private class EndComparer : IComparer<FoldMarker>
	{
		public static readonly EndComparer Instance = new EndComparer();

		public int Compare(FoldMarker x, FoldMarker y)
		{
			if (x.EndLine < y.EndLine)
			{
				return -1;
			}
			if (x.EndLine == y.EndLine)
			{
				return x.EndColumn.CompareTo(y.EndColumn);
			}
			return 1;
		}
	}

	private List<FoldMarker> foldMarker = new List<FoldMarker>();

	private List<FoldMarker> foldMarkerByEnd = new List<FoldMarker>();

	private IFoldingStrategy foldingStrategy;

	private IDocument document;

	public IList<FoldMarker> FoldMarker => foldMarker.AsReadOnly();

	public IFoldingStrategy FoldingStrategy
	{
		get
		{
			return foldingStrategy;
		}
		set
		{
			foldingStrategy = value;
		}
	}

	public event EventHandler FoldingsChanged;

	internal FoldingManager(IDocument document, LineManager lineTracker)
	{
		this.document = document;
		document.DocumentChanged += DocumentChanged;
	}

	private void DocumentChanged(object sender, DocumentEventArgs e)
	{
		int count = foldMarker.Count;
		document.UpdateSegmentListOnDocumentChange(foldMarker, e);
		if (count != foldMarker.Count)
		{
			document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
		}
	}

	public List<FoldMarker> GetFoldingsFromPosition(int line, int column)
	{
		List<FoldMarker> list = new List<FoldMarker>();
		if (this.foldMarker != null)
		{
			for (int i = 0; i < this.foldMarker.Count; i++)
			{
				FoldMarker foldMarker = this.foldMarker[i];
				if ((foldMarker.StartLine == line && column > foldMarker.StartColumn && (foldMarker.EndLine != line || column < foldMarker.EndColumn)) || (foldMarker.EndLine == line && column < foldMarker.EndColumn && (foldMarker.StartLine != line || column > foldMarker.StartColumn)) || (line > foldMarker.StartLine && line < foldMarker.EndLine))
				{
					list.Add(foldMarker);
				}
			}
		}
		return list;
	}

	private List<FoldMarker> GetFoldingsByStartAfterColumn(int lineNumber, int column, bool forceFolded)
	{
		List<FoldMarker> list = new List<FoldMarker>();
		if (this.foldMarker != null)
		{
			int i = this.foldMarker.BinarySearch(new FoldMarker(document, lineNumber, column, lineNumber, column), StartComparer.Instance);
			if (i < 0)
			{
				i = ~i;
			}
			for (; i < this.foldMarker.Count; i++)
			{
				FoldMarker foldMarker = this.foldMarker[i];
				if (foldMarker.StartLine > lineNumber)
				{
					break;
				}
				if (foldMarker.StartColumn > column && (!forceFolded || foldMarker.IsFolded))
				{
					list.Add(foldMarker);
				}
			}
		}
		return list;
	}

	public List<FoldMarker> GetFoldingsWithStart(int lineNumber)
	{
		return GetFoldingsByStartAfterColumn(lineNumber, -1, forceFolded: false);
	}

	public List<FoldMarker> GetFoldedFoldingsWithStart(int lineNumber)
	{
		return GetFoldingsByStartAfterColumn(lineNumber, -1, forceFolded: true);
	}

	public List<FoldMarker> GetFoldedFoldingsWithStartAfterColumn(int lineNumber, int column)
	{
		return GetFoldingsByStartAfterColumn(lineNumber, column, forceFolded: true);
	}

	private List<FoldMarker> GetFoldingsByEndAfterColumn(int lineNumber, int column, bool forceFolded)
	{
		List<FoldMarker> list = new List<FoldMarker>();
		if (this.foldMarker != null)
		{
			int i = foldMarkerByEnd.BinarySearch(new FoldMarker(document, lineNumber, column, lineNumber, column), EndComparer.Instance);
			if (i < 0)
			{
				i = ~i;
			}
			for (; i < foldMarkerByEnd.Count; i++)
			{
				FoldMarker foldMarker = foldMarkerByEnd[i];
				if (foldMarker.EndLine > lineNumber)
				{
					break;
				}
				if (foldMarker.EndColumn > column && (!forceFolded || foldMarker.IsFolded))
				{
					list.Add(foldMarker);
				}
			}
		}
		return list;
	}

	public List<FoldMarker> GetFoldingsWithEnd(int lineNumber)
	{
		return GetFoldingsByEndAfterColumn(lineNumber, -1, forceFolded: false);
	}

	public List<FoldMarker> GetFoldedFoldingsWithEnd(int lineNumber)
	{
		return GetFoldingsByEndAfterColumn(lineNumber, -1, forceFolded: true);
	}

	public bool IsFoldStart(int lineNumber)
	{
		return GetFoldingsWithStart(lineNumber).Count > 0;
	}

	public bool IsFoldEnd(int lineNumber)
	{
		return GetFoldingsWithEnd(lineNumber).Count > 0;
	}

	public List<FoldMarker> GetFoldingsContainsLineNumber(int lineNumber)
	{
		List<FoldMarker> list = new List<FoldMarker>();
		if (foldMarker != null)
		{
			foreach (FoldMarker item in foldMarker)
			{
				if (item.StartLine < lineNumber && lineNumber < item.EndLine)
				{
					list.Add(item);
				}
			}
		}
		return list;
	}

	public bool IsBetweenFolding(int lineNumber)
	{
		return GetFoldingsContainsLineNumber(lineNumber).Count > 0;
	}

	public bool IsLineVisible(int lineNumber)
	{
		foreach (FoldMarker item in GetFoldingsContainsLineNumber(lineNumber))
		{
			if (item.IsFolded)
			{
				return false;
			}
		}
		return true;
	}

	public List<FoldMarker> GetTopLevelFoldedFoldings()
	{
		List<FoldMarker> list = new List<FoldMarker>();
		if (foldMarker != null)
		{
			Point point = new Point(0, 0);
			foreach (FoldMarker item in foldMarker)
			{
				if (item.IsFolded && (item.StartLine > point.Y || (item.StartLine == point.Y && item.StartColumn >= point.X)))
				{
					list.Add(item);
					point = new Point(item.EndColumn, item.EndLine);
				}
			}
		}
		return list;
	}

	public void UpdateFoldings(string fileName, object parseInfo)
	{
		UpdateFoldings(foldingStrategy.GenerateFoldMarkers(document, fileName, parseInfo));
	}

	public void UpdateFoldings(List<FoldMarker> newFoldings)
	{
		int count = foldMarker.Count;
		lock (this)
		{
			if (newFoldings != null && newFoldings.Count != 0)
			{
				newFoldings.Sort();
				if (foldMarker.Count == newFoldings.Count)
				{
					for (int i = 0; i < foldMarker.Count; i++)
					{
						newFoldings[i].IsFolded = foldMarker[i].IsFolded;
					}
					foldMarker = newFoldings;
				}
				else
				{
					int num = 0;
					int num2 = 0;
					while (num < foldMarker.Count && num2 < newFoldings.Count)
					{
						int num3 = newFoldings[num2].CompareTo(foldMarker[num]);
						if (num3 > 0)
						{
							num++;
							continue;
						}
						if (num3 == 0)
						{
							newFoldings[num2].IsFolded = foldMarker[num].IsFolded;
						}
						num2++;
					}
				}
			}
			if (newFoldings != null)
			{
				foldMarker = newFoldings;
				foldMarkerByEnd = new List<FoldMarker>(newFoldings);
				foldMarkerByEnd.Sort(EndComparer.Instance);
			}
			else
			{
				foldMarker.Clear();
				foldMarkerByEnd.Clear();
			}
		}
		if (count != foldMarker.Count)
		{
			document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
			document.CommitUpdate();
		}
	}

	public string SerializeToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (FoldMarker item in foldMarker)
		{
			stringBuilder.Append(item.Offset);
			stringBuilder.Append("\n");
			stringBuilder.Append(item.Length);
			stringBuilder.Append("\n");
			stringBuilder.Append(item.FoldText);
			stringBuilder.Append("\n");
			stringBuilder.Append(item.IsFolded);
			stringBuilder.Append("\n");
		}
		return stringBuilder.ToString();
	}

	public void DeserializeFromString(string str)
	{
		try
		{
			string[] array = str.Split('\n');
			for (int i = 0; i < array.Length && array[i].Length > 0; i += 4)
			{
				int num = int.Parse(array[i]);
				int num2 = int.Parse(array[i + 1]);
				string foldText = array[i + 2];
				bool isFolded = bool.Parse(array[i + 3]);
				bool flag = false;
				foreach (FoldMarker item in foldMarker)
				{
					if (item.Offset == num && item.Length == num2)
					{
						item.IsFolded = isFolded;
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					foldMarker.Add(new FoldMarker(document, num, num2, foldText, isFolded));
				}
			}
			if (array.Length > 0)
			{
				NotifyFoldingsChanged(EventArgs.Empty);
			}
		}
		catch (Exception)
		{
		}
	}

	public void NotifyFoldingsChanged(EventArgs e)
	{
		if (FoldingsChanged != null)
		{
			FoldingsChanged(this, e);
		}
	}
}
