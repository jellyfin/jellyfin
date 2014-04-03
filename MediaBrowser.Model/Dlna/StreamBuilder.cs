using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
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

            var streams = mediaSources.Select(i => BuildAudioItem(options.ItemId, i, options.Profile)).ToList();

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

        private StreamInfo BuildAudioItem(string itemId, MediaSourceInfo item, DeviceProfile profile)
        {
            var playlistItem = new StreamInfo
            {
                ItemId = itemId,
                MediaType = DlnaProfileType.Audio,
                MediaSourceId = item.Id
            };

            var audioStream = item.MediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Audio);

            var directPlay = profile.DirectPlayProfiles
                .FirstOrDefault(i => i.Type == playlistItem.MediaType && IsAudioProfileSupported(i, item, audioStream));

            if (directPlay != null)
            {
                var audioCodec = audioStream == null ? null : audioStream.Codec;

                // Make sure audio codec profiles are satisfied
                if (!string.IsNullOrEmpty(audioCodec) && profile.CodecProfiles.Where(i => i.Type == CodecType.Audio && i.ContainsCodec(audioCodec))
                    .All(i => AreConditionsSatisfied(i.Conditions, item.Path, null, audioStream)))
                {
                    playlistItem.IsDirectStream = true;
                    playlistItem.Container = item.Container;

                    return playlistItem;
                }
            }

            var transcodingProfile = profile.TranscodingProfiles
                .FirstOrDefault(i => i.Type == playlistItem.MediaType);

            if (transcodingProfile != null)
            {
                playlistItem.IsDirectStream = false;
                playlistItem.Container = transcodingProfile.Container;
                playlistItem.AudioCodec = transcodingProfile.AudioCodec;

                var audioTranscodingConditions = profile.CodecProfiles
                    .Where(i => i.Type == CodecType.Audio && i.ContainsCodec(transcodingProfile.AudioCodec))
                    .Take(1)
                    .SelectMany(i => i.Conditions);

                ApplyTranscodingConditions(playlistItem, audioTranscodingConditions);
            }

            return playlistItem;
        }

        private StreamInfo BuildVideoItem(MediaSourceInfo item, VideoOptions options)
        {
            var playlistItem = new StreamInfo
            {
                ItemId = options.ItemId,
                MediaType = DlnaProfileType.Video,
                MediaSourceId = item.Id
            };

            var audioStream = item.MediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Audio);
            var videoStream = item.MediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Video);

            if (IsEligibleForDirectPlay(item, options))
            {
                // See if it can be direct played
                var directPlay = options.Profile.DirectPlayProfiles
                    .FirstOrDefault(i => i.Type == playlistItem.MediaType && IsVideoProfileSupported(i, item, videoStream, audioStream));

                if (directPlay != null)
                {
                    var videoCodec = videoStream == null ? null : videoStream.Codec;

                    // Make sure video codec profiles are satisfied
                    if (!string.IsNullOrEmpty(videoCodec) && options.Profile.CodecProfiles.Where(i => i.Type == CodecType.Video && i.ContainsCodec(videoCodec))
                        .All(i => AreConditionsSatisfied(i.Conditions, item.Path, videoStream, audioStream)))
                    {
                        var audioCodec = audioStream == null ? null : audioStream.Codec;

                        // Make sure audio codec profiles are satisfied
                        if (string.IsNullOrEmpty(audioCodec) || options.Profile.CodecProfiles.Where(i => i.Type == CodecType.VideoAudio && i.ContainsCodec(audioCodec))
                            .All(i => AreConditionsSatisfied(i.Conditions, item.Path, videoStream, audioStream)))
                        {
                            playlistItem.IsDirectStream = true;
                            playlistItem.Container = item.Container;

                            return playlistItem;
                        }
                    }
                }
            }

            // Can't direct play, find the transcoding profile
            var transcodingProfile = options.Profile.TranscodingProfiles
                .FirstOrDefault(i => i.Type == playlistItem.MediaType);

            if (transcodingProfile != null)
            {
                playlistItem.IsDirectStream = false;
                playlistItem.Container = transcodingProfile.Container;
                playlistItem.AudioCodec = transcodingProfile.AudioCodec.Split(',').FirstOrDefault();
                playlistItem.VideoCodec = transcodingProfile.VideoCodec;

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
            }

            return playlistItem;
        }

        private bool IsEligibleForDirectPlay(MediaSourceInfo item, VideoOptions options)
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
                    case ProfileConditionValue.VideoBitDepth:
                    case ProfileConditionValue.VideoProfile:
                        {
                            // Not supported yet
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

        private bool IsAudioProfileSupported(DirectPlayProfile profile, MediaSourceInfo item, MediaStream audioStream)
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

        private bool IsVideoProfileSupported(DirectPlayProfile profile, MediaSourceInfo item, MediaStream videoStream, MediaStream audioStream)
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

        private bool AreConditionsSatisfied(IEnumerable<ProfileCondition> conditions, string mediaPath, MediaStream videoStream, MediaStream audioStream)
        {
            return conditions.All(i => IsConditionSatisfied(i, mediaPath, videoStream, audioStream));
        }

        /// <summary>
        /// Determines whether [is condition satisfied] [the specified condition].
        /// </summary>
        /// <param name="condition">The condition.</param>
        /// <param name="mediaPath">The media path.</param>
        /// <param name="videoStream">The video stream.</param>
        /// <param name="audioStream">The audio stream.</param>
        /// <returns><c>true</c> if [is condition satisfied] [the specified condition]; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.InvalidOperationException">Unexpected ProfileConditionType</exception>
        private bool IsConditionSatisfied(ProfileCondition condition, string mediaPath, MediaStream videoStream, MediaStream audioStream)
        {
            if (condition.Property == ProfileConditionValue.Has64BitOffsets)
            {
                // TODO: Determine how to evaluate this
            }

            if (condition.Property == ProfileConditionValue.VideoProfile)
            {
                var profile = videoStream == null ? null : videoStream.Profile;

                if (!string.IsNullOrEmpty(profile))
                {
                    switch (condition.Condition)
                    {
                        case ProfileConditionType.Equals:
                            return string.Equals(profile, condition.Value, StringComparison.OrdinalIgnoreCase);
                        case ProfileConditionType.NotEquals:
                            return !string.Equals(profile, condition.Value, StringComparison.OrdinalIgnoreCase);
                        default:
                            throw new InvalidOperationException("Unexpected ProfileConditionType");
                    }
                }
            }

            else if (condition.Property == ProfileConditionValue.AudioProfile)
            {
                var profile = audioStream == null ? null : audioStream.Profile;

                if (!string.IsNullOrEmpty(profile))
                {
                    switch (condition.Condition)
                    {
                        case ProfileConditionType.Equals:
                            return string.Equals(profile, condition.Value, StringComparison.OrdinalIgnoreCase);
                        case ProfileConditionType.NotEquals:
                            return !string.Equals(profile, condition.Value, StringComparison.OrdinalIgnoreCase);
                        default:
                            throw new InvalidOperationException("Unexpected ProfileConditionType");
                    }
                }
            }

            else
            {
                var actualValue = GetConditionValue(condition, mediaPath, videoStream, audioStream);

                if (actualValue.HasValue)
                {
                    double expected;
                    if (double.TryParse(condition.Value, NumberStyles.Any, _usCulture, out expected))
                    {
                        switch (condition.Condition)
                        {
                            case ProfileConditionType.Equals:
                                return actualValue.Value.Equals(expected);
                            case ProfileConditionType.GreaterThanEqual:
                                return actualValue.Value >= expected;
                            case ProfileConditionType.LessThanEqual:
                                return actualValue.Value <= expected;
                            case ProfileConditionType.NotEquals:
                                return !actualValue.Value.Equals(expected);
                            default:
                                throw new InvalidOperationException("Unexpected ProfileConditionType");
                        }
                    }
                }
            }

            // Value doesn't exist in metadata. Fail it if required.
            return !condition.IsRequired;
        }

        /// <summary>
        /// Gets the condition value.
        /// </summary>
        /// <param name="condition">The condition.</param>
        /// <param name="mediaPath">The media path.</param>
        /// <param name="videoStream">The video stream.</param>
        /// <param name="audioStream">The audio stream.</param>
        /// <returns>System.Nullable{System.Int64}.</returns>
        /// <exception cref="System.InvalidOperationException">Unexpected Property</exception>
        private double? GetConditionValue(ProfileCondition condition, string mediaPath, MediaStream videoStream, MediaStream audioStream)
        {
            switch (condition.Property)
            {
                case ProfileConditionValue.AudioBitrate:
                    return audioStream == null ? null : audioStream.BitRate;
                case ProfileConditionValue.AudioChannels:
                    return audioStream == null ? null : audioStream.Channels;
                case ProfileConditionValue.VideoBitrate:
                    return videoStream == null ? null : videoStream.BitRate;
                case ProfileConditionValue.VideoFramerate:
                    return videoStream == null ? null : (videoStream.AverageFrameRate ?? videoStream.RealFrameRate);
                case ProfileConditionValue.Height:
                    return videoStream == null ? null : videoStream.Height;
                case ProfileConditionValue.Width:
                    return videoStream == null ? null : videoStream.Width;
                case ProfileConditionValue.VideoLevel:
                    return videoStream == null ? null : videoStream.Level;
                case ProfileConditionValue.VideoBitDepth:
                    return videoStream == null ? null : GetBitDepth(videoStream);
                default:
                    throw new InvalidOperationException("Unexpected Property");
            }
        }

        private int? GetBitDepth(MediaStream videoStream)
        {
            var eightBit = new List<string>
            {
                "yuv420p",
                "yuv411p",
                "yuvj420p",
                "uyyvyy411",
                "nv12",
                "nv21",
                "rgb444le",
                "rgb444be",
                "bgr444le",
                "bgr444be",
                "yuvj411p"            
            };

            if (!string.IsNullOrEmpty(videoStream.PixelFormat))
            {
                if (eightBit.Contains(videoStream.PixelFormat, StringComparer.OrdinalIgnoreCase))
                {
                    return 8;
                }
            }

            return null;
        }
    }

}
