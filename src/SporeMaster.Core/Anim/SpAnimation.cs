using System.Text;
using SporeMaster.Core.Diagnostics;
using SporeMaster.Core.IO;

namespace SporeMaster.Core.Anim;

/// <summary>Thrown when a stream does not look like a Spore animation (<c>.anim</c>).</summary>
public sealed class NotAnimationException : Exception
{
    public NotAnimationException(string message)
        : base(message)
    {
    }
}

/// <summary>One channel of an animation: a named track with keyframes and one or more components.</summary>
public sealed class SpAnimationChannel
{
    public string Name { get; set; } = string.Empty;

    public int MovementFlags { get; set; }

    public int PrimaryContextFlags { get; set; }

    public int SecondaryContextFlags { get; set; }

    public int BindFlags { get; set; }

    /// <summary>Number of keyframes on this channel.</summary>
    public int KeyframeCount { get; set; }

    /// <summary>Number of components (position/rotation/info) in this channel.</summary>
    public int ComponentCount { get; set; }

    /// <summary>The decoded components (each holds its keyframe values).</summary>
    public List<SpAnimationComponent> Components { get; } = new();
}

/// <summary>The kind of an animation component (the low nibble of its flags).</summary>
public enum SpAnimationComponentType
{
    Info = 0,
    Position = 1,
    Rotation = 2,
    Rigblock = 3,
}

/// <summary>The decoded values of one animation keyframe (interpretation depends on the component type).</summary>
public sealed class SpAnimationKeyframe
{
    /// <summary>Primitive values: position XYZ + weight, rotation XYZW + weight, or rigblock value + weight.</summary>
    public float[] Values { get; init; } = Array.Empty<float>();

    /// <summary>Frame time (only meaningful for <see cref="SpAnimationComponentType.Info"/>).</summary>
    public int Time { get; set; }
}

/// <summary>
/// One animation component: a 32-byte descriptor (flags, id, keyframe offset/stride, index) plus the
/// keyframes it points to. The component type is <c>flags &amp; 0xF</c>.
/// </summary>
public sealed class SpAnimationComponent
{
    public int Flags { get; set; }

    public SpAnimationComponentType Type => (SpAnimationComponentType)(Flags & 0xF);

    public int Id { get; set; }

    public int Index { get; set; }

    public int KeyframeStride { get; set; }

    public List<SpAnimationKeyframe> Keyframes { get; } = new();
}

/// <summary>
/// A Spore animation resource (type <c>0xEE17C6AD</c>), magic <c>0x4D494E41</c> ("ANIM").
/// </summary>
/// <remarks>
/// Reads the resource header, the event table, the channel table (name, keyframe/component counts)
/// and the per-component keyframes (position/rotation/info/rigblock). The format is pointer-based:
/// header fields live at fixed offsets and the channel/event/component tables are referenced from the
/// file start. See <c>docs/ANIM_FORMAT.md</c>.
/// </remarks>
public sealed class SpAnimation
{
    /// <summary>Animation magic: <c>'A' 'N' 'I' 'M'</c> read little-endian from <c>41 4E 49 4D</c>.</summary>
    public const uint Magic = 0x4D494E41;

    private const uint ChannelMagic = 0x4E414843; // 'C' 'H' 'A' 'N'

    public const int MinVersion = 0x14;
    public const int MaxVersion = 0x19;

    public uint FileSize { get; set; }

    public int Version { get; set; }

    /// <summary>Resource id (FNV hash of the animation name).</summary>
    public int Id { get; set; }

    /// <summary>Animation length in seconds.</summary>
    public float Length { get; set; }

    public List<SpAnimationChannel> Channels { get; } = new();

    /// <summary>Event names, in order (empty when an event has no name).</summary>
    public List<string> Events { get; } = new();

    public DiagnosticSink Diagnostics { get; } = new();

    public static SpAnimation Open(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Read(stream);
    }

