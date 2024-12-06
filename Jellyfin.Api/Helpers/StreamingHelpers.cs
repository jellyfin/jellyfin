using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// The streaming helpers.
/// </summary>
public static class StreamingHelpers
{
    /// <summary>
    /// Gets the current streaming state.
    /// </summary>
    /// <param name="streamingRequest">The <see cref="StreamingRequestDto"/>.</param>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="encodingHelper">Instance of <see cref="EncodingHelper"/>.</param>
    /// <param name="transcodeManager">Instance of the <see cref="ITranscodeManager"/> interface.</param>
    /// <param name="transcodingJobType">The <see cref="TranscodingJobType"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> containing the current <see cref="StreamState"/>.</returns>
    public static async Task<StreamState> GetStreamingState(
        StreamingRequestDto streamingRequest,
        HttpContext httpContext,
        IMediaSourceManager mediaSourceManager,
        IUserManager userManager,
        ILibraryManager libraryManager,
        IServerConfigurationManager serverConfigurationManager,
        IMediaEncoder mediaEncoder,
        EncodingHelper encodingHelper,
        ITranscodeManager transcodeManager,
        TranscodingJobType transcodingJobType,
        CancellationToken cancellationToken)
    {
        var httpRequest = httpContext.Request;
        if (!string.IsNullOrWhiteSpace(streamingRequest.Params))
        {
            ParseParams(streamingRequest);
        }

        streamingRequest.StreamOptions = ParseStreamOptions(httpRequest.Query);
        if (httpRequest.Path.Value is null)
        {
            throw new ResourceNotFoundException(nameof(httpRequest.Path));
        }

        var url = httpRequest.Path.Value.AsSpan().RightPart('.').ToString();

        if (string.IsNullOrEmpty(streamingRequest.AudioCodec))
        {
            streamingRequest.AudioCodec = encodingHelper.InferAudioCodec(url);
        }

        var state = new StreamState(mediaSourceManager, transcodingJobType, transcodeManager)
        {
            Request = streamingRequest,
            RequestedUrl = url,
            UserAgent = httpRequest.Headers[HeaderNames.UserAgent]
        };

        var userId = httpContext.User.GetUserId();
        if (!userId.IsEmpty())
        {
            state.User = userManager.GetUserById(userId);
        }

        if (state.IsVideoRequest && !string.IsNullOrWhiteSpace(state.Request.VideoCodec))
        {
            state.SupportedVideoCodecs = state.Request.VideoCodec.Split(',', StringSplitOptions.RemoveEmptyEntries);
            state.Request.VideoCodec = state.SupportedVideoCodecs.FirstOrDefault();
        }

        if (!string.IsNullOrWhiteSpace(streamingRequest.AudioCodec))
        {
            state.SupportedAudioCodecs = streamingRequest.AudioCodec.Split(',', StringSplitOptions.RemoveEmptyEntries);
            state.Request.AudioCodec = state.SupportedAudioCodecs.FirstOrDefault(mediaEncoder.CanEncodeToAudioCodec)
                                       ?? state.SupportedAudioCodecs.FirstOrDefault();
        }

        if (!string.IsNullOrWhiteSpace(streamingRequest.SubtitleCodec))
        {
            state.SupportedSubtitleCodecs = streamingRequest.SubtitleCodec.Split(',', StringSplitOptions.RemoveEmptyEntries);
            state.Request.SubtitleCodec = state.SupportedSubtitleCodecs.FirstOrDefault(mediaEncoder.CanEncodeToSubtitleCodec)
                                          ?? state.SupportedSubtitleCodecs.FirstOrDefault();
        }

        var item = libraryManager.GetItemById<BaseItem>(streamingRequest.Id)
            ?? throw new ResourceNotFoundException();

        state.IsInputVideo = item.MediaType == MediaType.Video;

        MediaSourceInfo? mediaSource = null;
        if (string.IsNullOrWhiteSpace(streamingRequest.LiveStreamId))
        {
            var currentJob = !string.IsNullOrWhiteSpace(streamingRequest.PlaySessionId)
                ? transcodeManager.GetTranscodingJob(streamingRequest.PlaySessionId)
                : null;

            if (currentJob is not null)
            {
                mediaSource = currentJob.MediaSource;
            }

            if (mediaSource is null)
            {
                var mediaSources = await mediaSourceManager.GetPlaybackMediaSources(libraryManager.GetItemById<BaseItem>(streamingRequest.Id), null, false, false, cancellationToken).ConfigureAwait(false);

                mediaSource = string.IsNullOrEmpty(streamingRequest.MediaSourceId)
                    ? mediaSources[0]
                    : mediaSources.Find(i => string.Equals(i.Id, streamingRequest.MediaSourceId, StringComparison.Ordinal));

                if (mediaSource is null && Guid.Parse(streamingRequest.MediaSourceId).Equals(streamingRequest.Id))
                {
                    mediaSource = mediaSources[0];
                }
            }
        }
        else
        {
            var liveStreamInfo = await mediaSourceManager.GetLiveStreamWithDirectStreamProvider(streamingRequest.LiveStreamId, cancellationToken).ConfigureAwait(false);
            mediaSource = liveStreamInfo.Item1;
            state.DirectStreamProvider = liveStreamInfo.Item2;

            // Cap the max bitrate when it is too high. This is usually due to ffmpeg is unable to probe the source liveTV streams' bitrate.
            if (mediaSource.FallbackMaxStreamingBitrate is not null && streamingRequest.VideoBitRate is not null)
            {
                streamingRequest.VideoBitRate = Math.Min(streamingRequest.VideoBitRate.Value, mediaSource.FallbackMaxStreamingBitrate.Value);
            }
        }

        var encodingOptions = serverConfigurationManager.GetEncodingOptions();

        encodingHelper.AttachMediaSourceInfo(state, encodingOptions, mediaSource, url);

        string? containerInternal = Path.GetExtension(state.RequestedUrl);

        if (!string.IsNullOrEmpty(streamingRequest.Container))
        {
            containerInternal = streamingRequest.Container;
        }

        if (string.IsNullOrEmpty(containerInternal))
        {
            containerInternal = streamingRequest.Static ?
                StreamBuilder.NormalizeMediaSourceFormatIntoSingleContainer(state.InputContainer, null, DlnaProfileType.Audio)
                : GetOutputFileExtension(state, mediaSource);
        }

        var outputAudioCodec = streamingRequest.AudioCodec;
        state.OutputAudioCodec = outputAudioCodec;
        state.OutputContainer = (containerInternal ?? string.Empty).TrimStart('.');
        state.OutputAudioChannels = encodingHelper.GetNumAudioChannelsParam(state, state.AudioStream, state.OutputAudioCodec);
        if (EncodingHelper.LosslessAudioCodecs.Contains(outputAudioCodec))
        {
            state.OutputAudioBitrate = state.AudioStream.BitRate ?? 0;
        }
        else
        {
            state.OutputAudioBitrate = encodingHelper.GetAudioBitrateParam(streamingRequest.AudioBitRate, streamingRequest.AudioCodec, state.AudioStream, state.OutputAudioChannels) ?? 0;
        }

        if (outputAudioCodec.StartsWith("pcm_", StringComparison.Ordinal))
        {
            containerInternal = ".pcm";
        }

        if (state.VideoRequest is not null)
        {
            state.OutputVideoCodec = state.Request.VideoCodec;
            state.OutputVideoBitrate = encodingHelper.GetVideoBitrateParamValue(state.VideoRequest, state.VideoStream, state.OutputVideoCodec);

            encodingHelper.TryStreamCopy(state);

            if (!EncodingHelper.IsCopyCodec(state.OutputVideoCodec) && state.OutputVideoBitrate.HasValue)
            {
                var isVideoResolutionNotRequested = !state.VideoRequest.Width.HasValue
                    && !state.VideoRequest.Height.HasValue
                    && !state.VideoRequest.MaxWidth.HasValue
                    && !state.VideoRequest.MaxHeight.HasValue;

                if (isVideoResolutionNotRequested
                    && state.VideoStream is not null
                    && state.VideoRequest.VideoBitRate.HasValue
                    && state.VideoStream.BitRate.HasValue
                    && state.VideoRequest.VideoBitRate.Value >= state.VideoStream.BitRate.Value)
                {
                    // Don't downscale the resolution if the width/height/MaxWidth/MaxHeight is not requested,
                    // and the requested video bitrate is higher than source video bitrate.
                    if (state.VideoStream.Width.HasValue || state.VideoStream.Height.HasValue)
                    {
                        state.VideoRequest.MaxWidth = state.VideoStream?.Width;
                        state.VideoRequest.MaxHeight = state.VideoStream?.Height;
                    }
                }
                else
                {
                    var h264EquivalentBitrate = EncodingHelper.ScaleBitrate(
                        state.OutputVideoBitrate.Value,
                        state.ActualOutputVideoCodec,
                        "h264");
                    var resolution = ResolutionNormalizer.Normalize(
                        state.VideoStream?.BitRate,
                        state.OutputVideoBitrate.Value,
                        h264EquivalentBitrate,
                        state.VideoRequest.MaxWidth,
                        state.VideoRequest.MaxHeight,
                        state.TargetFramerate);

                    state.VideoRequest.MaxWidth = resolution.MaxWidth;
                    state.VideoRequest.MaxHeight = resolution.MaxHeight;
                }
            }

            if (state.AudioStream is not null && !EncodingHelper.IsCopyCodec(state.OutputAudioCodec) && string.Equals(state.AudioStream.Codec, state.OutputAudioCodec, StringComparison.OrdinalIgnoreCase) && state.OutputAudioBitrate.HasValue)
            {
                state.OutputAudioCodec = state.SupportedAudioCodecs.Where(c => !EncodingHelper.LosslessAudioCodecs.Contains(c)).FirstOrDefault(mediaEncoder.CanEncodeToAudioCodec);
            }
        }

        var ext = string.IsNullOrWhiteSpace(state.OutputContainer)
            ? GetOutputFileExtension(state, mediaSource)
            : ("." + GetContainerFileExtension(state.OutputContainer));

        state.OutputFilePath = GetOutputFilePath(state, ext, serverConfigurationManager, streamingRequest.DeviceId, streamingRequest.PlaySessionId);

        return state;
    }

