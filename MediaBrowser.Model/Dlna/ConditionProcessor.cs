using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.MediaInfo;
using System;

namespace MediaBrowser.Model.Dlna
{
    public class ConditionProcessor
    {
        public bool IsVideoConditionSatisfied(ProfileCondition condition,
            int? audioBitrate,
            int? audioChannels,
            int? width,
            int? height,
            int? bitDepth,
            int? videoBitrate,
            string videoProfile,
            double? videoLevel,
            double? videoFramerate,
            int? packetLength,
            TransportStreamTimestamp? timestamp)
        {
            switch (condition.Property)
            {
                case ProfileConditionValue.AudioProfile:
                    // TODO: Implement
                    return true;
                case ProfileConditionValue.Has64BitOffsets:
                    // TODO: Implement
                    return true;
                case ProfileConditionValue.VideoFramerate:
                    return IsConditionSatisfied(condition, videoFramerate);
                case ProfileConditionValue.VideoLevel:
                    return IsConditionSatisfied(condition, videoLevel);
                case ProfileConditionValue.VideoProfile:
                    return IsConditionSatisfied(condition, videoProfile);
                case ProfileConditionValue.PacketLength:
                    return IsConditionSatisfied(condition, packetLength);
                case ProfileConditionValue.AudioBitrate:
                    return IsConditionSatisfied(condition, audioBitrate);
                case ProfileConditionValue.AudioChannels:
                    return IsConditionSatisfied(condition, audioChannels);
                case ProfileConditionValue.VideoBitDepth:
                    return IsConditionSatisfied(condition, bitDepth);
                case ProfileConditionValue.VideoBitrate:
                    return IsConditionSatisfied(condition, videoBitrate);
                case ProfileConditionValue.Height:
                    return IsConditionSatisfied(condition, height);
                case ProfileConditionValue.Width:
                    return IsConditionSatisfied(condition, width);
                case ProfileConditionValue.VideoTimestamp:
                    return IsConditionSatisfied(condition, timestamp);
                default:
                    throw new ArgumentException("Unexpected condition on video file: " + condition.Property);
            }
        }

        public bool IsImageConditionSatisfied(ProfileCondition condition, int? width, int? height)
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

        public bool IsAudioConditionSatisfied(ProfileCondition condition, int? audioChannels, int? audioBitrate)
        {
            switch (condition.Property)
            {
                case ProfileConditionValue.AudioBitrate:
                    return IsConditionSatisfied(condition, audioBitrate);
                case ProfileConditionValue.AudioChannels:
                    return IsConditionSatisfied(condition, audioChannels);
                default:
                    throw new ArgumentException("Unexpected condition on audio file: " + condition.Property);
            }
        }

        public bool IsVideoAudioConditionSatisfied(ProfileCondition condition, 
            int? audioChannels, 
            int? audioBitrate,
            string audioProfile)
        {
            switch (condition.Property)
            {
                case ProfileConditionValue.AudioProfile:
                    return IsConditionSatisfied(condition, audioProfile);
                case ProfileConditionValue.AudioBitrate:
                    return IsConditionSatisfied(condition, audioBitrate);
                case ProfileConditionValue.AudioChannels:
                    return IsConditionSatisfied(condition, audioChannels);
                default:
                    throw new ArgumentException("Unexpected condition on audio file: " + condition.Property);
            }
        }

        private bool IsConditionSatisfied(ProfileCondition condition, int? currentValue)
        {
            if (!currentValue.HasValue)
            {
                // If the value is unknown, it satisfies if not marked as required
                return !condition.IsRequired;
            }

            int expected;
            if (IntHelper.TryParseCultureInvariant(condition.Value, out expected))
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
                        throw new InvalidOperationException("Unexpected ProfileConditionType");
                }
            }

            return false;
        }

        private bool IsConditionSatisfied(ProfileCondition condition, string currentValue)
        {
            if (string.IsNullOrEmpty(currentValue))
            {
                // If the value is unknown, it satisfies if not marked as required
                return !condition.IsRequired;
            }

            string expected = condition.Value;

            switch (condition.Condition)
            {
                case ProfileConditionType.Equals:
                    return StringHelper.EqualsIgnoreCase(currentValue, expected);
                case ProfileConditionType.NotEquals:
                    return !StringHelper.EqualsIgnoreCase(currentValue, expected);
                default:
                    throw new InvalidOperationException("Unexpected ProfileConditionType");
            }
        }
        
        private bool IsConditionSatisfied(ProfileCondition condition, double? currentValue)
        {
            if (!currentValue.HasValue)
            {
                // If the value is unknown, it satisfies if not marked as required
                return !condition.IsRequired;
            }

            double expected;
            if (DoubleHelper.TryParseCultureInvariant(condition.Value, out expected))
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
                        throw new InvalidOperationException("Unexpected ProfileConditionType");
                }
            }

            return false;
        }
        
        private bool IsConditionSatisfied(ProfileCondition condition, TransportStreamTimestamp? timestamp)
        {
            if (!timestamp.HasValue)
            {
                // If the value is unknown, it satisfies if not marked as required
                return !condition.IsRequired;
            }
            
            TransportStreamTimestamp expected = (TransportStreamTimestamp)Enum.Parse(typeof(TransportStreamTimestamp), condition.Value, true);
            
            switch (condition.Condition)
            {
                case ProfileConditionType.Equals:
                    return timestamp == expected;
                case ProfileConditionType.NotEquals:
                    return timestamp != expected;
                default:
                    throw new InvalidOperationException("Unexpected ProfileConditionType");
            }
        }
    }
}
