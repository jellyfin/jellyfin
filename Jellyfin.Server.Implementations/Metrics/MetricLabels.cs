using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;

namespace Jellyfin.Server.Implementations.Metrics
{
    internal static class MetricLabels
    {
        private static readonly (TranscodeReason Reason, string Label)[] _transcodeReasons =
        [
            (TranscodeReason.ContainerNotSupported, "container_not_supported"),
            (TranscodeReason.VideoCodecNotSupported, "video_codec_not_supported"),
            (TranscodeReason.AudioCodecNotSupported, "audio_codec_not_supported"),
            (TranscodeReason.SubtitleCodecNotSupported, "subtitle_codec_not_supported"),
            (TranscodeReason.AudioIsExternal, "audio_is_external"),
            (TranscodeReason.SecondaryAudioNotSupported, "secondary_audio_not_supported"),
            (TranscodeReason.StreamCountExceedsLimit, "stream_count_exceeds_limit"),
            (TranscodeReason.VideoProfileNotSupported, "video_profile_not_supported"),
            (TranscodeReason.VideoRangeTypeNotSupported, "video_range_type_not_supported"),
            (TranscodeReason.VideoCodecTagNotSupported, "video_codec_tag_not_supported"),
            (TranscodeReason.VideoLevelNotSupported, "video_level_not_supported"),
            (TranscodeReason.VideoResolutionNotSupported, "video_resolution_not_supported"),
            (TranscodeReason.VideoBitDepthNotSupported, "video_bit_depth_not_supported"),
            (TranscodeReason.VideoFramerateNotSupported, "video_framerate_not_supported"),
            (TranscodeReason.VideoRotationNotSupported, "video_rotation_not_supported"),
            (TranscodeReason.RefFramesNotSupported, "ref_frames_not_supported"),
            (TranscodeReason.AnamorphicVideoNotSupported, "anamorphic_video_not_supported"),
            (TranscodeReason.InterlacedVideoNotSupported, "interlaced_video_not_supported"),
            (TranscodeReason.AudioChannelsNotSupported, "audio_channels_not_supported"),
            (TranscodeReason.AudioProfileNotSupported, "audio_profile_not_supported"),
            (TranscodeReason.AudioSampleRateNotSupported, "audio_sample_rate_not_supported"),
            (TranscodeReason.AudioBitDepthNotSupported, "audio_bit_depth_not_supported"),
            (TranscodeReason.ContainerBitrateExceedsLimit, "container_bitrate_exceeds_limit"),
            (TranscodeReason.VideoBitrateNotSupported, "video_bitrate_not_supported"),
            (TranscodeReason.AudioBitrateNotSupported, "audio_bitrate_not_supported"),
            (TranscodeReason.UnknownVideoStreamInfo, "unknown_video_stream_info"),
            (TranscodeReason.UnknownAudioStreamInfo, "unknown_audio_stream_info"),
            (TranscodeReason.DirectPlayError, "direct_play_error")
        ];

        public static string PlayMethod(PlayMethod playMethod)
            => playMethod switch
            {
                MediaBrowser.Model.Session.PlayMethod.DirectPlay => "direct_play",
                MediaBrowser.Model.Session.PlayMethod.DirectStream => "direct_stream",
                MediaBrowser.Model.Session.PlayMethod.Transcode => "transcode",
                _ => "unknown"
            };

        public static string Acceleration(HardwareAccelerationType? acceleration)
            => acceleration switch
            {
                null => "unknown",
                HardwareAccelerationType.none => "software",
                HardwareAccelerationType.amf => "amf",
                HardwareAccelerationType.qsv => "qsv",
                HardwareAccelerationType.nvenc => "nvenc",
                HardwareAccelerationType.v4l2m2m => "v4l2m2m",
                HardwareAccelerationType.vaapi => "vaapi",
                HardwareAccelerationType.videotoolbox => "videotoolbox",
                HardwareAccelerationType.rkmpp => "rkmpp",
                _ => "unknown"
            };

        public static string CollectionType(CollectionTypeOptions? collectionType)
            => collectionType switch
            {
                null => "unknown",
                CollectionTypeOptions.movies => "movies",
                CollectionTypeOptions.tvshows => "tvshows",
                CollectionTypeOptions.music => "music",
                CollectionTypeOptions.musicvideos => "musicvideos",
                CollectionTypeOptions.homevideos => "homevideos",
                CollectionTypeOptions.boxsets => "boxsets",
                CollectionTypeOptions.books => "books",
                CollectionTypeOptions.mixed => "mixed",
                _ => "unknown"
            };

        public static string StreamKind(MediaStreamTypeEntity streamType)
            => streamType switch
            {
                MediaStreamTypeEntity.Audio => "audio",
                MediaStreamTypeEntity.Video => "video",
                MediaStreamTypeEntity.Subtitle => "subtitle",
                MediaStreamTypeEntity.EmbeddedImage => "embedded_image",
                MediaStreamTypeEntity.Data => "data",
                MediaStreamTypeEntity.Lyric => "lyric",
                _ => "unknown"
            };

        public static string TranscodeReasons(TranscodeReason reasons)
        {
            if (reasons == 0)
            {
                return "none";
            }

            var remaining = (int)reasons;
            var labels = new List<string>();
            foreach (var (reason, label) in _transcodeReasons)
            {
                if ((reasons & reason) == reason)
                {
                    labels.Add(label);
                    remaining &= ~(int)reason;
                }
            }

            if (remaining != 0)
            {
                labels.Add($"unknown_0x{unchecked((uint)remaining):x8}");
            }

            labels.Sort(StringComparer.Ordinal);
            return string.Join("|", labels);
        }
    }
}
