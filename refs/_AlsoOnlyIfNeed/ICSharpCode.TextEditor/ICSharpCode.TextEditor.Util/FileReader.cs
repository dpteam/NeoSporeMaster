using System;
using System.IO;
using System.Text;

namespace ICSharpCode.TextEditor.Util;

public static class FileReader
{
	public static bool IsUnicode(Encoding encoding)
	{
		int codePage = encoding.CodePage;
		if (codePage != 65001 && codePage != 65000 && codePage != 1200)
		{
			return codePage == 1201;
		}
		return true;
	}

	public static string ReadFileContent(Stream fs, ref Encoding encoding)
	{
		using StreamReader streamReader = OpenStream(fs, encoding);
		streamReader.Peek();
		encoding = streamReader.CurrentEncoding;
		return streamReader.ReadToEnd();
	}

	public static string ReadFileContent(string fileName, Encoding encoding)
	{
		using FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		return ReadFileContent(fs, ref encoding);
	}

	public static StreamReader OpenStream(Stream fs, Encoding defaultEncoding)
	{
		if (fs == null)
		{
			throw new ArgumentNullException("fs");
		}
		if (fs.Length >= 2)
		{
			int num = fs.ReadByte();
			int num2 = fs.ReadByte();
			switch ((num << 8) | num2)
			{
			case 0:
			case 61371:
			case 65279:
			case 65534:
				fs.Position = 0L;
				return new StreamReader(fs);
			default:
				return AutoDetect(fs, (byte)num, (byte)num2, defaultEncoding);
			}
		}
		if (defaultEncoding != null)
		{
			return new StreamReader(fs, defaultEncoding);
		}
		return new StreamReader(fs);
	}

	private static StreamReader AutoDetect(Stream fs, byte firstByte, byte secondByte, Encoding defaultEncoding)
	{
		int num = (int)Math.Min(fs.Length, 500000L);
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < num; i++)
		{
			byte b = i switch
			{
				0 => firstByte, 
				1 => secondByte, 
				_ => (byte)fs.ReadByte(), 
			};
			if (b < 128)
			{
				if (num2 == 3)
				{
					num2 = 1;
					break;
				}
			}
			else if (b < 192)
			{
				if (num2 != 3)
				{
					num2 = 1;
					break;
				}
				num3--;
				if (num3 < 0)
				{
					num2 = 1;
					break;
				}
				if (num3 == 0)
				{
					num2 = 2;
				}
			}
			else
			{
				if (b < 194 || b >= 245)
				{
					num2 = 1;
					break;
				}
				if (num2 != 2 && num2 != 0)
				{
					num2 = 1;
					break;
				}
				num2 = 3;
				num3 = ((b < 224) ? 1 : ((b >= 240) ? 3 : 2));
			}
		}
		fs.Position = 0L;
		switch (num2)
		{
		case 0:
		case 1:
			if (IsUnicode(defaultEncoding))
			{
				defaultEncoding = Encoding.Default;
			}
			return new StreamReader(fs, defaultEncoding);
		default:
			return new StreamReader(fs);
		}
	}
}
