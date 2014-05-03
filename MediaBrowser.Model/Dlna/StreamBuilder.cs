using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MediaBrowser.Model.Dlna
{
    public class StreamBuilder
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public StreamInfo BuildAudioItem(AudioOptions options)
        {
            ValidateAudioInput(options);

            var mediaSources = options.MediaSources;

            // If the client wants a specific media soure, filter now
            if (!string.IsNullOrEmpty(options.MediaSourceId))
            {
                // Avoid implicitly captured closure
                var mediaSourceId = options.MediaSourceId;

                mediaSources = mediaSources
                    .Where(i => string.Equals(i.Id, mediaSourceId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var streams = mediaSources.Select(i => BuildAudioItem(i, options)).ToList();

            foreach (var stream in streams)
            {
                stream.DeviceId = options.DeviceId;
                stream.DeviceProfileId = options.Profile.Id;
            }

            return GetOptimalStream(streams);
        }

        public StreamInfo BuildVideoItem(VideoOptions options)
        {
            ValidateInput(options);

            var mediaSources = options.MediaSources;

            // If the client wants a specific media soure, filter now
            if (!string.IsNullOrEmpty(options.MediaSourceId))
            {
                // Avoid implicitly captured closure
                var mediaSourceId = options.MediaSourceId;

                mediaSources = mediaSources
                    .Where(i => string.Equals(i.Id, mediaSourceId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var streams = mediaSources.Select(i => BuildVideoItem(i, options)).ToList();

            foreach (var stream in streams)
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
            return streams.FirstOrDefault(i => i.IsDirectStream) ??
                streams.FirstOrDefault();
        }

        private StreamInfo BuildAudioItem(MediaSourceInfo item, AudioOptions options)
        {
            var playlistItem = new StreamInfo
            {
                ItemId = options.ItemId,
                MediaType = DlnaProfileType.Audio,
                MediaSource = item,
                RunTimeTicks = item.RunTimeTicks
            };

            var maxBitrateSetting = options.MaxBitrate ?? options.Profile.MaxBitrate;

            var audioStream = item.MediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Audio);

            // Honor the max bitrate setting
            if (IsAudioEligibleForDirectPlay(item, maxBitrateSetting))
            {
                var directPlay = options.Profile.DirectPlayProfiles
                    .FirstOrDefault(i => i.Type == playlistItem.MediaType && IsAudioDirectPlaySupported(i, item, audioStream));

                if (directPlay != null)
                {
                    var audioCodec = audioStream == null ? null : audioStream.Codec;

                    // Make sure audio codec profiles are satisfied
                    if (!string.IsNullOrEmpty(audioCodec))
                    {
                        var conditionProcessor = new ConditionProcessor();

                        var conditions = options.Profile.CodecProfiles.Where(i => i.Type == CodecType.Audio && i.ContainsCodec(audioCodec))
                                .SelectMany(i => i.Conditions);

                        var audioChannels = audioStream == null ? null : audioStream.Channels;
                        var audioBitrate = audioStream == null ? null : audioStream.BitRate;

                        if (conditions.All(c => conditionProcessor.IsAudioConditionSatisfied(c, audioChannels, audioBitrate)))
                        {
                            playlistItem.IsDirectStream = true;
                            playlistItem.Container = item.Container;

                            return playlistItem;
                        }
                    }
                }
            }

            var transcodingProfile = options.Profile.TranscodingProfiles
                .FirstOrDefault(i => i.Type == playlistItem.MediaType);

            if (transcodingProfile != null)
            {
                playlistItem.IsDirectStream = false;
                playlistItem.TranscodeSeekInfo = transcodingProfile.TranscodeSeekInfo;
                playlistItem.EstimateContentLength = transcodingProfile.EstimateContentLength;
                playlistItem.Container = transcodingProfile.Container;
                playlistItem.AudioCodec = transcodingProfile.AudioCodec;
                playlistItem.Protocol = transcodingProfile.Protocol;

                var audioTranscodingConditions = options.Profile.CodecProfiles
                    .Where(i => i.Type == CodecType.Audio && i.ContainsCodec(transcodingProfile.AudioCodec))
                    .Take(1)
                    .SelectMany(i => i.Conditions);

                ApplyTranscodingConditions(playlistItem, audioTranscodingConditions);

                // Honor requested max channels
                if (options.MaxAudioChannels.HasValue)
                {
                    var currentValue = playlistItem.MaxAudioChannels ?? options.MaxAudioChannels.Value;

                    playlistItem.MaxAudioChannels = Math.Min(options.MaxAudioChannels.Value, currentValue);
                }

                // Honor requested max bitrate
                if (maxBitrateSetting.HasValue)
                {
                    var currentValue = playlistItem.AudioBitrate ?? maxBitrateSetting.Value;

                    playlistItem.AudioBitrate = Math.Min(maxBitrateSetting.Value, currentValue);
                }
            }

            return playlistItem;
        }

        private StreamInfo BuildVideoItem(MediaSourceInfo item, VideoOptions options)
        {
            var playlistItem = new StreamInfo
            {
                ItemId = options.ItemId,
                MediaType = DlnaProfileType.Video,
                MediaSource = item,
                RunTimeTicks = item.RunTimeTicks
            };

            var audioStream = item.MediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Audio);
            var videoStream = item.MediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Video);

            var maxBitrateSetting = options.MaxBitrate ?? options.Profile.MaxBitrate;

            if (IsEligibleForDirectPlay(item, options, maxBitrateSetting))
            {
                // See if it can be direct played
                var directPlay = GetVideoDirectPlayProfile(options.Profile, item, videoStream, audioStream);

                if (directPlay != null)
                {
                    playlistItem.IsDirectStream = true;
                    playlistItem.Container = item.Container;

                    return playlistItem;
                }
            }

            // Can't direct play, find the transcoding profile
            var transcodingProfile = options.Profile.TranscodingProfiles
                .FirstOrDefault(i => i.Type == playlistItem.MediaType);

            if (transcodingProfile != null)
            {
                playlistItem.IsDirectStream = false;
                playlistItem.Container = transcodingProfile.Container;
                playlistItem.EstimateContentLength = transcodingProfile.EstimateContentLength;
                playlistItem.TranscodeSeekInfo = transcodingProfile.TranscodeSeekInfo;
                playlistItem.AudioCodec = transcodingProfile.AudioCodec.Split(',').FirstOrDefault();
                playlistItem.VideoCodec = transcodingProfile.VideoCodec;
                playlistItem.Protocol = transcodingProfile.Protocol;
                playlistItem.AudioStreamIndex = options.AudioStreamIndex;
                playlistItem.SubtitleStreamIndex = options.SubtitleStreamIndex;

                var videoTranscodingConditions = options.Profile.CodecProfiles
                    .Where(i => i.Type == CodecType.Video && i.ContainsCodec(transcodingProfile.VideoCodec))
                    .Take(1)
                    .SelectMany(i => i.Conditions);

                ApplyTranscodingConditions(playlistItem, videoTranscodingConditions);

                var audioTranscodingConditions = options.Profile.CodecProfiles
                    .Where(i => i.Type == CodecType.VideoAudio && i.ContainsCodec(transcodingProfile.AudioCodec))
                    .Take(1)
                    .SelectMany(i => i.Conditions);

                ApplyTranscodingConditions(playlistItem, audioTranscodingConditions);

                // Honor requested max channels
                if (options.MaxAudioChannels.HasValue)
                {
                    var currentValue = playlistItem.MaxAudioChannels ?? options.MaxAudioChannels.Value;

                    playlistItem.MaxAudioChannels = Math.Min(options.MaxAudioChannels.Value, currentValue);
                }

                // Honor requested max bitrate
                if (options.MaxAudioTranscodingBitrate.HasValue)
                {
                    var currentValue = playlistItem.AudioBitrate ?? options.MaxAudioTranscodingBitrate.Value;

                    playlistItem.AudioBitrate = Math.Min(options.MaxAudioTranscodingBitrate.Value, currentValue);
                }

                // Honor max rate
                if (maxBitrateSetting.HasValue)
                {
                    var videoBitrate = maxBitrateSetting.Value;

                    if (playlistItem.AudioBitrate.HasValue)
                    {
                        videoBitrate -= playlistItem.AudioBitrate.Value;
                    }

                    var currentValue = playlistItem.VideoBitrate ?? videoBitrate;

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
            var directPlay = profile.DirectPlayProfiles
                .FirstOrDefault(i => i.Type == DlnaProfileType.Video && IsVideoDirectPlaySupported(i, mediaSource, videoStream, audioStream));

            if (directPlay == null)
            {
                return null;
            }

            var container = mediaSource.Container;

            var conditions = profile.ContainerProfiles
                .Where(i => i.Type == DlnaProfileType.Video && i.GetContainers().Contains(container, StringComparer.OrdinalIgnoreCase))
                .SelectMany(i => i.Conditions);

            var conditionProcessor = new ConditionProcessor();

            var width = videoStream == null ? null : videoStream.Width;
            var height = videoStream == null ? null : videoStream.Height;
            var bitDepth = videoStream == null ? null : videoStream.BitDepth;
            var videoBitrate = videoStream == null ? null : videoStream.BitRate;
            var videoLevel = videoStream == null ? null : videoStream.Level;
            var videoProfile = videoStream == null ? null : videoStream.Profile;
            var videoFramerate = videoStream == null ? null : videoStream.AverageFrameRate ?? videoStream.AverageFrameRate;

            var audioBitrate = audioStream == null ? null : audioStream.BitRate;
            var audioChannels = audioStream == null ? null : audioStream.Channels;
            var audioProfile = audioStream == null ? null : audioStream.Profile;

            var timestamp = videoStream == null ? TransportStreamTimestamp.None : mediaSource.Timestamp;
            var packetLength = videoStream == null ? null : videoStream.PacketLength;

            // Check container conditions
            if (!conditions.All(i => conditionProcessor.IsVideoConditionSatisfied(i,
                audioBitrate,
                audioChannels,
                width,
                height,
                bitDepth,
                videoBitrate,
                videoProfile,
                videoLevel,
                videoFramerate,
                packetLength,
                timestamp)))
            {
                return null;
            }

            var videoCodec = videoStream == null ? null : videoStream.Codec;

            if (string.IsNullOrEmpty(videoCodec))
            {
                return null;
            }

            conditions = profile.CodecProfiles
               .Where(i => i.Type == CodecType.Video && i.ContainsCodec(videoCodec))
               .SelectMany(i => i.Conditions);

            if (!conditions.All(i => conditionProcessor.IsVideoConditionSatisfied(i,
                audioBitrate,
                audioChannels,
                width,
                height,
                bitDepth,
                videoBitrate,
                videoProfile,
                videoLevel,
                videoFramerate,
                packetLength,
                timestamp)))
            {
                return null;
            }

            if (audioStream != null)
            {
                var audioCodec = audioStream.Codec;

                if (string.IsNullOrEmpty(audioCodec))
                {
                    return null;
                }

                conditions = profile.CodecProfiles
                  .Where(i => i.Type == CodecType.VideoAudio && i.ContainsCodec(audioCodec))
                  .SelectMany(i => i.Conditions);

                if (!conditions.All(i => conditionProcessor.IsVideoAudioConditionSatisfied(i,
                  audioChannels,
                  audioBitrate,
                  audioProfile)))
                {
                    return null;
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

            if (options.AudioStreamIndex.HasValue &&
                item.MediaStreams.Count(i => i.Type == MediaStreamType.Audio) > 1)
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
            foreach (var condition in conditions
                .Where(i => !string.IsNullOrEmpty(i.Value)))
            {
                var value = condition.Value;

                switch (condition.Property)
                {
                    case ProfileConditionValue.AudioBitrate:
                        {
                            int num;
                            if (int.TryParse(value, NumberStyles.Any, _usCulture, out num))
                            {
                                item.AudioBitrate = num;
                            }
                            break;
                        }
                    case ProfileConditionValue.AudioChannels:
                        {
                            int num;
                            if (int.TryParse(value, NumberStyles.Any, _usCulture, out num))
                            {
                                item.MaxAudioChannels = num;
                            }
                            break;
                        }
                    case ProfileConditionValue.AudioProfile:
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
                            if (int.TryParse(value, NumberStyles.Any, _usCulture, out num))
                            {
                                item.MaxHeight = num;
                            }
                            break;
                        }
                    case ProfileConditionValue.VideoBitrate:
                        {
                            int num;
                            if (int.TryParse(value, NumberStyles.Any, _usCulture, out num))
                            {
                                item.VideoBitrate = num;
                            }
                            break;
                        }
                    case ProfileConditionValue.VideoFramerate:
                        {
                            int num;
                            if (int.TryParse(value, NumberStyles.Any, _usCulture, out num))
                            {
                                item.MaxFramerate = num;
                            }
                            break;
                        }
                    case ProfileConditionValue.VideoLevel:
                        {
                            int num;
                            if (int.TryParse(value, NumberStyles.Any, _usCulture, out num))
                            {
                                item.VideoLevel = num;
                            }
                            break;
                        }
                    case ProfileConditionValue.Width:
                        {
                            int num;
                            if (int.TryParse(value, NumberStyles.Any, _usCulture, out num))
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
                var mediaContainer = item.Container ?? string.Empty;
                if (!profile.GetContainers().Any(i => string.Equals(i, mediaContainer, StringComparison.OrdinalIgnoreCase)))
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
                var mediaContainer = item.Container ?? string.Empty;
                if (!profile.GetContainers().Any(i => string.Equals(i, mediaContainer, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }

            // Check video codec
            var videoCodecs = profile.GetVideoCodecs();
            if (videoCodecs.Count > 0)
            {
                var videoCodec = videoStream == null ? null : videoStream.Codec;
                if (string.IsNullOrEmpty(videoCodec) || !videoCodecs.Contains(videoCodec, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            var audioCodecs = profile.GetAudioCodecs();
            if (audioCodecs.Count > 0)
            {
                // Check audio codecs
                var audioCodec = audioStream == null ? null : audioStream.Codec;
                if (string.IsNullOrEmpty(audioCodec) || !audioCodecs.Contains(audioCodec, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
