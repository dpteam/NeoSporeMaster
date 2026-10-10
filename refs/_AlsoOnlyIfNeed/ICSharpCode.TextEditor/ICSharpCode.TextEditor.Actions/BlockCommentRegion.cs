namespace ICSharpCode.TextEditor.Actions;

public class BlockCommentRegion
{
	private string commentStart = string.Empty;

	private string commentEnd = string.Empty;

	private int startOffset = -1;

	private int endOffset = -1;

	public string CommentStart => commentStart;

	public string CommentEnd => commentEnd;

	public int StartOffset => startOffset;

	public int EndOffset => endOffset;

	public BlockCommentRegion(string commentStart, string commentEnd, int startOffset, int endOffset)
	{
		this.commentStart = commentStart;
		this.commentEnd = commentEnd;
		this.startOffset = startOffset;
		this.endOffset = endOffset;
	}

	public override int GetHashCode()
	{
		int num = 0;
		if (commentStart != null)
		{
			num += 1000000007 * commentStart.GetHashCode();
		}
		if (commentEnd != null)
		{
			num += 1000000009 * commentEnd.GetHashCode();
		}
		num += 1000000021 * startOffset.GetHashCode();
		return num + 1000000033 * endOffset.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (!(obj is BlockCommentRegion blockCommentRegion))
		{
			return false;
		}
		if (commentStart == blockCommentRegion.commentStart && commentEnd == blockCommentRegion.commentEnd && startOffset == blockCommentRegion.startOffset)
		{
			return endOffset == blockCommentRegion.endOffset;
		}
		return false;
	}
}
