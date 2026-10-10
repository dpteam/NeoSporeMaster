using System.Globalization;
using System.Text;
using SporeMaster.Core.IO;

namespace SporeMaster.Core.Gait;

/// <summary>Spline parameters attached to a gait path key.</summary>
public sealed class GaitSplineParams
{
    public float S0 { get; set; }

    public float S1 { get; set; }

    public sbyte TanFl0 { get; set; }

    public sbyte TanFl1 { get; set; }

    public short HerFl { get; set; }

    /// <summary>A trailing word (often uninitialised memory); preserved verbatim.</summary>
    public uint Trailing { get; set; }

    public static GaitSplineParams Read(Stream s)
    {
        return new GaitSplineParams
        {
            S0 = s.ReadF32LE(),
            S1 = s.ReadF32LE(),
            TanFl0 = (sbyte)s.ReadU8(),
            TanFl1 = (sbyte)s.ReadU8(),
            HerFl = s.ReadI16(),
            Trailing = s.ReadU32(),
        };
    }

    public void Write(Stream s)
    {
        s.WriteF32LE(S0);
        s.WriteF32LE(S1);
        s.WriteByte((byte)TanFl0);
        s.WriteByte((byte)TanFl1);
        s.WriteI16(HerFl);
        s.WriteU32(Trailing);
    }
}

/// <summary>A position key inside a gait path.</summary>
public sealed class GaitPathPosKey
{
    public uint Vftable { get; set; } = 0xCDCDCDCD;

    public int Tick { get; set; }

    public float[] Pos { get; set; } = new float[3];

    public GaitSplineParams[] Spl { get; } = CreateSpline(3);

    internal static GaitSplineParams[] CreateSpline(int count)
    {
        var array = new GaitSplineParams[count];
        for (int i = 0; i < count; i++)
        {
            array[i] = new GaitSplineParams();
        }

        return array;
    }
}

/// <summary>A rotation key inside a gait path.</summary>
public sealed class GaitPathRotKey
{
    public uint Vftable { get; set; } = 0xCDCDCDCD;

    public int Tick { get; set; }

    public float[] Rot { get; set; } = new float[4];

    public GaitSplineParams[] Spl { get; } = GaitPathPosKey.CreateSpline(4);
}

/// <summary>A blend/bound key inside a gait path.</summary>
public sealed class GaitPathBndKey
{
    public uint Vftable { get; set; } = 0xCDCDCDCD;

    public int Tick { get; set; }

    public float Bnd { get; set; }

    public GaitSplineParams Spl { get; set; } = new();
}

/// <summary>The keyed path data of a gait group.</summary>
public sealed class GaitPathData
{
    public List<GaitPathPosKey> PosKeys { get; } = new();

    public List<GaitPathRotKey> RotKeys { get; } = new();

    public List<GaitPathBndKey> BndKeys { get; } = new();

    public static GaitPathData Read(Stream s)
    {
        var data = new GaitPathData();
        int numPos = s.ReadI32();
        int numRot = s.ReadI32();
        int numBnd = s.ReadI32();

        for (int i = 0; i < numPos; i++)
        {
            var key = new GaitPathPosKey();
            key.Vftable = s.ReadU32();
            key.Tick = s.ReadI32();
            for (int c = 0; c < 3; c++)
            {
                key.Pos[c] = s.ReadF32LE();
            }

            for (int j = 0; j < 3; j++)
            {
                key.Spl[j] = GaitSplineParams.Read(s);
            }

            data.PosKeys.Add(key);
        }

        for (int i = 0; i < numRot; i++)
        {
            var key = new GaitPathRotKey();
            key.Vftable = s.ReadU32();
            key.Tick = s.ReadI32();
            for (int c = 0; c < 4; c++)
            {
                key.Rot[c] = s.ReadF32LE();
            }

            for (int j = 0; j < 4; j++)
            {
                key.Spl[j] = GaitSplineParams.Read(s);
            }

            data.RotKeys.Add(key);
        }

        for (int i = 0; i < numBnd; i++)
        {
            var key = new GaitPathBndKey
            {
                Vftable = s.ReadU32(),
                Tick = s.ReadI32(),
                Bnd = s.ReadF32LE(),
                Spl = GaitSplineParams.Read(s),
            };
            data.BndKeys.Add(key);
        }

        return data;
    }

