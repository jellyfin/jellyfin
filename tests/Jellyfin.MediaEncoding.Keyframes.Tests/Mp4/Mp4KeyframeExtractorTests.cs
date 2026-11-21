using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Jellyfin.MediaEncoding.Keyframes.Mp4;

public class Mp4KeyframeExtractorTests
{
    // Ten samples of 100 units each at a timescale of 1000, so one sample lasts 0.1 seconds.
    private static readonly byte[] _stts = FullBox("stts", 0, U32(1), U32(10), U32(100));

    // Samples 1, 5 and 9 are sync samples, which are presented at 0, 0.4 and 0.8 seconds.
    private static readonly byte[] _stss = FullBox("stss", 0, U32(3), U32(1), U32(5), U32(9));

    [Fact]
    public void GetKeyframeData_SyncSampleTable_ReturnsKeyframes()
    {
        var result = Extract(Movie(VideoTrack(_stts, _stss)));

        Assert.Equal(10_000_000, result.TotalDuration);
        Assert.Equal([0, 4_000_000, 8_000_000], result.KeyframeTicks);
    }

    [Fact]
    public void GetKeyframeData_MovieHeaderAfterMediaData_ReturnsKeyframes()
    {
        // A media data box with a 64 bit size, followed by the movie header.
        byte[] file =
        [
            .. Box("ftyp", Encoding.ASCII.GetBytes("isom")),
            .. U32(1), .. Encoding.ASCII.GetBytes("mdat"), .. U64(24), .. new byte[8],
            .. Movie(Track("soun", null, _stts), VideoTrack(_stts, _stss))
        ];

        var result = Extract(file);

        Assert.Equal([0, 4_000_000, 8_000_000], result.KeyframeTicks);
    }

    [Fact]
    public void GetKeyframeData_CompositionOffsetsAndEditList_StartsAtZero()
    {
        // Every sample is presented 200 units after it is decoded, and the edit list removes that delay again.
        var ctts = FullBox("ctts", 0, U32(1), U32(10), U32(200));
        var elst = FullBox("elst", 0, U32(1), U32(1000), U32(200), U32(0x00010000));

        var result = Extract(Movie(VideoTrack(_stts, _stss, ctts, elst)));

        Assert.Equal([0, 4_000_000, 8_000_000], result.KeyframeTicks);
    }

    [Fact]
    public void GetKeyframeData_CompositionOffsetsWithoutEditList_KeepsDelay()
    {
        var ctts = FullBox("ctts", 0, U32(2), U32(4), U32(200), U32(6), U32(100));

        var result = Extract(Movie(VideoTrack(_stts, _stss, ctts)));

        Assert.Equal([2_000_000, 5_000_000, 9_000_000], result.KeyframeTicks);
    }

    [Fact]
    public void GetKeyframeData_NegativeCompositionOffsets_ReturnsKeyframes()
    {
        var ctts = FullBox("ctts", 1, U32(2), U32(4), U32(0), U32(6), U32(unchecked((uint)-100)));

        var result = Extract(Movie(VideoTrack(_stts, _stss, ctts)));

        Assert.Equal([0, 3_000_000, 7_000_000], result.KeyframeTicks);
    }

    [Fact]
    public void GetKeyframeData_EmptyEdit_DelaysKeyframes()
    {
        // The movie timescale is 600, so an empty edit of 300 units delays the track by 0.5 seconds.
        var elst = FullBox("elst", 0, U32(2), U32(300), U32(uint.MaxValue), U32(0x00010000), U32(600), U32(0), U32(0x00010000));

        var result = Extract(Movie(VideoTrack(_stts, _stss, null, elst)));

        Assert.Equal([5_000_000, 9_000_000, 13_000_000], result.KeyframeTicks);
    }

    [Fact]
    public void GetKeyframeData_Version1EditList_ReturnsKeyframes()
    {
        var elst = FullBox("elst", 1, U32(1), U64(600), U64(400), U32(0x00010000));

        var result = Extract(Movie(VideoTrack(_stts, _stss, null, elst)));

        // The first sync sample is before the start of the edit.
        Assert.Equal([0, 4_000_000], result.KeyframeTicks);
    }

    [Fact]
    public void GetKeyframeData_EditStartsBetweenFrames_StartsAtFirstPresentedFrame()
    {
        // The edit starts at 250, the first frame at or after that is presented at 300.
        var elst = FullBox("elst", 0, U32(1), U32(600), U32(250), U32(0x00010000));

        var result = Extract(Movie(VideoTrack(_stts, _stss, null, elst)));

        Assert.Equal([1_000_000, 5_000_000], result.KeyframeTicks);
    }

