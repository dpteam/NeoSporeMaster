using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
using System.Xml.XPath;
using Gibbed.Spore.Helpers;
using Gibbed.Spore.Package;
using Gibbed.Spore.Properties;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.Win32;
using SporeEdit_v1._8.My;
using SporeEdit_v1._8.Spore;

namespace SporeEdit_v1._8
{
	[DesignerGenerated]
	public class Form1 : Form
	{
		private IContainer components;

		[AccessedThroughProperty("SplitContainer2")]
		private SplitContainer _SplitContainer2;

		[AccessedThroughProperty("tpages")]
		private TabControl _tpages;

		[AccessedThroughProperty("spage")]
		private TabPage _spage;

		[AccessedThroughProperty("SplitContainer5")]
		private SplitContainer _SplitContainer5;

		[AccessedThroughProperty("reslist")]
		private TreeView _reslist;

		[AccessedThroughProperty("spanel")]
		private Panel _spanel;

		[AccessedThroughProperty("start")]
		private Button _start;

		[AccessedThroughProperty("searchbox")]
		private TextBox _searchbox;

		[AccessedThroughProperty("epage")]
		private TabPage _epage;

		[AccessedThroughProperty("SplitContainer3")]
		private SplitContainer _SplitContainer3;

		[AccessedThroughProperty("editbox")]
		private TextBox _editbox;

		[AccessedThroughProperty("savebox")]
		private CheckBox _savebox;

		[AccessedThroughProperty("save")]
		private Button _save;

		[AccessedThroughProperty("ppage")]
		private TabPage _ppage;

		[AccessedThroughProperty("spath")]
		private Button _spath;

		[AccessedThroughProperty("edit")]
		private Button _edit;

		[AccessedThroughProperty("oall")]
		private Button _oall;

		[AccessedThroughProperty("progress")]
		private ProgressBar _progress;

		[AccessedThroughProperty("pathdialog")]
		private FolderBrowserDialog _pathdialog;

		[AccessedThroughProperty("openDialog")]
		private OpenFileDialog _openDialog;

		[AccessedThroughProperty("SplitContainer1")]
		private SplitContainer _SplitContainer1;

		[AccessedThroughProperty("menuStrip")]
		private MenuStrip _menuStrip;

		[AccessedThroughProperty("fileMenuItem")]
		private ToolStripMenuItem _fileMenuItem;

		[AccessedThroughProperty("openMenuItem")]
		private ToolStripMenuItem _openMenuItem;

		[AccessedThroughProperty("toolStripMenuItem1")]
		private ToolStripSeparator _toolStripMenuItem1;

		[AccessedThroughProperty("saveAllToolStripMenuItem")]
		private ToolStripMenuItem _saveAllToolStripMenuItem;

		[AccessedThroughProperty("SplitContainer4")]
		private SplitContainer _SplitContainer4;

		[AccessedThroughProperty("typeList")]
		private TreeView _typeList;

		[AccessedThroughProperty("fileList")]
		private ListView _fileList;

		[AccessedThroughProperty("ColumnHeader1")]
		private ColumnHeader _ColumnHeader1;

		[AccessedThroughProperty("ColumnHeader2")]
		private ColumnHeader _ColumnHeader2;

		[AccessedThroughProperty("ColumnHeader3")]
		private ColumnHeader _ColumnHeader3;

		[AccessedThroughProperty("ColumnHeader4")]
		private ColumnHeader _ColumnHeader4;

		[AccessedThroughProperty("Panel2")]
		private Panel _Panel2;

		[AccessedThroughProperty("Panel1")]
		private Panel _Panel1;

		[AccessedThroughProperty("saveAllFolderDialog")]
		private FolderBrowserDialog _saveAllFolderDialog;

		[AccessedThroughProperty("prpage")]
		private TabPage _prpage;

		[AccessedThroughProperty("cxml")]
		private Button _cxml;

		[AccessedThroughProperty("cxmlbrowser")]
		private FolderBrowserDialog _cxmlbrowser;

		[AccessedThroughProperty("SplitContainer6")]
		private SplitContainer _SplitContainer6;

		[AccessedThroughProperty("cresult")]
		private TextBox _cresult;

		[AccessedThroughProperty("cprop")]
		private Button _cprop;

		[AccessedThroughProperty("credits")]
		private Button _credits;

		[AccessedThroughProperty("SplitContainer7")]
		private SplitContainer _SplitContainer7;

		[AccessedThroughProperty("Label1")]
		private Label _Label1;

		[AccessedThroughProperty("exbox")]
		private TextBox _exbox;

		[AccessedThroughProperty("Label2")]
		private Label _Label2;

		[AccessedThroughProperty("sopen")]
		private Button _sopen;

		private object datapath;

		private object filename;

		private object oldtext;

		private Font MonospaceFont;

		private Dictionary<uint, string> FileNames;

		private Dictionary<uint, string> GroupNames;

		private DatabaseIndex[] DatabaseFiles;

		internal virtual SplitContainer SplitContainer2
		{
			[DebuggerNonUserCode]
			get
			{
				return _SplitContainer2;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_SplitContainer2 = value;
			}
		}

		internal virtual TabControl tpages
		{
			[DebuggerNonUserCode]
			get
			{
				return _tpages;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				EventHandler value2 = tpages_SelectedIndexChanged;
				if (_tpages != null)
				{
					_tpages.SelectedIndexChanged -= value2;
				}
				_tpages = value;
				if (_tpages != null)
				{
					_tpages.SelectedIndexChanged += value2;
				}
			}
		}

		internal virtual TabPage spage
		{
			[DebuggerNonUserCode]
			get
			{
				return _spage;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_spage = value;
			}
		}

		internal virtual SplitContainer SplitContainer5
		{
			[DebuggerNonUserCode]
			get
			{
				return _SplitContainer5;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_SplitContainer5 = value;
			}
		}

		internal virtual TreeView reslist
		{
			[DebuggerNonUserCode]
			get
			{
				return _reslist;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				TreeNodeMouseClickEventHandler value2 = reslist_NodeMouseClick;
				if (_reslist != null)
				{
					_reslist.NodeMouseClick -= value2;
				}
				_reslist = value;
				if (_reslist != null)
				{
					_reslist.NodeMouseClick += value2;
				}
			}
		}

		internal virtual Panel spanel
		{
			[DebuggerNonUserCode]
			get
			{
				return _spanel;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_spanel = value;
			}
		}

		internal virtual Button start
		{
			[DebuggerNonUserCode]
			get
			{
				return _start;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				EventHandler value2 = delegate
				{
					start_Click();
				};
				if (_start != null)
				{
					_start.Click -= value2;
				}
				_start = value;
				if (_start != null)
				{
					_start.Click += value2;
				}
			}
		}

		internal virtual TextBox searchbox
		{
			[DebuggerNonUserCode]
			get
			{
				return _searchbox;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				KeyEventHandler value2 = searchbox_KeyDown;
				if (_searchbox != null)
				{
					_searchbox.KeyDown -= value2;
				}
				_searchbox = value;
				if (_searchbox != null)
				{
					_searchbox.KeyDown += value2;
				}
			}
		}

		internal virtual TabPage epage
		{
			[DebuggerNonUserCode]
			get
			{
				return _epage;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_epage = value;
			}
		}

		internal virtual SplitContainer SplitContainer3
		{
			[DebuggerNonUserCode]
			get
			{
				return _SplitContainer3;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_SplitContainer3 = value;
			}
		}

		internal virtual TextBox editbox
		{
			[DebuggerNonUserCode]
			get
			{
				return _editbox;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_editbox = value;
			}
		}

		internal virtual CheckBox savebox
		{
			[DebuggerNonUserCode]
			get
			{
				return _savebox;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_savebox = value;
			}
		}

		internal virtual Button save
		{
			[DebuggerNonUserCode]
			get
			{
				return _save;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				EventHandler value2 = save_Click;
				if (_save != null)
				{
					_save.Click -= value2;
				}
				_save = value;
				if (_save != null)
				{
					_save.Click += value2;
				}
			}
		}

		internal virtual TabPage ppage
		{
			[DebuggerNonUserCode]
			get
			{
				return _ppage;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_ppage = value;
			}
		}