    public void Write(Stream s)
    {
        s.WriteI32(PosKeys.Count);
        s.WriteI32(RotKeys.Count);
        s.WriteI32(BndKeys.Count);

        foreach (GaitPathPosKey key in PosKeys)
        {
            s.WriteU32(key.Vftable);
            s.WriteI32(key.Tick);
            foreach (float v in key.Pos)
            {
                s.WriteF32LE(v);
            }

            foreach (GaitSplineParams p in key.Spl)
            {
                p.Write(s);
            }
        }

        foreach (GaitPathRotKey key in RotKeys)
        {
            s.WriteU32(key.Vftable);
            s.WriteI32(key.Tick);
            foreach (float v in key.Rot)
            {
                s.WriteF32LE(v);
            }

            foreach (GaitSplineParams p in key.Spl)
            {
                p.Write(s);
            }
        }

        foreach (GaitPathBndKey key in BndKeys)
        {
            s.WriteU32(key.Vftable);
            s.WriteI32(key.Tick);
            s.WriteF32LE(key.Bnd);
            key.Spl.Write(s);
        }
    }
}

/// <summary>A gait group (one locomotion style with its path data).</summary>
public sealed class GaitGroupKey
{
    public int GroupIndex { get; set; }

    public float Speedi { get; set; }

    public float StepHeight { get; set; }

    public int Taxon { get; set; }

    public float StepGallop { get; set; }

    public float StepPhaseBiasH { get; set; }

    public float StepPhaseBiasV { get; set; }

    public float StepSkew { get; set; }

    public float FootTilt { get; set; }

    public float TiltPoint { get; set; }

    public float ToeCurlMax { get; set; }

    public float ToeCurlBegin { get; set; }

    public float ToeCurlEnd { get; set; }

    public float SwayAmplitude { get; set; }

    public float SwayPhase { get; set; }

    public float TrackWidthReduction { get; set; }

    public float VerticalPhaseOffset { get; set; }

    public float SagittalPhaseOffset { get; set; }

    public float LateralPhaseOffset { get; set; }

    public float MaxVerticalOffset { get; set; }

    public float VerticalDist { get; set; }

    public float WalkRunShape { get; set; }

    public float MaxSagittalOffset { get; set; }

    public float MaxLateralOffset { get; set; }

    public float YawPhaseOffset { get; set; }

    public float PitchPhaseOffset { get; set; }

    public float RollPhaseOffset { get; set; }

    public float MaxYawAngle { get; set; }

    public float MaxPitchAngle { get; set; }

    public float MaxRollAngle { get; set; }

    public List<float> Triggers { get; } = new();

    public List<float> DutyFactors { get; } = new();

    public List<GaitPathData> PathData { get; } = new();

    public static GaitGroupKey Read(Stream s)
    {
        var key = new GaitGroupKey
        {
            Speedi = s.ReadF32LE(),
            StepHeight = s.ReadF32LE(),
            Taxon = s.ReadI32(),
            StepGallop = s.ReadF32LE(),
            StepPhaseBiasH = s.ReadF32LE(),
            StepPhaseBiasV = s.ReadF32LE(),
            StepSkew = s.ReadF32LE(),
            FootTilt = s.ReadF32LE(),
            TiltPoint = s.ReadF32LE(),
            ToeCurlMax = s.ReadF32LE(),
            ToeCurlBegin = s.ReadF32LE(),
            ToeCurlEnd = s.ReadF32LE(),
            SwayAmplitude = s.ReadF32LE(),
            SwayPhase = s.ReadF32LE(),
            TrackWidthReduction = s.ReadF32LE(),
            VerticalPhaseOffset = s.ReadF32LE(),
            SagittalPhaseOffset = s.ReadF32LE(),
            LateralPhaseOffset = s.ReadF32LE(),
            MaxVerticalOffset = s.ReadF32LE(),
            VerticalDist = s.ReadF32LE(),
            WalkRunShape = s.ReadF32LE(),
            MaxSagittalOffset = s.ReadF32LE(),
            MaxLateralOffset = s.ReadF32LE(),
            YawPhaseOffset = s.ReadF32LE(),
            PitchPhaseOffset = s.ReadF32LE(),
            RollPhaseOffset = s.ReadF32LE(),
            MaxYawAngle = s.ReadF32LE(),
            MaxPitchAngle = s.ReadF32LE(),
            MaxRollAngle = s.ReadF32LE(),
        };

        int taxonCount = s.ReadI32();
        int pathCount = s.ReadI32();
        for (int i = 0; i < taxonCount; i++)
        {
            key.Triggers.Add(s.ReadF32LE());
            key.DutyFactors.Add(s.ReadF32LE());
        }

        for (int i = 0; i < pathCount; i++)
        {
            key.PathData.Add(GaitPathData.Read(s));
        }

        return key;
    }

