using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Gibbed.Spore.Helpers;
using Gibbed.Spore.Package;
using Gibbed.Spore.Properties;
using System.Xml;
using SporeMaster;
using System.Collections;

namespace WindowsFormsApplication1
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            ListBoxModColours = new List<Color>();
            ListBoxModFiles = new List<FileListEntry>();
            InitializeComponent();
        }

        private List<Color> ListBoxModColours;
        private List<FileListEntry> ListBoxModFiles;

        private class FileListEntry
        {
            public FileListEntry(String f1, String f2)
            {
                file1 = f1;
                file2 = f2;
                compareWindow = null;
            }

            public String file1;
            public String file2;
            public IndividualCompare compareWindow;
        };

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = browseButton.Enabled = textBox1.Enabled = false;

            foreach (FileListEntry f in ListBoxModFiles)
            {
                if (f.compareWindow != null)
                    f.compareWindow.Close();
            }

            listBox1.Items.Clear();
            ListBoxModColours.Clear();
            ListBoxModFiles.Clear();

            statusMode.Text = "Scanning";
            statusProgress.Visible = true;
            if (textBox1.Text != "")
            {
                //Constants
                /*NameRegistry.Files = NameRegistry.Groups = new NameRegistry();
                NameRegistry.Types = new NameRegistry();
                NameRegistry.Properties = new NameRegistry();*/
                /*string appPath = Path.GetDirectoryName(Application.ExecutablePath) + "\\";
                NameRegistry.Files = NameRegistry.Groups = new NameRegistry(appPath+"reg_file.txt");
                NameRegistry.Types = new NameRegistry(appPath+"reg_type.txt");
                NameRegistry.Properties = new NameRegistry(appPath+"reg_property.txt");*/
                /*var group_sporemaster = "sporemaster".FNV();
                var instance_names = "names".FNV();
                var locale_type = NameRegistry.Types.toHash("locale");
                var prop_type = NameRegistry.Types.toHash("prop");
                var rw4_type = NameRegistry.Types.toHash("rw4");*/

                string[] fileEntries = Directory.GetFiles(textBox1.Text, "*.package");
                foreach (string fileName1 in fileEntries)
                {
                    bool skipFiles = true;
                    foreach (string fileName2 in fileEntries)
                    {
                        if (fileName1 == fileName2)
                        {
                            skipFiles = false;
                            continue;
                        }
                        if (skipFiles)
                            continue; // don't compare files we've already compared.

                        String fileNameShort1 = Path.GetFileNameWithoutExtension(fileName1);
                        String fileNameShort2 = Path.GetFileNameWithoutExtension(fileName2);

                        if (ignoreCheckBox.Checked && (fileNameShort1.ToLower().StartsWith("spore_") || fileNameShort2.ToLower().StartsWith("spore_")))
                            continue;

                        statusFiles.Text = fileNameShort1 + " -- " + fileNameShort2;

                        //Streams
                        Stream ModFile = File.OpenRead(fileName1);
                        Stream CompareFile = File.OpenRead(fileName2);

                        DatabasePackedFile dbModFile = new DatabasePackedFile();
                        DatabasePackedFile dbCompareFile = new DatabasePackedFile();
                        dbModFile.Read(ModFile);
                        dbCompareFile.Read(CompareFile);

                        ModFile.Close();
                        CompareFile.Close();

                        var ModDbFiles = dbModFile.Indices;
                        var CompareDbFiles = dbCompareFile.Indices;

                        bool matchFound = false;

                        statusProgress.Maximum = ModDbFiles.Count;
                        foreach (var i in ModDbFiles)
                        {
                            if (i.GroupId == 0x9C9059AE && i.InstanceId == 0xCC2F616F && i.TypeId == 0x2B6CAB5F)
                                continue; // sporemaster/names.txt

                            Application.DoEvents(); // Keep the form responsive.
                            foreach (var c in CompareDbFiles)
                            {
                                if (i.GroupId == c.GroupId && i.InstanceId == c.InstanceId && i.TypeId == c.TypeId)
                                {
                                    //MessageBox.Show( i.GroupId.ToString("X") + " " + i.InstanceId.ToString("X") + " " + i.TypeId.ToString("X") );
                                    matchFound = true;
                                    break;
                                }
                                if (matchFound)
                                    break;
                            }
                            try
                            {
                                statusProgress.Value++;
                            }
                            catch ( NullReferenceException ex )
                            {
                                return; // They closed the form during Application.DoEvents().
                            }
                        }
                        statusProgress.Value = 0;
                        listBox1.Items.Add((matchFound ? "Inc" : "C") + "ompatible: " + fileNameShort1 + " - " + fileNameShort2);
                        ListBoxModColours.Add(matchFound ? Color.Red : Color.Green);
                        ListBoxModFiles.Add(new FileListEntry(fileName1, fileName2));
                    }
                }
            }
            statusMode.Text = listBox1.Items.Count > 0 ? "Double-click an item to see the details." : "Idle";
            statusFiles.Text = "";
            statusProgress.Visible = false;

            button1.Enabled = browseButton.Enabled = textBox1.Enabled = true;
        }
        /*
        private ArrayList CheckReplaceFileName(string[] items ,string name,UInt32 hash)
        {
            ArrayList returnItems = new ArrayList();
            string hashString = ("#" + hash.ToString("X8"));
            for (int i = 0; i < items.Length; i++ )
            {
                if (items[i].Contains(hashString))
                {
                    returnItems.Add(items[i].Replace(hashString, name));
                }
                else
                {
                    returnItems.Add(items[i]);
                }
            }
            return returnItems;
        }*/

        private byte[] unpack(Stream archive, DatabaseIndex index)
        {
            if (index.Compressed)
            {
                archive.Seek(index.Offset, SeekOrigin.Begin);
                return archive.RefPackDecompress(index.CompressedSize, index.DecompressedSize);
            }
            else
            {
                archive.Seek(index.Offset, SeekOrigin.Begin);
                byte[] d = new byte[index.DecompressedSize];
                archive.Read(d, 0, d.Length);
                return d;
            }
        }

        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if ( e.Index >= 0 )
                e.Graphics.DrawString(listBox1.Items[e.Index].ToString(), listBox1.Font, new SolidBrush(ListBoxModColours[e.Index]), e.Bounds);           
        }

        private void listBox1_MeasureItem(object sender, MeasureItemEventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //((ListBox)sender).SelectedIndex = -1;
        }

        private void browseButton_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                textBox1.Text = folderBrowserDialog1.SelectedPath;
        }

        private void textBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            browseButton_Click(sender, null);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.Text = System.Environment.SpecialFolder.ProgramFiles + "\\Electronic Arts\\SPORE\\Data";
            if (File.Exists(textBox1.Text))
                return;
            textBox1.Text = System.Environment.SpecialFolder.ProgramFiles + "\\SPORE\\Data";
            if (File.Exists(textBox1.Text))
                return;
            textBox1.Text = System.Environment.SpecialFolder.ProgramFiles + " (x86)\\Electronic Arts\\SPORE\\Data";
            if (File.Exists(textBox1.Text))
                return;
            textBox1.Text = System.Environment.SpecialFolder.ProgramFiles + " (x86)\\SPORE\\Data";
            if (!File.Exists(textBox1.Text))
                textBox1.Text = "";
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
            {
                if (ListBoxModFiles[listBox1.SelectedIndex].compareWindow == null || ListBoxModFiles[listBox1.SelectedIndex].compareWindow.IsDisposed)
                    ListBoxModFiles[listBox1.SelectedIndex].compareWindow = new IndividualCompare(ListBoxModFiles[listBox1.SelectedIndex].file1, ListBoxModFiles[listBox1.SelectedIndex].file2, this);
                else
                    ListBoxModFiles[listBox1.SelectedIndex].compareWindow.Focus();
            }
        }
    }
}
