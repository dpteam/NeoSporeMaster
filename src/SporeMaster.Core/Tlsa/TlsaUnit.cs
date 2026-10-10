using System.Globalization;
using System.Text;
using SporeMaster.Core.Hashing;
using SporeMaster.Core.IO;
using SporeMaster.Core.Names;

namespace SporeMaster.Core.Tlsa;

/// <summary>One animation reference inside a TLSA choice.</summary>
public sealed class TlsaAnimation
{
    public int Id { get; set; }

    public string Description { get; set; } = string.Empty;

    public float DurationScale { get; set; } = 1.0f;

    public float Duration { get; set; } = -1.0f;
}

/// <summary>A weighted set of animations inside a TLSA group.</summary>
public sealed class TlsaChoice
{
    public float ProbabilityThreshold { get; set; } = 1.0f;

    public List<TlsaAnimation> Animations { get; } = new();
}

/// <summary>A TLSA animation group.</summary>
public sealed class TlsaGroup
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public float PriorityOverride { get; set; }

    public float BlendInTime { get; set; } = -1.0f;

    public bool Idle { get; set; }

    public bool AllowLocomotion { get; set; }

    public bool RandomizeChoicePerLoop { get; set; }

    public int MatchVariantForToolMask { get; set; }

    public int DisableToolOverlayMask { get; set; }

    public int EndMode { get; set; } = 4;

    public List<TlsaChoice> Choices { get; } = new();

    /// <summary>Returns the instance name derived from an animation description (after the last '/').</summary>
    public static string InstanceFromDescription(string description)
    {
        int index = description.LastIndexOf('/');
        return index < 0 ? description : description[(index + 1)..];
    }
}

/// <summary>
/// A Spore TLSA file (<c>.tlsa</c>, type <c>0x4AEB6BC6</c>): animation group tables.
/// </summary>
/// <remarks>
/// Binary layout is big-endian and mirrors SporeModder FX's <c>TLSAUnit</c>; see
/// <c>docs/TLSA_FORMAT.md</c>. The text form is SporeModder's ArgScript (<c>.tlsa_t</c>).
/// </remarks>
public sealed class TlsaUnit
{
    public const int Magic = 0x74736C61; // "tsla" (sic)

    public int Version { get; set; } = 10;

    public List<TlsaGroup> Groups { get; } = new();

    // ---- binary ----

    public static TlsaUnit Read(Stream stream)
    {
        if (stream.ReadI32BE() != Magic)
        {
            throw new InvalidDataException("Not a TLSA file (bad magic).");
        }

        var unit = new TlsaUnit { Version = stream.ReadI32BE() };
        int count = stream.ReadI32BE();
        if (count < 0 || count > 1_000_000)
        {
            throw new InvalidDataException($"Unreasonable TLSA group count {count}.");
        }

        for (int i = 0; i < count; i++)
        {
            var group = new TlsaGroup();
            unit.Groups.Add(group);
            unit.ReadGroup(stream, group);
        }

        return unit;
    }

    private void ReadGroup(Stream stream, TlsaGroup group)
    {
        if (Version <= 8)
        {
            group.Id = stream.ReadI32BE();
            if (Version == 8)
            {
                group.PriorityOverride = stream.ReadF32BE();
            }

            int numAnims = stream.ReadI32BE();
            var choice = new TlsaChoice();
            group.Choices.Add(choice);
            for (int i = 0; i < numAnims; i++)
            {
                var anim = new TlsaAnimation { Description = ReadUtf16(stream) };
                choice.Animations.Add(anim);
            }

            for (int i = 0; i < numAnims; i++)
            {
                choice.Animations[i].Id = stream.ReadI32BE();
            }

            float durationScale = stream.ReadF32BE();
            float duration = stream.ReadF32BE();
            foreach (TlsaAnimation anim in choice.Animations)
            {
                anim.Duration = duration;
                anim.DurationScale = durationScale;
            }

            group.Idle = stream.ReadByte() != 0;
            group.BlendInTime = stream.ReadF32BE();
            group.AllowLocomotion = stream.ReadByte() != 0;
            group.DisableToolOverlayMask = stream.ReadI32BE();
            group.MatchVariantForToolMask = stream.ReadI32BE();
            group.EndMode = stream.ReadI32BE();
            return;
        }

        group.Id = stream.ReadI32BE();
        group.Name = ReadUtf16(stream);
        group.PriorityOverride = stream.ReadF32BE();
        group.BlendInTime = stream.ReadF32BE();
        group.Idle = stream.ReadByte() != 0;
        group.AllowLocomotion = stream.ReadByte() != 0;
        if (Version == 10)
        {
            group.RandomizeChoicePerLoop = stream.ReadByte() != 0;
        }

        group.MatchVariantForToolMask = stream.ReadI32BE();
        group.DisableToolOverlayMask = stream.ReadI32BE();
        group.EndMode = stream.ReadI32BE();

        int choiceCount = stream.ReadI32BE();
        for (int i = 0; i < choiceCount; i++)
        {
            var choice = new TlsaChoice { ProbabilityThreshold = stream.ReadF32BE() };
            group.Choices.Add(choice);
            int animCount = stream.ReadI32BE();
            for (int j = 0; j < animCount; j++)
            {
                var anim = new TlsaAnimation
                {
                    DurationScale = stream.ReadF32BE(),
                    Duration = stream.ReadF32BE(),
                    Id = stream.ReadI32BE(),
                    Description = ReadUtf16(stream),
                };
                choice.Animations.Add(anim);
            }
        }
    }

