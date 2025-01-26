#nullable disable

#pragma warning disable CS1591, SA1401

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.MediaEncoding
{
    // For now, a common base class until the API and MediaEncoding classes are unified
    public class EncodingJobInfo
    {
        public int? OutputAudioBitrate;
        public int? OutputAudioChannels;

        private TranscodeReason? _transcodeReasons = null;

        public EncodingJobInfo(TranscodingJobType jobType)
        {
            TranscodingType = jobType;
            RemoteHttpHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            SupportedAudioCodecs = Array.Empty<string>();
            SupportedVideoCodecs = Array.Empty<string>();
            SupportedSubtitleCodecs = Array.Empty<string>();
        }

        public TranscodeReason TranscodeReasons
        {
            get
            {
                if (!_transcodeReasons.HasValue)
                {
                    if (BaseRequest.TranscodeReasons is null)
                    {
                        _transcodeReasons = 0;
                        return 0;
                    }

                    _ = Enum.TryParse<TranscodeReason>(BaseRequest.TranscodeReasons, out var reason);
                    _transcodeReasons = reason;
                }

                return _transcodeReasons.Value;
            }
        }

        public IProgress<double> Progress { get; set; }

        public MediaStream VideoStream { get; set; }

        public VideoType VideoType { get; set; }

        public Dictionary<string, string> RemoteHttpHeaders { get; set; }

        public string OutputVideoCodec { get; set; }

        public MediaProtocol InputProtocol { get; set; }

        public string MediaPath { get; set; }

        public bool IsInputVideo { get; set; }

        public string OutputAudioCodec { get; set; }

        public int? OutputVideoBitrate { get; set; }

        public MediaStream SubtitleStream { get; set; }

        public SubtitleDeliveryMethod SubtitleDeliveryMethod { get; set; }

        public string[] SupportedSubtitleCodecs { get; set; }

        public int InternalSubtitleStreamOffset { get; set; }

        public MediaSourceInfo MediaSource { get; set; }

        public User User { get; set; }

        public long? RunTimeTicks { get; set; }

        public bool ReadInputAtNativeFramerate { get; set; }

        public string OutputFilePath { get; set; }

        public string MimeType { get; set; }

        public bool IgnoreInputDts => MediaSource.IgnoreDts;

        public bool IgnoreInputIndex => MediaSource.IgnoreIndex;

        public bool GenPtsInput => MediaSource.GenPtsInput;

        public bool DiscardCorruptFramesInput => false;

        public bool EnableFastSeekInput => false;

        public bool GenPtsOutput => false;

        public string OutputContainer { get; set; }

        public string OutputVideoSync { get; set; }

        public string AlbumCoverPath { get; set; }

        public string InputAudioSync { get; set; }

        public string InputVideoSync { get; set; }

        public TransportStreamTimestamp InputTimestamp { get; set; }

        public MediaStream AudioStream { get; set; }

        public string[] SupportedAudioCodecs { get; set; }

        public string[] SupportedVideoCodecs { get; set; }

        public string InputContainer { get; set; }

        public IsoType? IsoType { get; set; }

        public BaseEncodingJobOptions BaseRequest { get; set; }

        public bool IsVideoRequest { get; set; }

        public TranscodingJobType TranscodingType { get; set; }

        public long? StartTimeTicks => BaseRequest.StartTimeTicks;

        public bool CopyTimestamps => BaseRequest.CopyTimestamps;

        public bool IsSegmentedLiveStream
            => TranscodingType != TranscodingJobType.Progressive && !RunTimeTicks.HasValue;

        public int? TotalOutputBitrate => (OutputAudioBitrate ?? 0) + (OutputVideoBitrate ?? 0);

        public int? OutputWidth
        {
            get
            {
                if (VideoStream is not null && VideoStream.Width.HasValue && VideoStream.Height.HasValue)
                {
                    var size = new ImageDimensions(VideoStream.Width.Value, VideoStream.Height.Value);

                    var newSize = DrawingUtils.Resize(
                        size,
                        BaseRequest.Width ?? 0,
                        BaseRequest.Height ?? 0,
                        BaseRequest.MaxWidth ?? 0,
                        BaseRequest.MaxHeight ?? 0);

                    return newSize.Width;
                }

                if (!IsVideoRequest)
                {
                    return null;
                }

                return BaseRequest.MaxWidth ?? BaseRequest.Width;
            }
        }

        public int? OutputHeight
        {
            get
            {
                if (VideoStream is not null && VideoStream.Width.HasValue && VideoStream.Height.HasValue)
                {
                    var size = new ImageDimensions(VideoStream.Width.Value, VideoStream.Height.Value);

                    var newSize = DrawingUtils.Resize(
                        size,
                        BaseRequest.Width ?? 0,
                        BaseRequest.Height ?? 0,
                        BaseRequest.MaxWidth ?? 0,
                        BaseRequest.MaxHeight ?? 0);

                    return newSize.Height;
                }

                if (!IsVideoRequest)
                {
                    return null;
                }

                return BaseRequest.MaxHeight ?? BaseRequest.Height;
            }
        }

        public int? OutputAudioSampleRate
        {
            get
            {
                if (BaseRequest.Static
                    || EncodingHelper.IsCopyCodec(OutputAudioCodec))
                {
                    if (AudioStream is not null)
                    {
                        return AudioStream.SampleRate;
                    }
                }
                else if (BaseRequest.AudioSampleRate.HasValue)
                {
                    // Don't exceed what the encoder supports
                    // Seeing issues of attempting to encode to 88200
                    return BaseRequest.AudioSampleRate.Value;
                }

                return null;
            }
        }

        public int? OutputAudioBitDepth
        {
            get
            {
                if (BaseRequest.Static
                    || EncodingHelper.IsCopyCodec(OutputAudioCodec))
                {
                    if (AudioStream is not null)
                    {
                        return AudioStream.BitDepth;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the target video level.
        /// </summary>
        public double? TargetVideoLevel
        {
            get
            {
                if (BaseRequest.Static || EncodingHelper.IsCopyCodec(OutputVideoCodec))
                {
                    return VideoStream?.Level;
                }

                var level = GetRequestedLevel(ActualOutputVideoCodec);
                if (double.TryParse(level, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the target video bit depth.
        /// </summary>
        public int? TargetVideoBitDepth
        {
            get
            {
                if (BaseRequest.Static
                    || EncodingHelper.IsCopyCodec(OutputVideoCodec))
                {
                    return VideoStream?.BitDepth;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the target reference frames.
        /// </summary>
        /// <value>The target reference frames.</value>
        public int? TargetRefFrames
        {
            get
            {
                if (BaseRequest.Static
                    || EncodingHelper.IsCopyCodec(OutputVideoCodec))
                {
                    return VideoStream?.RefFrames;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the target framerate.
        /// </summary>
        public float? TargetFramerate
        {
            get
            {
                if (BaseRequest.Static
                    || EncodingHelper.IsCopyCodec(OutputVideoCodec))
                {
                    return VideoStream?.ReferenceFrameRate;
                }

                return BaseRequest.MaxFramerate ?? BaseRequest.Framerate;
            }
        }

        public TransportStreamTimestamp TargetTimestamp
        {
            get
            {
                if (BaseRequest.Static)
                {
                    return InputTimestamp;
                }

                return string.Equals(OutputContainer, "m2ts", StringComparison.OrdinalIgnoreCase) ?
                    TransportStreamTimestamp.Valid :
                    TransportStreamTimestamp.None;
            }
        }

        /// <summary>
        /// Gets the target packet length.
        /// </summary>
        public int? TargetPacketLength
        {
            get
            {
                if (BaseRequest.Static || EncodingHelper.IsCopyCodec(OutputVideoCodec))
                {
                    return VideoStream?.PacketLength;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the target video profile.
        /// </summary>
        public string TargetVideoProfile
        {
            get
            {
                if (BaseRequest.Static || EncodingHelper.IsCopyCodec(OutputVideoCodec))
                {
                    return VideoStream?.Profile;
                }

                var requestedProfile = GetRequestedProfiles(ActualOutputVideoCodec).FirstOrDefault();
                if (!string.IsNullOrEmpty(requestedProfile))
                {
                    return requestedProfile;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the target video range type.
        /// </summary>
        public VideoRangeType TargetVideoRangeType
        {
            get
            {
                if (BaseRequest.Static || EncodingHelper.IsCopyCodec(OutputVideoCodec))
                {
                    return VideoStream?.VideoRangeType ?? VideoRangeType.Unknown;
                }

                if (Enum.TryParse(GetRequestedRangeTypes(ActualOutputVideoCodec).FirstOrDefault() ?? "Unknown", true, out VideoRangeType requestedRangeType))
                {
                    return requestedRangeType;
                }

                return VideoRangeType.Unknown;
            }
        }

        public string TargetVideoCodecTag
        {
            get
            {
                if (BaseRequest.Static
                    || EncodingHelper.IsCopyCodec(OutputVideoCodec))
                {
                    return VideoStream?.CodecTag;
                }

                return null;
            }
        }

        public bool? IsTargetAnamorphic
        {
            get
            {
                if (BaseRequest.Static
                    || EncodingHelper.IsCopyCodec(OutputVideoCodec))
                {
                    return VideoStream?.IsAnamorphic;
                }

                return false;
            }
        }

        public string ActualOutputVideoCodec
        {
            get
            {
                if (VideoStream is null)
                {
                    return null;
                }

                if (EncodingHelper.IsCopyCodec(OutputVideoCodec))
                {
                    return VideoStream.Codec;
                }

                return OutputVideoCodec;
            }
        }

        public string ActualOutputAudioCodec
        {
            get
            {
                if (AudioStream is null)
                {
                    return null;
                }

                if (EncodingHelper.IsCopyCodec(OutputAudioCodec))
                {
                    return AudioStream.Codec;
                }

                return OutputAudioCodec;
            }
        }

        public bool? IsTargetInterlaced
        {
            get
            {
                if (BaseRequest.Static
                    || EncodingHelper.IsCopyCodec(OutputVideoCodec))
                {
                    return VideoStream?.IsInterlaced;
                }

                if (DeInterlace(ActualOutputVideoCodec, true))
                {
                    return false;
                }

                return VideoStream?.IsInterlaced;
            }
        }

        public bool? IsTargetAVC
        {
            get
            {
                if (BaseRequest.Static || EncodingHelper.IsCopyCodec(OutputVideoCodec))
                {
                    return VideoStream?.IsAVC;
                }

                return false;
            }
        }

        public int? TargetVideoStreamCount
        {
            get
            {
                if (BaseRequest.Static)
                {
                    return GetMediaStreamCount(MediaStreamType.Video, int.MaxValue);
                }

                return GetMediaStreamCount(MediaStreamType.Video, 1);
            }
        }

        public int? TargetAudioStreamCount
        {
            get
            {
                if (BaseRequest.Static)
                {
                    return GetMediaStreamCount(MediaStreamType.Audio, int.MaxValue);
                }

                return GetMediaStreamCount(MediaStreamType.Audio, 1);
            }
        }

        public bool EnableAudioVbrEncoding => BaseRequest.EnableAudioVbrEncoding;

        public int HlsListSize => 0;

        public bool EnableBreakOnNonKeyFrames(string videoCodec)
        {
            if (TranscodingType != TranscodingJobType.Progressive)
            {
                if (IsSegmentedLiveStream)
                {
                    return false;
                }

                return BaseRequest.BreakOnNonKeyFrames && EncodingHelper.IsCopyCodec(videoCodec);
            }

            return false;
        }

        private int? GetMediaStreamCount(MediaStreamType type, int limit)
        {
            var count = MediaSource.GetStreamCount(type);

            if (count.HasValue)
            {
                count = Math.Min(count.Value, limit);
            }

            return count;
        }

        public string GetMimeType(string outputPath, bool enableStreamDefault = true)
        {
            if (!string.IsNullOrEmpty(MimeType))
            {
                return MimeType;
            }

            if (enableStreamDefault)
            {
                return MimeTypes.GetMimeType(outputPath);
            }

            return MimeTypes.GetMimeType(outputPath, null);
        }

        public bool DeInterlace(string videoCodec, bool forceDeinterlaceIfSourceIsInterlaced)
        {
            var videoStream = VideoStream;
            var isInputInterlaced = videoStream is not null && videoStream.IsInterlaced;

            if (!isInputInterlaced)
            {
                return false;
            }

            // Support general param
            if (BaseRequest.DeInterlace)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(videoCodec))
            {
                if (string.Equals(BaseRequest.GetOption(videoCodec, "deinterlace"), "true", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return forceDeinterlaceIfSourceIsInterlaced;
        }

        public string[] GetRequestedProfiles(string codec)
        {
            if (!string.IsNullOrEmpty(BaseRequest.Profile))
            {
                return BaseRequest.Profile.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries);
            }

            if (!string.IsNullOrEmpty(codec))
            {
                var profile = BaseRequest.GetOption(codec, "profile");

                if (!string.IsNullOrEmpty(profile))
                {
                    return profile.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries);
                }
            }

            return Array.Empty<string>();
        }

        public string[] GetRequestedRangeTypes(string codec)
        {
            if (!string.IsNullOrEmpty(BaseRequest.VideoRangeType))
            {
                return BaseRequest.VideoRangeType.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries);
            }

            if (!string.IsNullOrEmpty(codec))
            {
                var rangetype = BaseRequest.GetOption(codec, "rangetype");

                if (!string.IsNullOrEmpty(rangetype))
                {
                    return rangetype.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries);
                }
            }

            return Array.Empty<string>();
        }

        public string[] GetRequestedCodecTags(string codec)
        {
            if (!string.IsNullOrEmpty(BaseRequest.CodecTag))
            {
                return BaseRequest.CodecTag.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries);
            }

            if (!string.IsNullOrEmpty(codec))
            {
                var codectag = BaseRequest.GetOption(codec, "codectag");

                if (!string.IsNullOrEmpty(codectag))
                {
                    return codectag.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries);
                }
            }

            return Array.Empty<string>();
        }

        public string GetRequestedLevel(string codec)
        {
            if (!string.IsNullOrEmpty(BaseRequest.Level))
            {
                return BaseRequest.Level;
            }

            if (!string.IsNullOrEmpty(codec))
            {
                return BaseRequest.GetOption(codec, "level");
            }

            return null;
        }

        public int? GetRequestedMaxRefFrames(string codec)
        {
            if (BaseRequest.MaxRefFrames.HasValue)
            {
                return BaseRequest.MaxRefFrames;
            }

            if (!string.IsNullOrEmpty(codec))
            {
                var value = BaseRequest.GetOption(codec, "maxrefframes");
                if (int.TryParse(value, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }

            return null;
        }

        public int? GetRequestedVideoBitDepth(string codec)
        {
            if (BaseRequest.MaxVideoBitDepth.HasValue)
            {
                return BaseRequest.MaxVideoBitDepth;
            }

            if (!string.IsNullOrEmpty(codec))
            {
                var value = BaseRequest.GetOption(codec, "videobitdepth");
                if (int.TryParse(value, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }

            return null;
        }

        public int? GetRequestedAudioBitDepth(string codec)
        {
            if (BaseRequest.MaxAudioBitDepth.HasValue)
            {
                return BaseRequest.MaxAudioBitDepth;
            }

            if (!string.IsNullOrEmpty(codec))
            {
                var value = BaseRequest.GetOption(codec, "audiobitdepth");
                if (int.TryParse(value, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }

            return null;
        }

        public int? GetRequestedAudioChannels(string codec)
        {
            if (!string.IsNullOrEmpty(codec))
            {
                var value = BaseRequest.GetOption(codec, "audiochannels");
                if (int.TryParse(value, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }

            if (BaseRequest.MaxAudioChannels.HasValue)
            {
                return BaseRequest.MaxAudioChannels;
            }

            if (BaseRequest.AudioChannels.HasValue)
            {
                return BaseRequest.AudioChannels;
            }

            if (BaseRequest.TranscodingMaxAudioChannels.HasValue)
            {
                return BaseRequest.TranscodingMaxAudioChannels;
            }

            return null;
        }

        public virtual void ReportTranscodingProgress(TimeSpan? transcodingPosition, float? framerate, double? percentComplete, long? bytesTranscoded, int? bitRate)
        {
            Progress.Report(percentComplete.Value);
        }
    }
}
