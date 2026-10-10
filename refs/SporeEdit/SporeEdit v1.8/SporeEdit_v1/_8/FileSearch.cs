using System;
using System.Collections;
using System.IO;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace SporeEdit_v1._8
{
	public class FileSearch
	{
		private const string DefaultFileMask = "*.*";

		private const string DefaultDirectoryMask = "*";

		private DirectoryInfo _InitialDirectory;

		private string _DirectoryMask;

		private string _FileMask;

		private ArrayList _Directories;

		private ArrayList _Files;

		public DirectoryInfo InitialDirectory
		{
			get
			{
				return _InitialDirectory;
			}
			set
			{
				_InitialDirectory = value;
			}
		}

		public string DirectoryMask
		{
			get
			{
				return _DirectoryMask;
			}
			set
			{
				_DirectoryMask = value;
			}
		}

		public string FileMask
		{
			get
			{
				return _FileMask;
			}
			set
			{
				_FileMask = value;
			}
		}

		public ArrayList Directories => _Directories;

		public ArrayList Files => _Files;

		public FileSearch()
		{
			_Directories = new ArrayList();
			_Files = new ArrayList();
		}

		public FileSearch(string BaseDirectory, string FileMask = "*.*", string DirectoryMask = "*")
			: this(new DirectoryInfo(BaseDirectory), FileMask, DirectoryMask)
		{
		}

		public FileSearch(DirectoryInfo BaseDirectory, string FileMask = "*.*", string DirectoryMask = "*")
		{
			_Directories = new ArrayList();
			_Files = new ArrayList();
			_InitialDirectory = BaseDirectory;
			_FileMask = FileMask;
			_DirectoryMask = DirectoryMask;
		}

		protected override void Finalize()
		{
			_Files = null;
			_Directories = null;
			base.Finalize();
		}

		public void Search(DirectoryInfo BaseDirectory = null, string FileMask = null, string DirectoryMask = null)
		{
			if (!Information.IsNothing(BaseDirectory))
			{
				_InitialDirectory = BaseDirectory;
			}
			if (Information.IsNothing(_InitialDirectory))
			{
				throw new ArgumentException("A Directory Must be specified!", "Directory");
			}
			if (Information.IsNothing(FileMask))
			{
				_FileMask = "*.*";
			}
			else
			{
				_FileMask = FileMask;
			}
			if (Information.IsNothing(DirectoryMask))
			{
				_DirectoryMask = "*";
			}
			else
			{
				_DirectoryMask = DirectoryMask;
			}
			DoSearch(_InitialDirectory);
		}

		private void DoSearch(DirectoryInfo BaseDirectory)
		{
			try
			{
				_Files.AddRange(BaseDirectory.GetFiles(_FileMask));
			}
			catch (UnauthorizedAccessException ex)
			{
				ProjectData.SetProjectError(ex);
				UnauthorizedAccessException ex2 = ex;
				ProjectData.ClearProjectError();
			}
			try
			{
				DirectoryInfo[] directories = BaseDirectory.GetDirectories(_DirectoryMask);
				_Directories.AddRange(directories);
				DirectoryInfo[] array = directories;
				foreach (DirectoryInfo baseDirectory in array)
				{
					DoSearch(baseDirectory);
				}
			}
			catch (UnauthorizedAccessException ex3)
			{
				ProjectData.SetProjectError(ex3);
				UnauthorizedAccessException ex4 = ex3;
				ProjectData.ClearProjectError();
			}
		}
	}
}
