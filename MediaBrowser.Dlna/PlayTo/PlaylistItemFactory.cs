using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MediaBrowser.Dlna.PlayTo
{
    public class PlaylistItemFactory
    {
        private readonly IItemRepository _itemRepo;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public PlaylistItemFactory(IItemRepository itemRepo)
        {
            _itemRepo = itemRepo;
        }

        public PlaylistItem Create(Audio item, DeviceProfile profile)
        {
            var playlistItem = new PlaylistItem
            {
                ItemId = item.Id.ToString("N"),
                MediaType = DlnaProfileType.Audio
            };

            var mediaStreams = _itemRepo.GetMediaStreams(new MediaStreamQuery
            {
                ItemId = item.Id,
                Type = MediaStreamType.Audio
            });

            var audioStream = mediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Audio);

            var directPlay = profile.DirectPlayProfiles
                .FirstOrDefault(i => i.Type == playlistItem.MediaType && IsSupported(i, item, audioStream));

            if (directPlay != null)
            {
                playlistItem.Transcode = false;
                playlistItem.FileFormat = Path.GetExtension(item.Path);
                playlistItem.MimeType = directPlay.MimeType;

                return playlistItem;
            }

            var transcodingProfile = profile.TranscodingProfiles
                .FirstOrDefault(i => i.Type == playlistItem.MediaType && IsSupported(profile, i, item));

            if (transcodingProfile != null)
            {
                playlistItem.Transcode = true;

                playlistItem.FileFormat = "." + transcodingProfile.Container.TrimStart('.');
                playlistItem.MimeType = transcodingProfile.MimeType;
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
                playlistItem.FileFormat = Path.GetExtension(item.Path);
                playlistItem.MimeType = directPlay.MimeType;

                return playlistItem;
            }

            var transcodingProfile = profile.TranscodingProfiles
                .FirstOrDefault(i => i.Type == playlistItem.MediaType && IsSupported(profile, i, item));

            if (transcodingProfile != null)
            {
                playlistItem.Transcode = true;

                playlistItem.FileFormat = "." + transcodingProfile.Container.TrimStart('.');
                playlistItem.MimeType = transcodingProfile.MimeType;
            }

            return playlistItem;
        }

        public PlaylistItem Create(Video item, DeviceProfile profile)
        {
            var playlistItem = new PlaylistItem
            {
                ItemId = item.Id.ToString("N"),
                MediaType = DlnaProfileType.Video
            };

            var mediaStreams = _itemRepo.GetMediaStreams(new MediaStreamQuery
            {
                ItemId = item.Id

            }).ToList();

            var audioStream = mediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Audio);
            var videoStream = mediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Video);

            var directPlay = profile.DirectPlayProfiles
                .FirstOrDefault(i => i.Type == playlistItem.MediaType && IsSupported(i, item, videoStream, audioStream));

            if (directPlay != null)
            {
                playlistItem.Transcode = false;
                playlistItem.FileFormat = Path.GetExtension(item.Path);
                playlistItem.MimeType = directPlay.MimeType;

                return playlistItem;
            }

            var transcodingProfile = profile.TranscodingProfiles
                .FirstOrDefault(i => i.Type == playlistItem.MediaType && IsSupported(profile, i, item));

            if (transcodingProfile != null)
            {
                playlistItem.Transcode = true;

                playlistItem.FileFormat = "." + transcodingProfile.Container.TrimStart('.');
                playlistItem.MimeType = transcodingProfile.MimeType;
            }

            return playlistItem;
        }

        private bool IsSupported(DirectPlayProfile profile, Photo item)
        {
            var mediaPath = item.Path;

            var mediaContainer = Path.GetExtension(mediaPath);

            if (!profile.Containers.Any(i => string.Equals("." + i.TrimStart('.'), mediaContainer, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            if (!profile.Conditions.Any(i => IsConditionSatisfied(i, mediaPath, null, null)))
            {
                return false;
            }

            return true;
        }
        
        private bool IsSupported(DirectPlayProfile profile, Audio item, MediaStream audioStream)
        {
            var mediaPath = item.Path;

            var mediaContainer = Path.GetExtension(mediaPath);

            if (!profile.Containers.Any(i => string.Equals("." + i.TrimStart('.'), mediaContainer, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            if (!profile.Conditions.Any(i => IsConditionSatisfied(i, mediaPath, null, audioStream)))
            {
                return false;
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

            var mediaContainer = Path.GetExtension(mediaPath);

            if (!profile.Containers.Any(i => string.Equals("." + i.TrimStart('.'), mediaContainer, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            if (!profile.Conditions.Any(i => IsConditionSatisfied(i, mediaPath, videoStream, audioStream)))
            {
                return false;
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

            return false;
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
                case ProfileConditionValue.VideoHeight:
                    return videoStream == null ? null : videoStream.Height;
                case ProfileConditionValue.VideoWidth:
                    return videoStream == null ? null : videoStream.Width;
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
    }
}