    public void Write(Stream s)
    {
        s.WriteF32LE(Speedi);
        s.WriteF32LE(StepHeight);
        s.WriteI32(Taxon);
        s.WriteF32LE(StepGallop);
        s.WriteF32LE(StepPhaseBiasH);
        s.WriteF32LE(StepPhaseBiasV);
        s.WriteF32LE(StepSkew);
        s.WriteF32LE(FootTilt);
        s.WriteF32LE(TiltPoint);
        s.WriteF32LE(ToeCurlMax);
        s.WriteF32LE(ToeCurlBegin);
        s.WriteF32LE(ToeCurlEnd);
        s.WriteF32LE(SwayAmplitude);
        s.WriteF32LE(SwayPhase);
        s.WriteF32LE(TrackWidthReduction);
        s.WriteF32LE(VerticalPhaseOffset);
        s.WriteF32LE(SagittalPhaseOffset);
        s.WriteF32LE(LateralPhaseOffset);
        s.WriteF32LE(MaxVerticalOffset);
        s.WriteF32LE(VerticalDist);
        s.WriteF32LE(WalkRunShape);
        s.WriteF32LE(MaxSagittalOffset);
        s.WriteF32LE(MaxLateralOffset);
        s.WriteF32LE(YawPhaseOffset);
        s.WriteF32LE(PitchPhaseOffset);
        s.WriteF32LE(RollPhaseOffset);
        s.WriteF32LE(MaxYawAngle);
        s.WriteF32LE(MaxPitchAngle);
        s.WriteF32LE(MaxRollAngle);

        s.WriteI32(Triggers.Count);
        s.WriteI32(PathData.Count);
        for (int i = 0; i < Triggers.Count; i++)
        {
            s.WriteF32LE(Triggers[i]);
            s.WriteF32LE(DutyFactors[i]);
        }

        foreach (GaitPathData path in PathData)
        {
            path.Write(s);
        }
    }
}

/// <summary>A step-length key of a gait file.</summary>
public sealed class GaitStepLengthKey
{
    public float Speedi { get; set; }

    public float StepLength { get; set; }
}

/// <summary>
/// A Spore gait file (<c>.gait</c>, type <c>0x25DF0112</c>): locomotion parameters for creatures.
/// </summary>
/// <remarks>All numbers are little-endian; see <c>docs/GAIT_FORMAT.md</c>.</remarks>
public sealed class GaitFile
{
    public const int Magic = 0x54494147; // 'GAIT'

    public const int Version = 11;

    public int HeaderVersion { get; set; } = Version;

    public List<GaitStepLengthKey> StepLengthKeys { get; } = new();

    public List<GaitGroupKey> GaitGroupKeys { get; } = new();

    public static GaitFile Read(Stream s)
    {
        if (s.ReadI32() != Magic)
        {
            throw new InvalidDataException("Not a GAIT file (bad magic).");
        }

        int fileSize = s.ReadI32();
        int version = s.ReadI32();
        if (version != Version)
        {
            throw new InvalidDataException($"Unsupported GAIT version {version}.");
        }

        if (s.ReadI32() != 1)
        {
            throw new InvalidDataException("GAIT: expected constant 1 after version.");
        }

        var file = new GaitFile { HeaderVersion = version };
        int stepCount = s.ReadI32();
        int groupCount = s.ReadI32();

        for (int i = 0; i < stepCount; i++)
        {
            file.StepLengthKeys.Add(new GaitStepLengthKey { Speedi = s.ReadF32LE(), StepLength = s.ReadF32LE() });
        }

        for (int i = 0; i < groupCount; i++)
        {
            file.GaitGroupKeys.Add(GaitGroupKey.Read(s));
        }

        _ = fileSize;
        return file;
    }

