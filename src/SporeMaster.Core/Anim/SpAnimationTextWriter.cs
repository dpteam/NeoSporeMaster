using System.Globalization;
using System.Text;

namespace SporeMaster.Core.Anim;

/// <summary>
/// Writes a <see cref="SpAnimation"/> to a human-readable text form (<c>.anim_t</c>). This is a
/// research/inspection dump (channels, keyframes, interpolators and events), not a byte-exact
/// round-trip format.
/// </summary>
public static class SpAnimationTextWriter
{
    public static string Write(SpAnimation animation)
    {
        ArgumentNullException.ThrowIfNull(animation);

        var sb = new StringBuilder();
        sb.Append("# Spore animation (text dump by SporeMaster 3)\n");
        sb.Append($"version {animation.Version}\n");
        sb.Append($"id 0x{animation.Id:X8}\n");
        sb.Append($"length {F(animation.Length)}\n");

        sb.Append($"events {animation.Events.Count}\n");
        foreach (string name in animation.Events)
        {
            sb.Append($"  event \"{name}\"\n");
        }

        sb.Append($"channels {animation.Channels.Count}\n");
        foreach (SpAnimationChannel channel in animation.Channels)
        {
            sb.Append("channel");
            sb.Append($" \"{channel.Name}\"");
            sb.Append($" keys {channel.KeyframeCount}");
            sb.Append($" components {channel.Components.Count}");
            sb.Append($" movement 0x{channel.MovementFlags:X}");
            sb.Append($" bind 0x{channel.BindFlags:X}\n");

            foreach (SpAnimationComponent component in channel.Components)
            {
                sb.Append($"  component {component.Type.ToString().ToLowerInvariant()}");
                sb.Append($" flags 0x{component.Flags:X}");
                sb.Append($" id 0x{component.Id:X}");
                sb.Append($" index {component.Index}");
                sb.Append($" keyframes {component.Keyframes.Count}\n");

                foreach (SpAnimationKeyframe keyframe in component.Keyframes)
                {
                    sb.Append("    ");
                    if (component.Type == SpAnimationComponentType.Info)
                    {
                        sb.Append($"time {keyframe.Time}");
                    }
                    else
                    {
                        sb.Append("values ");
                        sb.Append(string.Join(' ', keyframe.Values.Select(F)));
                    }

                    if (keyframe.Interpolators.Count > 0)
                    {
                        sb.Append("  interp ");
                        sb.Append(string.Join(' ', keyframe.Interpolators.Select(FormatInterpolator)));
                    }

                    sb.Append('\n');
                }
            }
        }

        return sb.ToString();
    }

    public static void WriteFile(SpAnimation animation, string path) =>
        File.WriteAllText(path, Write(animation), new UTF8Encoding(false));

    private static string FormatInterpolator(SpAnimationInterpolator interpolator) =>
        $"[{interpolator.PreviousMode}:{F(interpolator.PreviousFactor)}->{interpolator.NextMode}:{F(interpolator.NextFactor)}]";

    private static string F(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);
}
