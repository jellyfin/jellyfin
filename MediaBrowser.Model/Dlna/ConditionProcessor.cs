#pragma warning disable CS1591

using System;
using System.Linq;
using System.Globalization;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Model.Dlna
{
    public static class ConditionProcessor
    {
        public static bool IsVideoConditionSatisfied(
            ProfileCondition condition,
            int? width,
            int? height,
            int? videoBitDepth,
            int? videoBitrate,
            string? videoProfile,
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

            if (int.TryParse(condition.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var expected))
            {
                switch (condition.Condition)
                {
                    case ProfileConditionType.Equals:
                    case ProfileConditionType.EqualsAny:
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
                    return expected.Split('|').Contains(currentValue, StringComparer.OrdinalIgnoreCase);
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

            if (double.TryParse(condition.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var expected))
            {
                switch (condition.Condition)
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
    }
}