    public void Write(Stream s)
    {
        s.WriteI32(Magic);
        long sizePosition = s.Position;
        s.WriteI32(0); // patch later
        s.WriteI32(HeaderVersion);
        s.WriteI32(1);

        s.WriteI32(StepLengthKeys.Count);
        s.WriteI32(GaitGroupKeys.Count);

        foreach (GaitStepLengthKey key in StepLengthKeys)
        {
            s.WriteF32LE(key.Speedi);
            s.WriteF32LE(key.StepLength);
        }

        foreach (GaitGroupKey key in GaitGroupKeys)
        {
            key.Write(s);
        }

        long end = s.Position;
        s.Position = sizePosition;
        s.WriteI32((int)end);
        s.Position = end;
    }

    // ---- text ----

    public string ToScript()
    {
        var builder = new StringBuilder();
        builder.Append("version ").Append(Version).Append('\n');

        foreach (GaitStepLengthKey key in StepLengthKeys)
        {
            builder.Append("\nstepLengthKey\n");
            builder.Append("\tspeedi ").Append(Format(key.Speedi)).Append('\n');
            builder.Append("\tstepLength ").Append(Format(key.StepLength)).Append('\n');
            builder.Append("end\n");
        }

        foreach (GaitGroupKey key in GaitGroupKeys)
        {
            builder.Append("\ngaitGroupKey\n");
            builder.Append("\tgroupIndex ").Append(key.GroupIndex).Append('\n');
            AppendFloat(builder, "speedi", key.Speedi);
            AppendFloat(builder, "stepHeight", key.StepHeight);
            AppendFloat(builder, "stepGallop", key.StepGallop);
            AppendFloat(builder, "stepPhaseBiasH", key.StepPhaseBiasH);
            AppendFloat(builder, "stepPhaseBiasV", key.StepPhaseBiasV);
            AppendFloat(builder, "stepSkew", key.StepSkew);
            AppendFloat(builder, "footTilt", key.FootTilt);
            AppendFloat(builder, "tiltPoint", key.TiltPoint);
            AppendFloat(builder, "toeCurlMax", key.ToeCurlMax);
            AppendFloat(builder, "toeCurlBegin", key.ToeCurlBegin);
            AppendFloat(builder, "toeCurlEnd", key.ToeCurlEnd);
            AppendFloat(builder, "swayAmplitude", key.SwayAmplitude);
            AppendFloat(builder, "swayPhase", key.SwayPhase);
            AppendFloat(builder, "trackWidthReduction", key.TrackWidthReduction);
            AppendFloat(builder, "verticalPhaseOffset", key.VerticalPhaseOffset);
            AppendFloat(builder, "sagittalPhaseOffset", key.SagittalPhaseOffset);
            AppendFloat(builder, "lateralPhaseOffset", key.LateralPhaseOffset);
            AppendFloat(builder, "maxVerticalOffset", key.MaxVerticalOffset);
            AppendFloat(builder, "verticalDist", key.VerticalDist);
            AppendFloat(builder, "walkRunShape", key.WalkRunShape);
            AppendFloat(builder, "maxSagittalOffset", key.MaxSagittalOffset);
            AppendFloat(builder, "maxLateralOffset", key.MaxLateralOffset);
            AppendFloat(builder, "yawPhaseOffset", key.YawPhaseOffset);
            AppendFloat(builder, "pitchPhaseOffset", key.PitchPhaseOffset);
            AppendFloat(builder, "rollPhaseOffset", key.RollPhaseOffset);
            AppendFloat(builder, "maxYawAngle", key.MaxYawAngle);
            AppendFloat(builder, "maxPitchAngle", key.MaxPitchAngle);
            AppendFloat(builder, "maxRollAngle", key.MaxRollAngle);

            builder.Append("\n\ttaxon ").Append(key.Taxon).Append('\n');
            for (int i = 0; i < key.Triggers.Count; i++)
            {
                AppendFloat(builder, "trigger", key.Triggers[i]);
                AppendFloat(builder, "dutyFactor", key.DutyFactors.Count > i ? key.DutyFactors[i] : 0f);
            }

            foreach (GaitPathData path in key.PathData)
            {
                AppendPath(builder, path);
            }

            builder.Append("end\n");
        }

        return builder.ToString();
    }

