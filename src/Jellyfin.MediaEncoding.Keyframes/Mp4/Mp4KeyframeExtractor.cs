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

        if (!TryFindVideoSampleTable(stream, trak, out var stbl, out var mediaTimescale, out var mediaDuration)
            || !TryFindBox(stream, stbl.PayloadStart, stbl.End, BoxStts, out var sttsBox)
            || !TryReadTable(stream, sttsBox, 8, out var stts))
        {
            return false;
        }

        long sampleCount = 0;
        for (var i = 0; i < stts.Count; i++)
        {
            sampleCount += BinaryPrimitives.ReadUInt32BigEndian(stts.Entries.AsSpan(i * 8));
        }

        // Cover art is stored as a video track with a single sample.
        if (sampleCount <= 1)
        {
            return false;
        }

        // From here on this is the track ffmpeg is going to pick, so a layout that is not handled must not fall through to another track.
        isUnsupported = true;

        // Partial sync samples and random access point groups add keyframes that are not listed in the sync sample table.
        if (TryFindBox(stream, stbl.PayloadStart, stbl.End, BoxStps, out _)
            || HasRandomAccessGroups(stream, stbl)
            || !TryReadEditList(stream, trak, out var mediaTime, out var emptyDuration, out var hasMediaEdit)
            || !TryReadOptionalTable(stream, stbl, BoxCtts, 8, out var ctts, out _)
            || !TryReadOptionalTable(stream, stbl, BoxStss, 4, out var stss, out var hasSyncTable))
        {
            return false;
        }

        // With an edit list ffmpeg moves the first presented frame to the start of the edit,
        // even if the edit starts between two frames.
        if (hasMediaEdit && !TryFindFirstPresentedTime(stts, ctts, sampleCount, mediaTime, out mediaTime))
        {
            return false;
        }

        if (!TryReadKeyframeTimes(stts, ctts, stss, hasSyncTable, sampleCount, out var times))
        {
            return false;
        }

        var startOffsetTicks = ScaleToTicks(emptyDuration, movieTimescale);
        foreach (var time in times)
        {
            // Keyframes before the start of the edit are not presented.
            var ticks = ScaleToTicks(time - mediaTime, mediaTimescale) + startOffsetTicks;
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
    /// Finds the sample table box of a track, if it is a video track.
    /// </summary>
    private static bool TryFindVideoSampleTable(Stream stream, Box trak, out Box stbl, out long mediaTimescale, out long mediaDuration)
    {
        stbl = default;
        mediaTimescale = 0;
        mediaDuration = 0;

        if (!TryFindBox(stream, trak.PayloadStart, trak.End, BoxMdia, out var mdia)
            || !TryFindBox(stream, mdia.PayloadStart, mdia.End, BoxHdlr, out var hdlr)
            || hdlr.End - hdlr.PayloadStart < 12)
        {
            return false;
        }

        // The handler type follows the version, the flags and a reserved field.
        Span<byte> buffer = stackalloc byte[12];
        stream.Position = hdlr.PayloadStart;
        stream.ReadExactly(buffer);
        if (BinaryPrimitives.ReadUInt32BigEndian(buffer[8..]) != HandlerVideo)
        {
            return false;
        }

        return TryFindBox(stream, mdia.PayloadStart, mdia.End, BoxMdhd, out var mdhd)
            && TryReadTimescaleAndDuration(stream, mdhd, out mediaTimescale, out mediaDuration)
            && TryFindBox(stream, mdia.PayloadStart, mdia.End, BoxMinf, out var minf)
            && TryFindBox(stream, minf.PayloadStart, minf.End, BoxStbl, out stbl);
    }

    /// <summary>
    /// Gets the presentation time of every keyframe in units of the media timescale.
    /// </summary>
    private static bool TryReadKeyframeTimes(Table stts, Table ctts, Table stss, bool hasSyncTable, long sampleCount, out List<long> times)
    {
        times = [];

        // Without a sync sample table every sample is a sync sample.
        var keyframeCount = hasSyncTable ? stss.Count : sampleCount;
        var decodingTimes = new RunCursor(stts);
        var compositionOffsets = new RunCursor(ctts);
        long previousSample = -1;

        for (long i = 0; i < keyframeCount; i++)
        {
            // Sample numbers in the sync sample table are 1-based.
            var sample = hasSyncTable ? BinaryPrimitives.ReadUInt32BigEndian(stss.Entries.AsSpan((int)i * 4)) - 1L : i;
            if (sample <= previousSample)
            {
                return false;
            }

            previousSample = sample;

            if (!TryGetPresentationTime(ref decodingTimes, ref compositionOffsets, sample, out var time))
            {
                // The sync sample table refers to a sample that does not exist.
                break;
            }

            times.Add(time);
        }

        return true;
    }

    /// <summary>
    /// Finds the presentation time of the first frame that is presented at or after the given media time.
    /// </summary>
    private static bool TryFindFirstPresentedTime(Table stts, Table ctts, long sampleCount, long mediaTime, out long presentedTime)
    {
        presentedTime = long.MaxValue;

        var decodingTimes = new RunCursor(stts);
        var compositionOffsets = new RunCursor(ctts);

        // Samples are stored in decoding order, so every sample has to be looked at.
        for (long sample = 0; sample < sampleCount; sample++)
        {
            if (!TryGetPresentationTime(ref decodingTimes, ref compositionOffsets, sample, out var time))
            {
                break;
            }

            if (time >= mediaTime && time < presentedTime)
            {
                presentedTime = time;
            }
        }

        return presentedTime != long.MaxValue;
    }

    /// <summary>
    /// Gets the presentation time of a sample in units of the media timescale.
    /// Samples have to be requested in ascending order.
    /// </summary>
    private static bool TryGetPresentationTime(ref RunCursor decodingTimes, ref RunCursor compositionOffsets, long sample, out long time)
    {
        time = 0;
        if (!decodingTimes.MoveTo(sample))
        {
            return false;
        }

        var decodingTime = decodingTimes.Total + ((sample - decodingTimes.RunStart) * decodingTimes.Value);

        // Version 0 offsets are unsigned by specification, but writers store negative values in them, so they are read as signed like ffmpeg does.
        // A sample without an entry in the table has no offset.
        var compositionOffset = compositionOffsets.MoveTo(sample) ? unchecked((int)compositionOffsets.Value) : 0;

        time = decodingTime + compositionOffset;
        return true;
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

        if (!TryReadEditListHeader(stream, elst, out var isVersion1, out var entryCount))
        {
            return false;
        }

        for (var i = 0; i < entryCount; i++)
        {
            ReadEdit(stream, isVersion1, out var segmentDuration, out var entryMediaTime);

            // Nothing may follow the edit that presents the media.
            if (hasMediaEdit || entryMediaTime < -1 || segmentDuration < 0)
            {
                return false;
            }

            // A media time of -1 marks an empty edit, which delays the start of the track.
            if (entryMediaTime == -1)
            {
                emptyDuration += segmentDuration;
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

    /// <summary>
    /// Reads the version and the number of entries of an edit list, and leaves the stream at the first entry.
    /// </summary>
    private static bool TryReadEditListHeader(Stream stream, Box elst, out bool isVersion1, out uint entryCount)
    {
        isVersion1 = false;
        entryCount = 0;

        var payloadSize = elst.End - elst.PayloadStart;
        if (payloadSize < 8)
        {
            return false;
        }

        Span<byte> buffer = stackalloc byte[8];
        stream.Position = elst.PayloadStart;
        stream.ReadExactly(buffer);
        isVersion1 = buffer[0] == 1;
        entryCount = BinaryPrimitives.ReadUInt32BigEndian(buffer[4..]);

        // The durations and media times are 64 bit in version 1 and 32 bit otherwise, both are followed by a 32 bit rate.
        var entrySize = isVersion1 ? 20 : 12;
        return entryCount * (long)entrySize <= payloadSize - 8;
    }

    /// <summary>
    /// Reads the duration and the media time of the next edit list entry.
    /// </summary>
    private static void ReadEdit(Stream stream, bool isVersion1, out long segmentDuration, out long mediaTime)
    {
        Span<byte> buffer = stackalloc byte[20];
        if (isVersion1)
        {
            stream.ReadExactly(buffer);
            segmentDuration = BinaryPrimitives.ReadInt64BigEndian(buffer);
            mediaTime = BinaryPrimitives.ReadInt64BigEndian(buffer[8..]);
        }
        else
        {
            stream.ReadExactly(buffer[..12]);
            segmentDuration = BinaryPrimitives.ReadUInt32BigEndian(buffer);
            mediaTime = BinaryPrimitives.ReadInt32BigEndian(buffer[4..]);
        }
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
    /// Reads a table that may be missing. A missing table is read as an empty one.
    /// </summary>
    private static bool TryReadOptionalTable(Stream stream, Box container, uint type, int entrySize, out Table table, out bool exists)
    {
        table = new Table([], 0);
        exists = TryFindBox(stream, container.PayloadStart, container.End, type, out var box);
        return !exists || TryReadTable(stream, box, entrySize, out table);
    }

    /// <summary>
    /// Reads the entries of a box that holds 4 bytes of version and flags, a 4 byte entry count and then entries of a fixed size.
    /// </summary>
    private static bool TryReadTable(Stream stream, Box box, int entrySize, out Table table)
    {
        table = new Table([], 0);

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

        var entries = new byte[size];
        stream.ReadExactly(entries);
        table = new Table(entries, (int)(size / entrySize));
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

    /// <summary>
    /// Walks a table in which every 8 byte entry holds a number of consecutive samples and the value they share.
    /// </summary>
    private struct RunCursor
    {
        private readonly Table _table;
        private int _index;

        public RunCursor(Table table)
        {
            _table = table;
            _index = 0;
            RunStart = 0;
            Total = 0;
        }

        /// <summary>
        /// Gets the number of the first sample in the current run.
        /// </summary>
        public long RunStart { get; private set; }

        /// <summary>
        /// Gets the sum of the values of all samples before the current run.
        /// </summary>
        public long Total { get; private set; }

        /// <summary>
        /// Gets the value shared by the samples in the current run.
        /// </summary>
        public readonly uint Value => BinaryPrimitives.ReadUInt32BigEndian(_table.Entries.AsSpan((_index * 8) + 4));

        /// <summary>
        /// Moves forward to the run that contains the sample.
        /// </summary>
        /// <param name="sample">The zero-based sample number, which must not be before the current run.</param>
        /// <returns><c>true</c> if the table has a run for the sample.</returns>
        public bool MoveTo(long sample)
        {
            while (_index < _table.Count)
            {
                long runLength = BinaryPrimitives.ReadUInt32BigEndian(_table.Entries.AsSpan(_index * 8));
                if (sample < RunStart + runLength)
                {
                    return true;
                }

                Total += runLength * Value;
                RunStart += runLength;
                _index++;
            }

            return false;
        }
    }

    private readonly record struct Box(long PayloadStart, long End);

    private readonly record struct Table(byte[] Entries, int Count);
}
