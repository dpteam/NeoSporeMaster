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
}

/// <summary>
/// A Spore animation resource (type <c>0xEE17C6AD</c>), magic <c>0x4D494E41</c> ("ANIM").
/// </summary>
/// <remarks>
/// This reads the resource header, the event table and the channel table (name, keyframe and
/// component counts). Fully decoding the per-component keyframes and interpolators is future work —
/// see <c>docs/ANIM_FORMAT.md</c>. The format is pointer-based: header fields are fixed offsets and
/// the channel/event tables are referenced from the file start.
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

                Channels.Add(ReadChannel(stream, channelOffset));
            }
            catch (Exception ex)
            {
                Diagnostics.Warn($"Animation channel {i} could not be read: {ex.Message}");
            }
        }
    }

    private SpAnimationChannel ReadChannel(Stream stream, long offset)
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
        stream.Seek(offset + 0xDC, SeekOrigin.Begin);
        int componentCount = stream.ReadI32();

        return new SpAnimationChannel
        {
            Name = name,
            MovementFlags = movementFlags,
            PrimaryContextFlags = primaryFlags,
            SecondaryContextFlags = secondaryFlags,
            BindFlags = bindFlags,
            KeyframeCount = keyframeCount,
            ComponentCount = componentCount,
        };
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