    public void Write(Stream stream)
    {
        stream.WriteI32BE(Magic);
        stream.WriteI32BE(Version);
        stream.WriteI32BE(Groups.Count);
        foreach (TlsaGroup group in Groups)
        {
            WriteGroup(stream, group);
        }
    }

    private void WriteGroup(Stream stream, TlsaGroup group)
    {
        if (Version <= 8)
        {
            stream.WriteI32BE(group.Id);
            if (Version == 8)
            {
                stream.WriteF32BE(group.PriorityOverride);
            }

            List<TlsaAnimation> anims = group.Choices.Count > 0 ? group.Choices[0].Animations : new List<TlsaAnimation>();
            stream.WriteI32BE(anims.Count);
            foreach (TlsaAnimation anim in anims)
            {
                WriteUtf16(stream, anim.Description);
            }

            foreach (TlsaAnimation anim in anims)
            {
                stream.WriteI32BE(anim.Id);
            }

            stream.WriteF32BE(anims.Count > 0 ? anims[0].DurationScale : 1.0f);
            stream.WriteF32BE(anims.Count > 0 ? anims[0].Duration : -1.0f);
            stream.WriteByte(group.Idle ? (byte)1 : (byte)0);
            stream.WriteF32BE(group.BlendInTime);
            stream.WriteByte(group.AllowLocomotion ? (byte)1 : (byte)0);
            stream.WriteI32BE(group.DisableToolOverlayMask);
            stream.WriteI32BE(group.MatchVariantForToolMask);
            stream.WriteI32BE(group.EndMode);
            return;
        }

        stream.WriteI32BE(group.Id);
        WriteUtf16(stream, group.Name);
        stream.WriteF32BE(group.PriorityOverride);
        stream.WriteF32BE(group.BlendInTime);
        stream.WriteByte(group.Idle ? (byte)1 : (byte)0);
        stream.WriteByte(group.AllowLocomotion ? (byte)1 : (byte)0);
        if (Version == 10)
        {
            stream.WriteByte(group.RandomizeChoicePerLoop ? (byte)1 : (byte)0);
        }

        stream.WriteI32BE(group.MatchVariantForToolMask);
        stream.WriteI32BE(group.DisableToolOverlayMask);
        stream.WriteI32BE(group.EndMode);

        stream.WriteI32BE(group.Choices.Count);
        foreach (TlsaChoice choice in group.Choices)
        {
            stream.WriteF32BE(choice.ProbabilityThreshold);
            stream.WriteI32BE(choice.Animations.Count);
            foreach (TlsaAnimation anim in choice.Animations)
            {
                stream.WriteF32BE(anim.DurationScale);
                stream.WriteF32BE(anim.Duration);
                stream.WriteI32BE(anim.Id);
                WriteUtf16(stream, anim.Description);
            }
        }
    }

    private static string ReadUtf16(Stream stream)
    {
        int length = stream.ReadI32BE();
        if (length <= 0)
        {
            return string.Empty;
        }

        return Encoding.Unicode.GetString(stream.ReadBytesExactly(length * 2));
    }

