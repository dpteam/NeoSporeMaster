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
    public partial class IndividualCompare : Form
    {
        Color[] ListBoxModColours;
        Color[] ListBoxCompareColours;
        public IndividualCompare( String file1, String file2, Form launcher )
        {
            InitializeComponent();

            Owner = launcher;

            file1Label.Text = Path.GetFileNameWithoutExtension(file1);
            file2Label.Text = Path.GetFileNameWithoutExtension(file2);

            //Constants
            string appPath = Path.GetDirectoryName(Application.ExecutablePath) + "\\";
            NameRegistry.Files = NameRegistry.Groups = new NameRegistry(appPath+"reg_file.txt");
            NameRegistry.Types = new NameRegistry(appPath+"reg_type.txt");
            NameRegistry.Properties = new NameRegistry(appPath+"reg_property.txt");
            var group_sporemaster = "sporemaster".FNV();
            var instance_names = "names".FNV();
            var locale_type = NameRegistry.Types.toHash("locale");
            var prop_type = NameRegistry.Types.toHash("prop");
            var rw4_type = NameRegistry.Types.toHash("rw4");

            //Streams
            Stream ModFile = File.OpenRead(file1);
            Stream CompareFile = File.OpenRead(file2);

            DatabasePackedFile dbModFile = new DatabasePackedFile();
            DatabasePackedFile dbCompareFile = new DatabasePackedFile();
            dbModFile.Read(ModFile);
            dbCompareFile.Read(CompareFile);

            var ModDbFiles = dbModFile.Indices;
            var CompareDbFiles = dbCompareFile.Indices;

            ArrayList ModFileList = new ArrayList();
            ArrayList CompareFileList = new ArrayList();
            foreach (var i in ModDbFiles)
            {
                /*if (!(i.TypeId == locale_type && (NameRegistry.Groups.toName(i.InstanceId).StartsWith("auto_") ||NameRegistry.Groups.toName(i.InstanceId).EndsWith("_auto"))))
                {*/
                    var fn = NameRegistry.getFileName(i.GroupId, i.InstanceId, i.TypeId);
                    if (!fn.Contains("sporemaster/names.txt"))
                    {// add filenames to list
                        ModFileList.Add(fn);
                    }
                    else
                    {
                        // replace hashed filenames with the actual names from sporemaster names textfile if theyre there
                        byte[] data = unpack(ModFile, i);
                        UTF8Encoding enc = new UTF8Encoding();
                        string[] refs = enc.GetString(data, 0, data.Length).Split(new string[]{"\r\n"},StringSplitOptions.RemoveEmptyEntries);
                        if (refs != null)
                        {
                            foreach (string r in refs)
                            {
                                ModFileList = CheckReplaceFileName((string[])ModFileList.ToArray(typeof(string)), r, NameRegistry.Files.toHash(r));
                            }
                        }
                    }
                //}
            }
            foreach (var c in CompareDbFiles)
            {
                /*if (!(c.TypeId == locale_type && (NameRegistry.Groups.toName(c.InstanceId).StartsWith("auto_") ||NameRegistry.Groups.toName(c.InstanceId).EndsWith("_auto"))))
                {*/
                    var fn = NameRegistry.getFileName(c.GroupId, c.InstanceId, c.TypeId);
                    if (!fn.Contains("sporemaster/names.txt"))
                    {// add filenames to list*/
                        CompareFileList.Add(fn);
                    }
                    else
                    {
                        // replace hashed filenames with the actual names from sporemaster names textfile if theyre there
                        byte[] data = unpack(CompareFile, c);
                        UTF8Encoding enc = new UTF8Encoding();
                        string[] refs = enc.GetString(data, 0, data.Length).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                        if (refs != null)
                        {
                            foreach (string r in refs)
                            {
                                CompareFileList = CheckReplaceFileName((string[])CompareFileList.ToArray(typeof(string)), r, NameRegistry.Files.toHash(r));
                            }
                        }
                    }
                //}
            }

            ModFile.Close();
            CompareFile.Close();

            populateListBoxes(ModFileList, CompareFileList);
            Show();
        }
       
        private void populateListBoxes(ArrayList Mod, ArrayList Compare)
        {
            string[] ModFiles = (string[])Mod.ToArray(typeof(string));
            string[] CompareFiles = (string[])Compare.ToArray(typeof(string));
            ListBoxModColours = (from m in ModFiles select Compare.Contains(m) ? Color.Red : Color.Black).ToArray();
            ListBoxCompareColours = (from m in CompareFiles select Mod.Contains(m) ? Color.Red : Color.Black).ToArray();

            listBox1.DataSource = ModFiles;
            listBox2.DataSource = CompareFiles;
            listBox1.SelectedIndex = -1;
            listBox2.SelectedIndex = -1;
        }
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
        }

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
            e.Graphics.DrawString(listBox1.Items[e.Index].ToString(), listBox1.Font, new SolidBrush(ListBoxModColours[e.Index]), e.Bounds);           
        }

        private void listBox2_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            e.Graphics.DrawString(listBox2.Items[e.Index].ToString(), listBox2.Font, new SolidBrush(ListBoxCompareColours[e.Index]), e.Bounds);
        }

        private void listBox1_MeasureItem(object sender, MeasureItemEventArgs e)
        {

        }

        private void listBox2_MeasureItem(object sender, MeasureItemEventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ((ListBox)sender).SelectedIndex = -1;
        }

        private void IndividualCompare_Load(object sender, EventArgs e)
        {

        }

        private void IndividualCompare_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Close();
        }
    }
}
