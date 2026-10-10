using System.Drawing;
using System.Drawing.Text;
using System.Text;

namespace ICSharpCode.TextEditor.Document;

public class DefaultTextEditorProperties : ITextEditorProperties
{
	private int tabIndent = 4;

	private int indentationSize = 4;

	private IndentStyle indentStyle = IndentStyle.Smart;

	private DocumentSelectionMode documentSelectionMode;

	private Encoding encoding = Encoding.UTF8;

	private BracketMatchingStyle bracketMatchingStyle = BracketMatchingStyle.After;

	private FontContainer fontContainer;

	private static Font DefaultFont;

	private bool allowCaretBeyondEOL;

	private bool showMatchingBracket = true;

	private bool showLineNumbers = true;

	private bool showSpaces;

	private bool showTabs;

	private bool showEOLMarker;

	private bool showInvalidLines;

	private bool isIconBarVisible;

	private bool enableFolding = true;

	private bool showHorizontalRuler;

	private bool showVerticalRuler = true;

	private bool convertTabsToSpaces;

	private TextRenderingHint textRenderingHint;

	private bool mouseWheelScrollDown = true;

	private bool mouseWheelTextZoom = true;

	private bool hideMouseCursor;

	private bool cutCopyWholeLine = true;

	private int verticalRulerRow = 80;

	private LineViewerStyle lineViewerStyle;

	private string lineTerminator = "\r\n";

	private bool autoInsertCurlyBracket = true;

	private bool supportReadOnlySegments;

	public int TabIndent
	{
		get
		{
			return tabIndent;
		}
		set
		{
			tabIndent = value;
		}
	}

	public int IndentationSize
	{
		get
		{
			return indentationSize;
		}
		set
		{
			indentationSize = value;
		}
	}

	public IndentStyle IndentStyle
	{
		get
		{
			return indentStyle;
		}
		set
		{
			indentStyle = value;
		}
	}

	public DocumentSelectionMode DocumentSelectionMode
	{
		get
		{
			return documentSelectionMode;
		}
		set
		{
			documentSelectionMode = value;
		}
	}

	public bool AllowCaretBeyondEOL
	{
		get
		{
			return allowCaretBeyondEOL;
		}
		set
		{
			allowCaretBeyondEOL = value;
		}
	}

	public bool ShowMatchingBracket
	{
		get
		{
			return showMatchingBracket;
		}
		set
		{
			showMatchingBracket = value;
		}
	}

	public bool ShowLineNumbers
	{
		get
		{
			return showLineNumbers;
		}
		set
		{
			showLineNumbers = value;
		}
	}

	public bool ShowSpaces
	{
		get
		{
			return showSpaces;
		}
		set
		{
			showSpaces = value;
		}
	}

	public bool ShowTabs
	{
		get
		{
			return showTabs;
		}
		set
		{
			showTabs = value;
		}
	}

	public bool ShowEOLMarker
	{
		get
		{
			return showEOLMarker;
		}
		set
		{
			showEOLMarker = value;
		}
	}

	public bool ShowInvalidLines
	{
		get
		{
			return showInvalidLines;
		}
		set
		{
			showInvalidLines = value;
		}
	}

	public bool IsIconBarVisible
	{
		get
		{
			return isIconBarVisible;
		}
		set
		{
			isIconBarVisible = value;
		}
	}

	public bool EnableFolding
	{
		get
		{
			return enableFolding;
		}
		set
		{
			enableFolding = value;
		}
	}

	public bool ShowHorizontalRuler
	{
		get
		{
			return showHorizontalRuler;
		}
		set
		{
			showHorizontalRuler = value;
		}
	}

	public bool ShowVerticalRuler
	{
		get
		{
			return showVerticalRuler;
		}
		set
		{
			showVerticalRuler = value;
		}
	}

	public bool ConvertTabsToSpaces
	{
		get
		{
			return convertTabsToSpaces;
		}
		set
		{
			convertTabsToSpaces = value;
		}
	}

	public TextRenderingHint TextRenderingHint
	{
		get
		{
			return textRenderingHint;
		}
		set
		{
			textRenderingHint = value;
		}
	}

	public bool MouseWheelScrollDown
	{
		get
		{
			return mouseWheelScrollDown;
		}
		set
		{
			mouseWheelScrollDown = value;
		}
	}

	public bool MouseWheelTextZoom
	{
		get
		{
			return mouseWheelTextZoom;
		}
		set
		{
			mouseWheelTextZoom = value;
		}
	}

	public bool HideMouseCursor
	{
		get
		{
			return hideMouseCursor;
		}
		set
		{
			hideMouseCursor = value;
		}
	}

	public bool CutCopyWholeLine
	{
		get
		{
			return cutCopyWholeLine;
		}
		set
		{
			cutCopyWholeLine = value;
		}
	}

	public Encoding Encoding
	{
		get
		{
			return encoding;
		}
		set
		{
			encoding = value;
		}
	}

	public int VerticalRulerRow
	{
		get
		{
			return verticalRulerRow;
		}
		set
		{
			verticalRulerRow = value;
		}
	}

	public LineViewerStyle LineViewerStyle
	{
		get
		{
			return lineViewerStyle;
		}
		set
		{
			lineViewerStyle = value;
		}
	}

	public string LineTerminator
	{
		get
		{
			return lineTerminator;
		}
		set
		{
			lineTerminator = value;
		}
	}

	public bool AutoInsertCurlyBracket
	{
		get
		{
			return autoInsertCurlyBracket;
		}
		set
		{
			autoInsertCurlyBracket = value;
		}
	}

	public Font Font
	{
		get
		{
			return fontContainer.DefaultFont;
		}
		set
		{
			fontContainer.DefaultFont = value;
		}
	}

	public FontContainer FontContainer => fontContainer;

	public BracketMatchingStyle BracketMatchingStyle
	{
		get
		{
			return bracketMatchingStyle;
		}
		set
		{
			bracketMatchingStyle = value;
		}
	}

	public bool SupportReadOnlySegments
	{
		get
		{
			return supportReadOnlySegments;
		}
		set
		{
			supportReadOnlySegments = value;
		}
	}

	public DefaultTextEditorProperties()
	{
		if (DefaultFont == null)
		{
			DefaultFont = new Font("Courier New", 10f);
		}
		fontContainer = new FontContainer(DefaultFont);
	}
}
