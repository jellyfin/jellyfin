#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Model.Dlna
{
    public class StreamBuilder
    {
        private readonly ILogger _logger;
        private readonly ITranscoderSupport _transcoderSupport;

        public StreamBuilder(ITranscoderSupport transcoderSupport, ILogger logger)
        {
            _transcoderSupport = transcoderSupport;
            _logger = logger;
        }

        public StreamBuilder(ILogger<StreamBuilder> logger)
            : this(new FullTranscoderSupport(), logger)
        {
        }

        public StreamInfo BuildAudioItem(AudioOptions options)
        {
            ValidateAudioInput(options);

            var mediaSources = new List<MediaSourceInfo>();
            foreach (MediaSourceInfo i in options.MediaSources)
            {
                if (string.IsNullOrEmpty(options.MediaSourceId) ||
                    string.Equals(i.Id, options.MediaSourceId, StringComparison.OrdinalIgnoreCase))
                {
                    mediaSources.Add(i);
                }
            }

            var streams = new List<StreamInfo>();
            foreach (MediaSourceInfo i in mediaSources)
            {
                StreamInfo streamInfo = BuildAudioItem(i, options);
                if (streamInfo != null)
                {
                    streams.Add(streamInfo);
                }
            }

            foreach (StreamInfo stream in streams)
            {
                stream.DeviceId = options.DeviceId;
                stream.DeviceProfileId = options.Profile.Id;
            }

            return GetOptimalStream(streams, options.GetMaxBitrate(true) ?? 0);
        }

        public StreamInfo BuildVideoItem(VideoOptions options)
        {
            ValidateInput(options);

            var mediaSources = new List<MediaSourceInfo>();
            foreach (MediaSourceInfo i in options.MediaSources)
            {
                if (string.IsNullOrEmpty(options.MediaSourceId) ||
                    string.Equals(i.Id, options.MediaSourceId, StringComparison.OrdinalIgnoreCase))
                {
                    mediaSources.Add(i);
                }
            }

            var streams = new List<StreamInfo>();
            foreach (MediaSourceInfo i in mediaSources)
            {
                var streamInfo = BuildVideoItem(i, options);
                if (streamInfo != null)
                {
                    streams.Add(streamInfo);
                }
            }

            foreach (StreamInfo stream in streams)
            {
                stream.DeviceId = options.DeviceId;
                stream.DeviceProfileId = options.Profile.Id;
            }

            return GetOptimalStream(streams, options.GetMaxBitrate(false) ?? 0);
        }

        private static StreamInfo GetOptimalStream(List<StreamInfo> streams, int maxBitrate)
            => SortMediaSources(streams, maxBitrate).FirstOrDefault();

        private static IOrderedEnumerable<StreamInfo> SortMediaSources(List<StreamInfo> streams, int maxBitrate)
        {
            return streams.OrderBy(i =>
            {
                // Nothing beats direct playing a file
                if (i.PlayMethod == PlayMethod.DirectPlay && i.MediaSource.Protocol == MediaProtocol.File)
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
                switch (i.MediaSource.Protocol)
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
                    if (i.MediaSource.Bitrate.HasValue)
                    {
                        return Math.Abs(i.MediaSource.Bitrate.Value - maxBitrate);
                    }
                }

                return 0;
            }).ThenBy(streams.IndexOf);
        }

        private static TranscodeReason? GetTranscodeReasonForFailedCondition(ProfileCondition condition)
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
                    return null;

                case ProfileConditionValue.Height:
                    return TranscodeReason.VideoResolutionNotSupported;

                case ProfileConditionValue.IsAnamorphic:
                    return TranscodeReason.AnamorphicVideoNotSupported;

                case ProfileConditionValue.IsAvc:
                    // TODO
                    return null;

                case ProfileConditionValue.IsInterlaced:
                    return TranscodeReason.InterlacedVideoNotSupported;

                case ProfileConditionValue.IsSecondaryAudio:
                    return TranscodeReason.SecondaryAudioNotSupported;

                case ProfileConditionValue.NumAudioStreams:
                    // TODO
                    return null;

                case ProfileConditionValue.NumVideoStreams:
                    // TODO
                    return null;

                case ProfileConditionValue.PacketLength:
                    // TODO
                    return null;

                case ProfileConditionValue.RefFrames:
                    return TranscodeReason.RefFramesNotSupported;

                case ProfileConditionValue.VideoBitDepth:
                    return TranscodeReason.VideoBitDepthNotSupported;

                case ProfileConditionValue.AudioBitDepth:
                    return TranscodeReason.AudioBitDepthNotSupported;

                case ProfileConditionValue.VideoBitrate:
                    return TranscodeReason.VideoBitrateNotSupported;

                case ProfileConditionValue.VideoCodecTag:
                    return TranscodeReason.VideoCodecNotSupported;

                case ProfileConditionValue.VideoFramerate:
                    return TranscodeReason.VideoFramerateNotSupported;

                case ProfileConditionValue.VideoLevel:
                    return TranscodeReason.VideoLevelNotSupported;

                case ProfileConditionValue.VideoProfile:
                    return TranscodeReason.VideoProfileNotSupported;

                case ProfileConditionValue.VideoTimestamp:
                    // TODO
                    return null;

                case ProfileConditionValue.Width:
                    return TranscodeReason.VideoResolutionNotSupported;

                default:
                    return null;
            }
        }

        public static string NormalizeMediaSourceFormatIntoSingleContainer(string inputContainer, DeviceProfile profile, DlnaProfileType type)
        {
            if (string.IsNullOrEmpty(inputContainer))
            {
                return null;
            }

            var formats = ContainerProfile.SplitValue(inputContainer);

            if (formats.Length == 1)
            {
                return formats[0];
            }

            if (profile != null)
            {
                foreach (var format in formats)
                {
                    foreach (var directPlayProfile in profile.DirectPlayProfiles)
                    {
                        if (directPlayProfile.Type == type
                            && directPlayProfile.SupportsContainer(format))
                        {
                            return format;
                        }
                    }
                }
            }

            return formats[0];
        }

        private StreamInfo BuildAudioItem(MediaSourceInfo item, AudioOptions options)
        {
            StreamInfo playlistItem = new StreamInfo
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

            var audioStream = item.GetDefaultAudioStream(null);

            var directPlayInfo = GetAudioDirectPlayMethods(item, audioStream, options);

            var directPlayMethods = directPlayInfo.PlayMethods;
            var transcodeReasons = directPlayInfo.TranscodeReasons.ToHashSet();

            if (directPlayMethods.Any())
            {
                var conditions = GetConditionsForCodec(audioStream?.Codec, item.Container, options.Profile.CodecProfiles, CodecType.Audio, item, audioStream);
                var failedConditions = FindFailedConditions(conditions, CodecType.Audio, item, audioStream);

                if (!failedConditions.Any())
                {
                    if (directPlayMethods.Contains(PlayMethod.DirectStream))
                    {
                        playlistItem.PlayMethod = PlayMethod.DirectStream;
                    }

                    playlistItem.Container = NormalizeMediaSourceFormatIntoSingleContainer(item.Container, options.Profile, DlnaProfileType.Audio);

                    return playlistItem;
                }
                else
                {
                    transcodeReasons.UnionWith(GetAllTranscodeReasonsForFailedConditions(failedConditions, options.Profile, "AudioCodecProfile", item));
                }
            }

            TranscodingProfile transcodingProfile = null;
            foreach (var i in options.Profile.TranscodingProfiles)
            {
                if (i.Type == playlistItem.MediaType
                    && i.Context == options.Context
                    && _transcoderSupport.CanEncodeToAudioCodec(i.AudioCodec ?? i.Container))
                {
                    transcodingProfile = i;
                    break;
                }
            }

            if (transcodingProfile != null)
            {
                if (!item.SupportsTranscoding)
                {
                    return null;
                }

                SetStreamInfoOptionsFromTranscodingProfile(playlistItem, transcodingProfile);

                var audioTranscodingConditions = GetConditionsForCodec(transcodingProfile.AudioCodec, transcodingProfile.Container, options.Profile.CodecProfiles, CodecType.Audio, item, audioStream);
                ApplyTranscodingConditions(playlistItem, audioTranscodingConditions, null, true, true);

                // Honor requested max channels
                playlistItem.GlobalMaxAudioChannels = options.MaxAudioChannels;

                var configuredBitrate = options.GetMaxBitrate(true);

                var transcodingBitrate = options.AudioTranscodingBitrate ??
                    (options.Context == EncodingContext.Streaming ? options.Profile.MusicStreamingTranscodingBitrate : null) ??
                    configuredBitrate ??
                    128000;

                if (configuredBitrate.HasValue)
                {
                    transcodingBitrate = Math.Min(configuredBitrate.Value, transcodingBitrate);
                }

                playlistItem.AudioBitrate = Math.Min(playlistItem.AudioBitrate ?? transcodingBitrate, transcodingBitrate);
            }

            playlistItem.TranscodeReasons = transcodeReasons.ToArray();
            return playlistItem;
        }

        private static int? GetBitrateForDirectPlayCheck(MediaSourceInfo item, AudioOptions options, bool isAudio)
        {
            if (item.Protocol == MediaProtocol.File)
            {
                return options.Profile.MaxStaticBitrate;
            }

            return options.GetMaxBitrate(isAudio);
        }

        private (IEnumerable<PlayMethod> PlayMethods, IEnumerable<TranscodeReason> TranscodeReasons) GetAudioDirectPlayMethods(MediaSourceInfo item, MediaStream audioStream, AudioOptions options)
        {
            DirectPlayProfile directPlayProfile = options.Profile.DirectPlayProfiles
                .FirstOrDefault(x => x.Type == DlnaProfileType.Audio && IsAudioDirectPlaySupported(x, item, audioStream));

            if (directPlayProfile == null)
            {
                _logger.LogDebug(
                    "Profile: {0}, No audio direct play profiles found for {1} with codec {2}",
                    options.Profile.Name ?? "Unknown Profile",
                    item.Path ?? "Unknown path",
                    audioStream.Codec ?? "Unknown codec");

                return (Enumerable.Empty<PlayMethod>(), GetTranscodeReasonsFromDirectPlayProfile(item, null, audioStream, options.Profile.DirectPlayProfiles));
            }

            var playMethods = new List<PlayMethod>();
            var transcodeReasons = new HashSet<TranscodeReason>();

            // While options takes the network and other factors into account. Only applies to direct stream
            if (item.SupportsDirectStream)
            {
                if (IsAudioEligibleForDirectPlay(item, options.GetMaxBitrate(true) ?? 0, PlayMethod.DirectStream))
                {
                    if (options.EnableDirectStream)
                    {
                        playMethods.Add(PlayMethod.DirectStream);
                    }
                }
                else
                {
                    transcodeReasons.Add(TranscodeReason.ContainerBitrateExceedsLimit);
                }
            }

            // The profile describes what the device supports
            // If device requirements are satisfied then allow both direct stream and direct play
            if (item.SupportsDirectPlay)
            {
                if (IsAudioEligibleForDirectPlay(item, GetBitrateForDirectPlayCheck(item, options, true) ?? 0, PlayMethod.DirectPlay))
                {
                    if (options.EnableDirectPlay)
                    {
                        playMethods.Add(PlayMethod.DirectPlay);
                    }
                }
                else
                {
                    transcodeReasons.Add(TranscodeReason.ContainerBitrateExceedsLimit);
                }
            }

            if (playMethods.Count > 0)
            {
                transcodeReasons.Clear();
            }

            return (playMethods, transcodeReasons);
        }

        private static IEnumerable<TranscodeReason> GetTranscodeReasonsFromDirectPlayProfile(MediaSourceInfo item, MediaStream videoStream, MediaStream audioStream, IEnumerable<DirectPlayProfile> directPlayProfiles)
        {
            var mediaType = videoStream == null ? DlnaProfileType.Audio : DlnaProfileType.Video;

            var containerSupported = false;
            var audioSupported = false;
            var videoSupported = false;

            foreach (var profile in directPlayProfiles)
            {
                // Check container type
                if (profile.Type == mediaType && profile.SupportsContainer(item.Container))
                {
                    containerSupported = true;

                    videoSupported = videoStream != null && profile.SupportsVideoCodec(videoStream.Codec);

                    audioSupported = audioStream != null && profile.SupportsAudioCodec(audioStream.Codec);

                    if (videoSupported && audioSupported)
                    {
                        break;
                    }
                }
            }

            if (!containerSupported)
            {
                yield return TranscodeReason.ContainerNotSupported;
            }

            if (videoStream != null && !videoSupported)
            {
                yield return TranscodeReason.VideoCodecNotSupported;
            }

            if (audioStream != null && !audioSupported)
            {
                yield return TranscodeReason.AudioCodecNotSupported;
            }

            yield break;
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

            var topStreams = new List<MediaStream>();
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

        private static void SetStreamInfoOptionsFromTranscodingProfile(StreamInfo playlistItem, TranscodingProfile transcodingProfile)
        {
            if (string.IsNullOrEmpty(transcodingProfile.AudioCodec))
            {
                playlistItem.AudioCodecs = Array.Empty<string>();
            }
            else
            {
                playlistItem.AudioCodecs = transcodingProfile.AudioCodec.Split(',');
            }

            playlistItem.Container = transcodingProfile.Container;
            playlistItem.EstimateContentLength = transcodingProfile.EstimateContentLength;
            playlistItem.TranscodeSeekInfo = transcodingProfile.TranscodeSeekInfo;

            if (string.IsNullOrEmpty(transcodingProfile.VideoCodec))
            {
                playlistItem.VideoCodecs = Array.Empty<string>();
            }
            else
            {
                playlistItem.VideoCodecs = transcodingProfile.VideoCodec.Split(',');
            }

            playlistItem.CopyTimestamps = transcodingProfile.CopyTimestamps;
            playlistItem.EnableSubtitlesInManifest = transcodingProfile.EnableSubtitlesInManifest;
            playlistItem.EnableMpegtsM2TsMode = transcodingProfile.EnableMpegtsM2TsMode;

            playlistItem.BreakOnNonKeyFrames = transcodingProfile.BreakOnNonKeyFrames;

            if (transcodingProfile.MinSegments > 0)
            {
                playlistItem.MinSegments = transcodingProfile.MinSegments;
            }

            if (transcodingProfile.SegmentLength > 0)
            {
                playlistItem.SegmentLength = transcodingProfile.SegmentLength;
            }

            playlistItem.SubProtocol = transcodingProfile.Protocol;

            if (!string.IsNullOrEmpty(transcodingProfile.MaxAudioChannels)
                && int.TryParse(transcodingProfile.MaxAudioChannels, NumberStyles.Any, CultureInfo.InvariantCulture, out int transcodingMaxAudioChannels))
            {
                playlistItem.TranscodingMaxAudioChannels = transcodingMaxAudioChannels;
            }
        }

        private StreamInfo BuildVideoItem(MediaSourceInfo item, VideoOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            StreamInfo outputStreamInfo = new StreamInfo
            {
                ItemId = options.ItemId,
                MediaType = DlnaProfileType.Video,
                MediaSource = item,
                RunTimeTicks = item.RunTimeTicks,
                Context = options.Context,
                DeviceProfile = options.Profile
            };

            outputStreamInfo.SubtitleStreamIndex = options.SubtitleStreamIndex ?? GetDefaultSubtitleStreamIndex(item, options.Profile.SubtitleProfiles);
            var subtitleStream = outputStreamInfo.SubtitleStreamIndex.HasValue ? item.GetMediaStream(MediaStreamType.Subtitle, outputStreamInfo.SubtitleStreamIndex.Value) : null;

            var audioStream = item.GetDefaultAudioStream(options.AudioStreamIndex ?? item.DefaultAudioStreamIndex);
            if (audioStream != null)
            {
                outputStreamInfo.AudioStreamIndex = audioStream.Index;
            }

            var videoStream = item.VideoStream;

            var transcodeReasons = new HashSet<TranscodeReason>();

            // First, try to DirectPlay.
            var canDirectPlayStream = TryBuildVideoStreamForDirectPlay(item, options, videoStream, audioStream, subtitleStream, outputStreamInfo, transcodeReasons);
            if (canDirectPlayStream)
            {
                return outputStreamInfo;
            }

            // Try to find a DirectPlayProfile that supports source video stream, so video can just be remuxed.
            var canNonVideoTranscodeStream = TryBuildVideoStreamForNonVideoTranscode(transcodeReasons, item, options, videoStream, audioStream, subtitleStream, outputStreamInfo);
            if (canNonVideoTranscodeStream)
            {
                return outputStreamInfo;
            }

            // Fallback to a full transcode of the stream.
            var canFullTranscodeStream = TryBuildVideoStreamForFullTranscode(transcodeReasons, item, options, videoStream, audioStream, subtitleStream, outputStreamInfo);
            if (canFullTranscodeStream)
            {
                return outputStreamInfo;
            }

            return null;
        }

        private static int GetDefaultAudioBitrate(string audioCodec, int? audioChannels)
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

                    return audioChannels >= 6 ? 640000 : 384000;
                }

                if (string.Equals(audioCodec, "flac", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(audioCodec, "alac", StringComparison.OrdinalIgnoreCase))
                {
                    if ((audioChannels ?? 0) < 2)
                    {
                        return 768000;
                    }

                    return audioChannels >= 6 ? 3584000 : 1536000;
                }
            }

            return 192000;
        }

        private IEnumerable<ProfileCondition> GetConditionsForCodec(string targetCodec, string targetContainer, IEnumerable<CodecProfile> codecProfiles, CodecType codecType, MediaSourceInfo mediaSource, MediaStream stream)
        {
            if (targetCodec == null)
            {
                yield break;
            }

            foreach (var codecProfile in codecProfiles)
            {
                if (codecProfile.Type != codecType || !codecProfile.ContainsAnyCodec(targetCodec, targetContainer))
                {
                    // Ignore this codec profile as it doesn't apply
                    continue;
                }

                // Add the conditions only if the conditions apply
                var failedApplyConditions = FindFailedConditions(codecProfile.ApplyConditions, codecType, mediaSource, stream);
                if (!failedApplyConditions.Any())
                {
                    foreach (var condition in codecProfile.Conditions)
                    {
                        yield return condition;
                    }
                }
            }

            yield break;
        }

        private IEnumerable<ProfileCondition> FindFailedConditions(IEnumerable<ProfileCondition> conditions, CodecType codecType, MediaSourceInfo mediaSource, MediaStream stream)
        {
            int? bitrate = stream?.BitRate;
            string profile = stream?.Profile;
            int? bitDepth = stream?.BitDepth;
            bool? isSecondaryAudio = mediaSource.IsSecondaryAudio(stream);
            int? channels = stream?.Channels;
            int? sampleRate = stream?.SampleRate;

            foreach (ProfileCondition condition in conditions)
            {
                switch (codecType)
                {
                    case CodecType.Audio:
                        {
                            if (!ConditionProcessor.IsAudioConditionSatisfied(condition, channels, bitrate, sampleRate, bitDepth))
                            {
                                yield return condition;
                            }

                            break;
                        }
                    case CodecType.VideoAudio:
                        {
                            if (!ConditionProcessor.IsVideoAudioConditionSatisfied(condition, channels, bitrate, sampleRate, bitDepth, profile, isSecondaryAudio))
                            {
                                yield return condition;
                            }

                            break;
                        }

                    case CodecType.Video:
                        {
                            int? width = stream?.Width;
                            int? height = stream?.Height;
                            double? level = stream?.Level;
                            float frameRate = stream?.AverageFrameRate ?? 0;
                            bool? isAnamorphic = stream?.IsAnamorphic;
                            bool? isInterlaced = stream?.IsInterlaced;
                            string codecTag = stream?.CodecTag;
                            bool? isAvc = stream?.IsAVC;

                            TransportStreamTimestamp? timestamp = stream != null ? mediaSource.Timestamp : TransportStreamTimestamp.None;
                            int? packetLength = stream?.PacketLength;
                            int? refFrames = stream?.RefFrames;

                            int? numAudioStreams = mediaSource.GetStreamCount(MediaStreamType.Audio);
                            int? numVideoStreams = mediaSource.GetStreamCount(MediaStreamType.Video);

                            if (!ConditionProcessor.IsVideoConditionSatisfied(condition, width, height, bitDepth, bitrate, profile, level, frameRate, packetLength, timestamp, isAnamorphic, isInterlaced, refFrames, numVideoStreams, numAudioStreams, codecTag, isAvc))
                            {
                                yield return condition;
                            }

                            break;
                        }
                }
            }

            yield break;
        }

        private IEnumerable<TranscodeReason> GetAllTranscodeReasonsForFailedConditions(IEnumerable<ProfileCondition> failedConditions, DeviceProfile deviceProfile, string profileType, MediaSourceInfo mediaSource)
        {
            foreach (var failedCondition in failedConditions)
            {
                LogConditionFailure(deviceProfile, profileType, failedCondition, mediaSource);

                var transcodeReason = GetTranscodeReasonForFailedCondition(failedCondition);
                if (transcodeReason != null)
                {
                    yield return transcodeReason.Value;
                }
            }

            yield break;
        }

        private bool TryBuildVideoStreamForDirectPlay(MediaSourceInfo mediaSource, VideoOptions options, MediaStream videoStream, MediaStream audioStream, MediaStream subtitleStream, StreamInfo outputStreamInfo, HashSet<TranscodeReason> transcodeReasons)
        {
            // TODO: This doesn't account for situations where the device is able to handle the media's bitrate, but the connection isn't fast enough
            var directPlayEligibilityResult = IsEligibleForDirectPlay(mediaSource, GetBitrateForDirectPlayCheck(mediaSource, options, true) ?? 0, subtitleStream, audioStream, options, PlayMethod.DirectPlay);
            var directStreamEligibilityResult = IsEligibleForDirectPlay(mediaSource, options.GetMaxBitrate(false) ?? 0, subtitleStream, audioStream, options, PlayMethod.DirectStream);
            bool isEligibleForDirectPlay = options.EnableDirectPlay && (options.ForceDirectPlay || directPlayEligibilityResult.DirectPlay);
            bool isEligibleForDirectStream = options.EnableDirectStream && (options.ForceDirectStream || directStreamEligibilityResult.DirectPlay);

            _logger.LogInformation(
                "Profile: {0}, Path: {1}, isEligibleForDirectPlay: {2}, isEligibleForDirectStream: {3}",
                options.Profile.Name ?? "Unknown Profile",
                mediaSource.Path ?? "Unknown path",
                isEligibleForDirectPlay,
                isEligibleForDirectStream);

            // Local lambda that adds transcode reasons from direct play/stream eligibility checks
            Action addEligibilityTranscodeReasons = () =>
            {
                if (directPlayEligibilityResult.Reason.HasValue)
                {
                    transcodeReasons.Add(directPlayEligibilityResult.Reason.Value);
                }

                if (directStreamEligibilityResult.Reason.HasValue)
                {
                    transcodeReasons.Add(directStreamEligibilityResult.Reason.Value);
                }
            };

            if (!isEligibleForDirectPlay && !isEligibleForDirectStream)
            {
                addEligibilityTranscodeReasons();
                return false;
            }

            // Since the stream is eligible for direct play, see if it can be direct played
            var directPlayInfo = GetVideoDirectPlayProfile(options, mediaSource, videoStream, audioStream, isEligibleForDirectStream);
            var directPlay = directPlayInfo.PlayMethod;

            if (directPlay == null)
            {
                transcodeReasons.UnionWith(directPlayInfo.TranscodeReasons);
                addEligibilityTranscodeReasons();
                return false;
            }

            outputStreamInfo.PlayMethod = directPlay.Value;
            outputStreamInfo.Container = NormalizeMediaSourceFormatIntoSingleContainer(mediaSource.Container, options.Profile, DlnaProfileType.Video);

            if (subtitleStream != null)
            {
                var subtitleProfile = GetSubtitleProfile(mediaSource, subtitleStream, options.Profile.SubtitleProfiles, directPlay.Value, _transcoderSupport, mediaSource.Container, null);

                outputStreamInfo.SubtitleDeliveryMethod = subtitleProfile.Method;
                outputStreamInfo.SubtitleFormat = subtitleProfile.Format;
            }

            return true;
        }

        private bool TryBuildVideoStreamForNonVideoTranscode(IEnumerable<TranscodeReason> transcodeReasons, MediaSourceInfo mediaSource, VideoOptions options, MediaStream videoStream, MediaStream audioStream, MediaStream subtitleStream, StreamInfo outputStreamInfo)
        {
            // Check that the transcode reasons are all unrelated to video
            foreach (var transcodeReason in transcodeReasons)
            {
                switch (transcodeReason)
                {
                    case TranscodeReason.ContainerNotSupported:
                    case TranscodeReason.AudioCodecNotSupported:
                    case TranscodeReason.AudioBitrateNotSupported:
                    case TranscodeReason.AudioChannelsNotSupported:
                    case TranscodeReason.UnknownAudioStreamInfo:
                    case TranscodeReason.AudioProfileNotSupported:
                    case TranscodeReason.AudioSampleRateNotSupported:
                    case TranscodeReason.SecondaryAudioNotSupported:
                        // These are the only `TranscodeReason`s that do not indicate that video needs to be transcoded.
                        break;
                    default:
                        return false;
                }
            }

            // Find the target `DirectPlayProfile`
            var targetProfile = options.Profile.DirectPlayProfiles
                .Where(p =>
                {
                    if (p.Type != DlnaProfileType.Video || !p.SupportsVideoCodec(videoStream.Codec))
                    {
                        return false;
                    }

                    var videoCodecConditions = GetConditionsForCodec(videoStream.Codec, p.Container, options.Profile.CodecProfiles, CodecType.Video, mediaSource, videoStream);
                    return !FindFailedConditions(videoCodecConditions, CodecType.Video, mediaSource, videoStream).Any();
                })
                .OrderBy(p => ContainerProfile.SplitValue(p.AudioCodec).Length)
                .FirstOrDefault();

            if (targetProfile == null)
            {
                _logger.LogWarning("Could not find compatible non-video transcode despite the absence of video-related TranscodeReasons for {0}", mediaSource.Path);
                return false;
            }

            outputStreamInfo.PlayMethod = PlayMethod.Transcode;
            outputStreamInfo.AudioCodecs = targetProfile.AudioCodec.Split(',');
            outputStreamInfo.Container = targetProfile.Container;
            outputStreamInfo.EstimateContentLength = false;
            outputStreamInfo.TranscodeSeekInfo = TranscodeSeekInfo.Auto;
            outputStreamInfo.CopyTimestamps = true;
            outputStreamInfo.TranscodingMaxAudioChannels = audioStream.Channels;
            outputStreamInfo.GlobalMaxAudioChannels = options.MaxAudioChannels;
            outputStreamInfo.VideoCodecs = new string[] { videoStream.Codec };
            outputStreamInfo.TranscodeReasons = transcodeReasons.ToArray();

            outputStreamInfo.AudioBitrate = GetAudioBitrate(options.GetMaxBitrate(false) ?? 0, outputStreamInfo.AudioCodecs, audioStream, outputStreamInfo);
            outputStreamInfo.VideoBitrate = videoStream.BitRate;

            // TODO: It could be possible to support HLS (given support and correct codecs) or DASH (given support) in the future.
            outputStreamInfo.SubProtocol = "http";

            if (subtitleStream != null)
            {
                var subtitleProfile = GetSubtitleProfile(mediaSource, subtitleStream, options.Profile.SubtitleProfiles, PlayMethod.Transcode, _transcoderSupport, outputStreamInfo.Container, outputStreamInfo.SubProtocol);

                outputStreamInfo.SubtitleDeliveryMethod = subtitleProfile.Method;
                outputStreamInfo.SubtitleFormat = subtitleProfile.Format;
                outputStreamInfo.SubtitleCodecs = new[] { subtitleProfile.Format };
            }

            return true;
        }

        private bool TryBuildVideoStreamForFullTranscode(IEnumerable<TranscodeReason> transcodeReasons, MediaSourceInfo mediaSource, VideoOptions options, MediaStream videoStream, MediaStream audioStream, MediaStream subtitleStream, StreamInfo outputStreamInfo)
        {
            // Can't direct play, find the transcoding profile
            TranscodingProfile transcodingProfile = options.Profile.TranscodingProfiles
                .Where(elem => elem.Type == outputStreamInfo.MediaType)
                .FirstOrDefault();

            if (transcodingProfile == null || !mediaSource.SupportsTranscoding)
            {
                return false;
            }

            if (subtitleStream != null)
            {
                var subtitleProfile = GetSubtitleProfile(mediaSource, subtitleStream, options.Profile.SubtitleProfiles, PlayMethod.Transcode, _transcoderSupport, transcodingProfile.Container, transcodingProfile.Protocol);

                outputStreamInfo.SubtitleDeliveryMethod = subtitleProfile.Method;
                outputStreamInfo.SubtitleFormat = subtitleProfile.Format;
                outputStreamInfo.SubtitleCodecs = new[] { subtitleProfile.Format };
            }

            outputStreamInfo.PlayMethod = PlayMethod.Transcode;

            SetStreamInfoOptionsFromTranscodingProfile(outputStreamInfo, transcodingProfile);

            var isFirstAppliedAudioCodecProfile = true;
            var isFirstAppliedVideoCodecProfile = true;
            foreach (var i in options.Profile.CodecProfiles)
            {
                if (i.Type == CodecType.Video && i.ContainsAnyCodec(transcodingProfile.VideoCodec, transcodingProfile.Container))
                {
                    // Check if these conditions should apply
                    var failedApplyConditions = FindFailedConditions(i.ApplyConditions, CodecType.Video, mediaSource, videoStream);
                    if (!failedApplyConditions.Any())
                    {
                        var transcodingVideoCodecs = ContainerProfile.SplitValue(transcodingProfile.VideoCodec);
                        foreach (var transcodingVideoCodec in transcodingVideoCodecs)
                        {
                            if (i.ContainsAnyCodec(transcodingVideoCodec, transcodingProfile.Container))
                            {
                                ApplyTranscodingConditions(outputStreamInfo, i.Conditions, transcodingVideoCodec, true, isFirstAppliedVideoCodecProfile);
                                isFirstAppliedVideoCodecProfile = false;
                            }
                        }
                    }
                }
                else if (i.Type == CodecType.Audio && i.ContainsAnyCodec(transcodingProfile.AudioCodec, transcodingProfile.Container))
                {
                    // Check if these conditions should apply
                    var failedConditions = FindFailedConditions(i.ApplyConditions, CodecType.VideoAudio, mediaSource, audioStream);
                    if (!failedConditions.Any())
                    {
                        var transcodingAudioCodecs = ContainerProfile.SplitValue(transcodingProfile.AudioCodec);
                        foreach (var transcodingAudioCodec in transcodingAudioCodecs)
                        {
                            if (i.ContainsAnyCodec(transcodingAudioCodec, transcodingProfile.Container))
                            {
                                ApplyTranscodingConditions(outputStreamInfo, i.Conditions, transcodingAudioCodec, true, isFirstAppliedAudioCodecProfile);
                                isFirstAppliedAudioCodecProfile = false;
                            }
                        }
                    }
                }
            }

            // Honor requested max channels
            outputStreamInfo.GlobalMaxAudioChannels = options.MaxAudioChannels;

            int audioBitrate = GetAudioBitrate(options.GetMaxBitrate(false) ?? 0, outputStreamInfo.TargetAudioCodec, audioStream, outputStreamInfo);
            outputStreamInfo.AudioBitrate = Math.Min(outputStreamInfo.AudioBitrate ?? audioBitrate, audioBitrate);

            var maxBitrateSetting = options.GetMaxBitrate(false);
            // Honor max rate
            if (maxBitrateSetting.HasValue)
            {
                var availableBitrateForVideo = maxBitrateSetting.Value;

                if (outputStreamInfo.AudioBitrate.HasValue)
                {
                    availableBitrateForVideo -= outputStreamInfo.AudioBitrate.Value;
                }

                // Make sure the video bitrate is lower than bitrate settings but at least 64k
                outputStreamInfo.VideoBitrate = Math.Max(Math.Min(availableBitrateForVideo, outputStreamInfo.VideoBitrate ?? availableBitrateForVideo), 64000);
            }

            outputStreamInfo.TranscodeReasons = transcodeReasons.ToArray();

            return true;
        }

        private static int GetAudioBitrate(int maxTotalBitrate, string[] targetAudioCodecs, MediaStream audioStream, StreamInfo item)
        {
            string targetAudioCodec = targetAudioCodecs.Length != 0 ? targetAudioCodecs[0] : null;

            int? targetAudioChannels = item.GetTargetAudioChannels(targetAudioCodec);

            int defaultBitrate;
            int encoderAudioBitrateLimit = int.MaxValue;

            if (audioStream == null)
            {
                defaultBitrate = 192000;
            }
            else
            {
                if (targetAudioChannels.HasValue
                    && audioStream.Channels.HasValue
                    && audioStream.Channels.Value > targetAudioChannels.Value)
                {
                    // Reduce the bitrate if we're downmixing.
                    defaultBitrate = GetDefaultAudioBitrate(targetAudioCodec, targetAudioChannels);
                }
                else if (targetAudioChannels.HasValue
                         && audioStream.Channels.HasValue
                         && audioStream.Channels.Value <= targetAudioChannels.Value
                         && !string.IsNullOrEmpty(audioStream.Codec)
                         && targetAudioCodecs != null
                         && targetAudioCodecs.Length > 0
                         && !Array.Exists(targetAudioCodecs, elem => string.Equals(audioStream.Codec, elem, StringComparison.OrdinalIgnoreCase)))
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

        private static int GetMaxAudioBitrateForTotalBitrate(int totalBitrate)
        {
            if (totalBitrate <= 640000)
            {
                return 128000;
            }
            else if (totalBitrate <= 2000000)
            {
                return 384000;
            }
            else if (totalBitrate <= 3000000)
            {
                return 448000;
            }
            else if (totalBitrate <= 4000000)
            {
                return 640000;
            }
            else if (totalBitrate <= 5000000)
            {
                return 768000;
            }
            else if (totalBitrate <= 10000000)
            {
                return 1536000;
            }
            else if (totalBitrate <= 15000000)
            {
                return 2304000;
            }
            else if (totalBitrate <= 20000000)
            {
                return 3584000;
            }

            return 7168000;
        }

        private (PlayMethod? PlayMethod, HashSet<TranscodeReason> TranscodeReasons) GetVideoDirectPlayProfile(
            VideoOptions options,
            MediaSourceInfo mediaSource,
            MediaStream videoStream,
            MediaStream audioStream,
            bool isEligibleForDirectStream)
        {
            if (options.ForceDirectPlay)
            {
                return (PlayMethod.DirectPlay, new HashSet<TranscodeReason>());
            }

            if (options.ForceDirectStream)
            {
                return (PlayMethod.DirectStream, new HashSet<TranscodeReason>());
            }

            DeviceProfile profile = options.Profile;
            string container = mediaSource.Container;

            // See if it can be direct played
            DirectPlayProfile directPlay = null;
            foreach (var p in profile.DirectPlayProfiles)
            {
                if (p.Type == DlnaProfileType.Video && IsVideoDirectPlaySupported(p, container, videoStream, audioStream))
                {
                    directPlay = p;
                    break;
                }
            }

            if (directPlay == null)
            {
                _logger.LogDebug(
                    "Container: {Container}, Video: {Video}, Audio: {Audio} cannot be direct played by profile: {Profile} for path: {Path}",
                    container,
                    videoStream?.Codec ?? "no video",
                    audioStream?.Codec ?? "no audio",
                    profile.Name ?? "unknown profile",
                    mediaSource.Path ?? "unknown path");

                return (null, GetTranscodeReasonsFromDirectPlayProfile(mediaSource, videoStream, audioStream, profile.DirectPlayProfiles).ToHashSet());
            }

            // Check container conditions
            var containerConditions = new List<ProfileCondition>();
            foreach (var p in profile.ContainerProfiles)
            {
                if (p.Type == DlnaProfileType.Video
                    && p.ContainsContainer(container))
                {
                    foreach (var c in p.Conditions)
                    {
                        containerConditions.Add(c);
                    }
                }
            }

            var failedContainerConditions = FindFailedConditions(containerConditions, CodecType.Video, mediaSource, videoStream);

            // Check video conditions
            var videoCodecConditions = GetConditionsForCodec(videoStream.Codec, mediaSource.Container, profile.CodecProfiles, CodecType.Video, mediaSource, videoStream);
            var failedVideoCodecConditions = FindFailedConditions(videoCodecConditions, CodecType.Video, mediaSource, videoStream);

            // Check audio conditions
            var failedVideoAudioCodecConditions = Enumerable.Empty<ProfileCondition>();
            if (audioStream != null)
            {
                var audioCodecConditions = GetConditionsForCodec(videoStream.Codec, mediaSource.Container, profile.CodecProfiles, CodecType.VideoAudio, mediaSource, audioStream);
                failedVideoAudioCodecConditions = FindFailedConditions(audioCodecConditions, CodecType.VideoAudio, mediaSource, audioStream);
            }

            if (failedContainerConditions.Any() || failedVideoCodecConditions.Any() || failedVideoAudioCodecConditions.Any())
            {
                var transcodeReasons = new HashSet<TranscodeReason>();

                transcodeReasons.UnionWith(GetAllTranscodeReasonsForFailedConditions(failedContainerConditions, profile, "VideoContainerProfile", mediaSource));

                transcodeReasons.UnionWith(GetAllTranscodeReasonsForFailedConditions(failedVideoCodecConditions, profile, "VideoCodecProfile", mediaSource));

                transcodeReasons.UnionWith(GetAllTranscodeReasonsForFailedConditions(failedVideoAudioCodecConditions, profile, "VideoAudioCodecProfile", mediaSource));

                return (null, transcodeReasons);
            }

            if (isEligibleForDirectStream && mediaSource.SupportsDirectStream)
            {
                return (PlayMethod.DirectStream, new HashSet<TranscodeReason>());
            }

            return (null, new HashSet<TranscodeReason> { TranscodeReason.ContainerBitrateExceedsLimit });
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

        private (bool DirectPlay, TranscodeReason? Reason) IsEligibleForDirectPlay(
            MediaSourceInfo item,
            int maxBitrate,
            MediaStream subtitleStream,
            MediaStream audioStream,
            VideoOptions options,
            PlayMethod playMethod)
        {
            if (subtitleStream != null)
            {
                var subtitleProfile = GetSubtitleProfile(item, subtitleStream, options.Profile.SubtitleProfiles, playMethod, _transcoderSupport, item.Container, null);

                if (subtitleProfile.Method != SubtitleDeliveryMethod.Drop
                    && subtitleProfile.Method != SubtitleDeliveryMethod.External
                    && subtitleProfile.Method != SubtitleDeliveryMethod.Embed)
                {
                    _logger.LogDebug("Not eligible for {0} due to unsupported subtitles", playMethod);
                    return (false, TranscodeReason.SubtitleCodecNotSupported);
                }
            }

            bool result = IsAudioEligibleForDirectPlay(item, maxBitrate, playMethod);
            if (!result)
            {
                return (false, TranscodeReason.ContainerBitrateExceedsLimit);
            }

            if (audioStream?.IsExternal == true)
            {
                return (false, TranscodeReason.AudioIsExternal);
            }

            return (true, null);
        }

        public static SubtitleProfile GetSubtitleProfile(
            MediaSourceInfo mediaSource,
            MediaStream subtitleStream,
            SubtitleProfile[] subtitleProfiles,
            PlayMethod playMethod,
            ITranscoderSupport transcoderSupport,
            string outputContainer,
            string transcodingSubProtocol)
        {
            if (!subtitleStream.IsExternal && (playMethod != PlayMethod.Transcode || !string.Equals(transcodingSubProtocol, "hls", StringComparison.OrdinalIgnoreCase)))
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

                    if (!ContainerProfile.ContainsContainer(profile.Container, outputContainer))
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

                    if (!ContainerProfile.ContainsContainer(profile.Container, outputContainer))
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

        private static bool IsSubtitleEmbedSupported(string transcodingContainer)
        {
            if (!string.IsNullOrEmpty(transcodingContainer))
            {
                string[] normalizedContainers = ContainerProfile.SplitValue(transcodingContainer);

                if (ContainerProfile.ContainsContainer(normalizedContainers, "ts")
                    || ContainerProfile.ContainsContainer(normalizedContainers, "mpegts")
                    || ContainerProfile.ContainsContainer(normalizedContainers, "mp4"))
                {
                    return false;
                }
                else if (ContainerProfile.ContainsContainer(normalizedContainers, "mkv")
                    || ContainerProfile.ContainsContainer(normalizedContainers, "matroska"))
                {
                    return true;
                }
            }

            return false;
        }

        private static SubtitleProfile GetExternalSubtitleProfile(MediaSourceInfo mediaSource, MediaStream subtitleStream, SubtitleProfile[] subtitleProfiles, PlayMethod playMethod, ITranscoderSupport transcoderSupport, bool allowConversion)
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

        private bool IsAudioEligibleForDirectPlay(MediaSourceInfo item, int maxBitrate, PlayMethod playMethod)
        {
            // Don't restrict by bitrate if coming from an external domain
            if (item.IsRemote)
            {
                return true;
            }

            int requestedMaxBitrate = maxBitrate > 0 ? maxBitrate : 1000000;

            // If we don't know the bitrate, then force a transcode if requested max bitrate is under 40 mbps
            int itemBitrate = item.Bitrate ?? 40000000;

            if (itemBitrate > requestedMaxBitrate)
            {
                _logger.LogDebug(
                    "Bitrate exceeds {PlayBackMethod} limit: media bitrate: {MediaBitrate}, max bitrate: {MaxBitrate}",
                    playMethod,
                    itemBitrate,
                    requestedMaxBitrate);
                return false;
            }

            return true;
        }

        private static void ValidateInput(VideoOptions options)
        {
            ValidateAudioInput(options);

            if (options.AudioStreamIndex.HasValue && string.IsNullOrEmpty(options.MediaSourceId))
            {
                throw new ArgumentException("MediaSourceId is required when a specific audio stream is requested");
            }

            if (options.SubtitleStreamIndex.HasValue && string.IsNullOrEmpty(options.MediaSourceId))
            {
                throw new ArgumentException("MediaSourceId is required when a specific subtitle stream is requested");
            }
        }

        private static void ValidateAudioInput(AudioOptions options)
        {
            if (options.ItemId.Equals(Guid.Empty))
            {
                throw new ArgumentException("ItemId is required");
            }

            if (string.IsNullOrEmpty(options.DeviceId))
            {
                throw new ArgumentException("DeviceId is required");
            }

            if (options.Profile == null)
            {
                throw new ArgumentException("Profile is required");
            }

            if (options.MediaSources == null)
            {
                throw new ArgumentException("MediaSources is required");
            }
        }

        private void ApplyTranscodingConditions(StreamInfo item, IEnumerable<ProfileCondition> conditions, string qualifier, bool enableQualifiedConditions, bool enableNonQualifiedConditions)
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

                            if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
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

                            if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
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

                            if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
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

                            if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
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

                            if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
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

                            // change from split by | to comma
                            // strip spaces to avoid having to encode
                            var values = value
                                .Split('|', StringSplitOptions.RemoveEmptyEntries);

                            if (condition.Condition == ProfileConditionType.Equals || condition.Condition == ProfileConditionType.EqualsAny)
                            {
                                item.SetOption(qualifier, "profile", string.Join(',', values));
                            }

                            break;
                        }

                    case ProfileConditionValue.Height:
                        {
                            if (!enableNonQualifiedConditions)
                            {
                                continue;
                            }

                            if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
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

                            if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
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

                            if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
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

                            if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
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

                            if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
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

        private static bool IsAudioDirectPlaySupported(DirectPlayProfile profile, MediaSourceInfo item, MediaStream audioStream)
        {
            // Check container type
            if (!profile.SupportsContainer(item.Container))
            {
                return false;
            }

            // Check audio codec
            string audioCodec = audioStream?.Codec;
            if (!profile.SupportsAudioCodec(audioCodec))
            {
                return false;
            }

            return true;
        }

        private bool IsVideoDirectPlaySupported(DirectPlayProfile profile, string container, MediaStream videoStream, MediaStream audioStream)
        {
            // Check container type
            if (!profile.SupportsContainer(container))
            {
                return false;
            }

            // Check video codec
            string videoCodec = videoStream?.Codec;
            if (!profile.SupportsVideoCodec(videoCodec))
            {
                return false;
            }

            // Check audio codec
            if (audioStream != null && !profile.SupportsAudioCodec(audioStream.Codec))
            {
                return false;
            }

            return true;
        }
    }
}
