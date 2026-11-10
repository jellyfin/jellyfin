using System;
using System.Buffers.Binary;
using System.Linq;
using System.Text;
using MediaBrowser.MediaEncoding.BdInfo;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.BdInfo;

public class BlurayIsoReaderTests
{
    [Fact]
    public void ParsePlaylist_ReadsClipsAndDurations()
    {
        var items = BlurayIsoReader.ParsePlaylist(BuildPlaylist(("00055", 27000000, 32538658), ("00056", 27000000, 33041659)));

        Assert.Equal(new[] { "00055", "00056" }, items.Select(i => i.ClipName));
        Assert.Equal(123.081, items[0].DurationSeconds, 3);
        Assert.Equal(134.259, items[1].DurationSeconds, 3);
    }

    [Fact]
    public void SelectMainPlaylist_IgnoresLongerPlaylistsThatLoopClips()
    {
        var loop = Enumerable.Repeat(new BlurayIsoReader.PlaylistItem("00038", 60), 200).ToArray();
        BlurayIsoReader.PlaylistItem[] extra = [new("00001", 90)];
        BlurayIsoReader.PlaylistItem[] main = [new("00064", 7000), new("00065", 100)];

        Assert.Same(main, BlurayIsoReader.SelectMainPlaylist([loop, extra, main]));
    }

    [Fact]
    public void SelectMainPlaylist_NoCandidates_ReturnsEmpty()
    {
        Assert.Empty(BlurayIsoReader.SelectMainPlaylist([]));
    }

    [Fact]
    public void MergeExtents_JoinsOnlyAdjacentRanges()
    {
        var extents = BlurayIsoReader.MergeExtents([(0, 10), (10, 20), (30, 40), (40, 50), (5, 6)]);

        Assert.Equal(new (long, long)[] { (0, 20), (30, 50), (5, 6) }, extents);
    }

    private static byte[] BuildPlaylist(params (string Clip, uint In, uint Out)[] items)
    {
        const int PlaylistStart = 40;
        const int ItemLength = 30;
        var data = new byte[PlaylistStart + 10 + (items.Length * (ItemLength + 2))];
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(8), PlaylistStart);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(PlaylistStart + 6), (ushort)items.Length);

        var position = PlaylistStart + 10;
        foreach (var (clip, inTime, outTime) in items)
        {
            BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(position), ItemLength);
            Encoding.ASCII.GetBytes(clip, data.AsSpan(position + 2));
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(position + 14), inTime);
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(position + 18), outTime);
            position += ItemLength + 2;
        }

        return data;
    }
}
