using System.Globalization;
using System.Text;
using SporeMaster.Core.IO;
using SporeMaster.Core.Names;

namespace SporeMaster.Core.Cnv;

/// <summary>A localized text reference inside a conversation, with requirement flags.</summary>
public sealed class CnvText
{
    public bool[] RequireFlags { get; } = new bool[154];

    public bool[] ExcludeFlags { get; } = new bool[154];

    public int TableId { get; set; }

    public int InstanceId { get; set; }

    public int Emotion { get; set; }

    public static CnvText Read(Stream s)
    {
        var text = new CnvText();
        ReadBools(s, text.RequireFlags);
        ReadBools(s, text.ExcludeFlags);
        text.InstanceId = s.ReadI32BE();
        text.TableId = s.ReadI32BE();
        text.Emotion = s.ReadI32BE();
        return text;
    }

    public void Write(Stream s)
    {
        WriteBools(s, RequireFlags);
        WriteBools(s, ExcludeFlags);
        s.WriteI32BE(InstanceId);
        s.WriteI32BE(TableId);
        s.WriteI32BE(Emotion);
    }

    internal static void ReadBools(Stream s, bool[] flags)
    {
        for (int i = 0; i < flags.Length; i++)
        {
            flags[i] = s.ReadU8() != 0;
        }
    }

    internal static void WriteBools(Stream s, bool[] flags)
    {
        foreach (bool flag in flags)
        {
            s.WriteByte(flag ? (byte)1 : (byte)0);
        }
    }
}

/// <summary>One animation variation of a conversation animation.</summary>
public sealed class CnvAnimationVariation
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<int> IdleAnimIds { get; } = new();

    public List<int> ActionAnimIds { get; } = new();
}

/// <summary>A conversation animation (a set of variations).</summary>
public sealed class CnvAnimation
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<CnvAnimationVariation> Variations { get; } = new();
}

/// <summary>A response option of a conversation dialog.</summary>
public sealed class CnvDialogResponse
{
    public bool[] RequireFlags { get; } = new bool[154];

    public bool[] ExcludeFlags { get; } = new bool[154];

    public bool[] EnableRequireFlags { get; } = new bool[154];

    public bool[] EnableExcludeFlags { get; } = new bool[154];

    public int Action { get; set; }

    public int GroupId { get; set; }

    public int TypeId { get; set; }

    public int InstanceId { get; set; }

    public List<CnvText> Texts { get; } = new();

    public List<int> Ints1 { get; } = new();

    public List<int> Ints2 { get; } = new();

    public List<int> Ints3 { get; } = new();

    public List<int> Ints4 { get; } = new();
}

/// <summary>A conversation dialog node.</summary>
public sealed class CnvDialog
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool Value1 { get; set; }

    public bool ShowAccept { get; set; }

    public bool ShowDecline { get; set; }

    public bool Value4 { get; set; }

    public bool ShowStatic { get; set; }

    public int Action { get; set; } = -1;

    public int GroupId { get; set; }

    public int TypeId { get; set; }

    public int InstanceId { get; set; }

    public List<CnvText> Texts { get; } = new();

    public List<CnvDialogResponse> Responses { get; } = new();
}

/// <summary>An event-log entry.</summary>
public sealed class CnvEventLog
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<CnvText> Texts { get; } = new();
}

/// <summary>An undecoded conversation record (mission/badge data).</summary>
public sealed class CnvUnknown
{
    public int MissionId { get; set; }

    public int Value2 { get; set; }

    public int Value3 { get; set; }

    public List<int> Badges1 { get; } = new();

    public List<int> Ints2 { get; } = new();
}

/// <summary>
/// A Spore conversation file (<c>.cnv</c>, type <c>0x055ADA24</c>): dialog trees, animations and
/// event logs.
/// </summary>
/// <remarks>
/// The format mixes endianness (SporeModder's <c>readInt</c> is big-endian, <c>readLEInt</c> is
/// little-endian): counts and actions are little-endian, while string lengths, resource keys and
/// text ids are big-endian. See <c>docs/CNV_FORMAT.md</c>. Binary reading/writing is byte-exact;
/// a text dump is provided (full text import is future work).
/// </remarks>
public sealed class CnvUnit
{
    public List<CnvAnimation> Animations { get; } = new();

