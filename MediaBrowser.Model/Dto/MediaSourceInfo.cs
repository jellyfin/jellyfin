#nullable disable
#pragma warning disable CS1591

using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Dto
{
    public class MediaSourceInfo
    {
        public MediaSourceInfo()
        {
            Formats = [];
            MediaStreams = [];
            MediaAttachments = [];
            RequiredHttpHeaders = [];
            SupportsTranscoding = true;
            SupportsDirectStream = true;
            SupportsDirectPlay = true;
            SupportsProbing = true;
            UseMostCompatibleTranscodingProfile = false;
        }

        public MediaProtocol Protocol { get; set; }

        public string Id { get; set; }

        public string Path { get; set; }

        public string EncoderPath { get; set; }

        public MediaProtocol? EncoderProtocol { get; set; }

        public MediaSourceType Type { get; set; }

        public string Container { get; set; }

        public long? Size { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the media is remote.
        /// Differentiate internet url vs local network.
        /// </summary>
        public bool IsRemote { get; set; }

        public string ETag { get; set; }

        public long? RunTimeTicks { get; set; }

        public bool ReadAtNativeFramerate { get; set; }

        public bool IgnoreDts { get; set; }

        public bool IgnoreIndex { get; set; }

        public bool GenPtsInput { get; set; }

        public bool SupportsTranscoding { get; set; }

        public bool SupportsDirectStream { get; set; }

        public bool SupportsDirectPlay { get; set; }

        public bool IsInfiniteStream { get; set; }

        [DefaultValue(false)]
        public bool UseMostCompatibleTranscodingProfile { get; set; }

        public bool RequiresOpening { get; set; }

        public string OpenToken { get; set; }

        public bool RequiresClosing { get; set; }

        public string LiveStreamId { get; set; }

        public int? BufferMs { get; set; }

        public bool RequiresLooping { get; set; }

        public bool SupportsProbing { get; set; }

        public VideoType? VideoType { get; set; }

        public IsoType? IsoType { get; set; }

        public Video3DFormat? Video3DFormat { get; set; }

        public IReadOnlyList<MediaStream> MediaStreams { get; set; }

        public IReadOnlyList<MediaAttachment> MediaAttachments { get; set; }

        public string[] Formats { get; set; }

        public int? Bitrate { get; set; }

        public int? FallbackMaxStreamingBitrate { get; set; }

        public TransportStreamTimestamp? Timestamp { get; set; }

        public Dictionary<string, string> RequiredHttpHeaders { get; set; }

        public string TranscodingUrl { get; set; }

        public MediaStreamProtocol TranscodingSubProtocol { get; set; }

        public string TranscodingContainer { get; set; }

        public int? AnalyzeDurationMs { get; set; }

        [JsonIgnore]
        public TranscodeReason TranscodeReasons { get; set; }

        public int? DefaultAudioStreamIndex { get; set; }

        public int? DefaultSubtitleStreamIndex { get; set; }

        public bool HasSegments { get; set; }

        [JsonIgnore]
        public MediaStream VideoStream
        {
            get
            {
                foreach (var i in MediaStreams)
                {
                    if (i.Type == MediaStreamType.Video)
                    {
                        return i;
                    }
                }

                return null;
            }
        }

        public void InferTotalBitrate(bool force = false)
        {
            if (MediaStreams is null)
            {
                return;
            }

            if (!force && Bitrate.HasValue)
            {
                return;
            }

            var bitrate = 0;
            foreach (var stream in MediaStreams)
            {
                if (!stream.IsExternal)
                {
                    bitrate += stream.BitRate ?? 0;
                }
            }

            if (bitrate > 0)
            {
                Bitrate = bitrate;
            }
        }

        public MediaStream GetDefaultAudioStream(int? defaultIndex)
        {
            if (defaultIndex.HasValue && defaultIndex != -1)
            {
                var val = defaultIndex.Value;

                foreach (var i in MediaStreams)
                {
                    if (i.Type == MediaStreamType.Audio && i.Index == val)
                    {
                        return i;
                    }
                }
            }

            foreach (var i in MediaStreams)
            {
                if (i.Type == MediaStreamType.Audio && i.IsDefault)
                {
                    return i;
                }
            }

            foreach (var i in MediaStreams)
            {
                if (i.Type == MediaStreamType.Audio)
                {
                    return i;
                }
            }

            return null;
        }

        public MediaStream GetMediaStream(MediaStreamType type, int index)
        {
            foreach (var i in MediaStreams)
            {
                if (i.Type == type && i.Index == index)
                {
                    return i;
                }
            }

            return null;
        }

        public int? GetStreamCount(MediaStreamType type)
        {
            int numMatches = 0;
            int numStreams = 0;

            foreach (var i in MediaStreams)
            {
                numStreams++;
                if (i.Type == type)
                {
                    numMatches++;
                }
            }

            if (numStreams == 0)
            {
                return null;
            }

            return numMatches;
        }

        public bool? IsSecondaryAudio(MediaStream stream)
        {
            if (stream.IsExternal)
            {
                return false;
            }

            // Look for the first audio track
            foreach (var currentStream in MediaStreams)
            {
                if (currentStream.Type == MediaStreamType.Audio && !currentStream.IsExternal)
                {
                    return currentStream.Index != stream.Index;
                }
            }

            return null;
        }
    }
}
