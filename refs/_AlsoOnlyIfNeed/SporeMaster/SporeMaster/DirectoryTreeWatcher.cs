using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Threading;

namespace SporeMaster;

internal class DirectoryTreeWatcher
{
	public delegate void ChangeHandler();

	private delegate void changeDelegate();

	private delegate void onFileChangedDelegate(FileSystemEventArgs e);

	private delegate void onFileRenamedDelegate(RenamedEventArgs e);

	private DirectoryTree target;

	private int side;

	private string path;

	private FileSystemWatcher fswatch;

	private Dispatcher disp;

	private bool stopped;

	private HashSet<string> fullTextExtensions;

	public string Path => path;

	public event ChangeHandler Change;

	public DirectoryTreeWatcher(DirectoryTree target, int side, string path, Dispatcher disp, ChangeHandler Change, HashSet<string> fullTextExtensions, PleaseWait initProgress)
	{
		this.target = target;
		this.side = side;
		this.path = path;
		this.Change = (ChangeHandler)Delegate.Combine(this.Change, Change);
		this.disp = disp;
		this.fullTextExtensions = fullTextExtensions;
		init(initProgress);
	}

	public void Dispose()
	{
		stopped = true;
		fswatch.Dispose();
		target.decPresent(side);
	}

	private void init(PleaseWait progress)
	{
		fswatch = new FileSystemWatcher(path);
		fswatch.Changed += thr_onFileChanged;
		fswatch.Deleted += thr_onFileChanged;
		fswatch.Renamed += thr_onFileRenamed;
		fswatch.Created += thr_onFileChanged;
		fswatch.IncludeSubdirectories = true;
		progress?.beginTask(0.25, 1.0);
		string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
		long[] sizes = new long[files.Length];
		double total_size = 0.0;
		for (int i = 0; i < files.Length; i++)
		{
			sizes[i] = new FileInfo(files[i]).Length;
			total_size += (double)sizes[i];
		}
		progress?.endTask();
		progress?.beginTask(0.75, total_size + (double)(files.Length * 4096));
		for (int j = 0; j < files.Length; j++)
		{
			updateFile(files[j].Substring(path.Length + 1), 1);
			progress?.addProgress(sizes[j] + 4096);
		}
		if (Change != null)
		{
			thr_Change();
		}
		fswatch.EnableRaisingEvents = true;
		progress?.endTask();
	}

	private void thr_Change()
	{
		disp.BeginInvoke(new changeDelegate(onChange));
	}

	private void thr_onFileChanged(object source, FileSystemEventArgs e)
	{
		disp.BeginInvoke(new onFileChangedDelegate(onFileChanged), e);
	}

	private void thr_onFileRenamed(object source, RenamedEventArgs e)
	{
		disp.BeginInvoke(new onFileRenamedDelegate(onFileRenamed), e);
	}

	private void onChange()
	{
		if (!stopped)
		{
			Change();
		}
	}

	private void onFileChanged(FileSystemEventArgs e)
	{
		if (!stopped)
		{
			string p = e.FullPath.Substring(path.Length + 1);
			if (e.ChangeType == WatcherChangeTypes.Created)
			{
				updateFile(p, 1);
			}
			else if (e.ChangeType == WatcherChangeTypes.Deleted)
			{
				updateFile(p, -1);
			}
			else if (e.ChangeType == WatcherChangeTypes.Changed)
			{
				updateFile(p, 0);
			}
			if (Change != null)
			{
				Change();
			}
		}
	}

	private void onFileRenamed(RenamedEventArgs e)
	{
		if (stopped)
		{
			return;
		}
		string newpath = e.FullPath.Substring(path.Length + 1);
		string oldpath = e.OldFullPath.Substring(path.Length + 1);
		updateFile(oldpath, -1);
		if (Directory.Exists(e.FullPath))
		{
			string[] files = Directory.GetFiles(e.FullPath, "*.*", SearchOption.AllDirectories);
			foreach (string f in files)
			{
				updateFile(f.Substring(path.Length + 1), 1);
			}
		}
		updateFile(newpath, 1);
		if (Change != null)
		{
			Change();
		}
	}

	private void updateFile(string relativePath, int isNewFile)
	{
		if (relativePath.EndsWith(".search_index"))
		{
			return;
		}
		DirectoryTree n = target.getFile(relativePath, isNewFile > 0);
		if (n == null)
		{
			return;
		}
		if (n.IsFolder)
		{
			if (isNewFile < 0)
			{
				n.decPresent(side);
			}
			return;
		}
		if (isNewFile > 0)
		{
			n.incPresent(side);
		}
		if (isNewFile < 0)
		{
			n.decPresent(side);
		}
		if (isNewFile >= 0 && !n.IsFolder && fullTextExtensions.Contains(n.FileType))
		{
			n.LoadFullText(side, path + "\\" + relativePath);
		}
	}
}
