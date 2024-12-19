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
        /// <param name="isAnamorphic">A value indicating whether the video is anamorphic.</param>
        /// <param name="isInterlaced">A value indicating whether the video is interlaced.</param>
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
            switch (condition.Property)
            {
                case ProfileConditionValue.IsInterlaced:
                    return IsConditionSatisfied(condition, isInterlaced);
                case ProfileConditionValue.IsAnamorphic:
                    return IsConditionSatisfied(condition, isAnamorphic);
                case ProfileConditionValue.IsAvc:
                    return IsConditionSatisfied(condition, isAvc);
                case ProfileConditionValue.VideoFramerate:
                    return IsConditionSatisfied(condition, videoFramerate);
                case ProfileConditionValue.VideoLevel:
                    return IsConditionSatisfied(condition, videoLevel);
                case ProfileConditionValue.VideoProfile:
                    return IsConditionSatisfied(condition, videoProfile);
                case ProfileConditionValue.VideoRangeType:
                    return IsConditionSatisfied(condition, videoRangeType);
                case ProfileConditionValue.VideoCodecTag:
                    return IsConditionSatisfied(condition, videoCodecTag);
                case ProfileConditionValue.PacketLength:
                    return IsConditionSatisfied(condition, packetLength);
                case ProfileConditionValue.VideoBitDepth:
                    return IsConditionSatisfied(condition, videoBitDepth);
                case ProfileConditionValue.VideoBitrate:
                    return IsConditionSatisfied(condition, videoBitrate);
                case ProfileConditionValue.Height:
                    return IsConditionSatisfied(condition, height);
                case ProfileConditionValue.Width:
                    return IsConditionSatisfied(condition, width);
                case ProfileConditionValue.RefFrames:
                    return IsConditionSatisfied(condition, refFrames);
                case ProfileConditionValue.NumAudioStreams:
                    return IsConditionSatisfied(condition, numAudioStreams);
                case ProfileConditionValue.NumVideoStreams:
                    return IsConditionSatisfied(condition, numVideoStreams);
                case ProfileConditionValue.VideoTimestamp:
                    return IsConditionSatisfied(condition, timestamp);
                default:
                    return true;
            }
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
            switch (condition.Property)
            {
                case ProfileConditionValue.Height:
                    return IsConditionSatisfied(condition, height);
                case ProfileConditionValue.Width:
                    return IsConditionSatisfied(condition, width);
                default:
                    throw new ArgumentException("Unexpected condition on image file: " + condition.Property);
            }
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
            switch (condition.Property)
            {
                case ProfileConditionValue.AudioBitrate:
                    return IsConditionSatisfied(condition, audioBitrate);
                case ProfileConditionValue.AudioChannels:
                    return IsConditionSatisfied(condition, audioChannels);
                case ProfileConditionValue.AudioSampleRate:
                    return IsConditionSatisfied(condition, audioSampleRate);
                case ProfileConditionValue.AudioBitDepth:
                    return IsConditionSatisfied(condition, audioBitDepth);
                default:
                    throw new ArgumentException("Unexpected condition on audio file: " + condition.Property);
            }
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
            switch (condition.Property)
            {
                case ProfileConditionValue.AudioProfile:
                    return IsConditionSatisfied(condition, audioProfile);
                case ProfileConditionValue.AudioBitrate:
                    return IsConditionSatisfied(condition, audioBitrate);
                case ProfileConditionValue.AudioChannels:
                    return IsConditionSatisfied(condition, audioChannels);
                case ProfileConditionValue.IsSecondaryAudio:
                    return IsConditionSatisfied(condition, isSecondaryTrack);
                case ProfileConditionValue.AudioSampleRate:
                    return IsConditionSatisfied(condition, audioSampleRate);
                case ProfileConditionValue.AudioBitDepth:
                    return IsConditionSatisfied(condition, audioBitDepth);
                default:
                    throw new ArgumentException("Unexpected condition on audio file: " + condition.Property);
            }
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
                switch (conditionType)
                {
                    case ProfileConditionType.Equals:
                        return currentValue.Value.Equals(expected);
                    case ProfileConditionType.GreaterThanEqual:
                        return currentValue.Value >= expected;
                    case ProfileConditionType.LessThanEqual:
                        return currentValue.Value <= expected;
                    case ProfileConditionType.NotEquals:
                        return !currentValue.Value.Equals(expected);
                    default:
                        throw new InvalidOperationException("Unexpected ProfileConditionType: " + condition.Condition);
                }
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

            switch (condition.Condition)
            {
                case ProfileConditionType.EqualsAny:
                    return expected.Split('|').Contains(currentValue, StringComparison.OrdinalIgnoreCase);
                case ProfileConditionType.Equals:
                    return string.Equals(currentValue, expected, StringComparison.OrdinalIgnoreCase);
                case ProfileConditionType.NotEquals:
                    return !string.Equals(currentValue, expected, StringComparison.OrdinalIgnoreCase);
                default:
                    throw new InvalidOperationException("Unexpected ProfileConditionType: " + condition.Condition);
            }
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
                switch (condition.Condition)
                {
                    case ProfileConditionType.Equals:
                        return currentValue.Value == expected;
                    case ProfileConditionType.NotEquals:
                        return currentValue.Value != expected;
                    default:
                        throw new InvalidOperationException("Unexpected ProfileConditionType: " + condition.Condition);
                }
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
                switch (conditionType)
                {
                    case ProfileConditionType.Equals:
                        return currentValue.Value.Equals(expected);
                    case ProfileConditionType.GreaterThanEqual:
                        return currentValue.Value >= expected;
                    case ProfileConditionType.LessThanEqual:
                        return currentValue.Value <= expected;
                    case ProfileConditionType.NotEquals:
                        return !currentValue.Value.Equals(expected);
                    default:
                        throw new InvalidOperationException("Unexpected ProfileConditionType: " + condition.Condition);
                }
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

            switch (condition.Condition)
            {
                case ProfileConditionType.Equals:
                    return timestamp == expected;
                case ProfileConditionType.NotEquals:
                    return timestamp != expected;
                default:
                    throw new InvalidOperationException("Unexpected ProfileConditionType: " + condition.Condition);
            }
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
