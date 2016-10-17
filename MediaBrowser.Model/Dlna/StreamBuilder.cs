using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Generic;

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

        public StreamBuilder(ILogger logger)
            : this(new FullTranscoderSupport(), logger)
        {
        }

        public StreamInfo BuildAudioItem(AudioOptions options)
        {
            ValidateAudioInput(options);

            List<MediaSourceInfo> mediaSources = new List<MediaSourceInfo>();
            foreach (MediaSourceInfo i in options.MediaSources)
            {
                if (string.IsNullOrEmpty(options.MediaSourceId) ||
                    StringHelper.EqualsIgnoreCase(i.Id, options.MediaSourceId))
                {
                    mediaSources.Add(i);
                }
            }

            List<StreamInfo> streams = new List<StreamInfo>();
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

            return GetOptimalStream(streams, options.GetMaxBitrate(true));
        }

        public StreamInfo BuildVideoItem(VideoOptions options)
        {
            ValidateInput(options);

            List<MediaSourceInfo> mediaSources = new List<MediaSourceInfo>();
            foreach (MediaSourceInfo i in options.MediaSources)
            {
                if (string.IsNullOrEmpty(options.MediaSourceId) ||
                    StringHelper.EqualsIgnoreCase(i.Id, options.MediaSourceId))
                {
                    mediaSources.Add(i);
                }
            }

            List<StreamInfo> streams = new List<StreamInfo>();
            foreach (MediaSourceInfo i in mediaSources)
            {
                StreamInfo streamInfo = BuildVideoItem(i, options);
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

            return GetOptimalStream(streams, options.GetMaxBitrate(false));
        }

        private StreamInfo GetOptimalStream(List<StreamInfo> streams, int? maxBitrate)
        {
            streams = StreamInfoSorter.SortMediaSources(streams, maxBitrate);

            foreach (StreamInfo stream in streams)
            {
                return stream;
            }

            return null;
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
                playlistItem.Container = item.Container;
                return playlistItem;
            }

            if (options.ForceDirectStream)
            {
                playlistItem.PlayMethod = PlayMethod.DirectStream;
                playlistItem.Container = item.Container;
                return playlistItem;
            }

            MediaStream audioStream = item.GetDefaultAudioStream(null);

            List<PlayMethod> directPlayMethods = GetAudioDirectPlayMethods(item, audioStream, options);

            ConditionProcessor conditionProcessor = new ConditionProcessor();

            int? inputAudioChannels = audioStream == null ? null : audioStream.Channels;
            int? inputAudioBitrate = audioStream == null ? null : audioStream.BitDepth;

            if (directPlayMethods.Count > 0)
            {
                string audioCodec = audioStream == null ? null : audioStream.Codec;

                // Make sure audio codec profiles are satisfied
                if (!string.IsNullOrEmpty(audioCodec))
                {
                    List<ProfileCondition> conditions = new List<ProfileCondition>();
                    foreach (CodecProfile i in options.Profile.CodecProfiles)
                    {
                        if (i.Type == CodecType.Audio && i.ContainsCodec(audioCodec, item.Container))
                        {
                            bool applyConditions = true;
                            foreach (ProfileCondition applyCondition in i.ApplyConditions)
                            {
                                if (!conditionProcessor.IsAudioConditionSatisfied(applyCondition, inputAudioChannels, inputAudioBitrate))
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
                                    conditions.Add(c);
                                }
                            }
                        }
                    }

                    bool all = true;
                    foreach (ProfileCondition c in conditions)
                    {
                        if (!conditionProcessor.IsAudioConditionSatisfied(c, inputAudioChannels, inputAudioBitrate))
                        {
                            LogConditionFailure(options.Profile, "AudioCodecProfile", c, item);
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

                        playlistItem.Container = item.Container;

                        return playlistItem;
                    }
                }
            }

            TranscodingProfile transcodingProfile = null;
            foreach (TranscodingProfile i in options.Profile.TranscodingProfiles)
            {
                if (i.Type == playlistItem.MediaType && i.Context == options.Context)
                {
                    if (_transcoderSupport.CanEncodeToAudioCodec(i.AudioCodec ?? i.Container))
                    {
                        transcodingProfile = i;
                        break;
                    }
                }
            }

            if (transcodingProfile != null)
            {
                if (!item.SupportsTranscoding)
                {
                    return null;
                }

                playlistItem.PlayMethod = PlayMethod.Transcode;
                playlistItem.TranscodeSeekInfo = transcodingProfile.TranscodeSeekInfo;
                playlistItem.EstimateContentLength = transcodingProfile.EstimateContentLength;
                playlistItem.Container = transcodingProfile.Container;

                if (string.IsNullOrEmpty(transcodingProfile.AudioCodec))
                {
                    playlistItem.AudioCodecs = new string[] { };
                }
                else
                {
                    playlistItem.AudioCodecs = transcodingProfile.AudioCodec.Split(',');
                }
                playlistItem.SubProtocol = transcodingProfile.Protocol;

                List<CodecProfile> audioCodecProfiles = new List<CodecProfile>();
                foreach (CodecProfile i in options.Profile.CodecProfiles)
                {
                    if (i.Type == CodecType.Audio && i.ContainsCodec(transcodingProfile.AudioCodec, transcodingProfile.Container))
                    {
                        audioCodecProfiles.Add(i);
                    }

                    if (audioCodecProfiles.Count >= 1) break;
                }

                List<ProfileCondition> audioTranscodingConditions = new List<ProfileCondition>();
                foreach (CodecProfile i in audioCodecProfiles)
                {
                    bool applyConditions = true;
                    foreach (ProfileCondition applyCondition in i.ApplyConditions)
                    {
                        if (!conditionProcessor.IsAudioConditionSatisfied(applyCondition, inputAudioChannels, inputAudioBitrate))
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

                ApplyTranscodingConditions(playlistItem, audioTranscodingConditions);

                // Honor requested max channels
                if (options.MaxAudioChannels.HasValue)
                {
                    int currentValue = playlistItem.MaxAudioChannels ?? options.MaxAudioChannels.Value;

                    playlistItem.MaxAudioChannels = Math.Min(options.MaxAudioChannels.Value, currentValue);
                }

                int transcodingBitrate = options.AudioTranscodingBitrate ??
                    options.Profile.MusicStreamingTranscodingBitrate ??
                    128000;

                int? configuredBitrate = options.GetMaxBitrate(true);

                if (configuredBitrate.HasValue)
                {
                    transcodingBitrate = Math.Min(configuredBitrate.Value, transcodingBitrate);
                }

                playlistItem.AudioBitrate = Math.Min(transcodingBitrate, playlistItem.AudioBitrate ?? transcodingBitrate);

            }

            return playlistItem;
        }

        private int? GetBitrateForDirectPlayCheck(MediaSourceInfo item, AudioOptions options, bool isAudio)
        {
            if (item.Protocol == MediaProtocol.File)
            {
                return options.Profile.MaxStaticBitrate;
            }

            return options.GetMaxBitrate(isAudio);
        }

        private List<PlayMethod> GetAudioDirectPlayMethods(MediaSourceInfo item, MediaStream audioStream, AudioOptions options)
        {
            DirectPlayProfile directPlayProfile = null;
            foreach (DirectPlayProfile i in options.Profile.DirectPlayProfiles)
            {
                if (i.Type == DlnaProfileType.Audio && IsAudioDirectPlaySupported(i, item, audioStream))
                {
                    directPlayProfile = i;
                    break;
                }
            }

            List<PlayMethod> playMethods = new List<PlayMethod>();

            if (directPlayProfile != null)
            {
                // While options takes the network and other factors into account. Only applies to direct stream
                if (item.SupportsDirectStream && IsAudioEligibleForDirectPlay(item, options.GetMaxBitrate(true)) && options.EnableDirectStream)
                {
                    playMethods.Add(PlayMethod.DirectStream);
                }

                // The profile describes what the device supports
                // If device requirements are satisfied then allow both direct stream and direct play
                if (item.SupportsDirectPlay &&
                    IsAudioEligibleForDirectPlay(item, GetBitrateForDirectPlayCheck(item, options, true)) && options.EnableDirectPlay)
                {
                    playMethods.Add(PlayMethod.DirectPlay);
                }
            }
            else
            {
                _logger.Info("Profile: {0}, No direct play profiles found for Path: {1}",
                    options.Profile.Name ?? "Unknown Profile",
                    item.Path ?? "Unknown path");
            }

            return playMethods;
        }

        private int? GetDefaultSubtitleStreamIndex(MediaSourceInfo item, SubtitleProfile[] subtitleProfiles)
        {
            int highestScore = -1;

            foreach (MediaStream stream in item.MediaStreams)
            {
                if (stream.Type == MediaStreamType.Subtitle && stream.Score.HasValue)
                {
                    if (stream.Score.Value > highestScore)
                    {
                        highestScore = stream.Score.Value;
                    }
                }
            }

            List<MediaStream> topStreams = new List<MediaStream>();
            foreach (MediaStream stream in item.MediaStreams)
            {
                if (stream.Type == MediaStreamType.Subtitle && stream.Score.HasValue && stream.Score.Value == highestScore)
                {
                    topStreams.Add(stream);
                }
            }

            // If multiple streams have an equal score, try to pick the most efficient one
            if (topStreams.Count > 1)
            {
                foreach (MediaStream stream in topStreams)
                {
                    foreach (SubtitleProfile profile in subtitleProfiles)
                    {
                        if (profile.Method == SubtitleDeliveryMethod.External && StringHelper.EqualsIgnoreCase(profile.Format, stream.Codec))
                        {
                            return stream.Index;
                        }
                    }
                }
            }

            // If no optimization panned out, just use the original default
            return item.DefaultSubtitleStreamIndex;
        }

        private StreamInfo BuildVideoItem(MediaSourceInfo item, VideoOptions options)
        {
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
            MediaStream subtitleStream = playlistItem.SubtitleStreamIndex.HasValue ? item.GetMediaStream(MediaStreamType.Subtitle, playlistItem.SubtitleStreamIndex.Value) : null;

            MediaStream audioStream = item.GetDefaultAudioStream(options.AudioStreamIndex ?? item.DefaultAudioStreamIndex);
            int? audioStreamIndex = null;
            if (audioStream != null)
            {
                audioStreamIndex = audioStream.Index;
            }

            MediaStream videoStream = item.VideoStream;

            // TODO: This doesn't accout for situation of device being able to handle media bitrate, but wifi connection not fast enough
            bool isEligibleForDirectPlay = options.EnableDirectPlay && (options.ForceDirectPlay || IsEligibleForDirectPlay(item, GetBitrateForDirectPlayCheck(item, options, true), subtitleStream, options, PlayMethod.DirectPlay));
            bool isEligibleForDirectStream = options.EnableDirectStream && (options.ForceDirectStream || IsEligibleForDirectPlay(item, options.GetMaxBitrate(false), subtitleStream, options, PlayMethod.DirectStream));

            _logger.Info("Profile: {0}, Path: {1}, isEligibleForDirectPlay: {2}, isEligibleForDirectStream: {3}",
                options.Profile.Name ?? "Unknown Profile",
                item.Path ?? "Unknown path",
                isEligibleForDirectPlay,
                isEligibleForDirectStream);

            if (isEligibleForDirectPlay || isEligibleForDirectStream)
            {
                // See if it can be direct played
                PlayMethod? directPlay = GetVideoDirectPlayProfile(options, item, videoStream, audioStream, isEligibleForDirectPlay, isEligibleForDirectStream);

                if (directPlay != null)
                {
                    playlistItem.PlayMethod = directPlay.Value;
                    playlistItem.Container = item.Container;

                    if (subtitleStream != null)
                    {
                        SubtitleProfile subtitleProfile = GetSubtitleProfile(subtitleStream, options.Profile.SubtitleProfiles, directPlay.Value);

                        playlistItem.SubtitleDeliveryMethod = subtitleProfile.Method;
                        playlistItem.SubtitleFormat = subtitleProfile.Format;
                    }

                    return playlistItem;
                }
            }

            // Can't direct play, find the transcoding profile
            TranscodingProfile transcodingProfile = null;
            foreach (TranscodingProfile i in options.Profile.TranscodingProfiles)
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
                    SubtitleProfile subtitleProfile = GetSubtitleProfile(subtitleStream, options.Profile.SubtitleProfiles, PlayMethod.Transcode);

                    playlistItem.SubtitleDeliveryMethod = subtitleProfile.Method;
                    playlistItem.SubtitleFormat = subtitleProfile.Format;
                }

                playlistItem.PlayMethod = PlayMethod.Transcode;
                playlistItem.Container = transcodingProfile.Container;
                playlistItem.EstimateContentLength = transcodingProfile.EstimateContentLength;
                playlistItem.TranscodeSeekInfo = transcodingProfile.TranscodeSeekInfo;

                playlistItem.AudioCodecs = transcodingProfile.AudioCodec.Split(',');

                playlistItem.VideoCodec = transcodingProfile.VideoCodec;
                playlistItem.CopyTimestamps = transcodingProfile.CopyTimestamps;
                playlistItem.EnableSubtitlesInManifest = transcodingProfile.EnableSubtitlesInManifest;
                playlistItem.EnableSplittingOnNonKeyFrames = transcodingProfile.EnableSplittingOnNonKeyFrames;

                if (!string.IsNullOrEmpty(transcodingProfile.MaxAudioChannels))
                {
                    int transcodingMaxAudioChannels;
                    if (IntHelper.TryParseCultureInvariant(transcodingProfile.MaxAudioChannels, out transcodingMaxAudioChannels))
                    {
                        playlistItem.TranscodingMaxAudioChannels = transcodingMaxAudioChannels;
                    }
                }
                playlistItem.SubProtocol = transcodingProfile.Protocol;
                playlistItem.AudioStreamIndex = audioStreamIndex;
                ConditionProcessor conditionProcessor = new ConditionProcessor();

                List<ProfileCondition> videoTranscodingConditions = new List<ProfileCondition>();
                foreach (CodecProfile i in options.Profile.CodecProfiles)
                {
                    if (i.Type == CodecType.Video && i.ContainsCodec(transcodingProfile.VideoCodec, transcodingProfile.Container))
                    {
                        bool applyConditions = true;
                        foreach (ProfileCondition applyCondition in i.ApplyConditions)
                        {
                            bool? isSecondaryAudio = audioStream == null ? null : item.IsSecondaryAudio(audioStream);
                            int? inputAudioBitrate = audioStream == null ? null : audioStream.BitRate;
                            int? audioChannels = audioStream == null ? null : audioStream.Channels;
                            string audioProfile = audioStream == null ? null : audioStream.Profile;

                            if (!conditionProcessor.IsVideoAudioConditionSatisfied(applyCondition, audioChannels, inputAudioBitrate, audioProfile, isSecondaryAudio))
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
                                videoTranscodingConditions.Add(c);
                            }
                            break;
                        }
                    }
                }
                ApplyTranscodingConditions(playlistItem, videoTranscodingConditions);

                List<ProfileCondition> audioTranscodingConditions = new List<ProfileCondition>();
                foreach (CodecProfile i in options.Profile.CodecProfiles)
                {
                    if (i.Type == CodecType.VideoAudio && i.ContainsCodec(playlistItem.TargetAudioCodec, transcodingProfile.Container))
                    {
                        bool applyConditions = true;
                        foreach (ProfileCondition applyCondition in i.ApplyConditions)
                        {
                            int? width = videoStream == null ? null : videoStream.Width;
                            int? height = videoStream == null ? null : videoStream.Height;
                            int? bitDepth = videoStream == null ? null : videoStream.BitDepth;
                            int? videoBitrate = videoStream == null ? null : videoStream.BitRate;
                            double? videoLevel = videoStream == null ? null : videoStream.Level;
                            string videoProfile = videoStream == null ? null : videoStream.Profile;
                            float? videoFramerate = videoStream == null ? null : videoStream.AverageFrameRate ?? videoStream.AverageFrameRate;
                            bool? isAnamorphic = videoStream == null ? null : videoStream.IsAnamorphic;
                            string videoCodecTag = videoStream == null ? null : videoStream.CodecTag;
                            bool? isAvc = videoStream == null ? null : videoStream.IsAVC;

                            TransportStreamTimestamp? timestamp = videoStream == null ? TransportStreamTimestamp.None : item.Timestamp;
                            int? packetLength = videoStream == null ? null : videoStream.PacketLength;
                            int? refFrames = videoStream == null ? null : videoStream.RefFrames;

                            int? numAudioStreams = item.GetStreamCount(MediaStreamType.Audio);
                            int? numVideoStreams = item.GetStreamCount(MediaStreamType.Video);

                            if (!conditionProcessor.IsVideoConditionSatisfied(applyCondition, width, height, bitDepth, videoBitrate, videoProfile, videoLevel, videoFramerate, packetLength, timestamp, isAnamorphic, refFrames, numVideoStreams, numAudioStreams, videoCodecTag, isAvc))
                            {
                                LogConditionFailure(options.Profile, "VideoCodecProfile", applyCondition, item);
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
                            break;
                        }
                    }
                }
                ApplyTranscodingConditions(playlistItem, audioTranscodingConditions);

                // Honor requested max channels
                if (options.MaxAudioChannels.HasValue)
                {
                    int currentValue = playlistItem.MaxAudioChannels ?? options.MaxAudioChannels.Value;

                    playlistItem.MaxAudioChannels = Math.Min(options.MaxAudioChannels.Value, currentValue);
                }

                int audioBitrate = GetAudioBitrate(playlistItem.SubProtocol, options.GetMaxBitrate(false), playlistItem.TargetAudioChannels, playlistItem.TargetAudioCodec, audioStream);
                playlistItem.AudioBitrate = Math.Min(playlistItem.AudioBitrate ?? audioBitrate, audioBitrate);

                int? maxBitrateSetting = options.GetMaxBitrate(false);
                // Honor max rate
                if (maxBitrateSetting.HasValue)
                {
                    int videoBitrate = maxBitrateSetting.Value;

                    if (playlistItem.AudioBitrate.HasValue)
                    {
                        videoBitrate -= playlistItem.AudioBitrate.Value;
                    }

                    // Make sure the video bitrate is lower than bitrate settings but at least 64k
                    int currentValue = playlistItem.VideoBitrate ?? videoBitrate;
                    playlistItem.VideoBitrate = Math.Max(Math.Min(videoBitrate, currentValue), 64000);
                }
            }

            return playlistItem;
        }

        private int GetAudioBitrate(string subProtocol, int? maxTotalBitrate, int? targetAudioChannels, string targetAudioCodec, MediaStream audioStream)
        {
            int defaultBitrate = audioStream == null ? 192000 : audioStream.BitRate ?? 192000;
            // Reduce the bitrate if we're downmixing
            if (targetAudioChannels.HasValue && audioStream != null && audioStream.Channels.HasValue && targetAudioChannels.Value < audioStream.Channels.Value)
            {
                defaultBitrate = StringHelper.EqualsIgnoreCase(targetAudioCodec, "ac3") ? 192000 : 128000;
            }

            if (StringHelper.EqualsIgnoreCase(subProtocol, "hls"))
            {
                defaultBitrate = Math.Min(384000, defaultBitrate);
            }
            else
            {
                defaultBitrate = Math.Min(448000, defaultBitrate);
            }

            int encoderAudioBitrateLimit = int.MaxValue;

            if (audioStream != null)
            {
                // Seeing webm encoding failures when source has 1 audio channel and 22k bitrate. 
                // Any attempts to transcode over 64k will fail
                if (audioStream.Channels.HasValue &&
                    audioStream.Channels.Value == 1)
                {
                    if ((audioStream.BitRate ?? 0) < 64000)
                    {
                        encoderAudioBitrateLimit = 64000;
                    }
                }
            }

            if (maxTotalBitrate.HasValue)
            {
                if (maxTotalBitrate.Value < 640000)
                {
                    defaultBitrate = Math.Min(128000, defaultBitrate);
                }
            }

            return Math.Min(defaultBitrate, encoderAudioBitrateLimit);
        }

        private PlayMethod? GetVideoDirectPlayProfile(VideoOptions options,
            MediaSourceInfo mediaSource,
            MediaStream videoStream,
            MediaStream audioStream,
            bool isEligibleForDirectPlay,
            bool isEligibleForDirectStream)
        {
            DeviceProfile profile = options.Profile;

            if (options.ForceDirectPlay)
            {
                return PlayMethod.DirectPlay;
            }
            if (options.ForceDirectStream)
            {
                return PlayMethod.DirectStream;
            }

            if (videoStream == null)
            {
                _logger.Info("Profile: {0}, Cannot direct stream with no known video stream. Path: {1}",
                    profile.Name ?? "Unknown Profile",
                    mediaSource.Path ?? "Unknown path");

                return null;
            }

            // See if it can be direct played
            DirectPlayProfile directPlay = null;
            foreach (DirectPlayProfile i in profile.DirectPlayProfiles)
            {
                if (i.Type == DlnaProfileType.Video && IsVideoDirectPlaySupported(i, mediaSource, videoStream, audioStream))
                {
                    directPlay = i;
                    break;
                }
            }

            if (directPlay == null)
            {
                _logger.Info("Profile: {0}, No direct play profiles found for Path: {1}",
                    profile.Name ?? "Unknown Profile",
                    mediaSource.Path ?? "Unknown path");

                return null;
            }

            string container = mediaSource.Container;

            List<ProfileCondition> conditions = new List<ProfileCondition>();
            foreach (ContainerProfile i in profile.ContainerProfiles)
            {
                if (i.Type == DlnaProfileType.Video &&
                    ListHelper.ContainsIgnoreCase(i.GetContainers(), container))
                {
                    foreach (ProfileCondition c in i.Conditions)
                    {
                        conditions.Add(c);
                    }
                }
            }

            ConditionProcessor conditionProcessor = new ConditionProcessor();

            int? width = videoStream == null ? null : videoStream.Width;
            int? height = videoStream == null ? null : videoStream.Height;
            int? bitDepth = videoStream == null ? null : videoStream.BitDepth;
            int? videoBitrate = videoStream == null ? null : videoStream.BitRate;
            double? videoLevel = videoStream == null ? null : videoStream.Level;
            string videoProfile = videoStream == null ? null : videoStream.Profile;
            float? videoFramerate = videoStream == null ? null : videoStream.AverageFrameRate ?? videoStream.AverageFrameRate;
            bool? isAnamorphic = videoStream == null ? null : videoStream.IsAnamorphic;
            string videoCodecTag = videoStream == null ? null : videoStream.CodecTag;
            bool? isAvc = videoStream == null ? null : videoStream.IsAVC;

            int? audioBitrate = audioStream == null ? null : audioStream.BitRate;
            int? audioChannels = audioStream == null ? null : audioStream.Channels;
            string audioProfile = audioStream == null ? null : audioStream.Profile;

            TransportStreamTimestamp? timestamp = videoStream == null ? TransportStreamTimestamp.None : mediaSource.Timestamp;
            int? packetLength = videoStream == null ? null : videoStream.PacketLength;
            int? refFrames = videoStream == null ? null : videoStream.RefFrames;

            int? numAudioStreams = mediaSource.GetStreamCount(MediaStreamType.Audio);
            int? numVideoStreams = mediaSource.GetStreamCount(MediaStreamType.Video);

            // Check container conditions
            foreach (ProfileCondition i in conditions)
            {
                if (!conditionProcessor.IsVideoConditionSatisfied(i, width, height, bitDepth, videoBitrate, videoProfile, videoLevel, videoFramerate, packetLength, timestamp, isAnamorphic, refFrames, numVideoStreams, numAudioStreams, videoCodecTag, isAvc))
                {
                    LogConditionFailure(profile, "VideoContainerProfile", i, mediaSource);

                    return null;
                }
            }

            string videoCodec = videoStream == null ? null : videoStream.Codec;

            if (string.IsNullOrEmpty(videoCodec))
            {
                _logger.Info("Profile: {0}, DirectPlay=false. Reason=Unknown video codec. Path: {1}",
                    profile.Name ?? "Unknown Profile",
                    mediaSource.Path ?? "Unknown path");

                return null;
            }

            conditions = new List<ProfileCondition>();
            foreach (CodecProfile i in profile.CodecProfiles)
            {
                if (i.Type == CodecType.Video && i.ContainsCodec(videoCodec, container))
                {
                    bool applyConditions = true;
                    foreach (ProfileCondition applyCondition in i.ApplyConditions)
                    {
                        if (!conditionProcessor.IsVideoConditionSatisfied(applyCondition, width, height, bitDepth, videoBitrate, videoProfile, videoLevel, videoFramerate, packetLength, timestamp, isAnamorphic, refFrames, numVideoStreams, numAudioStreams, videoCodecTag, isAvc))
                        {
                            LogConditionFailure(profile, "VideoCodecProfile", applyCondition, mediaSource);
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
                if (!conditionProcessor.IsVideoConditionSatisfied(i, width, height, bitDepth, videoBitrate, videoProfile, videoLevel, videoFramerate, packetLength, timestamp, isAnamorphic, refFrames, numVideoStreams, numAudioStreams, videoCodecTag, isAvc))
                {
                    LogConditionFailure(profile, "VideoCodecProfile", i, mediaSource);

                    return null;
                }
            }

            if (audioStream != null)
            {
                string audioCodec = audioStream.Codec;

                if (string.IsNullOrEmpty(audioCodec))
                {
                    _logger.Info("Profile: {0}, DirectPlay=false. Reason=Unknown audio codec. Path: {1}",
                        profile.Name ?? "Unknown Profile",
                        mediaSource.Path ?? "Unknown path");

                    return null;
                }

                conditions = new List<ProfileCondition>();
                bool? isSecondaryAudio = audioStream == null ? null : mediaSource.IsSecondaryAudio(audioStream);

                foreach (CodecProfile i in profile.CodecProfiles)
                {
                    if (i.Type == CodecType.VideoAudio && i.ContainsCodec(audioCodec, container))
                    {
                        bool applyConditions = true;
                        foreach (ProfileCondition applyCondition in i.ApplyConditions)
                        {
                            if (!conditionProcessor.IsVideoAudioConditionSatisfied(applyCondition, audioChannels, audioBitrate, audioProfile, isSecondaryAudio))
                            {
                                LogConditionFailure(profile, "VideoAudioCodecProfile", applyCondition, mediaSource);
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
                    if (!conditionProcessor.IsVideoAudioConditionSatisfied(i, audioChannels, audioBitrate, audioProfile, isSecondaryAudio))
                    {
                        LogConditionFailure(profile, "VideoAudioCodecProfile", i, mediaSource);

                        return null;
                    }
                }
            }

            if (isEligibleForDirectStream && mediaSource.SupportsDirectStream)
            {
                return PlayMethod.DirectStream;
            }

            return null;
        }

        private void LogConditionFailure(DeviceProfile profile, string type, ProfileCondition condition, MediaSourceInfo mediaSource)
        {
            _logger.Info("Profile: {0}, DirectPlay=false. Reason={1}.{2} Condition: {3}. ConditionValue: {4}. IsRequired: {5}. Path: {6}",
                type,
                profile.Name ?? "Unknown Profile",
                condition.Property,
                condition.Condition,
                condition.Value ?? string.Empty,
                condition.IsRequired,
                mediaSource.Path ?? "Unknown path");
        }

        private bool IsEligibleForDirectPlay(MediaSourceInfo item,
            int? maxBitrate,
            MediaStream subtitleStream,
            VideoOptions options,
            PlayMethod playMethod)
        {
            if (subtitleStream != null)
            {
                SubtitleProfile subtitleProfile = GetSubtitleProfile(subtitleStream, options.Profile.SubtitleProfiles, playMethod);

                if (subtitleProfile.Method != SubtitleDeliveryMethod.External && subtitleProfile.Method != SubtitleDeliveryMethod.Embed)
                {
                    _logger.Info("Not eligible for {0} due to unsupported subtitles", playMethod);
                    return false;
                }
            }

            return IsAudioEligibleForDirectPlay(item, maxBitrate);
        }

        public static SubtitleProfile GetSubtitleProfile(MediaStream subtitleStream, SubtitleProfile[] subtitleProfiles, PlayMethod playMethod)
        {
            if (playMethod != PlayMethod.Transcode && !subtitleStream.IsExternal)
            {
                // Look for supported embedded subs
                foreach (SubtitleProfile profile in subtitleProfiles)
                {
                    if (!profile.SupportsLanguage(subtitleStream.Language))
                    {
                        continue;
                    }

                    if (profile.Method != SubtitleDeliveryMethod.Embed)
                    {
                        continue;
                    }

                    if (subtitleStream.IsTextSubtitleStream == MediaStream.IsTextFormat(profile.Format) && StringHelper.EqualsIgnoreCase(profile.Format, subtitleStream.Codec))
                    {
                        return profile;
                    }
                }
            }

            // Look for an external or hls profile that matches the stream type (text/graphical) and doesn't require conversion
            return GetExternalSubtitleProfile(subtitleStream, subtitleProfiles, playMethod, false) ?? GetExternalSubtitleProfile(subtitleStream, subtitleProfiles, playMethod, true) ?? new SubtitleProfile
            {
                Method = SubtitleDeliveryMethod.Encode,
                Format = subtitleStream.Codec
            };
        }

        private static SubtitleProfile GetExternalSubtitleProfile(MediaStream subtitleStream, SubtitleProfile[] subtitleProfiles, PlayMethod playMethod, bool allowConversion)
        {
            foreach (SubtitleProfile profile in subtitleProfiles)
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

                if ((profile.Method == SubtitleDeliveryMethod.External && subtitleStream.IsTextSubtitleStream == MediaStream.IsTextFormat(profile.Format)) ||
                    (profile.Method == SubtitleDeliveryMethod.Hls && subtitleStream.IsTextSubtitleStream))
                {
                    bool requiresConversion = !StringHelper.EqualsIgnoreCase(subtitleStream.Codec, profile.Format);

                    if (!requiresConversion)
                    {
                        return profile;
                    }

                    if (!allowConversion)
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

        private bool IsAudioEligibleForDirectPlay(MediaSourceInfo item, int? maxBitrate)
        {
            if (!maxBitrate.HasValue)
            {
                _logger.Info("Cannot direct play due to unknown supported bitrate");
                return false;
            }

            if (!item.Bitrate.HasValue)
            {
                _logger.Info("Cannot direct play due to unknown content bitrate");
                return false;
            }

            if (item.Bitrate.Value > maxBitrate.Value)
            {
                _logger.Info("Bitrate exceeds DirectPlay limit");
                return false;
            }

            return true;
        }

        private void ValidateInput(VideoOptions options)
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

        private void ValidateAudioInput(AudioOptions options)
        {
            if (string.IsNullOrEmpty(options.ItemId))
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

        private void ApplyTranscodingConditions(StreamInfo item, IEnumerable<ProfileCondition> conditions)
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
                            int num;
                            if (IntHelper.TryParseCultureInvariant(value, out num))
                            {
                                item.AudioBitrate = num;
                            }
                            break;
                        }
                    case ProfileConditionValue.AudioChannels:
                        {
                            int num;
                            if (IntHelper.TryParseCultureInvariant(value, out num))
                            {
                                item.MaxAudioChannels = num;
                            }
                            break;
                        }
                    case ProfileConditionValue.IsAnamorphic:
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
                            int num;
                            if (IntHelper.TryParseCultureInvariant(value, out num))
                            {
                                item.MaxRefFrames = num;
                            }
                            break;
                        }
                    case ProfileConditionValue.VideoBitDepth:
                        {
                            int num;
                            if (IntHelper.TryParseCultureInvariant(value, out num))
                            {
                                item.MaxVideoBitDepth = num;
                            }
                            break;
                        }
                    case ProfileConditionValue.VideoProfile:
                        {
                            item.VideoProfile = (value ?? string.Empty).Split('|')[0];
                            break;
                        }
                    case ProfileConditionValue.Height:
                        {
                            int num;
                            if (IntHelper.TryParseCultureInvariant(value, out num))
                            {
                                item.MaxHeight = num;
                            }
                            break;
                        }
                    case ProfileConditionValue.VideoBitrate:
                        {
                            int num;
                            if (IntHelper.TryParseCultureInvariant(value, out num))
                            {
                                item.VideoBitrate = num;
                            }
                            break;
                        }
                    case ProfileConditionValue.VideoFramerate:
                        {
                            float num;
                            if (FloatHelper.TryParseCultureInvariant(value, out num))
                            {
                                item.MaxFramerate = num;
                            }
                            break;
                        }
                    case ProfileConditionValue.VideoLevel:
                        {
                            int num;
                            if (IntHelper.TryParseCultureInvariant(value, out num))
                            {
                                item.VideoLevel = num;
                            }
                            break;
                        }
                    case ProfileConditionValue.Width:
                        {
                            int num;
                            if (IntHelper.TryParseCultureInvariant(value, out num))
                            {
                                item.MaxWidth = num;
                            }
                            break;
                        }
                }
            }
        }

        private bool IsAudioDirectPlaySupported(DirectPlayProfile profile, MediaSourceInfo item, MediaStream audioStream)
        {
            if (profile.Container.Length > 0)
            {
                // Check container type
                string mediaContainer = item.Container ?? string.Empty;
                bool any = false;
                foreach (string i in profile.GetContainers())
                {
                    if (StringHelper.EqualsIgnoreCase(i, mediaContainer))
                    {
                        any = true;
                        break;
                    }
                }
                if (!any)
                {
                    return false;
                }
            }

            // Check audio codec
            List<string> audioCodecs = profile.GetAudioCodecs();
            if (audioCodecs.Count > 0)
            {
                // Check audio codecs
                string audioCodec = audioStream == null ? null : audioStream.Codec;
                if (string.IsNullOrEmpty(audioCodec) || !ListHelper.ContainsIgnoreCase(audioCodecs, audioCodec))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsVideoDirectPlaySupported(DirectPlayProfile profile, MediaSourceInfo item, MediaStream videoStream, MediaStream audioStream)
        {
            if (profile.Container.Length > 0)
            {
                // Check container type
                string mediaContainer = item.Container ?? string.Empty;
                bool any = false;
                foreach (string i in profile.GetContainers())
                {
                    if (StringHelper.EqualsIgnoreCase(i, mediaContainer))
                    {
                        any = true;
                        break;
                    }
                }
                if (!any)
                {
                    return false;
                }
            }

            // Check video codec
            List<string> videoCodecs = profile.GetVideoCodecs();
            if (videoCodecs.Count > 0)
            {
                string videoCodec = videoStream == null ? null : videoStream.Codec;
                if (string.IsNullOrEmpty(videoCodec) || !ListHelper.ContainsIgnoreCase(videoCodecs, videoCodec))
                {
                    return false;
                }
            }

            // Check audio codec
            List<string> audioCodecs = profile.GetAudioCodecs();
            if (audioCodecs.Count > 0)
            {
                // Check audio codecs
                string audioCodec = audioStream == null ? null : audioStream.Codec;
                if (string.IsNullOrEmpty(audioCodec) || !ListHelper.ContainsIgnoreCase(audioCodecs, audioCodec))
                {
                    return false;
                }
            }

            return true;
        }
    }
}