using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Models.StreamingDtos;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// The streaming helpers.
    /// </summary>
    public static class StreamingHelpers
    {
        /// <summary>
        /// Gets or sets a value indicating the streaming event handlers. (Used by streaming plugins. eg. DLNA).
        /// </summary>
        public static EventHandler<StreamEventArgs>? StreamEvent { get; set; }

        /// <summary>
        /// Gets the current streaming state.
        /// </summary>
        /// <param name="streamingRequest">The <see cref="StreamingRequestDto"/>.</param>
        /// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
        /// <param name="authorizationContext">Instance of the <see cref="IAuthorizationContext"/> interface.</param>
        /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
        /// <param name="encodingHelper">Instance of <see cref="EncodingHelper"/>.</param>
        /// <param name="deviceManager">Instance of the <see cref="IDeviceManager"/> interface.</param>
        /// <param name="transcodingJobHelper">Initialized <see cref="TranscodingJobHelper"/>.</param>
        /// <param name="transcodingJobType">The <see cref="TranscodingJobType"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="Task"/> containing the current <see cref="StreamState"/>.</returns>
        public static async Task<StreamState> GetStreamingState(
            StreamingRequestDto streamingRequest,
            HttpRequest httpRequest,
            IAuthorizationContext authorizationContext,
            IMediaSourceManager mediaSourceManager,
            IUserManager userManager,
            ILibraryManager libraryManager,
            IServerConfigurationManager serverConfigurationManager,
            IMediaEncoder mediaEncoder,
            EncodingHelper encodingHelper,
            IDeviceManager deviceManager,
            TranscodingJobHelper transcodingJobHelper,
            TranscodingJobType transcodingJobType,
            CancellationToken cancellationToken)
        {
            StreamingHelpers.StreamEvent?.Invoke(null, new StreamEventArgs()
            {
                Type = StreamEventType.OnHeaderProcessing,
                Request = httpRequest,
                StreamingRequest = streamingRequest,
            });

            if (!string.IsNullOrWhiteSpace(streamingRequest.Params))
            {
                ParseParams(streamingRequest);
            }

            streamingRequest.StreamOptions = ParseStreamOptions(httpRequest.Query);
            if (httpRequest.Path.Value == null)
            {
                throw new ResourceNotFoundException(nameof(httpRequest.Path));
            }

            var url = httpRequest.Path.Value.Split('.')[^1];

            if (string.IsNullOrEmpty(streamingRequest.AudioCodec))
            {
                streamingRequest.AudioCodec = encodingHelper.InferAudioCodec(url);
            }

            var state = new StreamState(mediaSourceManager, transcodingJobType, transcodingJobHelper)
            {
                Request = streamingRequest,
                RequestedUrl = httpRequest.Path,
                UserAgent = httpRequest.Headers[HeaderNames.UserAgent]
            };

            var auth = authorizationContext.GetAuthorizationInfo(httpRequest);
            if (!auth.UserId.Equals(Guid.Empty))
            {
                state.User = userManager.GetUserById(auth.UserId);
            }

            if (state.IsVideoRequest && !string.IsNullOrWhiteSpace(state.Request.VideoCodec))
            {
                state.SupportedVideoCodecs = state.Request.VideoCodec.Split(',', StringSplitOptions.RemoveEmptyEntries);
                state.Request.VideoCodec = state.SupportedVideoCodecs.FirstOrDefault();
            }

            if (!string.IsNullOrWhiteSpace(streamingRequest.AudioCodec))
            {
                state.SupportedAudioCodecs = streamingRequest.AudioCodec.Split(',', StringSplitOptions.RemoveEmptyEntries);
                state.Request.AudioCodec = state.SupportedAudioCodecs.FirstOrDefault(i => mediaEncoder.CanEncodeToAudioCodec(i))
                                           ?? state.SupportedAudioCodecs.FirstOrDefault();
            }

            if (!string.IsNullOrWhiteSpace(streamingRequest.SubtitleCodec))
            {
                state.SupportedSubtitleCodecs = streamingRequest.SubtitleCodec.Split(',', StringSplitOptions.RemoveEmptyEntries);
                state.Request.SubtitleCodec = state.SupportedSubtitleCodecs.FirstOrDefault(i => mediaEncoder.CanEncodeToSubtitleCodec(i))
                                              ?? state.SupportedSubtitleCodecs.FirstOrDefault();
            }

            var item = libraryManager.GetItemById(streamingRequest.Id);

            state.IsInputVideo = string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase);

            MediaSourceInfo? mediaSource = null;
            if (string.IsNullOrWhiteSpace(streamingRequest.LiveStreamId))
            {
                var currentJob = !string.IsNullOrWhiteSpace(streamingRequest.PlaySessionId)
                    ? transcodingJobHelper.GetTranscodingJob(streamingRequest.PlaySessionId)
                    : null;

                if (currentJob != null)
                {
                    mediaSource = currentJob.MediaSource;
                }

                if (mediaSource == null)
                {
                    var mediaSources = await mediaSourceManager.GetPlaybackMediaSources(libraryManager.GetItemById(streamingRequest.Id), null, false, false, cancellationToken).ConfigureAwait(false);

                    mediaSource = string.IsNullOrEmpty(streamingRequest.MediaSourceId)
                        ? mediaSources[0]
                        : mediaSources.Find(i => string.Equals(i.Id, streamingRequest.MediaSourceId, StringComparison.InvariantCulture));

                    if (mediaSource == null && Guid.Parse(streamingRequest.MediaSourceId) == streamingRequest.Id)
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
                    : GetOutputFileExtension(state);
            }

            state.OutputContainer = (containerInternal ?? string.Empty).TrimStart('.');

            state.OutputAudioBitrate = encodingHelper.GetAudioBitrateParam(streamingRequest.AudioBitRate, streamingRequest.AudioCodec, state.AudioStream);

            state.OutputAudioCodec = streamingRequest.AudioCodec;

            state.OutputAudioChannels = encodingHelper.GetNumAudioChannelsParam(state, state.AudioStream, state.OutputAudioCodec);

            if (state.VideoRequest != null)
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
                        && state.VideoStream != null
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
                        var resolution = ResolutionNormalizer.Normalize(
                            state.VideoStream?.BitRate,
                            state.VideoStream?.Width,
                            state.VideoStream?.Height,
                            state.OutputVideoBitrate.Value,
                            state.VideoStream?.Codec,
                            state.OutputVideoCodec,
                            state.VideoRequest.MaxWidth,
                            state.VideoRequest.MaxHeight);

                        state.VideoRequest.MaxWidth = resolution.MaxWidth;
                        state.VideoRequest.MaxHeight = resolution.MaxHeight;
                    }
                }
            }

            StreamingHelpers.StreamEvent?.Invoke(null, new StreamEventArgs()
            {
                Type = StreamEventType.OnCodecProcessing,
                State = state,
                DeviceManager = deviceManager,
                Request = httpRequest,
                DeviceProfileId = streamingRequest.DeviceProfileId, // TODO: move this down a level.
                IsStaticallyStreamed = streamingRequest.Static,
                StreamingRequest = streamingRequest,
            });

            var ext = string.IsNullOrWhiteSpace(state.OutputContainer)
                ? GetOutputFileExtension(state)
                : ("." + state.OutputContainer);

            state.OutputFilePath = GetOutputFilePath(state, ext!, serverConfigurationManager, streamingRequest.DeviceId, streamingRequest.PlaySessionId);

            return state;
        }

        /// <summary>
        /// Parses query parameters as StreamOptions.
        /// </summary>
        /// <param name="queryString">The query string.</param>
        /// <returns>A <see cref="Dictionary{String,String}"/> containing the stream options.</returns>
        private static Dictionary<string, string> ParseStreamOptions(IQueryCollection queryString)
        {
            Dictionary<string, string> streamOptions = new Dictionary<string, string>();
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
        /// <returns>System.String.</returns>
        private static string? GetOutputFileExtension(StreamState state)
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

                if (string.Equals(videoCodec, "h264", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(videoCodec, "h265", StringComparison.OrdinalIgnoreCase))
                {
                    return ".ts";
                }

                if (string.Equals(videoCodec, "theora", StringComparison.OrdinalIgnoreCase))
                {
                    return ".ogv";
                }

                if (string.Equals(videoCodec, "vpx", StringComparison.OrdinalIgnoreCase))
                {
                    return ".webm";
                }

                if (string.Equals(videoCodec, "wmv", StringComparison.OrdinalIgnoreCase))
                {
                    return ".asf";
                }
            }

            // Try to infer based on the desired audio codec
            if (!state.IsVideoRequest)
            {
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

            return null;
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
            var ext = outputFileExtension?.ToLowerInvariant();
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
                        request.DeviceProfileId = val;
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
                        if (videoRequest != null)
                        {
                            videoRequest.VideoCodec = val;
                        }

                        break;
                    case 5:
                        request.AudioCodec = val;
                        break;
                    case 6:
                        if (videoRequest != null)
                        {
                            videoRequest.AudioStreamIndex = int.Parse(val, CultureInfo.InvariantCulture);
                        }

                        break;
                    case 7:
                        if (videoRequest != null)
                        {
                            videoRequest.SubtitleStreamIndex = int.Parse(val, CultureInfo.InvariantCulture);
                        }

                        break;
                    case 8:
                        if (videoRequest != null)
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
                        if (videoRequest != null)
                        {
                            videoRequest.MaxFramerate = float.Parse(val, CultureInfo.InvariantCulture);
                        }

                        break;
                    case 12:
                        if (videoRequest != null)
                        {
                            videoRequest.MaxWidth = int.Parse(val, CultureInfo.InvariantCulture);
                        }

                        break;
                    case 13:
                        if (videoRequest != null)
                        {
                            videoRequest.MaxHeight = int.Parse(val, CultureInfo.InvariantCulture);
                        }

                        break;
                    case 14:
                        request.StartTimeTicks = long.Parse(val, CultureInfo.InvariantCulture);
                        break;
                    case 15:
                        if (videoRequest != null)
                        {
                            videoRequest.Level = val;
                        }

                        break;
                    case 16:
                        if (videoRequest != null)
                        {
                            videoRequest.MaxRefFrames = int.Parse(val, CultureInfo.InvariantCulture);
                        }

                        break;
                    case 17:
                        if (videoRequest != null)
                        {
                            videoRequest.MaxVideoBitDepth = int.Parse(val, CultureInfo.InvariantCulture);
                        }

                        break;
                    case 18:
                        if (videoRequest != null)
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
                        if (videoRequest != null)
                        {
                            videoRequest.CopyTimestamps = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                        }

                        break;
                    case 25:
                        if (!string.IsNullOrWhiteSpace(val) && videoRequest != null)
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
                        if (videoRequest != null)
                        {
                            videoRequest.EnableSubtitlesInManifest = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                        }

                        break;
                    case 28:
                        request.Tag = val;
                        break;
                    case 29:
                        if (videoRequest != null)
                        {
                            videoRequest.RequireAvc = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                        }

                        break;
                    case 30:
                        request.SubtitleCodec = val;
                        break;
                    case 31:
                        if (videoRequest != null)
                        {
                            videoRequest.RequireNonAnamorphic = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                        }

                        break;
                    case 32:
                        if (videoRequest != null)
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
    }
}
