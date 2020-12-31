using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Defines the <see cref="MediaSourceInfo" />.
    /// </summary>
    public class MediaSourceInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaSourceInfo"/> class.
        /// </summary>
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
        }

        /// <summary>
        /// Gets or sets the Protocol.
        /// </summary>
        public MediaProtocol Protocol { get; set; }

        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the Path.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets the EncoderPath.
        /// </summary>
        public string? EncoderPath { get; set; }

        /// <summary>
        /// Gets or sets the EncoderProtocol.
        /// </summary>
        public MediaProtocol? EncoderProtocol { get; set; }

        /// <summary>
        /// Gets or sets the Type.
        /// </summary>
        public MediaSourceType? Type { get; set; }

        /// <summary>
        /// Gets or sets the Container.
        /// </summary>
        public string? Container { get; set; }

        /// <summary>
        /// Gets or sets the Size.
        /// </summary>
        public long? Size { get; set; }

        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsRemote
        /// Differentiate internet url vs local network..
        /// </summary>
        public bool IsRemote { get; set; }

        /// <summary>
        /// Gets or sets the ETag.
        /// </summary>
        public string? ETag { get; set; }

        /// <summary>
        /// Gets or sets the RunTimeTicks.
        /// </summary>
        public long? RunTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether ReadAtNativeFramerate.
        /// </summary>
        public bool ReadAtNativeFramerate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IgnoreDts.
        /// </summary>
        public bool IgnoreDts { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IgnoreIndex.
        /// </summary>
        public bool IgnoreIndex { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether GenPtsInput.
        /// </summary>
        public bool GenPtsInput { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether SupportsTranscoding.
        /// </summary>
        public bool SupportsTranscoding { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether SupportsDirectStream.
        /// </summary>
        public bool SupportsDirectStream { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether SupportsDirectPlay.
        /// </summary>
        public bool SupportsDirectPlay { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsInfiniteStream.
        /// </summary>
        public bool IsInfiniteStream { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether RequiresOpening.
        /// </summary>
        public bool RequiresOpening { get; set; }

        /// <summary>
        /// Gets or sets the OpenToken.
        /// </summary>
        public string? OpenToken { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether RequiresClosing.
        /// </summary>
        public bool RequiresClosing { get; set; }

        /// <summary>
        /// Gets or sets the LiveStreamId.
        /// </summary>
        public string? LiveStreamId { get; set; }

        /// <summary>
        /// Gets or sets the Buffer in milliseconds.
        /// </summary>
        public int? BufferMs { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether RequiresLooping.
        /// </summary>
        public bool RequiresLooping { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether SupportsProbing.
        /// </summary>
        public bool SupportsProbing { get; set; }

        /// <summary>
        /// Gets or sets the video type.
        /// </summary>
        public VideoType? VideoType { get; set; }

        /// <summary>
        /// Gets or sets the Iso type.
        /// </summary>
        public IsoType? IsoType { get; set; }

        /// <summary>
        /// Gets or sets the Video3DFormat.
        /// </summary>
        public Video3DFormat? Video3DFormat { get; set; }

        /// <summary>
        /// Gets or sets the MediaStreams.
        /// </summary>
        public List<MediaStream> MediaStreams { get; set; }

        /// <summary>
        /// Gets or sets the MediaAttachments.
        /// </summary>
        public IReadOnlyList<MediaAttachment> MediaAttachments { get; set; }

        /// <summary>
        /// Gets or sets the Formats.
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
        public string[] Formats { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

        /// <summary>
        /// Gets or sets the Bitrate.
        /// </summary>
        public int? Bitrate { get; set; }

        /// <summary>
        /// Gets or sets the Timestamp.
        /// </summary>
        public TransportStreamTimestamp? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the RequiredHttpHeaders.
        /// </summary>
        public Dictionary<string, string> RequiredHttpHeaders { get; set; }

        /// <summary>
        /// Gets or sets the TranscodingUrl.
        /// </summary>
        public string? TranscodingUrl { get; set; }

        /// <summary>
        /// Gets or sets the transcoding sub protocol.
        /// </summary>
        public string? TranscodingSubProtocol { get; set; }

        /// <summary>
        /// Gets or sets the transcoding container.
        /// </summary>
        public string? TranscodingContainer { get; set; }

        /// <summary>
        /// Gets or sets the analyze duration in milliseconds.
        /// </summary>
        public int? AnalyzeDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the transcode reasons.
        /// </summary>
        [JsonIgnore]
        public TranscodeReason[]? TranscodeReasons { get; set; }

        /// <summary>
        /// Gets or sets the DefaultAudioStreamIndex.
        /// </summary>
        public int? DefaultAudioStreamIndex { get; set; }

        /// <summary>
        /// Gets or sets the DefaultSubtitleStreamIndex.
        /// </summary>
        public int? DefaultSubtitleStreamIndex { get; set; }

        /// <summary>
        /// Gets the VideoStream.
        /// </summary>
        [JsonIgnore]
        public MediaStream? VideoStream
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

        /// <summary>
        /// Gets the default audio stream.
        /// </summary>
        /// <param name="defaultIndex">The default index.</param>
        /// <returns>The <see cref="MediaStream"/>.</returns>
        public MediaStream? GetDefaultAudioStream(int? defaultIndex)
        {
            if (defaultIndex.HasValue)
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

        /// <summary>
        /// The GetMediaStream.
        /// </summary>
        /// <param name="type">The <see cref="MediaStreamType"/>.</param>
        /// <param name="index">The index.</param>
        /// <returns>The <see cref="MediaStream"/>.</returns>
        public MediaStream? GetMediaStream(MediaStreamType type, int index)
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

        /// <summary>
        /// Gets the stream count.
        /// </summary>
        /// <param name="type">The <see cref="MediaStreamType"/>.</param>
        /// <returns>The number of streams or null.</returns>
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

        /// <summary>
        /// Checks to see if there is secondary audio.
        /// </summary>
        /// <param name="stream">The <see cref="MediaStream"/>.</param>
        /// <returns>True if there is, false if not, null if there is no streams.</returns>
        public bool? IsSecondaryAudio(MediaStream? stream)
        {
            if (stream == null)
            {
                return null;
            }

            // Look for the first audio track marked as default
            foreach (var currentStream in MediaStreams)
            {
                if (currentStream.Type == MediaStreamType.Audio && currentStream.IsDefault)
                {
                    return currentStream.Index != stream.Index;
                }
            }

            // Look for the first audio track
            foreach (var currentStream in MediaStreams)
            {
                if (currentStream.Type == MediaStreamType.Audio)
                {
                    return currentStream.Index != stream.Index;
                }
            }

            return null;
        }

        /// <summary>
        /// Infers Total Bitrate.
        /// </summary>
        /// <param name="force">True to force.</param>
        public void InferTotalBitrate(bool force = false)
        {
            if (MediaStreams == null)
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
    }
}
