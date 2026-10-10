using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using VTreeView;

namespace SporeMaster;

internal class Renamer
{
	private byte[][] oldNames;

	private uint[] hashes;

	private string leftPath;

	private string rightPath;

	private List<string> errors = new List<string>();

	public static void rename(uint[] hashes, string[] oldNames, PleaseWait progress)
	{
		IEnumerable<byte[]> nameBytes = oldNames.Select((string n) => Encoding.UTF8.GetBytes(n.ToLowerInvariant()));
		DirectoryTree.Tree.Search(SearchSpec.all);
		Renamer renamer = new Renamer();
		renamer.oldNames = nameBytes.ToArray();
		renamer.hashes = hashes;
		renamer.leftPath = MainWindow.Instance.LeftPath + "\\";
		renamer.rightPath = MainWindow.Instance.RightPath + "\\";
		Renamer renamer2 = renamer;
		renamer2.renameTree(DirectoryTree.Tree);
		DirectoryTree.Tree.Search(SearchSpec.all);
		if (renamer2.errors.Count == 0)
		{
			return;
		}
		string s = "There were errors updating the following files:\n";
		foreach (string e in renamer2.errors)
		{
			s = s + "    " + e + "\n";
		}
		MessageBox.Show(s, "Warning");
	}

	private void renameTree(DirectoryTree node)
	{
		foreach (TreeNode c in node.Children)
		{
			renameTree(c as DirectoryTree);
		}
		if (node.FullName.EndsWith(".prop.xml"))
		{
			if (node.LeftPresent)
			{
				byte[][] array = oldNames;
				foreach (byte[] oldName in array)
				{
					if (node.LeftContains(oldName))
					{
						if (new RebuildPropertyFile(leftPath + node.Path).Failed)
						{
							errors.Add(leftPath + node.Path);
						}
						break;
					}
				}
			}
			if (node.RightPresent)
			{
				byte[][] array2 = oldNames;
				foreach (byte[] oldName2 in array2)
				{
					if (node.RightContains(oldName2))
					{
						if (new RebuildPropertyFile(rightPath + node.Path).Failed)
						{
							errors.Add(rightPath + node.Path);
						}
						break;
					}
				}
			}
		}
		try
		{
			uint hash = NameRegistry.Files.toHash(node.BaseName);
			if (!hashes.Contains(hash))
			{
				return;
			}
			string newPath = node.Parent.Path + "\\" + NameRegistry.Files.toName(hash);
			string ext = node.FileType;
			if (ext != "")
			{
				newPath = newPath + "." + ext;
			}
			if (newPath != node.Path)
			{
				if (node.LeftPresent)
				{
					Directory.Move(leftPath + node.Path, leftPath + newPath);
				}
				if (node.RightPresent)
				{
					Directory.Move(rightPath + node.Path, rightPath + newPath);
				}
			}
		}
		catch (Exception)
		{
			if (node.LeftPresent)
			{
				errors.Add(leftPath + node.Path);
			}
			if (node.RightPresent)
			{
				errors.Add(rightPath + node.Path);
			}
		}
	}
}
