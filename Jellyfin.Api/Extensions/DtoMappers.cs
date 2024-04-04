using System;
using Jellyfin.Api.Models.Requests;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dlna;

namespace Jellyfin.Api.Extensions;

internal static class DtoMappers
{
    internal static VideoRequestDto ToDomain(this VideoStreamRequest request, Guid itemId)
    {
        var streamingRequest = new VideoRequestDto
        {
            Id = itemId,
            Container = request.Container,
            Static = request.Static ?? false,
            Params = request.Params,
            Tag = request.Tag,
            PlaySessionId = request.PlaySessionId,
            SegmentContainer = request.SegmentContainer,
            SegmentLength = request.SegmentLength,
            MinSegments = request.MinSegments,
            MediaSourceId = request.MediaSourceId,
            DeviceId = request.DeviceId,
            AudioCodec = request.AudioCodec,
            EnableAutoStreamCopy = request.EnableAutoStreamCopy ?? true,
            AllowAudioStreamCopy = request.AllowAudioStreamCopy ?? true,
            AllowVideoStreamCopy = request.AllowVideoStreamCopy ?? true,
            BreakOnNonKeyFrames = request.BreakOnNonKeyFrames ?? false,
            AudioSampleRate = request.AudioSampleRate,
            MaxAudioChannels = request.MaxAudioChannels,
            AudioBitRate = request.AudioBitRate,
            MaxAudioBitDepth = request.MaxAudioBitDepth,
            AudioChannels = request.AudioChannels,
            Profile = request.Profile,
            Level = request.Level,
            Framerate = request.Framerate,
            MaxFramerate = request.MaxFramerate,
            CopyTimestamps = request.CopyTimestamps ?? false,
            StartTimeTicks = request.StartTimeTicks,
            Width = request.Width,
            Height = request.Height,
            MaxWidth = request.MaxWidth,
            MaxHeight = request.MaxHeight,
            VideoBitRate = request.VideoBitRate,
            SubtitleStreamIndex = request.SubtitleStremaIndex,
            SubtitleMethod = request.SubtitleDeliveryMethod ?? SubtitleDeliveryMethod.Encode,
            MaxRefFrames = request.MaxRefFrames,
            MaxVideoBitDepth = request.MaxVideoBitDepth,
            RequireAvc = request.RequireAvc ?? false,
            DeInterlace = request.DeInterlace ?? false,
            RequireNonAnamorphic = request.RequireNoAnamorphic ?? false,
            TranscodingMaxAudioChannels = request.MaxAudioChannels,
            CpuCoreLimit = request.CpuCoreLimit,
            LiveStreamId = request.LiveStreamId,
            EnableMpegtsM2TsMode = request.EnableMpegM2TsMode ?? false,
            VideoCodec = request.VideoCodec,
            SubtitleCodec = request.SubtitleCodec,
            TranscodeReasons = request.TranscodeReasons,
            AudioStreamIndex = request.AudioStreamIndex,
            VideoStreamIndex = request.VideoStreamIndex,
            Context = request.Context ?? EncodingContext.Streaming,
            StreamOptions = request.StreamOptions
        };

        return streamingRequest;
    }
}
