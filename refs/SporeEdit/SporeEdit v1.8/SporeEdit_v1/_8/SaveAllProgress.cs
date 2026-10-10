using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Gibbed.Spore.Helpers;
using Gibbed.Spore.Package;
using Microsoft.VisualBasic.CompilerServices;

namespace SporeEdit_v1._8
{
	[DesignerGenerated]
	public class SaveAllProgress : Form
	{
		public delegate void SetStatusDelegate(string status, int percent);

		public delegate void SaveDoneDelegate();

		private struct SaveAllInformation
		{
			public string BasePath;

			public Stream Archive;

			public DatabaseIndex[] Files;

			public Dictionary<uint, string> FileNames;

			public Dictionary<uint, string> GroupNames;
		}

		private IContainer components;

		[AccessedThroughProperty("cancelButton")]
		private Button _cancelButton;

		[AccessedThroughProperty("statusLabel")]
		private Label _statusLabel;

		[AccessedThroughProperty("progressBar")]
		private ProgressBar _progressBar;

		private Thread SaveThread;

		private virtual Button cancelButton
		{
			[DebuggerNonUserCode]
			get
			{
				return _cancelButton;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_cancelButton = value;
			}
		}

		private virtual Label statusLabel
		{
			[DebuggerNonUserCode]
			get
			{
				return _statusLabel;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_statusLabel = value;
			}
		}

		private virtual ProgressBar progressBar
		{
			[DebuggerNonUserCode]
			get
			{
				return _progressBar;
			}
			[MethodImpl(MethodImplOptions.Synchronized)]
			[DebuggerNonUserCode]
			set
			{
				_progressBar = value;
			}
		}

