using System;
using System.Globalization;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// The condition processor.
    /// </summary>
    public static class ConditionProcessor
    {
        /// <summary>
        /// Checks if a video condition is satisfied.
        /// </summary>
        /// <param name="condition">The <see cref="ProfileCondition"/>.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="videoBitDepth">The bit depth.</param>
        /// <param name="videoBitrate">The bitrate.</param>
        /// <param name="videoProfile">The video profile.</param>
        /// <param name="videoRangeType">The <see cref="VideoRangeType"/>.</param>
        /// <param name="videoLevel">The video level.</param>
        /// <param name="videoFramerate">The framerate.</param>
        /// <param name="packetLength">The packet length.</param>
        /// <param name="timestamp">The <see cref="TransportStreamTimestamp"/>.</param>
        /// <param name="isAnamorphic">A value indicating whether tthe video is anamorphic.</param>
        /// <param name="isInterlaced">A value indicating whether tthe video is interlaced.</param>
        /// <param name="refFrames">The reference frames.</param>
        /// <param name="numVideoStreams">The number of video streams.</param>
        /// <param name="numAudioStreams">The number of audio streams.</param>
        /// <param name="videoCodecTag">The video codec tag.</param>
        /// <param name="isAvc">A value indicating whether the video is AVC.</param>
        /// <returns><b>True</b> if the condition is satisfied.</returns>
        public static bool IsVideoConditionSatisfied(
            ProfileCondition condition,
            int? width,
            int? height,
            int? videoBitDepth,
            int? videoBitrate,
            string? videoProfile,
            VideoRangeType? videoRangeType,
            double? videoLevel,
            float? videoFramerate,
            int? packetLength,
            TransportStreamTimestamp? timestamp,
            bool? isAnamorphic,
            bool? isInterlaced,
            int? refFrames,
            int? numVideoStreams,
            int? numAudioStreams,
            string? videoCodecTag,
            bool? isAvc)
        {
            return condition.Property switch
            {
                ProfileConditionValue.IsInterlaced => IsConditionSatisfied(condition, isInterlaced),
                ProfileConditionValue.IsAnamorphic => IsConditionSatisfied(condition, isAnamorphic),
                ProfileConditionValue.IsAvc => IsConditionSatisfied(condition, isAvc),
                ProfileConditionValue.VideoFramerate => IsConditionSatisfied(condition, videoFramerate),
                ProfileConditionValue.VideoLevel => IsConditionSatisfied(condition, videoLevel),
                ProfileConditionValue.VideoProfile => IsConditionSatisfied(condition, videoProfile),
                ProfileConditionValue.VideoRangeType => IsConditionSatisfied(condition, videoRangeType),
                ProfileConditionValue.VideoCodecTag => IsConditionSatisfied(condition, videoCodecTag),
                ProfileConditionValue.PacketLength => IsConditionSatisfied(condition, packetLength),
                ProfileConditionValue.VideoBitDepth => IsConditionSatisfied(condition, videoBitDepth),
                ProfileConditionValue.VideoBitrate => IsConditionSatisfied(condition, videoBitrate),
                ProfileConditionValue.Height => IsConditionSatisfied(condition, height),
                ProfileConditionValue.Width => IsConditionSatisfied(condition, width),
                ProfileConditionValue.RefFrames => IsConditionSatisfied(condition, refFrames),
                ProfileConditionValue.NumAudioStreams => IsConditionSatisfied(condition, numAudioStreams),
                ProfileConditionValue.NumVideoStreams => IsConditionSatisfied(condition, numVideoStreams),
                ProfileConditionValue.VideoTimestamp => IsConditionSatisfied(condition, timestamp),
                _ => true,
            };
        }

        /// <summary>
        /// Checks if a image condition is satisfied.
        /// </summary>
        /// <param name="condition">The <see cref="ProfileCondition"/>.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <returns><b>True</b> if the condition is satisfied.</returns>
        public static bool IsImageConditionSatisfied(ProfileCondition condition, int? width, int? height)
        {
            return condition.Property switch
            {
                ProfileConditionValue.Height => IsConditionSatisfied(condition, height),
                ProfileConditionValue.Width => IsConditionSatisfied(condition, width),
                _ => throw new ArgumentException("Unexpected condition on image file: " + condition.Property),
            };
        }

        /// <summary>
        /// Checks if an audio condition is satisfied.
        /// </summary>
        /// <param name="condition">The <see cref="ProfileCondition"/>.</param>
        /// <param name="audioChannels">The channel count.</param>
        /// <param name="audioBitrate">The bitrate.</param>
        /// <param name="audioSampleRate">The sample rate.</param>
        /// <param name="audioBitDepth">The bit depth.</param>
        /// <returns><b>True</b> if the condition is satisfied.</returns>
        public static bool IsAudioConditionSatisfied(ProfileCondition condition, int? audioChannels, int? audioBitrate, int? audioSampleRate, int? audioBitDepth)
        {
            return condition.Property switch
            {
                ProfileConditionValue.AudioBitrate => IsConditionSatisfied(condition, audioBitrate),
                ProfileConditionValue.AudioChannels => IsConditionSatisfied(condition, audioChannels),
                ProfileConditionValue.AudioSampleRate => IsConditionSatisfied(condition, audioSampleRate),
                ProfileConditionValue.AudioBitDepth => IsConditionSatisfied(condition, audioBitDepth),
                _ => throw new ArgumentException("Unexpected condition on audio file: " + condition.Property),
            };
        }

        /// <summary>
        /// Checks if an audio condition is satisfied for a video.
        /// </summary>
        /// <param name="condition">The <see cref="ProfileCondition"/>.</param>
        /// <param name="audioChannels">The channel count.</param>
        /// <param name="audioBitrate">The bitrate.</param>
        /// <param name="audioSampleRate">The sample rate.</param>
        /// <param name="audioBitDepth">The bit depth.</param>
        /// <param name="audioProfile">The profile.</param>
        /// <param name="isSecondaryTrack">A value indicating whether the audio is a secondary track.</param>
        /// <returns><b>True</b> if the condition is satisfied.</returns>
        public static bool IsVideoAudioConditionSatisfied(
            ProfileCondition condition,
            int? audioChannels,
            int? audioBitrate,
            int? audioSampleRate,
            int? audioBitDepth,
            string? audioProfile,
            bool? isSecondaryTrack)
        {
            return condition.Property switch
            {
                ProfileConditionValue.AudioProfile => IsConditionSatisfied(condition, audioProfile),
                ProfileConditionValue.AudioBitrate => IsConditionSatisfied(condition, audioBitrate),
                ProfileConditionValue.AudioChannels => IsConditionSatisfied(condition, audioChannels),
                ProfileConditionValue.IsSecondaryAudio => IsConditionSatisfied(condition, isSecondaryTrack),
                ProfileConditionValue.AudioSampleRate => IsConditionSatisfied(condition, audioSampleRate),
                ProfileConditionValue.AudioBitDepth => IsConditionSatisfied(condition, audioBitDepth),
                _ => throw new ArgumentException("Unexpected condition on audio file: " + condition.Property),
            };
        }

        private static bool IsConditionSatisfied(ProfileCondition condition, int? currentValue)
        {
            if (!currentValue.HasValue)
            {
                // If the value is unknown, it satisfies if not marked as required
                return !condition.IsRequired;
            }

            var conditionType = condition.Condition;
            if (condition.Condition == ProfileConditionType.EqualsAny)
            {
                foreach (var singleConditionString in condition.Value.AsSpan().Split('|'))
                {
                    if (int.TryParse(singleConditionString, NumberStyles.Integer, CultureInfo.InvariantCulture, out int conditionValue)
                        && conditionValue.Equals(currentValue))
                    {
                        return true;
                    }
                }

                return false;
            }

            if (int.TryParse(condition.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var expected))
            {
                return conditionType switch
                {
                    ProfileConditionType.Equals => currentValue.Value.Equals(expected),
                    ProfileConditionType.GreaterThanEqual => currentValue.Value >= expected,
                    ProfileConditionType.LessThanEqual => currentValue.Value <= expected,
                    ProfileConditionType.NotEquals => !currentValue.Value.Equals(expected),
                    _ => throw new InvalidOperationException("Unexpected ProfileConditionType: " + condition.Condition),
                };
            }

            return false;
        }

        private static bool IsConditionSatisfied(ProfileCondition condition, string? currentValue)
        {
            if (string.IsNullOrEmpty(currentValue))
            {
                // If the value is unknown, it satisfies if not marked as required
                return !condition.IsRequired;
            }

            string expected = condition.Value;

            return condition.Condition switch
            {
                ProfileConditionType.EqualsAny => expected.Split('|').Contains(currentValue, StringComparison.OrdinalIgnoreCase),
                ProfileConditionType.Equals => string.Equals(currentValue, expected, StringComparison.OrdinalIgnoreCase),
                ProfileConditionType.NotEquals => !string.Equals(currentValue, expected, StringComparison.OrdinalIgnoreCase),
                _ => throw new InvalidOperationException("Unexpected ProfileConditionType: " + condition.Condition),
            };
        }

        private static bool IsConditionSatisfied(ProfileCondition condition, bool? currentValue)
        {
            if (!currentValue.HasValue)
            {
                // If the value is unknown, it satisfies if not marked as required
                return !condition.IsRequired;
            }

            if (bool.TryParse(condition.Value, out var expected))
            {
                return condition.Condition switch
                {
                    ProfileConditionType.Equals => currentValue.Value == expected,
                    ProfileConditionType.NotEquals => currentValue.Value != expected,
                    _ => throw new InvalidOperationException("Unexpected ProfileConditionType: " + condition.Condition),
                };
            }

            return false;
        }

        private static bool IsConditionSatisfied(ProfileCondition condition, double? currentValue)
        {
            if (!currentValue.HasValue)
            {
                // If the value is unknown, it satisfies if not marked as required
                return !condition.IsRequired;
            }

            var conditionType = condition.Condition;
            if (condition.Condition == ProfileConditionType.EqualsAny)
            {
                foreach (var singleConditionString in condition.Value.AsSpan().Split('|'))
                {
                    if (double.TryParse(singleConditionString, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double conditionValue)
                        && conditionValue.Equals(currentValue))
                    {
                        return true;
                    }
                }

                return false;
            }

            if (double.TryParse(condition.Value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var expected))
            {
                return conditionType switch
                {
                    ProfileConditionType.Equals => currentValue.Value.Equals(expected),
                    ProfileConditionType.GreaterThanEqual => currentValue.Value >= expected,
                    ProfileConditionType.LessThanEqual => currentValue.Value <= expected,
                    ProfileConditionType.NotEquals => !currentValue.Value.Equals(expected),
                    _ => throw new InvalidOperationException("Unexpected ProfileConditionType: " + condition.Condition),
                };
            }

            return false;
        }

        private static bool IsConditionSatisfied(ProfileCondition condition, TransportStreamTimestamp? timestamp)
        {
            if (!timestamp.HasValue)
            {
                // If the value is unknown, it satisfies if not marked as required
                return !condition.IsRequired;
            }

            var expected = (TransportStreamTimestamp)Enum.Parse(typeof(TransportStreamTimestamp), condition.Value, true);

            return condition.Condition switch
            {
                ProfileConditionType.Equals => timestamp == expected,
                ProfileConditionType.NotEquals => timestamp != expected,
                _ => throw new InvalidOperationException("Unexpected ProfileConditionType: " + condition.Condition),
            };
        }

        private static bool IsConditionSatisfied(ProfileCondition condition, VideoRangeType? currentValue)
        {
            if (!currentValue.HasValue || currentValue.Equals(VideoRangeType.Unknown))
            {
                // If the value is unknown, it satisfies if not marked as required
                return !condition.IsRequired;
            }

            var conditionType = condition.Condition;
            if (conditionType == ProfileConditionType.EqualsAny)
            {
                foreach (var singleConditionString in condition.Value.AsSpan().Split('|'))
                {
                    if (Enum.TryParse(singleConditionString, true, out VideoRangeType conditionValue)
                        && conditionValue.Equals(currentValue))
                    {
                        return true;
                    }
                }

                return false;
            }

            if (Enum.TryParse(condition.Value, true, out VideoRangeType expected))
            {
                return conditionType switch
                {
                    ProfileConditionType.Equals => currentValue.Value == expected,
                    ProfileConditionType.NotEquals => currentValue.Value != expected,
                    _ => throw new InvalidOperationException("Unexpected ProfileConditionType: " + condition.Condition)
                };
            }

            return false;
        }
    }
}
