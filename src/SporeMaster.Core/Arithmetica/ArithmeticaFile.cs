using System.Globalization;
using System.Text;
using SporeMaster.Core.IO;

namespace SporeMaster.Core.Arithmetica;

/// <summary>One instruction of an <c>.arth</c> (HTRA) program: a tiny stack-machine opcode.</summary>
public sealed class ArithmeticaOperation
{
    public int Line { get; set; }

    public float Value { get; set; }

    public int IndexValue { get; set; } = -1;

    public int OpCode { get; set; }
}

/// <summary>One function of an <c>.arth</c> program: its name (string index) and first operation index.</summary>
public sealed class ArithmeticaFunction
{
    public int StringIndex { get; set; }

    public int FirstOpIndex { get; set; }
}

/// <summary>
/// A Spore Arithmetica program (type <c>0x0472329B</c>, extension <c>.htra</c>/<c>.arth</c>), as
/// read/written by SporeModder FX's <c>ArithmeticaFile</c>. Header fields are a mix of big- and
/// little-endian; the layout round-trips byte-for-byte.
/// </summary>
public sealed class ArithmeticaFile
{
    public const int Magic = 0x48545241; // "HTRA"

    private static readonly string[] OpCodeNames =
    {
        "add", "subtract", "multiply", "divide", "pow", "push", "pushVariable", "popIntoVariable",
        "popIntoVariable2", "nop", "random", "greater", "less", "greaterEqual", "lessEqual", "equal",
        "notEqual", "beginIf", "else", "endIf", "and", "or", "goto", "call", "return", "floor", "ceil",
        "sin", "cos", "tan", "asin", "acos", "atan",
    };

    public List<ArithmeticaOperation> Operations { get; } = new();

    public List<ArithmeticaFunction> Functions { get; } = new();

    public List<string> Strings { get; } = new();

    /// <summary>Bytes after the strings (alignment padding), kept so writing reproduces the file exactly.</summary>
    public byte[] Trailing { get; set; } = Array.Empty<byte>();

    public static ArithmeticaFile Read(Stream s)
    {
        var file = new ArithmeticaFile();

        int numDwords = s.ReadI32BE();
        int magic = s.ReadI32BE();
        if (magic != Magic)
        {
            throw new InvalidDataException($"Wrong HTRA magic 0x{magic:X8}.");
        }

        int version = s.ReadI32();
        if (version != 1)
        {
            throw new InvalidDataException($"Unsupported HTRA version {version}.");
        }

        int field8 = s.ReadI32();
        if (field8 != 2)
        {
            throw new InvalidDataException($"Unexpected HTRA field_8 {field8}.");
        }

        int fieldC = s.ReadI32();
        if (fieldC != numDwords)
        {
            throw new InvalidDataException("Inconsistent HTRA dword count.");
        }

        int count = s.ReadI32();
        for (int i = 0; i < count; i++)
        {
            file.Operations.Add(new ArithmeticaOperation
            {
                Line = s.ReadI32(),
                Value = s.ReadF32LE(),
                IndexValue = s.ReadI32(),
                OpCode = s.ReadI32(),
            });
        }

        int functionCount = s.ReadI32();
        for (int i = 0; i < functionCount; i++)
        {
            file.Functions.Add(new ArithmeticaFunction
            {
                StringIndex = s.ReadI32(),
                FirstOpIndex = s.ReadI32(),
            });
        }

        int stringCount = s.ReadI32();
        int stringCount2 = s.ReadI32();
        if (stringCount != stringCount2)
        {
            throw new InvalidDataException("Inconsistent HTRA string count.");
        }

        for (int i = 0; i < stringCount; i++)
        {
            file.Strings.Add(ReadCString(s));
        }

        long remaining = s.Length - s.Position;
        if (remaining > 0)
        {
            file.Trailing = s.ReadBytesExactly((int)remaining);
        }

        return file;
    }

    public static ArithmeticaFile Open(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Read(stream);
    }

