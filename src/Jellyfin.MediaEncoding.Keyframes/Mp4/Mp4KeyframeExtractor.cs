using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

namespace Jellyfin.MediaEncoding.Keyframes.Mp4;

/// <summary>
/// The keyframe extractor for the ISO base media file format (mp4, m4v, mov).
/// </summary>
/// <remarks>
/// The keyframes are read from the sample tables in the movie header, so the media data itself is never read.
/// </remarks>
public static class Mp4KeyframeExtractor
{
    // Sample tables larger than this are not read. A 24 hour video at 120 fps needs about 83 MiB.
    private const long MaxTableSize = 128 * 1024 * 1024;

    private const uint BoxMoov = 0x6D6F6F76; // moov
    private const uint BoxMvhd = 0x6D766864; // mvhd
    private const uint BoxMvex = 0x6D766578; // mvex
    private const uint BoxTrak = 0x7472616B; // trak
    private const uint BoxEdts = 0x65647473; // edts
    private const uint BoxElst = 0x656C7374; // elst
    private const uint BoxMdia = 0x6D646961; // mdia
    private const uint BoxMdhd = 0x6D646864; // mdhd
    private const uint BoxHdlr = 0x68646C72; // hdlr
    private const uint BoxMinf = 0x6D696E66; // minf
    private const uint BoxStbl = 0x7374626C; // stbl
    private const uint BoxStts = 0x73747473; // stts
    private const uint BoxCtts = 0x63747473; // ctts
    private const uint BoxStss = 0x73747373; // stss
    private const uint BoxStps = 0x73747073; // stps
    private const uint BoxSbgp = 0x73626770; // sbgp
    private const uint HandlerVideo = 0x76696465; // vide
    private const uint GroupingRap = 0x72617020; // "rap "
    private const uint GroupingSync = 0x73796E63; // sync

    /// <summary>
    /// Extracts the keyframes in ticks from the sample tables of the first video track.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>
    /// An instance of <see cref="KeyframeData"/>.
    /// The list of keyframes is empty if the file uses a layout this extractor does not handle.
    /// </returns>
    public static KeyframeData GetKeyframeData(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return GetKeyframeData(stream);
    }

    /// <summary>
    /// Extracts the keyframes in ticks from the sample tables of the first video track.
    /// </summary>
    /// <param name="stream">A seekable stream containing the file.</param>
    /// <returns>
    /// An instance of <see cref="KeyframeData"/>.
    /// The list of keyframes is empty if the file uses a layout this extractor does not handle.
    /// </returns>
    public static KeyframeData GetKeyframeData(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var empty = new KeyframeData(0, Array.Empty<long>());

        if (!TryFindBox(stream, 0, stream.Length, BoxMoov, out var moov))
        {
            return empty;
        }

        // Fragmented files keep their samples in movie fragments instead of the movie header.
        if (TryFindBox(stream, moov.PayloadStart, moov.End, BoxMvex, out _))
        {
            return empty;
        }

        if (!TryFindBox(stream, moov.PayloadStart, moov.End, BoxMvhd, out var mvhd)
            || !TryReadTimescaleAndDuration(stream, mvhd, out var movieTimescale, out var movieDuration))
        {
            return empty;
        }

        var position = moov.PayloadStart;
        while (TryFindBox(stream, position, moov.End, BoxTrak, out var trak))
        {
            position = trak.End;

            if (TryReadVideoTrack(stream, trak, movieTimescale, out var mediaDurationTicks, out var keyframes, out var isUnsupported))
            {
                var totalDuration = movieDuration > 0 ? ScaleToTicks(movieDuration, movieTimescale) : mediaDurationTicks;
                return new KeyframeData(totalDuration, keyframes);
            }

            if (isUnsupported)
            {
                return empty;
            }
        }

        return empty;
    }

