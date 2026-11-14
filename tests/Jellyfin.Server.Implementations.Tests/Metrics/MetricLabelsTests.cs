using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Metrics;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Metrics;

[Collection(MetricsTestCollection.Name)]
public class MetricLabelsTests
{
    [Theory]
    [InlineData(PlayMethod.DirectPlay, "direct_play")]
    [InlineData(PlayMethod.DirectStream, "direct_stream")]
    [InlineData(PlayMethod.Transcode, "transcode")]
    public void PlayMethod_UsesStableLabel(PlayMethod method, string label)
        => Assert.Equal(label, MetricLabels.PlayMethod(method));

    [Theory]
    [InlineData(HardwareAccelerationType.none, "software")]
    [InlineData(HardwareAccelerationType.amf, "amf")]
    [InlineData(HardwareAccelerationType.qsv, "qsv")]
    [InlineData(HardwareAccelerationType.nvenc, "nvenc")]
    [InlineData(HardwareAccelerationType.v4l2m2m, "v4l2m2m")]
    [InlineData(HardwareAccelerationType.vaapi, "vaapi")]
    [InlineData(HardwareAccelerationType.videotoolbox, "videotoolbox")]
    [InlineData(HardwareAccelerationType.rkmpp, "rkmpp")]
    public void Acceleration_UsesStableLabel(HardwareAccelerationType mode, string label)
        => Assert.Equal(label, MetricLabels.Acceleration(mode));

    [Theory]
    [InlineData(CollectionTypeOptions.movies, "movies")]
    [InlineData(CollectionTypeOptions.tvshows, "tvshows")]
    [InlineData(CollectionTypeOptions.music, "music")]
    [InlineData(CollectionTypeOptions.musicvideos, "musicvideos")]
    [InlineData(CollectionTypeOptions.homevideos, "homevideos")]
    [InlineData(CollectionTypeOptions.boxsets, "boxsets")]
    [InlineData(CollectionTypeOptions.books, "books")]
    [InlineData(CollectionTypeOptions.mixed, "mixed")]
    public void CollectionType_UsesStableLabel(CollectionTypeOptions type, string label)
        => Assert.Equal(label, MetricLabels.CollectionType(type));

    [Fact]
    public void TranscodeReasons_UsesStableNamesAndOrdering()
    {
        var reasons = TranscodeReason.VideoCodecNotSupported
            | TranscodeReason.AudioCodecNotSupported
            | (TranscodeReason)(1 << 30);

        Assert.Equal(
            "audio_codec_not_supported|unknown_0x40000000|video_codec_not_supported",
            MetricLabels.TranscodeReasons(reasons));
    }

    [Theory]
    [InlineData(MediaStreamTypeEntity.Audio, "audio")]
    [InlineData(MediaStreamTypeEntity.Video, "video")]
    [InlineData(MediaStreamTypeEntity.Subtitle, "subtitle")]
    [InlineData(MediaStreamTypeEntity.EmbeddedImage, "embedded_image")]
    [InlineData(MediaStreamTypeEntity.Data, "data")]
    [InlineData(MediaStreamTypeEntity.Lyric, "lyric")]
    public void StreamKind_UsesStableLabel(MediaStreamTypeEntity type, string label)
        => Assert.Equal(label, MetricLabels.StreamKind(type));

    [Fact]
    public void UnknownEnumValues_UseStableFallbacks()
    {
        Assert.Equal("unknown", MetricLabels.PlayMethod((PlayMethod)999));
        Assert.Equal("unknown", MetricLabels.Acceleration((HardwareAccelerationType)999));
        Assert.Equal("unknown", MetricLabels.Acceleration(null));
        Assert.Equal("unknown", MetricLabels.CollectionType((CollectionTypeOptions)999));
        Assert.Equal("unknown", MetricLabels.StreamKind((MediaStreamTypeEntity)999));
    }
}