		internal virtual Button spath
		{
			[DebuggerNonUserCode]
			get
			{
				return _spath;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				EventHandler value2 = spath_Click;
				if (_spath != null)
				{
					_spath.Click -= value2;
				}
				_spath = value;
				if (_spath != null)
				{
					_spath.Click += value2;
				}
			}
		}

		internal virtual Button edit
		{
			[DebuggerNonUserCode]
			get
			{
				return _edit;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				EventHandler value2 = edit_Click;
				if (_edit != null)
				{
					_edit.Click -= value2;
				}
				_edit = value;
				if (_edit != null)
				{
					_edit.Click += value2;
				}
			}
		}

		internal virtual Button oall
		{
			[DebuggerNonUserCode]
			get
			{
				return _oall;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				EventHandler value2 = oall_Click;
				if (_oall != null)
				{
					_oall.Click -= value2;
				}
				_oall = value;
				if (_oall != null)
				{
					_oall.Click += value2;
				}
			}
		}

		internal virtual ProgressBar progress
		{
			[DebuggerNonUserCode]
			get
			{
				return _progress;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_progress = value;
			}
		}

		internal virtual FolderBrowserDialog pathdialog
		{
			[DebuggerNonUserCode]
			get
			{
				return _pathdialog;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_pathdialog = value;
			}
		}

		internal virtual OpenFileDialog openDialog
		{
			[DebuggerNonUserCode]
			get
			{
				return _openDialog;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_openDialog = value;
			}
		}

		internal virtual SplitContainer SplitContainer1
		{
			[DebuggerNonUserCode]
			get
			{
				return _SplitContainer1;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_SplitContainer1 = value;
			}
		}

		private virtual MenuStrip menuStrip
		{
			[DebuggerNonUserCode]
			get
			{
				return _menuStrip;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_menuStrip = value;
			}
		}

		private virtual ToolStripMenuItem fileMenuItem
		{
			[DebuggerNonUserCode]
			get
			{
				return _fileMenuItem;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_fileMenuItem = value;
			}
		}

		private virtual ToolStripMenuItem openMenuItem
		{
			[DebuggerNonUserCode]
			get
			{
				return _openMenuItem;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				EventHandler value2 = openMenuItem_Click;
				if (_openMenuItem != null)
				{
					_openMenuItem.Click -= value2;
				}
				_openMenuItem = value;
				if (_openMenuItem != null)
				{
					_openMenuItem.Click += value2;
				}
			}
		}

		private virtual ToolStripSeparator toolStripMenuItem1
		{
			[DebuggerNonUserCode]
			get
			{
				return _toolStripMenuItem1;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_toolStripMenuItem1 = value;
			}
		}

		private virtual ToolStripMenuItem saveAllToolStripMenuItem
		{
			[DebuggerNonUserCode]
			get
			{
				return _saveAllToolStripMenuItem;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				EventHandler value2 = saveAllToolStripMenuItem_Click;
				if (_saveAllToolStripMenuItem != null)
				{
					_saveAllToolStripMenuItem.Click -= value2;
				}
				_saveAllToolStripMenuItem = value;
				if (_saveAllToolStripMenuItem != null)
				{
					_saveAllToolStripMenuItem.Click += value2;
				}
			}
		}

		internal virtual SplitContainer SplitContainer4
		{
			[DebuggerNonUserCode]
			get
			{
				return _SplitContainer4;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_SplitContainer4 = value;
			}
		}

		private virtual TreeView typeList
		{
			[DebuggerNonUserCode]
			get
			{
				return _typeList;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				TreeNodeMouseClickEventHandler value2 = typeList_NodeMouseClick;
				if (_typeList != null)
				{
					_typeList.NodeMouseClick -= value2;
				}
				_typeList = value;
				if (_typeList != null)
				{
					_typeList.NodeMouseClick += value2;
				}
			}
		}

		private virtual ListView fileList
		{
			[DebuggerNonUserCode]
			get
			{
				return _fileList;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_fileList = value;
			}
		}

		private virtual ColumnHeader ColumnHeader1
		{
			[DebuggerNonUserCode]
			get
			{
				return _ColumnHeader1;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_ColumnHeader1 = value;
			}
		}

		private virtual ColumnHeader ColumnHeader2
		{
			[DebuggerNonUserCode]
			get
			{
				return _ColumnHeader2;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_ColumnHeader2 = value;
			}
		}

		private virtual ColumnHeader ColumnHeader3
		{
			[DebuggerNonUserCode]
			get
			{
				return _ColumnHeader3;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_ColumnHeader3 = value;
			}
		}

		private virtual ColumnHeader ColumnHeader4
		{
			[DebuggerNonUserCode]
			get
			{
				return _ColumnHeader4;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_ColumnHeader4 = value;
			}
		}

		internal virtual Panel Panel2
		{
			[DebuggerNonUserCode]
			get
			{
				return _Panel2;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_Panel2 = value;
			}
		}

		internal virtual Panel Panel1
		{
			[DebuggerNonUserCode]
			get
			{
				return _Panel1;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_Panel1 = value;
			}
		}

		private virtual FolderBrowserDialog saveAllFolderDialog
		{
			[DebuggerNonUserCode]
			get
			{
				return _saveAllFolderDialog;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_saveAllFolderDialog = value;
			}
		}

		internal virtual TabPage prpage
		{
			[DebuggerNonUserCode]
			get
			{
				return _prpage;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_prpage = value;
			}
		}

		internal virtual Button cxml
		{
			[DebuggerNonUserCode]
			get
			{
				return _cxml;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				EventHandler value2 = cxml_Click;
				if (_cxml != null)
				{
					_cxml.Click -= value2;
				}
				_cxml = value;
				if (_cxml != null)
				{
					_cxml.Click += value2;
				}
			}
		}

		internal virtual FolderBrowserDialog cxmlbrowser
		{
			[DebuggerNonUserCode]
			get
			{
				return _cxmlbrowser;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_cxmlbrowser = value;
			}
		}

		internal virtual SplitContainer SplitContainer6
		{
			[DebuggerNonUserCode]
			get
			{
				return _SplitContainer6;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_SplitContainer6 = value;
			}
		}

		internal virtual TextBox cresult
		{
			[DebuggerNonUserCode]
			get
			{
				return _cresult;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_cresult = value;
			}
		}

		internal virtual Button cprop
		{
			[DebuggerNonUserCode]
			get
			{
				return _cprop;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				EventHandler value2 = delegate
				{
					cprop_Click_temp();
				};
				if (_cprop != null)
				{
					_cprop.Click -= value2;
				}
				_cprop = value;
				if (_cprop != null)
				{
					_cprop.Click += value2;
				}
			}
		}

		internal virtual Button credits
		{
			[DebuggerNonUserCode]
			get
			{
				return _credits;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				EventHandler value2 = credits_Click;
				if (_credits != null)
				{
					_credits.Click -= value2;
				}
				_credits = value;
				if (_credits != null)
				{
					_credits.Click += value2;
				}
			}
		}

		internal virtual SplitContainer SplitContainer7
		{
			[DebuggerNonUserCode]
			get
			{
				return _SplitContainer7;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_SplitContainer7 = value;
			}
		}

		internal virtual Label Label1
		{
			[DebuggerNonUserCode]
			get
			{
				return _Label1;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_Label1 = value;
			}
		}

		internal virtual TextBox exbox
		{
			[DebuggerNonUserCode]
			get
			{
				return _exbox;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_exbox = value;
			}
		}

		internal virtual Label Label2
		{
			[DebuggerNonUserCode]
			get
			{
				return _Label2;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_Label2 = value;
			}
		}

		internal virtual Button sopen
		{
			[DebuggerNonUserCode]
			get
			{
				return _sopen;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				EventHandler value2 = sopen_Click;
				if (_sopen != null)
				{
					_sopen.Click -= value2;
				}
				_sopen = value;
				if (_sopen != null)
				{
					_sopen.Click += value2;
				}
			}
		}

		public Form1()
		{
			base.Load += Form1_Load;
			datapath = "";
			filename = "";
			oldtext = "";
			MonospaceFont = new Font(FontFamily.GenericMonospace, 9f);
			InitializeComponent();
		}

