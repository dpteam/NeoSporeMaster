using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ICSharpCode.TextEditor;

internal class Ime
{
	[StructLayout(LayoutKind.Sequential)]
	private class COMPOSITIONFORM
	{
		public int dwStyle;

		public POINT ptCurrentPos;

		public RECT rcArea;
	}

	[StructLayout(LayoutKind.Sequential)]
	private class POINT
	{
		public int x;

		public int y;
	}

	[StructLayout(LayoutKind.Sequential)]
	private class RECT
	{
		public int left;

		public int top;

		public int right;

		public int bottom;
	}

	[StructLayout(LayoutKind.Sequential)]
	private class LOGFONT
	{
		public int lfHeight;

		public int lfWidth;

		public int lfEscapement;

		public int lfOrientation;

		public int lfWeight;

		public byte lfItalic;

		public byte lfUnderline;

		public byte lfStrikeOut;

		public byte lfCharSet;

		public byte lfOutPrecision;

		public byte lfClipPrecision;

		public byte lfQuality;

		public byte lfPitchAndFamily;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string lfFaceName;
	}

	private const int WM_IME_CONTROL = 643;

	private const int IMC_SETCOMPOSITIONWINDOW = 12;

	private const int CFS_POINT = 2;

	private const int IMC_SETCOMPOSITIONFONT = 10;

	private Font font;

	private IntPtr hIMEWnd;

	private IntPtr hWnd;

	private LOGFONT lf;

	private static bool disableIME;

	public Font Font
	{
		get
		{
			return font;
		}
		set
		{
			if (!value.Equals(font))
			{
				font = value;
				lf = null;
				SetIMEWindowFont(value);
			}
		}
	}

	public IntPtr HWnd
	{
		set
		{
			if (hWnd != value)
			{
				hWnd = value;
				if (!disableIME)
				{
					hIMEWnd = ImmGetDefaultIMEWnd(value);
				}
				SetIMEWindowFont(font);
			}
		}
	}

	public Ime(IntPtr hWnd, Font font)
	{
		string environmentVariable = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432");
		if (environmentVariable == "IA64" || environmentVariable == "AMD64" || Environment.OSVersion.Platform == PlatformID.Unix)
		{
			disableIME = true;
		}
		else
		{
			hIMEWnd = ImmGetDefaultIMEWnd(hWnd);
		}
		this.hWnd = hWnd;
		this.font = font;
		SetIMEWindowFont(font);
	}

	[DllImport("imm32.dll")]
	private static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hWnd);

	[DllImport("user32.dll")]
	private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, COMPOSITIONFORM lParam);

	[DllImport("user32.dll")]
	private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, [In][MarshalAs(UnmanagedType.LPStruct)] LOGFONT lParam);

	private void SetIMEWindowFont(Font f)
	{
		if (disableIME || hIMEWnd == IntPtr.Zero)
		{
			return;
		}
		if (lf == null)
		{
			lf = new LOGFONT();
			f.ToLogFont(lf);
			lf.lfFaceName = f.Name;
		}
		try
		{
			SendMessage(hIMEWnd, 643, new IntPtr(10), lf);
		}
		catch (AccessViolationException ex)
		{
			Handle(ex);
		}
	}

	public void SetIMEWindowLocation(int x, int y)
	{
		if (disableIME || hIMEWnd == IntPtr.Zero)
		{
			return;
		}
		POINT pOINT = new POINT();
		pOINT.x = x;
		pOINT.y = y;
		COMPOSITIONFORM cOMPOSITIONFORM = new COMPOSITIONFORM();
		cOMPOSITIONFORM.dwStyle = 2;
		cOMPOSITIONFORM.ptCurrentPos = pOINT;
		cOMPOSITIONFORM.rcArea = new RECT();
		try
		{
			SendMessage(hIMEWnd, 643, new IntPtr(12), cOMPOSITIONFORM);
		}
		catch (AccessViolationException ex)
		{
			Handle(ex);
		}
	}

	private void Handle(Exception ex)
	{
		Console.WriteLine(ex);
		if (!disableIME)
		{
			disableIME = true;
			MessageBox.Show("Error calling IME: " + ex.Message + "\nIME is disabled.", "IME error");
		}
	}
}