    [Fact]
    public void GetKeyframeData_NoSyncSampleTable_ReturnsEverySample()
    {
        var result = Extract(Movie(VideoTrack(_stts, null)));

        Assert.Equal(Enumerable.Range(0, 10).Select(i => i * 1_000_000L), result.KeyframeTicks);
    }

    [Fact]
    public void GetKeyframeData_CoverArtTrack_IsSkipped()
    {
        var coverArt = VideoTrack(FullBox("stts", 0, U32(1), U32(1), U32(1000)), FullBox("stss", 0, U32(1), U32(1)));

        var result = Extract(Movie(coverArt, VideoTrack(_stts, _stss)));

        Assert.Equal([0, 4_000_000, 8_000_000], result.KeyframeTicks);
    }

    [Fact]
    public void GetKeyframeData_Fragmented_ReturnsNoKeyframes()
    {
        var result = Extract(Movie(VideoTrack(_stts, _stss), Box("mvex")));

        Assert.Empty(result.KeyframeTicks);
    }

    [Fact]
    public void GetKeyframeData_MultipleEdits_ReturnsNoKeyframes()
    {
        var elst = FullBox("elst", 0, U32(2), U32(300), U32(0), U32(0x00010000), U32(300), U32(500), U32(0x00010000));

        var result = Extract(Movie(VideoTrack(_stts, _stss, null, elst)));

        Assert.Empty(result.KeyframeTicks);
    }

    [Fact]
    public void GetKeyframeData_PartialSyncSamples_ReturnsNoKeyframes()
    {
        var result = Extract(Movie(Track("vide", null, _stts, _stss, FullBox("stps", 0, U32(1), U32(3)))));

        Assert.Empty(result.KeyframeTicks);
    }

    [Fact]
    public void GetKeyframeData_EntryCountLargerThanBox_ReturnsNoKeyframes()
    {
        var result = Extract(Movie(VideoTrack(_stts, FullBox("stss", 0, U32(1000), U32(1)))));

        Assert.Empty(result.KeyframeTicks);
    }

    [Fact]
    public void GetKeyframeData_NotAnMp4File_ReturnsNoKeyframes()
    {
        var result = Extract(Encoding.ASCII.GetBytes("This is not an mp4 file, but it is long enough to be read as a box."));

        Assert.Empty(result.KeyframeTicks);
    }

    [Fact]
    public void GetKeyframeData_Truncated_ReturnsNoKeyframes()
    {
        var file = Movie(VideoTrack(_stts, _stss));

        var result = Extract(file[..^10]);

        Assert.Empty(result.KeyframeTicks);
    }

    private static KeyframeData Extract(byte[] file)
    {
        using var stream = new MemoryStream(file);
        return Mp4KeyframeExtractor.GetKeyframeData(stream);
    }

    private static byte[] Movie(params byte[][] children)
    {
        // Timescale 600 and a duration of one second.
        var mvhd = FullBox("mvhd", 0, U32(0), U32(0), U32(600), U32(600));
        return Box("moov", [mvhd, .. children]);
    }

    private static byte[] VideoTrack(byte[] stts, byte[]? stss, byte[]? ctts = null, byte[]? elst = null)
    {
        return Track("vide", elst, new[] { stts, ctts, stss }.OfType<byte[]>().ToArray());
    }

    private static byte[] Track(string handler, byte[]? elst, params byte[][] tables)
    {
        var mdhd = FullBox("mdhd", 0, U32(0), U32(0), U32(1000), U32(1000));
        var hdlr = FullBox("hdlr", 0, U32(0), Encoding.ASCII.GetBytes(handler), new byte[12]);
        var mdia = Box("mdia", mdhd, hdlr, Box("minf", Box("stbl", tables)));
        return elst is null
            ? Box("trak", mdia)
            : Box("trak", Box("edts", elst), mdia);
    }

    private static byte[] FullBox(string type, byte version, params byte[][] payload)
        => Box(type, [[version, 0, 0, 0], .. payload]);

    private static byte[] Box(string type, params byte[][] payload)
    {
        var size = 8 + payload.Sum(p => p.Length);
        return [.. U32((uint)size), .. Encoding.ASCII.GetBytes(type), .. payload.SelectMany(p => p)];
    }

    private static byte[] U32(uint value)
    {
        var buffer = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        return buffer;
    }

    private static byte[] U64(ulong value)
    {
        var buffer = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, value);
        return buffer;
    }
}
