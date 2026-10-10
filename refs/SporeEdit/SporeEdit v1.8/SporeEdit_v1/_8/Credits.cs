using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace SporeEdit_v1._8
{
	[DesignerGenerated]
	public class Credits : Form
	{
		private IContainer components;

		[AccessedThroughProperty("TableLayoutPanel")]
		private TableLayoutPanel _TableLayoutPanel;

		[AccessedThroughProperty("LogoPictureBox")]
		private PictureBox _LogoPictureBox;

		[AccessedThroughProperty("LabelProductName")]
		private Label _LabelProductName;

		[AccessedThroughProperty("TableLayoutPanel1")]
		private TableLayoutPanel _TableLayoutPanel1;

		[AccessedThroughProperty("PictureBox1")]
		private PictureBox _PictureBox1;

		[AccessedThroughProperty("Label1")]
		private Label _Label1;

		[AccessedThroughProperty("TableLayoutPanel2")]
		private TableLayoutPanel _TableLayoutPanel2;

		[AccessedThroughProperty("PictureBox2")]
		private PictureBox _PictureBox2;

		[AccessedThroughProperty("Label2")]
		private Label _Label2;

		[AccessedThroughProperty("TextBoxDescription")]
		private TextBox _TextBoxDescription;

		[AccessedThroughProperty("OKButton")]
		private Button _OKButton;

		[AccessedThroughProperty("ricklink")]
		private LinkLabel _ricklink;

		[AccessedThroughProperty("judlink")]
		private LinkLabel _judlink;

		[AccessedThroughProperty("stevelink")]
		private LinkLabel _stevelink;

		internal virtual TableLayoutPanel TableLayoutPanel
		{
			[DebuggerNonUserCode]
			get
			{
				return _TableLayoutPanel;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_TableLayoutPanel = value;
			}
		}

		internal virtual PictureBox LogoPictureBox
		{
			[DebuggerNonUserCode]
			get
			{
				return _LogoPictureBox;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_LogoPictureBox = value;
			}
		}

		internal virtual Label LabelProductName
		{
			[DebuggerNonUserCode]
			get
			{
				return _LabelProductName;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_LabelProductName = value;
			}
		}

		internal virtual TableLayoutPanel TableLayoutPanel1
		{
			[DebuggerNonUserCode]
			get
			{
				return _TableLayoutPanel1;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_TableLayoutPanel1 = value;
			}
		}

		internal virtual PictureBox PictureBox1
		{
			[DebuggerNonUserCode]
			get
			{
				return _PictureBox1;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_PictureBox1 = value;
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

		internal virtual TableLayoutPanel TableLayoutPanel2
		{
			[DebuggerNonUserCode]
			get
			{
				return _TableLayoutPanel2;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_TableLayoutPanel2 = value;
			}
		}

		internal virtual PictureBox PictureBox2
		{
			[DebuggerNonUserCode]
			get
			{
				return _PictureBox2;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_PictureBox2 = value;
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

		internal virtual TextBox TextBoxDescription
		{
			[DebuggerNonUserCode]
			get
			{
				return _TextBoxDescription;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_TextBoxDescription = value;
			}
		}

		internal virtual Button OKButton
		{
			[DebuggerNonUserCode]
			get
			{
				return _OKButton;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_OKButton = value;
			}
		}

		internal virtual LinkLabel ricklink
		{
			[DebuggerNonUserCode]
			get
			{
				return _ricklink;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				EventHandler value2 = ricklink_Click;
				if (_ricklink != null)
				{
					_ricklink.Click -= value2;
				}
				_ricklink = value;
				if (_ricklink != null)
				{
					_ricklink.Click += value2;
				}
			}
		}

		internal virtual LinkLabel judlink
		{
			[DebuggerNonUserCode]
			get
			{
				return _judlink;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				LinkLabelLinkClickedEventHandler value2 = judlink_LinkClicked;
				if (_judlink != null)
				{
					_judlink.LinkClicked -= value2;
				}
				_judlink = value;
				if (_judlink != null)
				{
					_judlink.LinkClicked += value2;
				}
			}
		}

		internal virtual LinkLabel stevelink
		{
			[DebuggerNonUserCode]
			get
			{
				return _stevelink;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				EventHandler value2 = stevelink_Click;
				if (_stevelink != null)
				{
					_stevelink.Click -= value2;
				}
				_stevelink = value;
				if (_stevelink != null)
				{
					_stevelink.Click += value2;
				}
			}
		}

		[DebuggerNonUserCode]
		public Credits()
		{
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SporeEdit_v1._8.Credits));
			TableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			LogoPictureBox = new System.Windows.Forms.PictureBox();
			LabelProductName = new System.Windows.Forms.Label();
			TableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			PictureBox1 = new System.Windows.Forms.PictureBox();
			Label1 = new System.Windows.Forms.Label();
			TableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
			PictureBox2 = new System.Windows.Forms.PictureBox();
			Label2 = new System.Windows.Forms.Label();
			TextBoxDescription = new System.Windows.Forms.TextBox();
			OKButton = new System.Windows.Forms.Button();
			ricklink = new System.Windows.Forms.LinkLabel();
			judlink = new System.Windows.Forms.LinkLabel();
			stevelink = new System.Windows.Forms.LinkLabel();
			TableLayoutPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)LogoPictureBox).BeginInit();
			TableLayoutPanel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)PictureBox1).BeginInit();
			TableLayoutPanel2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)PictureBox2).BeginInit();
			SuspendLayout();
			TableLayoutPanel.ColumnCount = 2;
			TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33f));
			TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 67f));
			TableLayoutPanel.Controls.Add(LogoPictureBox, 0, 0);
			System.Drawing.Point point2 = (TableLayoutPanel.Location = new System.Drawing.Point(0, 0));
			TableLayoutPanel.Name = "TableLayoutPanel";
			TableLayoutPanel.RowCount = 1;
			TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20f));
			TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20f));
			TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20f));
			TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20f));
			TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20f));
			TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20f));
			System.Drawing.Size size2 = (TableLayoutPanel.Size = new System.Drawing.Size(200, 100));
			TableLayoutPanel.TabIndex = 0;
			LogoPictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
			LogoPictureBox.Image = (System.Drawing.Image)resources.GetObject("LogoPictureBox.Image");
			point2 = (LogoPictureBox.Location = new System.Drawing.Point(3, 3));
			LogoPictureBox.Name = "LogoPictureBox";
			TableLayoutPanel.SetRowSpan(LogoPictureBox, 6);
			size2 = (LogoPictureBox.Size = new System.Drawing.Size(60, 114));
			LogoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			LogoPictureBox.TabIndex = 0;
			LogoPictureBox.TabStop = false;
			LabelProductName.Dock = System.Windows.Forms.DockStyle.Fill;
			point2 = (LabelProductName.Location = new System.Drawing.Point(149, 0));
			System.Windows.Forms.Padding padding2 = (LabelProductName.Margin = new System.Windows.Forms.Padding(6, 0, 3, 0));
			size2 = (LabelProductName.MaximumSize = new System.Drawing.Size(0, 17));
			LabelProductName.Name = "LabelProductName";
			size2 = (LabelProductName.Size = new System.Drawing.Size(0, 17));
			LabelProductName.TabIndex = 0;
			LabelProductName.Text = "Product Name";
			LabelProductName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			TableLayoutPanel1.ColumnCount = 2;
			TableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33f));
			TableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 67f));
			TableLayoutPanel1.Controls.Add(PictureBox1, 0, 0);
			point2 = (TableLayoutPanel1.Location = new System.Drawing.Point(0, 0));
			TableLayoutPanel1.Name = "TableLayoutPanel1";
			TableLayoutPanel1.RowCount = 1;
			TableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20f));
			TableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20f));
			TableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20f));
			TableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20f));
			TableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20f));
			TableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20f));
			size2 = (TableLayoutPanel1.Size = new System.Drawing.Size(200, 100));
			TableLayoutPanel1.TabIndex = 0;
			PictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			PictureBox1.Image = (System.Drawing.Image)resources.GetObject("PictureBox1.Image");
			point2 = (PictureBox1.Location = new System.Drawing.Point(3, 3));
			PictureBox1.Name = "PictureBox1";
			TableLayoutPanel1.SetRowSpan(PictureBox1, 6);
			size2 = (PictureBox1.Size = new System.Drawing.Size(60, 114));
			PictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			PictureBox1.TabIndex = 0;
			PictureBox1.TabStop = false;
			Label1.Dock = System.Windows.Forms.DockStyle.Fill;
			point2 = (Label1.Location = new System.Drawing.Point(149, 0));
			padding2 = (Label1.Margin = new System.Windows.Forms.Padding(6, 0, 3, 0));
			size2 = (Label1.MaximumSize = new System.Drawing.Size(0, 17));
			Label1.Name = "Label1";
			size2 = (Label1.Size = new System.Drawing.Size(257, 17));
			Label1.TabIndex = 0;
			Label1.Text = "Product Name";
			Label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			TableLayoutPanel2.ColumnCount = 2;
			TableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33f));
			TableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 67f));
			TableLayoutPanel2.Controls.Add(PictureBox2, 0, 0);
			TableLayoutPanel2.Controls.Add(Label2, 1, 0);
			TableLayoutPanel2.Controls.Add(TextBoxDescription, 1, 4);
			TableLayoutPanel2.Controls.Add(OKButton, 1, 5);
			TableLayoutPanel2.Controls.Add(ricklink, 1, 1);
			TableLayoutPanel2.Controls.Add(judlink, 1, 2);
			TableLayoutPanel2.Controls.Add(stevelink, 1, 3);
			TableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
			point2 = (TableLayoutPanel2.Location = new System.Drawing.Point(0, 0));
			TableLayoutPanel2.Name = "TableLayoutPanel2";
			TableLayoutPanel2.RowCount = 6;
			TableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10f));
			TableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10f));
			TableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10f));
			TableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10f));
			TableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50f));
			TableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10f));
			size2 = (TableLayoutPanel2.Size = new System.Drawing.Size(435, 315));
			TableLayoutPanel2.TabIndex = 1;
			PictureBox2.Dock = System.Windows.Forms.DockStyle.Fill;
			PictureBox2.Image = (System.Drawing.Image)resources.GetObject("PictureBox2.Image");
			point2 = (PictureBox2.Location = new System.Drawing.Point(3, 3));
			PictureBox2.Name = "PictureBox2";
			TableLayoutPanel2.SetRowSpan(PictureBox2, 6);
			size2 = (PictureBox2.Size = new System.Drawing.Size(137, 309));
			PictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			PictureBox2.TabIndex = 0;
			PictureBox2.TabStop = false;
			Label2.Dock = System.Windows.Forms.DockStyle.Fill;
			point2 = (Label2.Location = new System.Drawing.Point(149, 0));
			padding2 = (Label2.Margin = new System.Windows.Forms.Padding(6, 0, 3, 0));
			size2 = (Label2.MaximumSize = new System.Drawing.Size(0, 17));
			Label2.Name = "Label2";
			size2 = (Label2.Size = new System.Drawing.Size(283, 17));
			Label2.TabIndex = 0;
			Label2.Text = "SporeEdit v1.8";
			Label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			TextBoxDescription.Dock = System.Windows.Forms.DockStyle.Fill;
			point2 = (TextBoxDescription.Location = new System.Drawing.Point(149, 127));
			padding2 = (TextBoxDescription.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3));
			TextBoxDescription.Multiline = true;
			TextBoxDescription.Name = "TextBoxDescription";
			TextBoxDescription.ReadOnly = true;
			TextBoxDescription.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			size2 = (TextBoxDescription.Size = new System.Drawing.Size(283, 151));
			TextBoxDescription.TabIndex = 0;
			TextBoxDescription.TabStop = false;
			TextBoxDescription.Text = resources.GetString("TextBoxDescription.Text");
			OKButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
			OKButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			point2 = (OKButton.Location = new System.Drawing.Point(357, 289));
			OKButton.Name = "OKButton";
			size2 = (OKButton.Size = new System.Drawing.Size(75, 23));
			OKButton.TabIndex = 0;
			OKButton.Text = "&OK";
			ricklink.AutoSize = true;
			point2 = (ricklink.Location = new System.Drawing.Point(146, 31));
			ricklink.Name = "ricklink";
			size2 = (ricklink.Size = new System.Drawing.Size(137, 13));
			ricklink.TabIndex = 1;
			ricklink.TabStop = true;
			ricklink.Text = "http://blog.spore.vg/mods/";
			judlink.AutoSize = true;
			point2 = (judlink.Location = new System.Drawing.Point(146, 62));
			judlink.Name = "judlink";
			size2 = (judlink.Size = new System.Drawing.Size(157, 13));
			judlink.TabIndex = 2;
			judlink.TabStop = true;
			judlink.Text = "http://www.sporeprograms.com";
			stevelink.AutoSize = true;
			point2 = (stevelink.Location = new System.Drawing.Point(146, 93));
			stevelink.Name = "stevelink";
			size2 = (stevelink.Size = new System.Drawing.Size(284, 13));
			stevelink.TabIndex = 3;
			stevelink.TabStop = true;
			stevelink.Text = "http://www.gamingsteve.com/blab/index.php?board=12.0";
			System.Drawing.SizeF sizeF2 = (AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f));
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			size2 = (ClientSize = new System.Drawing.Size(435, 315));
			Controls.Add(TableLayoutPanel2);
			FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			Name = "Credits";
			ShowInTaskbar = false;
			StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			Text = "Credits";
			TableLayoutPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)LogoPictureBox).EndInit();
			TableLayoutPanel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)PictureBox1).EndInit();
			TableLayoutPanel2.ResumeLayout(false);
			TableLayoutPanel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)PictureBox2).EndInit();
			ResumeLayout(false);
		}

		[DllImport("shell32.dll", CharSet = CharSet.Ansi, EntryPoint = "ShellExecuteA", ExactSpelling = true, SetLastError = true)]
		private static extern long ShellExecute(int hwnd, [MarshalAs(UnmanagedType.VBByRefStr)] ref string lpOperation, [MarshalAs(UnmanagedType.VBByRefStr)] ref string lpFile, [MarshalAs(UnmanagedType.VBByRefStr)] ref string lpParameters, [MarshalAs(UnmanagedType.VBByRefStr)] ref string lpDirectory, int nShowCmd);

		private void OK_Button_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void Cancel_Button_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void judlink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			try
			{
				string lpOperation = "open";
				LinkLabel linkLabel = judlink;
				string lpFile = linkLabel.Text;
				string lpParameters = Conversions.ToString(0);
				string lpDirectory = Conversions.ToString(0);
				ShellExecute(0, ref lpOperation, ref lpFile, ref lpParameters, ref lpDirectory, 1);
				linkLabel.Text = lpFile;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				Interaction.MsgBox("An error has occured! " + ex2.Message, MsgBoxStyle.Critical, "SporeEdit v1.8 - Error");
				ProjectData.ClearProjectError();
			}
		}

		private void ricklink_Click(object sender, EventArgs e)
		{
			try
			{
				string lpOperation = "open";
				LinkLabel linkLabel = ricklink;
				string lpFile = linkLabel.Text;
				string lpParameters = Conversions.ToString(0);
				string lpDirectory = Conversions.ToString(0);
				ShellExecute(0, ref lpOperation, ref lpFile, ref lpParameters, ref lpDirectory, 1);
				linkLabel.Text = lpFile;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				Interaction.MsgBox("An error has occured! " + ex2.Message, MsgBoxStyle.Critical, "SporeEdit v1.8 - Error");
				ProjectData.ClearProjectError();
			}
		}

		private void stevelink_Click(object sender, EventArgs e)
		{
			try
			{
				string lpOperation = "open";
				LinkLabel linkLabel = stevelink;
				string lpFile = linkLabel.Text;
				string lpParameters = Conversions.ToString(0);
				string lpDirectory = Conversions.ToString(0);
				ShellExecute(0, ref lpOperation, ref lpFile, ref lpParameters, ref lpDirectory, 1);
				linkLabel.Text = lpFile;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2 = ex;
				Interaction.MsgBox("An error has occured! " + ex2.Message, MsgBoxStyle.Critical, "SporeEdit v1.8 - Error");
				ProjectData.ClearProjectError();
			}
		}
	}
}
