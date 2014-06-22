using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Dlna
{
    public class StreamBuilder
    {
        public StreamInfo BuildAudioItem(AudioOptions options)
        {
            ValidateAudioInput(options);

            List<MediaSourceInfo> mediaSources = options.MediaSources;

            // If the client wants a specific media soure, filter now
            if (!string.IsNullOrEmpty(options.MediaSourceId))
            {
                // Avoid implicitly captured closure
                string mediaSourceId = options.MediaSourceId;

                mediaSources = new List<MediaSourceInfo>();
                foreach (MediaSourceInfo i in mediaSources)
                {
                    if (StringHelper.EqualsIgnoreCase(i.Id, mediaSourceId))
                        mediaSources.Add(i);
                }
            }

            List<StreamInfo> streams = new List<StreamInfo>();
            foreach (MediaSourceInfo i in mediaSources)
                streams.Add(BuildAudioItem(i, options));

            foreach (StreamInfo stream in streams)
            {
                stream.DeviceId = options.DeviceId;
                stream.DeviceProfileId = options.Profile.Id;
            }

            return GetOptimalStream(streams);
        }

        public StreamInfo BuildVideoItem(VideoOptions options)
        {
            ValidateInput(options);

            List<MediaSourceInfo> mediaSources = options.MediaSources;

            // If the client wants a specific media soure, filter now
            if (!string.IsNullOrEmpty(options.MediaSourceId))
            {
                // Avoid implicitly captured closure
                string mediaSourceId = options.MediaSourceId;

                var newMediaSources = new List<MediaSourceInfo>();
                foreach (MediaSourceInfo i in mediaSources)
                {
                    if (StringHelper.EqualsIgnoreCase(i.Id, mediaSourceId))
                        newMediaSources.Add(i);
                }

                mediaSources = newMediaSources;
            }

            List<StreamInfo> streams = new List<StreamInfo>();
            foreach (MediaSourceInfo i in mediaSources)
                streams.Add(BuildVideoItem(i, options));

            foreach (StreamInfo stream in streams)
            {
                stream.DeviceId = options.DeviceId;
                stream.DeviceProfileId = options.Profile.Id;
            }

            return GetOptimalStream(streams);
        }

        private StreamInfo GetOptimalStream(List<StreamInfo> streams)
        {
            // Grab the first one that can be direct streamed
            // If that doesn't produce anything, just take the first
            foreach (StreamInfo i in streams)
            {
                if (i.IsDirectStream)
                {
                    return i;
                }
            }

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
                RunTimeTicks = item.RunTimeTicks
            };

            int? maxBitrateSetting = options.MaxBitrate ?? options.Profile.MaxBitrate;

            MediaStream audioStream = item.DefaultAudioStream;

            // Honor the max bitrate setting
            if (IsAudioEligibleForDirectPlay(item, maxBitrateSetting))
            {
                DirectPlayProfile directPlay = null;
                foreach (DirectPlayProfile i in options.Profile.DirectPlayProfiles)
                {
                    if (i.Type == playlistItem.MediaType && IsAudioDirectPlaySupported(i, item, audioStream))
                    {
                        directPlay = i;
                        break;
                    }
                }

                if (directPlay != null)
                {
                    string audioCodec = audioStream == null ? null : audioStream.Codec;

                    // Make sure audio codec profiles are satisfied
                    if (!string.IsNullOrEmpty(audioCodec))
                    {
                        ConditionProcessor conditionProcessor = new ConditionProcessor();

                        List<ProfileCondition> conditions = new List<ProfileCondition>();
                        foreach (CodecProfile i in options.Profile.CodecProfiles)
                        {
                            if (i.Type == CodecType.Audio && i.ContainsCodec(audioCodec))
                                conditions.AddRange(i.Conditions);
                        }

                        int? audioChannels = audioStream.Channels;
                        int? audioBitrate = audioStream.BitRate;

                        bool all = true;
                        foreach (ProfileCondition c in conditions)
                        {
                            if (!conditionProcessor.IsAudioConditionSatisfied(c, audioChannels, audioBitrate))
                            {
                                all = false;
                                break;
                            }
                        }

                        if (all)
                        {
                            playlistItem.IsDirectStream = true;
                            playlistItem.Container = item.Container;

                            return playlistItem;
                        }
                    }
                }
            }

            TranscodingProfile transcodingProfile = null;
            foreach (TranscodingProfile i in options.Profile.TranscodingProfiles)
            {
                if (i.Type == playlistItem.MediaType)
                {
                    transcodingProfile = i;
                    break;
                }
            }

            if (transcodingProfile != null)
            {
                playlistItem.IsDirectStream = false;
                playlistItem.TranscodeSeekInfo = transcodingProfile.TranscodeSeekInfo;
                playlistItem.EstimateContentLength = transcodingProfile.EstimateContentLength;
                playlistItem.Container = transcodingProfile.Container;
                playlistItem.AudioCodec = transcodingProfile.AudioCodec;
                playlistItem.Protocol = transcodingProfile.Protocol;

                List<CodecProfile> audioCodecProfiles = new List<CodecProfile>();
                foreach (CodecProfile i in options.Profile.CodecProfiles)
                {
                    if (i.Type == CodecType.Audio && i.ContainsCodec(transcodingProfile.AudioCodec))
                    {
                        audioCodecProfiles.Add(i);
                    }

                    if (audioCodecProfiles.Count >= 1) break;
                }

                List<ProfileCondition> audioTranscodingConditions = new List<ProfileCondition>();
                foreach (CodecProfile i in audioCodecProfiles)
                        audioTranscodingConditions.AddRange(i.Conditions);

                ApplyTranscodingConditions(playlistItem, audioTranscodingConditions);

                // Honor requested max channels
                if (options.MaxAudioChannels.HasValue)
                {
                    int currentValue = playlistItem.MaxAudioChannels ?? options.MaxAudioChannels.Value;

                    playlistItem.MaxAudioChannels = Math.Min(options.MaxAudioChannels.Value, currentValue);
                }

                // Honor requested max bitrate
                if (maxBitrateSetting.HasValue)
                {
                    int currentValue = playlistItem.AudioBitrate ?? maxBitrateSetting.Value;

                    playlistItem.AudioBitrate = Math.Min(maxBitrateSetting.Value, currentValue);
                }
            }

            return playlistItem;
        }

        private StreamInfo BuildVideoItem(MediaSourceInfo item, VideoOptions options)
        {
            StreamInfo playlistItem = new StreamInfo
            {
                ItemId = options.ItemId,
                MediaType = DlnaProfileType.Video,
                MediaSource = item,
                RunTimeTicks = item.RunTimeTicks
            };

            MediaStream audioStream = item.DefaultAudioStream;
            MediaStream videoStream = item.VideoStream;

            int? maxBitrateSetting = options.MaxBitrate ?? options.Profile.MaxBitrate;

            if (IsEligibleForDirectPlay(item, options, maxBitrateSetting))
            {
                // See if it can be direct played
                DirectPlayProfile directPlay = GetVideoDirectPlayProfile(options.Profile, item, videoStream, audioStream);

                if (directPlay != null)
                {
                    playlistItem.IsDirectStream = true;
                    playlistItem.Container = item.Container;

                    return playlistItem;
                }
            }

            // Can't direct play, find the transcoding profile
            TranscodingProfile transcodingProfile = null;
            foreach (TranscodingProfile i in options.Profile.TranscodingProfiles)
            {
                if (i.Type == playlistItem.MediaType)
                {
                    transcodingProfile = i;
                    break;
                }
            }

            if (transcodingProfile != null)
            {
                playlistItem.IsDirectStream = false;
                playlistItem.Container = transcodingProfile.Container;
                playlistItem.EstimateContentLength = transcodingProfile.EstimateContentLength;
                playlistItem.TranscodeSeekInfo = transcodingProfile.TranscodeSeekInfo;
                playlistItem.AudioCodec = transcodingProfile.AudioCodec.Split(',')[0];
                playlistItem.VideoCodec = transcodingProfile.VideoCodec;
                playlistItem.Protocol = transcodingProfile.Protocol;
                playlistItem.AudioStreamIndex = options.AudioStreamIndex ?? item.DefaultAudioStreamIndex;
                playlistItem.SubtitleStreamIndex = options.SubtitleStreamIndex ?? item.DefaultSubtitleStreamIndex;

                List<ProfileCondition> videoTranscodingConditions = new List<ProfileCondition>();
                foreach (CodecProfile i in options.Profile.CodecProfiles)
                {
                    if (i.Type == CodecType.Video && i.ContainsCodec(transcodingProfile.VideoCodec))
                    {
                        videoTranscodingConditions.AddRange(i.Conditions);
                        break;
                    }
                }
                ApplyTranscodingConditions(playlistItem, videoTranscodingConditions);

                List<ProfileCondition> audioTranscodingConditions = new List<ProfileCondition>();
                foreach (CodecProfile i in options.Profile.CodecProfiles)
                {
                    if (i.Type == CodecType.VideoAudio && i.ContainsCodec(transcodingProfile.AudioCodec))
                    {
                        audioTranscodingConditions.AddRange(i.Conditions);
                        break;
                    }
                }
                ApplyTranscodingConditions(playlistItem, audioTranscodingConditions);

                // Honor requested max channels
                if (options.MaxAudioChannels.HasValue)
                {
                    int currentValue = playlistItem.MaxAudioChannels ?? options.MaxAudioChannels.Value;

                    playlistItem.MaxAudioChannels = Math.Min(options.MaxAudioChannels.Value, currentValue);
                }

                // Honor requested max bitrate
                if (options.MaxAudioTranscodingBitrate.HasValue)
                {
                    int currentValue = playlistItem.AudioBitrate ?? options.MaxAudioTranscodingBitrate.Value;

                    playlistItem.AudioBitrate = Math.Min(options.MaxAudioTranscodingBitrate.Value, currentValue);
                }

                // Honor max rate
                if (maxBitrateSetting.HasValue)
                {
                    int videoBitrate = maxBitrateSetting.Value;

                    if (playlistItem.AudioBitrate.HasValue)
                    {
                        videoBitrate -= playlistItem.AudioBitrate.Value;
                    }

                    int currentValue = playlistItem.VideoBitrate ?? videoBitrate;

                    playlistItem.VideoBitrate = Math.Min(videoBitrate, currentValue);
                }
            }

            return playlistItem;
        }

        private DirectPlayProfile GetVideoDirectPlayProfile(DeviceProfile profile,
            MediaSourceInfo mediaSource,
            MediaStream videoStream,
            MediaStream audioStream)
        {
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
                return null;
            }

            string container = mediaSource.Container;

            List<ProfileCondition> conditions = new List<ProfileCondition>();
            foreach (ContainerProfile i in profile.ContainerProfiles)
            {
                if (i.Type == DlnaProfileType.Video &&
                    ListHelper.ContainsIgnoreCase(i.GetContainers(), container))
                {
                    conditions.AddRange(i.Conditions);
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

            int? audioBitrate = audioStream == null ? null : audioStream.BitRate;
            int? audioChannels = audioStream == null ? null : audioStream.Channels;
            string audioProfile = audioStream == null ? null : audioStream.Profile;

            TransportStreamTimestamp? timestamp = videoStream == null ? TransportStreamTimestamp.None : mediaSource.Timestamp;
            int? packetLength = videoStream == null ? null : videoStream.PacketLength;

            // Check container conditions
            foreach (ProfileCondition i in conditions)
            {
                if (!conditionProcessor.IsVideoConditionSatisfied(i, audioBitrate, audioChannels, width, height, bitDepth, videoBitrate, videoProfile, videoLevel, videoFramerate, packetLength, timestamp, isAnamorphic))
                {
                    return null;
                }
            }

            string videoCodec = videoStream == null ? null : videoStream.Codec;

            if (string.IsNullOrEmpty(videoCodec))
            {
                return null;
            }

            conditions = new List<ProfileCondition>();
            foreach (CodecProfile i in profile.CodecProfiles)
            {
                if (i.Type == CodecType.Video && i.ContainsCodec(videoCodec))
                    conditions.AddRange(i.Conditions);
            }

            foreach (ProfileCondition i in conditions)
            {
                if (!conditionProcessor.IsVideoConditionSatisfied(i, audioBitrate, audioChannels, width, height, bitDepth, videoBitrate, videoProfile, videoLevel, videoFramerate, packetLength, timestamp, isAnamorphic))
                {
                    return null;
                }
            }

            if (audioStream != null)
            {
                string audioCodec = audioStream.Codec;

                if (string.IsNullOrEmpty(audioCodec))
                {
                    return null;
                }

                conditions = new List<ProfileCondition>();
                foreach (CodecProfile i in profile.CodecProfiles)
                {
                    if (i.Type == CodecType.VideoAudio && i.ContainsCodec(audioCodec))
                        conditions.AddRange(i.Conditions);
                }

                foreach (ProfileCondition i in conditions)
                {
                    if (!conditionProcessor.IsVideoAudioConditionSatisfied(i, audioChannels, audioBitrate, audioProfile))
                    {
                        return null;
                    }
                }
            }

            return directPlay;
        }

        private bool IsEligibleForDirectPlay(MediaSourceInfo item, VideoOptions options, int? maxBitrate)
        {
            if (options.SubtitleStreamIndex.HasValue)
            {
                return false;
            }

            return IsAudioEligibleForDirectPlay(item, maxBitrate);
        }

        private bool IsAudioEligibleForDirectPlay(MediaSourceInfo item, int? maxBitrate)
        {
            // Honor the max bitrate setting
            return !maxBitrate.HasValue || (item.Bitrate.HasValue && item.Bitrate.Value <= maxBitrate.Value);
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
                    case ProfileConditionValue.AudioProfile:
                    case ProfileConditionValue.IsAnamorphic:
                    case ProfileConditionValue.Has64BitOffsets:
                    case ProfileConditionValue.PacketLength:
                    case ProfileConditionValue.VideoTimestamp:
                    case ProfileConditionValue.VideoBitDepth:
                        {
                            // Not supported yet
                            break;
                        }
                    case ProfileConditionValue.VideoProfile:
                        {
                            item.VideoProfile = value;
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
                            double num;
                            if (DoubleHelper.TryParseCultureInvariant(value, out num))
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
                    default:
                        throw new ArgumentException("Unrecognized ProfileConditionValue");
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

            return true;
        }

        private bool IsVideoDirectPlaySupported(DirectPlayProfile profile, MediaSourceInfo item, MediaStream videoStream, MediaStream audioStream)
        {
            // Only plain video files can be direct played
            if (item.VideoType != VideoType.VideoFile)
            {
                return false;
            }

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