		[DebuggerNonUserCode]
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing && components != null)
				{
					components.Dispose();
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		[System.Diagnostics.DebuggerStepThrough]
		private void InitializeComponent()
		{
			SplitContainer2 = new System.Windows.Forms.SplitContainer();
			tpages = new System.Windows.Forms.TabControl();
			spage = new System.Windows.Forms.TabPage();
			SplitContainer5 = new System.Windows.Forms.SplitContainer();
			reslist = new System.Windows.Forms.TreeView();
			spanel = new System.Windows.Forms.Panel();
			exbox = new System.Windows.Forms.TextBox();
			Label2 = new System.Windows.Forms.Label();
			progress = new System.Windows.Forms.ProgressBar();
			start = new System.Windows.Forms.Button();
			searchbox = new System.Windows.Forms.TextBox();
			epage = new System.Windows.Forms.TabPage();
			SplitContainer3 = new System.Windows.Forms.SplitContainer();
			editbox = new System.Windows.Forms.TextBox();
			Panel1 = new System.Windows.Forms.Panel();
			savebox = new System.Windows.Forms.CheckBox();
			save = new System.Windows.Forms.Button();
			ppage = new System.Windows.Forms.TabPage();
			SplitContainer1 = new System.Windows.Forms.SplitContainer();
			SplitContainer7 = new System.Windows.Forms.SplitContainer();
			menuStrip = new System.Windows.Forms.MenuStrip();
			fileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			openMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			saveAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			Label1 = new System.Windows.Forms.Label();
			SplitContainer4 = new System.Windows.Forms.SplitContainer();
			typeList = new System.Windows.Forms.TreeView();
			fileList = new System.Windows.Forms.ListView();
			ColumnHeader1 = new System.Windows.Forms.ColumnHeader();
			ColumnHeader2 = new System.Windows.Forms.ColumnHeader();
			ColumnHeader3 = new System.Windows.Forms.ColumnHeader();
			ColumnHeader4 = new System.Windows.Forms.ColumnHeader();
			prpage = new System.Windows.Forms.TabPage();
			SplitContainer6 = new System.Windows.Forms.SplitContainer();
			cprop = new System.Windows.Forms.Button();
			cxml = new System.Windows.Forms.Button();
			cresult = new System.Windows.Forms.TextBox();
			Panel2 = new System.Windows.Forms.Panel();
			sopen = new System.Windows.Forms.Button();
			credits = new System.Windows.Forms.Button();
			spath = new System.Windows.Forms.Button();
			edit = new System.Windows.Forms.Button();
			oall = new System.Windows.Forms.Button();
			pathdialog = new System.Windows.Forms.FolderBrowserDialog();
			openDialog = new System.Windows.Forms.OpenFileDialog();
			saveAllFolderDialog = new System.Windows.Forms.FolderBrowserDialog();
			cxmlbrowser = new System.Windows.Forms.FolderBrowserDialog();
			SplitContainer2.Panel1.SuspendLayout();
			SplitContainer2.Panel2.SuspendLayout();
			SplitContainer2.SuspendLayout();
			tpages.SuspendLayout();
			spage.SuspendLayout();
			SplitContainer5.Panel1.SuspendLayout();
			SplitContainer5.Panel2.SuspendLayout();
			SplitContainer5.SuspendLayout();
			spanel.SuspendLayout();
			epage.SuspendLayout();
			SplitContainer3.Panel1.SuspendLayout();
			SplitContainer3.Panel2.SuspendLayout();
			SplitContainer3.SuspendLayout();
			Panel1.SuspendLayout();
			ppage.SuspendLayout();
			SplitContainer1.Panel1.SuspendLayout();
			SplitContainer1.Panel2.SuspendLayout();
			SplitContainer1.SuspendLayout();
			SplitContainer7.Panel1.SuspendLayout();
			SplitContainer7.Panel2.SuspendLayout();
			SplitContainer7.SuspendLayout();
			menuStrip.SuspendLayout();
			SplitContainer4.Panel1.SuspendLayout();
			SplitContainer4.Panel2.SuspendLayout();
			SplitContainer4.SuspendLayout();
			prpage.SuspendLayout();
			SplitContainer6.Panel1.SuspendLayout();
			SplitContainer6.Panel2.SuspendLayout();
			SplitContainer6.SuspendLayout();
			Panel2.SuspendLayout();
			SuspendLayout();
			SplitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
			SplitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
			SplitContainer2.IsSplitterFixed = true;
			System.Drawing.Point point2 = (SplitContainer2.Location = new System.Drawing.Point(0, 0));
			SplitContainer2.Name = "SplitContainer2";
			SplitContainer2.Panel1.Controls.Add(tpages);
			SplitContainer2.Panel2.Controls.Add(Panel2);
			System.Drawing.Size size2 = (SplitContainer2.Size = new System.Drawing.Size(548, 388));
			SplitContainer2.SplitterDistance = 454;
			SplitContainer2.TabIndex = 1;
			tpages.Appearance = System.Windows.Forms.TabAppearance.Buttons;
			tpages.Controls.Add(spage);
			tpages.Controls.Add(epage);
			tpages.Controls.Add(ppage);
			tpages.Controls.Add(prpage);
			tpages.Dock = System.Windows.Forms.DockStyle.Fill;
			point2 = (tpages.Location = new System.Drawing.Point(0, 0));
			tpages.Name = "tpages";
			tpages.SelectedIndex = 0;
			tpages.ShowToolTips = true;
			size2 = (tpages.Size = new System.Drawing.Size(454, 388));
			tpages.TabIndex = 1;
			spage.Controls.Add(SplitContainer5);
			point2 = (spage.Location = new System.Drawing.Point(4, 25));
			spage.Name = "spage";
			System.Windows.Forms.Padding padding2 = (spage.Padding = new System.Windows.Forms.Padding(3));
			size2 = (spage.Size = new System.Drawing.Size(446, 359));
			spage.TabIndex = 0;
			spage.Text = "Search";
			spage.ToolTipText = "This tab contains search results from searching through Spore's data files.";
			spage.UseVisualStyleBackColor = true;
			SplitContainer5.Dock = System.Windows.Forms.DockStyle.Fill;
			SplitContainer5.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
			SplitContainer5.IsSplitterFixed = true;
			point2 = (SplitContainer5.Location = new System.Drawing.Point(3, 3));
			SplitContainer5.Name = "SplitContainer5";
			SplitContainer5.Orientation = System.Windows.Forms.Orientation.Horizontal;
			SplitContainer5.Panel1.Controls.Add(reslist);
			SplitContainer5.Panel2.Controls.Add(spanel);
			size2 = (SplitContainer5.Size = new System.Drawing.Size(440, 353));
			SplitContainer5.SplitterDistance = 277;
			SplitContainer5.TabIndex = 0;
			reslist.Dock = System.Windows.Forms.DockStyle.Fill;
			point2 = (reslist.Location = new System.Drawing.Point(0, 0));
			reslist.Name = "reslist";
			size2 = (reslist.Size = new System.Drawing.Size(440, 277));
			reslist.TabIndex = 0;
			spanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			spanel.Controls.Add(exbox);
			spanel.Controls.Add(Label2);
			spanel.Controls.Add(progress);
			spanel.Controls.Add(start);
			spanel.Controls.Add(searchbox);
			spanel.Dock = System.Windows.Forms.DockStyle.Fill;
			point2 = (spanel.Location = new System.Drawing.Point(0, 0));
			spanel.Name = "spanel";
			size2 = (spanel.Size = new System.Drawing.Size(440, 72));
			spanel.TabIndex = 12;
			point2 = (exbox.Location = new System.Drawing.Point(117, 47));
			exbox.Name = "exbox";
			size2 = (exbox.Size = new System.Drawing.Size(319, 20));
			exbox.TabIndex = 12;
			exbox.Text = ".trigger";
			Label2.AutoSize = true;
			point2 = (Label2.Location = new System.Drawing.Point(4, 47));
			Label2.Name = "Label2";
			size2 = (Label2.Size = new System.Drawing.Size(107, 13));
			Label2.TabIndex = 11;
			Label2.Text = "Search for extension:\r\n";
			point2 = (progress.Location = new System.Drawing.Point(3, 25));
			progress.Name = "progress";
			size2 = (progress.Size = new System.Drawing.Size(432, 19));
			progress.Step = 1;
			progress.TabIndex = 10;
			point2 = (start.Location = new System.Drawing.Point(353, 2));
			start.Name = "start";
			size2 = (start.Size = new System.Drawing.Size(82, 22));
			start.TabIndex = 8;
			start.Text = "Search";
			start.UseVisualStyleBackColor = true;
			point2 = (searchbox.Location = new System.Drawing.Point(3, 2));
			searchbox.Name = "searchbox";
			size2 = (searchbox.Size = new System.Drawing.Size(344, 20));
			searchbox.TabIndex = 9;
			epage.Controls.Add(SplitContainer3);
			point2 = (epage.Location = new System.Drawing.Point(4, 25));
			epage.Name = "epage";
			padding2 = (epage.Padding = new System.Windows.Forms.Padding(3));
			size2 = (epage.Size = new System.Drawing.Size(446, 359));
			epage.TabIndex = 1;
			epage.Text = "Edit";
			epage.ToolTipText = "This tab allows you to edit values found in the Spore data files.";
			epage.UseVisualStyleBackColor = true;
			SplitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
			SplitContainer3.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
			SplitContainer3.IsSplitterFixed = true;
			point2 = (SplitContainer3.Location = new System.Drawing.Point(3, 3));
			SplitContainer3.Name = "SplitContainer3";
			SplitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
			SplitContainer3.Panel1.Controls.Add(editbox);
			SplitContainer3.Panel2.Controls.Add(Panel1);
			size2 = (SplitContainer3.Size = new System.Drawing.Size(440, 353));
			SplitContainer3.SplitterDistance = 321;
			SplitContainer3.TabIndex = 0;
			editbox.AcceptsTab = true;
			editbox.Dock = System.Windows.Forms.DockStyle.Fill;
			point2 = (editbox.Location = new System.Drawing.Point(0, 0));
			editbox.Multiline = true;
			editbox.Name = "editbox";
			size2 = (editbox.Size = new System.Drawing.Size(440, 321));
			editbox.TabIndex = 6;
			editbox.WordWrap = false;
			Panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			Panel1.Controls.Add(savebox);
			Panel1.Controls.Add(save);
			Panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			point2 = (Panel1.Location = new System.Drawing.Point(0, 0));
			Panel1.Name = "Panel1";
			size2 = (Panel1.Size = new System.Drawing.Size(440, 28));
			Panel1.TabIndex = 0;
			savebox.AutoSize = true;
			savebox.Checked = true;
			savebox.CheckState = System.Windows.Forms.CheckState.Checked;
			point2 = (savebox.Location = new System.Drawing.Point(3, 7));
			savebox.Name = "savebox";
			size2 = (savebox.Size = new System.Drawing.Size(118, 17));
			savebox.TabIndex = 10;
			savebox.Text = "Save backup of file";
			savebox.UseVisualStyleBackColor = true;
			point2 = (save.Location = new System.Drawing.Point(263, 3));
			save.Name = "save";
			size2 = (save.Size = new System.Drawing.Size(82, 22));
			save.TabIndex = 9;
			save.Text = "Save";
			save.UseVisualStyleBackColor = true;
			ppage.Controls.Add(SplitContainer1);
			point2 = (ppage.Location = new System.Drawing.Point(4, 25));
			ppage.Name = "ppage";
			padding2 = (ppage.Padding = new System.Windows.Forms.Padding(3));
			size2 = (ppage.Size = new System.Drawing.Size(446, 359));
			ppage.TabIndex = 2;
			ppage.Text = "Packages";
			ppage.UseVisualStyleBackColor = true;
			SplitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			point2 = (SplitContainer1.Location = new System.Drawing.Point(3, 3));
			SplitContainer1.Name = "SplitContainer1";
			SplitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			SplitContainer1.Panel1.Controls.Add(SplitContainer7);
			SplitContainer1.Panel2.Controls.Add(SplitContainer4);
			size2 = (SplitContainer1.Size = new System.Drawing.Size(440, 353));
			SplitContainer1.SplitterDistance = 27;
			SplitContainer1.TabIndex = 0;
			SplitContainer7.Dock = System.Windows.Forms.DockStyle.Fill;
			SplitContainer7.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
			SplitContainer7.IsSplitterFixed = true;
			point2 = (SplitContainer7.Location = new System.Drawing.Point(0, 0));
			SplitContainer7.Name = "SplitContainer7";
			SplitContainer7.Panel1.Controls.Add(menuStrip);
			SplitContainer7.Panel2.Controls.Add(Label1);
			size2 = (SplitContainer7.Size = new System.Drawing.Size(440, 27));
			SplitContainer7.SplitterDistance = 323;
			SplitContainer7.TabIndex = 0;
			menuStrip.Dock = System.Windows.Forms.DockStyle.Fill;
			menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[1] { fileMenuItem });
			point2 = (menuStrip.Location = new System.Drawing.Point(0, 0));
			menuStrip.Name = "menuStrip";
			size2 = (menuStrip.Size = new System.Drawing.Size(323, 27));
			menuStrip.TabIndex = 1;
			menuStrip.Text = "menuStrip1";
			fileMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[3] { openMenuItem, toolStripMenuItem1, saveAllToolStripMenuItem });
			fileMenuItem.Name = "fileMenuItem";
			size2 = (fileMenuItem.Size = new System.Drawing.Size(35, 23));
			fileMenuItem.Text = "&File";
			openMenuItem.Name = "openMenuItem";
			size2 = (openMenuItem.Size = new System.Drawing.Size(123, 22));
			openMenuItem.Text = "&Open";
			toolStripMenuItem1.Name = "toolStripMenuItem1";
			size2 = (toolStripMenuItem1.Size = new System.Drawing.Size(120, 6));
			saveAllToolStripMenuItem.Name = "saveAllToolStripMenuItem";
			size2 = (saveAllToolStripMenuItem.Size = new System.Drawing.Size(123, 22));
			saveAllToolStripMenuItem.Text = "Save &All";
			Label1.AutoSize = true;
			Label1.Dock = System.Windows.Forms.DockStyle.Fill;
			point2 = (Label1.Location = new System.Drawing.Point(0, 0));
			Label1.Name = "Label1";
			size2 = (Label1.Size = new System.Drawing.Size(102, 13));
			Label1.TabIndex = 0;
			Label1.Text = "<-- CLICK FILE, FFS";
			SplitContainer4.Dock = System.Windows.Forms.DockStyle.Fill;
			point2 = (SplitContainer4.Location = new System.Drawing.Point(0, 0));
			SplitContainer4.Name = "SplitContainer4";
			SplitContainer4.Panel1.Controls.Add(typeList);
			SplitContainer4.Panel2.Controls.Add(fileList);
			size2 = (SplitContainer4.Size = new System.Drawing.Size(440, 322));
			SplitContainer4.SplitterDistance = 146;
			SplitContainer4.TabIndex = 1;
			typeList.Dock = System.Windows.Forms.DockStyle.Fill;
			point2 = (typeList.Location = new System.Drawing.Point(0, 0));
			typeList.Name = "typeList";
			size2 = (typeList.Size = new System.Drawing.Size(146, 322));
			typeList.TabIndex = 4;
			fileList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[4] { ColumnHeader1, ColumnHeader2, ColumnHeader3, ColumnHeader4 });
			fileList.Dock = System.Windows.Forms.DockStyle.Fill;
			fileList.Font = new System.Drawing.Font("Courier New", 9.267326f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			fileList.FullRowSelect = true;
			point2 = (fileList.Location = new System.Drawing.Point(0, 0));
			fileList.Name = "fileList";
			size2 = (fileList.Size = new System.Drawing.Size(290, 322));
			fileList.TabIndex = 3;
			fileList.UseCompatibleStateImageBehavior = false;
			fileList.View = System.Windows.Forms.View.Details;
			ColumnHeader1.Text = "Name";
			ColumnHeader1.Width = 100;
			ColumnHeader2.Text = "Group";
			ColumnHeader2.Width = 100;
			ColumnHeader3.Text = "Size";
			ColumnHeader3.Width = 100;
			ColumnHeader4.Text = "Unknown";
			ColumnHeader4.Width = 100;
			prpage.Controls.Add(SplitContainer6);
			point2 = (prpage.Location = new System.Drawing.Point(4, 25));
			prpage.Name = "prpage";
			padding2 = (prpage.Padding = new System.Windows.Forms.Padding(3));
			size2 = (prpage.Size = new System.Drawing.Size(446, 359));
			prpage.TabIndex = 3;
			prpage.Text = "Properties";
			prpage.UseVisualStyleBackColor = true;
			SplitContainer6.Dock = System.Windows.Forms.DockStyle.Fill;
			SplitContainer6.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			SplitContainer6.IsSplitterFixed = true;
			point2 = (SplitContainer6.Location = new System.Drawing.Point(3, 3));
			SplitContainer6.Name = "SplitContainer6";
			SplitContainer6.Orientation = System.Windows.Forms.Orientation.Horizontal;
			SplitContainer6.Panel1.Controls.Add(cprop);
			SplitContainer6.Panel1.Controls.Add(cxml);
			SplitContainer6.Panel2.Controls.Add(cresult);
			size2 = (SplitContainer6.Size = new System.Drawing.Size(440, 353));
			SplitContainer6.SplitterDistance = 30;
			SplitContainer6.TabIndex = 0;
			point2 = (cprop.Location = new System.Drawing.Point(112, 4));
			cprop.Name = "cprop";
			size2 = (cprop.Size = new System.Drawing.Size(101, 23));
			cprop.TabIndex = 1;
			cprop.Text = "XML --> Prop";
			cprop.UseVisualStyleBackColor = true;
			point2 = (cxml.Location = new System.Drawing.Point(5, 4));
			cxml.Name = "cxml";
			size2 = (cxml.Size = new System.Drawing.Size(101, 23));
			cxml.TabIndex = 0;
			cxml.Text = "Prop --> XML";
			cxml.UseVisualStyleBackColor = true;
			cresult.Dock = System.Windows.Forms.DockStyle.Fill;
			point2 = (cresult.Location = new System.Drawing.Point(0, 0));
			cresult.Multiline = true;
			cresult.Name = "cresult";
			cresult.ReadOnly = true;
			cresult.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			size2 = (cresult.Size = new System.Drawing.Size(440, 319));
			cresult.TabIndex = 1;
			Panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			Panel2.Controls.Add(sopen);
			Panel2.Controls.Add(credits);
			Panel2.Controls.Add(spath);
			Panel2.Controls.Add(edit);
			Panel2.Controls.Add(oall);
			Panel2.Dock = System.Windows.Forms.DockStyle.Fill;
			point2 = (Panel2.Location = new System.Drawing.Point(0, 0));
			Panel2.Name = "Panel2";
			size2 = (Panel2.Size = new System.Drawing.Size(90, 388));
			Panel2.TabIndex = 2;
			sopen.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			point2 = (sopen.Location = new System.Drawing.Point(3, 87));
			sopen.Name = "sopen";
			size2 = (sopen.Size = new System.Drawing.Size(84, 22));
			sopen.TabIndex = 6;
			sopen.Text = "Notepad";
			sopen.UseVisualStyleBackColor = true;
			credits.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			point2 = (credits.Location = new System.Drawing.Point(3, 115));
			credits.Name = "credits";
			size2 = (credits.Size = new System.Drawing.Size(84, 22));
			credits.TabIndex = 5;
			credits.Text = "Credits";
			credits.UseVisualStyleBackColor = true;
			spath.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			point2 = (spath.Location = new System.Drawing.Point(3, 59));
			spath.Name = "spath";
			size2 = (spath.Size = new System.Drawing.Size(84, 22));
			spath.TabIndex = 4;
			spath.Text = "Set Path";
			spath.UseVisualStyleBackColor = true;
			edit.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			point2 = (edit.Location = new System.Drawing.Point(3, 3));
			edit.Name = "edit";
			size2 = (edit.Size = new System.Drawing.Size(84, 22));
			edit.TabIndex = 3;
			edit.Text = "Edit";
			edit.UseVisualStyleBackColor = true;
			oall.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			point2 = (oall.Location = new System.Drawing.Point(3, 31));
			oall.Name = "oall";
			size2 = (oall.Size = new System.Drawing.Size(84, 22));
			oall.TabIndex = 2;
			oall.Text = "Output";
			oall.UseVisualStyleBackColor = true;
			pathdialog.Description = "Select the path to your extracted Spore data.";
			pathdialog.RootFolder = System.Environment.SpecialFolder.MyComputer;
			pathdialog.ShowNewFolderButton = false;
			openDialog.FileName = "OpenFileDialog1";
			openDialog.Filter = "Spore Data Files|*.package";
			cxmlbrowser.Description = "Select the directory that contains the .prop files you wish to convert. (.prop files in subdirectories will be converted also.)";
			System.Drawing.SizeF sizeF2 = (AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f));
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			size2 = (ClientSize = new System.Drawing.Size(548, 388));
			Controls.Add(SplitContainer2);
			size2 = (MinimumSize = new System.Drawing.Size(556, 380));
			Name = "Form1";
			ShowIcon = false;
			Text = "SporeEdit v1.8";
			SplitContainer2.Panel1.ResumeLayout(false);
			SplitContainer2.Panel2.ResumeLayout(false);
			SplitContainer2.ResumeLayout(false);
			tpages.ResumeLayout(false);
			spage.ResumeLayout(false);
			SplitContainer5.Panel1.ResumeLayout(false);
			SplitContainer5.Panel2.ResumeLayout(false);
			SplitContainer5.ResumeLayout(false);
			spanel.ResumeLayout(false);
			spanel.PerformLayout();
			epage.ResumeLayout(false);
			SplitContainer3.Panel1.ResumeLayout(false);
			SplitContainer3.Panel1.PerformLayout();
			SplitContainer3.Panel2.ResumeLayout(false);
			SplitContainer3.ResumeLayout(false);
			Panel1.ResumeLayout(false);
			Panel1.PerformLayout();
			ppage.ResumeLayout(false);
			SplitContainer1.Panel1.ResumeLayout(false);
			SplitContainer1.Panel2.ResumeLayout(false);
			SplitContainer1.ResumeLayout(false);
			SplitContainer7.Panel1.ResumeLayout(false);
			SplitContainer7.Panel1.PerformLayout();
			SplitContainer7.Panel2.ResumeLayout(false);
			SplitContainer7.Panel2.PerformLayout();
			SplitContainer7.ResumeLayout(false);
			menuStrip.ResumeLayout(false);
			menuStrip.PerformLayout();
			SplitContainer4.Panel1.ResumeLayout(false);
			SplitContainer4.Panel2.ResumeLayout(false);
			SplitContainer4.ResumeLayout(false);
			prpage.ResumeLayout(false);
			SplitContainer6.Panel1.ResumeLayout(false);
			SplitContainer6.Panel2.ResumeLayout(false);
			SplitContainer6.Panel2.PerformLayout();
			SplitContainer6.ResumeLayout(false);
			Panel2.ResumeLayout(false);
			ResumeLayout(false);
		}

		private void LoadFileNames(string path)
		{
			FileNames = new Dictionary<uint, string>();
			if (!MyProject.Computer.FileSystem.FileExists(path))
			{
				return;
			}
			TextReader textReader = new StreamReader(path);
			while (true)
			{
				string text = textReader.ReadLine();
				if (text == null)
				{
					break;
				}
				uint key = text.FNV();
				FileNames[key] = text;
			}
			textReader.Close();
		}

		private void LoadGroupNames(string path)
		{
			GroupNames = new Dictionary<uint, string>();
			if (!MyProject.Computer.FileSystem.FileExists(path))
			{
				return;
			}
			XPathDocument xPathDocument = new XPathDocument(path);
			XPathNavigator xPathNavigator = xPathDocument.CreateNavigator();
			XPathNodeIterator xPathNodeIterator = xPathNavigator.Select("/names/name");
			while (xPathNodeIterator.MoveNext())
			{
				string attribute = xPathNodeIterator.Current.GetAttribute("id", "");
				string value = xPathNodeIterator.Current.Value;
				uint key;
				if (attribute.StartsWith("(hash(") && attribute.EndsWith("))"))
				{
					string input = attribute.Substring(6, checked(attribute.Length - 8));
					key = input.FNV();
				}
				else
				{
					key = attribute.GetHexNumber();
				}
				GroupNames[key] = value;
			}
		}

		private void PropHandleDirectory(PropConverter converter, string path, string filter, bool recursive)
		{
			string[] files = Directory.GetFiles(path, filter, SearchOption.TopDirectoryOnly);
			string[] array = files;
			foreach (string text in array)
			{
				if (!text.Contains(".prop.xml"))
				{
					string text2 = text.Replace(".prop", ".prop.xml");
					cresult.AppendText(text + "\r\n" + text2 + "\r\n\r\n");
					PropertyFile propertyFile = new PropertyFile();
					Stream stream = new FileStream(text, FileMode.Open, FileAccess.Read);
					Stream stream2 = new FileStream(text2, FileMode.Create, FileAccess.Write);
					converter.Convert(stream, stream2);
					stream.Close();
					stream2.Close();
					Application.DoEvents();
				}
			}
			if (recursive)
			{
				files = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
				string[] array2 = files;
				foreach (string path2 in array2)
				{
					PropHandleDirectory(converter, path2, filter, recursive);
				}
			}
		}

		private static void XMLHandleDirectory(XMLConverter converter, string path, string filter, bool recursive)
		{
			string[] files = Directory.GetFiles(path, filter, SearchOption.TopDirectoryOnly);
			string[] array = files;
			foreach (string text in array)
			{
				if (text.Contains(".prop"))
				{
					string text2 = text;
					text2 = text2.Replace(".prop.xml", ".prop");
					Console.WriteLine("{0} => {1}", text, text2);
					PropertyFile propertyFile = new PropertyFile();
					Stream stream = new FileStream(text, FileMode.Open, FileAccess.Read);
					Stream stream2 = new FileStream(text2, FileMode.Create, FileAccess.Write);
					converter.Convert(stream, stream2);
					stream.Close();
					stream2.Close();
				}
			}
			if (recursive)
			{
				files = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
				string[] array2 = files;
				foreach (string path2 in array2)
				{
					XMLHandleDirectory(converter, path2, filter, recursive);
				}
			}
		}

		private void start_Click()
		{
			object obj = RuntimeHelpers.GetObjectValue(datapath);
			if (Operators.CompareString(exbox.Text, "", TextCompare: false) == 0)
			{
				Interaction.MsgBox("An error has occured! You must enter a file extension.", MsgBoxStyle.Critical, "SporeEdit v1.8 - Error");
				return;
			}
			int num = 0;
			if (!MyProject.Computer.FileSystem.DirectoryExists(Conversions.ToString(datapath)))
			{
				Interaction.MsgBox(Operators.ConcatenateObject(Operators.ConcatenateObject("An error has occured! Directory '", datapath), "' does not exist! Please specify correct directory."), MsgBoxStyle.Critical, "SporeEdit v1.8 - Error");
				return;
			}
			start.Enabled = false;
			string left = exbox.Text;
			FileSearch fileSearch;
			if (Operators.CompareString(left, ".prop.xml", TextCompare: false) == 0)
			{
				if (MyProject.Computer.FileSystem.DirectoryExists(Conversions.ToString(Operators.ConcatenateObject(datapath, "\\prop"))))
				{
					obj = Operators.ConcatenateObject(datapath, "\\prop");
				}
				fileSearch = new FileSearch(new DirectoryInfo(Conversions.ToString(obj)), "*.prop.xml");
			}
			else if (Operators.CompareString(left, ".trigger", TextCompare: false) == 0)
			{
				if (MyProject.Computer.FileSystem.DirectoryExists(Conversions.ToString(Operators.ConcatenateObject(datapath, "\\trigger"))))
				{
					obj = Operators.ConcatenateObject(datapath, "\\trigger");
				}
				fileSearch = new FileSearch(new DirectoryInfo(Conversions.ToString(obj)), "*.trigger");
			}
			else
			{
				fileSearch = new FileSearch(new DirectoryInfo(Conversions.ToString(obj)), "*." + exbox.Text);
			}
			fileSearch.Search();
			reslist.Nodes.Clear();
			progress.Maximum = fileSearch.Files.Count;
			checked
			{
				IEnumerator enumerator = default(IEnumerator);
				try
				{
					enumerator = fileSearch.Files.GetEnumerator();
					object instance = default(object);
					while (enumerator.MoveNext())
					{
						FileInfo fileInfo = (FileInfo)enumerator.Current;
						num++;
						progress.Increment(1);
						if (!fileInfo.FullName.Contains(exbox.Text))
						{
							continue;
						}
						int num2 = 0;
						string text = MyProject.Computer.FileSystem.ReadAllText(fileInfo.FullName);
						string[] array = text.Split('\r');
						string[] array2 = array;
						for (int i = 0; i < array2.Length; i++)
						{
							string text2 = array2[i];
							if (Strings.UCase(text2).Contains(Strings.UCase(searchbox.Text)))
							{
								if (num2 == 0)
								{
									instance = reslist.Nodes.Add(fileInfo.FullName);
									num2 = 1;
								}
								text2 = text2.Replace("\r", "");
								text2 = text2.Replace("\n", "");
								object instance2 = NewLateBinding.LateGet(instance, null, "Nodes", new object[0], null, null, null);
								object[] array3 = new object[1] { text2 };
								bool[] array4 = new bool[1] { true };
								NewLateBinding.LateCall(instance2, null, "Add", array3, null, null, array4, IgnoreReturn: true);
								if (array4[0])
								{
									text2 = (string)Conversions.ChangeType(RuntimeHelpers.GetObjectValue(array3[0]), typeof(string));
								}
							}
						}
						num2 = 0;
					}
				}
				finally
				{
					if (enumerator is IDisposable)
					{
						(enumerator as IDisposable).Dispose();
					}
				}
				start.Enabled = true;
			}
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			if (MyProject.Computer.FileSystem.FileExists(Application.StartupPath + "\\path.txt"))
			{
				datapath = MyProject.Computer.FileSystem.ReadAllText(Application.StartupPath + "\\path.txt");
				if (!MyProject.Computer.FileSystem.DirectoryExists(Conversions.ToString(datapath)))
				{
					Interaction.MsgBox("Invalid path! Path does not exist. Setting path to 'C:\\Sporedata'", MsgBoxStyle.Critical, "SporeEdit v1.8 - Error");
					datapath = "C:\\Sporedata";
				}
			}
			else
			{
				Interaction.MsgBox("Path file not found! Setting path to 'C:\\Sporedata'", MsgBoxStyle.Critical, "SporeEdit v1.8 - Error");
				datapath = "C:\\Sporedata";
			}
			string text = (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Electronic Arts\\SPORE", "DataDir", "");
			checked
			{
				if (text != null && Operators.ConditionalCompareObjectGreater(NewLateBinding.LateGet(datapath, null, "Length", new object[0], null, null, null), 0, TextCompare: false))
				{
					if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"')
					{
						text = text.Substring(1, text.Length - 2);
					}
					openDialog.InitialDirectory = text;
				}
				LoadFileNames(Application.StartupPath + "\\data\\hash_names.txt");
				LoadGroupNames(Application.StartupPath + "\\data\\group_names.xml");
			}
		}

		private void reslist_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			try
			{
				MyProject.Computer.Clipboard.SetText(e.Node.Text);
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				Interaction.MsgBox("An error has occured in reslist_NodeMouseClick! " + ex2.Message, MsgBoxStyle.Critical);
				ProjectData.ClearProjectError();
			}
		}

		private void oall_Click(object sender, EventArgs e)
		{
			if (Operators.CompareString(tpages.SelectedTab.Text, "Edit", TextCompare: false) == 0)
			{
				Interaction.MsgBox("You cannot output while editing.", MsgBoxStyle.Critical, "SporeEdit v1.8 - Error");
				return;
			}
			try
			{
				if (!MyProject.Computer.FileSystem.DirectoryExists(Application.StartupPath + "\\output"))
				{
					MyProject.Computer.FileSystem.CreateDirectory(Application.StartupPath + "\\output");
				}
				string file = Application.StartupPath + "\\output\\" + Conversions.ToString(MyProject.Computer.Clock.LocalTime.Month) + "-" + Conversions.ToString(MyProject.Computer.Clock.LocalTime.Day) + "-" + Conversions.ToString(MyProject.Computer.Clock.LocalTime.Year) + ".txt";
				IEnumerator enumerator = default(IEnumerator);
				try
				{
					enumerator = reslist.Nodes.GetEnumerator();
					IEnumerator enumerator2 = default(IEnumerator);
					while (enumerator.MoveNext())
					{
						TreeNode treeNode = (TreeNode)enumerator.Current;
						MyProject.Computer.FileSystem.WriteAllText(file, treeNode.Text + "\r\n\r\n", append: true);
						try
						{
							enumerator2 = treeNode.Nodes.GetEnumerator();
							while (enumerator2.MoveNext())
							{
								TreeNode treeNode2 = (TreeNode)enumerator2.Current;
								MyProject.Computer.FileSystem.WriteAllText(file, treeNode2.Text + "\r\n\r\n\r\n\r\n", append: true);
							}
						}
						finally
						{
							if (enumerator2 is IDisposable)
							{
								(enumerator2 as IDisposable).Dispose();
							}
						}
					}
				}
				finally
				{
					if (enumerator is IDisposable)
					{
						(enumerator as IDisposable).Dispose();
					}
				}
				Interaction.MsgBox("Output completed!");
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				Interaction.MsgBox("An error has occured in oall_click! " + ex2.Message, MsgBoxStyle.Critical);
				ProjectData.ClearProjectError();
			}
		}

		private void edit_Click(object sender, EventArgs e)
		{
			//Discarded unreachable code: IL_010f
			if (Operators.CompareString(tpages.SelectedTab.Text, "Edit", TextCompare: false) == 0)
			{
				Interaction.MsgBox("You are already editing!", MsgBoxStyle.Critical, "SporeEdit v1.8 - Error");
				return;
			}
			tpages.SelectTab(1);
			try
			{
				IEnumerator enumerator = default(IEnumerator);
				try
				{
					enumerator = reslist.Nodes.GetEnumerator();
					IEnumerator enumerator2 = default(IEnumerator);
					while (enumerator.MoveNext())
					{
						TreeNode treeNode = (TreeNode)enumerator.Current;
						if (Operators.CompareString(reslist.SelectedNode.FullPath, treeNode.FullPath, TextCompare: false) == 0)
						{
							continue;
						}
						try
						{
							enumerator2 = treeNode.Nodes.GetEnumerator();
							while (enumerator2.MoveNext())
							{
								TreeNode treeNode2 = (TreeNode)enumerator2.Current;
								if (treeNode2.IsSelected)
								{
									editbox.Text = treeNode2.Text;
									filename = treeNode.Text;
									oldtext = treeNode2.Text;
									return;
								}
							}
						}
						finally
						{
							if (enumerator2 is IDisposable)
							{
								(enumerator2 as IDisposable).Dispose();
							}
						}
					}
				}
				finally
				{
					if (enumerator is IDisposable)
					{
						(enumerator as IDisposable).Dispose();
					}
				}
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				Interaction.MsgBox("An error has occured in edit_click! " + ex2.Message, MsgBoxStyle.Critical);
				ProjectData.ClearProjectError();
			}
		}

		private void save_Click(object sender, EventArgs e)
		{
			try
			{
				if (Operators.ConditionalCompareObjectEqual(filename, "", TextCompare: false))
				{
					Interaction.MsgBox("Filename is null, please select a result in search results.", MsgBoxStyle.Critical, "SporeEdit v1.8 - Error");
					return;
				}
				if (!MyProject.Computer.FileSystem.FileExists(Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject(datapath, "\\"), filename))))
				{
					Interaction.MsgBox(Operators.ConcatenateObject(Operators.ConcatenateObject("An error has occured! File ", filename), " does not exist!"), MsgBoxStyle.Critical, "SporeEdit v1.8 - Error");
					return;
				}
				if (savebox.Checked)
				{
					if (!MyProject.Computer.FileSystem.DirectoryExists(Application.StartupPath + "\\backups"))
					{
						MyProject.Computer.FileSystem.CreateDirectory(Application.StartupPath + "\\backups");
					}
					if (MyProject.Computer.FileSystem.FileExists(Conversions.ToString(Operators.ConcatenateObject(Application.StartupPath + "\\backups\\", filename))))
					{
						string text = MyProject.Computer.FileSystem.ReadAllText(Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject(datapath, "\\"), filename)));
						MyProject.Computer.FileSystem.WriteAllText(Conversions.ToString(Operators.ConcatenateObject(Application.StartupPath + "\\backups\\", filename)), text, append: false, Encoding.ASCII);
					}
					else
					{
						MyProject.Computer.FileSystem.CopyFile(Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject(datapath, "\\"), filename)), Conversions.ToString(Operators.ConcatenateObject(Application.StartupPath + "\\backups\\", filename)));
					}
				}
				string text2 = MyProject.Computer.FileSystem.ReadAllText(Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject(datapath, "\\"), filename)));
				text2 = text2.Replace(Conversions.ToString(oldtext), editbox.Text);
				MyProject.Computer.FileSystem.WriteAllText(Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject(datapath, "\\"), filename)), text2, append: false, Encoding.ASCII);
				IEnumerator enumerator = default(IEnumerator);
				try
				{
					enumerator = reslist.Nodes.GetEnumerator();
					IEnumerator enumerator2 = default(IEnumerator);
					while (enumerator.MoveNext())
					{
						TreeNode treeNode = (TreeNode)enumerator.Current;
						if (Operators.CompareString(reslist.SelectedNode.FullPath, treeNode.FullPath, TextCompare: false) == 0)
						{
							continue;
						}
						try
						{
							enumerator2 = treeNode.Nodes.GetEnumerator();
							while (enumerator2.MoveNext())
							{
								TreeNode treeNode2 = (TreeNode)enumerator2.Current;
								if (Operators.ConditionalCompareObjectEqual(treeNode2.Text, oldtext, TextCompare: false))
								{
									treeNode2.Text = editbox.Text;
									break;
								}
							}
						}
						finally
						{
							if (enumerator2 is IDisposable)
							{
								(enumerator2 as IDisposable).Dispose();
							}
						}
					}
				}
				finally
				{
					if (enumerator is IDisposable)
					{
						(enumerator as IDisposable).Dispose();
					}
				}
				Interaction.MsgBox("File saved.");
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				Interaction.MsgBox("An error has occured in save_click! " + ex2.Message, MsgBoxStyle.Critical);
				ProjectData.ClearProjectError();
			}
		}

		private void tpages_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (Operators.CompareString(tpages.SelectedTab.Text, "Edit", TextCompare: false) == 0)
			{
				start.Enabled = false;
				oall.Enabled = false;
				edit.Enabled = false;
			}
			else
			{
				start.Enabled = true;
				oall.Enabled = true;
				edit.Enabled = true;
			}
		}

		private void spath_Click(object sender, EventArgs e)
		{
			switch (pathdialog.ShowDialog())
			{
			case DialogResult.OK:
			{
				string selectedPath = pathdialog.SelectedPath;
				try
				{
					datapath = selectedPath;
					MyProject.Computer.FileSystem.WriteAllText(Application.StartupPath + "\\path.txt", selectedPath, append: false, Encoding.ASCII);
					Interaction.MsgBox("Path set to " + selectedPath + " successfully!");
				}
				catch (Exception ex)
				{
					ProjectData.SetProjectError(ex);
					Exception ex2 = ex;
					Interaction.MsgBox("An error has occured in path_save! " + ex2.Message, MsgBoxStyle.Critical);
					ProjectData.ClearProjectError();
				}
				break;
			}
			}
		}

		private void openMenuItem_Click(object sender, EventArgs e)
		{
			if (openDialog.ShowDialog() != DialogResult.OK)
			{
				return;
			}
			if (openDialog.InitialDirectory != null)
			{
				openDialog.InitialDirectory = null;
			}
			Stream stream = openDialog.OpenFile();
			DatabasePackedFile databasePackedFile = new DatabasePackedFile();
			databasePackedFile.Read(stream);
			DatabaseFiles = databasePackedFile.Indices.ToArray();
			Dictionary<uint, TreeNode> dictionary = new Dictionary<uint, TreeNode>();
			typeList.Nodes.Clear();
			typeList.BeginUpdate();
			TreeNode treeNode = new TreeNode("Known");
			TreeNode treeNode2 = new TreeNode("Unknown");
			checked
			{
				int num = DatabaseFiles.Length - 1;
				for (int i = 0; i <= num; i++)
				{
					DatabaseIndex databaseIndex = DatabaseFiles[i];
					TreeNode treeNode3 = null;
					if (!dictionary.ContainsKey(databaseIndex.TypeId))
					{
						treeNode3 = new TreeNode();
						string text = Types.GetExtensionFromId(databaseIndex.TypeId);
						bool flag = false;
						if (text == null)
						{
							text = "#" + databaseIndex.TypeId.ToString("X8");
							flag = true;
						}
						treeNode3.Text = text;
						treeNode3.Tag = new List<DatabaseIndex>();
						if (!flag)
						{
							treeNode.Nodes.Add(treeNode3);
						}
						else
						{
							treeNode3.NodeFont = MonospaceFont;
							treeNode2.Nodes.Add(treeNode3);
						}
						dictionary[databaseIndex.TypeId] = treeNode3;
					}
					else
					{
						treeNode3 = dictionary[databaseIndex.TypeId];
					}
					List<DatabaseIndex> list = treeNode3.Tag as List<DatabaseIndex>;
					list.Add(databaseIndex);
				}
				if (treeNode.Nodes.Count > 0)
				{
					typeList.Nodes.Add(treeNode);
				}
				if (treeNode2.Nodes.Count > 0)
				{
					typeList.Nodes.Add(treeNode2);
				}
				typeList.Sort();
				typeList.EndUpdate();
				if (treeNode.Nodes.Count > 0)
				{
					treeNode.Expand();
				}
				else if (treeNode2.Nodes.Count > 0)
				{
					treeNode2.Expand();
				}
			}
		}

		private void typeList_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			if (e.Node == null || e.Node.Tag == null)
			{
				return;
			}
			List<DatabaseIndex> list = e.Node.Tag as List<DatabaseIndex>;
			fileList.BeginUpdate();
			fileList.Items.Clear();
			fileList.Sorting = SortOrder.None;
			foreach (DatabaseIndex item in list)
			{
				ListViewItem listViewItem = new ListViewItem("");
				if (FileNames.ContainsKey(item.InstanceId))
				{
					listViewItem.Text = FileNames[item.InstanceId];
				}
				else
				{
					listViewItem.Text = "#" + item.InstanceId.ToString("X8");
				}
				if (GroupNames.ContainsKey(item.GroupId))
				{
					listViewItem.SubItems.Add(GroupNames[item.GroupId]);
				}
				else
				{
					listViewItem.SubItems.Add("#" + item.GroupId.ToString("X8")).Font = MonospaceFont;
				}
				listViewItem.SubItems.Add(item.DecompressedSize.ToString());
				listViewItem.SubItems.Add(item.Unknown.ToString("X8")).Font = MonospaceFont;
				listViewItem.Tag = item;
				fileList.Items.Add(listViewItem);
			}
			fileList.Sorting = SortOrder.Ascending;
			fileList.Sort();
			fileList.EndUpdate();
		}

		private void saveAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (typeList.Nodes.Count == 0)
			{
				Interaction.MsgBox("An error has occured! You must open a .package before you can save all files.", MsgBoxStyle.Critical, "SporeEdit v1.8 - Error");
			}
			else if (saveAllFolderDialog.ShowDialog() == DialogResult.OK)
			{
				Stream stream = openDialog.OpenFile();
				if (stream != null)
				{
					string selectedPath = saveAllFolderDialog.SelectedPath;
					SaveAllProgress saveAllProgress = new SaveAllProgress();
					saveAllProgress.ShowSaveProgress(this, stream, DatabaseFiles, FileNames, GroupNames, selectedPath);
					stream.Close();
				}
			}
		}

		private void cxml_Click(object sender, EventArgs e)
		{
			try
			{
				if (cxmlbrowser.ShowDialog() == DialogResult.OK)
				{
					PropConverter converter = new PropConverter();
					string selectedPath = cxmlbrowser.SelectedPath;
					cresult.Text = "";
					PropHandleDirectory(converter, selectedPath, "*.prop", recursive: true);
				}
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				Interaction.MsgBox("An error has occured in cxml_click! " + ex2.Message, MsgBoxStyle.Critical);
				ProjectData.ClearProjectError();
			}
		}

		private void cprop_Click(object sender, EventArgs e)
		{
			if (cxmlbrowser.ShowDialog() == DialogResult.OK)
			{
				XMLConverter converter = new XMLConverter();
				string selectedPath = cxmlbrowser.SelectedPath;
				cresult.Text = "";
				XMLHandleDirectory(converter, selectedPath, "*.prop.xml", recursive: true);
			}
		}

		private void cprop_Click_temp()
		{
			Interaction.MsgBox("An error has occured! This feature has been temporarily disabled.", MsgBoxStyle.Critical, "SporeEdit v1.8 - Error");
		}

		private void credits_Click(object sender, EventArgs e)
		{
			Credits credits = new Credits();
			credits.ShowDialog();
		}

		private void sopen_Click(object sender, EventArgs e)
		{
			//Discarded unreachable code: IL_0103, IL_0141
			if (Operators.CompareString(tpages.SelectedTab.Text, "Search", TextCompare: false) != 0)
			{
				Interaction.MsgBox("An error has occured! You cannot open a file in Notepad when you aren't searching.", MsgBoxStyle.Critical, "SporeEdit v1.8 - Error");
				return;
			}
			if (!MyProject.Computer.FileSystem.FileExists("C:\\WINDOWS\\notepad.exe"))
			{
				Interaction.MsgBox("An error has occured! Notepad was not found on your harddrive.", MsgBoxStyle.Critical, "SporeEdit v1.8 - Error");
				return;
			}
			try
			{
				IEnumerator enumerator = default(IEnumerator);
				try
				{
					enumerator = reslist.Nodes.GetEnumerator();
					IEnumerator enumerator2 = default(IEnumerator);
					while (enumerator.MoveNext())
					{
						TreeNode treeNode = (TreeNode)enumerator.Current;
						if (Operators.CompareString(reslist.SelectedNode.FullPath, treeNode.FullPath, TextCompare: false) != 0)
						{
							try
							{
								enumerator2 = treeNode.Nodes.GetEnumerator();
								while (enumerator2.MoveNext())
								{
									TreeNode treeNode2 = (TreeNode)enumerator2.Current;
									if (treeNode2.IsSelected)
									{
										Interaction.Shell("C:\\WINDOWS\\notepad.exe " + treeNode2.Parent.Text, AppWinStyle.NormalFocus);
										return;
									}
								}
							}
							finally
							{
								if (enumerator2 is IDisposable)
								{
									(enumerator2 as IDisposable).Dispose();
								}
							}
							continue;
						}
						Interaction.Shell("C:\\WINDOWS\\notepad.exe " + treeNode.Text, AppWinStyle.NormalFocus);
						break;
					}
				}
				finally
				{
					if (enumerator is IDisposable)
					{
						(enumerator as IDisposable).Dispose();
					}
				}
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				Interaction.MsgBox("An error has occured! " + ex2.Message, MsgBoxStyle.Critical, "SporeEdit v1.8 - Error");
				ProjectData.ClearProjectError();
			}
		}

		private void searchbox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Return)
			{
				start_Click();
			}
		}
	}
}