    public List<CnvDialog> Dialogs { get; } = new();

    public List<CnvEventLog> EventLogs { get; } = new();

    public List<CnvUnknown> Unknowns { get; } = new();

    public static CnvUnit Read(Stream s)
    {
        var unit = new CnvUnit();

        int count = s.ReadI32();
        for (int i = 0; i < count; i++)
        {
            unit.Animations.Add(ReadAnimation(s));
        }

        count = s.ReadI32();
        for (int i = 0; i < count; i++)
        {
            unit.Dialogs.Add(ReadDialog(s));
        }

        count = s.ReadI32();
        for (int i = 0; i < count; i++)
        {
            unit.EventLogs.Add(ReadEventLog(s));
        }

        count = s.ReadI32();
        for (int i = 0; i < count; i++)
        {
            unit.Unknowns.Add(ReadUnknown(s));
        }

        return unit;
    }

    public void Write(Stream s)
    {
        s.WriteI32(Animations.Count);
        foreach (CnvAnimation anim in Animations)
        {
            WriteAnimation(s, anim);
        }

        s.WriteI32(Dialogs.Count);
        foreach (CnvDialog dialog in Dialogs)
        {
            WriteDialog(s, dialog);
        }

        s.WriteI32(EventLogs.Count);
        foreach (CnvEventLog log in EventLogs)
        {
            WriteEventLog(s, log);
        }

        s.WriteI32(Unknowns.Count);
        foreach (CnvUnknown unk in Unknowns)
        {
            WriteUnknown(s, unk);
        }
    }

    private static string ReadString(Stream s)
    {
        int length = s.ReadI32BE();
        return length <= 0 ? string.Empty : Encoding.ASCII.GetString(s.ReadBytesExactly(length));
    }

    private static void WriteString(Stream s, string value)
    {
        s.WriteI32BE(value.Length);
        s.Write(Encoding.ASCII.GetBytes(value));
    }

    private static List<int> ReadIntList(Stream s, bool bigEndian)
    {
        int count = s.ReadI32();
        var list = new List<int>(Math.Max(0, count));
        for (int i = 0; i < count; i++)
        {
            list.Add(bigEndian ? s.ReadI32BE() : s.ReadI32());
        }

        return list;
    }

    private static void WriteIntList(Stream s, List<int> list, bool bigEndian)
    {
        s.WriteI32(list.Count);
        foreach (int value in list)
        {
            if (bigEndian)
            {
                s.WriteI32BE(value);
            }
            else
            {
                s.WriteI32(value);
            }
        }
    }

    private static CnvAnimation ReadAnimation(Stream s)
    {
        var anim = new CnvAnimation { Id = s.ReadI32(), Name = ReadString(s) };
        int count = s.ReadI32();
        for (int i = 0; i < count; i++)
        {
            var variation = new CnvAnimationVariation { Id = s.ReadI32(), Name = ReadString(s) };
            variation.IdleAnimIds.AddRange(ReadIntList(s, bigEndian: true));
            variation.ActionAnimIds.AddRange(ReadIntList(s, bigEndian: true));
            anim.Variations.Add(variation);
        }

        return anim;
    }

    private static void WriteAnimation(Stream s, CnvAnimation anim)
    {
        s.WriteI32(anim.Id);
        WriteString(s, anim.Name);
        s.WriteI32(anim.Variations.Count);
        foreach (CnvAnimationVariation v in anim.Variations)
        {
            s.WriteI32(v.Id);
            WriteString(s, v.Name);
            WriteIntList(s, v.IdleAnimIds, bigEndian: true);
            WriteIntList(s, v.ActionAnimIds, bigEndian: true);
        }
    }

