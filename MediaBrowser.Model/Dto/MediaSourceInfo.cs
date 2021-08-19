#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Dto
{
    public class MediaSourceInfo
    {
        public MediaSourceInfo()
        {
            Formats = Array.Empty<string>();
            MediaStreams = new List<MediaStream>();
            MediaAttachments = Array.Empty<MediaAttachment>();
            RequiredHttpHeaders = new Dictionary<string, string>();
            SupportsTranscoding = true;
            SupportsDirectStream = true;
            SupportsDirectPlay = true;
            SupportsProbing = true;
            TranscodeReasons = Array.Empty<TranscodeReason>();
        }

        public MediaProtocol Protocol { get; set; }

        public string? Id { get; set; }

        public string? Path { get; set; }

        public string? EncoderPath { get; set; }

        public MediaProtocol? EncoderProtocol { get; set; }

        public MediaSourceType Type { get; set; }

        public string? Container { get; set; }

        public long? Size { get; set; }

        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the media is remote.
        /// Differentiate internet url vs local network.
        /// </summary>
        public bool IsRemote { get; set; }

        public string? ETag { get; set; }

        public long? RunTimeTicks { get; set; }

        public bool ReadAtNativeFramerate { get; set; }

        public bool IgnoreDts { get; set; }

        public bool IgnoreIndex { get; set; }

        public bool GenPtsInput { get; set; }

        public bool SupportsTranscoding { get; set; }

        public bool SupportsDirectStream { get; set; }

        public bool SupportsDirectPlay { get; set; }

        public bool IsInfiniteStream { get; set; }

        public bool RequiresOpening { get; set; }

        public string? OpenToken { get; set; }

        public bool RequiresClosing { get; set; }

        public string? LiveStreamId { get; set; }

        public int? BufferMs { get; set; }

        public bool RequiresLooping { get; set; }

        public bool SupportsProbing { get; set; }

        public VideoType? VideoType { get; set; }

        public IsoType? IsoType { get; set; }

        public Video3DFormat? Video3DFormat { get; set; }

        public List<MediaStream> MediaStreams { get; set; }

        public IReadOnlyList<MediaAttachment> MediaAttachments { get; set; }

        public string[] Formats { get; set; }

        public int? Bitrate { get; set; }

        public TransportStreamTimestamp? Timestamp { get; set; }

        public Dictionary<string, string> RequiredHttpHeaders { get; set; }

        public string? TranscodingUrl { get; set; }

        public string? TranscodingSubProtocol { get; set; }

        public string? TranscodingContainer { get; set; }

        public int? AnalyzeDurationMs { get; set; }

        [JsonIgnore]
        public TranscodeReason[] TranscodeReasons { get; set; }

        public int? DefaultAudioStreamIndex { get; set; }

        public int? DefaultSubtitleStreamIndex { get; set; }

        [JsonIgnore]
        public MediaStream? VideoStream
        {
            get
            {
                return MediaStreams.Find(stream => stream.Type == MediaStreamType.Video);
            }
        }

        public void InferTotalBitrate(bool force = false)
        {
            if (MediaStreams == null || (!force && Bitrate.HasValue))
            {
                return;
            }

            int bitrate = MediaStreams
                .Where(stream => !stream.IsExternal)
                .Select(stream => stream.BitRate ?? 0)
                .Sum();

            if (bitrate > 0)
            {
                Bitrate = bitrate;
            }
        }

        public MediaStream? GetDefaultAudioStream(int? defaultIndex)
        {
            MediaStream? firstAudioStreamLabeledDefault = null;
            MediaStream? firstAudioStream = null;
            foreach (var i in MediaStreams)
            {
                if (i.Type != MediaStreamType.Audio)
                {
                    // Ignore all streams that are not audio
                    continue;
                }

                if (defaultIndex != null && i.Index == defaultIndex.Value)
                {
                    // A default index override was given and there was a match, so early return.
                    return i;
                }

                if (firstAudioStreamLabeledDefault == null && i.IsDefault)
                {
                    // Found the first audio stream marked as default
                    firstAudioStreamLabeledDefault = i;

                    if (defaultIndex == null)
                    {
                        // Since no overriding default index was given, okay to early return.
                        return i;
                    }
                }

                if (firstAudioStream == null)
                {
                    // Found the first audio stream
                    firstAudioStream = i;
                }
            }

            return firstAudioStreamLabeledDefault ?? firstAudioStream;
        }

        public MediaStream? GetMediaStream(MediaStreamType type, int index)
        {
            return MediaStreams.Find(i => i.Type == type && i.Index == index);
        }

        public int? GetStreamCount(MediaStreamType type)
        {
            if (MediaStreams.Count == 0)
            {
                return null;
            }

            return MediaStreams.Where(i => i.Type == type).Count();
        }

        public bool? IsSecondaryAudio(MediaStream stream)
        {
            MediaStream? defaultAudioStream = GetDefaultAudioStream(null);
            return defaultAudioStream?.Index.Equals(stream.Index);
        }
    }
}
