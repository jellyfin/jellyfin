using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class EncodingJobFactory
    {
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IMediaEncoder _mediaEncoder;

        public EncodingJobFactory(ILogger logger, ILibraryManager libraryManager, IMediaSourceManager mediaSourceManager, IMediaEncoder mediaEncoder)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _mediaSourceManager = mediaSourceManager;
            _mediaEncoder = mediaEncoder;
        }

        public async Task<EncodingJob> CreateJob(EncodingJobOptions options, EncodingHelper encodingHelper, bool isVideoRequest, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var request = options;

            if (string.IsNullOrEmpty(request.AudioCodec))
            {
                request.AudioCodec = InferAudioCodec(request.Container);
            }

            var state = new EncodingJob(_logger, _mediaSourceManager)
            {
                Options = options,
                IsVideoRequest = isVideoRequest,
                Progress = progress
            };

            if (!string.IsNullOrWhiteSpace(request.VideoCodec))
            {
                state.SupportedVideoCodecs = request.VideoCodec.Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToArray();
                request.VideoCodec = state.SupportedVideoCodecs.FirstOrDefault();
            }

            if (!string.IsNullOrWhiteSpace(request.AudioCodec))
            {
                state.SupportedAudioCodecs = request.AudioCodec.Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToArray();
                request.AudioCodec = state.SupportedAudioCodecs.FirstOrDefault();
            }

            if (!string.IsNullOrWhiteSpace(request.SubtitleCodec))
            {
                state.SupportedSubtitleCodecs = request.SubtitleCodec.Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToArray();
                request.SubtitleCodec = state.SupportedSubtitleCodecs.FirstOrDefault(i => _mediaEncoder.CanEncodeToSubtitleCodec(i))
                    ?? state.SupportedSubtitleCodecs.FirstOrDefault();
            }

            var item = _libraryManager.GetItemById(request.Id);
            state.ItemType = item.GetType().Name;

            state.IsInputVideo = string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase);

            // TODO
            // var primaryImage = item.GetImageInfo(ImageType.Primary, 0) ??
            //                    item.Parents.Select(i => i.GetImageInfo(ImageType.Primary, 0)).FirstOrDefault(i => i != null);

            // if (primaryImage != null)
            // {
            //     state.AlbumCoverPath = primaryImage.Path;
            // }

            // TODO network path substition useful ?
            var mediaSources = await _mediaSourceManager.GetPlayackMediaSources(item, null, true, true, cancellationToken).ConfigureAwait(false);

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

            state.OutputAudioCodec = state.Options.AudioCodec;

            state.OutputAudioChannels = encodingHelper.GetNumAudioChannelsParam(state, state.AudioStream, state.OutputAudioCodec);

            if (videoRequest != null)
            {
                state.OutputVideoCodec = state.Options.VideoCodec;
                state.OutputVideoBitrate = encodingHelper.GetVideoBitrateParamValue(state.Options, state.VideoStream, state.OutputVideoCodec);

                if (state.OutputVideoBitrate.HasValue)
                {
                    var resolution = ResolutionNormalizer.Normalize(
                        state.VideoStream?.BitRate,
                        state.VideoStream?.Width,
                        state.VideoStream?.Height,
                        state.OutputVideoBitrate.Value,
                        state.VideoStream?.Codec,
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

        private string InferAudioCodec(string container)
        {
            var ext = "." + (container ?? string.Empty);

            if (string.Equals(ext, ".mp3", StringComparison.OrdinalIgnoreCase))
            {
                return "mp3";
            }
            else if (string.Equals(ext, ".aac", StringComparison.OrdinalIgnoreCase))
            {
                return "aac";
            }
            else if (string.Equals(ext, ".wma", StringComparison.OrdinalIgnoreCase))
            {
                return "wma";
            }
            else if (string.Equals(ext, ".ogg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".oga", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".ogv", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".webm", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".webma", StringComparison.OrdinalIgnoreCase))
            {
                return "vorbis";
            }

            return "copy";
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
            var outputContainer = state.Options.Container;

            var mediaProfile = state.IsVideoRequest ?
                profile.GetAudioMediaProfile(outputContainer, audioCodec, state.OutputAudioChannels, state.OutputAudioBitrate, state.OutputAudioSampleRate, state.OutputAudioBitDepth) :
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
                //state.EnableMpegtsM2TsMode = transcodingProfile.EnableMpegtsM2TsMode;
                state.TranscodeSeekInfo = transcodingProfile.TranscodeSeekInfo;

                state.Options.CopyTimestamps = transcodingProfile.CopyTimestamps;
            }
        }
    }
}
