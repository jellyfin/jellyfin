using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Dlna
{
    public class StreamBuilder
    {
        private readonly ILocalPlayer _localPlayer;

        public StreamBuilder(ILocalPlayer localPlayer)
        {
            _localPlayer = localPlayer;
        }
        public StreamBuilder()
            : this(new NullLocalPlayer())
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

            return GetOptimalStream(streams);
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

            return GetOptimalStream(streams);
        }

        private StreamInfo GetOptimalStream(List<StreamInfo> streams)
        {
            streams = StreamInfoSorter.SortMediaSources(streams);

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

            MediaStream audioStream = item.GetDefaultAudioStream(null);

            List<PlayMethod> directPlayMethods = GetAudioDirectPlayMethods(item, audioStream, options);

            if (directPlayMethods.Count > 0)
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
                            foreach (ProfileCondition c in i.Conditions)
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
                        if (item.Protocol == MediaProtocol.File &&
                            directPlayMethods.Contains(PlayMethod.DirectPlay) && 
                            _localPlayer.CanAccessFile(item.Path))
                        {
                            playlistItem.PlayMethod = PlayMethod.DirectPlay;
                        }
                        else if (item.Protocol == MediaProtocol.Http &&
                            directPlayMethods.Contains(PlayMethod.DirectPlay) &&
                            _localPlayer.CanAccessUrl(item.Path, item.RequiredHttpHeaders.Count > 0))
                        {
                            playlistItem.PlayMethod = PlayMethod.DirectPlay;
                        }
                        else if (directPlayMethods.Contains(PlayMethod.DirectStream))
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

                playlistItem.PlayMethod = PlayMethod.Transcode;
                playlistItem.TranscodeSeekInfo = transcodingProfile.TranscodeSeekInfo;
                playlistItem.EstimateContentLength = transcodingProfile.EstimateContentLength;
                playlistItem.Container = transcodingProfile.Container;
                playlistItem.AudioCodec = transcodingProfile.AudioCodec;
                playlistItem.SubProtocol = transcodingProfile.Protocol;

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
                    foreach (ProfileCondition c in i.Conditions)
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

                int configuredBitrate = options.AudioTranscodingBitrate ??
                    (options.Context == EncodingContext.Static ? options.Profile.MusicSyncBitrate : options.Profile.MusicStreamingTranscodingBitrate) ??
                    128000;

                playlistItem.AudioBitrate = Math.Min(configuredBitrate, playlistItem.AudioBitrate ?? configuredBitrate);
            }

            return playlistItem;
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
                if (item.SupportsDirectStream && IsAudioEligibleForDirectPlay(item, options.GetMaxBitrate()))
                {
                    playMethods.Add(PlayMethod.DirectStream);
                }
                
                // The profile describes what the device supports
                // If device requirements are satisfied then allow both direct stream and direct play
                if (IsAudioEligibleForDirectPlay(item, options.Profile.MaxStaticBitrate))
                {
                    playMethods.Add(PlayMethod.DirectPlay);
                }
            }

            return playMethods;
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

            playlistItem.SubtitleStreamIndex = options.SubtitleStreamIndex ?? item.DefaultSubtitleStreamIndex;
            MediaStream subtitleStream = playlistItem.SubtitleStreamIndex.HasValue ? item.GetMediaStream(MediaStreamType.Subtitle, playlistItem.SubtitleStreamIndex.Value) : null;

            MediaStream audioStream = item.GetDefaultAudioStream(options.AudioStreamIndex ?? item.DefaultAudioStreamIndex);
            int? audioStreamIndex = audioStream == null ? (int?)null : audioStream.Index;

            MediaStream videoStream = item.VideoStream;

            // TODO: This doesn't accout for situation of device being able to handle media bitrate, but wifi connection not fast enough
            bool isEligibleForDirectPlay = IsEligibleForDirectPlay(item, options.Profile.MaxStaticBitrate, subtitleStream, options);
            bool isEligibleForDirectStream = IsEligibleForDirectPlay(item, options.GetMaxBitrate(), subtitleStream, options);

            if (isEligibleForDirectPlay || isEligibleForDirectStream)
            {
                // See if it can be direct played
                PlayMethod? directPlay = GetVideoDirectPlayProfile(options.Profile, item, videoStream, audioStream, isEligibleForDirectPlay, isEligibleForDirectStream);

                if (directPlay != null)
                {
                    playlistItem.PlayMethod = directPlay.Value;
                    playlistItem.Container = item.Container;

                    if (subtitleStream != null)
                    {
                        SubtitleProfile subtitleProfile = GetSubtitleProfile(subtitleStream, options.Profile.SubtitleProfiles, options.Context);

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
                    SubtitleProfile subtitleProfile = GetSubtitleProfile(subtitleStream, options.Profile.SubtitleProfiles, options.Context);

                    playlistItem.SubtitleDeliveryMethod = subtitleProfile.Method;
                    playlistItem.SubtitleFormat = subtitleProfile.Format;
                }

                playlistItem.PlayMethod = PlayMethod.Transcode;
                playlistItem.Container = transcodingProfile.Container;
                playlistItem.EstimateContentLength = transcodingProfile.EstimateContentLength;
                playlistItem.TranscodeSeekInfo = transcodingProfile.TranscodeSeekInfo;
                playlistItem.AudioCodec = transcodingProfile.AudioCodec.Split(',')[0];
                playlistItem.VideoCodec = transcodingProfile.VideoCodec;
                playlistItem.SubProtocol = transcodingProfile.Protocol;
                playlistItem.AudioStreamIndex = audioStreamIndex;

                List<ProfileCondition> videoTranscodingConditions = new List<ProfileCondition>();
                foreach (CodecProfile i in options.Profile.CodecProfiles)
                {
                    if (i.Type == CodecType.Video && i.ContainsCodec(transcodingProfile.VideoCodec))
                    {
                        foreach (ProfileCondition c in i.Conditions)
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
                        foreach (ProfileCondition c in i.Conditions)
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

                int? maxBitrateSetting = options.GetMaxBitrate();
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

        private PlayMethod? GetVideoDirectPlayProfile(DeviceProfile profile,
            MediaSourceInfo mediaSource,
            MediaStream videoStream,
            MediaStream audioStream,
            bool isEligibleForDirectPlay,
            bool isEligibleForDirectStream)
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
            bool? isCabac = videoStream == null ? null : videoStream.IsCabac;

            int? audioBitrate = audioStream == null ? null : audioStream.BitRate;
            int? audioChannels = audioStream == null ? null : audioStream.Channels;
            string audioProfile = audioStream == null ? null : audioStream.Profile;

            TransportStreamTimestamp? timestamp = videoStream == null ? TransportStreamTimestamp.None : mediaSource.Timestamp;
            int? packetLength = videoStream == null ? null : videoStream.PacketLength;
            int? refFrames = videoStream == null ? null : videoStream.RefFrames;

            // Check container conditions
            foreach (ProfileCondition i in conditions)
            {
                if (!conditionProcessor.IsVideoConditionSatisfied(i, audioBitrate, audioChannels, width, height, bitDepth, videoBitrate, videoProfile, videoLevel, videoFramerate, packetLength, timestamp, isAnamorphic, isCabac, refFrames))
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
                    foreach (ProfileCondition c in i.Conditions)
                    {
                        conditions.Add(c);
                    }
                }
            }

            foreach (ProfileCondition i in conditions)
            {
                if (!conditionProcessor.IsVideoConditionSatisfied(i, audioBitrate, audioChannels, width, height, bitDepth, videoBitrate, videoProfile, videoLevel, videoFramerate, packetLength, timestamp, isAnamorphic, isCabac, refFrames))
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
                        foreach (ProfileCondition c in i.Conditions)
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

            if (isEligibleForDirectPlay)
            {
                if (mediaSource.Protocol == MediaProtocol.Http)
                {
                    if (_localPlayer.CanAccessUrl(mediaSource.Path, mediaSource.RequiredHttpHeaders.Count > 0))
                    {
                        return PlayMethod.DirectPlay;
                    }
                }

                else if (mediaSource.Protocol == MediaProtocol.File)
                {
                    if (_localPlayer.CanAccessFile(mediaSource.Path))
                    {
                        return PlayMethod.DirectPlay;
                    }
                }
            }

            if (isEligibleForDirectStream)
            {
                if (mediaSource.SupportsDirectStream)
                {
                    return PlayMethod.DirectStream;
                }
            }

            return null;
        }

        private bool IsEligibleForDirectPlay(MediaSourceInfo item,
            int? maxBitrate,
            MediaStream subtitleStream,
            VideoOptions options)
        {
            if (subtitleStream != null)
            {
                SubtitleProfile subtitleProfile = GetSubtitleProfile(subtitleStream, options.Profile.SubtitleProfiles, options.Context);

                if (subtitleProfile.Method != SubtitleDeliveryMethod.External && subtitleProfile.Method != SubtitleDeliveryMethod.Embed)
                {
                    return false;
                }
            }

            return IsAudioEligibleForDirectPlay(item, maxBitrate);
        }

        public static SubtitleProfile GetSubtitleProfile(MediaStream subtitleStream, SubtitleProfile[] subtitleProfiles, EncodingContext context)
        {
            // Look for an external profile that matches the stream type (text/graphical)
            foreach (SubtitleProfile profile in subtitleProfiles)
            {
                if (profile.Method == SubtitleDeliveryMethod.External && subtitleStream.IsTextSubtitleStream == MediaStream.IsTextFormat(profile.Format))
                {
                    if (subtitleStream.SupportsExternalStream)
                    {
                        return profile;
                    }

                    // For sync we can handle the longer extraction times
                    if (context == EncodingContext.Static && subtitleStream.IsTextSubtitleStream)
                    {
                        return profile;
                    }
                }
            }

            foreach (SubtitleProfile profile in subtitleProfiles)
            {
                if (profile.Method == SubtitleDeliveryMethod.Embed && subtitleStream.IsTextSubtitleStream == MediaStream.IsTextFormat(profile.Format))
                {
                    return profile;
                }
            }

            return new SubtitleProfile
            {
                Method = SubtitleDeliveryMethod.Encode,
                Format = subtitleStream.Codec
            };
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
                    case ProfileConditionValue.IsCabac:
                        {
                            bool val;
                            if (BoolHelper.TryParseCultureInvariant(value, out val))
                            {
                                if (condition.Condition == ProfileConditionType.Equals)
                                {
                                    item.Cabac = val;
                                }
                                else if (condition.Condition == ProfileConditionType.NotEquals)
                                {
                                    item.Cabac = !val;
                                }
                            }
                            break;
                        }
                    case ProfileConditionValue.IsAnamorphic:
                    case ProfileConditionValue.AudioProfile:
                    case ProfileConditionValue.Has64BitOffsets:
                    case ProfileConditionValue.PacketLength:
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