    public void Write(Stream s)
    {
        int stringsBytes = 0;
        foreach (string text in Strings)
        {
            stringsBytes += Encoding.ASCII.GetByteCount(text) + 1;
        }

        int bodyLength =
            (4 + (Operations.Count * 16)) +
            (4 + (Functions.Count * 8)) +
            4 + 4 + stringsBytes +
            Trailing.Length;
        int total = 20 + bodyLength;
        int numDwords = (total - 4) / 4;

        s.WriteI32BE(numDwords);
        s.WriteI32BE(Magic);
        s.WriteI32(1);
        s.WriteI32(2);
        s.WriteI32(numDwords);

        s.WriteI32(Operations.Count);
        foreach (ArithmeticaOperation operation in Operations)
        {
            s.WriteI32(operation.Line);
            s.WriteF32LE(operation.Value);
            s.WriteI32(operation.IndexValue);
            s.WriteI32(operation.OpCode);
        }

        s.WriteI32(Functions.Count);
        foreach (ArithmeticaFunction function in Functions)
        {
            s.WriteI32(function.StringIndex);
            s.WriteI32(function.FirstOpIndex);
        }

        s.WriteI32(Strings.Count);
        s.WriteI32(Strings.Count);
        foreach (string text in Strings)
        {
            WriteCString(s, text);
        }

        if (Trailing.Length > 0)
        {
            s.Write(Trailing);
        }
    }

    public void WriteFile(string path)
    {
        using FileStream stream = File.Create(path);
        Write(stream);
    }

    /// <summary>Human-readable text dump (not a round-trip format).</summary>
    public string ToText()
    {
        var sb = new StringBuilder();
        sb.Append("# Spore Arithmetica (HTRA) dump by SporeMaster 3\n");
        sb.Append($"operations {Operations.Count}\n");
        sb.Append($"functions {Functions.Count}\n");
        sb.Append($"strings {Strings.Count}\n\n");

        sb.Append("## functions\n");
        foreach (ArithmeticaFunction function in Functions)
        {
            string name = function.StringIndex >= 0 && function.StringIndex < Strings.Count
                ? Strings[function.StringIndex]
                : $"#{function.StringIndex}";
            sb.Append($"  {name} -> op {function.FirstOpIndex}\n");
        }

        sb.Append("\n## strings\n");
        for (int i = 0; i < Strings.Count; i++)
        {
            sb.Append($"  [{i}] {Strings[i]}\n");
        }

        sb.Append("\n## operations\n");
        int lastLine = int.MinValue;
        for (int i = 0; i < Operations.Count; i++)
        {
            ArithmeticaOperation op = Operations[i];
            if (op.Line != lastLine)
            {
                sb.Append($"\n# line {op.Line}\n");
                lastLine = op.Line;
            }

            string name = op.OpCode >= 0 && op.OpCode < OpCodeNames.Length
                ? OpCodeNames[op.OpCode]
                : $"op{op.OpCode}";
            sb.Append($"  [{i}] {name} value={op.Value.ToString("0.######", CultureInfo.InvariantCulture)}");
            if (op.IndexValue != -1)
            {
                string arg = op.IndexValue >= 0 && op.IndexValue < Strings.Count
                    ? Strings[op.IndexValue]
                    : $"#{op.IndexValue}";
                sb.Append($" ({arg})");
            }

            sb.Append('\n');
        }

        return sb.ToString();
    }

    private static string ReadCString(Stream s)
    {
        var bytes = new List<byte>();
        int b;
        while ((b = s.ReadByte()) != -1 && b != 0)
        {
            bytes.Add((byte)b);
        }

        if (b == -1 && bytes.Count == 0)
        {
            throw new InvalidDataException("HTRA string is not null-terminated.");
        }

        return Encoding.ASCII.GetString(bytes.ToArray());
    }

    private static void WriteCString(Stream s, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        s.Write(bytes);
        s.WriteByte(0);
    }
}