    private static bool TryReadVideoTrack(
        Stream stream,
        Box trak,
        long movieTimescale,
        out long mediaDurationTicks,
        out List<long> keyframes,
        out bool isUnsupported)
    {
        mediaDurationTicks = 0;
        keyframes = [];
        isUnsupported = false;

        if (!TryFindBox(stream, trak.PayloadStart, trak.End, BoxMdia, out var mdia)
            || !TryFindBox(stream, mdia.PayloadStart, mdia.End, BoxHdlr, out var hdlr)
            || hdlr.End - hdlr.PayloadStart < 12)
        {
            return false;
        }

        Span<byte> buffer = stackalloc byte[12];
        stream.Position = hdlr.PayloadStart;
        stream.ReadExactly(buffer);
        if (BinaryPrimitives.ReadUInt32BigEndian(buffer[8..]) != HandlerVideo)
        {
            return false;
        }

        if (!TryFindBox(stream, mdia.PayloadStart, mdia.End, BoxMdhd, out var mdhd)
            || !TryReadTimescaleAndDuration(stream, mdhd, out var mediaTimescale, out var mediaDuration)
            || !TryFindBox(stream, mdia.PayloadStart, mdia.End, BoxMinf, out var minf)
            || !TryFindBox(stream, minf.PayloadStart, minf.End, BoxStbl, out var stbl)
            || !TryFindBox(stream, stbl.PayloadStart, stbl.End, BoxStts, out var sttsBox)
            || !TryReadTable(stream, sttsBox, 8, out var stts, out var sttsCount))
        {
            return false;
        }

        long sampleCount = 0;
        for (var i = 0; i < sttsCount; i++)
        {
            sampleCount += BinaryPrimitives.ReadUInt32BigEndian(stts.AsSpan(i * 8));
        }

        // Cover art is stored as a video track with a single sample.
        if (sampleCount <= 1)
        {
            return false;
        }

        // From here on this is the track ffmpeg is going to pick, so a layout that is not handled must not fall through to another track.
        isUnsupported = true;

        // Partial sync samples and random access point groups add keyframes that are not listed in the sync sample table.
        if (TryFindBox(stream, stbl.PayloadStart, stbl.End, BoxStps, out _) || HasRandomAccessGroups(stream, stbl))
        {
            return false;
        }

        if (!TryReadEditList(stream, trak, out var mediaTime, out var emptyDuration, out var hasMediaEdit))
        {
            return false;
        }

        byte[] ctts = [];
        var cttsCount = 0;
        if (TryFindBox(stream, stbl.PayloadStart, stbl.End, BoxCtts, out var cttsBox)
            && !TryReadTable(stream, cttsBox, 8, out ctts, out cttsCount))
        {
            return false;
        }

        byte[] stss = [];
        var stssCount = 0;
        var hasSyncTable = TryFindBox(stream, stbl.PayloadStart, stbl.End, BoxStss, out var stssBox);
        if (hasSyncTable && !TryReadTable(stream, stssBox, 4, out stss, out stssCount))
        {
            return false;
        }

        // With an edit list ffmpeg moves the first presented frame to the start of the edit,
        // even if the edit starts between two frames.
        if (hasMediaEdit && !TryFindFirstPresentedTime(stts, sttsCount, ctts, cttsCount, mediaTime, out mediaTime))
        {
            return false;
        }

        var startOffsetTicks = ScaleToTicks(emptyDuration, movieTimescale);
        var keyframeCount = hasSyncTable ? stssCount : sampleCount;

        var sttsIndex = 0;
        long sttsRunStart = 0;
        long sttsRunDts = 0;
        var cttsIndex = 0;
        long cttsRunStart = 0;
        long previousSample = -1;

        for (long i = 0; i < keyframeCount; i++)
        {
            // Sample numbers in the sync sample table are 1-based. Without a table every sample is a sync sample.
            var sample = hasSyncTable ? BinaryPrimitives.ReadUInt32BigEndian(stss.AsSpan((int)i * 4)) - 1L : i;
            if (sample <= previousSample)
            {
                return false;
            }

            previousSample = sample;

            while (sttsIndex < sttsCount)
            {
                var runLength = BinaryPrimitives.ReadUInt32BigEndian(stts.AsSpan(sttsIndex * 8));
                if (sample < sttsRunStart + runLength)
                {
                    break;
                }

                sttsRunDts += runLength * (long)BinaryPrimitives.ReadUInt32BigEndian(stts.AsSpan((sttsIndex * 8) + 4));
                sttsRunStart += runLength;
                sttsIndex++;
            }

            if (sttsIndex == sttsCount)
            {
                // The sync sample table refers to a sample that does not exist.
                break;
            }

            var dts = sttsRunDts + ((sample - sttsRunStart) * BinaryPrimitives.ReadUInt32BigEndian(stts.AsSpan((sttsIndex * 8) + 4)));

            while (cttsIndex < cttsCount)
            {
                var runLength = BinaryPrimitives.ReadUInt32BigEndian(ctts.AsSpan(cttsIndex * 8));
                if (sample < cttsRunStart + runLength)
                {
                    break;
                }

                cttsRunStart += runLength;
                cttsIndex++;
            }

            // Version 0 offsets are unsigned by specification, but writers store negative values in them, so they are read as signed like ffmpeg does.
            long compositionOffset = cttsIndex < cttsCount
                ? BinaryPrimitives.ReadInt32BigEndian(ctts.AsSpan((cttsIndex * 8) + 4))
                : 0;

            var ticks = ScaleToTicks(dts + compositionOffset - mediaTime, mediaTimescale) + startOffsetTicks;
            if (ticks >= 0)
            {
                keyframes.Add(ticks);
            }
        }

        keyframes.Sort();
        mediaDurationTicks = ScaleToTicks(mediaDuration, mediaTimescale);
        isUnsupported = false;
        return keyframes.Count > 0;
    }