    private static void AppendPath(StringBuilder builder, GaitPathData path)
    {
        builder.Append("\n\tpathData\n");

        foreach (GaitPathPosKey key in path.PosKeys)
        {
            builder.Append("\n\t\ttick ").Append(key.Tick).Append('\n');
            builder.Append("\t\tpos (").Append(string.Join(", ", key.Pos.Select(Format))).Append(")\n");
            AppendSpline(builder, key.Spl);
        }

        foreach (GaitPathRotKey key in path.RotKeys)
        {
            builder.Append("\n\t\ttick ").Append(key.Tick).Append('\n');
            builder.Append("\t\trot (").Append(string.Join(", ", key.Rot.Select(Format))).Append(")\n");
            AppendSpline(builder, key.Spl);
        }

        foreach (GaitPathBndKey key in path.BndKeys)
        {
            builder.Append("\n\t\ttick ").Append(key.Tick).Append('\n');
            builder.Append("\t\tbnd ").Append(Format(key.Bnd)).Append('\n');
            AppendSpline(builder, new[] { key.Spl });
        }

        builder.Append("\tend\n");
    }

    private static void AppendSpline(StringBuilder builder, GaitSplineParams[] spl)
    {
        builder.Append("\t\tspl");
        foreach (GaitSplineParams p in spl)
        {
            builder.Append(' ').Append(Format(p.S0)).Append(' ').Append(Format(p.S1));
        }

        builder.Append('\n');

        builder.Append("\t\ttanFl");
        foreach (GaitSplineParams p in spl)
        {
            builder.Append(' ').Append(p.TanFl0).Append(' ').Append(p.TanFl1);
        }

        builder.Append('\n');

        builder.Append("\t\therFl");
        foreach (GaitSplineParams p in spl)
        {
            builder.Append(' ').Append(p.HerFl);
        }

        builder.Append('\n');
    }

    private static void AppendFloat(StringBuilder builder, string command, float value) =>
        builder.Append('\t').Append(command).Append(' ').Append(Format(value)).Append('\n');

    private static string Format(float value) => value.ToString("0.#######", CultureInfo.InvariantCulture);

    // ---- parsing ----

    public static GaitFile Parse(string text)
    {
        var file = new GaitFile();
        string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        GaitStepLengthKey? step = null;
        GaitGroupKey? group = null;
        GaitPathData? path = null;
        object? lastKey = null; // current pos/rot/bnd key for spl commands
        int lastTick = -1;
        bool inPath = false;

        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            List<string> t = Tokenize(line);
            string command = t[0];

            if (command == "end")
            {
                if (inPath)
                {
                    inPath = false;
                    path = null;
                }
                else if (step is not null)
                {
                    file.StepLengthKeys.Add(step);
                    step = null;
                }
                else if (group is not null)
                {
                    file.GaitGroupKeys.Add(group);
                    group = null;
                }

                continue;
            }

            switch (command)
            {
                case "version":
                    file.HeaderVersion = int.Parse(t[1], CultureInfo.InvariantCulture);
                    break;
                case "stepLengthKey":
                    step = new GaitStepLengthKey();
                    break;
                case "speedi" when step is not null:
                    step.Speedi = ParseFloat(t[1]);
                    break;
                case "stepLength" when step is not null:
                    step.StepLength = ParseFloat(t[1]);
                    break;
                case "gaitGroupKey":
                    group = new GaitGroupKey();
                    break;
                case "pathData" when group is not null:
                    path = new GaitPathData();
                    group.PathData.Add(path);
                    inPath = true;
                    break;
                case "tick":
                    lastTick = int.Parse(t[1], CultureInfo.InvariantCulture);
                    lastKey = null;
                    break;
                case "pos" when path is not null:
                    lastKey = new GaitPathPosKey { Tick = lastTick, Pos = ParseVector(t, 3) };
                    path.PosKeys.Add((GaitPathPosKey)lastKey);
                    break;
                case "rot" when path is not null:
                    lastKey = new GaitPathRotKey { Tick = lastTick, Rot = ParseVector(t, 4) };
                    path.RotKeys.Add((GaitPathRotKey)lastKey);
                    break;
                case "bnd" when path is not null:
                    lastKey = new GaitPathBndKey { Tick = lastTick, Bnd = ParseFloat(t[1]) };
                    path.BndKeys.Add((GaitPathBndKey)lastKey);
                    break;
                case "spl":
                    ApplySpline(lastKey, t, 2, (p, i, v) =>
                    {
                        if (i == 0)
                        {
                            p.S0 = v;
                        }
                        else
                        {
                            p.S1 = v;
                        }
                    });
                    break;
                case "tanFl":
                    ApplySpline(lastKey, t, 2, (p, i, v) =>
                    {
                        if (i == 0)
                        {
                            p.TanFl0 = (sbyte)v;
                        }
                        else
                        {
                            p.TanFl1 = (sbyte)v;
                        }
                    });
                    break;
                case "herFl":
                    ApplySpline(lastKey, t, 1, (p, _, v) => p.HerFl = (short)v);
                    break;
                default:
                    if (group is not null)
                    {
                        ApplyGroupCommand(group, command, t);
                    }

                    break;
            }
        }

