using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using IConfigurationManager = MediaBrowser.Common.Configuration.IConfigurationManager;

namespace Jellyfin.Controller.Tests.MediaEncoding;

public class EncodingHelperHdrMetadataTests
{
    private static EncodingHelper CreateEncodingHelper()
    {
        var mediaEncoder = new Mock<IMediaEncoder>();
        mediaEncoder
            .Setup(e => e.SupportsBitStreamFilterWithOption(It.IsAny<BitStreamFilterOptionType>()))
            .Returns(true);

        return new EncodingHelper(
            Mock.Of<IApplicationPaths>(),
            mediaEncoder.Object,
            Mock.Of<ISubtitleEncoder>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<IConfigurationManager>(),
            Mock.Of<IPathManager>());
    }

    /// <summary>
    /// HEVC Main 10, Dolby Vision Profile 8.1 (bl compat id 1) with HDR10+ metadata,
    /// which resolves to VideoRangeType.DOVIWithHDR10Plus.
    /// </summary>
    private static MediaStream CreateDoviWithHdr10PlusStream() => new()
    {
        Type = MediaStreamType.Video,
        Codec = "hevc",
        Profile = "Main 10",
        Width = 3840,
        Height = 1606,
        BitDepth = 10,
        ColorPrimaries = "bt2020",
        ColorSpace = "bt2020nc",
        ColorTransfer = "smpte2084",
        DvProfile = 8,
        DvBlSignalCompatibilityId = 1,
        RpuPresentFlag = 1,
        BlPresentFlag = 1,
        Hdr10PlusPresentFlag = true
    };

    private static EncodingJobInfo CreateHlsStreamCopyState(MediaStream videoStream, string? hevcRangeTypes)
    {
        var state = new EncodingJobInfo(TranscodingJobType.Hls)
        {
            VideoStream = videoStream,
            SupportedVideoCodecs = new[] { "hevc" },
            BaseRequest = new BaseEncodingJobOptions
            {
                AllowVideoStreamCopy = true,
                Context = EncodingContext.Streaming
            }
        };

        if (hevcRangeTypes is not null)
        {
            state.BaseRequest.StreamOptions["hevc-rangetype"] = hevcRangeTypes;
        }

        return state;
    }

    [Theory]
    // Dolby Vision client that does not declare the coexistence range type: keep stripping HDR10+.
    [InlineData("SDR,HDR10,HLG,DOVI,DOVIWithHDR10", "-bsf:v hevc_mp4toannexb,hevc_metadata=remove_hdr10plus=1")]
    // Dolby Vision client that explicitly declares DOVIWithHDR10Plus: copy the bitstream untouched.
    [InlineData("SDR,HDR10,HLG,DOVI,DOVIWithHDR10,DOVIWithHDR10Plus,HDR10Plus", "-bsf:v hevc_mp4toannexb")]
    // HDR10-only client: the HDR10 base layer is compatible, copy untouched.
    [InlineData("SDR,HDR10,HLG", "-bsf:v hevc_mp4toannexb")]
    // Client without range type conditions: copy untouched.
    [InlineData(null, "-bsf:v hevc_mp4toannexb")]
    public void GetBitStreamArgs_DoviWithHdr10PlusSource_RemovesHdr10PlusOnlyWhenCoexistenceNotDeclared(string? hevcRangeTypes, string expectedArgs)
    {
        var helper = CreateEncodingHelper();
        var state = CreateHlsStreamCopyState(CreateDoviWithHdr10PlusStream(), hevcRangeTypes);

        Assert.Equal(expectedArgs, helper.GetBitStreamArgs(state, MediaStreamType.Video));
    }

    [Theory]
    [InlineData("SDR,HDR10,HLG,DOVI,DOVIWithHDR10", true)]
    [InlineData("SDR,HDR10,HLG,DOVI,DOVIWithHDR10,DOVIWithHDR10Plus,HDR10Plus", true)]
    [InlineData("SDR,HDR10,HLG", true)]
    [InlineData(null, true)]
    // SDR-only clients must never receive an HDR bitstream via stream copy.
    [InlineData("SDR", false)]
    public void CanStreamCopyVideo_DoviWithHdr10PlusSource_MatchesRangeSupport(string? hevcRangeTypes, bool expectedCanCopy)
    {
        var helper = CreateEncodingHelper();
        var state = CreateHlsStreamCopyState(CreateDoviWithHdr10PlusStream(), hevcRangeTypes);

        Assert.Equal(expectedCanCopy, helper.CanStreamCopyVideo(state, state.VideoStream));
    }

    [Theory]
    // Without the coexistence declaration the server plans an HDR10+ removal, and the
    // HLS playlist must not advertise HDR10+ via SUPPLEMENTAL-CODECS.
    [InlineData("SDR,HDR10,HLG,DOVI,DOVIWithHDR10", true)]
    // With the declaration the metadata survives, so the playlist may advertise it.
    [InlineData("SDR,HDR10,HLG,DOVI,DOVIWithHDR10,DOVIWithHDR10Plus,HDR10Plus", false)]
    [InlineData("SDR,HDR10,HLG", false)]
    [InlineData(null, false)]
    public void IsHdr10PlusRemoved_DoviWithHdr10PlusSource_TracksCoexistenceDeclaration(string? hevcRangeTypes, bool expectedRemoved)
    {
        var helper = CreateEncodingHelper();
        var state = CreateHlsStreamCopyState(CreateDoviWithHdr10PlusStream(), hevcRangeTypes);

        Assert.Equal(expectedRemoved, helper.IsHdr10PlusRemoved(state));
    }

    [Fact]
    public void IsDoviRemoved_DoviWithHdr10PlusSource_CoexistenceDeclared_KeepsDoviMetadata()
    {
        var helper = CreateEncodingHelper();
        var state = CreateHlsStreamCopyState(
            CreateDoviWithHdr10PlusStream(),
            "SDR,HDR10,HLG,DOVI,DOVIWithHDR10,DOVIWithHDR10Plus,HDR10Plus");

        // The dvh1 tagging and the DV SUPPLEMENTAL-CODECS entry both require the
        // Dolby Vision metadata to survive the copy.
        Assert.False(helper.IsDoviRemoved(state));
    }
}