    private static void WriteUtf16(Stream stream, string value)
    {
        byte[] bytes = Encoding.Unicode.GetBytes(value);
        stream.WriteI32BE(bytes.Length / 2);
        stream.Write(bytes);
    }

    // ---- text ----

    public string ToScript(NameRegistrySet names)
    {
        var builder = new StringBuilder();
        builder.Append("version ").Append(Version).Append('\n');

        foreach (TlsaGroup group in Groups)
        {
            builder.Append('\n');
            builder.Append("anim ").Append(names.GetFileName(unchecked((uint)group.Id)));
            if (!string.IsNullOrEmpty(group.Name))
            {
                builder.Append(" \"").Append(group.Name.Replace("\"", "&quot;")).Append('"');
            }

            builder.Append('\n');
            if (Version == 10 && group.RandomizeChoicePerLoop)
            {
                builder.Append("\trandomizeChoicePerLoop true\n");
            }

            builder.Append("\tendMode ").Append(group.EndMode).Append('\n');
            if (group.Idle)
            {
                builder.Append("\tidle true\n");
            }

            if (group.BlendInTime != -1.0f)
            {
                builder.Append("\tblendInTime ").Append(Format(group.BlendInTime)).Append('\n');
            }

            builder.Append("\tallowLocomotion ").Append(group.AllowLocomotion ? "true" : "false").Append('\n');

            if (group.DisableToolOverlayMask != 0)
            {
                builder.Append("\tdisableToolOverlay").Append(BitList(group.DisableToolOverlayMask)).Append('\n');
            }

            if (group.MatchVariantForToolMask != 0)
            {
                builder.Append("\tmatchVariantForTool").Append(BitList(group.MatchVariantForToolMask)).Append('\n');
            }

            if (Version >= 8)
            {
                builder.Append("\tpriorityOverride ").Append(Format(group.PriorityOverride)).Append('\n');
            }

            float accumulated = 0f;
            foreach (TlsaChoice choice in group.Choices)
            {
                builder.Append("\n\tchoice");
                if (Version > 8)
                {
                    builder.Append(" -probability ").Append(Format(choice.ProbabilityThreshold - accumulated));
                }

                builder.Append('\n');
                foreach (TlsaAnimation anim in choice.Animations)
                {
                    builder.Append("\t\tanimation \"").Append(anim.Description.Replace("\"", "&quot;")).Append('"');
                    if (Fnv.Hash(TlsaGroup.InstanceFromDescription(anim.Description)) != unchecked((uint)anim.Id))
                    {
                        builder.Append(" -instanceID ").Append(names.GetFileName(unchecked((uint)anim.Id)));
                    }

                    if (anim.DurationScale != 1.0f)
                    {
                        builder.Append(" -durationScale ").Append(Format(anim.DurationScale));
                    }

                    if (anim.Duration != -1.0f)
                    {
                        builder.Append(" -duration ").Append(Format(anim.Duration));
                    }

                    builder.Append('\n');
                }

                builder.Append("\tend\n");
                accumulated = choice.ProbabilityThreshold;
            }

            builder.Append("end\n");
        }

        return builder.ToString();
    }