    private static CnvDialog ReadDialog(Stream s)
    {
        var dialog = new CnvDialog
        {
            Id = s.ReadI32(),
            Name = ReadString(s),
            Value1 = s.ReadU8() != 0,
            ShowAccept = s.ReadU8() != 0,
            ShowDecline = s.ReadU8() != 0,
            Value4 = s.ReadU8() != 0,
            ShowStatic = s.ReadU8() != 0,
            Action = s.ReadI32(),
            GroupId = s.ReadI32BE(),
            TypeId = s.ReadI32BE(),
            InstanceId = s.ReadI32BE(),
        };

        int count = s.ReadI32();
        for (int i = 0; i < count; i++)
        {
            dialog.Texts.Add(CnvText.Read(s));
        }

        count = s.ReadI32();
        for (int i = 0; i < count; i++)
        {
            var response = new CnvDialogResponse();
            CnvText.ReadBools(s, response.RequireFlags);
            CnvText.ReadBools(s, response.ExcludeFlags);
            CnvText.ReadBools(s, response.EnableRequireFlags);
            CnvText.ReadBools(s, response.EnableExcludeFlags);
            response.Action = s.ReadI32();
            response.GroupId = s.ReadI32BE();
            response.TypeId = s.ReadI32BE();
            response.InstanceId = s.ReadI32BE();

            int textCount = s.ReadI32();
            for (int j = 0; j < textCount; j++)
            {
                response.Texts.Add(CnvText.Read(s));
            }

            response.Ints1.AddRange(ReadIntList(s, bigEndian: true));
            response.Ints2.AddRange(ReadIntList(s, bigEndian: true));
            response.Ints3.AddRange(ReadIntList(s, bigEndian: true));
            response.Ints4.AddRange(ReadIntList(s, bigEndian: true));
            dialog.Responses.Add(response);
        }

        return dialog;
    }

    private static void WriteDialog(Stream s, CnvDialog dialog)
    {
        s.WriteI32(dialog.Id);
        WriteString(s, dialog.Name);
        s.WriteByte(dialog.Value1 ? (byte)1 : (byte)0);
        s.WriteByte(dialog.ShowAccept ? (byte)1 : (byte)0);
        s.WriteByte(dialog.ShowDecline ? (byte)1 : (byte)0);
        s.WriteByte(dialog.Value4 ? (byte)1 : (byte)0);
        s.WriteByte(dialog.ShowStatic ? (byte)1 : (byte)0);
        s.WriteI32(dialog.Action);
        s.WriteI32BE(dialog.GroupId);
        s.WriteI32BE(dialog.TypeId);
        s.WriteI32BE(dialog.InstanceId);

        s.WriteI32(dialog.Texts.Count);
        foreach (CnvText text in dialog.Texts)
        {
            text.Write(s);
        }

        s.WriteI32(dialog.Responses.Count);
        foreach (CnvDialogResponse r in dialog.Responses)
        {
            CnvText.WriteBools(s, r.RequireFlags);
            CnvText.WriteBools(s, r.ExcludeFlags);
            CnvText.WriteBools(s, r.EnableRequireFlags);
            CnvText.WriteBools(s, r.EnableExcludeFlags);
            s.WriteI32(r.Action);
            s.WriteI32BE(r.GroupId);
            s.WriteI32BE(r.TypeId);
            s.WriteI32BE(r.InstanceId);

            s.WriteI32(r.Texts.Count);
            foreach (CnvText text in r.Texts)
            {
                text.Write(s);
            }

            WriteIntList(s, r.Ints1, bigEndian: true);
            WriteIntList(s, r.Ints2, bigEndian: true);
            WriteIntList(s, r.Ints3, bigEndian: true);
            WriteIntList(s, r.Ints4, bigEndian: true);
        }
    }

