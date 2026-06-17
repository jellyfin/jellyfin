using System;
using System.Collections.Generic;
using System.IO;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Moq;
using Xunit;

using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Jellyfin.Controller.Tests.MediaEncoding;

public class EncodingHelperTests
{
    [Fact]
    public void GetMapArgs_MpegTsWithDataStream_MapsUsingStreamIndex()
    {
        var data = new MediaStream { Index = 0, Type = MediaStreamType.Data, Codec = "epg" };
        var video = new MediaStream { Index = 1, Type = MediaStreamType.Video, Codec = "h264" };
        var audio = new MediaStream { Index = 2, Type = MediaStreamType.Audio, Codec = "ac3" };
        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "dut" };
        var state = BuildState(sub, SubtitleDeliveryMethod.Encode, additionalStreams: [data, sub]);
        state.MediaSource.IsInfiniteStream = true;
        state.VideoStream = video;
        state.AudioStream = audio;

        var args = CreateHelper().GetMapArgs(state);

        Assert.Contains("-map 0:1", args, StringComparison.Ordinal);
        Assert.Contains("-map 0:2", args, StringComparison.Ordinal);
        Assert.DoesNotContain("-sn", args, StringComparison.Ordinal);
        Assert.DoesNotContain("-map 0:3", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetMapArgs_LiveStreamUnknownIndex_UsesStreamTypeSelectors()
    {
        var video = new MediaStream { Index = -1, Type = MediaStreamType.Video, Codec = "h264" };
        var audio = new MediaStream { Index = -1, Type = MediaStreamType.Audio, Codec = "ac3" };
        var state = BuildState(subtitle: null, deliveryMethod: null);
        state.MediaSource.IsInfiniteStream = true;
        state.VideoStream = video;
        state.AudioStream = audio;

        var args = CreateHelper().GetMapArgs(state);

        Assert.Contains("-map 0:v:0", args, StringComparison.Ordinal);
        Assert.Contains("-map 0:a:0", args, StringComparison.Ordinal);
        Assert.Contains("-map -0:s", args, StringComparison.Ordinal);
        Assert.DoesNotContain("-sn", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetMapArgs_LiveStreamUnknownIndexWithEmbedSubtitle_MapsFirstSubtitleStream()
    {
        var video = new MediaStream { Index = -1, Type = MediaStreamType.Video, Codec = "h264" };
        var audio = new MediaStream { Index = -1, Type = MediaStreamType.Audio, Codec = "ac3" };
        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "srt" };
        var state = BuildState(sub, SubtitleDeliveryMethod.Embed, additionalStreams: [sub]);
        state.MediaSource.IsInfiniteStream = true;
        state.VideoStream = video;
        state.AudioStream = audio;

        var args = CreateHelper().GetMapArgs(state);

        Assert.Contains("-map 0:v:0", args, StringComparison.Ordinal);
        Assert.Contains("-map 0:a:0", args, StringComparison.Ordinal);
        Assert.Contains("-map 0:s:0", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetMapArgs_UnknownVideoIndexOnNonLiveStream_UsesSn()
    {
        var video = new MediaStream { Index = -1, Type = MediaStreamType.Video, Codec = "h264" };
        var audio = new MediaStream { Index = 2, Type = MediaStreamType.Audio, Codec = "ac3" };
        var state = BuildState(subtitle: null, deliveryMethod: null);
        state.MediaSource.IsInfiniteStream = false;
        state.VideoStream = video;
        state.AudioStream = audio;

        var args = CreateHelper().GetMapArgs(state);

        Assert.Equal("-sn", args);
    }

    [Fact]
    public void GetNegativeMapArgsByFilters_FilterComplex_UsesStreamIndex()
    {
        var data = new MediaStream { Index = 0, Type = MediaStreamType.Data, Codec = "epg" };
        var video = new MediaStream { Index = 1, Type = MediaStreamType.Video, Codec = "h264" };
        var audio = new MediaStream { Index = 2, Type = MediaStreamType.Audio, Codec = "ac3" };
        var state = BuildState(subtitle: null, deliveryMethod: null, additionalStreams: [data]);
        state.VideoStream = video;
        state.AudioStream = audio;

        var args = CreateHelper().GetNegativeMapArgsByFilters(state, " -filter_complex \"[0:1]scale[main]\"");

        Assert.Equal("-map -0:1 ", args);
    }

    [Fact]
    public void GetSwVidFilterChain_LiveDvbsub_UsesInputStreamOverlay()
    {
        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "dut" };
        var state = BuildLiveDvbsubState(sub);

        var (_, subFilters, overlayFilters) = CreateHelper().GetSwVidFilterChain(
            state,
            new EncodingOptions(),
            "libx264");

        Assert.NotEmpty(subFilters);
        Assert.Contains(subFilters, f => f.Contains("format=bgra", StringComparison.Ordinal));
        Assert.Contains(overlayFilters, f => f.Contains("overlay=", StringComparison.Ordinal));
        Assert.DoesNotContain(subFilters, f => f.Contains("subtitles=f=", StringComparison.Ordinal));
    }

    [Fact]
    public void GetAppleVidFilterChain_LiveDvbsubWithoutVtSupport_FallsBackToSoftwareOverlay()
    {
        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "dut" };
        var state = BuildLiveDvbsubState(sub);
        var options = new EncodingOptions { HardwareAccelerationType = HardwareAccelerationType.videotoolbox };

        var (_, subFilters, overlayFilters) = CreateHelper().GetAppleVidFilterChain(
            state,
            options,
            "h264_videotoolbox");

        Assert.NotEmpty(subFilters);
        Assert.NotEmpty(overlayFilters);
        Assert.Contains(overlayFilters, f => f.Contains("overlay=", StringComparison.Ordinal));
        Assert.DoesNotContain(overlayFilters, f => f.Contains("overlay_videotoolbox", StringComparison.Ordinal));
    }

    [Fact]
    public void GetAppleVidFilterChain_LiveDvbsub_UsesVideotoolboxOverlayWhenSupported()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "dut" };
        var state = BuildLiveDvbsubState(sub);
        var options = new EncodingOptions { HardwareAccelerationType = HardwareAccelerationType.videotoolbox };

        var (_, subFilters, overlayFilters) = CreateVideoToolboxHelper().GetAppleVidFilterChain(
            state,
            options,
            "h264_videotoolbox");

        Assert.NotEmpty(subFilters);
        Assert.Contains(subFilters, f => f.Contains("format=bgra", StringComparison.Ordinal));
        Assert.Contains(subFilters, f => f.Contains("hwupload", StringComparison.Ordinal));
        Assert.Contains(overlayFilters, f => f.Contains("overlay_videotoolbox", StringComparison.Ordinal));
    }

