using MediaBrowser.MediaEncoding.Encoder;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.Encoder;

public class TrickplayFrameRateTests
{
    [Theory]
    // Progressive sources use the stream frame rate unchanged.
    [InlineData(false, 25f, false, 25f)]
    [InlineData(false, 25f, true, 25f)]
    [InlineData(false, 50f, true, 50f)]
    [InlineData(false, 23.976f, true, 23.976f)]
    // Interlaced, frame rate reported: single rate deinterlacing keeps it, double rate doubles it.
    [InlineData(true, 25f, false, 25f)]
    [InlineData(true, 25f, true, 50f)]
    [InlineData(true, 29.97f, true, 59.94f)]
    // Interlaced, field rate reported (field-coded streams): decoder outputs one frame per field pair.
    [InlineData(true, 50f, false, 25f)]
    [InlineData(true, 50f, true, 25f)]
    [InlineData(true, 59.94f, true, 29.97f)]
    public void GetTrickplayFilterFrameRate_ReturnsDeinterlacedOutputRate(bool isInterlaced, float frameRate, bool doubleRate, float expected)
    {
        var stream = new MediaStream
        {
            Type = MediaStreamType.Video,
            IsInterlaced = isInterlaced,
            AverageFrameRate = frameRate,
            RealFrameRate = frameRate
        };

        Assert.Equal(expected, MediaEncoder.GetTrickplayFilterFrameRate(stream, doubleRate), 3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetTrickplayFilterFrameRate_UnknownFrameRate_DefaultsTo30(bool doubleRate)
    {
        var stream = new MediaStream
        {
            Type = MediaStreamType.Video,
            IsInterlaced = true
        };

        Assert.Equal(30f, MediaEncoder.GetTrickplayFilterFrameRate(stream, doubleRate), 3);
    }
}