    private static CnvEventLog ReadEventLog(Stream s)
    {
        var log = new CnvEventLog { Id = s.ReadI32(), Name = ReadString(s) };
        int count = s.ReadI32();
        for (int i = 0; i < count; i++)
        {
            log.Texts.Add(CnvText.Read(s));
        }

        return log;
    }

    private static void WriteEventLog(Stream s, CnvEventLog log)
    {
        s.WriteI32(log.Id);
        WriteString(s, log.Name);
        s.WriteI32(log.Texts.Count);
        foreach (CnvText text in log.Texts)
        {
            text.Write(s);
        }
    }

    private static CnvUnknown ReadUnknown(Stream s)
    {
        var unk = new CnvUnknown { MissionId = s.ReadI32(), Value2 = s.ReadI32(), Value3 = s.ReadI32() };
        unk.Badges1.AddRange(ReadIntList(s, bigEndian: false));
        unk.Ints2.AddRange(ReadIntList(s, bigEndian: false));
        return unk;
    }

    private static void WriteUnknown(Stream s, CnvUnknown unk)
    {
        s.WriteI32(unk.MissionId);
        s.WriteI32(unk.Value2);
        s.WriteI32(unk.Value3);
        WriteIntList(s, unk.Badges1, bigEndian: false);
        WriteIntList(s, unk.Ints2, bigEndian: false);
    }

    // ---- text dump ----

    public string ToScript(NameRegistrySet names)
    {
        var builder = new StringBuilder();

        foreach (CnvAnimation anim in Animations)
        {
            builder.Append("animation ").Append(ArgScriptName(names, anim.Id, anim.Name)).Append('\n');
            foreach (CnvAnimationVariation v in anim.Variations)
            {
                builder.Append("\tvariation ").Append(ArgScriptName(names, v.Id, v.Name)).Append('\n');
                builder.Append("\t\tidle");
                foreach (int id in v.IdleAnimIds)
                {
                    builder.Append(' ').Append(names.GetFileName(unchecked((uint)id)));
                }

                builder.Append('\n');
                builder.Append("\t\taction");
                foreach (int id in v.ActionAnimIds)
                {
                    builder.Append(' ').Append(names.GetFileName(unchecked((uint)id)));
                }

                builder.Append('\n');
                builder.Append("\tend\n");
            }

            builder.Append("end\n");
        }

        foreach (CnvDialog dialog in Dialogs)
        {
            builder.Append("\ndialog ").Append(ArgScriptName(names, dialog.Id, dialog.Name)).Append('\n');
            if (dialog.Value1)
            {
                builder.Append("\tvalue1 true\n");
            }

            if (dialog.ShowAccept)
            {
                builder.Append("\tshowAccept true\n");
            }

            if (dialog.ShowDecline)
            {
                builder.Append("\tshowDecline true\n");
            }

            if (dialog.Value4)
            {
                builder.Append("\tvalue4 true\n");
            }

            if (dialog.ShowStatic)
            {
                builder.Append("\tshowStatic true\n");
            }

            if (dialog.Action != -1)
            {
                builder.Append("\taction ").Append(names.GetFileName(unchecked((uint)dialog.Action))).Append('\n');
            }

            AppendKeyRef(builder, names, dialog.GroupId, dialog.TypeId, dialog.InstanceId);

            foreach (CnvText text in dialog.Texts)
            {
                AppendText(builder, names, text);
            }

            foreach (CnvDialogResponse response in dialog.Responses)
            {
                builder.Append("\tresponse\n");
                builder.Append("\t\taction ").Append(names.GetFileName(unchecked((uint)response.Action))).Append('\n');
                AppendKeyRef(builder, names, response.GroupId, response.TypeId, response.InstanceId, "\t\t");
                foreach (CnvText text in response.Texts)
                {
                    AppendText(builder, names, text, "\t\t");
                }

                AppendFlags(builder, "require", response.RequireFlags);
                AppendFlags(builder, "exclude", response.ExcludeFlags);
                AppendFlags(builder, "enableRequire", response.EnableRequireFlags);
                AppendFlags(builder, "enableExclude", response.EnableExcludeFlags);
                AppendInts(builder, names, "ints1", response.Ints1, "\t\t");
                AppendInts(builder, names, "ints2", response.Ints2, "\t\t");
                AppendInts(builder, names, "ints3", response.Ints3, "\t\t");
                AppendInts(builder, names, "ints4", response.Ints4, "\t\t");
                builder.Append("\tend\n");
            }

            builder.Append("end\n");
        }

        foreach (CnvEventLog log in EventLogs)
        {
            builder.Append("\neventLog ").Append(ArgScriptName(names, log.Id, log.Name)).Append('\n');
            foreach (CnvText text in log.Texts)
            {
                AppendText(builder, names, text, "\t");
            }

            builder.Append("end\n");
        }

        foreach (CnvUnknown unk in Unknowns)
        {
            builder.Append("\nunknown\n");
            builder.Append("\tmissionID ").Append(names.GetFileName(unchecked((uint)unk.MissionId))).Append('\n');
            if (unk.Value2 != 0)
            {
                builder.Append("\tvalue2 ").Append(names.GetFileName(unchecked((uint)unk.Value2))).Append('\n');
            }

            if (unk.Value3 != 0)
            {
                builder.Append("\tvalue3 ").Append(names.GetFileName(unchecked((uint)unk.Value3))).Append('\n');
            }

            AppendInts(builder, names, "badges1", unk.Badges1, "\t");
            AppendInts(builder, names, "ints2", unk.Ints2, "\t");
            builder.Append("end\n");
        }

        return builder.ToString();
    }

