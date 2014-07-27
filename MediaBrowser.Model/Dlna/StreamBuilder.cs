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
        private string[] _serverTextSubtitleOutputs = new string[] { "srt", "vtt" };

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

            // If the client wants a specific media source, filter now
            if (!string.IsNullOrEmpty(options.MediaSourceId))
            {
                var newMediaSources = new List<MediaSourceInfo>();
                foreach (MediaSourceInfo i in mediaSources)
                {
                    if (StringHelper.EqualsIgnoreCase(i.Id, options.MediaSourceId))
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

            int? maxBitrateSetting = options.GetMaxBitrate();

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
                            {
                                foreach (var c in i.Conditions)
                                {
                                    conditions.Add(c);
                                }
                            }
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
                if (i.Type == playlistItem.MediaType && i.Context == options.Context)
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
                {
                    foreach (var c in i.Conditions)
                    {
                        audioTranscodingConditions.Add(c);
                    }
                }

                ApplyTranscodingConditions(playlistItem, audioTranscodingConditions);

                // Honor requested max channels
                if (options.MaxAudioChannels.HasValue)
                {
                    int currentValue = playlistItem.MaxAudioChannels ?? options.MaxAudioChannels.Value;

                    playlistItem.MaxAudioChannels = Math.Min(options.MaxAudioChannels.Value, currentValue);
                }

                if (!playlistItem.AudioBitrate.HasValue)
                {
                    playlistItem.AudioBitrate = 128000;
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

            int? audioStreamIndex = options.AudioStreamIndex ?? item.DefaultAudioStreamIndex;
            playlistItem.SubtitleStreamIndex = options.SubtitleStreamIndex ?? item.DefaultSubtitleStreamIndex;

            MediaStream audioStream = audioStreamIndex.HasValue ? item.GetMediaStream(MediaStreamType.Audio, audioStreamIndex.Value) : null;
            MediaStream subtitleStream = playlistItem.SubtitleStreamIndex.HasValue ? item.GetMediaStream(MediaStreamType.Subtitle, playlistItem.SubtitleStreamIndex.Value) : null;

            MediaStream videoStream = item.VideoStream;

            int? maxBitrateSetting = options.GetMaxBitrate();

            if (IsEligibleForDirectPlay(item, maxBitrateSetting, subtitleStream, options))
            {
                // See if it can be direct played
                DirectPlayProfile directPlay = GetVideoDirectPlayProfile(options.Profile, item, videoStream, audioStream);

                if (directPlay != null)
                {
                    playlistItem.IsDirectStream = true;
                    playlistItem.Container = item.Container;

                    if (subtitleStream != null)
                    {
                        playlistItem.SubtitleDeliveryMethod = GetDirectStreamSubtitleDeliveryMethod(subtitleStream, options);
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
                if (subtitleStream != null)
                {
                    playlistItem.SubtitleDeliveryMethod = GetTranscodedSubtitleDeliveryMethod(subtitleStream, options);
                }

                playlistItem.IsDirectStream = false;
                playlistItem.Container = transcodingProfile.Container;
                playlistItem.EstimateContentLength = transcodingProfile.EstimateContentLength;
                playlistItem.TranscodeSeekInfo = transcodingProfile.TranscodeSeekInfo;
                playlistItem.AudioCodec = transcodingProfile.AudioCodec.Split(',')[0];
                playlistItem.VideoCodec = transcodingProfile.VideoCodec;
                playlistItem.Protocol = transcodingProfile.Protocol;
                playlistItem.AudioStreamIndex = audioStreamIndex;

                List<ProfileCondition> videoTranscodingConditions = new List<ProfileCondition>();
                foreach (CodecProfile i in options.Profile.CodecProfiles)
                {
                    if (i.Type == CodecType.Video && i.ContainsCodec(transcodingProfile.VideoCodec))
                    {
                        foreach (var c in i.Conditions)
                        {
                            videoTranscodingConditions.Add(c);
                        }
                        break;
                    }
                }
                ApplyTranscodingConditions(playlistItem, videoTranscodingConditions);

                List<ProfileCondition> audioTranscodingConditions = new List<ProfileCondition>();
                foreach (CodecProfile i in options.Profile.CodecProfiles)
                {
                    if (i.Type == CodecType.VideoAudio && i.ContainsCodec(transcodingProfile.AudioCodec))
                    {
                        foreach (var c in i.Conditions)
                        {
                            audioTranscodingConditions.Add(c);
                        }
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

                if (!playlistItem.AudioBitrate.HasValue)
                {
                    playlistItem.AudioBitrate = GetAudioBitrate(playlistItem.TargetAudioChannels, playlistItem.TargetAudioCodec);
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

        private int GetAudioBitrate(int? channels, string codec)
        {
            if (channels.HasValue)
            {
                if (channels.Value >= 5)
                {
                    return 320000;
                }
            }

            return 128000;
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
                    foreach (var c in i.Conditions)
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
                {
                    foreach (var c in i.Conditions)
                    {
                        conditions.Add(c);
                    }
                }
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
                    {
                        foreach (var c in i.Conditions)
                        {
                            conditions.Add(c);
                        }
                    }
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

        private bool IsEligibleForDirectPlay(MediaSourceInfo item,
            int? maxBitrate,
            MediaStream subtitleStream,
            VideoOptions options)
        {
            if (subtitleStream != null)
            {
                if (!subtitleStream.IsTextSubtitleStream)
                {
                    return false;
                }

                SubtitleDeliveryMethod subtitleMethod = GetDirectStreamSubtitleDeliveryMethod(subtitleStream, options);

                if (subtitleMethod != SubtitleDeliveryMethod.External && subtitleMethod != SubtitleDeliveryMethod.Direct)
                {
                    return false;
                }
            }

            return IsAudioEligibleForDirectPlay(item, maxBitrate);
        }

        private SubtitleDeliveryMethod GetDirectStreamSubtitleDeliveryMethod(MediaStream subtitleStream,
            VideoOptions options)
        {
            if (subtitleStream.IsTextSubtitleStream)
            {
                string subtitleFormat = NormalizeSubtitleFormat(subtitleStream.Codec);

                bool supportsDirect = ContainsSubtitleFormat(options.Profile.SoftSubtitleProfiles, new[] { subtitleFormat });

                if (supportsDirect)
                {
                    return SubtitleDeliveryMethod.Direct;
                }
                
                // See if the device can retrieve the subtitles externally
                bool supportsSubsExternally = options.Context == EncodingContext.Streaming &&
                    ContainsSubtitleFormat(options.Profile.ExternalSubtitleProfiles, _serverTextSubtitleOutputs);

                if (supportsSubsExternally)
                {
                    return SubtitleDeliveryMethod.External;
                }
            }

            return SubtitleDeliveryMethod.Encode;
        }

        private SubtitleDeliveryMethod GetTranscodedSubtitleDeliveryMethod(MediaStream subtitleStream,
            VideoOptions options)
        {
            if (subtitleStream.IsTextSubtitleStream)
            {
                // See if the device can retrieve the subtitles externally
                bool supportsSubsExternally = options.Context == EncodingContext.Streaming &&
                    ContainsSubtitleFormat(options.Profile.ExternalSubtitleProfiles, _serverTextSubtitleOutputs);

                if (supportsSubsExternally)
                {
                    return SubtitleDeliveryMethod.External;
                }

                // See if the device can retrieve the subtitles externally
                bool supportsEmbedded = ContainsSubtitleFormat(options.Profile.SoftSubtitleProfiles, _serverTextSubtitleOutputs);

                if (supportsEmbedded)
                {
                    return SubtitleDeliveryMethod.Embed;
                }
            }

            return SubtitleDeliveryMethod.Encode;
        }

        private string NormalizeSubtitleFormat(string codec)
        {
            if (StringHelper.EqualsIgnoreCase(codec, "subrip"))
            {
                return SubtitleFormat.SRT;
            }

            return codec;
        }

        private bool ContainsSubtitleFormat(SubtitleProfile[] profiles, string[] formats)
        {
            foreach (SubtitleProfile profile in profiles)
            {
                if (ListHelper.ContainsIgnoreCase(formats, profile.Format))
                {
                    return true;
                }
            }

            return false;
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