    /// <summary>
    /// Finds the presentation time of the first frame that is presented at or after the given media time.
    /// </summary>
    private static bool TryFindFirstPresentedTime(byte[] stts, int sttsCount, byte[] ctts, int cttsCount, long mediaTime, out long presentedTime)
    {
        presentedTime = long.MaxValue;

        long dts = 0;
        var cttsIndex = 0;
        long cttsRemaining = cttsCount > 0 ? BinaryPrimitives.ReadUInt32BigEndian(ctts) : 0;
        for (var i = 0; i < sttsCount; i++)
        {
            var runLength = BinaryPrimitives.ReadUInt32BigEndian(stts.AsSpan(i * 8));
            long delta = BinaryPrimitives.ReadUInt32BigEndian(stts.AsSpan((i * 8) + 4));
            for (long j = 0; j < runLength; j++)
            {
                while (cttsRemaining == 0 && cttsIndex < cttsCount)
                {
                    cttsIndex++;
                    cttsRemaining = cttsIndex < cttsCount ? BinaryPrimitives.ReadUInt32BigEndian(ctts.AsSpan(cttsIndex * 8)) : 0;
                }

                long compositionOffset = 0;
                if (cttsIndex < cttsCount)
                {
                    compositionOffset = BinaryPrimitives.ReadInt32BigEndian(ctts.AsSpan((cttsIndex * 8) + 4));
                    cttsRemaining--;
                }

                var pts = dts + compositionOffset;
                if (pts >= mediaTime && pts < presentedTime)
                {
                    presentedTime = pts;
                }

                dts += delta;
            }
        }

        return presentedTime != long.MaxValue;
    }

    /// <summary>
    /// Reads the edit list of a track.
    /// Only the usual layout is handled: any number of leading empty edits followed by one edit that presents the media.
    /// </summary>
    private static bool TryReadEditList(Stream stream, Box trak, out long mediaTime, out long emptyDuration, out bool hasMediaEdit)
    {
        mediaTime = 0;
        emptyDuration = 0;
        hasMediaEdit = false;

        if (!TryFindBox(stream, trak.PayloadStart, trak.End, BoxEdts, out var edts)
            || !TryFindBox(stream, edts.PayloadStart, edts.End, BoxElst, out var elst))
        {
            return true;
        }

        if (elst.End - elst.PayloadStart < 8)
        {
            return false;
        }

        Span<byte> buffer = stackalloc byte[20];
        stream.Position = elst.PayloadStart;
        stream.ReadExactly(buffer[..8]);
        var version = buffer[0];
        var entryCount = BinaryPrimitives.ReadUInt32BigEndian(buffer[4..]);
        var entrySize = version == 1 ? 20 : 12;
        if (entryCount * (long)entrySize > elst.End - elst.PayloadStart - 8)
        {
            return false;
        }

        for (var i = 0; i < entryCount; i++)
        {
            stream.ReadExactly(buffer[..entrySize]);
            long segmentDuration;
            long entryMediaTime;
            if (version == 1)
            {
                segmentDuration = BinaryPrimitives.ReadInt64BigEndian(buffer);
                entryMediaTime = BinaryPrimitives.ReadInt64BigEndian(buffer[8..]);
            }
            else
            {
                segmentDuration = BinaryPrimitives.ReadUInt32BigEndian(buffer);
                entryMediaTime = BinaryPrimitives.ReadInt32BigEndian(buffer[4..]);
            }

            if (entryMediaTime == -1)
            {
                if (hasMediaEdit || segmentDuration < 0)
                {
                    return false;
                }

                emptyDuration += segmentDuration;
            }
            else if (hasMediaEdit || entryMediaTime < 0)
            {
                return false;
            }
            else
            {
                hasMediaEdit = true;
                mediaTime = entryMediaTime;
            }
        }

        // An edit list that never presents the media is not handled.
        return hasMediaEdit || entryCount == 0;
    }