    private static void AppendText(StringBuilder builder, NameRegistrySet names, CnvText text, string indent = "\t")
    {
        builder.Append(indent).Append("text ")
            .Append(names.GetFileName(unchecked((uint)text.TableId))).Append('!')
            .Append(names.GetFileName(unchecked((uint)text.InstanceId)));
        if (text.Emotion != 0)
        {
            builder.Append(" -emotion ").Append(names.GetFileName(unchecked((uint)text.Emotion)));
        }

        builder.Append('\n');
        AppendFlags(builder, "require", text.RequireFlags, indent);
        AppendFlags(builder, "exclude", text.ExcludeFlags, indent);
    }

    private static void AppendFlags(StringBuilder builder, string command, bool[] flags, string indent = "\t")
    {
        var sb = new StringBuilder();
        for (int i = 0; i < flags.Length; i++)
        {
            if (flags[i])
            {
                sb.Append(' ').Append(i);
            }
        }

        if (sb.Length > 0)
        {
            builder.Append(indent).Append(command).Append(sb).Append('\n');
        }
    }

    private static void AppendKeyRef(
        StringBuilder builder,
        NameRegistrySet names,
        int groupId,
        int typeId,
        int instanceId,
        string indent = "\t")
    {
        if (groupId != 0)
        {
            builder.Append(indent).Append("group ").Append(names.GetFileName(unchecked((uint)groupId))).Append('\n');
        }

        if (typeId != 0)
        {
            builder.Append(indent).Append("type ").Append(names.GetFileName(unchecked((uint)typeId))).Append('\n');
        }

        if (instanceId != 0)
        {
            builder.Append(indent).Append("instance ").Append(names.GetFileName(unchecked((uint)instanceId))).Append('\n');
        }
    }

    private static void AppendInts(StringBuilder builder, NameRegistrySet names, string command, List<int> values, string indent)
    {
        if (values.Count == 0)
        {
            return;
        }

        builder.Append(indent).Append(command);
        foreach (int value in values)
        {
            builder.Append(' ').Append(names.GetFileName(unchecked((uint)value)));
        }

        builder.Append('\n');
    }

    private static string ArgScriptName(NameRegistrySet names, int id, string name)
    {
        uint hash = Hashing.Fnv.Hash(name);
        return hash == unchecked((uint)id) ? name : $"{name} {names.GetFileName(unchecked((uint)id))}";
    }
}