        return file;
    }

    private static void ApplySpline(object? key, List<string> tokens, int valuesPerSpline, Action<GaitSplineParams, int, float> setter)
    {
        GaitSplineParams[]? spl = key switch
        {
            GaitPathPosKey p => p.Spl,
            GaitPathRotKey r => r.Spl,
            GaitPathBndKey b => new[] { b.Spl },
            _ => null,
        };

        if (spl is null)
        {
            return;
        }

        int index = 1;
        foreach (GaitSplineParams p in spl)
        {
            for (int i = 0; i < valuesPerSpline && index < tokens.Count; i++, index++)
            {
                setter(p, i, ParseFloat(tokens[index]));
            }
        }
    }

    private static void ApplyGroupCommand(GaitGroupKey group, string command, List<string> t)
    {
        if (t.Count < 2)
        {
            return;
        }

        float value = ParseFloat(t[1]);
        switch (command)
        {
            case "groupIndex": group.GroupIndex = (int)value; break;
            case "taxon": group.Taxon = (int)value; break;
            case "trigger": group.Triggers.Add(value); break;
            case "dutyFactor": group.DutyFactors.Add(value); break;
            case "speedi": group.Speedi = value; break;
            case "stepHeight": group.StepHeight = value; break;
            case "stepGallop": group.StepGallop = value; break;
            case "stepPhaseBiasH": group.StepPhaseBiasH = value; break;
            case "stepPhaseBiasV": group.StepPhaseBiasV = value; break;
            case "stepSkew": group.StepSkew = value; break;
            case "footTilt": group.FootTilt = value; break;
            case "tiltPoint": group.TiltPoint = value; break;
            case "toeCurlMax": group.ToeCurlMax = value; break;
            case "toeCurlBegin": group.ToeCurlBegin = value; break;
            case "toeCurlEnd": group.ToeCurlEnd = value; break;
            case "swayAmplitude": group.SwayAmplitude = value; break;
            case "swayPhase": group.SwayPhase = value; break;
            case "trackWidthReduction": group.TrackWidthReduction = value; break;
            case "verticalPhaseOffset": group.VerticalPhaseOffset = value; break;
            case "sagittalPhaseOffset": group.SagittalPhaseOffset = value; break;
            case "lateralPhaseOffset": group.LateralPhaseOffset = value; break;
            case "maxVerticalOffset": group.MaxVerticalOffset = value; break;
            case "verticalDist": group.VerticalDist = value; break;
            case "walkRunShape": group.WalkRunShape = value; break;
            case "maxSagittalOffset": group.MaxSagittalOffset = value; break;
            case "maxLateralOffset": group.MaxLateralOffset = value; break;
            case "yawPhaseOffset": group.YawPhaseOffset = value; break;
            case "pitchPhaseOffset": group.PitchPhaseOffset = value; break;
            case "rollPhaseOffset": group.RollPhaseOffset = value; break;
            case "maxYawAngle": group.MaxYawAngle = value; break;
            case "maxPitchAngle": group.MaxPitchAngle = value; break;
            case "maxRollAngle": group.MaxRollAngle = value; break;
        }
    }

    private static float ParseFloat(string token)
    {
        token = token.Trim('(', ')', '"');
        return float.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private static float[] ParseVector(List<string> tokens, int count)
    {
        var values = new List<float>();
        foreach (string token in tokens.Skip(1))
        {
            string cleaned = token.Trim('(', ')');
            foreach (string part in cleaned.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                values.Add(ParseFloat(part));
            }
        }

        var result = new float[count];
        Array.Copy(values.ToArray(), result, Math.Min(count, values.Count));
        return result;
    }

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

            int start = i;
            while (i < line.Length && !char.IsWhiteSpace(line[i]))
            {
                i++;
            }

            tokens.Add(line[start..i]);
        }

        return tokens;
    }
}
