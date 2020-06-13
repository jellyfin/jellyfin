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

        private static StreamInfo GetOptimalStream(List<StreamInfo> streams, long maxBitrate)
            => SortMediaSources(streams, maxBitrate).FirstOrDefault();

        private static IOrderedEnumerable<StreamInfo> SortMediaSources(List<StreamInfo> streams, long maxBitrate)
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

        public static string NormalizeMediaSourceFormatIntoSingleContainer(string inputContainer, string _, DeviceProfile profile, DlnaProfileType type)
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
                playlistItem.Container = NormalizeMediaSourceFormatIntoSingleContainer(item.Container, item.Path, options.Profile, DlnaProfileType.Audio);
                return playlistItem;
            }

            if (options.ForceDirectStream)
            {
                playlistItem.PlayMethod = PlayMethod.DirectStream;
                playlistItem.Container = NormalizeMediaSourceFormatIntoSingleContainer(item.Container, item.Path, options.Profile, DlnaProfileType.Audio);
                return playlistItem;
            }

            var audioStream = item.GetDefaultAudioStream(null);

            var directPlayInfo = GetAudioDirectPlayMethods(item, audioStream, options);

            var directPlayMethods = directPlayInfo.Item1;
            var transcodeReasons = directPlayInfo.Item2.ToList();

            int? inputAudioChannels = audioStream?.Channels;
            int? inputAudioBitrate = audioStream?.BitDepth;
            int? inputAudioSampleRate = audioStream?.SampleRate;
            int? inputAudioBitDepth = audioStream?.BitDepth;

            if (directPlayMethods.Count() > 0)
            {
                string audioCodec = audioStream?.Codec;

                // Make sure audio codec profiles are satisfied
                var conditions = new List<ProfileCondition>();
                foreach (var i in options.Profile.CodecProfiles)
                {
                    if (i.Type == CodecType.Audio && i.ContainsAnyCodec(audioCodec, item.Container))
                    {
                        bool applyConditions = true;
                        foreach (ProfileCondition applyCondition in i.ApplyConditions)
                        {
                            if (!ConditionProcessor.IsAudioConditionSatisfied(applyCondition, inputAudioChannels, inputAudioBitrate, inputAudioSampleRate, inputAudioBitDepth))
                            {
                                LogConditionFailure(options.Profile, "AudioCodecProfile", applyCondition, item);
                                applyConditions = false;
                                break;
                            }
                        }

                        if (applyConditions)
                        {
                            conditions.AddRange(i.Conditions);
                        }
                    }
                }

                bool all = true;
                foreach (ProfileCondition c in conditions)
                {
                    if (!ConditionProcessor.IsAudioConditionSatisfied(c, inputAudioChannels, inputAudioBitrate, inputAudioSampleRate, inputAudioBitDepth))
                    {
                        LogConditionFailure(options.Profile, "AudioCodecProfile", c, item);
                        var transcodeReason = GetTranscodeReasonForFailedCondition(c);
                        if (transcodeReason.HasValue)
                        {
                            transcodeReasons.Add(transcodeReason.Value);
                        }

                        all = false;
                        break;
                    }
                }

                if (all)
                {
                    if (directPlayMethods.Contains(PlayMethod.DirectStream))
                    {
                        playlistItem.PlayMethod = PlayMethod.DirectStream;
                    }

                    playlistItem.Container = NormalizeMediaSourceFormatIntoSingleContainer(item.Container, item.Path, options.Profile, DlnaProfileType.Audio);

                    return playlistItem;
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

                var audioCodecProfiles = new List<CodecProfile>();
                foreach (var i in options.Profile.CodecProfiles)
                {
                    if (i.Type == CodecType.Audio && i.ContainsAnyCodec(transcodingProfile.AudioCodec, transcodingProfile.Container))
                    {
                        audioCodecProfiles.Add(i);
                    }

                    if (audioCodecProfiles.Count >= 1) break;
                }

                var audioTranscodingConditions = new List<ProfileCondition>();
                foreach (var i in audioCodecProfiles)
                {
                    bool applyConditions = true;
                    foreach (var applyCondition in i.ApplyConditions)
                    {
                        if (!ConditionProcessor.IsAudioConditionSatisfied(applyCondition, inputAudioChannels, inputAudioBitrate, inputAudioSampleRate, inputAudioBitDepth))
                        {
                            LogConditionFailure(options.Profile, "AudioCodecProfile", applyCondition, item);
                            applyConditions = false;
                            break;
                        }
                    }

                    if (applyConditions)
                    {
                        foreach (ProfileCondition c in i.Conditions)
                        {
                            audioTranscodingConditions.Add(c);
                        }
                    }
                }

                ApplyTranscodingConditions(playlistItem, audioTranscodingConditions, null, true, true);

                // Honor requested max channels
                playlistItem.GlobalMaxAudioChannels = options.MaxAudioChannels;

                var configuredBitrate = options.GetMaxBitrate(true);

                long transcodingBitrate = options.AudioTranscodingBitrate ??
                    (options.Context == EncodingContext.Streaming ? options.Profile.MusicStreamingTranscodingBitrate : null) ??
                    configuredBitrate ??
                    128000;

                if (configuredBitrate.HasValue)
                {
                    transcodingBitrate = Math.Min(configuredBitrate.Value, transcodingBitrate);
                }

                var longBitrate = Math.Min(transcodingBitrate, playlistItem.AudioBitrate ?? transcodingBitrate);
                playlistItem.AudioBitrate = longBitrate > int.MaxValue ? int.MaxValue : Convert.ToInt32(longBitrate);
            }

            playlistItem.TranscodeReasons = transcodeReasons.ToArray();
            return playlistItem;
        }

        private static long? GetBitrateForDirectPlayCheck(MediaSourceInfo item, AudioOptions options, bool isAudio)
        {
            if (item.Protocol == MediaProtocol.File)
            {
                return options.Profile.MaxStaticBitrate;
            }

            return options.GetMaxBitrate(isAudio);
        }

        private (IEnumerable<PlayMethod>, IEnumerable<TranscodeReason>) GetAudioDirectPlayMethods(MediaSourceInfo item, MediaStream audioStream, AudioOptions options)
        {
            DirectPlayProfile directPlayProfile = options.Profile.DirectPlayProfiles
                .FirstOrDefault(x => x.Type == DlnaProfileType.Audio && IsAudioDirectPlaySupported(x, item, audioStream));

            if (directPlayProfile == null)
            {
                _logger.LogInformation("Profile: {0}, No direct play profiles found for Path: {1}",
                    options.Profile.Name ?? "Unknown Profile",
                    item.Path ?? "Unknown path");

                return (Enumerable.Empty<PlayMethod>(), GetTranscodeReasonsFromDirectPlayProfile(item, null, audioStream, options.Profile.DirectPlayProfiles));
            }

            var playMethods = new List<PlayMethod>();
            var transcodeReasons = new List<TranscodeReason>();

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
            else
            {
                transcodeReasons = transcodeReasons.Distinct().ToList();
            }

            return (playMethods, transcodeReasons);
        }

        private static List<TranscodeReason> GetTranscodeReasonsFromDirectPlayProfile(MediaSourceInfo item, MediaStream videoStream, MediaStream audioStream, IEnumerable<DirectPlayProfile> directPlayProfiles)
        {
            var containerSupported = false;
            var audioSupported = false;
            var videoSupported = false;

            foreach (var profile in directPlayProfiles)
            {
                // Check container type
                if (profile.SupportsContainer(item.Container))
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

            var list = new List<TranscodeReason>();
            if (!containerSupported)
            {
                list.Add(TranscodeReason.ContainerNotSupported);
            }

            if (videoStream != null && !videoSupported)
            {
                list.Add(TranscodeReason.VideoCodecNotSupported);
            }

            if (audioStream != null && !audioSupported)
            {
                list.Add(TranscodeReason.AudioCodecNotSupported);
            }

            return list;
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

            StreamInfo playlistItem = new StreamInfo
            {
                ItemId = options.ItemId,
                MediaType = DlnaProfileType.Video,
                MediaSource = item,
                RunTimeTicks = item.RunTimeTicks,
                Context = options.Context,
                DeviceProfile = options.Profile
            };

            playlistItem.SubtitleStreamIndex = options.SubtitleStreamIndex ?? GetDefaultSubtitleStreamIndex(item, options.Profile.SubtitleProfiles);
            var subtitleStream = playlistItem.SubtitleStreamIndex.HasValue ? item.GetMediaStream(MediaStreamType.Subtitle, playlistItem.SubtitleStreamIndex.Value) : null;

            var audioStream = item.GetDefaultAudioStream(options.AudioStreamIndex ?? item.DefaultAudioStreamIndex);
            if (audioStream != null)
            {
                playlistItem.AudioStreamIndex = audioStream.Index;
            }

            var videoStream = item.VideoStream;

            // TODO: This doesn't accout for situation of device being able to handle media bitrate, but wifi connection not fast enough
            var directPlayEligibilityResult = IsEligibleForDirectPlay(item, GetBitrateForDirectPlayCheck(item, options, true) ?? 0, subtitleStream, options, PlayMethod.DirectPlay);
            var directStreamEligibilityResult = IsEligibleForDirectPlay(item, options.GetMaxBitrate(false) ?? 0, subtitleStream, options, PlayMethod.DirectStream);
            bool isEligibleForDirectPlay = options.EnableDirectPlay && (options.ForceDirectPlay || directPlayEligibilityResult.Item1);
            bool isEligibleForDirectStream = options.EnableDirectStream && (options.ForceDirectStream || directStreamEligibilityResult.Item1);

            _logger.LogInformation("Profile: {0}, Path: {1}, isEligibleForDirectPlay: {2}, isEligibleForDirectStream: {3}",
                options.Profile.Name ?? "Unknown Profile",
                item.Path ?? "Unknown path",
                isEligibleForDirectPlay,
                isEligibleForDirectStream);

            var transcodeReasons = new List<TranscodeReason>();

            if (isEligibleForDirectPlay || isEligibleForDirectStream)
            {
                // See if it can be direct played
                var directPlayInfo = GetVideoDirectPlayProfile(options, item, videoStream, audioStream, isEligibleForDirectPlay, isEligibleForDirectStream);
                var directPlay = directPlayInfo.Item1;

                if (directPlay != null)
                {
                    playlistItem.PlayMethod = directPlay.Value;
                    playlistItem.Container = NormalizeMediaSourceFormatIntoSingleContainer(item.Container, item.Path, options.Profile, DlnaProfileType.Video);

                    if (subtitleStream != null)
                    {
                        var subtitleProfile = GetSubtitleProfile(item, subtitleStream, options.Profile.SubtitleProfiles, directPlay.Value, _transcoderSupport, item.Container, null);

                        playlistItem.SubtitleDeliveryMethod = subtitleProfile.Method;
                        playlistItem.SubtitleFormat = subtitleProfile.Format;
                    }

                    return playlistItem;
                }

                transcodeReasons.AddRange(directPlayInfo.Item2);
            }

            if (directPlayEligibilityResult.Item2.HasValue)
            {
                transcodeReasons.Add(directPlayEligibilityResult.Item2.Value);
            }

            if (directStreamEligibilityResult.Item2.HasValue)
            {
                transcodeReasons.Add(directStreamEligibilityResult.Item2.Value);
            }

            // Can't direct play, find the transcoding profile
            TranscodingProfile transcodingProfile = null;
            foreach (var i in options.Profile.TranscodingProfiles)
            {
                if (i.Type == playlistItem.MediaType && i.Context == options.Context)
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

                if (subtitleStream != null)
                {
                    var subtitleProfile = GetSubtitleProfile(item, subtitleStream, options.Profile.SubtitleProfiles, PlayMethod.Transcode, _transcoderSupport, transcodingProfile.Container, transcodingProfile.Protocol);

                    playlistItem.SubtitleDeliveryMethod = subtitleProfile.Method;
                    playlistItem.SubtitleFormat = subtitleProfile.Format;
                    playlistItem.SubtitleCodecs = new[] { subtitleProfile.Format };
                }

                playlistItem.PlayMethod = PlayMethod.Transcode;

                SetStreamInfoOptionsFromTranscodingProfile(playlistItem, transcodingProfile);

                var isFirstAppliedCodecProfile = true;
                foreach (var i in options.Profile.CodecProfiles)
                {
                    if (i.Type == CodecType.Video && i.ContainsAnyCodec(transcodingProfile.VideoCodec, transcodingProfile.Container))
                    {
                        bool applyConditions = true;
                        foreach (ProfileCondition applyCondition in i.ApplyConditions)
                        {
                            int? width = videoStream?.Width;
                            int? height = videoStream?.Height;
                            int? bitDepth = videoStream?.BitDepth;
                            int? videoBitrate = videoStream?.BitRate;
                            double? videoLevel = videoStream?.Level;
                            string videoProfile = videoStream?.Profile;
                            float videoFramerate = videoStream == null ? 0 : videoStream.AverageFrameRate ?? videoStream.AverageFrameRate ?? 0;
                            bool? isAnamorphic = videoStream?.IsAnamorphic;
                            bool? isInterlaced = videoStream?.IsInterlaced;
                            string videoCodecTag = videoStream?.CodecTag;
                            bool? isAvc = videoStream?.IsAVC;

                            TransportStreamTimestamp? timestamp = videoStream == null ? TransportStreamTimestamp.None : item.Timestamp;
                            int? packetLength = videoStream?.PacketLength;
                            int? refFrames = videoStream?.RefFrames;

                            int? numAudioStreams = item.GetStreamCount(MediaStreamType.Audio);
                            int? numVideoStreams = item.GetStreamCount(MediaStreamType.Video);

                            if (!ConditionProcessor.IsVideoConditionSatisfied(applyCondition, width, height, bitDepth, videoBitrate, videoProfile, videoLevel, videoFramerate, packetLength, timestamp, isAnamorphic, isInterlaced, refFrames, numVideoStreams, numAudioStreams, videoCodecTag, isAvc))
                            {
                                //LogConditionFailure(options.Profile, "VideoCodecProfile.ApplyConditions", applyCondition, item);
                                applyConditions = false;
                                break;
                            }
                        }

                        if (applyConditions)
                        {
                            var transcodingVideoCodecs = ContainerProfile.SplitValue(transcodingProfile.VideoCodec);
                            foreach (var transcodingVideoCodec in transcodingVideoCodecs)
                            {
                                if (i.ContainsAnyCodec(transcodingVideoCodec, transcodingProfile.Container))
                                {
                                    ApplyTranscodingConditions(playlistItem, i.Conditions, transcodingVideoCodec, true, isFirstAppliedCodecProfile);
                                    isFirstAppliedCodecProfile = false;
                                }
                            }
                        }
                    }
                }

                // Honor requested max channels
                playlistItem.GlobalMaxAudioChannels = options.MaxAudioChannels;

                int audioBitrate = GetAudioBitrate(playlistItem.SubProtocol, options.GetMaxBitrate(false) ?? 0, playlistItem.TargetAudioCodec, audioStream, playlistItem);
                playlistItem.AudioBitrate = Math.Min(playlistItem.AudioBitrate ?? audioBitrate, audioBitrate);

                isFirstAppliedCodecProfile = true;
                foreach (var i in options.Profile.CodecProfiles)
                {
                    if (i.Type == CodecType.VideoAudio && i.ContainsAnyCodec(transcodingProfile.AudioCodec, transcodingProfile.Container))
                    {
                        bool applyConditions = true;
                        foreach (ProfileCondition applyCondition in i.ApplyConditions)
                        {
                            bool? isSecondaryAudio = audioStream == null ? null : item.IsSecondaryAudio(audioStream);
                            int? inputAudioBitrate = audioStream == null ? null : audioStream.BitRate;
                            int? audioChannels = audioStream == null ? null : audioStream.Channels;
                            string audioProfile = audioStream == null ? null : audioStream.Profile;
                            int? inputAudioSampleRate = audioStream == null ? null : audioStream.SampleRate;
                            int? inputAudioBitDepth = audioStream == null ? null : audioStream.BitDepth;

                            if (!ConditionProcessor.IsVideoAudioConditionSatisfied(applyCondition, audioChannels, inputAudioBitrate, inputAudioSampleRate, inputAudioBitDepth, audioProfile, isSecondaryAudio))
                            {
                                //LogConditionFailure(options.Profile, "VideoCodecProfile.ApplyConditions", applyCondition, item);
                                applyConditions = false;
                                break;
                            }
                        }

                        if (applyConditions)
                        {
                            var transcodingAudioCodecs = ContainerProfile.SplitValue(transcodingProfile.AudioCodec);
                            foreach (var transcodingAudioCodec in transcodingAudioCodecs)
                            {
                                if (i.ContainsAnyCodec(transcodingAudioCodec, transcodingProfile.Container))
                                {
                                    ApplyTranscodingConditions(playlistItem, i.Conditions, transcodingAudioCodec, true, isFirstAppliedCodecProfile);
                                    isFirstAppliedCodecProfile = false;
                                }
                            }
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
                    long currentValue = playlistItem.VideoBitrate ?? availableBitrateForVideo;
                    var longBitrate = Math.Max(Math.Min(availableBitrateForVideo, currentValue), 64000);
                    playlistItem.VideoBitrate = longBitrate >= int.MaxValue ? int.MaxValue : Convert.ToInt32(longBitrate);
                }
            }

            playlistItem.TranscodeReasons = transcodeReasons.ToArray();

            return playlistItem;
        }

        private static int GetDefaultAudioBitrateIfUnknown(MediaStream audioStream)
        {
            if ((audioStream.Channels ?? 0) >= 6)
            {
                return 384000;
            }

            return 192000;
        }

        private static int GetAudioBitrate(string subProtocol, long maxTotalBitrate, string[] targetAudioCodecs, MediaStream audioStream, StreamInfo item)
        {
            string targetAudioCodec = targetAudioCodecs.Length == 0 ? null : targetAudioCodecs[0];

            int? targetAudioChannels = item.GetTargetAudioChannels(targetAudioCodec);

            int defaultBitrate;
            int encoderAudioBitrateLimit = int.MaxValue;

            if (audioStream == null)
            {
                defaultBitrate = 192000;
            }
            else
            {
                if (targetAudioChannels.HasValue && audioStream.Channels.HasValue && targetAudioChannels.Value < audioStream.Channels.Value)
                {
                    // Reduce the bitrate if we're downmixing
                    defaultBitrate = targetAudioChannels.Value < 2 ? 128000 : 192000;
                }
                else
                {
                    defaultBitrate = audioStream.BitRate ?? GetDefaultAudioBitrateIfUnknown(audioStream);
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
            else if (totalBitrate <= 2000000)
            {
                return 384000;
            }
            else if (totalBitrate <= 3000000)
            {
                return 448000;
            }

            return 640000;
        }

        private (PlayMethod?, List<TranscodeReason>) GetVideoDirectPlayProfile(
            VideoOptions options,
            MediaSourceInfo mediaSource,
            MediaStream videoStream,
            MediaStream audioStream,
            bool isEligibleForDirectPlay,
            bool isEligibleForDirectStream)
        {
            if (options.ForceDirectPlay)
            {
                return (PlayMethod.DirectPlay, new List<TranscodeReason>());
            }
            if (options.ForceDirectStream)
            {
                return (PlayMethod.DirectStream, new List<TranscodeReason>());
            }

            DeviceProfile profile = options.Profile;

            // See if it can be direct played
            DirectPlayProfile directPlay = null;
            foreach (var i in profile.DirectPlayProfiles)
            {
                if (i.Type == DlnaProfileType.Video && IsVideoDirectPlaySupported(i, mediaSource, videoStream, audioStream))
                {
                    directPlay = i;
                    break;
                }
            }

            if (directPlay == null)
            {
                _logger.LogInformation("Profile: {0}, No direct play profiles found for Path: {1}",
                    profile.Name ?? "Unknown Profile",
                    mediaSource.Path ?? "Unknown path");

                return (null, GetTranscodeReasonsFromDirectPlayProfile(mediaSource, videoStream, audioStream, profile.DirectPlayProfiles));
            }

            string container = mediaSource.Container;

            var conditions = new List<ProfileCondition>();
            foreach (var i in profile.ContainerProfiles)
            {
                if (i.Type == DlnaProfileType.Video
                    && i.ContainsContainer(container))
                {
                    foreach (var c in i.Conditions)
                    {
                        conditions.Add(c);
                    }
                }
            }

            int? width = videoStream?.Width;
            int? height = videoStream?.Height;
            int? bitDepth = videoStream?.BitDepth;
            int? videoBitrate = videoStream?.BitRate;
            double? videoLevel = videoStream?.Level;
            string videoProfile = videoStream?.Profile;
            float videoFramerate = videoStream == null ? 0 : videoStream.AverageFrameRate ?? videoStream.AverageFrameRate ?? 0;
            bool? isAnamorphic = videoStream?.IsAnamorphic;
            bool? isInterlaced = videoStream?.IsInterlaced;
            string videoCodecTag = videoStream?.CodecTag;
            bool? isAvc = videoStream?.IsAVC;

            int? audioBitrate = audioStream?.BitRate;
            int? audioChannels = audioStream?.Channels;
            string audioProfile = audioStream?.Profile;
            int? audioSampleRate = audioStream?.SampleRate;
            int? audioBitDepth = audioStream?.BitDepth;

            TransportStreamTimestamp? timestamp = videoStream == null ? TransportStreamTimestamp.None : mediaSource.Timestamp;
            int? packetLength = videoStream?.PacketLength;
            int? refFrames = videoStream?.RefFrames;

            int? numAudioStreams = mediaSource.GetStreamCount(MediaStreamType.Audio);
            int? numVideoStreams = mediaSource.GetStreamCount(MediaStreamType.Video);

            // Check container conditions
            foreach (ProfileCondition i in conditions)
            {
                if (!ConditionProcessor.IsVideoConditionSatisfied(i, width, height, bitDepth, videoBitrate, videoProfile, videoLevel, videoFramerate, packetLength, timestamp, isAnamorphic, isInterlaced, refFrames, numVideoStreams, numAudioStreams, videoCodecTag, isAvc))
                {
                    LogConditionFailure(profile, "VideoContainerProfile", i, mediaSource);

                    var transcodeReason = GetTranscodeReasonForFailedCondition(i);
                    var transcodeReasons = transcodeReason.HasValue
                        ? new List<TranscodeReason> { transcodeReason.Value }
                        : new List<TranscodeReason>();

                    return (null, transcodeReasons);
                }
            }

            string videoCodec = videoStream?.Codec;

            conditions = new List<ProfileCondition>();
            foreach (var i in profile.CodecProfiles)
            {
                if (i.Type == CodecType.Video && i.ContainsAnyCodec(videoCodec, container))
                {
                    bool applyConditions = true;
                    foreach (ProfileCondition applyCondition in i.ApplyConditions)
                    {
                        if (!ConditionProcessor.IsVideoConditionSatisfied(applyCondition, width, height, bitDepth, videoBitrate, videoProfile, videoLevel, videoFramerate, packetLength, timestamp, isAnamorphic, isInterlaced, refFrames, numVideoStreams, numAudioStreams, videoCodecTag, isAvc))
                        {
                            //LogConditionFailure(profile, "VideoCodecProfile.ApplyConditions", applyCondition, mediaSource);
                            applyConditions = false;
                            break;
                        }
                    }

                    if (applyConditions)
                    {
                        foreach (ProfileCondition c in i.Conditions)
                        {
                            conditions.Add(c);
                        }
                    }
                }
            }

            foreach (ProfileCondition i in conditions)
            {
                if (!ConditionProcessor.IsVideoConditionSatisfied(i, width, height, bitDepth, videoBitrate, videoProfile, videoLevel, videoFramerate, packetLength, timestamp, isAnamorphic, isInterlaced, refFrames, numVideoStreams, numAudioStreams, videoCodecTag, isAvc))
                {
                    LogConditionFailure(profile, "VideoCodecProfile", i, mediaSource);

                    var transcodeReason = GetTranscodeReasonForFailedCondition(i);
                    var transcodeReasons = transcodeReason.HasValue
                        ? new List<TranscodeReason> { transcodeReason.Value }
                        : new List<TranscodeReason>();

                    return (null, transcodeReasons);
                }
            }

            if (audioStream != null)
            {
                string audioCodec = audioStream.Codec;
                conditions = new List<ProfileCondition>();
                bool? isSecondaryAudio = audioStream == null ? null : mediaSource.IsSecondaryAudio(audioStream);

                foreach (var i in profile.CodecProfiles)
                {
                    if (i.Type == CodecType.VideoAudio && i.ContainsAnyCodec(audioCodec, container))
                    {
                        bool applyConditions = true;
                        foreach (ProfileCondition applyCondition in i.ApplyConditions)
                        {
                            if (!ConditionProcessor.IsVideoAudioConditionSatisfied(applyCondition, audioChannels, audioBitrate, audioSampleRate, audioBitDepth, audioProfile, isSecondaryAudio))
                            {
                                //LogConditionFailure(profile, "VideoAudioCodecProfile.ApplyConditions", applyCondition, mediaSource);
                                applyConditions = false;
                                break;
                            }
                        }

                        if (applyConditions)
                        {
                            foreach (ProfileCondition c in i.Conditions)
                            {
                                conditions.Add(c);
                            }
                        }
                    }
                }

                foreach (ProfileCondition i in conditions)
                {
                    if (!ConditionProcessor.IsVideoAudioConditionSatisfied(i, audioChannels, audioBitrate, audioSampleRate, audioBitDepth, audioProfile, isSecondaryAudio))
                    {
                        LogConditionFailure(profile, "VideoAudioCodecProfile", i, mediaSource);

                        var transcodeReason = GetTranscodeReasonForFailedCondition(i);
                        var transcodeReasons = transcodeReason.HasValue
                            ? new List<TranscodeReason> { transcodeReason.Value }
                            : new List<TranscodeReason>();

                        return (null, transcodeReasons);
                    }
                }
            }

            if (isEligibleForDirectStream && mediaSource.SupportsDirectStream)
            {
                return (PlayMethod.DirectStream, new List<TranscodeReason>());
            }

            return (null, new List<TranscodeReason> { TranscodeReason.ContainerBitrateExceedsLimit });
        }

        private void LogConditionFailure(DeviceProfile profile, string type, ProfileCondition condition, MediaSourceInfo mediaSource)
        {
            _logger.LogInformation("Profile: {0}, DirectPlay=false. Reason={1}.{2} Condition: {3}. ConditionValue: {4}. IsRequired: {5}. Path: {6}",
                type,
                profile.Name ?? "Unknown Profile",
                condition.Property,
                condition.Condition,
                condition.Value ?? string.Empty,
                condition.IsRequired,
                mediaSource.Path ?? "Unknown path");
        }

        private (bool directPlay, TranscodeReason? reason) IsEligibleForDirectPlay(
            MediaSourceInfo item,
            long maxBitrate,
            MediaStream subtitleStream,
            VideoOptions options,
            PlayMethod playMethod)
        {
            if (subtitleStream != null)
            {
                var subtitleProfile = GetSubtitleProfile(item, subtitleStream, options.Profile.SubtitleProfiles, playMethod, _transcoderSupport, item.Container, null);

                if (subtitleProfile.Method != SubtitleDeliveryMethod.External && subtitleProfile.Method != SubtitleDeliveryMethod.Embed)
                {
                    _logger.LogInformation("Not eligible for {0} due to unsupported subtitles", playMethod);
                    return (false, TranscodeReason.SubtitleCodecNotSupported);
                }
            }

            bool result = IsAudioEligibleForDirectPlay(item, maxBitrate, playMethod);

            return (result, result ? (TranscodeReason?)null : TranscodeReason.ContainerBitrateExceedsLimit);
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

                    if (playMethod == PlayMethod.Transcode && !IsSubtitleEmbedSupported(subtitleStream, profile, transcodingSubProtocol, outputContainer))
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

                    if (playMethod == PlayMethod.Transcode && !IsSubtitleEmbedSupported(subtitleStream, profile, transcodingSubProtocol, outputContainer))
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

        private static bool IsSubtitleEmbedSupported(MediaStream subtitleStream, SubtitleProfile subtitleProfile, string transcodingSubProtocol, string transcodingContainer)
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

        private bool IsAudioEligibleForDirectPlay(MediaSourceInfo item, long maxBitrate, PlayMethod playMethod)
        {
            // Don't restrict by bitrate if coming from an external domain
            if (item.IsRemote)
            {
                return true;
            }

            long requestedMaxBitrate = maxBitrate > 0 ? maxBitrate : 1000000;

            // If we don't know the bitrate, then force a transcode if requested max bitrate is under 40 mbps
            int itemBitrate = item.Bitrate ?? 40000000;

            if (itemBitrate > requestedMaxBitrate)
            {
                _logger.LogInformation("Bitrate exceeds {PlayBackMethod} limit: media bitrate: {MediaBitrate}, max bitrate: {MaxBitrate}",
                    playMethod, itemBitrate, requestedMaxBitrate);
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

                            if (!string.IsNullOrEmpty(value))
                            {
                                // change from split by | to comma

                                // strip spaces to avoid having to encode
                                var values = value
                                    .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                                if (condition.Condition == ProfileConditionType.Equals || condition.Condition == ProfileConditionType.EqualsAny)
                                {
                                    item.SetOption(qualifier, "profile", string.Join(",", values));
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

        private bool IsVideoDirectPlaySupported(DirectPlayProfile profile, MediaSourceInfo item, MediaStream videoStream, MediaStream audioStream)
        {
            // Check container type
            if (!profile.SupportsContainer(item.Container))
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