    public static SpAnimation Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var anim = new SpAnimation();
        anim.ReadInternal(stream);
        return anim;
    }

    private void ReadInternal(Stream stream)
    {
        if (!stream.CanSeek)
        {
            throw new NotAnimationException("The animation format requires a seekable stream.");
        }

        long length = stream.Length;
        stream.Seek(0, SeekOrigin.Begin);

        uint magic = stream.ReadU32();
        if (magic != Magic)
        {
            throw new NotAnimationException($"Not a Spore animation (magic 0x{magic:X8}).");
        }

        FileSize = stream.ReadU32();
        Version = stream.ReadI32();

        if (Version < MinVersion)
        {
            Diagnostics.Warn($"Unsupported animation version {Version} (expected >= {MinVersion}).");
        }

        if (length < 0x158)
        {
            throw new NotAnimationException("Animation is too small to hold its header.");
        }

        stream.Seek(0x110, SeekOrigin.Begin);
        Id = stream.ReadI32();

        stream.Seek(0x120, SeekOrigin.Begin);
        Length = stream.ReadF32LE();

        stream.Seek(0x13C, SeekOrigin.Begin);
        int eventCount = stream.ReadI32();
        long eventPtr = stream.ReadU32();
        int channelCount = stream.ReadI32();
        long channelPtr = stream.ReadU32();

        ReadEvents(stream, eventCount, eventPtr, length);
        ReadChannels(stream, channelCount, channelPtr, length);
    }

    private void ReadEvents(Stream stream, int count, long pointer, long fileLength)
    {
        if (count <= 0 || count > 100_000)
        {
            return;
        }

        // Each event record is 0x60 bytes; the name pointer sits at +0x40.
        if (pointer <= 0 || pointer + (long)count * 0x60 > fileLength)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            try
            {
                stream.Seek(pointer + i * 0x60 + 0x40, SeekOrigin.Begin);
                long namePtr = stream.ReadU32();
                if (namePtr > 0 && namePtr < fileLength)
                {
                    Events.Add(ReadCString(stream, namePtr));
                }
                else
                {
                    Events.Add(string.Empty);
                }
            }
            catch (Exception ex)
            {
                Diagnostics.Warn($"Animation event {i} could not be read: {ex.Message}");
                Events.Add(string.Empty);
            }
        }
    }

    private void ReadChannels(Stream stream, int count, long pointer, long fileLength)
    {
        if (count <= 0 || count > 100_000)
        {
            return;
        }

        if (pointer <= 0 || pointer + (long)count * 4 > fileLength)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            try
            {
                stream.Seek(pointer + i * 4L, SeekOrigin.Begin);
                long channelOffset = stream.ReadU32();
                if (channelOffset <= 0 || channelOffset + 0xE4 > fileLength)
                {
                    continue;
                }

                Channels.Add(ReadChannel(stream, channelOffset, fileLength));
            }
            catch (Exception ex)
            {
                Diagnostics.Warn($"Animation channel {i} could not be read: {ex.Message}");
            }
        }
    }

    private SpAnimationChannel ReadChannel(Stream stream, long offset, long fileLength)
    {
        stream.Seek(offset, SeekOrigin.Begin);
        uint magic = stream.ReadU32();
        if (magic != ChannelMagic)
        {
            throw new NotAnimationException($"Bad channel magic 0x{magic:X8}.");
        }

        stream.Seek(offset + 8, SeekOrigin.Begin);
        string name = ReadCString(stream, offset + 8);

        stream.Seek(offset + 0x88, SeekOrigin.Begin);
        int movementFlags = stream.ReadI32();
        int primaryFlags = stream.ReadI32();
        stream.Seek(offset + 0x9C, SeekOrigin.Begin);
        int secondaryFlags = stream.ReadI32();
        stream.Seek(offset + 0xAC, SeekOrigin.Begin);
        int bindFlags = stream.ReadI32();
        stream.Seek(offset + 0xD4, SeekOrigin.Begin);
        int keyframeCount = stream.ReadI32();
        long keyframePtr = stream.ReadU32();
        stream.Seek(offset + 0xDC, SeekOrigin.Begin);
        int componentCount = stream.ReadI32();
        long componentPtr = stream.ReadU32();

        var channel = new SpAnimationChannel
        {
            Name = name,
            MovementFlags = movementFlags,
            PrimaryContextFlags = primaryFlags,
            SecondaryContextFlags = secondaryFlags,
            BindFlags = bindFlags,
            KeyframeCount = keyframeCount,
            ComponentCount = componentCount,
        };

        ReadComponents(stream, channel, componentPtr, keyframePtr, keyframeCount, fileLength);
        return channel;
    }

    /// <summary>Reads the 32-byte component descriptors and their keyframes.</summary>
    private void ReadComponents(
        Stream stream,
        SpAnimationChannel channel,
        long componentPtr,
        long keyframePtr,
        int keyframeCount,
        long fileLength)
    {
        if (componentPtr <= 0 || keyframePtr <= 0 || keyframeCount < 0)
        {
            return;
        }

        for (int i = 0; i < channel.ComponentCount; i++)
        {
            long descriptor = componentPtr + i * 32;
            if (descriptor + 32 > fileLength)
            {
                break;
            }

            try
            {
                stream.Seek(descriptor, SeekOrigin.Begin);
                var component = new SpAnimationComponent
                {
                    Flags = stream.ReadI32(),
                    Id = stream.ReadI32(),
                    Index = 0,
                    KeyframeStride = 0,
                };

                int keyframeOffset = stream.ReadI32();
                component.KeyframeStride = stream.ReadI32();
                component.Index = stream.ReadI32();

                if (component.KeyframeStride != 0)
                {
                    for (int k = 0; k < keyframeCount; k++)
                    {
                        long position = keyframePtr + (long)component.KeyframeStride * k + keyframeOffset;
                        if (position < 0 || position + KeyframePrefixSize(component.Type) > fileLength)
                        {
                            break;
                        }

                        stream.Seek(position, SeekOrigin.Begin);
                        component.Keyframes.Add(ReadKeyframe(stream, component.Type));
                    }
                }

                channel.Components.Add(component);
            }
            catch (Exception ex)
            {
                Diagnostics.Warn($"Animation component {i} could not be read: {ex.Message}");
                break;
            }
        }
    }

    /// <summary>The number of leading bytes that carry a keyframe's primitive values.</summary>
    private static int KeyframePrefixSize(SpAnimationComponentType type) => type switch
    {
        SpAnimationComponentType.Position => 16,   // xyz + weight
        SpAnimationComponentType.Rotation => 20,   // xyzw + weight
        SpAnimationComponentType.Rigblock => 8,    // value + weight
        SpAnimationComponentType.Info => 16,       // time + skip + event index + flags
        _ => 0,
    };

    private static SpAnimationKeyframe ReadKeyframe(Stream stream, SpAnimationComponentType type)
    {
        switch (type)
        {
            case SpAnimationComponentType.Position:
            case SpAnimationComponentType.Rotation:
            case SpAnimationComponentType.Rigblock:
            {
                float[] values = new float[type switch
                {
                    SpAnimationComponentType.Position => 4,
                    SpAnimationComponentType.Rotation => 5,
                    _ => 2,
                }];
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = stream.ReadF32LE();
                }

                return new SpAnimationKeyframe { Values = values };
            }

            case SpAnimationComponentType.Info:
            default:
            {
                int time = stream.ReadI32();
                stream.ReadU32(); // skip
                stream.ReadU16(); // eventStartIndex
                stream.ReadU16(); // padding
                stream.ReadI32(); // flags
                return new SpAnimationKeyframe { Time = time };
            }
        }
    }

    private static string ReadCString(Stream stream, long offset)
    {
        stream.Seek(offset, SeekOrigin.Begin);
        var bytes = new List<byte>(32);
        while (bytes.Count < 4096)
        {
            int b = stream.ReadByte();
            if (b <= 0)
            {
                break;
            }

            bytes.Add((byte)b);
        }

        return Encoding.ASCII.GetString(bytes.ToArray());
    }
}
