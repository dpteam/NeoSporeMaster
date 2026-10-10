using System;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace SporeEdit_v1._8
{
	public class SyntaxRTB : RichTextBox
	{
		private enum EditMessages
		{
			LineIndex = 187,
			LineFromChar = 201,
			GetFirstVisibleLine = 206,
			CharFromPos = 215,
			PosFromChar = 1062
		}

		private bool _SyntaxHighlight_CaseSensitive;

		private DataTable Words;

		public bool CaseSensitive
		{
			get
			{
				return _SyntaxHighlight_CaseSensitive;
			}
			set
			{
				_SyntaxHighlight_CaseSensitive = value;
			}
		}

		[DllImport("user32", CharSet = CharSet.Ansi, EntryPoint = "SendMessageA", ExactSpelling = true, SetLastError = true)]
		private static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

		[DllImport("user32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
		private static extern int LockWindowUpdate(int hWnd);

		protected override void OnTextChanged(EventArgs e)
		{
			ColorVisibleLines();
		}

		public void ColorRtb()
		{
			for (int i = 0; i < Lines.Length; i = checked(i + 1))
			{
				int charFromLineIndex = GetCharFromLineIndex(i);
				ColorLineNumber(i, charFromLineIndex);
			}
		}

		public void ColorVisibleLines()
		{
			int i = FirstVisibleLine();
			int num = LastVisibleLine();
			if (!(i == 0 && num == 0))
			{
				for (; i < num; i = checked(i + 1))
				{
					int charFromLineIndex = GetCharFromLineIndex(i);
					ColorLineNumber(i, charFromLineIndex);
				}
			}
		}

		public void ColorLineNumber(int LineIndex, int lStart)
		{
			int num = 0;
			int num2 = SelectionStart;
			LockWindowUpdate(Handle.ToInt32());
			int num3 = lStart;
			string[] array = ((!CaseSensitive) ? Strings.Split(Lines[LineIndex].ToLower()) : Strings.Split(Lines[LineIndex].ToString()));
			string[] array2 = array;
			checked
			{
				foreach (string text in array2)
				{
					SelectionStart = num3;
					SelectionLength = text.Length;
					if (Words.Rows.Contains(text))
					{
						DataRow dataRow = Words.Rows.Find(text);
						if (Conversions.ToBoolean(Operators.OrObject(!CaseSensitive, Operators.AndObject(CaseSensitive, Operators.CompareObjectEqual(dataRow["Word"], text, TextCompare: false)))))
						{
							SelectionColor = Color.FromName(Conversions.ToString(dataRow["Color"]));
						}
					}
					else
					{
						SelectionColor = Color.Black;
					}
					num3 += text.Length + 1;
				}
				string text2 = Lines[LineIndex];
				if (text2.Contains("#"))
				{
					SelectionStart = text2.IndexOf("#");
					SelectionLength = text2.Length - SelectionStart;
					SelectionColor = Color.Green;
				}
				if (text2.Contains("//"))
				{
					SelectionStart = text2.IndexOf("//");
					SelectionLength = text2.Length - SelectionStart;
					SelectionColor = Color.Green;
				}
				SelectionStart = num2;
				SelectionLength = 0;
				LockWindowUpdate(0);
			}
		}

		public int GetCharFromLineIndex(int LineIndex)
		{
			return SendMessage(Handle, 187, LineIndex, 0);
		}

		public int FirstVisibleLine()
		{
			return SendMessage(Handle, 206, 0, 0);
		}

		public int LastVisibleLine()
		{
			int num = checked((int)Math.Round((double)FirstVisibleLine() + (double)Height / (double)Font.Height));
			if (num > Lines.Length || num == 0)
			{
				num = Lines.Length;
			}
			return num;
		}

		public SyntaxRTB()
		{
			_SyntaxHighlight_CaseSensitive = false;
			Words = new DataTable();
			AcceptsTab = true;
			Words.Columns.Add("Word");
			Words.PrimaryKey = new DataColumn[1] { Words.Columns[0] };
			Words.Columns.Add("Color");
			string[] array = new string[33]
			{
				"property", "propertyGroup", "new", "state", "AddTransition", "Mode", "sinclude", "DeclareMessage", "DeclareState", "TECategory",
				"TEScript", "IncludeMachine", "set", "seti", "option", "include", "cheat", "hash", "log", "logger",
				"define", "prop", "enddef", "if", "else", "elseif", "endif", "setVariables", "setb", "setoption",
				"boolProp", "setResolution", "appProperties"
			};
			string[] array2 = new string[11]
			{
				"uint32", "bool", "int", "float", "Control", "Alt", "Shift", "KeyUp", "KeyDown", "true",
				"false"
			};
			string[] array3 = new string[6] { "-Event", "-Message", "-KeyMod", "-FromState", "-ToState", "-ObjectFlags" };
			string[] array4 = array;
			foreach (string value in array4)
			{
				DataRow dataRow = Words.NewRow();
				dataRow["Word"] = value;
				dataRow["Color"] = Color.Blue.Name;
				Words.Rows.Add(dataRow);
			}
			string[] array5 = array2;
			foreach (string value in array5)
			{
				DataRow dataRow = Words.NewRow();
				dataRow["Word"] = value;
				dataRow["Color"] = Color.DarkCyan.Name;
				Words.Rows.Add(dataRow);
			}
			string[] array6 = array3;
			foreach (string value in array6)
			{
				DataRow dataRow = Words.NewRow();
				dataRow["Word"] = value;
				dataRow["Color"] = Color.Orange.Name;
				Words.Rows.Add(dataRow);
			}
		}
	}
}
