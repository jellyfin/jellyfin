using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DiscUtils.Udf;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.MediaEncoding.BdInfo;

/// <summary>
/// Locates the clips of the main playlist inside a Blu-ray ISO image.
/// </summary>
internal static class BlurayIsoReader
{
    private const string PlaylistDirectory = @"BDMV\PLAYLIST";
    private const string StreamDirectory = @"BDMV\STREAM";
    private const double PlaylistTicksPerSecond = 45000;
    private const int MappingBufferSize = 16 * 1024 * 1024;

    /// <summary>
    /// Reads the clips of the main playlist of an image.
    /// </summary>
    /// <param name="image">A readable and seekable stream of the image.</param>
    /// <returns>The clips in playback order, empty if the image has no usable playlist.</returns>
    public static IReadOnlyList<BlurayIsoClip> ReadMainPlaylistClips(Stream image)
    {
        using var recorder = new ExtentRecordingStream(image);
        using var reader = new UdfReader(recorder);

        var playlists = reader.GetFiles(PlaylistDirectory, "*.mpls").Select(path =>
        {
            using var stream = reader.OpenFile(path, FileMode.Open, FileAccess.Read);
            var data = new byte[stream.Length];
            stream.ReadExactly(data);
            return ParsePlaylist(data);
        });

        var items = SelectMainPlaylist(playlists);
        var clips = new List<BlurayIsoClip>(items.Count);
        foreach (var item in items)
        {
            using var stream = reader.OpenFile($@"{StreamDirectory}\{item.ClipName}.m2ts", FileMode.Open, FileAccess.Read);
            clips.Add(new BlurayIsoClip(item.DurationSeconds, MapExtents(stream, recorder)));
        }

        return clips;
    }

    /// <summary>
    /// Selects the longest playlist which plays every clip at most once.
    /// Discs commonly contain much longer decoy playlists that loop the same few clips.
    /// </summary>
    /// <param name="playlists">The playlists of the disc.</param>
    /// <returns>The items of the main playlist, empty if there are no candidates.</returns>
    internal static IReadOnlyList<PlaylistItem> SelectMainPlaylist(IEnumerable<IReadOnlyList<PlaylistItem>> playlists)
        => playlists
            .Where(p => p.Count > 0 && p.Select(i => i.ClipName).Distinct(StringComparer.Ordinal).Count() == p.Count)
            .OrderByDescending(p => p.Sum(i => i.DurationSeconds))
            .FirstOrDefault()
            ?? [];

    /// <summary>
    /// Parses the play items of an MPLS file.
    /// </summary>
    /// <param name="data">The content of the file.</param>
    /// <returns>The play items.</returns>
    internal static IReadOnlyList<PlaylistItem> ParsePlaylist(ReadOnlySpan<byte> data)
    {
        var playlistStart = (int)BinaryPrimitives.ReadUInt32BigEndian(data[8..]);
        var itemCount = BinaryPrimitives.ReadUInt16BigEndian(data[(playlistStart + 6)..]);
        var position = playlistStart + 10;

        var items = new List<PlaylistItem>(itemCount);
        for (var i = 0; i < itemCount; i++)
        {
            var clipName = Encoding.ASCII.GetString(data.Slice(position + 2, 5));
            var inTime = BinaryPrimitives.ReadUInt32BigEndian(data[(position + 14)..]);
            var outTime = BinaryPrimitives.ReadUInt32BigEndian(data[(position + 18)..]);
            items.Add(new PlaylistItem(clipName, (outTime - inTime) / PlaylistTicksPerSecond));
            position += 2 + BinaryPrimitives.ReadUInt16BigEndian(data[position..]);
        }

        return items;
    }

    /// <summary>
    /// Merges adjacent byte ranges.
    /// </summary>
    /// <param name="reads">The byte ranges in file order, end exclusive.</param>
    /// <returns>The merged byte ranges.</returns>
    internal static IReadOnlyList<(long Start, long End)> MergeExtents(IEnumerable<(long Start, long End)> reads)
    {
        var extents = new List<(long Start, long End)>();
        foreach (var read in reads)
        {
            if (extents.Count > 0 && extents[^1].End == read.Start)
            {
                extents[^1] = (extents[^1].Start, read.End);
            }
            else
            {
                extents.Add(read);
            }
        }

        return extents;
    }

    // DiscUtils doesn't expose file extents, so walk the file while the recorder notes where it would have read from.
    private static IReadOnlyList<(long Start, long End)> MapExtents(Stream clip, ExtentRecordingStream recorder)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(MappingBufferSize);
        try
        {
            recorder.BeginRecording();
            var remaining = clip.Length;
            while (remaining > 0)
            {
                var read = clip.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                if (read <= 0)
                {
                    throw new EndOfStreamException("Unexpected end of Blu-ray clip while mapping its location.");
                }

                remaining -= read;
            }

            return MergeExtents(recorder.EndRecording());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// A play item of an MPLS file.
    /// </summary>
    /// <param name="ClipName">The name of the clip, without extension.</param>
    /// <param name="DurationSeconds">The playback duration in seconds.</param>
    internal sealed record PlaylistItem(string ClipName, double DurationSeconds);

    // While recording no data is read from the image, keeping the mapping of a disc free of any real I/O.
    private sealed class ExtentRecordingStream : Stream
    {
        private readonly Stream _inner;
        private List<(long Start, long End)>? _reads;

        public ExtentRecordingStream(Stream inner)
        {
            _inner = inner;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public void BeginRecording() => _reads = [];

        public List<(long Start, long End)> EndRecording()
        {
            var reads = _reads ?? [];
            _reads = null;
            return reads;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_reads is null)
            {
                return _inner.Read(buffer, offset, count);
            }

            var start = _inner.Position;
            var length = (int)Math.Min(count, _inner.Length - start);
            _reads.Add((start, start + length));
            _inner.Position = start + length;
            return length;
        }

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        public override void Flush()
        {
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