    [Fact]
    public void GetHwaccelType_LiveDvbsubBurnIn_KeepsVideotoolboxHwSurface()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        var sub = new MediaStream { Index = 4, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "rum" };
        var state = BuildLiveDvbsubState(sub);
        state.VideoStream = new MediaStream
        {
            Index = 1,
            Type = MediaStreamType.Video,
            Codec = "hevc",
            PixelFormat = "yuv420p10le",
            Width = 3840,
            Height = 2160,
        };

        var options = new EncodingOptions
        {
            HardwareAccelerationType = HardwareAccelerationType.videotoolbox,
            HardwareDecodingCodecs = ["hevc"],
        };

        var args = CreateVideoToolboxHelper().GetHwaccelType(state, options, "hevc", 10, outputHwSurface: true);

        Assert.Contains("-hwaccel videotoolbox", args, StringComparison.Ordinal);
        Assert.Contains("videotoolbox_vld", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetVideoProcessingFilterParam_LiveDvbsub_UsesInputStreamInFilterComplex()
    {
        var sub = new MediaStream { Index = 2, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "dut" };
        var state = BuildLiveDvbsubState(sub);
        var options = new EncodingOptions { HardwareAccelerationType = HardwareAccelerationType.nvenc };

        var filterParam = CreateHelper().GetVideoProcessingFilterParam(
            state,
            options,
            "h264_nvenc");

        Assert.Contains("-filter_complex", filterParam, StringComparison.Ordinal);
        Assert.Contains("[0:2]", filterParam, StringComparison.Ordinal);
        Assert.DoesNotContain(":si=", filterParam, StringComparison.Ordinal);
    }

    [Fact]
    public void GetNvidiaVidFiltersPrefered_LiveDvbsub_UsesCudaOverlay()
    {
        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "dut" };
        var state = BuildLiveDvbsubState(sub);
        var options = new EncodingOptions { HardwareAccelerationType = HardwareAccelerationType.nvenc };

        var (_, subFilters, overlayFilters) = CreateNvencHelper().GetNvidiaVidFiltersPrefered(
            state,
            options,
            " -hwaccel cuda -c:v h264_cuvid",
            "h264_nvenc");

        Assert.NotEmpty(subFilters);
        Assert.Contains(subFilters, f => f.Contains("format=yuva420p", StringComparison.Ordinal));
        Assert.Contains(subFilters, f => f.Contains("hwupload=derive_device=cuda", StringComparison.Ordinal));
        Assert.Contains(overlayFilters, f => f.Contains("overlay_cuda", StringComparison.Ordinal));
    }

    [Fact]
    public void GetIntelVaapiFullVidFiltersPrefered_LiveDvbsub_UsesVaapiOverlay()
    {
        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "dut" };
        var state = BuildLiveDvbsubState(sub);
        var options = new EncodingOptions { HardwareAccelerationType = HardwareAccelerationType.vaapi };

        var (_, subFilters, overlayFilters) = CreateVaapiHelper().GetIntelVaapiFullVidFiltersPrefered(
            state,
            options,
            "h264_vaapi",
            "h264_vaapi");

