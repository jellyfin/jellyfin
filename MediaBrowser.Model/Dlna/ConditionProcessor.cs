using System;
using System.Globalization;
using System.Linq;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="ConditionProcessor" />.
    /// </summary>
    public static class ConditionProcessor
    {
        /// <summary>
        /// Checks to see if the video condition is satisfied.
        /// </summary>
        /// <param name="condition">The condition<see cref="ProfileCondition"/>.</param>
        /// <param name="width">An optional value indicating the width.</param>
        /// <param name="height">An optional value indicating the height.</param>
        /// <param name="videoBitDepth">An optional value indicating the video bit depth.</param>
        /// <param name="videoBitrate">An optional value indicating the video bitrate.</param>
        /// <param name="videoProfile">An optional value indicating the video profile.</param>
        /// <param name="videoLevel">An optional value indicating the video level.</param>
        /// <param name="videoFramerate">An optional value indicating the framerate.</param>
        /// <param name="packetLength">An optional value indicating the packet length.</param>
        /// <param name="timestamp">An optional <see cref="TransportStreamTimestamp"/>.</param>
        /// <param name="isAnamorphic">An optional value indicating if it is anamorphic.</param>
        /// <param name="isInterlaced">An optional value indicating if it is interlaced.</param>
        /// <param name="refFrames">An optional value indicating the ref frames.</param>
        /// <param name="numVideoStreams">An optional value indicating the number of video streams.</param>
        /// <param name="numAudioStreams">An optional value indicating the number of audio streams.</param>
        /// <param name="videoCodecTag">An optional value indicating the video codec tag.</param>
        /// <param name="isAvc">An optional value indicating if it is sAvc.</param>
        /// <returns>True if all the criteria are satisfied.</returns>
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
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }

            return condition.Property switch
            {
                ProfileConditionValue.IsInterlaced => IsConditionSatisfied(condition, isInterlaced),
                ProfileConditionValue.IsAnamorphic => IsConditionSatisfied(condition, isAnamorphic),
                ProfileConditionValue.IsAvc => IsConditionSatisfied(condition, isAvc),
                ProfileConditionValue.VideoFramerate => IsConditionSatisfied(condition, videoFramerate),
                ProfileConditionValue.VideoLevel => IsConditionSatisfied(condition, videoLevel),
                ProfileConditionValue.VideoProfile => IsConditionSatisfied(condition, videoProfile),
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
        /// Checks to see if the image condition is satisfied.
        /// </summary>
        /// <param name="condition">The <see cref="ProfileCondition"/>.</param>
        /// <param name="width">An optional value indicating the width.</param>
        /// <param name="height">An optional value indicating the height.</param>
        /// <returns>True if the conditions are satisfied.</returns>
        public static bool IsImageConditionSatisfied(ProfileCondition condition, int? width, int? height)
        {
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }

            return condition.Property switch
            {
                ProfileConditionValue.Height => IsConditionSatisfied(condition, height),
                ProfileConditionValue.Width => IsConditionSatisfied(condition, width),
                _ => throw new ArgumentException("Unexpected condition on image file: " + condition.Property),
            };
        }

        /// <summary>
        /// Checks to see if the audio condition is satisfied.
        /// </summary>
        /// <param name="condition">The <see cref="ProfileCondition"/>.</param>
        /// <param name="audioChannels">An optional value indicating the number of audio channels.</param>
        /// <param name="audioBitrate">An optional value indicating the audio bitrate.</param>
        /// <param name="audioSampleRate">An optional value indicating the audio sample rate.</param>
        /// <param name="audioBitDepth">An optional value indicating the audio bit depth.</param>
        /// <returns>True if the conditions are satisfied.</returns>
        public static bool IsAudioConditionSatisfied(ProfileCondition condition, int? audioChannels, int? audioBitrate, int? audioSampleRate, int? audioBitDepth)
        {
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }

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
        /// Checks to see if the video and audio condition is satisfied.
        /// </summary>
        /// <param name="condition">The <see cref="ProfileCondition"/>.</param>
        /// <param name="audioChannels">An optional value indicating the number of audio channels.</param>
        /// <param name="audioBitrate">An optional value indicating the audio bitrate.</param>
        /// <param name="audioSampleRate">An optional value indicating the audio sample rate.</param>
        /// <param name="audioBitDepth">An optional value indicating the audio bit depth.</param>
        /// <param name="audioProfile">An optional value indicating the audio profile.</param>
        /// <param name="isSecondaryTrack">An optional value indicating whether there is a secondary track.</param>
        /// <returns>True if the conditions are satisfied.</returns>
        public static bool IsVideoAudioConditionSatisfied(
            ProfileCondition condition,
            int? audioChannels,
            int? audioBitrate,
            int? audioSampleRate,
            int? audioBitDepth,
            string? audioProfile,
            bool? isSecondaryTrack)
        {
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }

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

        /// <summary>
        /// Checks to see if an integer condition is satisfied.
        /// </summary>
        /// <param name="condition">The <see cref="ProfileCondition"/>.</param>
        /// <param name="currentValue">The An optional value indicating the current value.</param>
        /// <returns>True if the conditions are satisfied.</returns>
        private static bool IsConditionSatisfied(ProfileCondition condition, int? currentValue)
        {
            if (!currentValue.HasValue)
            {
                // If the value is unknown, it satisfies if not marked as required
                return !condition.IsRequired;
            }

            if (int.TryParse(condition.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var expected))
            {
                return condition.Condition switch
                {
                    ProfileConditionType.Equals or ProfileConditionType.EqualsAny => currentValue.Value.Equals(expected),
                    ProfileConditionType.GreaterThanEqual => currentValue.Value >= expected,
                    ProfileConditionType.LessThanEqual => currentValue.Value <= expected,
                    ProfileConditionType.NotEquals => !currentValue.Value.Equals(expected),
                    _ => throw new InvalidOperationException("Unexpected ProfileConditionType: " + condition.Condition),
                };
            }

            return false;
        }

        /// <summary>
        /// Checks to see if a string condition is satisfied.
        /// </summary>
        /// <param name="condition">The <see cref="ProfileCondition"/>.</param>
        /// <param name="currentValue">An optional value indicating the current value.</param>
        /// <returns>True if the condition is satisfied.</returns>
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
                ProfileConditionType.EqualsAny => expected.Split('|').Contains(currentValue, StringComparer.OrdinalIgnoreCase),
                ProfileConditionType.Equals => string.Equals(currentValue, expected, StringComparison.OrdinalIgnoreCase),
                ProfileConditionType.NotEquals => !string.Equals(currentValue, expected, StringComparison.OrdinalIgnoreCase),
                _ => throw new InvalidOperationException("Unexpected ProfileConditionType: " + condition.Condition),
            };
        }

        /// <summary>
        /// Checks to see if a boolean condition is satisfied.
        /// </summary>
        /// <param name="condition">The <see cref="ProfileCondition"/>.</param>
        /// <param name="currentValue">An optional value indicating the current value.</param>
        /// <returns>True if the condition is satisfied.</returns>
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

        /// <summary>
        /// Checks to see if a double condition is satisfied.
        /// </summary>
        /// <param name="condition">The condition<see cref="ProfileCondition"/>.</param>
        /// <param name="currentValue">An optional value indicating the current value.</param>
        /// <returns>True if the condition is satisfied.</returns>
        private static bool IsConditionSatisfied(ProfileCondition condition, double? currentValue)
        {
            if (!currentValue.HasValue)
            {
                // If the value is unknown, it satisfies if not marked as required
                return !condition.IsRequired;
            }

            if (double.TryParse(condition.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var expected))
            {
                return condition.Condition switch
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

        /// <summary>
        /// Checks if a <see cref="TransportStreamTimestamp"/> condition is satisfied.
        /// </summary>
        /// <param name="condition">The condition<see cref="ProfileCondition"/>.</param>
        /// <param name="timestamp">An optional value indicating the <see cref="TransportStreamTimestamp"/>.</param>
        /// <returns>True if the condition is satisfied.</returns>
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
    }
}