    private static string BitList(int mask)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                sb.Append(' ').Append(i);
            }
        }

        return sb.ToString();
    }

    private static string Format(float value) => value.ToString("0.#######", CultureInfo.InvariantCulture);

    // ---- text parsing ----

    public static TlsaUnit Parse(string text, NameRegistrySet names)
    {
        var unit = new TlsaUnit();
        string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        TlsaGroup? group = null;
        TlsaChoice? choice = null;
        float accumulated = 0f;

        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            List<string> tokens = Tokenize(line);
            string command = tokens[0];

            if (command == "end")
            {
                if (choice is not null)
                {
                    accumulated = choice.ProbabilityThreshold;
                    choice = null;
                }
                else if (group is not null)
                {
                    unit.Groups.Add(group);
                    group = null;
                }

                continue;
            }

            switch (command)
            {
                case "version":
                    unit.Version = int.Parse(tokens[1], CultureInfo.InvariantCulture);
                    break;
                case "anim" when tokens.Count >= 2:
                    group = new TlsaGroup { Id = unchecked((int)names.GetFileHash(tokens[1])) };
                    if (tokens.Count >= 3)
                    {
                        group.Name = tokens[2];
                    }

                    accumulated = 0f;
                    break;
                case "choice":
                    if (group is null)
                    {
                        break;
                    }

                    {
                        var c = new TlsaChoice();
                        int probIndex = tokens.IndexOf("-probability");
                        if (probIndex >= 0 && probIndex + 1 < tokens.Count)
                        {
                            c.ProbabilityThreshold = accumulated + float.Parse(tokens[probIndex + 1], CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            c.ProbabilityThreshold = 1.0f;
                        }

                        group.Choices.Add(c);
                        choice = c;
                    }

                    break;
                case "animation" when choice is not null && tokens.Count >= 2:
                    {
                        var anim = new TlsaAnimation { Description = tokens[1] };
                        int idIndex = tokens.IndexOf("-instanceID");
                        if (idIndex >= 0 && idIndex + 1 < tokens.Count)
                        {
                            anim.Id = unchecked((int)names.GetFileHash(tokens[idIndex + 1]));
                        }
                        else
                        {
                            anim.Id = unchecked((int)Fnv.Hash(TlsaGroup.InstanceFromDescription(anim.Description)));
                        }

                        int scaleIndex = tokens.IndexOf("-durationScale");
                        if (scaleIndex >= 0 && scaleIndex + 1 < tokens.Count)
                        {
                            anim.DurationScale = float.Parse(tokens[scaleIndex + 1], CultureInfo.InvariantCulture);
                        }

                        int durationIndex = tokens.IndexOf("-duration");
                        if (durationIndex >= 0 && durationIndex + 1 < tokens.Count)
                        {
                            anim.Duration = float.Parse(tokens[durationIndex + 1], CultureInfo.InvariantCulture);
                        }

                        choice.Animations.Add(anim);
                    }

                    break;
                default:
                    if (group is not null)
                    {
                        ApplyGroupCommand(group, command, tokens);
                    }

                    break;
            }
        }

        return unit;
    }

    private static void ApplyGroupCommand(TlsaGroup group, string command, List<string> tokens)
    {
        switch (command)
        {
            case "randomizeChoicePerLoop":
                group.RandomizeChoicePerLoop = ParseBool(tokens);
                break;
            case "endMode" when tokens.Count >= 2:
                group.EndMode = int.Parse(tokens[1], CultureInfo.InvariantCulture);
                break;
            case "idle":
                group.Idle = ParseBool(tokens);
                break;
            case "blendInTime" when tokens.Count >= 2:
                group.BlendInTime = float.Parse(tokens[1], CultureInfo.InvariantCulture);
                break;
            case "allowLocomotion":
                group.AllowLocomotion = ParseBool(tokens);
                break;
            case "priorityOverride" when tokens.Count >= 2:
                group.PriorityOverride = float.Parse(tokens[1], CultureInfo.InvariantCulture);
                break;
            case "disableToolOverlay":
                foreach (string token in tokens.Skip(1))
                {
                    if (int.TryParse(token, out int bit) && bit is >= 0 and < 32)
                    {
                        group.DisableToolOverlayMask |= 1 << bit;
                    }
                }

                break;
            case "matchVariantForTool":
                foreach (string token in tokens.Skip(1))
                {
                    if (int.TryParse(token, out int bit) && bit is >= 0 and < 32)
                    {
                        group.MatchVariantForToolMask |= 1 << bit;
                    }
                }

                break;
        }
    }

    private static bool ParseBool(List<string> tokens) =>
        tokens.Count >= 2 && tokens[1].Equals("true", StringComparison.OrdinalIgnoreCase);

    private static List<string> Tokenize(string line)
    {
        var tokens = new List<string>();
        int i = 0;
        while (i < line.Length)
        {
            if (char.IsWhiteSpace(line[i]))
            {
                i++;
                continue;
            }

            if (line[i] == '"')
            {
                int start = ++i;
                var sb = new StringBuilder();
                while (i < line.Length && line[i] != '"')
                {
                    sb.Append(line[i++]);
                }

                i++;
                tokens.Add(sb.ToString().Replace("&quot;", "\""));
                continue;
            }

            int wordStart = i;
            while (i < line.Length && !char.IsWhiteSpace(line[i]))
            {
                i++;
            }

            tokens.Add(line[wordStart..i]);
        }

        return tokens;
    }
}