        Assert.NotEmpty(subFilters);
        Assert.Contains(subFilters, f => f.Contains("format=bgra", StringComparison.Ordinal));
        Assert.Contains(subFilters, f => f.Contains("hwupload=derive_device=vaapi", StringComparison.Ordinal));
        Assert.Contains(overlayFilters, f => f.Contains("overlay_vaapi", StringComparison.Ordinal));
    }

    [Fact]
    public void GetIntelQsvVaapiVidFiltersPrefered_LiveDvbsub_UsesQsvOverlay()
    {
        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "dut" };
        var state = BuildLiveDvbsubState(sub);
        var options = new EncodingOptions { HardwareAccelerationType = HardwareAccelerationType.qsv };

        var (_, subFilters, overlayFilters) = CreateQsvHelper().GetIntelQsvVaapiVidFiltersPrefered(
            state,
            options,
            "h264_qsv",
            "h264_qsv");

        Assert.NotEmpty(subFilters);
        Assert.Contains(subFilters, f => f.Contains("format=bgra", StringComparison.Ordinal));
        Assert.Contains(subFilters, f => f.Contains("hwupload=derive_device=qsv", StringComparison.Ordinal));
        Assert.Contains(overlayFilters, f => f.Contains("overlay_qsv", StringComparison.Ordinal));
    }

    [Fact]
    public void GetRkmppVidFiltersPrefered_LiveDvbsub_UsesRkrgaOverlay()
    {
        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "dut" };
        var state = BuildLiveDvbsubState(sub);
        var options = new EncodingOptions { HardwareAccelerationType = HardwareAccelerationType.rkmpp };

        var (_, subFilters, overlayFilters) = CreateRkmppHelper().GetRkmppVidFiltersPrefered(
            state,
            options,
            "h264_rkmpp",
            "h264_rkmpp");

        Assert.NotEmpty(subFilters);
        Assert.Contains(subFilters, f => f.Contains("format=bgra", StringComparison.Ordinal));
        Assert.Contains(subFilters, f => f.Contains("hwupload=derive_device=rkmpp", StringComparison.Ordinal));
        Assert.Contains(overlayFilters, f => f.Contains("overlay_rkrga", StringComparison.Ordinal));
    }

    [Fact]
    public void GetHwaccelType_LiveDvbsubBurnIn_KeepsCudaHwSurface()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsWindows())
        {
            return;
        }

        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "dut" };
        var state = BuildLiveDvbsubState(sub);
        state.VideoStream!.PixelFormat = "yuv420p";
        var options = new EncodingOptions
        {
            HardwareAccelerationType = HardwareAccelerationType.nvenc,
            HardwareDecodingCodecs = ["h264"],
        };

        var args = CreateNvencHelper().GetHwaccelType(state, options, "h264", 8, outputHwSurface: true);

        Assert.Contains("-hwaccel cuda", args, StringComparison.Ordinal);
        Assert.Contains("-hwaccel_output_format cuda", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetHwaccelType_LiveDvbsubBurnIn_KeepsVaapiHwSurface()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "dut" };
        var state = BuildLiveDvbsubState(sub);
        state.VideoStream!.PixelFormat = "yuv420p";
        var options = new EncodingOptions
        {
            HardwareAccelerationType = HardwareAccelerationType.vaapi,
            HardwareDecodingCodecs = ["h264"],
        };

        var args = CreateVaapiHelper().GetHwaccelType(state, options, "h264", 8, outputHwSurface: true);

        Assert.Contains("-hwaccel vaapi", args, StringComparison.Ordinal);
        Assert.Contains("-hwaccel_output_format vaapi", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetVideoProcessingFilterParam_Videotoolbox_SkipsFpsFilterWhenFramerateRequested()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "rum" };
        var state = BuildLiveDvbsubState(sub);
        state.BaseRequest.Framerate = 30;
        var options = new EncodingOptions { HardwareAccelerationType = HardwareAccelerationType.videotoolbox };

        var filterParam = CreateVideoToolboxHelper().GetVideoProcessingFilterParam(
            state,
            options,
            "h264_videotoolbox");

        Assert.Contains("scale_vt", filterParam, StringComparison.Ordinal);
        Assert.Contains("overlay_videotoolbox", filterParam, StringComparison.Ordinal);
        Assert.DoesNotContain("fps=30", filterParam, StringComparison.Ordinal);
    }

    [Fact]
    public void GetVideoProcessingFilterParam_Nvenc_SkipsFpsFilterWhenFramerateRequested()
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
        {
            return;
        }

        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "rum" };
        var state = BuildLiveDvbsubState(sub);
        state.VideoStream!.PixelFormat = "yuv420p";
        state.BaseRequest.Framerate = 30;
        var options = new EncodingOptions
        {
            HardwareAccelerationType = HardwareAccelerationType.nvenc,
            HardwareDecodingCodecs = ["h264"],
        };

        var filterParam = CreateNvencHelper().GetVideoProcessingFilterParam(
            state,
            options,
            "h264_nvenc");

        Assert.Contains("scale_cuda", filterParam, StringComparison.Ordinal);
        Assert.Contains("overlay_cuda", filterParam, StringComparison.Ordinal);
        Assert.DoesNotContain("fps=30", filterParam, StringComparison.Ordinal);
    }

    [Fact]
    public void GetVideoProcessingFilterParam_SoftwareFallback_KeepsFpsFilterWhenFramerateRequested()
    {
        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "rum" };
        var state = BuildLiveDvbsubState(sub);
        state.BaseRequest.Framerate = 30;
        var options = new EncodingOptions { HardwareAccelerationType = HardwareAccelerationType.nvenc };

        var filterParam = CreateHelper().GetVideoProcessingFilterParam(
            state,
            options,
            "h264_nvenc");

        Assert.Contains("fps=30", filterParam, StringComparison.Ordinal);
        Assert.Contains("overlay=eof_action=pass", filterParam, StringComparison.Ordinal);
        Assert.DoesNotContain("overlay_cuda", filterParam, StringComparison.Ordinal);
    }

    [Fact]
    public void GetVideoProcessingFilterParam_LiveDvbsubWithDataStream_UsesFfprobeIndices()
    {
        var data = new MediaStream { Index = 0, Type = MediaStreamType.Data, Codec = "epg" };
        var video = new MediaStream { Index = 1, Type = MediaStreamType.Video, Codec = "h264", Width = 1920, Height = 1080 };
        var audio = new MediaStream { Index = 2, Type = MediaStreamType.Audio, Codec = "ac3" };
        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "rum" };
        var state = BuildState(sub, SubtitleDeliveryMethod.Encode, additionalStreams: [data, sub]);
        state.MediaSource.IsInfiniteStream = true;
        state.MediaSource.Path = "https://example.com/live/stream.ts";
        state.VideoStream = video;
        state.AudioStream = audio;
        var options = new EncodingOptions { HardwareAccelerationType = HardwareAccelerationType.videotoolbox };

        var helper = CreateHelper();
        var mapArgs = helper.GetMapArgs(state);
        var filterParam = helper.GetVideoProcessingFilterParam(state, options, "h264_videotoolbox");
        var negativeMapArgs = helper.GetNegativeMapArgsByFilters(state, filterParam);

        Assert.Contains("-map 0:1", mapArgs, StringComparison.Ordinal);
        Assert.Contains("-map 0:2", mapArgs, StringComparison.Ordinal);
        Assert.Equal("-map -0:1 ", negativeMapArgs);
        Assert.Contains("[0:3]", filterParam, StringComparison.Ordinal);
        Assert.Contains("[0:1]", filterParam, StringComparison.Ordinal);
        Assert.DoesNotContain("[0:0]", filterParam, StringComparison.Ordinal);
    }

    [Fact]
    public void GetVideoProcessingFilterParam_LiveDvbtxt_DoesNotBurnIn()
    {
        var sub = new MediaStream { Index = 2, Type = MediaStreamType.Subtitle, Codec = "DVBTXT", Language = "dut", Width = 492, Height = 250 };
        var state = BuildState(sub, SubtitleDeliveryMethod.Encode);
        state.MediaSource.IsInfiniteStream = true;
        state.MediaSource.Path = "https://example.com/live/stream.ts";
        state.VideoStream!.Width = 1920;
        state.VideoStream.Height = 1080;

        var filterParam = CreateHelper().GetVideoProcessingFilterParam(
            state,
            new EncodingOptions(),
            "h264_videotoolbox");

        Assert.DoesNotContain("-filter_complex", filterParam, StringComparison.Ordinal);
        Assert.DoesNotContain("subtitles=f=", filterParam, StringComparison.Ordinal);
        Assert.DoesNotContain("overlay=", filterParam, StringComparison.Ordinal);
    }

    [Fact]
    public void GetMapArgs_NoSubtitle_ExcludesAllSubs()
    {
        var state = BuildState(subtitle: null, deliveryMethod: null);
        var args = CreateHelper().GetMapArgs(state);

        Assert.Contains("-map -0:s", args, StringComparison.Ordinal);
        Assert.DoesNotContain("-map 1:", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetMapArgs_InternalSrt_MapsFromPrimaryInput()
    {
        var sub = new MediaStream { Index = 2, Type = MediaStreamType.Subtitle, Codec = "srt" };
        var state = BuildState(sub, SubtitleDeliveryMethod.Embed);
        var args = CreateHelper().GetMapArgs(state);

        Assert.Contains("-map 0:2", args, StringComparison.Ordinal);
        Assert.DoesNotContain("-map 1:", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetMapArgs_InternalSubAtHigherIndex_MapsCorrectIndex()
    {
        var sub0 = new MediaStream { Index = 2, Type = MediaStreamType.Subtitle, Codec = "srt" };
        var sub1 = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "ass" };
        var state = BuildState(sub1, SubtitleDeliveryMethod.Embed, additionalStreams: [sub0, sub1]);
        var args = CreateHelper().GetMapArgs(state);

        Assert.Contains("-map 0:3", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetMapArgs_ExternalSrt_MapsFirstStreamFromInput1()
    {
        var sub = new MediaStream
        {
            Index = 2,
            Type = MediaStreamType.Subtitle,
            Codec = "srt",
            IsExternal = true,
            SupportsExternalStream = true,
            Path = "/media/movie.en.srt"
        };
        var state = BuildState(sub, SubtitleDeliveryMethod.Embed);
        var args = CreateHelper().GetMapArgs(state);

        Assert.Contains("-map 1:0", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetMapArgs_SecondExternalSrt_StillMaps1Colon0()
    {
        // Two separate .srt files — selecting the second one still maps 1:0
        // because Jellyfin feeds only the selected file as ffmpeg input 1.
        var ext1 = new MediaStream
        {
            Index = 2,
            Type = MediaStreamType.Subtitle,
            Codec = "srt",
            IsExternal = true,
            SupportsExternalStream = true,
            Path = "/media/movie.en.srt"
        };
        var ext2 = new MediaStream
        {
            Index = 3,
            Type = MediaStreamType.Subtitle,
            Codec = "srt",
            IsExternal = true,
            SupportsExternalStream = true,
            Path = "/media/movie.fr.srt"
        };
        var state = BuildState(ext2, SubtitleDeliveryMethod.Embed, additionalStreams: [ext1, ext2]);
        var args = CreateHelper().GetMapArgs(state);

        Assert.Contains("-map 1:0", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetMapArgs_MksFirstTrack_MapsInFileIndex0()
    {
        var mks0 = new MediaStream
        {
            Index = 2,
            Type = MediaStreamType.Subtitle,
            Codec = "subrip",
            IsExternal = true,
            SupportsExternalStream = true,
            Path = "/media/movie.mks"
        };
        var mks1 = new MediaStream
        {
            Index = 3,
            Type = MediaStreamType.Subtitle,
            Codec = "ass",
            IsExternal = true,
            SupportsExternalStream = true,
            Path = "/media/movie.mks"
        };
        var state = BuildState(mks0, SubtitleDeliveryMethod.Embed, additionalStreams: [mks0, mks1]);
        var args = CreateHelper().GetMapArgs(state);

        Assert.Contains("-map 1:0", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetMapArgs_MksSecondTrack_MapsInFileIndex1()
    {
        var mks0 = new MediaStream
        {
            Index = 2,
            Type = MediaStreamType.Subtitle,
            Codec = "subrip",
            IsExternal = true,
            SupportsExternalStream = true,
            Path = "/media/movie.mks"
        };
        var mks1 = new MediaStream
        {
            Index = 3,
            Type = MediaStreamType.Subtitle,
            Codec = "ass",
            IsExternal = true,
            SupportsExternalStream = true,
            Path = "/media/movie.mks"
        };
        var mks2 = new MediaStream
        {
            Index = 4,
            Type = MediaStreamType.Subtitle,
            Codec = "subrip",
            IsExternal = true,
            SupportsExternalStream = true,
            Path = "/media/movie.mks"
        };
        var state = BuildState(mks1, SubtitleDeliveryMethod.Embed, additionalStreams: [mks0, mks1, mks2]);
        var args = CreateHelper().GetMapArgs(state);

        Assert.Contains("-map 1:1", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetMapArgs_ExternalSrtBeforeVideoAndAudio_MapsInFileStreamIndices()
    {
        var state = BuildExternalSrtBeforeVideoState(SubtitleDeliveryMethod.Embed);
        var args = CreateHelper().GetMapArgs(state);

        Assert.Contains("-map 0:0", args, StringComparison.Ordinal);
        Assert.Contains("-map 0:1", args, StringComparison.Ordinal);
        Assert.Contains("-map 1:0", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetMapArgs_ExternalSrtBeforeVideoAndAudio_InternalSubtitleEmbed_MapsInFileIndices()
    {
        var sub = new MediaStream
        {
            Index = 0,
            Type = MediaStreamType.Subtitle,
            Codec = "srt",
            IsExternal = true,
            SupportsExternalStream = true,
            Path = "/media/movie.ron.srt",
        };
        var video = new MediaStream { Index = 1, Type = MediaStreamType.Video, Codec = "h264", Path = "/media/movie.mkv" };
        var audio = new MediaStream { Index = 2, Type = MediaStreamType.Audio, Codec = "ac3", Path = "/media/movie.mkv" };
        var internalSub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "ass", Path = "/media/movie.mkv" };
        var state = new EncodingJobInfo(TranscodingJobType.Progressive)
        {
            MediaSource = new MediaSourceInfo
            {
                Container = "mkv",
                MediaStreams = [sub, video, audio, internalSub],
            },
            VideoStream = video,
            AudioStream = audio,
            SubtitleStream = internalSub,
            SubtitleDeliveryMethod = SubtitleDeliveryMethod.Embed,
            BaseRequest = new VideoRequestDto(),
            IsVideoRequest = true,
            IsInputVideo = true,
        };

        var args = CreateHelper().GetMapArgs(state);

        Assert.Contains("-map 0:0", args, StringComparison.Ordinal);
        Assert.Contains("-map 0:1", args, StringComparison.Ordinal);
        Assert.Contains("-map 0:2", args, StringComparison.Ordinal);
        Assert.DoesNotContain("-map 1:", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetVideoProcessingFilterParam_ExternalSrtBeforeVideoWithEncode_UsesExternalSubtitleFile()
    {
        var state = BuildExternalSrtBeforeVideoState(SubtitleDeliveryMethod.Encode);
        state.VideoStream!.Width = 1920;
        state.VideoStream.Height = 1080;

        var filterParam = CreateHelper().GetVideoProcessingFilterParam(state, new EncodingOptions(), "libx264");

        Assert.Contains("-vf", filterParam, StringComparison.Ordinal);
        Assert.Contains("subtitles=f=", filterParam, StringComparison.Ordinal);
        Assert.Contains("movie.ron.srt", filterParam, StringComparison.Ordinal);
        Assert.DoesNotContain("-filter_complex", filterParam, StringComparison.Ordinal);
    }

    [Fact]
    public void GetMapArgs_LiveMpegTsWithoutExternalStream_UsesFfprobeStreamIndex()
    {
        var data = new MediaStream { Index = 0, Type = MediaStreamType.Data, Codec = "epg" };
        var video = new MediaStream { Index = 1, Type = MediaStreamType.Video, Codec = "h264" };
        var audio = new MediaStream { Index = 2, Type = MediaStreamType.Audio, Codec = "ac3" };
        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "rum" };
        var state = BuildState(sub, SubtitleDeliveryMethod.Encode, additionalStreams: [data, sub]);
        state.MediaSource.IsInfiniteStream = true;
        state.VideoStream = video;
        state.AudioStream = audio;

        var args = CreateHelper().GetMapArgs(state);

        Assert.Contains("-map 0:1", args, StringComparison.Ordinal);
        Assert.Contains("-map 0:2", args, StringComparison.Ordinal);
        Assert.DoesNotContain("-map 0:0", args, StringComparison.Ordinal);
    }

    [Fact]
    public void FindIndex_ExternalSrtListedFirst_ReturnsInFileIndexForMainStreams()
    {
        var sub = new MediaStream
        {
            Index = 0,
            Type = MediaStreamType.Subtitle,
            Codec = "srt",
            IsExternal = true,
            Path = "/media/movie.ron.srt",
        };
        var video = new MediaStream { Index = 1, Type = MediaStreamType.Video, Codec = "h264", Path = "/media/movie.mkv" };
        var audio = new MediaStream { Index = 2, Type = MediaStreamType.Audio, Codec = "ac3", Path = "/media/movie.mkv" };
        var streams = new List<MediaStream> { sub, video, audio };

        Assert.Equal(0, EncodingHelper.FindIndex(streams, video));
        Assert.Equal(1, EncodingHelper.FindIndex(streams, audio));
    }

    [Fact]
    public void FindIndex_MksMultipleTracks_CountsWithinSameFile()
    {
        var mks0 = new MediaStream
        {
            Index = 2,
            Type = MediaStreamType.Subtitle,
            Codec = "subrip",
            IsExternal = true,
            Path = "/media/movie.mks",
        };
        var mks1 = new MediaStream
        {
            Index = 3,
            Type = MediaStreamType.Subtitle,
            Codec = "ass",
            IsExternal = true,
            Path = "/media/movie.mks",
        };
        var streams = new List<MediaStream> { mks0, mks1 };

        Assert.Equal(0, EncodingHelper.FindIndex(streams, mks0));
        Assert.Equal(1, EncodingHelper.FindIndex(streams, mks1));
    }

    [Fact]
    public void TryStreamCopy_LiveHlsMp2WithDvbsubBurnIn_UsesAudioCopyWhenClientSupportsMp2()
    {
        var video = new MediaStream { Index = 0, Type = MediaStreamType.Video, Codec = "h264", Width = 720, Height = 576, IsInterlaced = true };
        var audio = new MediaStream { Index = 1, Type = MediaStreamType.Audio, Codec = "mp2", Channels = 2 };
        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "eng" };
        var state = new EncodingJobInfo(TranscodingJobType.Hls)
        {
            MediaSource = new MediaSourceInfo
            {
                Container = "ts",
                IsInfiniteStream = true,
                MediaStreams = [video, audio, sub],
            },
            VideoStream = video,
            AudioStream = audio,
            SubtitleStream = sub,
            SubtitleDeliveryMethod = SubtitleDeliveryMethod.Encode,
            BaseRequest = new VideoRequestDto { AllowAudioStreamCopy = true, VideoCodec = "libx264" },
            IsVideoRequest = true,
            SupportedVideoCodecs = ["h264"],
            SupportedAudioCodecs = ["mp2", "aac"],
            OutputVideoCodec = "copy",
        };

        CreateHelper().TryStreamCopy(state, new EncodingOptions());

        Assert.Equal("copy", state.OutputAudioCodec);
    }

    [Fact]
    public void TryStreamCopy_LiveHlsMp2WithDvbsubBurnIn_TranscodesAudioWhenClientRequiresAac()
    {
        var video = new MediaStream { Index = 0, Type = MediaStreamType.Video, Codec = "h264", Width = 720, Height = 576, IsInterlaced = true };
        var audio = new MediaStream { Index = 1, Type = MediaStreamType.Audio, Codec = "mp2", Channels = 2 };
        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "eng" };
        var state = new EncodingJobInfo(TranscodingJobType.Hls)
        {
            MediaSource = new MediaSourceInfo
            {
                Container = "ts",
                IsInfiniteStream = true,
                MediaStreams = [video, audio, sub],
            },
            VideoStream = video,
            AudioStream = audio,
            SubtitleStream = sub,
            SubtitleDeliveryMethod = SubtitleDeliveryMethod.Encode,
            BaseRequest = new VideoRequestDto { AllowAudioStreamCopy = true, VideoCodec = "libx264", AudioCodec = "aac" },
            IsVideoRequest = true,
            SupportedVideoCodecs = ["h264"],
            SupportedAudioCodecs = ["aac"],
            OutputVideoCodec = "copy",
        };

        CreateHelper().TryStreamCopy(state, new EncodingOptions());

        Assert.NotEqual("copy", state.OutputAudioCodec);
    }

    [Theory]
    [InlineData(SubtitleDeliveryMethod.Embed, true, "movie.idx")]
    [InlineData(SubtitleDeliveryMethod.Encode, true, "movie.idx")]
    [InlineData(SubtitleDeliveryMethod.Embed, false, "movie.sub")]
    [InlineData(SubtitleDeliveryMethod.Encode, false, "movie.sub")]
    public void GetInputArgument_VobSub_UsesCorrectPath(
        SubtitleDeliveryMethod deliveryMethod,
        bool createIdxFile,
        string expectedFilename)
    {
        var tempDir = Directory.CreateTempSubdirectory("jellyfin-test-");
        try
        {
            var subFile = Path.Combine(tempDir.FullName, "movie.sub");
            File.WriteAllText(subFile, "dummy");

            if (createIdxFile)
            {
                File.WriteAllText(Path.Combine(tempDir.FullName, "movie.idx"), "dummy");
            }

            var sub = new MediaStream
            {
                Index = 2,
                Type = MediaStreamType.Subtitle,
                Codec = "dvdsub",
                IsExternal = true,
                SupportsExternalStream = true,
                Path = subFile
            };
            var state = BuildState(sub, deliveryMethod);
            var inputArgs = CreateHelper().GetInputArgument(state, new EncodingOptions(), null);

            Assert.Contains(expectedFilename, inputArgs, StringComparison.Ordinal);
        }
        finally
        {
            tempDir.Delete(true);
        }
    }

    private static EncodingJobInfo BuildExternalSrtBeforeVideoState(SubtitleDeliveryMethod deliveryMethod)
    {
        // Sidecar SRT is probed first, so Jellyfin indices are 0=sub, 1=video, 2=audio
        // while the MKV only has 0=video, 1=audio.
        var sub = new MediaStream
        {
            Index = 0,
            Type = MediaStreamType.Subtitle,
            Codec = "srt",
            IsExternal = true,
            SupportsExternalStream = true,
            Path = "/media/movie.ron.srt",
        };
        var video = new MediaStream { Index = 1, Type = MediaStreamType.Video, Codec = "h264", Path = "/media/movie.mkv" };
        var audio = new MediaStream { Index = 2, Type = MediaStreamType.Audio, Codec = "ac3", Path = "/media/movie.mkv" };

        return new EncodingJobInfo(TranscodingJobType.Progressive)
        {
            MediaSource = new MediaSourceInfo
            {
                Container = "mkv",
                MediaStreams = [sub, video, audio],
            },
            VideoStream = video,
            AudioStream = audio,
            SubtitleStream = sub,
            SubtitleDeliveryMethod = deliveryMethod,
            BaseRequest = new VideoRequestDto(),
            IsVideoRequest = true,
            IsInputVideo = true,
        };
    }

    private static EncodingJobInfo BuildState(
        MediaStream? subtitle,
        SubtitleDeliveryMethod? deliveryMethod,
        MediaStream[]? additionalStreams = null)
    {
        var video = new MediaStream { Index = 0, Type = MediaStreamType.Video, Codec = "h264" };
        var audio = new MediaStream { Index = 1, Type = MediaStreamType.Audio, Codec = "aac" };
        var streams = new List<MediaStream> { video, audio };

        if (additionalStreams is not null)
        {
            streams.AddRange(additionalStreams);
        }
        else if (subtitle is not null)
        {
            streams.Add(subtitle);
        }

        return new EncodingJobInfo(TranscodingJobType.Progressive)
        {
            MediaSource = new MediaSourceInfo
            {
                Container = "mkv",
                MediaStreams = streams,
            },
            VideoStream = video,
            AudioStream = audio,
            SubtitleStream = subtitle,
            SubtitleDeliveryMethod = deliveryMethod ?? SubtitleDeliveryMethod.Drop,
            BaseRequest = new VideoRequestDto(),
            IsVideoRequest = true,
            IsInputVideo = true,
        };
    }

    private static EncodingJobInfo BuildLiveDvbsubState(MediaStream sub)
    {
        var state = BuildState(sub, SubtitleDeliveryMethod.Encode);
        state.MediaSource.IsInfiniteStream = true;
        state.MediaSource.Path = "https://example.com/live/stream.ts";
        state.VideoStream!.Width = 1920;
        state.VideoStream.Height = 1080;
        return state;
    }

    [Fact]
    public void GetMinimumH264Level_720x576At50Fps_RequiresLevel31()
    {
        Assert.Equal(31, EncodingHelper.GetMinimumH264Level(720, 576, 50));
    }

    [Fact]
    public void GetMinimumH264Level_720x576At25Fps_FitsLevel30()
    {
        Assert.Equal(30, EncodingHelper.GetMinimumH264Level(720, 576, 25));
    }

    [Fact]
    public void AdjustH264TranscodingLevelForOutput_DoubleRateDeinterlace_BumpsLevel()
    {
        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "eng" };
        var state = BuildLiveDvbsubState(sub);
        state.OutputVideoCodec = "h264";
        state.VideoStream!.Width = 720;
        state.VideoStream.Height = 576;
        state.VideoStream.IsInterlaced = true;
        state.VideoStream.AverageFrameRate = 25;
        state.BaseRequest = new VideoRequestDto { Level = "30" };

        var options = new EncodingOptions { DeinterlaceDoubleRate = true };
        var adjusted = EncodingHelper.AdjustH264TranscodingLevelForOutput(state, options, "30");

        Assert.Equal("31", adjusted);
    }

    [Fact]
    public void AdjustH264TranscodingLevelForOutput_SingleRateDeinterlace_KeepsLevel30()
    {
        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "eng" };
        var state = BuildLiveDvbsubState(sub);
        state.OutputVideoCodec = "h264";
        state.VideoStream!.Width = 720;
        state.VideoStream.Height = 576;
        state.VideoStream.IsInterlaced = true;
        state.VideoStream.AverageFrameRate = 25;
        state.BaseRequest = new VideoRequestDto { Level = "30" };

        var options = new EncodingOptions { DeinterlaceDoubleRate = false };
        var adjusted = EncodingHelper.AdjustH264TranscodingLevelForOutput(state, options, "30");

        Assert.Equal("30", adjusted);
    }

    [Fact]
    public void ShouldDisableStartAtZeroForSubtitleTranscode_LiveDvbsub_ReturnsTrue()
    {
        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "eng" };
        var state = BuildLiveDvbsubState(sub);

        Assert.True(EncodingHelper.ShouldDisableStartAtZeroForSubtitleTranscode(state));
    }

    [Fact]
    public void ShouldDisableStartAtZeroForSubtitleTranscode_ExternalSrt_ReturnsFalse()
    {
        var state = BuildExternalSrtBeforeVideoState(SubtitleDeliveryMethod.Encode);

        Assert.False(EncodingHelper.ShouldDisableStartAtZeroForSubtitleTranscode(state));
    }

    [Fact]
    public void GetHlsSubtitleStartAtZeroArg_LiveDvbsub_OmitsFlag()
    {
        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "eng" };
        var state = BuildLiveDvbsubState(sub);

        Assert.Equal(string.Empty, EncodingHelper.GetHlsSubtitleStartAtZeroArg(state));
    }

    [Fact]
    public void GetHlsSubtitleStartAtZeroArg_ExternalTextSub_IncludesFlag()
    {
        var state = BuildExternalSrtBeforeVideoState(SubtitleDeliveryMethod.Encode);

        Assert.Equal(" -start_at_zero", EncodingHelper.GetHlsSubtitleStartAtZeroArg(state));
    }

    [Fact]
    public void GetVideoQualityParam_LiveDvbsubDoubleRateDeinterlace_EmitsLevel31()
    {
        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "eng" };
        var state = BuildLiveDvbsubState(sub);
        state.TranscodingType = TranscodingJobType.Hls;
        state.OutputVideoCodec = "h264";
        state.VideoStream!.Width = 720;
        state.VideoStream.Height = 576;
        state.VideoStream.IsInterlaced = true;
        state.VideoStream.AverageFrameRate = 25;
        state.VideoStream.Level = 30;
        state.BaseRequest = new VideoRequestDto { Level = "30" };

        var options = new EncodingOptions { DeinterlaceDoubleRate = true };
        var param = CreateHelper().GetVideoQualityParam(
            state,
            "libx264",
            options,
            EncoderPreset.veryfast);

        Assert.Contains("-level 31", param, StringComparison.Ordinal);
    }

    [Fact]
    public void DiscardCorruptFramesInput_LiveStream_IsEnabled()
    {
        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "eng" };
        var state = BuildLiveDvbsubState(sub);

        Assert.True(state.DiscardCorruptFramesInput);
    }

    [Fact]
    public void GetInputModifier_LiveStream_IncludesDiscardCorrupt()
    {
        var sub = new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "eng" };
        var state = BuildLiveDvbsubState(sub);
        state.MediaSource.IgnoreDts = true;
        state.MediaSource.Protocol = MediaProtocol.Http;
        state.MediaSource.Path = "http://example.com/stream.ts";

        var mediaEncoder = new Mock<IMediaEncoder>();
        mediaEncoder.Setup(m => m.EncoderVersion).Returns(new Version(7, 1, 3));
        mediaEncoder
            .Setup(m => m.EscapeSubtitleFilterPath(It.IsAny<string>()))
            .Returns<string>(path => path);

        var helper = new EncodingHelper(
            Mock.Of<IApplicationPaths>(),
            mediaEncoder.Object,
            Mock.Of<ISubtitleEncoder>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<IConfigurationManager>(),
            Mock.Of<IPathManager>());

        var modifier = helper.GetInputModifier(state, new EncodingOptions(), null);

        Assert.Contains("+discardcorrupt", modifier, StringComparison.Ordinal);
        Assert.Contains("+igndts", modifier, StringComparison.Ordinal);
    }

    private static EncodingHelper CreateHelper()
    {
        var mediaEncoder = CreateBaseMediaEncoderMock();
        return CreateEncodingHelper(mediaEncoder);
    }

    private static Mock<IMediaEncoder> CreateBaseMediaEncoderMock()
    {
        var mediaEncoder = new Mock<IMediaEncoder>();
        mediaEncoder
            .Setup(m => m.EscapeSubtitleFilterPath(It.IsAny<string>()))
            .Returns<string>(path => path);
        return mediaEncoder;
    }

    private static EncodingHelper CreateEncodingHelper(Mock<IMediaEncoder> mediaEncoder)
    {
        return new EncodingHelper(
            Mock.Of<IApplicationPaths>(),
            mediaEncoder.Object,
            Mock.Of<ISubtitleEncoder>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<IConfigurationManager>(),
            Mock.Of<IPathManager>());
    }

    private static void SetupOpenclSupport(Mock<IMediaEncoder> mediaEncoder)
    {
        mediaEncoder.Setup(m => m.SupportsHwaccel("opencl")).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilter("scale_opencl")).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilterWithOption(FilterOptionType.TonemapOpenclBt2390)).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilterWithOption(FilterOptionType.OverlayOpenclFrameSync)).Returns(true);
    }

    private static void SetupCudaSupport(Mock<IMediaEncoder> mediaEncoder)
    {
        mediaEncoder.Setup(m => m.SupportsHwaccel("cuda")).Returns(true);
        mediaEncoder.Setup(m => m.SupportsDecoder(It.IsAny<string>())).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilterWithOption(FilterOptionType.ScaleCudaFormat)).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilter("yadif_cuda")).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilterWithOption(FilterOptionType.TonemapCudaName)).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilter("overlay_cuda")).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilter("hwupload_cuda")).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilterWithOption(FilterOptionType.OverlayCudaAlphaFormat)).Returns(true);
    }

    private static void SetupVaapiSupport(Mock<IMediaEncoder> mediaEncoder)
    {
        mediaEncoder.Setup(m => m.SupportsHwaccel("drm")).Returns(true);
        mediaEncoder.Setup(m => m.SupportsHwaccel("vaapi")).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilter("scale_vaapi")).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilter("deinterlace_vaapi")).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilter("tonemap_vaapi")).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilter("procamp_vaapi")).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilterWithOption(FilterOptionType.OverlayVaapiFrameSync)).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilter("transpose_vaapi")).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilter("hwupload_vaapi")).Returns(true);
        mediaEncoder.Setup(m => m.IsVaapiDeviceInteliHD).Returns(true);
    }

    private static void SetupRkmppSupport(Mock<IMediaEncoder> mediaEncoder)
    {
        mediaEncoder.Setup(m => m.SupportsHwaccel("rkmpp")).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilter("scale_rkrga")).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilter("vpp_rkrga")).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilter("overlay_rkrga")).Returns(true);
    }

    private static EncodingHelper CreateNvencHelper()
    {
        var mediaEncoder = CreateBaseMediaEncoderMock();
        SetupCudaSupport(mediaEncoder);
        mediaEncoder.Setup(m => m.SupportsFilter("alphasrc")).Returns(true);
        mediaEncoder.Setup(m => m.EncoderVersion).Returns(new Version(7, 1, 3));
        return CreateEncodingHelper(mediaEncoder);
    }

    private static EncodingHelper CreateVaapiHelper()
    {
        var mediaEncoder = CreateBaseMediaEncoderMock();
        SetupVaapiSupport(mediaEncoder);
        SetupOpenclSupport(mediaEncoder);
        mediaEncoder.Setup(m => m.SupportsFilter("alphasrc")).Returns(true);
        mediaEncoder.Setup(m => m.EncoderVersion).Returns(new Version(7, 1, 3));
        return CreateEncodingHelper(mediaEncoder);
    }

    private static EncodingHelper CreateQsvHelper()
    {
        var mediaEncoder = CreateBaseMediaEncoderMock();
        SetupVaapiSupport(mediaEncoder);
        SetupOpenclSupport(mediaEncoder);
        mediaEncoder.Setup(m => m.SupportsHwaccel("qsv")).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilter("alphasrc")).Returns(true);
        mediaEncoder.Setup(m => m.EncoderVersion).Returns(new Version(7, 1, 3));
        return CreateEncodingHelper(mediaEncoder);
    }

    private static EncodingHelper CreateRkmppHelper()
    {
        var mediaEncoder = CreateBaseMediaEncoderMock();
        SetupRkmppSupport(mediaEncoder);
        SetupOpenclSupport(mediaEncoder);
        mediaEncoder.Setup(m => m.SupportsFilter("alphasrc")).Returns(true);
        mediaEncoder.Setup(m => m.EncoderVersion).Returns(new Version(7, 1, 3));
        return CreateEncodingHelper(mediaEncoder);
    }

    private static EncodingHelper CreateVideoToolboxHelper()
    {
        var mediaEncoder = CreateBaseMediaEncoderMock();
        mediaEncoder.Setup(m => m.SupportsHwaccel("videotoolbox")).Returns(true);
        mediaEncoder.Setup(m => m.SupportsDecoder(It.IsAny<string>())).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilter("alphasrc")).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilter("yadif_videotoolbox")).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilter("overlay_videotoolbox")).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilter("tonemap_videotoolbox")).Returns(true);
        mediaEncoder.Setup(m => m.SupportsFilter("scale_vt")).Returns(true);
        mediaEncoder.Setup(m => m.EncoderVersion).Returns(new Version(7, 1, 3));
        return CreateEncodingHelper(mediaEncoder);
    }
}
