using System;
using System.Text;

namespace ICSharpCode.TextEditor.Document;

public class GapTextBufferStrategy : ITextBufferStrategy
{
	private const int minGapLength = 128;

	private const int maxGapLength = 2048;

	private char[] buffer = new char[0];

	private int gapBeginOffset;

	private int gapEndOffset;

	private int gapLength;

	public int Length => buffer.Length - gapLength;

	public void SetContent(string text)
	{
		if (text == null)
		{
			text = string.Empty;
		}
		buffer = text.ToCharArray();
		gapBeginOffset = (gapEndOffset = (gapLength = 0));
	}

	public char GetCharAt(int offset)
	{
		if (offset < 0 || offset >= Length)
		{
			throw new ArgumentOutOfRangeException("offset", offset, "0 <= offset < " + Length);
		}
		if (offset >= gapBeginOffset)
		{
			return buffer[offset + gapLength];
		}
		return buffer[offset];
	}

	public string GetText(int offset, int length)
	{
		if (offset < 0 || offset > Length)
		{
			throw new ArgumentOutOfRangeException("offset", offset, "0 <= offset <= " + Length);
		}
		if (length < 0 || offset + length > Length)
		{
			throw new ArgumentOutOfRangeException("length", length, "0 <= length, offset(" + offset + ")+length <= " + Length.ToString());
		}
		int num = offset + length;
		if (num < gapBeginOffset)
		{
			return new string(buffer, offset, length);
		}
		if (offset > gapBeginOffset)
		{
			return new string(buffer, offset + gapLength, length);
		}
		int num2 = gapBeginOffset - offset;
		int num3 = num - gapBeginOffset;
		StringBuilder stringBuilder = new StringBuilder(num2 + num3);
		stringBuilder.Append(buffer, offset, num2);
		stringBuilder.Append(buffer, gapEndOffset, num3);
		return stringBuilder.ToString();
	}

	public void Insert(int offset, string text)
	{
		Replace(offset, 0, text);
	}

	public void Remove(int offset, int length)
	{
		Replace(offset, length, string.Empty);
	}

	public void Replace(int offset, int length, string text)
	{
		if (text == null)
		{
			text = string.Empty;
		}
		if (offset < 0 || offset > Length)
		{
			throw new ArgumentOutOfRangeException("offset", offset, "0 <= offset <= " + Length);
		}
		if (length < 0 || offset + length > Length)
		{
			throw new ArgumentOutOfRangeException("length", length, "0 <= length, offset+length <= " + Length);
		}
		PlaceGap(offset, text.Length - length);
		gapEndOffset += length;
		text.CopyTo(0, buffer, gapBeginOffset, text.Length);
		gapBeginOffset += text.Length;
		gapLength = gapEndOffset - gapBeginOffset;
		if (gapLength > 2048)
		{
			MakeNewBuffer(gapBeginOffset, 128);
		}
	}

	private void PlaceGap(int newGapOffset, int minRequiredGapLength)
	{
		if (gapLength < minRequiredGapLength)
		{
			MakeNewBuffer(newGapOffset, minRequiredGapLength);
			return;
		}
		while (newGapOffset < gapBeginOffset)
		{
			buffer[--gapEndOffset] = buffer[--gapBeginOffset];
		}
		while (newGapOffset > gapBeginOffset)
		{
			buffer[gapBeginOffset++] = buffer[gapEndOffset++];
		}
	}

	private void MakeNewBuffer(int newGapOffset, int newGapLength)
	{
		if (newGapLength < 128)
		{
			newGapLength = 128;
		}
		char[] array = new char[Length + newGapLength];
		if (newGapOffset < gapBeginOffset)
		{
			Array.Copy(buffer, 0, array, 0, newGapOffset);
			Array.Copy(buffer, newGapOffset, array, newGapOffset + newGapLength, gapBeginOffset - newGapOffset);
			Array.Copy(buffer, gapEndOffset, array, array.Length - (buffer.Length - gapEndOffset), buffer.Length - gapEndOffset);
		}
		else
		{
			Array.Copy(buffer, 0, array, 0, gapBeginOffset);
			Array.Copy(buffer, gapEndOffset, array, gapBeginOffset, newGapOffset - gapBeginOffset);
			int num = array.Length - (newGapOffset + newGapLength);
			Array.Copy(buffer, buffer.Length - num, array, newGapOffset + newGapLength, num);
		}
		gapBeginOffset = newGapOffset;
		gapEndOffset = newGapOffset + newGapLength;
		gapLength = newGapLength;
		buffer = array;
	}
}