    /// <summary>
    /// Parses query parameters as StreamOptions.
    /// </summary>
    /// <param name="queryString">The query string.</param>
    /// <returns>A <see cref="Dictionary{String,String}"/> containing the stream options.</returns>
    private static Dictionary<string, string?> ParseStreamOptions(IQueryCollection queryString)
    {
        Dictionary<string, string?> streamOptions = new Dictionary<string, string?>();
        foreach (var param in queryString)
        {
            if (char.IsLower(param.Key[0]))
            {
                // This was probably not parsed initially and should be a StreamOptions
                // or the generated URL should correctly serialize it
                // TODO: This should be incorporated either in the lower framework for parsing requests
                streamOptions[param.Key] = param.Value;
            }
        }

        return streamOptions;
    }

    /// <summary>
    /// Gets the output file extension.
    /// </summary>
    /// <param name="state">The state.</param>
    /// <param name="mediaSource">The mediaSource.</param>
    /// <returns>System.String.</returns>
    private static string GetOutputFileExtension(StreamState state, MediaSourceInfo? mediaSource)
    {
        var ext = Path.GetExtension(state.RequestedUrl);
        if (!string.IsNullOrEmpty(ext))
        {
            return ext;
        }

        // Try to infer based on the desired video codec
        if (state.IsVideoRequest)
        {
            var videoCodec = state.Request.VideoCodec;

            if (string.Equals(videoCodec, "h264", StringComparison.OrdinalIgnoreCase))
            {
                return ".ts";
            }

            if (string.Equals(videoCodec, "hevc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(videoCodec, "av1", StringComparison.OrdinalIgnoreCase))
            {
                return ".mp4";
            }

            if (string.Equals(videoCodec, "theora", StringComparison.OrdinalIgnoreCase))
            {
                return ".ogv";
            }

            if (string.Equals(videoCodec, "vp8", StringComparison.OrdinalIgnoreCase)
                || string.Equals(videoCodec, "vp9", StringComparison.OrdinalIgnoreCase)
                || string.Equals(videoCodec, "vpx", StringComparison.OrdinalIgnoreCase))
            {
                return ".webm";
            }

            if (string.Equals(videoCodec, "wmv", StringComparison.OrdinalIgnoreCase))
            {
                return ".asf";
            }
        }
        else
        {
            // Try to infer based on the desired audio codec
            var audioCodec = state.Request.AudioCodec;

            if (string.Equals("aac", audioCodec, StringComparison.OrdinalIgnoreCase))
            {
                return ".aac";
            }

            if (string.Equals("mp3", audioCodec, StringComparison.OrdinalIgnoreCase))
            {
                return ".mp3";
            }

            if (string.Equals("vorbis", audioCodec, StringComparison.OrdinalIgnoreCase))
            {
                return ".ogg";
            }

            if (string.Equals("wma", audioCodec, StringComparison.OrdinalIgnoreCase))
            {
                return ".wma";
            }
        }

        // Fallback to the container of mediaSource
        if (!string.IsNullOrEmpty(mediaSource?.Container))
        {
            var idx = mediaSource.Container.IndexOf(',', StringComparison.OrdinalIgnoreCase);
            return '.' + (idx == -1 ? mediaSource.Container : mediaSource.Container[..idx]).Trim();
        }

        throw new InvalidOperationException("Failed to find an appropriate file extension");
    }

    /// <summary>
    /// Gets the output file path for transcoding.
    /// </summary>
    /// <param name="state">The current <see cref="StreamState"/>.</param>
    /// <param name="outputFileExtension">The file extension of the output file.</param>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="deviceId">The device id.</param>
    /// <param name="playSessionId">The play session id.</param>
    /// <returns>The complete file path, including the folder, for the transcoding file.</returns>
    private static string GetOutputFilePath(StreamState state, string outputFileExtension, IServerConfigurationManager serverConfigurationManager, string? deviceId, string? playSessionId)
    {
        var data = $"{state.MediaPath}-{state.UserAgent}-{deviceId!}-{playSessionId!}";

        var filename = data.GetMD5().ToString("N", CultureInfo.InvariantCulture);
        var ext = outputFileExtension.ToLowerInvariant();
        var folder = serverConfigurationManager.GetTranscodePath();

        return Path.Combine(folder, filename + ext);
    }

    /// <summary>
    /// Parses the parameters.
    /// </summary>
    /// <param name="request">The request.</param>
    private static void ParseParams(StreamingRequestDto request)
    {
        if (string.IsNullOrEmpty(request.Params))
        {
            return;
        }

        var vals = request.Params.Split(';');

        var videoRequest = request as VideoRequestDto;

        for (var i = 0; i < vals.Length; i++)
        {
            var val = vals[i];

            if (string.IsNullOrWhiteSpace(val))
            {
                continue;
            }

            switch (i)
            {
                case 0:
                    // DeviceProfileId
                    break;
                case 1:
                    request.DeviceId = val;
                    break;
                case 2:
                    request.MediaSourceId = val;
                    break;
                case 3:
                    request.Static = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                    break;
                case 4:
                    if (videoRequest is not null)
                    {
                        videoRequest.VideoCodec = val;
                    }

                    break;
                case 5:
                    request.AudioCodec = val;
                    break;
                case 6:
                    if (videoRequest is not null)
                    {
                        videoRequest.AudioStreamIndex = int.Parse(val, CultureInfo.InvariantCulture);
                    }

                    break;
                case 7:
                    if (videoRequest is not null)
                    {
                        videoRequest.SubtitleStreamIndex = int.Parse(val, CultureInfo.InvariantCulture);
                    }

                    break;
                case 8:
                    if (videoRequest is not null)
                    {
                        videoRequest.VideoBitRate = int.Parse(val, CultureInfo.InvariantCulture);
                    }

                    break;
                case 9:
                    request.AudioBitRate = int.Parse(val, CultureInfo.InvariantCulture);
                    break;
                case 10:
                    request.MaxAudioChannels = int.Parse(val, CultureInfo.InvariantCulture);
                    break;
                case 11:
                    if (videoRequest is not null)
                    {
                        videoRequest.MaxFramerate = float.Parse(val, CultureInfo.InvariantCulture);
                    }

                    break;
                case 12:
                    if (videoRequest is not null)
                    {
                        videoRequest.MaxWidth = int.Parse(val, CultureInfo.InvariantCulture);
                    }

                    break;
                case 13:
                    if (videoRequest is not null)
                    {
                        videoRequest.MaxHeight = int.Parse(val, CultureInfo.InvariantCulture);
                    }

                    break;
                case 14:
                    request.StartTimeTicks = long.Parse(val, CultureInfo.InvariantCulture);
                    break;
                case 15:
                    if (videoRequest is not null)
                    {
                        videoRequest.Level = val;
                    }

                    break;
                case 16:
                    if (videoRequest is not null)
                    {
                        videoRequest.MaxRefFrames = int.Parse(val, CultureInfo.InvariantCulture);
                    }

                    break;
                case 17:
                    if (videoRequest is not null)
                    {
                        videoRequest.MaxVideoBitDepth = int.Parse(val, CultureInfo.InvariantCulture);
                    }

                    break;
                case 18:
                    if (videoRequest is not null)
                    {
                        videoRequest.Profile = val;
                    }

                    break;
                case 19:
                    // cabac no longer used
                    break;
                case 20:
                    request.PlaySessionId = val;
                    break;
                case 21:
                    // api_key
                    break;
                case 22:
                    request.LiveStreamId = val;
                    break;
                case 23:
                    // Duplicating ItemId because of MediaMonkey
                    break;
                case 24:
                    if (videoRequest is not null)
                    {
                        videoRequest.CopyTimestamps = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                    }

                    break;
                case 25:
                    if (!string.IsNullOrWhiteSpace(val) && videoRequest is not null)
                    {
                        if (Enum.TryParse(val, out SubtitleDeliveryMethod method))
                        {
                            videoRequest.SubtitleMethod = method;
                        }
                    }

                    break;
                case 26:
                    request.TranscodingMaxAudioChannels = int.Parse(val, CultureInfo.InvariantCulture);
                    break;
                case 27:
                    if (videoRequest is not null)
                    {
                        videoRequest.EnableSubtitlesInManifest = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                    }

                    break;
                case 28:
                    request.Tag = val;
                    break;
                case 29:
                    if (videoRequest is not null)
                    {
                        videoRequest.RequireAvc = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                    }

                    break;
                case 30:
                    request.SubtitleCodec = val;
                    break;
                case 31:
                    if (videoRequest is not null)
                    {
                        videoRequest.RequireNonAnamorphic = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                    }

                    break;
                case 32:
                    if (videoRequest is not null)
                    {
                        videoRequest.DeInterlace = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                    }

                    break;
                case 33:
                    request.TranscodeReasons = val;
                    break;
            }
        }
    }

    /// <summary>
    /// Parses the container into its file extension.
    /// </summary>
    /// <param name="container">The container.</param>
    private static string? GetContainerFileExtension(string? container)
    {
        if (string.Equals(container, "mpegts", StringComparison.OrdinalIgnoreCase))
        {
            return "ts";
        }

        if (string.Equals(container, "matroska", StringComparison.OrdinalIgnoreCase))
        {
            return "mkv";
        }

        return container;
    }
}