		[DebuggerNonUserCode]
		public SaveAllProgress()
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
			cancelButton = new System.Windows.Forms.Button();
			statusLabel = new System.Windows.Forms.Label();
			progressBar = new System.Windows.Forms.ProgressBar();
			SuspendLayout();
			cancelButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
			System.Drawing.Point point2 = (cancelButton.Location = new System.Drawing.Point(387, 66));
			cancelButton.Name = "cancelButton";
			System.Drawing.Size size2 = (cancelButton.Size = new System.Drawing.Size(75, 23));
			cancelButton.TabIndex = 5;
			cancelButton.Text = "&Cancel";
			cancelButton.UseVisualStyleBackColor = true;
			statusLabel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			point2 = (statusLabel.Location = new System.Drawing.Point(11, 9));
			statusLabel.Name = "statusLabel";
			size2 = (statusLabel.Size = new System.Drawing.Size(451, 23));
			statusLabel.TabIndex = 4;
			statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			progressBar.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			point2 = (progressBar.Location = new System.Drawing.Point(14, 35));
			progressBar.Name = "progressBar";
			size2 = (progressBar.Size = new System.Drawing.Size(448, 23));
			progressBar.TabIndex = 3;
			progressBar.Value = 50;
			System.Drawing.SizeF sizeF2 = (AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f));
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			size2 = (ClientSize = new System.Drawing.Size(472, 98));
			Controls.Add(cancelButton);
			Controls.Add(statusLabel);
			Controls.Add(progressBar);
			FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			Name = "saveAllFolderDialog";
			ShowInTaskbar = false;
			StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			Text = "SporeEdit v1.8 - Save Progress";
			ResumeLayout(false);
		}

		private void SetStatus(string status, int percent)
		{
			if (progressBar.InvokeRequired || statusLabel.InvokeRequired)
			{
				SetStatusDelegate method = SetStatus;
				Invoke(method, status, percent);
			}
			else
			{
				statusLabel.Text = status;
				progressBar.Value = percent;
			}
		}

		private void SaveDone()
		{
			if (InvokeRequired)
			{
				SaveDoneDelegate method = SaveDone;
				Invoke(method);
			}
			else
			{
				Close();
			}
		}

		public void SaveAll(object oinfo)
		{
			SaveAllInformation saveAllInformation = (SaveAllInformation)oinfo;
			XmlTextWriter xmlTextWriter = new XmlTextWriter(saveAllInformation.BasePath + "\\files.xml", Encoding.UTF8);
			xmlTextWriter.Formatting = Formatting.Indented;
			xmlTextWriter.WriteStartDocument();
			xmlTextWriter.WriteStartElement("files");
			checked
			{
				int num = saveAllInformation.Files.Length - 1;
				for (int i = 0; i <= num; i++)
				{
					DatabaseIndex databaseIndex = saveAllInformation.Files[i];
					string text = null;
					string text2 = null;
					string text3 = null;
					text = ((!saveAllInformation.FileNames.ContainsKey(databaseIndex.InstanceId)) ? ("#" + databaseIndex.InstanceId.ToString("X8")) : saveAllInformation.FileNames[databaseIndex.InstanceId]);
					text3 = ((!saveAllInformation.GroupNames.ContainsKey(databaseIndex.GroupId)) ? ("#" + databaseIndex.GroupId.ToString("X8")) : saveAllInformation.GroupNames[databaseIndex.GroupId]);
					text2 = Types.GetExtensionFromId(databaseIndex.TypeId);
					if (text2 == null)
					{
						text2 = "#" + databaseIndex.TypeId.ToString("X8");
					}
					else
					{
						text = text + "." + text2;
					}
					string text4 = text2 + "\\" + text3;
					Directory.CreateDirectory(saveAllInformation.BasePath + "\\" + text4);
					string text5 = text4 + "\\" + text;
					SetStatus(text5, i);
					xmlTextWriter.WriteStartElement("file");
					xmlTextWriter.WriteAttributeString("groupid", "0x" + databaseIndex.GroupId.ToString("X8"));
					xmlTextWriter.WriteAttributeString("instanceid", "0x" + databaseIndex.InstanceId.ToString("X8"));
					xmlTextWriter.WriteAttributeString("typeid", "0x" + databaseIndex.TypeId.ToString("X8"));
					xmlTextWriter.WriteValue(text5);
					xmlTextWriter.WriteEndElement();
					text5 = saveAllInformation.BasePath + "\\" + text5;
					if (databaseIndex.Compressed)
					{
						saveAllInformation.Archive.Seek(databaseIndex.Offset, SeekOrigin.Begin);
						byte[] array = saveAllInformation.Archive.RefPackDecompress(databaseIndex.CompressedSize, databaseIndex.DecompressedSize);
						FileStream fileStream = new FileStream(text5, FileMode.Create);
						fileStream.Write(array, 0, array.Length);
						fileStream.Close();
					}
					else
					{
						saveAllInformation.Archive.Seek(databaseIndex.Offset, SeekOrigin.Begin);
						byte[] array2 = new byte[(int)(unchecked((long)databaseIndex.DecompressedSize) - 1L) + 1];
						saveAllInformation.Archive.Read(array2, 0, array2.Length);
						FileStream fileStream2 = new FileStream(text5, FileMode.Create);
						fileStream2.Write(array2, 0, array2.Length);
						fileStream2.Close();
					}
				}
				xmlTextWriter.WriteEndElement();
				xmlTextWriter.WriteEndDocument();
				xmlTextWriter.Flush();
				xmlTextWriter.Close();
				SaveDone();
			}
		}

		public void ShowSaveProgress(IWin32Window owner, Stream archive, DatabaseIndex[] files, Dictionary<uint, string> fileNames, Dictionary<uint, string> groupNames, string basePath)
		{
			SaveAllInformation saveAllInformation = default(SaveAllInformation);
			saveAllInformation.BasePath = basePath;
			saveAllInformation.Archive = archive;
			saveAllInformation.Files = files;
			saveAllInformation.FileNames = fileNames;
			saveAllInformation.GroupNames = groupNames;
			progressBar.Value = 0;
			progressBar.Maximum = files.Length;
			SaveThread = new Thread(SaveAll);
			SaveThread.Start(saveAllInformation);
			ShowDialog(owner);
		}

		private void OnCancel(object sender, EventArgs e)
		{
			if (SaveThread != null)
			{
				SaveThread.Abort();
			}
			Close();
		}
	}
}