    private static bool HasRandomAccessGroups(Stream stream, Box stbl)
    {
        Span<byte> buffer = stackalloc byte[8];
        var position = stbl.PayloadStart;
        while (TryFindBox(stream, position, stbl.End, BoxSbgp, out var sbgp))
        {
            position = sbgp.End;
            if (sbgp.End - sbgp.PayloadStart < 8)
            {
                continue;
            }

            stream.Position = sbgp.PayloadStart;
            stream.ReadExactly(buffer);
            var groupingType = BinaryPrimitives.ReadUInt32BigEndian(buffer[4..]);
            if (groupingType is GroupingRap or GroupingSync)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Reads the timescale and duration from a movie header or media header box, which share their layout.
    /// </summary>
    private static bool TryReadTimescaleAndDuration(Stream stream, Box box, out long timescale, out long duration)
    {
        timescale = 0;
        duration = 0;

        Span<byte> buffer = stackalloc byte[32];
        if (box.End - box.PayloadStart < 4)
        {
            return false;
        }

        stream.Position = box.PayloadStart;
        stream.ReadExactly(buffer[..4]);
        var isVersion1 = buffer[0] == 1;
        var length = isVersion1 ? 28 : 16;
        if (box.End - box.PayloadStart - 4 < length)
        {
            return false;
        }

        stream.ReadExactly(buffer[..length]);
        if (isVersion1)
        {
            timescale = BinaryPrimitives.ReadUInt32BigEndian(buffer[16..]);
            duration = BinaryPrimitives.ReadInt64BigEndian(buffer[20..]);
        }
        else
        {
            timescale = BinaryPrimitives.ReadUInt32BigEndian(buffer[8..]);
            var duration32 = BinaryPrimitives.ReadUInt32BigEndian(buffer[12..]);
            duration = duration32 == uint.MaxValue ? 0 : duration32;
        }

        // A duration of all ones means the duration is unknown.
        if (duration < 0)
        {
            duration = 0;
        }

        return timescale > 0;
    }

    /// <summary>
    /// Reads the entries of a full box that consists of a version, flags, an entry count and fixed size entries.
    /// </summary>
    private static bool TryReadTable(Stream stream, Box box, int entrySize, out byte[] entries, out int entryCount)
    {
        entries = [];
        entryCount = 0;

        var payloadSize = box.End - box.PayloadStart;
        if (payloadSize < 8)
        {
            return false;
        }

        Span<byte> buffer = stackalloc byte[8];
        stream.Position = box.PayloadStart;
        stream.ReadExactly(buffer);
        var size = BinaryPrimitives.ReadUInt32BigEndian(buffer[4..]) * (long)entrySize;
        if (size > payloadSize - 8 || size > MaxTableSize)
        {
            return false;
        }

        entries = new byte[size];
        stream.ReadExactly(entries);
        entryCount = (int)(size / entrySize);
        return true;
    }

    private static bool TryFindBox(Stream stream, long start, long end, uint type, out Box box)
    {
        Span<byte> header = stackalloc byte[8];
        var position = start;
        while (end - position >= 8)
        {
            stream.Position = position;
            stream.ReadExactly(header);
            long size = BinaryPrimitives.ReadUInt32BigEndian(header);
            var boxType = BinaryPrimitives.ReadUInt32BigEndian(header[4..]);
            var headerSize = 8;

            if (size == 1)
            {
                // The size is stored in the following 8 bytes.
                if (end - position < 16)
                {
                    break;
                }

                stream.ReadExactly(header);
                size = BinaryPrimitives.ReadInt64BigEndian(header);
                headerSize = 16;
            }
            else if (size == 0)
            {
                // The box extends to the end of its container.
                size = end - position;
            }

            if (size < headerSize || size > end - position)
            {
                break;
            }

            if (boxType == type)
            {
                box = new Box(position + headerSize, position + size);
                return true;
            }

            position += size;
        }

        box = default;
        return false;
    }

    private static long ScaleToTicks(long value, long timescale)
    {
        return (long)((Int128)value * TimeSpan.TicksPerSecond / timescale);
    }

    private readonly record struct Box(long PayloadStart, long End);
}
