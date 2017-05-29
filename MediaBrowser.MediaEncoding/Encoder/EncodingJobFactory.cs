using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class EncodingJobFactory
    {
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IConfigurationManager _config;
        private readonly IMediaEncoder _mediaEncoder;

        protected static readonly CultureInfo UsCulture = new CultureInfo("en-US");
        
        public EncodingJobFactory(ILogger logger, ILibraryManager libraryManager, IMediaSourceManager mediaSourceManager, IConfigurationManager config, IMediaEncoder mediaEncoder)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _mediaSourceManager = mediaSourceManager;
            _config = config;
            _mediaEncoder = mediaEncoder;
        }

        public async Task<EncodingJob> CreateJob(EncodingJobOptions options, EncodingHelper encodingHelper, bool isVideoRequest, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var request = options;

            if (string.IsNullOrEmpty(request.AudioCodec))
            {
                request.AudioCodec = InferAudioCodec(request.OutputContainer);
            }

            var state = new EncodingJob(_logger, _mediaSourceManager)
            {
                Options = options,
                IsVideoRequest = isVideoRequest,
                Progress = progress
            };

            if (!string.IsNullOrWhiteSpace(request.VideoCodec))
            {
                state.SupportedVideoCodecs = request.VideoCodec.Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
                request.VideoCodec = state.SupportedVideoCodecs.FirstOrDefault();
            }

            if (!string.IsNullOrWhiteSpace(request.AudioCodec))
            {
                state.SupportedAudioCodecs = request.AudioCodec.Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
                request.AudioCodec = state.SupportedAudioCodecs.FirstOrDefault();
            }

            if (!string.IsNullOrWhiteSpace(request.SubtitleCodec))
            {
                state.SupportedSubtitleCodecs = request.SubtitleCodec.Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
                request.SubtitleCodec = state.SupportedSubtitleCodecs.FirstOrDefault(i => _mediaEncoder.CanEncodeToSubtitleCodec(i))
                    ?? state.SupportedSubtitleCodecs.FirstOrDefault();
            }

            var item = _libraryManager.GetItemById(request.ItemId);
            state.ItemType = item.GetType().Name;

            state.IsInputVideo = string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase);

            var primaryImage = item.GetImageInfo(ImageType.Primary, 0) ??
                               item.Parents.Select(i => i.GetImageInfo(ImageType.Primary, 0)).FirstOrDefault(i => i != null);

            if (primaryImage != null)
            {
                state.AlbumCoverPath = primaryImage.Path;
            }

            var mediaSources = await _mediaSourceManager.GetPlayackMediaSources(request.ItemId, null, false, new[] { MediaType.Audio, MediaType.Video }, cancellationToken).ConfigureAwait(false);

            var mediaSource = string.IsNullOrEmpty(request.MediaSourceId)
               ? mediaSources.First()
               : mediaSources.First(i => string.Equals(i.Id, request.MediaSourceId));

            var videoRequest = state.Options;

            encodingHelper.AttachMediaSourceInfo(state, mediaSource, null);

            //var container = Path.GetExtension(state.RequestedUrl);

            //if (string.IsNullOrEmpty(container))
            //{
            //    container = request.Static ?
            //        state.InputContainer :
            //        (Path.GetExtension(GetOutputFilePath(state)) ?? string.Empty).TrimStart('.');
            //}

            //state.OutputContainer = (container ?? string.Empty).TrimStart('.');

            state.OutputAudioBitrate = encodingHelper.GetAudioBitrateParam(state.Options, state.AudioStream);
            state.OutputAudioSampleRate = request.AudioSampleRate;

            state.OutputAudioCodec = state.Options.AudioCodec;

            state.OutputAudioChannels = encodingHelper.GetNumAudioChannelsParam(state.Options, state.AudioStream, state.OutputAudioCodec);

            if (videoRequest != null)
            {
                state.OutputVideoCodec = state.Options.VideoCodec;
                state.OutputVideoBitrate = encodingHelper.GetVideoBitrateParamValue(state.Options, state.VideoStream, state.OutputVideoCodec);

                if (state.OutputVideoBitrate.HasValue)
                {
                    var resolution = ResolutionNormalizer.Normalize(
                        state.VideoStream == null ? (int?)null : state.VideoStream.BitRate,
                        state.OutputVideoBitrate.Value,
                        state.VideoStream == null ? null : state.VideoStream.Codec,
                        state.OutputVideoCodec,
                        videoRequest.MaxWidth,
                        videoRequest.MaxHeight);

                    videoRequest.MaxWidth = resolution.MaxWidth;
                    videoRequest.MaxHeight = resolution.MaxHeight;
                }
            }

            ApplyDeviceProfileSettings(state);

            if (videoRequest != null)
            {
                encodingHelper.TryStreamCopy(state);
            }

            //state.OutputFilePath = GetOutputFilePath(state);

            return state;
        }

        protected EncodingOptions GetEncodingOptions()
        {
            return _config.GetConfiguration<EncodingOptions>("encoding");
        }

        /// <summary>
        /// Infers the video codec.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>System.Nullable{VideoCodecs}.</returns>
        private static string InferVideoCodec(string container)
        {
            var ext = "." + (container ?? string.Empty);

            if (string.Equals(ext, ".asf", StringComparison.OrdinalIgnoreCase))
            {
                return "wmv";
            }
            if (string.Equals(ext, ".webm", StringComparison.OrdinalIgnoreCase))
            {
                return "vpx";
            }
            if (string.Equals(ext, ".ogg", StringComparison.OrdinalIgnoreCase) || string.Equals(ext, ".ogv", StringComparison.OrdinalIgnoreCase))
            {
                return "theora";
            }
            if (string.Equals(ext, ".m3u8", StringComparison.OrdinalIgnoreCase) || string.Equals(ext, ".ts", StringComparison.OrdinalIgnoreCase))
            {
                return "h264";
            }

            return "copy";
        }

        private string InferAudioCodec(string container)
        {
            var ext = "." + (container ?? string.Empty);

            if (string.Equals(ext, ".mp3", StringComparison.OrdinalIgnoreCase))
            {
                return "mp3";
            }
            if (string.Equals(ext, ".aac", StringComparison.OrdinalIgnoreCase))
            {
                return "aac";
            }
            if (string.Equals(ext, ".wma", StringComparison.OrdinalIgnoreCase))
            {
                return "wma";
            }
            if (string.Equals(ext, ".ogg", StringComparison.OrdinalIgnoreCase))
            {
                return "vorbis";
            }
            if (string.Equals(ext, ".oga", StringComparison.OrdinalIgnoreCase))
            {
                return "vorbis";
            }
            if (string.Equals(ext, ".ogv", StringComparison.OrdinalIgnoreCase))
            {
                return "vorbis";
            }
            if (string.Equals(ext, ".webm", StringComparison.OrdinalIgnoreCase))
            {
                return "vorbis";
            }
            if (string.Equals(ext, ".webma", StringComparison.OrdinalIgnoreCase))
            {
                return "vorbis";
            }

            return "copy";
        }

        /// <summary>
        /// Determines whether the specified stream is H264.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns><c>true</c> if the specified stream is H264; otherwise, <c>false</c>.</returns>
        protected bool IsH264(MediaStream stream)
        {
            var codec = stream.Codec ?? string.Empty;

            return codec.IndexOf("264", StringComparison.OrdinalIgnoreCase) != -1 ||
                   codec.IndexOf("avc", StringComparison.OrdinalIgnoreCase) != -1;
        }

        private static int GetVideoProfileScore(string profile)
        {
            var list = new List<string>
            {
                "Constrained Baseline",
                "Baseline",
                "Extended",
                "Main",
                "High",
                "Progressive High",
                "Constrained High"
            };

            return Array.FindIndex(list.ToArray(), t => string.Equals(t, profile, StringComparison.OrdinalIgnoreCase));
        }

        private void ApplyDeviceProfileSettings(EncodingJob state)
        {
            var profile = state.Options.DeviceProfile;

            if (profile == null)
            {
                // Don't use settings from the default profile. 
                // Only use a specific profile if it was requested.
                return;
            }

            var audioCodec = state.ActualOutputAudioCodec;

            var videoCodec = state.ActualOutputVideoCodec;
            var outputContainer = state.Options.OutputContainer;

            var mediaProfile = state.IsVideoRequest ?
                profile.GetAudioMediaProfile(outputContainer, audioCodec, state.OutputAudioChannels, state.OutputAudioBitrate, state.OutputAudioSampleRate) :
                profile.GetVideoMediaProfile(outputContainer,
                audioCodec,
                videoCodec,
                state.OutputWidth,
                state.OutputHeight,
                state.TargetVideoBitDepth,
                state.OutputVideoBitrate,
                state.TargetVideoProfile,
                state.TargetVideoLevel,
                state.TargetFramerate,
                state.TargetPacketLength,
                state.TargetTimestamp,
                state.IsTargetAnamorphic,
                state.IsTargetInterlaced,
                state.TargetRefFrames,
                state.TargetVideoStreamCount,
                state.TargetAudioStreamCount,
                state.TargetVideoCodecTag,
                state.IsTargetAVC);

            if (mediaProfile != null)
            {
                state.MimeType = mediaProfile.MimeType;
            }

            var transcodingProfile = state.IsVideoRequest ?
                profile.GetAudioTranscodingProfile(outputContainer, audioCodec) :
                profile.GetVideoTranscodingProfile(outputContainer, audioCodec, videoCodec);

            if (transcodingProfile != null)
            {
                state.EstimateContentLength = transcodingProfile.EstimateContentLength;
                state.EnableMpegtsM2TsMode = transcodingProfile.EnableMpegtsM2TsMode;
                state.TranscodeSeekInfo = transcodingProfile.TranscodeSeekInfo;

                state.Options.CopyTimestamps = transcodingProfile.CopyTimestamps;
            }
        }
    }
}
