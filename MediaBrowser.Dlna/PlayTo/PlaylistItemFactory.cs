using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MediaBrowser.Dlna.PlayTo
{
    public class PlaylistItemFactory
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public PlaylistItem Create(Audio item, List<MediaStream> mediaStreams, DeviceProfile profile)
        {
            var playlistItem = new PlaylistItem
            {
                ItemId = item.Id.ToString("N"),
                MediaType = DlnaProfileType.Audio
            };

            var audioStream = mediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Audio);

            var directPlay = profile.DirectPlayProfiles
                .FirstOrDefault(i => i.Type == playlistItem.MediaType && IsSupported(i, item, audioStream));

            if (directPlay != null)
            {
                var audioCodec = audioStream == null ? null : audioStream.Codec;

                // Make sure audio codec profiles are satisfied
                if (!string.IsNullOrEmpty(audioCodec) && profile.CodecProfiles.Where(i => i.Type == CodecType.AudioCodec && i.ContainsCodec(audioCodec))
                    .All(i => AreConditionsSatisfied(i.Conditions, item.Path, null, audioStream)))
                {
                    playlistItem.Transcode = false;
                    playlistItem.Container = Path.GetExtension(item.Path);

                    return playlistItem;
                }
            }

            var transcodingProfile = profile.TranscodingProfiles
                .FirstOrDefault(i => i.Type == playlistItem.MediaType && IsSupported(profile, i, item));

            if (transcodingProfile != null)
            {
                playlistItem.Transcode = true;
                playlistItem.TranscodingSettings = transcodingProfile.Settings.ToList();
                playlistItem.Container = "." + transcodingProfile.Container.TrimStart('.');
                playlistItem.AudioCodec = transcodingProfile.AudioCodec;

                var audioTranscodingConditions = profile.CodecProfiles
                    .Where(i => i.Type == CodecType.AudioCodec && i.ContainsCodec(transcodingProfile.AudioCodec))
                    .Take(1)
                    .SelectMany(i => i.Conditions);

                ApplyTranscodingConditions(playlistItem, audioTranscodingConditions);
            }

            return playlistItem;
        }

        public PlaylistItem Create(Photo item, DeviceProfile profile)
        {
            var playlistItem = new PlaylistItem
            {
                ItemId = item.Id.ToString("N"),
                MediaType = DlnaProfileType.Photo
            };

            var directPlay = profile.DirectPlayProfiles
                .FirstOrDefault(i => i.Type == playlistItem.MediaType && IsSupported(i, item));

            if (directPlay != null)
            {
                playlistItem.Transcode = false;
                playlistItem.Container = Path.GetExtension(item.Path);

                return playlistItem;
            }

            var transcodingProfile = profile.TranscodingProfiles
                .FirstOrDefault(i => i.Type == playlistItem.MediaType && IsSupported(profile, i, item));

            if (transcodingProfile != null)
            {
                playlistItem.Transcode = true;
                playlistItem.TranscodingSettings = transcodingProfile.Settings.ToList();
                playlistItem.Container = "." + transcodingProfile.Container.TrimStart('.');
            }

            return playlistItem;
        }

        public PlaylistItem Create(Video item, List<MediaStream> mediaStreams, DeviceProfile profile)
        {
            var playlistItem = new PlaylistItem
            {
                ItemId = item.Id.ToString("N"),
                MediaType = DlnaProfileType.Video
            };

            var audioStream = mediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Audio);
            var videoStream = mediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Video);

            var directPlay = profile.DirectPlayProfiles
                .FirstOrDefault(i => i.Type == playlistItem.MediaType && IsSupported(i, item, videoStream, audioStream));

            if (directPlay != null)
            {
                var videoCodec = videoStream == null ? null : videoStream.Codec;

                // Make sure video codec profiles are satisfied
                if (!string.IsNullOrEmpty(videoCodec) && profile.CodecProfiles.Where(i => i.Type == CodecType.VideoCodec && i.ContainsCodec(videoCodec))
                    .All(i => AreConditionsSatisfied(i.Conditions, item.Path, videoStream, audioStream)))
                {
                    var audioCodec = audioStream == null ? null : audioStream.Codec;

                    // Make sure audio codec profiles are satisfied
                    if (string.IsNullOrEmpty(audioCodec) || profile.CodecProfiles.Where(i => i.Type == CodecType.VideoAudioCodec && i.ContainsCodec(audioCodec))
                        .All(i => AreConditionsSatisfied(i.Conditions, item.Path, videoStream, audioStream)))
                    {
                        playlistItem.Transcode = false;
                        playlistItem.Container = Path.GetExtension(item.Path);

                        return playlistItem;
                    }
                }
            }

            var transcodingProfile = profile.TranscodingProfiles
                .FirstOrDefault(i => i.Type == playlistItem.MediaType && IsSupported(profile, i, item));

            if (transcodingProfile != null)
            {
                playlistItem.Transcode = true;
                playlistItem.TranscodingSettings = transcodingProfile.Settings.ToList();
                playlistItem.Container = "." + transcodingProfile.Container.TrimStart('.');
                playlistItem.AudioCodec = transcodingProfile.AudioCodec.Split(',').FirstOrDefault();
                playlistItem.VideoCodec = transcodingProfile.VideoCodec;

                var videoTranscodingConditions = profile.CodecProfiles
                    .Where(i => i.Type == CodecType.VideoCodec && i.ContainsCodec(transcodingProfile.VideoCodec))
                    .Take(1)
                    .SelectMany(i => i.Conditions);

                ApplyTranscodingConditions(playlistItem, videoTranscodingConditions);

                var audioTranscodingConditions = profile.CodecProfiles
                    .Where(i => i.Type == CodecType.VideoAudioCodec && i.ContainsCodec(transcodingProfile.AudioCodec))
                    .Take(1)
                    .SelectMany(i => i.Conditions);

                ApplyTranscodingConditions(playlistItem, audioTranscodingConditions);
            }

            return playlistItem;
        }

        private void ApplyTranscodingConditions(PlaylistItem item, IEnumerable<ProfileCondition> conditions)
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
                    case ProfileConditionValue.Filesize:
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

        private bool IsSupported(DirectPlayProfile profile, Photo item)
        {
            var mediaPath = item.Path;

            if (profile.Container.Length > 0)
            {
                // Check container type
                var mediaContainer = Path.GetExtension(mediaPath);
                if (!profile.GetContainers().Any(i => string.Equals("." + i.TrimStart('.'), mediaContainer, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsSupported(DirectPlayProfile profile, Audio item, MediaStream audioStream)
        {
            var mediaPath = item.Path;

            if (profile.Container.Length > 0)
            {
                // Check container type
                var mediaContainer = Path.GetExtension(mediaPath);
                if (!profile.GetContainers().Any(i => string.Equals("." + i.TrimStart('.'), mediaContainer, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsSupported(DirectPlayProfile profile, Video item, MediaStream videoStream, MediaStream audioStream)
        {
            if (item.VideoType != VideoType.VideoFile)
            {
                return false;
            }

            var mediaPath = item.Path;

            if (profile.Container.Length > 0)
            {
                // Check container type
                var mediaContainer = Path.GetExtension(mediaPath);
                if (!profile.GetContainers().Any(i => string.Equals("." + i.TrimStart('.'), mediaContainer, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }

            // Check video codec
            var videoCodecs = profile.GetVideoCodecs();
            if (videoCodecs.Count > 0)
            {
                var videoCodec = videoStream == null ? null : videoStream.Codec;
                if (string.IsNullOrWhiteSpace(videoCodec) || !videoCodecs.Contains(videoCodec, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            var audioCodecs = profile.GetAudioCodecs();
            if (audioCodecs.Count > 0)
            {
                // Check audio codecs
                var audioCodec = audioStream == null ? null : audioStream.Codec;
                if (string.IsNullOrWhiteSpace(audioCodec) || !audioCodecs.Contains(audioCodec, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsSupported(DeviceProfile profile, TranscodingProfile transcodingProfile, Audio item)
        {
            // Placeholder for future conditions
            return true;
        }

        private bool IsSupported(DeviceProfile profile, TranscodingProfile transcodingProfile, Photo item)
        {
            // Placeholder for future conditions
            return true;
        }

        private bool IsSupported(DeviceProfile profile, TranscodingProfile transcodingProfile, Video item)
        {
            // Placeholder for future conditions
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

                if (!string.IsNullOrWhiteSpace(profile))
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

                if (!string.IsNullOrWhiteSpace(profile))
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
                    long expected;
                    if (long.TryParse(condition.Value, NumberStyles.Any, _usCulture, out expected))
                    {
                        switch (condition.Condition)
                        {
                            case ProfileConditionType.Equals:
                                return actualValue.Value == expected;
                            case ProfileConditionType.GreaterThanEqual:
                                return actualValue.Value >= expected;
                            case ProfileConditionType.LessThanEqual:
                                return actualValue.Value <= expected;
                            case ProfileConditionType.NotEquals:
                                return actualValue.Value != expected;
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
        private long? GetConditionValue(ProfileCondition condition, string mediaPath, MediaStream videoStream, MediaStream audioStream)
        {
            switch (condition.Property)
            {
                case ProfileConditionValue.AudioBitrate:
                    return audioStream == null ? null : audioStream.BitRate;
                case ProfileConditionValue.AudioChannels:
                    return audioStream == null ? null : audioStream.Channels;
                case ProfileConditionValue.Filesize:
                    return new FileInfo(mediaPath).Length;
                case ProfileConditionValue.VideoBitrate:
                    return videoStream == null ? null : videoStream.BitRate;
                case ProfileConditionValue.VideoFramerate:
                    return videoStream == null ? null : (ConvertToLong(videoStream.AverageFrameRate ?? videoStream.RealFrameRate));
                case ProfileConditionValue.Height:
                    return videoStream == null ? null : videoStream.Height;
                case ProfileConditionValue.Width:
                    return videoStream == null ? null : videoStream.Width;
                case ProfileConditionValue.VideoLevel:
                    return videoStream == null ? null : ConvertToLong(videoStream.Level);
                default:
                    throw new InvalidOperationException("Unexpected Property");
            }
        }

        /// <summary>
        /// Converts to long.
        /// </summary>
        /// <param name="val">The value.</param>
        /// <returns>System.Nullable{System.Int64}.</returns>
        private long? ConvertToLong(float? val)
        {
            return val.HasValue ? Convert.ToInt64(val.Value) : (long?)null;
        }

        /// <summary>
        /// Converts to long.
        /// </summary>
        /// <param name="val">The value.</param>
        /// <returns>System.Nullable{System.Int64}.</returns>
        private long? ConvertToLong(double? val)
        {
            return val.HasValue ? Convert.ToInt64(val.Value) : (long?)null;
        }
    }
}
