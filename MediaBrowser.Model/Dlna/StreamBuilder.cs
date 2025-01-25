using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Class StreamBuilder.
    /// </summary>
    public class StreamBuilder
    {
        // Aliases
        internal const TranscodeReason ContainerReasons = TranscodeReason.ContainerNotSupported | TranscodeReason.ContainerBitrateExceedsLimit;
        internal const TranscodeReason AudioCodecReasons = TranscodeReason.AudioBitrateNotSupported | TranscodeReason.AudioChannelsNotSupported | TranscodeReason.AudioProfileNotSupported | TranscodeReason.AudioSampleRateNotSupported | TranscodeReason.SecondaryAudioNotSupported | TranscodeReason.AudioBitDepthNotSupported | TranscodeReason.AudioIsExternal;
        internal const TranscodeReason AudioReasons = TranscodeReason.AudioCodecNotSupported | AudioCodecReasons;
        internal const TranscodeReason VideoCodecReasons = TranscodeReason.VideoResolutionNotSupported | TranscodeReason.AnamorphicVideoNotSupported | TranscodeReason.InterlacedVideoNotSupported | TranscodeReason.VideoBitDepthNotSupported | TranscodeReason.VideoBitrateNotSupported | TranscodeReason.VideoFramerateNotSupported | TranscodeReason.VideoLevelNotSupported | TranscodeReason.RefFramesNotSupported | TranscodeReason.VideoRangeTypeNotSupported | TranscodeReason.VideoProfileNotSupported;
        internal const TranscodeReason VideoReasons = TranscodeReason.VideoCodecNotSupported | VideoCodecReasons;
        internal const TranscodeReason DirectStreamReasons = AudioReasons | TranscodeReason.ContainerNotSupported | TranscodeReason.VideoCodecTagNotSupported;

        private readonly ILogger _logger;
        private readonly ITranscoderSupport _transcoderSupport;
        private static readonly string[] _supportedHlsVideoCodecs = ["h264", "hevc", "vp9", "av1"];
        private static readonly string[] _supportedHlsAudioCodecsTs = ["aac", "ac3", "eac3", "mp3"];
        private static readonly string[] _supportedHlsAudioCodecsMp4 = ["aac", "ac3", "eac3", "mp3", "alac", "flac", "opus", "dts", "truehd"];

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamBuilder"/> class.
        /// </summary>
        /// <param name="transcoderSupport">The <see cref="ITranscoderSupport"/> object.</param>
        /// <param name="logger">The <see cref="ILogger"/> object.</param>
        public StreamBuilder(ITranscoderSupport transcoderSupport, ILogger logger)
        {
            _transcoderSupport = transcoderSupport;
            _logger = logger;
        }

        /// <summary>
        /// Gets the optimal audio stream.
        /// </summary>
        /// <param name="options">The <see cref="MediaOptions"/> object to get the audio stream from.</param>
        /// <returns>The <see cref="StreamInfo"/> of the optimal audio stream.</returns>
        public StreamInfo? GetOptimalAudioStream(MediaOptions options)
        {
            ValidateMediaOptions(options, false);

            List<StreamInfo> streams = [];
            foreach (var mediaSource in options.MediaSources)
            {
                if (!(string.IsNullOrEmpty(options.MediaSourceId)
                    || string.Equals(mediaSource.Id, options.MediaSourceId, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                StreamInfo? streamInfo = GetOptimalAudioStream(mediaSource, options);
                if (streamInfo is not null)
                {
                    streamInfo.DeviceId = options.DeviceId;
                    streamInfo.DeviceProfileId = options.Profile.Id?.ToString("N", CultureInfo.InvariantCulture);
                    streams.Add(streamInfo);
                }
            }

            return GetOptimalStream(streams, options.GetMaxBitrate(true) ?? 0);
        }

        private StreamInfo? GetOptimalAudioStream(MediaSourceInfo item, MediaOptions options)
        {
            var playlistItem = new StreamInfo
            {
                ItemId = options.ItemId,
                MediaType = DlnaProfileType.Audio,
                MediaSource = item,
                RunTimeTicks = item.RunTimeTicks,
                Context = options.Context,
                DeviceProfile = options.Profile
            };

            if (options.ForceDirectPlay)
            {
                playlistItem.PlayMethod = PlayMethod.DirectPlay;
                playlistItem.Container = NormalizeMediaSourceFormatIntoSingleContainer(item.Container, options.Profile, DlnaProfileType.Audio);
                return playlistItem;
            }

            if (options.ForceDirectStream)
            {
                playlistItem.PlayMethod = PlayMethod.DirectStream;
                playlistItem.Container = NormalizeMediaSourceFormatIntoSingleContainer(item.Container, options.Profile, DlnaProfileType.Audio);
                return playlistItem;
            }

            MediaStream audioStream = item.GetDefaultAudioStream(null);

            var directPlayInfo = GetAudioDirectPlayProfile(item, audioStream, options);

            var directPlayMethod = directPlayInfo.PlayMethod;
            var transcodeReasons = directPlayInfo.TranscodeReasons;

            var inputAudioChannels = audioStream?.Channels;
            var inputAudioBitrate = audioStream?.BitRate;
            var inputAudioSampleRate = audioStream?.SampleRate;
            var inputAudioBitDepth = audioStream?.BitDepth;

            if (directPlayMethod is PlayMethod.DirectPlay)
            {
                var profile = options.Profile;
                var audioFailureConditions = GetProfileConditionsForAudio(profile.CodecProfiles, item.Container, audioStream?.Codec, inputAudioChannels, inputAudioBitrate, inputAudioSampleRate, inputAudioBitDepth, true);
                var audioFailureReasons = AggregateFailureConditions(item, profile, "AudioCodecProfile", audioFailureConditions);
                transcodeReasons |= audioFailureReasons;

                if (audioFailureReasons == 0)
                {
                    playlistItem.PlayMethod = directPlayMethod.Value;
                    playlistItem.Container = NormalizeMediaSourceFormatIntoSingleContainer(item.Container, options.Profile, DlnaProfileType.Audio, directPlayInfo.Profile);

                    return playlistItem;
                }
            }

            if (directPlayMethod is PlayMethod.DirectStream)
            {
                var remuxContainer = item.TranscodingContainer ?? "ts";
                string[] supportedHlsContainers = ["ts", "mp4"];
                // If the container specified for the profile is an HLS supported container, use that container instead, overriding the preference
                // The client should be responsible to ensure this container is compatible
                remuxContainer = Array.Exists(supportedHlsContainers, element => string.Equals(element, directPlayInfo.Profile?.Container, StringComparison.OrdinalIgnoreCase)) ? directPlayInfo.Profile?.Container : remuxContainer;
                bool codeIsSupported;
                if (item.TranscodingSubProtocol == MediaStreamProtocol.hls)
                {
                    // Enforce HLS audio codec restrictions
                    if (string.Equals(remuxContainer, "mp4", StringComparison.OrdinalIgnoreCase))
                    {
                        codeIsSupported = _supportedHlsAudioCodecsMp4.Contains(directPlayInfo.Profile?.AudioCodec ?? directPlayInfo.Profile?.Container);
                    }
                    else
                    {
                        codeIsSupported = _supportedHlsAudioCodecsTs.Contains(directPlayInfo.Profile?.AudioCodec ?? directPlayInfo.Profile?.Container);
                    }
                }
                else
                {
                    // Let's assume the client has given a correct container for http
                    codeIsSupported = true;
                }

                if (codeIsSupported)
                {
                    playlistItem.PlayMethod = directPlayMethod.Value;
                    playlistItem.Container = remuxContainer;
                    playlistItem.TranscodeReasons = transcodeReasons;
                    playlistItem.SubProtocol = item.TranscodingSubProtocol;
                    item.TranscodingContainer = remuxContainer;
                    return playlistItem;
                }

                transcodeReasons |= TranscodeReason.AudioCodecNotSupported;
                playlistItem.TranscodeReasons = transcodeReasons;
            }

            TranscodingProfile? transcodingProfile = null;
            foreach (var tcProfile in options.Profile.TranscodingProfiles)
            {
                if (tcProfile.Type == playlistItem.MediaType
                    && tcProfile.Context == options.Context
                    && _transcoderSupport.CanEncodeToAudioCodec(tcProfile.AudioCodec ?? tcProfile.Container))
                {
                    transcodingProfile = tcProfile;
                    break;
                }
            }

            if (transcodingProfile is not null)
            {
                if (!item.SupportsTranscoding)
                {
                    return null;
                }

                SetStreamInfoOptionsFromTranscodingProfile(item, playlistItem, transcodingProfile);

                var audioTranscodingConditions = GetProfileConditionsForAudio(options.Profile.CodecProfiles, transcodingProfile.Container, transcodingProfile.AudioCodec, inputAudioChannels, inputAudioBitrate, inputAudioSampleRate, inputAudioBitDepth, false).ToArray();
                ApplyTranscodingConditions(playlistItem, audioTranscodingConditions, null, true, true);

                // Honor requested max channels
                playlistItem.GlobalMaxAudioChannels = options.MaxAudioChannels;

                var configuredBitrate = options.GetMaxBitrate(true);

                long transcodingBitrate = options.AudioTranscodingBitrate
                    ?? (options.Context == EncodingContext.Streaming ? options.Profile.MusicStreamingTranscodingBitrate : null)
                    ?? configuredBitrate
                    ?? 128000;

                if (configuredBitrate.HasValue)
                {
                    transcodingBitrate = Math.Min(configuredBitrate.Value, transcodingBitrate);
                }

                var longBitrate = Math.Min(transcodingBitrate, playlistItem.AudioBitrate ?? transcodingBitrate);
                playlistItem.AudioBitrate = longBitrate > int.MaxValue ? int.MaxValue : Convert.ToInt32(longBitrate);

                // Pure audio transcoding does not support comma separated list of transcoding codec at the moment.
                // So just use the AudioCodec as is would be safe enough as the _transcoderSupport.CanEncodeToAudioCodec
                // would fail so this profile will not even be picked up.
                if (playlistItem.AudioCodecs.Count == 0 && !string.IsNullOrWhiteSpace(transcodingProfile.AudioCodec))
                {
                    playlistItem.AudioCodecs = [transcodingProfile.AudioCodec];
                }
            }

            playlistItem.TranscodeReasons = transcodeReasons;
            return playlistItem;
        }

        /// <summary>
        /// Gets the optimal video stream.
        /// </summary>
        /// <param name="options">The <see cref="MediaOptions"/> object to get the video stream from.</param>
        /// <returns>The <see cref="StreamInfo"/> of the optimal video stream.</returns>
        public StreamInfo? GetOptimalVideoStream(MediaOptions options)
        {
            ValidateMediaOptions(options, true);

            var mediaSources = string.IsNullOrEmpty(options.MediaSourceId)
                ? options.MediaSources
                : options.MediaSources.Where(x => string.Equals(x.Id, options.MediaSourceId, StringComparison.OrdinalIgnoreCase));

            List<StreamInfo> streams = [];
            foreach (var mediaSourceInfo in mediaSources)
            {
                var streamInfo = BuildVideoItem(mediaSourceInfo, options);
                if (streamInfo is not null)
                {
                    streams.Add(streamInfo);
                }
            }

            foreach (var stream in streams)
            {
                stream.DeviceId = options.DeviceId;
                stream.DeviceProfileId = options.Profile.Id?.ToString("N", CultureInfo.InvariantCulture);
            }

            return GetOptimalStream(streams, options.GetMaxBitrate(false) ?? 0);
        }

        private static StreamInfo? GetOptimalStream(List<StreamInfo> streams, long maxBitrate)
            => SortMediaSources(streams, maxBitrate).FirstOrDefault();

        private static IOrderedEnumerable<StreamInfo> SortMediaSources(List<StreamInfo> streams, long maxBitrate)
        {
            return streams.OrderBy(i =>
            {
                // Nothing beats direct playing a file
                if (i.PlayMethod == PlayMethod.DirectPlay && i.MediaSource?.Protocol == MediaProtocol.File)
                {
                    return 0;
                }

                return 1;
            }).ThenBy(i =>
            {
                switch (i.PlayMethod)
                {
                    // Let's assume direct streaming a file is just as desirable as direct playing a remote url
                    case PlayMethod.DirectStream:
                    case PlayMethod.DirectPlay:
                        return 0;
                    default:
                        return 1;
                }
            }).ThenBy(i =>
            {
                switch (i.MediaSource?.Protocol)
                {
                    case MediaProtocol.File:
                        return 0;
                    default:
                        return 1;
                }
            }).ThenBy(i =>
            {
                if (maxBitrate > 0)
                {
                    if (i.MediaSource?.Bitrate is not null)
                    {
                        return Math.Abs(i.MediaSource.Bitrate.Value - maxBitrate);
                    }
                }

                return 0;
            }).ThenBy(streams.IndexOf);
        }

        private static TranscodeReason GetTranscodeReasonForFailedCondition(ProfileCondition condition)
        {
            switch (condition.Property)
            {
                case ProfileConditionValue.AudioBitrate:
                    return TranscodeReason.AudioBitrateNotSupported;

                case ProfileConditionValue.AudioChannels:
                    return TranscodeReason.AudioChannelsNotSupported;

                case ProfileConditionValue.AudioProfile:
                    return TranscodeReason.AudioProfileNotSupported;

                case ProfileConditionValue.AudioSampleRate:
                    return TranscodeReason.AudioSampleRateNotSupported;

                case ProfileConditionValue.Has64BitOffsets:
                    // TODO
                    return 0;

                case ProfileConditionValue.Height:
                    return TranscodeReason.VideoResolutionNotSupported;

                case ProfileConditionValue.IsAnamorphic:
                    return TranscodeReason.AnamorphicVideoNotSupported;

                case ProfileConditionValue.IsAvc:
                    // TODO
                    return 0;

                case ProfileConditionValue.IsInterlaced:
                    return TranscodeReason.InterlacedVideoNotSupported;

                case ProfileConditionValue.IsSecondaryAudio:
                    return TranscodeReason.SecondaryAudioNotSupported;

                case ProfileConditionValue.NumAudioStreams:
                    // TODO
                    return 0;

                case ProfileConditionValue.NumVideoStreams:
                    // TODO
                    return 0;

                case ProfileConditionValue.PacketLength:
                    // TODO
                    return 0;

                case ProfileConditionValue.RefFrames:
                    return TranscodeReason.RefFramesNotSupported;

                case ProfileConditionValue.VideoBitDepth:
                    return TranscodeReason.VideoBitDepthNotSupported;

                case ProfileConditionValue.AudioBitDepth:
                    return TranscodeReason.AudioBitDepthNotSupported;

                case ProfileConditionValue.VideoBitrate:
                    return TranscodeReason.VideoBitrateNotSupported;

                case ProfileConditionValue.VideoCodecTag:
                    return TranscodeReason.VideoCodecTagNotSupported;

                case ProfileConditionValue.VideoFramerate:
                    return TranscodeReason.VideoFramerateNotSupported;

                case ProfileConditionValue.VideoLevel:
                    return TranscodeReason.VideoLevelNotSupported;

                case ProfileConditionValue.VideoProfile:
                    return TranscodeReason.VideoProfileNotSupported;

                case ProfileConditionValue.VideoRangeType:
                    return TranscodeReason.VideoRangeTypeNotSupported;

                case ProfileConditionValue.VideoTimestamp:
                    // TODO
                    return 0;

                case ProfileConditionValue.Width:
                    return TranscodeReason.VideoResolutionNotSupported;

                default:
                    return 0;
            }
        }

        /// <summary>
        /// Normalizes input container.
        /// </summary>
        /// <param name="inputContainer">The input container.</param>
        /// <param name="profile">The <see cref="DeviceProfile"/>.</param>
        /// <param name="type">The <see cref="DlnaProfileType"/>.</param>
        /// <param name="playProfile">The <see cref="DirectPlayProfile"/> object to get the video stream from.</param>
        /// <returns>The normalized input container.</returns>
        public static string? NormalizeMediaSourceFormatIntoSingleContainer(string inputContainer, DeviceProfile? profile, DlnaProfileType type, DirectPlayProfile? playProfile = null)
        {
            // If the source is Live TV the inputContainer will be null until the mediasource is probed on first access
            if (profile is null || string.IsNullOrEmpty(inputContainer) || !inputContainer.Contains(',', StringComparison.OrdinalIgnoreCase))
            {
                return inputContainer;
            }

            var formats = ContainerHelper.Split(inputContainer);
            var playProfiles = playProfile is null ? profile.DirectPlayProfiles : [playProfile];
            foreach (var format in formats)
            {
                foreach (var directPlayProfile in playProfiles)
                {
                    if (directPlayProfile.Type != type)
                    {
                        continue;
                    }

                    if (directPlayProfile.SupportsContainer(format))
                    {
                        return format;
                    }
                }
            }

            return inputContainer;
        }

        private (DirectPlayProfile? Profile, PlayMethod? PlayMethod, TranscodeReason TranscodeReasons) GetAudioDirectPlayProfile(MediaSourceInfo item, MediaStream audioStream, MediaOptions options)
        {
            var directPlayProfile = options.Profile.DirectPlayProfiles
                .FirstOrDefault(x => x.Type == DlnaProfileType.Audio && IsAudioDirectPlaySupported(x, item, audioStream));

            TranscodeReason transcodeReasons = 0;
            if (directPlayProfile is null)
            {
                _logger.LogDebug(
                    "Profile: {0}, No audio direct play profiles found for {1} with codec {2}",
                    options.Profile.Name ?? "Unknown Profile",
                    item.Path ?? "Unknown path",
                    audioStream.Codec ?? "Unknown codec");

                var directStreamProfile = options.Profile.DirectPlayProfiles
                    .FirstOrDefault(x => x.Type == DlnaProfileType.Audio && IsAudioDirectStreamSupported(x, item, audioStream));

                if (directStreamProfile is not null)
                {
                    directPlayProfile = directStreamProfile;
                    transcodeReasons |= TranscodeReason.ContainerNotSupported;
                }
                else
                {
                    return (null, null, GetTranscodeReasonsFromDirectPlayProfile(item, null, audioStream, options.Profile.DirectPlayProfiles));
                }
            }

            // The profile describes what the device supports
            // If device requirements are satisfied then allow both direct stream and direct play
            // Note: As of 10.10 codebase, SupportsDirectPlay is always true because the MediaSourceInfo initializes this key as true
            // Need to check additionally for current transcode reasons
            if (item.SupportsDirectPlay && transcodeReasons == 0)
            {
                if (!IsBitrateLimitExceeded(item, options.GetMaxBitrate(true) ?? 0))
                {
                    if (options.EnableDirectPlay)
                    {
                        return (directPlayProfile, PlayMethod.DirectPlay, 0);
                    }
                }
                else
                {
                    transcodeReasons |= TranscodeReason.ContainerBitrateExceedsLimit;
                }
            }

            // While options takes the network and other factors into account. Only applies to direct stream
            if (item.SupportsDirectStream)
            {
                if (!IsBitrateLimitExceeded(item, options.GetMaxBitrate(true) ?? 0))
                {
                    // Note: as of 10.10 codebase, the options.EnableDirectStream is always false due to
                    // "direct-stream http streaming is currently broken"
                    // Don't check that option for audio as we always assume that is supported
                    if (transcodeReasons == TranscodeReason.ContainerNotSupported)
                    {
                        return (directPlayProfile, PlayMethod.DirectStream, transcodeReasons);
                    }
                }
                else
                {
                    transcodeReasons |= TranscodeReason.ContainerBitrateExceedsLimit;
                }
            }

            return (directPlayProfile, null, transcodeReasons);
        }

        private static TranscodeReason GetTranscodeReasonsFromDirectPlayProfile(MediaSourceInfo item, MediaStream? videoStream, MediaStream audioStream, IEnumerable<DirectPlayProfile> directPlayProfiles)
        {
            var mediaType = videoStream is null ? DlnaProfileType.Audio : DlnaProfileType.Video;

            var containerSupported = false;
            var audioSupported = false;
            var videoSupported = false;

            foreach (var profile in directPlayProfiles)
            {
                // Check container type
                if (profile.Type == mediaType && profile.SupportsContainer(item.Container))
                {
                    containerSupported = true;

                    videoSupported = videoStream is null || profile.SupportsVideoCodec(videoStream.Codec);

                    audioSupported = audioStream is null || profile.SupportsAudioCodec(audioStream.Codec);

                    if (videoSupported && audioSupported)
                    {
                        break;
                    }
                }
            }

            TranscodeReason reasons = 0;
            if (!containerSupported)
            {
                reasons |= TranscodeReason.ContainerNotSupported;
            }

            if (!videoSupported)
            {
                reasons |= TranscodeReason.VideoCodecNotSupported;
            }

            if (!audioSupported)
            {
                reasons |= TranscodeReason.AudioCodecNotSupported;
            }

            return reasons;
        }

        private static int? GetDefaultSubtitleStreamIndex(MediaSourceInfo item, SubtitleProfile[] subtitleProfiles)
        {
            int highestScore = -1;
            foreach (var stream in item.MediaStreams)
            {
                if (stream.Type == MediaStreamType.Subtitle
                    && stream.Score.HasValue
                    && stream.Score.Value > highestScore)
                {
                    highestScore = stream.Score.Value;
                }
            }

            List<MediaStream> topStreams = [];
            foreach (var stream in item.MediaStreams)
            {
                if (stream.Type == MediaStreamType.Subtitle && stream.Score.HasValue && stream.Score.Value == highestScore)
                {
                    topStreams.Add(stream);
                }
            }

            // If multiple streams have an equal score, try to pick the most efficient one
            if (topStreams.Count > 1)
            {
                foreach (var stream in topStreams)
                {
                    foreach (var profile in subtitleProfiles)
                    {
                        if (profile.Method == SubtitleDeliveryMethod.External && string.Equals(profile.Format, stream.Codec, StringComparison.OrdinalIgnoreCase))
                        {
                            return stream.Index;
                        }
                    }
                }
            }

            // If no optimization panned out, just use the original default
            return item.DefaultSubtitleStreamIndex;
        }

        private static void SetStreamInfoOptionsFromTranscodingProfile(MediaSourceInfo item, StreamInfo playlistItem, TranscodingProfile transcodingProfile)
        {
            var container = transcodingProfile.Container;
            var protocol = transcodingProfile.Protocol;

            item.TranscodingContainer = container;
            item.TranscodingSubProtocol = protocol;

            if (playlistItem.PlayMethod == PlayMethod.Transcode)
            {
                playlistItem.Container = container;
                playlistItem.SubProtocol = protocol;
            }

            playlistItem.TranscodeSeekInfo = transcodingProfile.TranscodeSeekInfo;
            if (int.TryParse(transcodingProfile.MaxAudioChannels, CultureInfo.InvariantCulture, out int transcodingMaxAudioChannels))
            {
                playlistItem.TranscodingMaxAudioChannels = transcodingMaxAudioChannels;
            }

            playlistItem.EstimateContentLength = transcodingProfile.EstimateContentLength;

            playlistItem.CopyTimestamps = transcodingProfile.CopyTimestamps;
            playlistItem.EnableSubtitlesInManifest = transcodingProfile.EnableSubtitlesInManifest;
            playlistItem.EnableMpegtsM2TsMode = transcodingProfile.EnableMpegtsM2TsMode;

            playlistItem.BreakOnNonKeyFrames = transcodingProfile.BreakOnNonKeyFrames;
            playlistItem.EnableAudioVbrEncoding = transcodingProfile.EnableAudioVbrEncoding;

            if (transcodingProfile.MinSegments > 0)
            {
                playlistItem.MinSegments = transcodingProfile.MinSegments;
            }

            if (transcodingProfile.SegmentLength > 0)
            {
                playlistItem.SegmentLength = transcodingProfile.SegmentLength;
            }
        }

        private static void SetStreamInfoOptionsFromDirectPlayProfile(MediaOptions options, MediaSourceInfo item, StreamInfo playlistItem, DirectPlayProfile? directPlayProfile)
        {
            var container = NormalizeMediaSourceFormatIntoSingleContainer(item.Container, options.Profile, DlnaProfileType.Video, directPlayProfile);
            var protocol = MediaStreamProtocol.http;

            item.TranscodingContainer = container;
            item.TranscodingSubProtocol = protocol;

            playlistItem.Container = container;
            playlistItem.SubProtocol = protocol;

            playlistItem.VideoCodecs = [item.VideoStream.Codec];
            playlistItem.AudioCodecs = ContainerHelper.Split(directPlayProfile?.AudioCodec);
        }

        private StreamInfo BuildVideoItem(MediaSourceInfo item, MediaOptions options)
        {
            ArgumentNullException.ThrowIfNull(item);

            StreamInfo playlistItem = new StreamInfo
            {
                ItemId = options.ItemId,
                MediaType = DlnaProfileType.Video,
                MediaSource = item,
                RunTimeTicks = item.RunTimeTicks,
                Context = options.Context,
                DeviceProfile = options.Profile,
                SubtitleStreamIndex = options.SubtitleStreamIndex ?? GetDefaultSubtitleStreamIndex(item, options.Profile.SubtitleProfiles),
                AlwaysBurnInSubtitleWhenTranscoding = options.AlwaysBurnInSubtitleWhenTranscoding
            };

            var subtitleStream = playlistItem.SubtitleStreamIndex.HasValue ? item.GetMediaStream(MediaStreamType.Subtitle, playlistItem.SubtitleStreamIndex.Value) : null;

            var audioStream = item.GetDefaultAudioStream(options.AudioStreamIndex ?? item.DefaultAudioStreamIndex);
            if (audioStream is not null)
            {
                playlistItem.AudioStreamIndex = audioStream.Index;
            }

            // Collect candidate audio streams
            ICollection<MediaStream> candidateAudioStreams = audioStream is null ? [] : [audioStream];
            if (!options.AudioStreamIndex.HasValue || options.AudioStreamIndex < 0)
            {
                if (audioStream?.IsDefault == true)
                {
                    candidateAudioStreams = item.MediaStreams.Where(stream => stream.Type == MediaStreamType.Audio && stream.IsDefault).ToArray();
                }
                else
                {
                    candidateAudioStreams = item.MediaStreams.Where(stream => stream.Type == MediaStreamType.Audio && stream.Language == audioStream?.Language).ToArray();
                }
            }

            var videoStream = item.VideoStream;

            var bitrateLimitExceeded = IsBitrateLimitExceeded(item, options.GetMaxBitrate(false) ?? 0);
            var isEligibleForDirectPlay = options.EnableDirectPlay && (options.ForceDirectPlay || !bitrateLimitExceeded);
            var isEligibleForDirectStream = options.EnableDirectStream && (options.ForceDirectStream || !bitrateLimitExceeded);
            TranscodeReason transcodeReasons = 0;

            // Force transcode or remux for BD/DVD folders
            if (item.VideoType == VideoType.Dvd || item.VideoType == VideoType.BluRay)
            {
                isEligibleForDirectPlay = false;
            }

            if (bitrateLimitExceeded)
            {
                transcodeReasons = TranscodeReason.ContainerBitrateExceedsLimit;
            }

            _logger.LogDebug(
                "Profile: {0}, Path: {1}, isEligibleForDirectPlay: {2}, isEligibleForDirectStream: {3}",
                options.Profile.Name ?? "Unknown Profile",
                item.Path ?? "Unknown path",
                isEligibleForDirectPlay,
                isEligibleForDirectStream);

            DirectPlayProfile? directPlayProfile = null;
            if (isEligibleForDirectPlay || isEligibleForDirectStream)
            {
                // See if it can be direct played
                var directPlayInfo = GetVideoDirectPlayProfile(options, item, videoStream, audioStream, candidateAudioStreams, subtitleStream, isEligibleForDirectPlay, isEligibleForDirectStream);
                var directPlay = directPlayInfo.PlayMethod;
                transcodeReasons |= directPlayInfo.TranscodeReasons;

                if (directPlay.HasValue)
                {
                    directPlayProfile = directPlayInfo.Profile;
                    playlistItem.PlayMethod = directPlay.Value;
                    playlistItem.Container = NormalizeMediaSourceFormatIntoSingleContainer(item.Container, options.Profile, DlnaProfileType.Video, directPlayProfile);
                    var videoCodec = videoStream?.Codec;
                    playlistItem.VideoCodecs = videoCodec is null ? [] : [videoCodec];

                    if (directPlay == PlayMethod.DirectPlay)
                    {
                        playlistItem.SubProtocol = MediaStreamProtocol.http;

                        var audioStreamIndex = directPlayInfo.AudioStreamIndex ?? audioStream?.Index;
                        if (audioStreamIndex.HasValue)
                        {
                            playlistItem.AudioStreamIndex = audioStreamIndex;
                            var audioCodec = item.GetMediaStream(MediaStreamType.Audio, audioStreamIndex.Value)?.Codec;
                            playlistItem.AudioCodecs = audioCodec is null ? [] : [audioCodec];
                        }
                    }
                    else if (directPlay == PlayMethod.DirectStream)
                    {
                        playlistItem.AudioStreamIndex = audioStream?.Index;
                        if (audioStream is not null)
                        {
                            playlistItem.AudioCodecs = ContainerHelper.Split(directPlayProfile?.AudioCodec);
                        }

                        SetStreamInfoOptionsFromDirectPlayProfile(options, item, playlistItem, directPlayProfile);
                        BuildStreamVideoItem(playlistItem, options, item, videoStream, audioStream, candidateAudioStreams, directPlayProfile?.Container, directPlayProfile?.VideoCodec, directPlayProfile?.AudioCodec);
                    }

                    if (subtitleStream is not null)
                    {
                        var subtitleProfile = GetSubtitleProfile(item, subtitleStream, options.Profile.SubtitleProfiles, directPlay.Value, _transcoderSupport, directPlayProfile?.Container, null);

                        playlistItem.SubtitleDeliveryMethod = subtitleProfile.Method;
                        playlistItem.SubtitleFormat = subtitleProfile.Format;
                    }
                }

                _logger.LogDebug(
                    "DirectPlay Result for Profile: {0}, Path: {1}, PlayMethod: {2}, AudioStreamIndex: {3}, SubtitleStreamIndex: {4}, Reasons: {5}",
                    options.Profile.Name ?? "Anonymous Profile",
                    item.Path ?? "Unknown path",
                    directPlayInfo.PlayMethod,
                    directPlayInfo.AudioStreamIndex ?? audioStream?.Index,
                    playlistItem.SubtitleStreamIndex,
                    directPlayInfo.TranscodeReasons);
            }

            playlistItem.TranscodeReasons = transcodeReasons;

            if (playlistItem.PlayMethod != PlayMethod.DirectStream && playlistItem.PlayMethod != PlayMethod.DirectPlay)
            {
                // Can't direct play, find the transcoding profile
                // If we do this for direct-stream we will overwrite the info
                var (transcodingProfile, playMethod) = GetVideoTranscodeProfile(item, options, videoStream, audioStream, playlistItem);

                if (transcodingProfile is not null && playMethod.HasValue)
                {
                    SetStreamInfoOptionsFromTranscodingProfile(item, playlistItem, transcodingProfile);

                    BuildStreamVideoItem(playlistItem, options, item, videoStream, audioStream, candidateAudioStreams, transcodingProfile.Container, transcodingProfile.VideoCodec, transcodingProfile.AudioCodec);

                    playlistItem.PlayMethod = PlayMethod.Transcode;

                    if (subtitleStream is not null)
                    {
                        var subtitleProfile = GetSubtitleProfile(item, subtitleStream, options.Profile.SubtitleProfiles, PlayMethod.Transcode, _transcoderSupport, transcodingProfile.Container, transcodingProfile.Protocol);
                        playlistItem.SubtitleDeliveryMethod = subtitleProfile.Method;
                        playlistItem.SubtitleFormat = subtitleProfile.Format;
                        playlistItem.SubtitleCodecs = [subtitleProfile.Format];
                    }

                    if ((playlistItem.TranscodeReasons & (VideoReasons | TranscodeReason.ContainerBitrateExceedsLimit)) != 0)
                    {
                        ApplyTranscodingConditions(playlistItem, transcodingProfile.Conditions, null, true, true);
                    }
                }
            }

            _logger.LogDebug(
                "StreamBuilder.BuildVideoItem( Profile={0}, Path={1}, AudioStreamIndex={2}, SubtitleStreamIndex={3} ) => ( PlayMethod={4}, TranscodeReason={5} ) {6}",
                options.Profile.Name ?? "Anonymous Profile",
                item.Path ?? "Unknown path",
                options.AudioStreamIndex,
                options.SubtitleStreamIndex,
                playlistItem.PlayMethod,
                playlistItem.TranscodeReasons,
                playlistItem.ToUrl("media:", "<token>"));

            item.Container = NormalizeMediaSourceFormatIntoSingleContainer(item.Container, options.Profile, DlnaProfileType.Video, directPlayProfile);
            return playlistItem;
        }

        private (TranscodingProfile? Profile, PlayMethod? PlayMethod) GetVideoTranscodeProfile(
            MediaSourceInfo item,
            MediaOptions options,
            MediaStream? videoStream,
            MediaStream? audioStream,
            StreamInfo playlistItem)
        {
            if (!(item.SupportsTranscoding || item.SupportsDirectStream))
            {
                return (null, null);
            }

            var transcodingProfiles = options.Profile.TranscodingProfiles
                .Where(i => i.Type == playlistItem.MediaType && i.Context == options.Context);

            if (item.UseMostCompatibleTranscodingProfile)
            {
                transcodingProfiles = transcodingProfiles.Where(i => string.Equals(i.Container, "ts", StringComparison.OrdinalIgnoreCase));
            }

            var videoCodec = videoStream?.Codec;
            float videoFramerate = videoStream?.ReferenceFrameRate ?? 0;
            TransportStreamTimestamp? timestamp = videoStream is null ? TransportStreamTimestamp.None : item.Timestamp;
            int? numAudioStreams = item.GetStreamCount(MediaStreamType.Audio);
            int? numVideoStreams = item.GetStreamCount(MediaStreamType.Video);

            var audioCodec = audioStream?.Codec;
            var audioProfile = audioStream?.Profile;
            var audioChannels = audioStream?.Channels;
            var audioBitrate = audioStream?.BitRate;
            var audioSampleRate = audioStream?.SampleRate;
            var audioBitDepth = audioStream?.BitDepth;

            var analyzedProfiles = transcodingProfiles
                .Select(transcodingProfile =>
                {
                    var rank = (Video: 3, Audio: 3);

                    var container = transcodingProfile.Container;

                    if (options.AllowVideoStreamCopy)
                    {
                        if (ContainerHelper.ContainsContainer(transcodingProfile.VideoCodec, videoCodec))
                        {
                            var appliedVideoConditions = options.Profile.CodecProfiles
                                .Where(i => i.Type == CodecType.Video &&
                                    i.ContainsAnyCodec(videoCodec, container) &&
                                    i.ApplyConditions.All(applyCondition => ConditionProcessor.IsVideoConditionSatisfied(applyCondition, videoStream?.Width, videoStream?.Height, videoStream?.BitDepth, videoStream?.BitRate, videoStream?.Profile, videoStream?.VideoRangeType, videoStream?.Level, videoFramerate, videoStream?.PacketLength, timestamp, videoStream?.IsAnamorphic, videoStream?.IsInterlaced, videoStream?.RefFrames, numVideoStreams, numAudioStreams, videoStream?.CodecTag, videoStream?.IsAVC)))
                                .Select(i =>
                                    i.Conditions.All(condition => ConditionProcessor.IsVideoConditionSatisfied(condition, videoStream?.Width, videoStream?.Height, videoStream?.BitDepth, videoStream?.BitRate, videoStream?.Profile, videoStream?.VideoRangeType, videoStream?.Level, videoFramerate, videoStream?.PacketLength, timestamp, videoStream?.IsAnamorphic, videoStream?.IsInterlaced, videoStream?.RefFrames, numVideoStreams, numAudioStreams, videoStream?.CodecTag, videoStream?.IsAVC)));

                            // An empty appliedVideoConditions means that the codec has no conditions for the current video stream
                            var conditionsSatisfied = appliedVideoConditions.All(satisfied => satisfied);
                            rank.Video = conditionsSatisfied ? 1 : 2;
                        }
                    }

                    if (options.AllowAudioStreamCopy)
                    {
                        // For Audio stream, we prefer the audio codec that can be directly copied, then the codec that can otherwise satisfies
                        // the transcoding conditions, then the one does not satisfy the transcoding conditions.
                        // For example: A client can support both aac and flac, but flac only supports 2 channels while aac supports 6.
                        // When the source audio is 6 channel flac, we should transcode to 6 channel aac, instead of down-mix to 2 channel flac.
                        var transcodingAudioCodecs = ContainerHelper.Split(transcodingProfile.AudioCodec);

                        foreach (var transcodingAudioCodec in transcodingAudioCodecs)
                        {
                            var appliedVideoConditions = options.Profile.CodecProfiles
                                .Where(i => i.Type == CodecType.VideoAudio &&
                                    i.ContainsAnyCodec(transcodingAudioCodec, container) &&
                                    i.ApplyConditions.All(applyCondition => ConditionProcessor.IsVideoAudioConditionSatisfied(applyCondition, audioChannels, audioBitrate, audioSampleRate, audioBitDepth, audioProfile, false)))
                                .Select(i =>
                                    i.Conditions.All(condition => ConditionProcessor.IsVideoAudioConditionSatisfied(condition, audioChannels, audioBitrate, audioSampleRate, audioBitDepth, audioProfile, false)));

                            // An empty appliedVideoConditions means that the codec has no conditions for the current audio stream
                            var conditionsSatisfied = appliedVideoConditions.All(satisfied => satisfied);

                            var rankAudio = 3;

                            if (conditionsSatisfied)
                            {
                                rankAudio = string.Equals(transcodingAudioCodec, audioCodec, StringComparison.OrdinalIgnoreCase) ? 1 : 2;
                            }

                            rank.Audio = Math.Min(rank.Audio, rankAudio);

                            if (rank.Audio == 1)
                            {
                                break;
                            }
                        }
                    }

                    PlayMethod playMethod = PlayMethod.Transcode;

                    if (rank.Video == 1)
                    {
                        playMethod = PlayMethod.DirectStream;
                    }

                    return (Profile: transcodingProfile, PlayMethod: playMethod, Rank: rank);
                })
                .OrderBy(analysis => analysis.Rank);

            var profileMatch = analyzedProfiles.FirstOrDefault();

            return (profileMatch.Profile, profileMatch.PlayMethod);
        }

        private void BuildStreamVideoItem(
            StreamInfo playlistItem,
            MediaOptions options,
            MediaSourceInfo item,
            MediaStream? videoStream,
            MediaStream? audioStream,
            IEnumerable<MediaStream> candidateAudioStreams,
            string? container,
            string? videoCodec,
            string? audioCodec)
        {
            // Prefer matching video codecs
            var videoCodecs = ContainerHelper.Split(videoCodec).ToList();

            if (videoCodecs.Count == 0 && videoStream is not null)
            {
                // Add the original codec if no codec is specified
                videoCodecs.Add(videoStream.Codec);
            }

            // Enforce HLS video codec restrictions
            if (playlistItem.SubProtocol == MediaStreamProtocol.hls)
            {
                videoCodecs = videoCodecs.Where(codec => _supportedHlsVideoCodecs.Contains(codec)).ToList();
            }

            playlistItem.VideoCodecs = videoCodecs;

            // Copy video codec options as a starting point, this applies to transcode and direct-stream
            playlistItem.MaxFramerate = videoStream?.ReferenceFrameRate;
            var qualifier = videoStream?.Codec;
            if (videoStream?.Level is not null)
            {
                playlistItem.SetOption(qualifier, "level", videoStream.Level.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (videoStream?.BitDepth is not null)
            {
                playlistItem.SetOption(qualifier, "videobitdepth", videoStream.BitDepth.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrEmpty(videoStream?.Profile))
            {
                playlistItem.SetOption(qualifier, "profile", videoStream.Profile.ToLowerInvariant());
            }

            // Prefer matching audio codecs, could do better here
            var audioCodecs = ContainerHelper.Split(audioCodec).ToList();

            if (audioCodecs.Count == 0 && audioStream is not null)
            {
                // Add the original codec if no codec is specified
                audioCodecs.Add(audioStream.Codec);
            }

            // Enforce HLS audio codec restrictions
            if (playlistItem.SubProtocol == MediaStreamProtocol.hls)
            {
                if (string.Equals(playlistItem.Container, "mp4", StringComparison.OrdinalIgnoreCase))
                {
                    audioCodecs = audioCodecs.Where(codec => _supportedHlsAudioCodecsMp4.Contains(codec)).ToList();
                }
                else
                {
                    audioCodecs = audioCodecs.Where(codec => _supportedHlsAudioCodecsTs.Contains(codec)).ToList();
                }
            }

            var audioStreamWithSupportedCodec = candidateAudioStreams.Where(stream => ContainerHelper.ContainsContainer(audioCodecs, false, stream.Codec)).FirstOrDefault();

            var channelsExceedsLimit = audioStreamWithSupportedCodec is not null && audioStreamWithSupportedCodec.Channels > (playlistItem.TranscodingMaxAudioChannels ?? int.MaxValue);

            var directAudioStreamSatisfied = audioStreamWithSupportedCodec is not null && !channelsExceedsLimit
                && options.Profile.CodecProfiles
                    .Where(i => i.Type == CodecType.VideoAudio
                        && i.ContainsAnyCodec(audioStreamWithSupportedCodec.Codec, container)
                        && i.ApplyConditions.All(applyCondition => ConditionProcessor.IsVideoAudioConditionSatisfied(applyCondition, audioStreamWithSupportedCodec.Channels, audioStreamWithSupportedCodec.BitRate, audioStreamWithSupportedCodec.SampleRate, audioStreamWithSupportedCodec.BitDepth, audioStreamWithSupportedCodec.Profile, false)))
                    .Select(i => i.Conditions.All(condition =>
                    {
                        var satisfied = ConditionProcessor.IsVideoAudioConditionSatisfied(condition, audioStreamWithSupportedCodec.Channels, audioStreamWithSupportedCodec.BitRate, audioStreamWithSupportedCodec.SampleRate, audioStreamWithSupportedCodec.BitDepth, audioStreamWithSupportedCodec.Profile, false);
                        if (!satisfied)
                        {
                            playlistItem.TranscodeReasons |= GetTranscodeReasonForFailedCondition(condition);
                        }

                        return satisfied;
                    }))
                    .All(satisfied => satisfied);

            directAudioStreamSatisfied = directAudioStreamSatisfied && !playlistItem.TranscodeReasons.HasFlag(TranscodeReason.ContainerBitrateExceedsLimit);

            var directAudioStream = directAudioStreamSatisfied ? audioStreamWithSupportedCodec : null;

            if (channelsExceedsLimit && playlistItem.TargetAudioStream is not null)
            {
                playlistItem.TranscodeReasons |= TranscodeReason.AudioChannelsNotSupported;
                playlistItem.TargetAudioStream.Channels = playlistItem.TranscodingMaxAudioChannels;
            }

            playlistItem.AudioCodecs = audioCodecs;
            if (directAudioStream is not null)
            {
                audioStream = directAudioStream;
                playlistItem.AudioStreamIndex = audioStream.Index;
                audioCodecs = [audioStream.Codec];
                playlistItem.AudioCodecs = audioCodecs;

                // Copy matching audio codec options
                playlistItem.AudioSampleRate = audioStream.SampleRate;
                playlistItem.SetOption(qualifier, "audiochannels", audioStream.Channels?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);

                if (!string.IsNullOrEmpty(audioStream.Profile))
                {
                    playlistItem.SetOption(audioStream.Codec, "profile", audioStream.Profile.ToLowerInvariant());
                }

                if (audioStream.Level.HasValue && audioStream.Level.Value != 0)
                {
                    playlistItem.SetOption(audioStream.Codec, "level", audioStream.Level.Value.ToString(CultureInfo.InvariantCulture));
                }
            }

            int? width = videoStream?.Width;
            int? height = videoStream?.Height;
            int? bitDepth = videoStream?.BitDepth;
            int? videoBitrate = videoStream?.BitRate;
            double? videoLevel = videoStream?.Level;
            string? videoProfile = videoStream?.Profile;
            VideoRangeType? videoRangeType = videoStream?.VideoRangeType;
            float videoFramerate = videoStream is null ? 0 : videoStream.ReferenceFrameRate ?? 0;
            bool? isAnamorphic = videoStream?.IsAnamorphic;
            bool? isInterlaced = videoStream?.IsInterlaced;
            string? videoCodecTag = videoStream?.CodecTag;
            bool? isAvc = videoStream?.IsAVC;

            TransportStreamTimestamp? timestamp = videoStream is null ? TransportStreamTimestamp.None : item.Timestamp;
            int? packetLength = videoStream?.PacketLength;
            int? refFrames = videoStream?.RefFrames;

            int? numAudioStreams = item.GetStreamCount(MediaStreamType.Audio);
            int? numVideoStreams = item.GetStreamCount(MediaStreamType.Video);

            var useSubContainer = playlistItem.SubProtocol == MediaStreamProtocol.hls;

            var appliedVideoConditions = options.Profile.CodecProfiles
                .Where(i => i.Type == CodecType.Video &&
                    i.ContainsAnyCodec(playlistItem.VideoCodecs, container, useSubContainer) &&
                    i.ApplyConditions.All(applyCondition => ConditionProcessor.IsVideoConditionSatisfied(applyCondition, width, height, bitDepth, videoBitrate, videoProfile, videoRangeType, videoLevel, videoFramerate, packetLength, timestamp, isAnamorphic, isInterlaced, refFrames, numVideoStreams, numAudioStreams, videoCodecTag, isAvc)))
                // Reverse codec profiles for backward compatibility - first codec profile has higher priority
                .Reverse();
            foreach (var condition in appliedVideoConditions)
            {
                foreach (var transcodingVideoCodec in playlistItem.VideoCodecs)
                {
                    if (condition.ContainsAnyCodec(transcodingVideoCodec, container, useSubContainer))
                    {
                        ApplyTranscodingConditions(playlistItem, condition.Conditions, transcodingVideoCodec, true, true);
                        continue;
                    }
                }
            }

            // Honor requested max channels
            playlistItem.GlobalMaxAudioChannels = channelsExceedsLimit ? playlistItem.TranscodingMaxAudioChannels : options.MaxAudioChannels;

            int audioBitrate = GetAudioBitrate(options.GetMaxBitrate(true) ?? 0, playlistItem.TargetAudioCodec, audioStream, playlistItem);
            playlistItem.AudioBitrate = Math.Min(playlistItem.AudioBitrate ?? audioBitrate, audioBitrate);

            bool? isSecondaryAudio = audioStream is null ? null : item.IsSecondaryAudio(audioStream);
            int? inputAudioBitrate = audioStream?.BitRate;
            int? audioChannels = audioStream?.Channels;
            string? audioProfile = audioStream?.Profile;
            int? inputAudioSampleRate = audioStream?.SampleRate;
            int? inputAudioBitDepth = audioStream?.BitDepth;

            var appliedAudioConditions = options.Profile.CodecProfiles
                .Where(i => i.Type == CodecType.VideoAudio &&
                    i.ContainsAnyCodec(playlistItem.AudioCodecs, container) &&
                    i.ApplyConditions.All(applyCondition => ConditionProcessor.IsVideoAudioConditionSatisfied(applyCondition, audioChannels, inputAudioBitrate, inputAudioSampleRate, inputAudioBitDepth, audioProfile, isSecondaryAudio)))
                // Reverse codec profiles for backward compatibility - first codec profile has higher priority
                .Reverse();

            foreach (var codecProfile in appliedAudioConditions)
            {
                foreach (var transcodingAudioCodec in playlistItem.AudioCodecs)
                {
                    if (codecProfile.ContainsAnyCodec(transcodingAudioCodec, container))
                    {
                        ApplyTranscodingConditions(playlistItem, codecProfile.Conditions, transcodingAudioCodec, true, true);
                        break;
                    }
                }
            }

            var maxBitrateSetting = options.GetMaxBitrate(false);
            // Honor max rate
            if (maxBitrateSetting.HasValue)
            {
                var availableBitrateForVideo = maxBitrateSetting.Value;

                if (playlistItem.AudioBitrate.HasValue)
                {
                    availableBitrateForVideo -= playlistItem.AudioBitrate.Value;
                }

                // Make sure the video bitrate is lower than bitrate settings but at least 64k
                // Don't use Math.Clamp as availableBitrateForVideo can be lower then 64k.
                var currentValue = playlistItem.VideoBitrate ?? availableBitrateForVideo;
                playlistItem.VideoBitrate = Math.Max(Math.Min(availableBitrateForVideo, currentValue), 64_000);
            }

            _logger.LogDebug(
                "Transcode Result for Profile: {Profile}, Path: {Path}, PlayMethod: {PlayMethod}, AudioStreamIndex: {AudioStreamIndex}, SubtitleStreamIndex: {SubtitleStreamIndex}, Reasons: {TranscodeReason}",
                options.Profile?.Name ?? "Anonymous Profile",
                item.Path ?? "Unknown path",
                playlistItem?.PlayMethod,
                audioStream?.Index,
                playlistItem?.SubtitleStreamIndex,
                playlistItem?.TranscodeReasons);
        }

        private static int GetDefaultAudioBitrate(string? audioCodec, int? audioChannels)
        {
            if (!string.IsNullOrEmpty(audioCodec))
            {
                // Default to a higher bitrate for stream copy
                if (string.Equals(audioCodec, "aac", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(audioCodec, "mp3", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(audioCodec, "ac3", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(audioCodec, "eac3", StringComparison.OrdinalIgnoreCase))
                {
                    if ((audioChannels ?? 0) < 2)
                    {
                        return 128000;
                    }

                    return (audioChannels ?? 0) >= 6 ? 640000 : 384000;
                }

                if (string.Equals(audioCodec, "flac", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(audioCodec, "alac", StringComparison.OrdinalIgnoreCase))
                {
                    if ((audioChannels ?? 0) < 2)
                    {
                        return 768000;
                    }

                    return (audioChannels ?? 0) >= 6 ? 3584000 : 1536000;
                }
            }

            return 192000;
        }

        private static int GetAudioBitrate(long maxTotalBitrate, IReadOnlyList<string> targetAudioCodecs, MediaStream? audioStream, StreamInfo item)
        {
            string? targetAudioCodec = targetAudioCodecs.Count == 0 ? null : targetAudioCodecs[0];

            int? targetAudioChannels = item.GetTargetAudioChannels(targetAudioCodec);

            int defaultBitrate;
            int encoderAudioBitrateLimit = int.MaxValue;

            if (audioStream is null)
            {
                defaultBitrate = 192000;
            }
            else
            {
                if (targetAudioChannels.HasValue
                    && audioStream.Channels.HasValue
                    && audioStream.Channels.Value > targetAudioChannels.Value)
                {
                    // Reduce the bitrate if we're down mixing.
                    defaultBitrate = GetDefaultAudioBitrate(targetAudioCodec, targetAudioChannels);
                }
                else if (targetAudioChannels.HasValue
                         && audioStream.Channels.HasValue
                         && audioStream.Channels.Value <= targetAudioChannels.Value
                         && !string.IsNullOrEmpty(audioStream.Codec)
                         && targetAudioCodecs is not null
                         && targetAudioCodecs.Count > 0
                         && !targetAudioCodecs.Any(elem => string.Equals(audioStream.Codec, elem, StringComparison.OrdinalIgnoreCase)))
                {
                    // Shift the bitrate if we're transcoding to a different audio codec.
                    defaultBitrate = GetDefaultAudioBitrate(targetAudioCodec, audioStream.Channels.Value);
                }
                else
                {
                    defaultBitrate = audioStream.BitRate ?? GetDefaultAudioBitrate(targetAudioCodec, targetAudioChannels);
                }

                // Seeing webm encoding failures when source has 1 audio channel and 22k bitrate.
                // Any attempts to transcode over 64k will fail
                if (audioStream.Channels == 1
                    && (audioStream.BitRate ?? 0) < 64000)
                {
                    encoderAudioBitrateLimit = 64000;
                }
            }

            if (maxTotalBitrate > 0)
            {
                defaultBitrate = Math.Min(GetMaxAudioBitrateForTotalBitrate(maxTotalBitrate), defaultBitrate);
            }

            return Math.Min(defaultBitrate, encoderAudioBitrateLimit);
        }

        private static int GetMaxAudioBitrateForTotalBitrate(long totalBitrate)
        {
            if (totalBitrate <= 640000)
            {
                return 128000;
            }

            if (totalBitrate <= 2000000)
            {
                return 384000;
            }

            if (totalBitrate <= 3000000)
            {
                return 448000;
            }

            if (totalBitrate <= 4000000)
            {
                return 640000;
            }

            if (totalBitrate <= 5000000)
            {
                return 768000;
            }

            if (totalBitrate <= 10000000)
            {
                return 1536000;
            }

            if (totalBitrate <= 15000000)
            {
                return 2304000;
            }

            if (totalBitrate <= 20000000)
            {
                return 3584000;
            }

            return 7168000;
        }

        private (DirectPlayProfile? Profile, PlayMethod? PlayMethod, int? AudioStreamIndex, TranscodeReason TranscodeReasons) GetVideoDirectPlayProfile(
            MediaOptions options,
            MediaSourceInfo mediaSource,
            MediaStream? videoStream,
            MediaStream? audioStream,
            ICollection<MediaStream> candidateAudioStreams,
            MediaStream? subtitleStream,
            bool isEligibleForDirectPlay,
            bool isEligibleForDirectStream)
        {
            if (options.ForceDirectPlay)
            {
                return (null, PlayMethod.DirectPlay, audioStream?.Index, 0);
            }

            if (options.ForceDirectStream)
            {
                return (null, PlayMethod.DirectStream, audioStream?.Index, 0);
            }

            DeviceProfile profile = options.Profile;
            string container = mediaSource.Container;

            // Video
            int? width = videoStream?.Width;
            int? height = videoStream?.Height;
            int? bitDepth = videoStream?.BitDepth;
            int? videoBitrate = videoStream?.BitRate;
            double? videoLevel = videoStream?.Level;
            string? videoProfile = videoStream?.Profile;
            VideoRangeType? videoRangeType = videoStream?.VideoRangeType;
            float videoFramerate = videoStream is null ? 0 : videoStream.ReferenceFrameRate ?? 0;
            bool? isAnamorphic = videoStream?.IsAnamorphic;
            bool? isInterlaced = videoStream?.IsInterlaced;
            string? videoCodecTag = videoStream?.CodecTag;
            bool? isAvc = videoStream?.IsAVC;

            TransportStreamTimestamp? timestamp = videoStream is null ? TransportStreamTimestamp.None : mediaSource.Timestamp;
            int? packetLength = videoStream?.PacketLength;
            int? refFrames = videoStream?.RefFrames;

            int? numAudioStreams = mediaSource.GetStreamCount(MediaStreamType.Audio);
            int? numVideoStreams = mediaSource.GetStreamCount(MediaStreamType.Video);

            var checkVideoConditions = (ProfileCondition[] conditions) =>
                conditions.Where(applyCondition => !ConditionProcessor.IsVideoConditionSatisfied(applyCondition, width, height, bitDepth, videoBitrate, videoProfile, videoRangeType, videoLevel, videoFramerate, packetLength, timestamp, isAnamorphic, isInterlaced, refFrames, numVideoStreams, numAudioStreams, videoCodecTag, isAvc));

            // Check container conditions
            var containerProfileReasons = AggregateFailureConditions(
                mediaSource,
                profile,
                "VideoCodecProfile",
                profile.ContainerProfiles
                    .Where(containerProfile => containerProfile.Type == DlnaProfileType.Video && containerProfile.ContainsContainer(container))
                    .SelectMany(containerProfile => checkVideoConditions(containerProfile.Conditions)));

            // Check video conditions
            var videoCodecProfileReasons = AggregateFailureConditions(
                mediaSource,
                profile,
                "VideoCodecProfile",
                profile.CodecProfiles
                    .Where(codecProfile => codecProfile.Type == CodecType.Video &&
                        codecProfile.ContainsAnyCodec(videoStream?.Codec, container) &&
                        !checkVideoConditions(codecProfile.ApplyConditions).Any())
                    .SelectMany(codecProfile => checkVideoConditions(codecProfile.Conditions)));

            // Check audio candidates profile conditions
            var audioStreamMatches = candidateAudioStreams.ToDictionary(s => s, audioStream => CheckVideoAudioStreamDirectPlay(options, mediaSource, container, audioStream));

            TranscodeReason subtitleProfileReasons = 0;
            if (subtitleStream is not null)
            {
                var subtitleProfile = GetSubtitleProfile(mediaSource, subtitleStream, options.Profile.SubtitleProfiles, PlayMethod.DirectPlay, _transcoderSupport, container, null);

                if (subtitleProfile.Method != SubtitleDeliveryMethod.Drop
                    && subtitleProfile.Method != SubtitleDeliveryMethod.External
                    && subtitleProfile.Method != SubtitleDeliveryMethod.Embed)
                {
                    _logger.LogDebug("Not eligible for {0} due to unsupported subtitles", PlayMethod.DirectPlay);
                    subtitleProfileReasons |= TranscodeReason.SubtitleCodecNotSupported;
                }
            }

            var containerSupported = false;
            TranscodeReason[] rankings = [TranscodeReason.VideoCodecNotSupported, VideoCodecReasons, TranscodeReason.AudioCodecNotSupported, AudioCodecReasons, ContainerReasons];

            // Check DirectPlay profiles to see if it can be direct played
            var analyzedProfiles = profile.DirectPlayProfiles
                .Where(directPlayProfile => directPlayProfile.Type == DlnaProfileType.Video)
                .Select((directPlayProfile, order) =>
                {
                    TranscodeReason directPlayProfileReasons = 0;
                    TranscodeReason audioCodecProfileReasons = 0;

                    // Check container type
                    if (!directPlayProfile.SupportsContainer(container))
                    {
                        directPlayProfileReasons |= TranscodeReason.ContainerNotSupported;
                    }
                    else
                    {
                        containerSupported = true;
                    }

                    // Check video codec
                    string? videoCodec = videoStream?.Codec;
                    if (!directPlayProfile.SupportsVideoCodec(videoCodec))
                    {
                        directPlayProfileReasons |= TranscodeReason.VideoCodecNotSupported;
                    }

                    // Check audio codec
                    MediaStream? selectedAudioStream = null;
                    if (candidateAudioStreams.Count != 0)
                    {
                        selectedAudioStream = candidateAudioStreams.FirstOrDefault(audioStream => directPlayProfile.SupportsAudioCodec(audioStream.Codec));
                        if (selectedAudioStream is null)
                        {
                            directPlayProfileReasons |= TranscodeReason.AudioCodecNotSupported;
                        }
                        else
                        {
                            audioCodecProfileReasons = audioStreamMatches.GetValueOrDefault(selectedAudioStream);
                        }
                    }

                    var failureReasons = directPlayProfileReasons | containerProfileReasons | subtitleProfileReasons;

                    if ((failureReasons & TranscodeReason.VideoCodecNotSupported) == 0)
                    {
                        failureReasons |= videoCodecProfileReasons;
                    }

                    if ((failureReasons & TranscodeReason.AudioCodecNotSupported) == 0)
                    {
                        failureReasons |= audioCodecProfileReasons;
                    }

                    var directStreamFailureReasons = failureReasons & (~DirectStreamReasons);

                    PlayMethod? playMethod = null;
                    if (failureReasons == 0 && isEligibleForDirectPlay && mediaSource.SupportsDirectPlay)
                    {
                        playMethod = PlayMethod.DirectPlay;
                    }
                    else if (directStreamFailureReasons == 0 && isEligibleForDirectStream && mediaSource.SupportsDirectStream)
                    {
                        playMethod = PlayMethod.DirectStream;
                    }

                    var ranked = GetRank(ref failureReasons, rankings);

                    return (Result: (Profile: directPlayProfile, PlayMethod: playMethod, AudioStreamIndex: selectedAudioStream?.Index, TranscodeReason: failureReasons), Order: order, Rank: ranked);
                })
                .OrderByDescending(analysis => analysis.Result.PlayMethod)
                .ThenByDescending(analysis => analysis.Rank)
                .ThenBy(analysis => analysis.Order)
                .ToArray()
                .ToLookup(analysis => analysis.Result.PlayMethod is not null);

            var profileMatch = analyzedProfiles[true]
                .Select(analysis => analysis.Result)
                .FirstOrDefault();
            if (profileMatch.Profile is not null)
            {
                return profileMatch;
            }

            var failureReasons = analyzedProfiles[false]
                .Select(analysis => analysis.Result)
                .Where(result => !containerSupported || !result.TranscodeReason.HasFlag(TranscodeReason.ContainerNotSupported))
                .FirstOrDefault().TranscodeReason;
            if (failureReasons == 0)
            {
                failureReasons = TranscodeReason.DirectPlayError;
            }

            return (Profile: null, PlayMethod: null, AudioStreamIndex: null, TranscodeReasons: failureReasons);
        }

        private TranscodeReason CheckVideoAudioStreamDirectPlay(MediaOptions options, MediaSourceInfo mediaSource, string container, MediaStream audioStream)
        {
            var profile = options.Profile;
            var audioFailureConditions = GetProfileConditionsForVideoAudio(profile.CodecProfiles, container, audioStream.Codec, audioStream.Channels, audioStream.BitRate, audioStream.SampleRate, audioStream.BitDepth, audioStream.Profile, mediaSource.IsSecondaryAudio(audioStream));

            var audioStreamFailureReasons = AggregateFailureConditions(mediaSource, profile, "VideoAudioCodecProfile", audioFailureConditions);
            if (audioStream.IsExternal == true)
            {
                audioStreamFailureReasons |= TranscodeReason.AudioIsExternal;
            }

            return audioStreamFailureReasons;
        }

        private TranscodeReason AggregateFailureConditions(MediaSourceInfo mediaSource, DeviceProfile profile, string type, IEnumerable<ProfileCondition> conditions)
        {
            return conditions.Aggregate<ProfileCondition, TranscodeReason>(0, (reasons, i) =>
            {
                LogConditionFailure(profile, type, i, mediaSource);
                var transcodeReasons = GetTranscodeReasonForFailedCondition(i);
                return reasons | transcodeReasons;
            });
        }

        private void LogConditionFailure(DeviceProfile profile, string type, ProfileCondition condition, MediaSourceInfo mediaSource)
        {
            _logger.LogDebug(
                "Profile: {0}, DirectPlay=false. Reason={1}.{2} Condition: {3}. ConditionValue: {4}. IsRequired: {5}. Path: {6}",
                type,
                profile.Name ?? "Unknown Profile",
                condition.Property,
                condition.Condition,
                condition.Value ?? string.Empty,
                condition.IsRequired,
                mediaSource.Path ?? "Unknown path");
        }

        /// <summary>
        /// Normalizes input container.
        /// </summary>
        /// <param name="mediaSource">The <see cref="MediaSourceInfo"/>.</param>
        /// <param name="subtitleStream">The <see cref="MediaStream"/> of the subtitle stream.</param>
        /// <param name="subtitleProfiles">The list of supported <see cref="SubtitleProfile"/>s.</param>
        /// <param name="playMethod">The <see cref="PlayMethod"/>.</param>
        /// <param name="transcoderSupport">The <see cref="ITranscoderSupport"/>.</param>
        /// <param name="outputContainer">The output container.</param>
        /// <param name="transcodingSubProtocol">The subtitle transcoding protocol.</param>
        /// <returns>The normalized input container.</returns>
        public static SubtitleProfile GetSubtitleProfile(
            MediaSourceInfo mediaSource,
            MediaStream subtitleStream,
            SubtitleProfile[] subtitleProfiles,
            PlayMethod playMethod,
            ITranscoderSupport transcoderSupport,
            string? outputContainer,
            MediaStreamProtocol? transcodingSubProtocol)
        {
            if (!subtitleStream.IsExternal && (playMethod != PlayMethod.Transcode || transcodingSubProtocol != MediaStreamProtocol.hls))
            {
                // Look for supported embedded subs of the same format
                foreach (var profile in subtitleProfiles)
                {
                    if (!profile.SupportsLanguage(subtitleStream.Language))
                    {
                        continue;
                    }

                    if (profile.Method != SubtitleDeliveryMethod.Embed)
                    {
                        continue;
                    }

                    if (!ContainerHelper.ContainsContainer(profile.Container, outputContainer))
                    {
                        continue;
                    }

                    if (playMethod == PlayMethod.Transcode && !IsSubtitleEmbedSupported(outputContainer))
                    {
                        continue;
                    }

                    if (subtitleStream.IsTextSubtitleStream == MediaStream.IsTextFormat(profile.Format) && string.Equals(profile.Format, subtitleStream.Codec, StringComparison.OrdinalIgnoreCase))
                    {
                        return profile;
                    }
                }

                // Look for supported embedded subs of a convertible format
                foreach (var profile in subtitleProfiles)
                {
                    if (!profile.SupportsLanguage(subtitleStream.Language))
                    {
                        continue;
                    }

                    if (profile.Method != SubtitleDeliveryMethod.Embed)
                    {
                        continue;
                    }

                    if (!ContainerHelper.ContainsContainer(profile.Container, outputContainer))
                    {
                        continue;
                    }

                    if (playMethod == PlayMethod.Transcode && !IsSubtitleEmbedSupported(outputContainer))
                    {
                        continue;
                    }

                    if (subtitleStream.IsTextSubtitleStream && subtitleStream.SupportsSubtitleConversionTo(profile.Format))
                    {
                        return profile;
                    }
                }
            }

            // Look for an external or hls profile that matches the stream type (text/graphical) and doesn't require conversion
            return GetExternalSubtitleProfile(mediaSource, subtitleStream, subtitleProfiles, playMethod, transcoderSupport, false) ??
                GetExternalSubtitleProfile(mediaSource, subtitleStream, subtitleProfiles, playMethod, transcoderSupport, true) ??
                new SubtitleProfile
                {
                    Method = SubtitleDeliveryMethod.Encode,
                    Format = subtitleStream.Codec
                };
        }

        private static bool IsSubtitleEmbedSupported(string? transcodingContainer)
        {
            if (!string.IsNullOrEmpty(transcodingContainer))
            {
                if (ContainerHelper.ContainsContainer(transcodingContainer, "ts,mpegts,mp4"))
                {
                    return false;
                }

                if (ContainerHelper.ContainsContainer(transcodingContainer, "mkv,matroska"))
                {
                    return true;
                }
            }

            return false;
        }

        private static SubtitleProfile? GetExternalSubtitleProfile(MediaSourceInfo mediaSource, MediaStream subtitleStream, SubtitleProfile[] subtitleProfiles, PlayMethod playMethod, ITranscoderSupport transcoderSupport, bool allowConversion)
        {
            foreach (var profile in subtitleProfiles)
            {
                if (profile.Method != SubtitleDeliveryMethod.External && profile.Method != SubtitleDeliveryMethod.Hls)
                {
                    continue;
                }

                if (profile.Method == SubtitleDeliveryMethod.Hls && playMethod != PlayMethod.Transcode)
                {
                    continue;
                }

                if (!profile.SupportsLanguage(subtitleStream.Language))
                {
                    continue;
                }

                if (!subtitleStream.IsExternal && !transcoderSupport.CanExtractSubtitles(subtitleStream.Codec))
                {
                    continue;
                }

                if ((profile.Method == SubtitleDeliveryMethod.External && subtitleStream.IsTextSubtitleStream == MediaStream.IsTextFormat(profile.Format)) ||
                    (profile.Method == SubtitleDeliveryMethod.Hls && subtitleStream.IsTextSubtitleStream))
                {
                    bool requiresConversion = !string.Equals(subtitleStream.Codec, profile.Format, StringComparison.OrdinalIgnoreCase);

                    if (!requiresConversion)
                    {
                        return profile;
                    }

                    if (!allowConversion)
                    {
                        continue;
                    }

                    // TODO: Build this into subtitleStream.SupportsExternalStream
                    if (mediaSource.IsInfiniteStream)
                    {
                        continue;
                    }

                    if (subtitleStream.IsTextSubtitleStream && subtitleStream.SupportsExternalStream && subtitleStream.SupportsSubtitleConversionTo(profile.Format))
                    {
                        return profile;
                    }
                }
            }

            return null;
        }

        private bool IsBitrateLimitExceeded(MediaSourceInfo item, long maxBitrate)
        {
            // Don't restrict bitrate if item is remote.
            if (item.IsRemote)
            {
                return false;
            }

            // If no maximum bitrate is set, default to no maximum bitrate.
            long requestedMaxBitrate = maxBitrate > 0 ? maxBitrate : int.MaxValue;

            // If we don't know the item bitrate, then force a transcode if requested max bitrate is under 40 mbps
            int itemBitrate = item.Bitrate ?? 40000000;

            if (itemBitrate > requestedMaxBitrate)
            {
                _logger.LogDebug(
                    "Bitrate exceeds limit: media bitrate: {MediaBitrate}, max bitrate: {MaxBitrate}",
                    itemBitrate,
                    requestedMaxBitrate);
                return true;
            }

            return false;
        }

        private static void ValidateMediaOptions(MediaOptions options, bool isMediaSource)
        {
            if (options.ItemId.IsEmpty())
            {
                ArgumentException.ThrowIfNullOrEmpty(options.DeviceId);
            }

            if (options.Profile is null)
            {
                throw new ArgumentException("Profile is required");
            }

            if (options.MediaSources is null)
            {
                throw new ArgumentException("MediaSources is required");
            }

            if (isMediaSource)
            {
                if (options.AudioStreamIndex.HasValue && string.IsNullOrEmpty(options.MediaSourceId))
                {
                    throw new ArgumentException("MediaSourceId is required when a specific audio stream is requested");
                }

                if (options.SubtitleStreamIndex.HasValue && string.IsNullOrEmpty(options.MediaSourceId))
                {
                    throw new ArgumentException("MediaSourceId is required when a specific subtitle stream is requested");
                }
            }
        }

        private static IEnumerable<ProfileCondition> GetProfileConditionsForVideoAudio(
            IEnumerable<CodecProfile> codecProfiles,
            string container,
            string codec,
            int? audioChannels,
            int? audioBitrate,
            int? audioSampleRate,
            int? audioBitDepth,
            string audioProfile,
            bool? isSecondaryAudio)
        {
            return codecProfiles
                .Where(profile => profile.Type == CodecType.VideoAudio &&
                    profile.ContainsAnyCodec(codec, container) &&
                    profile.ApplyConditions.All(applyCondition => ConditionProcessor.IsVideoAudioConditionSatisfied(applyCondition, audioChannels, audioBitrate, audioSampleRate, audioBitDepth, audioProfile, isSecondaryAudio)))
                .SelectMany(profile => profile.Conditions)
                .Where(condition => !ConditionProcessor.IsVideoAudioConditionSatisfied(condition, audioChannels, audioBitrate, audioSampleRate, audioBitDepth, audioProfile, isSecondaryAudio));
        }

        private static IEnumerable<ProfileCondition> GetProfileConditionsForAudio(
            IEnumerable<CodecProfile> codecProfiles,
            string container,
            string? codec,
            int? audioChannels,
            int? audioBitrate,
            int? audioSampleRate,
            int? audioBitDepth,
            bool checkConditions)
        {
            var conditions = codecProfiles
                .Where(profile => profile.Type == CodecType.Audio &&
                    profile.ContainsAnyCodec(codec, container) &&
                    profile.ApplyConditions.All(applyCondition => ConditionProcessor.IsAudioConditionSatisfied(applyCondition, audioChannels, audioBitrate, audioSampleRate, audioBitDepth)))
                .SelectMany(profile => profile.Conditions);

            if (!checkConditions)
            {
                return conditions;
            }

            return conditions.Where(condition => !ConditionProcessor.IsAudioConditionSatisfied(condition, audioChannels, audioBitrate, audioSampleRate, audioBitDepth));
        }

        private void ApplyTranscodingConditions(StreamInfo item, IEnumerable<ProfileCondition> conditions, string? qualifier, bool enableQualifiedConditions, bool enableNonQualifiedConditions)
        {
            foreach (ProfileCondition condition in conditions)
            {
                string value = condition.Value;

                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }

                // No way to express this
                if (condition.Condition == ProfileConditionType.GreaterThanEqual)
                {
                    continue;
                }

                switch (condition.Property)
                {
                    case ProfileConditionValue.AudioBitrate:
                        {
                            if (!enableNonQualifiedConditions)
                            {
                                continue;
                            }

                            if (int.TryParse(value, CultureInfo.InvariantCulture, out var num))
                            {
                                if (condition.Condition == ProfileConditionType.Equals)
                                {
                                    item.AudioBitrate = num;
                                }
                                else if (condition.Condition == ProfileConditionType.LessThanEqual)
                                {
                                    item.AudioBitrate = Math.Min(num, item.AudioBitrate ?? num);
                                }
                                else if (condition.Condition == ProfileConditionType.GreaterThanEqual)
                                {
                                    item.AudioBitrate = Math.Max(num, item.AudioBitrate ?? num);
                                }
                            }

                            break;
                        }

                    case ProfileConditionValue.AudioSampleRate:
                        {
                            if (!enableNonQualifiedConditions)
                            {
                                continue;
                            }

                            if (int.TryParse(value, CultureInfo.InvariantCulture, out var num))
                            {
                                if (condition.Condition == ProfileConditionType.Equals)
                                {
                                    item.AudioSampleRate = num;
                                }
                                else if (condition.Condition == ProfileConditionType.LessThanEqual)
                                {
                                    item.AudioSampleRate = Math.Min(num, item.AudioSampleRate ?? num);
                                }
                                else if (condition.Condition == ProfileConditionType.GreaterThanEqual)
                                {
                                    item.AudioSampleRate = Math.Max(num, item.AudioSampleRate ?? num);
                                }
                            }

                            break;
                        }

                    case ProfileConditionValue.AudioChannels:
                        {
                            if (string.IsNullOrEmpty(qualifier))
                            {
                                if (!enableNonQualifiedConditions)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                if (!enableQualifiedConditions)
                                {
                                    continue;
                                }
                            }

                            if (int.TryParse(value, CultureInfo.InvariantCulture, out var num))
                            {
                                if (condition.Condition == ProfileConditionType.Equals)
                                {
                                    item.SetOption(qualifier, "audiochannels", num.ToString(CultureInfo.InvariantCulture));
                                }
                                else if (condition.Condition == ProfileConditionType.LessThanEqual)
                                {
                                    item.SetOption(qualifier, "audiochannels", Math.Min(num, item.GetTargetAudioChannels(qualifier) ?? num).ToString(CultureInfo.InvariantCulture));
                                }
                                else if (condition.Condition == ProfileConditionType.GreaterThanEqual)
                                {
                                    item.SetOption(qualifier, "audiochannels", Math.Max(num, item.GetTargetAudioChannels(qualifier) ?? num).ToString(CultureInfo.InvariantCulture));
                                }
                            }

                            break;
                        }

                    case ProfileConditionValue.IsAvc:
                        {
                            if (!enableNonQualifiedConditions)
                            {
                                continue;
                            }

                            if (bool.TryParse(value, out var isAvc))
                            {
                                if (isAvc && condition.Condition == ProfileConditionType.Equals)
                                {
                                    item.RequireAvc = true;
                                }
                                else if (!isAvc && condition.Condition == ProfileConditionType.NotEquals)
                                {
                                    item.RequireAvc = true;
                                }
                            }

                            break;
                        }

                    case ProfileConditionValue.IsAnamorphic:
                        {
                            if (!enableNonQualifiedConditions)
                            {
                                continue;
                            }

                            if (bool.TryParse(value, out var isAnamorphic))
                            {
                                if (isAnamorphic && condition.Condition == ProfileConditionType.Equals)
                                {
                                    item.RequireNonAnamorphic = true;
                                }
                                else if (!isAnamorphic && condition.Condition == ProfileConditionType.NotEquals)
                                {
                                    item.RequireNonAnamorphic = true;
                                }
                            }

                            break;
                        }

                    case ProfileConditionValue.IsInterlaced:
                        {
                            if (string.IsNullOrEmpty(qualifier))
                            {
                                if (!enableNonQualifiedConditions)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                if (!enableQualifiedConditions)
                                {
                                    continue;
                                }
                            }

                            if (bool.TryParse(value, out var isInterlaced))
                            {
                                if (!isInterlaced && condition.Condition == ProfileConditionType.Equals)
                                {
                                    item.SetOption(qualifier, "deinterlace", "true");
                                }
                                else if (isInterlaced && condition.Condition == ProfileConditionType.NotEquals)
                                {
                                    item.SetOption(qualifier, "deinterlace", "true");
                                }
                            }

                            break;
                        }

                    case ProfileConditionValue.AudioProfile:
                    case ProfileConditionValue.Has64BitOffsets:
                    case ProfileConditionValue.PacketLength:
                    case ProfileConditionValue.NumAudioStreams:
                    case ProfileConditionValue.NumVideoStreams:
                    case ProfileConditionValue.IsSecondaryAudio:
                    case ProfileConditionValue.VideoTimestamp:
                        {
                            // Not supported yet
                            break;
                        }

                    case ProfileConditionValue.RefFrames:
                        {
                            if (string.IsNullOrEmpty(qualifier))
                            {
                                if (!enableNonQualifiedConditions)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                if (!enableQualifiedConditions)
                                {
                                    continue;
                                }
                            }

                            if (int.TryParse(value, CultureInfo.InvariantCulture, out var num))
                            {
                                if (condition.Condition == ProfileConditionType.Equals)
                                {
                                    item.SetOption(qualifier, "maxrefframes", num.ToString(CultureInfo.InvariantCulture));
                                }
                                else if (condition.Condition == ProfileConditionType.LessThanEqual)
                                {
                                    item.SetOption(qualifier, "maxrefframes", Math.Min(num, item.GetTargetRefFrames(qualifier) ?? num).ToString(CultureInfo.InvariantCulture));
                                }
                                else if (condition.Condition == ProfileConditionType.GreaterThanEqual)
                                {
                                    item.SetOption(qualifier, "maxrefframes", Math.Max(num, item.GetTargetRefFrames(qualifier) ?? num).ToString(CultureInfo.InvariantCulture));
                                }
                            }

                            break;
                        }

                    case ProfileConditionValue.VideoBitDepth:
                        {
                            if (string.IsNullOrEmpty(qualifier))
                            {
                                if (!enableNonQualifiedConditions)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                if (!enableQualifiedConditions)
                                {
                                    continue;
                                }
                            }

                            if (int.TryParse(value, CultureInfo.InvariantCulture, out var num))
                            {
                                if (condition.Condition == ProfileConditionType.Equals)
                                {
                                    item.SetOption(qualifier, "videobitdepth", num.ToString(CultureInfo.InvariantCulture));
                                }
                                else if (condition.Condition == ProfileConditionType.LessThanEqual)
                                {
                                    item.SetOption(qualifier, "videobitdepth", Math.Min(num, item.GetTargetVideoBitDepth(qualifier) ?? num).ToString(CultureInfo.InvariantCulture));
                                }
                                else if (condition.Condition == ProfileConditionType.GreaterThanEqual)
                                {
                                    item.SetOption(qualifier, "videobitdepth", Math.Max(num, item.GetTargetVideoBitDepth(qualifier) ?? num).ToString(CultureInfo.InvariantCulture));
                                }
                            }

                            break;
                        }

                    case ProfileConditionValue.VideoProfile:
                        {
                            if (string.IsNullOrEmpty(qualifier))
                            {
                                continue;
                            }

                            // Change from split by | to comma
                            // Strip spaces to avoid having to encode
                            var values = value
                                .Split('|', StringSplitOptions.RemoveEmptyEntries);

                            if (condition.Condition == ProfileConditionType.Equals)
                            {
                                item.SetOption(qualifier, "profile", string.Join(',', values));
                            }
                            else if (condition.Condition == ProfileConditionType.EqualsAny)
                            {
                                var currentValue = item.GetOption(qualifier, "profile");
                                if (!string.IsNullOrEmpty(currentValue) && values.Any(value => value == currentValue))
                                {
                                    item.SetOption(qualifier, "profile", currentValue);
                                }
                                else
                                {
                                    item.SetOption(qualifier, "profile", string.Join(',', values));
                                }
                            }

                            break;
                        }

                    case ProfileConditionValue.VideoRangeType:
                        {
                            if (string.IsNullOrEmpty(qualifier))
                            {
                                continue;
                            }

                            // change from split by | to comma
                            // strip spaces to avoid having to encode
                            var values = value
                                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                            if (condition.Condition == ProfileConditionType.Equals)
                            {
                                item.SetOption(qualifier, "rangetype", string.Join(',', values));
                            }
                            else if (condition.Condition == ProfileConditionType.NotEquals)
                            {
                                item.SetOption(qualifier, "rangetype", string.Join(',', Enum.GetNames(typeof(VideoRangeType)).Except(values)));
                            }
                            else if (condition.Condition == ProfileConditionType.EqualsAny)
                            {
                                var currentValue = item.GetOption(qualifier, "rangetype");
                                if (!string.IsNullOrEmpty(currentValue) && values.Any(v => string.Equals(v, currentValue, StringComparison.OrdinalIgnoreCase)))
                                {
                                    item.SetOption(qualifier, "rangetype", currentValue);
                                }
                                else
                                {
                                    item.SetOption(qualifier, "rangetype", string.Join(',', values));
                                }
                            }

                            break;
                        }

                    case ProfileConditionValue.VideoCodecTag:
                        {
                            if (string.IsNullOrEmpty(qualifier))
                            {
                                continue;
                            }

                            // change from split by | to comma
                            // strip spaces to avoid having to encode
                            var values = value
                                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                            if (condition.Condition == ProfileConditionType.Equals)
                            {
                                item.SetOption(qualifier, "codectag", string.Join(',', values));
                            }
                            else if (condition.Condition == ProfileConditionType.EqualsAny)
                            {
                                var currentValue = item.GetOption(qualifier, "codectag");
                                if (!string.IsNullOrEmpty(currentValue) && values.Any(v => string.Equals(v, currentValue, StringComparison.OrdinalIgnoreCase)))
                                {
                                    item.SetOption(qualifier, "codectag", currentValue);
                                }
                                else
                                {
                                    item.SetOption(qualifier, "codectag", string.Join(',', values));
                                }
                            }

                            break;
                        }

                    case ProfileConditionValue.Height:
                        {
                            if (!enableNonQualifiedConditions)
                            {
                                continue;
                            }

                            if (int.TryParse(value, CultureInfo.InvariantCulture, out var num))
                            {
                                if (condition.Condition == ProfileConditionType.Equals)
                                {
                                    item.MaxHeight = num;
                                }
                                else if (condition.Condition == ProfileConditionType.LessThanEqual)
                                {
                                    item.MaxHeight = Math.Min(num, item.MaxHeight ?? num);
                                }
                                else if (condition.Condition == ProfileConditionType.GreaterThanEqual)
                                {
                                    item.MaxHeight = Math.Max(num, item.MaxHeight ?? num);
                                }
                            }

                            break;
                        }

                    case ProfileConditionValue.VideoBitrate:
                        {
                            if (!enableNonQualifiedConditions)
                            {
                                continue;
                            }

                            if (int.TryParse(value, CultureInfo.InvariantCulture, out var num))
                            {
                                if (condition.Condition == ProfileConditionType.Equals)
                                {
                                    item.VideoBitrate = num;
                                }
                                else if (condition.Condition == ProfileConditionType.LessThanEqual)
                                {
                                    item.VideoBitrate = Math.Min(num, item.VideoBitrate ?? num);
                                }
                                else if (condition.Condition == ProfileConditionType.GreaterThanEqual)
                                {
                                    item.VideoBitrate = Math.Max(num, item.VideoBitrate ?? num);
                                }
                            }

                            break;
                        }

                    case ProfileConditionValue.VideoFramerate:
                        {
                            if (!enableNonQualifiedConditions)
                            {
                                continue;
                            }

                            if (float.TryParse(value, CultureInfo.InvariantCulture, out var num))
                            {
                                if (condition.Condition == ProfileConditionType.Equals)
                                {
                                    item.MaxFramerate = num;
                                }
                                else if (condition.Condition == ProfileConditionType.LessThanEqual)
                                {
                                    item.MaxFramerate = Math.Min(num, item.MaxFramerate ?? num);
                                }
                                else if (condition.Condition == ProfileConditionType.GreaterThanEqual)
                                {
                                    item.MaxFramerate = Math.Max(num, item.MaxFramerate ?? num);
                                }
                            }

                            break;
                        }

                    case ProfileConditionValue.VideoLevel:
                        {
                            if (string.IsNullOrEmpty(qualifier))
                            {
                                continue;
                            }

                            if (int.TryParse(value, CultureInfo.InvariantCulture, out var num))
                            {
                                if (condition.Condition == ProfileConditionType.Equals)
                                {
                                    item.SetOption(qualifier, "level", num.ToString(CultureInfo.InvariantCulture));
                                }
                                else if (condition.Condition == ProfileConditionType.LessThanEqual)
                                {
                                    item.SetOption(qualifier, "level", Math.Min(num, item.GetTargetVideoLevel(qualifier) ?? num).ToString(CultureInfo.InvariantCulture));
                                }
                                else if (condition.Condition == ProfileConditionType.GreaterThanEqual)
                                {
                                    item.SetOption(qualifier, "level", Math.Max(num, item.GetTargetVideoLevel(qualifier) ?? num).ToString(CultureInfo.InvariantCulture));
                                }
                            }

                            break;
                        }

                    case ProfileConditionValue.Width:
                        {
                            if (!enableNonQualifiedConditions)
                            {
                                continue;
                            }

                            if (int.TryParse(value, CultureInfo.InvariantCulture, out var num))
                            {
                                if (condition.Condition == ProfileConditionType.Equals)
                                {
                                    item.MaxWidth = num;
                                }
                                else if (condition.Condition == ProfileConditionType.LessThanEqual)
                                {
                                    item.MaxWidth = Math.Min(num, item.MaxWidth ?? num);
                                }
                                else if (condition.Condition == ProfileConditionType.GreaterThanEqual)
                                {
                                    item.MaxWidth = Math.Max(num, item.MaxWidth ?? num);
                                }
                            }

                            break;
                        }

                    default:
                        break;
                }
            }
        }

        private static bool IsAudioContainerSupported(DirectPlayProfile profile, MediaSourceInfo item)
        {
            // Check container type
            if (!profile.SupportsContainer(item.Container))
            {
                return false;
            }

            // Never direct play audio in matroska when the device only declare support for webm.
            // The first check is not enough because mkv is assumed can be webm.
            // See https://github.com/jellyfin/jellyfin/issues/13344
            return !ContainerHelper.ContainsContainer("mkv", item.Container)
                   || profile.SupportsContainer("mkv");
        }

        private static bool IsAudioDirectPlaySupported(DirectPlayProfile profile, MediaSourceInfo item, MediaStream audioStream)
        {
            if (!IsAudioContainerSupported(profile, item))
            {
                return false;
            }

            // Check audio codec
            string? audioCodec = audioStream?.Codec;
            if (!profile.SupportsAudioCodec(audioCodec))
            {
                return false;
            }

            return true;
        }

        private static bool IsAudioDirectStreamSupported(DirectPlayProfile profile, MediaSourceInfo item, MediaStream audioStream)
        {
            // Check container type, this should NOT be supported
            // If the container is supported, the file should be directly played
            if (IsAudioContainerSupported(profile, item))
            {
                return false;
            }

            // Check audio codec, we cannot use the SupportsAudioCodec here
            // Because that one assumes empty container supports all codec, which is just useless
            string? audioCodec = audioStream?.Codec;
            return string.Equals(profile.AudioCodec, audioCodec, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(profile.Container, audioCodec, StringComparison.OrdinalIgnoreCase);
        }

        private int GetRank(ref TranscodeReason a, TranscodeReason[] rankings)
        {
            var index = 1;
            foreach (var flag in rankings)
            {
                var reason = a & flag;
                if (reason != 0)
                {
                    return index;
                }

                index++;
            }

            return index;
        }
    }
}
