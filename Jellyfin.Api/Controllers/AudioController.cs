using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.StreamingDtos;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dlna;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// The audio controller.
/// </summary>
public class AudioController : BaseJellyfinApiController
{
    private readonly AudioHelper _audioHelper;

    private readonly TranscodingJobType _transcodingJobType = TranscodingJobType.Progressive;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioController"/> class.
    /// </summary>
    /// <param name="audioHelper">Instance of <see cref="AudioHelper"/>.</param>
    public AudioController(AudioHelper audioHelper)
    {
        _audioHelper = audioHelper;
    }

    /// <summary>
    /// Gets an audio stream.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="container">The audio container.</param>
    /// <param name="static">Optional. If true, the original file will be streamed statically without any encoding. Use either no url extension or the original file extension. true/false.</param>
    /// <param name="params">The streaming parameters.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="deviceProfileId">Optional. The dlna device profile id to utilize.</param>
    /// <param name="playSessionId">The play session id.</param>
    /// <param name="segmentContainer">The segment container.</param>
    /// <param name="segmentLength">The segment length.</param>
    /// <param name="minSegments">The minimum number of segments.</param>
    /// <param name="mediaSourceId">The media version id, if playing an alternate version.</param>
    /// <param name="deviceId">The device id of the client requesting. Used to stop encoding processes when needed.</param>
    /// <param name="audioCodec">Optional. Specify an audio codec to encode to, e.g. mp3. If omitted the server will auto-select using the url's extension.</param>
    /// <param name="enableAutoStreamCopy">Whether or not to allow automatic stream copy if requested values match the original source. Defaults to true.</param>
    /// <param name="allowVideoStreamCopy">Whether or not to allow copying of the video stream url.</param>
    /// <param name="allowAudioStreamCopy">Whether or not to allow copying of the audio stream url.</param>
    /// <param name="breakOnNonKeyFrames">Optional. Whether to break on non key frames.</param>
    /// <param name="audioSampleRate">Optional. Specify a specific audio sample rate, e.g. 44100.</param>
    /// <param name="maxAudioBitDepth">Optional. The maximum audio bit depth.</param>
    /// <param name="audioBitRate">Optional. Specify an audio bitrate to encode to, e.g. 128000. If omitted this will be left to encoder defaults.</param>
    /// <param name="audioChannels">Optional. Specify a specific number of audio channels to encode to, e.g. 2.</param>
    /// <param name="maxAudioChannels">Optional. Specify a maximum number of audio channels to encode to, e.g. 2.</param>
    /// <param name="profile">Optional. Specify a specific an encoder profile (varies by encoder), e.g. main, baseline, high.</param>
    /// <param name="level">Optional. Specify a level for the encoder profile (varies by encoder), e.g. 3, 3.1.</param>
    /// <param name="framerate">Optional. A specific video framerate to encode to, e.g. 23.976. Generally this should be omitted unless the device has specific requirements.</param>
    /// <param name="maxFramerate">Optional. A specific maximum video framerate to encode to, e.g. 23.976. Generally this should be omitted unless the device has specific requirements.</param>
    /// <param name="copyTimestamps">Whether or not to copy timestamps when transcoding with an offset. Defaults to false.</param>
    /// <param name="startTimeTicks">Optional. Specify a starting offset, in ticks. 1 tick = 10000 ms.</param>
    /// <param name="width">Optional. The fixed horizontal resolution of the encoded video.</param>
    /// <param name="height">Optional. The fixed vertical resolution of the encoded video.</param>
    /// <param name="videoBitRate">Optional. Specify a video bitrate to encode to, e.g. 500000. If omitted this will be left to encoder defaults.</param>
    /// <param name="subtitleStreamIndex">Optional. The index of the subtitle stream to use. If omitted no subtitles will be used.</param>
    /// <param name="subtitleMethod">Optional. Specify the subtitle delivery method.</param>
    /// <param name="maxRefFrames">Optional.</param>
    /// <param name="maxVideoBitDepth">Optional. The maximum video bit depth.</param>
    /// <param name="requireAvc">Optional. Whether to require avc.</param>
    /// <param name="deInterlace">Optional. Whether to deinterlace the video.</param>
    /// <param name="requireNonAnamorphic">Optional. Whether to require a non anamorphic stream.</param>
    /// <param name="transcodingMaxAudioChannels">Optional. The maximum number of audio channels to transcode.</param>
    /// <param name="cpuCoreLimit">Optional. The limit of how many cpu cores to use.</param>
    /// <param name="liveStreamId">The live stream id.</param>
    /// <param name="enableMpegtsM2TsMode">Optional. Whether to enable the MpegtsM2Ts mode.</param>
    /// <param name="videoCodec">Optional. Specify a video codec to encode to, e.g. h264. If omitted the server will auto-select using the url's extension.</param>
    /// <param name="subtitleCodec">Optional. Specify a subtitle codec to encode to.</param>
    /// <param name="transcodeReasons">Optional. The transcoding reason.</param>
    /// <param name="audioStreamIndex">Optional. The index of the audio stream to use. If omitted the first audio stream will be used.</param>
    /// <param name="videoStreamIndex">Optional. The index of the video stream to use. If omitted the first video stream will be used.</param>
    /// <param name="context">Optional. The <see cref="EncodingContext"/>.</param>
    /// <param name="streamOptions">Optional. The streaming options.</param>
    /// <param name="enableAudioVbrEncoding">Optional. Whether to enable Audio Encoding.</param>
    /// <response code="200">Audio stream returned.</response>
    /// <returns>A <see cref="FileResult"/> containing the audio file.</returns>
    [HttpGet("{itemId}/stream", Name = "GetAudioStream")]
    [HttpHead("{itemId}/stream", Name = "HeadAudioStream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesAudioFile]
    public async Task<ActionResult> GetAudioStream(
        [FromRoute, Required] Guid itemId,
        [FromQuery] [RegularExpression(EncodingHelper.ContainerValidationRegex)] string? container,
        [FromQuery] bool? @static,
        [FromQuery] string? @params,
        [FromQuery] string? tag,
        [FromQuery, ParameterObsolete] string? deviceProfileId,
        [FromQuery] string? playSessionId,
        [FromQuery] [RegularExpression(EncodingHelper.ContainerValidationRegex)] string? segmentContainer,
        [FromQuery] int? segmentLength,
        [FromQuery] int? minSegments,
        [FromQuery] string? mediaSourceId,
        [FromQuery] string? deviceId,
        [FromQuery] [RegularExpression(EncodingHelper.ContainerValidationRegex)] string? audioCodec,
        [FromQuery] bool? enableAutoStreamCopy,
        [FromQuery] bool? allowVideoStreamCopy,
        [FromQuery] bool? allowAudioStreamCopy,
        [FromQuery] bool? breakOnNonKeyFrames,
        [FromQuery] int? audioSampleRate,
        [FromQuery] int? maxAudioBitDepth,
        [FromQuery] int? audioBitRate,
        [FromQuery] int? audioChannels,
        [FromQuery] int? maxAudioChannels,
        [FromQuery] string? profile,
        [FromQuery] [RegularExpression(EncodingHelper.LevelValidationRegex)] string? level,
        [FromQuery] float? framerate,
        [FromQuery] float? maxFramerate,
        [FromQuery] bool? copyTimestamps,
        [FromQuery] long? startTimeTicks,
        [FromQuery] int? width,
        [FromQuery] int? height,
        [FromQuery] int? videoBitRate,
        [FromQuery] int? subtitleStreamIndex,
        [FromQuery] SubtitleDeliveryMethod? subtitleMethod,
        [FromQuery] int? maxRefFrames,
        [FromQuery] int? maxVideoBitDepth,
        [FromQuery] bool? requireAvc,
        [FromQuery] bool? deInterlace,
        [FromQuery] bool? requireNonAnamorphic,
        [FromQuery] int? transcodingMaxAudioChannels,
        [FromQuery] int? cpuCoreLimit,
        [FromQuery] string? liveStreamId,
        [FromQuery] bool? enableMpegtsM2TsMode,
        [FromQuery] [RegularExpression(EncodingHelper.ContainerValidationRegex)] string? videoCodec,
        [FromQuery] [RegularExpression(EncodingHelper.ContainerValidationRegex)] string? subtitleCodec,
        [FromQuery] string? transcodeReasons,
        [FromQuery] int? audioStreamIndex,
        [FromQuery] int? videoStreamIndex,
        [FromQuery] EncodingContext? context,
        [FromQuery] Dictionary<string, string>? streamOptions,
        [FromQuery] bool enableAudioVbrEncoding = true)
    {
        StreamingRequestDto streamingRequest = new StreamingRequestDto
        {
            Id = itemId,
            Container = container,
            Static = @static ?? false,
            Params = @params,
            Tag = tag,
            PlaySessionId = playSessionId,
            SegmentContainer = segmentContainer,
            SegmentLength = segmentLength,
            MinSegments = minSegments,
            MediaSourceId = mediaSourceId,
            DeviceId = deviceId,
            AudioCodec = audioCodec,
            EnableAutoStreamCopy = enableAutoStreamCopy ?? true,
            AllowAudioStreamCopy = allowAudioStreamCopy ?? true,
            AllowVideoStreamCopy = allowVideoStreamCopy ?? true,
            BreakOnNonKeyFrames = breakOnNonKeyFrames ?? false,
            AudioSampleRate = audioSampleRate,
            MaxAudioChannels = maxAudioChannels,
            AudioBitRate = audioBitRate,
            MaxAudioBitDepth = maxAudioBitDepth,
            AudioChannels = audioChannels,
            Profile = profile,
            Level = level,
            Framerate = framerate,
            MaxFramerate = maxFramerate,
            CopyTimestamps = copyTimestamps ?? false,
            StartTimeTicks = startTimeTicks,
            Width = width,
            Height = height,
            VideoBitRate = videoBitRate,
            SubtitleStreamIndex = subtitleStreamIndex,
            SubtitleMethod = subtitleMethod ?? SubtitleDeliveryMethod.Encode,
            MaxRefFrames = maxRefFrames,
            MaxVideoBitDepth = maxVideoBitDepth,
            RequireAvc = requireAvc ?? false,
            DeInterlace = deInterlace ?? false,
            RequireNonAnamorphic = requireNonAnamorphic ?? false,
            TranscodingMaxAudioChannels = transcodingMaxAudioChannels,
            CpuCoreLimit = cpuCoreLimit,
            LiveStreamId = liveStreamId,
            EnableMpegtsM2TsMode = enableMpegtsM2TsMode ?? false,
            VideoCodec = videoCodec,
            SubtitleCodec = subtitleCodec,
            TranscodeReasons = transcodeReasons,
            AudioStreamIndex = audioStreamIndex,
            VideoStreamIndex = videoStreamIndex,
            Context = context ?? EncodingContext.Static,
            StreamOptions = streamOptions,
            EnableAudioVbrEncoding = enableAudioVbrEncoding
        };

        return await _audioHelper.GetAudioStream(_transcodingJobType, streamingRequest).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an audio stream.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="container">The audio container.</param>
    /// <param name="static">Optional. If true, the original file will be streamed statically without any encoding. Use either no url extension or the original file extension. true/false.</param>
    /// <param name="params">The streaming parameters.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="deviceProfileId">Optional. The dlna device profile id to utilize.</param>
    /// <param name="playSessionId">The play session id.</param>
    /// <param name="segmentContainer">The segment container.</param>
    /// <param name="segmentLength">The segment length.</param>
    /// <param name="minSegments">The minimum number of segments.</param>
    /// <param name="mediaSourceId">The media version id, if playing an alternate version.</param>
    /// <param name="deviceId">The device id of the client requesting. Used to stop encoding processes when needed.</param>
    /// <param name="audioCodec">Optional. Specify an audio codec to encode to, e.g. mp3. If omitted the server will auto-select using the url's extension.</param>
    /// <param name="enableAutoStreamCopy">Whether or not to allow automatic stream copy if requested values match the original source. Defaults to true.</param>
    /// <param name="allowVideoStreamCopy">Whether or not to allow copying of the video stream url.</param>
    /// <param name="allowAudioStreamCopy">Whether or not to allow copying of the audio stream url.</param>
    /// <param name="breakOnNonKeyFrames">Optional. Whether to break on non key frames.</param>
    /// <param name="audioSampleRate">Optional. Specify a specific audio sample rate, e.g. 44100.</param>
    /// <param name="maxAudioBitDepth">Optional. The maximum audio bit depth.</param>
    /// <param name="audioBitRate">Optional. Specify an audio bitrate to encode to, e.g. 128000. If omitted this will be left to encoder defaults.</param>
    /// <param name="audioChannels">Optional. Specify a specific number of audio channels to encode to, e.g. 2.</param>
    /// <param name="maxAudioChannels">Optional. Specify a maximum number of audio channels to encode to, e.g. 2.</param>
    /// <param name="profile">Optional. Specify a specific an encoder profile (varies by encoder), e.g. main, baseline, high.</param>
    /// <param name="level">Optional. Specify a level for the encoder profile (varies by encoder), e.g. 3, 3.1.</param>
    /// <param name="framerate">Optional. A specific video framerate to encode to, e.g. 23.976. Generally this should be omitted unless the device has specific requirements.</param>
    /// <param name="maxFramerate">Optional. A specific maximum video framerate to encode to, e.g. 23.976. Generally this should be omitted unless the device has specific requirements.</param>
    /// <param name="copyTimestamps">Whether or not to copy timestamps when transcoding with an offset. Defaults to false.</param>
    /// <param name="startTimeTicks">Optional. Specify a starting offset, in ticks. 1 tick = 10000 ms.</param>
    /// <param name="width">Optional. The fixed horizontal resolution of the encoded video.</param>
    /// <param name="height">Optional. The fixed vertical resolution of the encoded video.</param>
    /// <param name="videoBitRate">Optional. Specify a video bitrate to encode to, e.g. 500000. If omitted this will be left to encoder defaults.</param>
    /// <param name="subtitleStreamIndex">Optional. The index of the subtitle stream to use. If omitted no subtitles will be used.</param>
    /// <param name="subtitleMethod">Optional. Specify the subtitle delivery method.</param>
    /// <param name="maxRefFrames">Optional.</param>
    /// <param name="maxVideoBitDepth">Optional. The maximum video bit depth.</param>
    /// <param name="requireAvc">Optional. Whether to require avc.</param>
    /// <param name="deInterlace">Optional. Whether to deinterlace the video.</param>
    /// <param name="requireNonAnamorphic">Optional. Whether to require a non anamporphic stream.</param>
    /// <param name="transcodingMaxAudioChannels">Optional. The maximum number of audio channels to transcode.</param>
    /// <param name="cpuCoreLimit">Optional. The limit of how many cpu cores to use.</param>
    /// <param name="liveStreamId">The live stream id.</param>
    /// <param name="enableMpegtsM2TsMode">Optional. Whether to enable the MpegtsM2Ts mode.</param>
    /// <param name="videoCodec">Optional. Specify a video codec to encode to, e.g. h264. If omitted the server will auto-select using the url's extension.</param>
    /// <param name="subtitleCodec">Optional. Specify a subtitle codec to encode to.</param>
    /// <param name="transcodeReasons">Optional. The transcoding reason.</param>
    /// <param name="audioStreamIndex">Optional. The index of the audio stream to use. If omitted the first audio stream will be used.</param>
    /// <param name="videoStreamIndex">Optional. The index of the video stream to use. If omitted the first video stream will be used.</param>
    /// <param name="context">Optional. The <see cref="EncodingContext"/>.</param>
    /// <param name="streamOptions">Optional. The streaming options.</param>
    /// <param name="enableAudioVbrEncoding">Optional. Whether to enable Audio Encoding.</param>
    /// <response code="200">Audio stream returned.</response>
    /// <returns>A <see cref="FileResult"/> containing the audio file.</returns>
    [HttpGet("{itemId}/stream.{container}", Name = "GetAudioStreamByContainer")]
    [HttpHead("{itemId}/stream.{container}", Name = "HeadAudioStreamByContainer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesAudioFile]
    public async Task<ActionResult> GetAudioStreamByContainer(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] [RegularExpression(EncodingHelper.ContainerValidationRegex)] string container,
        [FromQuery] bool? @static,
        [FromQuery] string? @params,
        [FromQuery] string? tag,
        [FromQuery, ParameterObsolete] string? deviceProfileId,
        [FromQuery] string? playSessionId,
        [FromQuery] [RegularExpression(EncodingHelper.ContainerValidationRegex)] string? segmentContainer,
        [FromQuery] int? segmentLength,
        [FromQuery] int? minSegments,
        [FromQuery] string? mediaSourceId,
        [FromQuery] string? deviceId,
        [FromQuery] [RegularExpression(EncodingHelper.ContainerValidationRegex)] string? audioCodec,
        [FromQuery] bool? enableAutoStreamCopy,
        [FromQuery] bool? allowVideoStreamCopy,
        [FromQuery] bool? allowAudioStreamCopy,
        [FromQuery] bool? breakOnNonKeyFrames,
        [FromQuery] int? audioSampleRate,
        [FromQuery] int? maxAudioBitDepth,
        [FromQuery] int? audioBitRate,
        [FromQuery] int? audioChannels,
        [FromQuery] int? maxAudioChannels,
        [FromQuery] string? profile,
        [FromQuery] [RegularExpression(EncodingHelper.LevelValidationRegex)] string? level,
        [FromQuery] float? framerate,
        [FromQuery] float? maxFramerate,
        [FromQuery] bool? copyTimestamps,
        [FromQuery] long? startTimeTicks,
        [FromQuery] int? width,
        [FromQuery] int? height,
        [FromQuery] int? videoBitRate,
        [FromQuery] int? subtitleStreamIndex,
        [FromQuery] SubtitleDeliveryMethod? subtitleMethod,
        [FromQuery] int? maxRefFrames,
        [FromQuery] int? maxVideoBitDepth,
        [FromQuery] bool? requireAvc,
        [FromQuery] bool? deInterlace,
        [FromQuery] bool? requireNonAnamorphic,
        [FromQuery] int? transcodingMaxAudioChannels,
        [FromQuery] int? cpuCoreLimit,
        [FromQuery] string? liveStreamId,
        [FromQuery] bool? enableMpegtsM2TsMode,
        [FromQuery] [RegularExpression(EncodingHelper.ContainerValidationRegex)] string? videoCodec,
        [FromQuery] [RegularExpression(EncodingHelper.ContainerValidationRegex)] string? subtitleCodec,
        [FromQuery] string? transcodeReasons,
        [FromQuery] int? audioStreamIndex,
        [FromQuery] int? videoStreamIndex,
        [FromQuery] EncodingContext? context,
        [FromQuery] Dictionary<string, string>? streamOptions,
        [FromQuery] bool enableAudioVbrEncoding = true)
    {
        StreamingRequestDto streamingRequest = new StreamingRequestDto
        {
            Id = itemId,
            Container = container,
            Static = @static ?? false,
            Params = @params,
            Tag = tag,
            PlaySessionId = playSessionId,
            SegmentContainer = segmentContainer,
            SegmentLength = segmentLength,
            MinSegments = minSegments,
            MediaSourceId = mediaSourceId,
            DeviceId = deviceId,
            AudioCodec = audioCodec,
            EnableAutoStreamCopy = enableAutoStreamCopy ?? true,
            AllowAudioStreamCopy = allowAudioStreamCopy ?? true,
            AllowVideoStreamCopy = allowVideoStreamCopy ?? true,
            BreakOnNonKeyFrames = breakOnNonKeyFrames ?? false,
            AudioSampleRate = audioSampleRate,
            MaxAudioChannels = maxAudioChannels,
            AudioBitRate = audioBitRate,
            MaxAudioBitDepth = maxAudioBitDepth,
            AudioChannels = audioChannels,
            Profile = profile,
            Level = level,
            Framerate = framerate,
            MaxFramerate = maxFramerate,
            CopyTimestamps = copyTimestamps ?? false,
            StartTimeTicks = startTimeTicks,
            Width = width,
            Height = height,
            VideoBitRate = videoBitRate,
            SubtitleStreamIndex = subtitleStreamIndex,
            SubtitleMethod = subtitleMethod ?? SubtitleDeliveryMethod.Encode,
            MaxRefFrames = maxRefFrames,
            MaxVideoBitDepth = maxVideoBitDepth,
            RequireAvc = requireAvc ?? false,
            DeInterlace = deInterlace ?? false,
            RequireNonAnamorphic = requireNonAnamorphic ?? false,
            TranscodingMaxAudioChannels = transcodingMaxAudioChannels,
            CpuCoreLimit = cpuCoreLimit,
            LiveStreamId = liveStreamId,
            EnableMpegtsM2TsMode = enableMpegtsM2TsMode ?? false,
            VideoCodec = videoCodec,
            SubtitleCodec = subtitleCodec,
            TranscodeReasons = transcodeReasons,
            AudioStreamIndex = audioStreamIndex,
            VideoStreamIndex = videoStreamIndex,
            Context = context ?? EncodingContext.Static,
            StreamOptions = streamOptions,
            EnableAudioVbrEncoding = enableAudioVbrEncoding
        };

        return await _audioHelper.GetAudioStream(_transcodingJobType, streamingRequest).ConfigureAwait(false);
    }
}
