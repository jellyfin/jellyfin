using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.MediaInfo;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaBrowser.Model.Dto
{
    public class MediaSourceInfo
    {
        public MediaProtocol Protocol { get; set; }
        public string Id { get; set; }

        public string Path { get; set; }

        public MediaSourceType Type { get; set; }

        public string Container { get; set; }
        public long? Size { get; set; }

        public string Name { get; set; }

        public long? RunTimeTicks { get; set; }

        public VideoType? VideoType { get; set; }

        public IsoType? IsoType { get; set; }

        public Video3DFormat? Video3DFormat { get; set; }

        public List<MediaStream> MediaStreams { get; set; }
        public List<string> PlayableStreamFileNames { get; set; }

        public List<string> Formats { get; set; }

        public int? Bitrate { get; set; }

        public TransportStreamTimestamp? Timestamp { get; set; }
        public Dictionary<string, string> RequiredHttpHeaders { get; set; }

        public MediaSourceInfo()
        {
            Formats = new List<string>();
            MediaStreams = new List<MediaStream>();
            RequiredHttpHeaders = new Dictionary<string, string>();
            PlayableStreamFileNames = new List<string>();
        }

        public int? DefaultAudioStreamIndex { get; set; }
        public int? DefaultSubtitleStreamIndex { get; set; }

        [IgnoreDataMember]
        public MediaStream DefaultAudioStream
        {
            get
            {
                if (DefaultAudioStreamIndex.HasValue)
                {
                    var val = DefaultAudioStreamIndex.Value;

                    foreach (MediaStream i in MediaStreams)
                    {
                        if (i.Type == MediaStreamType.Audio && i.Index == val)
                        {
                            return i;
                        }
                    }
                }

                foreach (MediaStream i in MediaStreams)
                {
                    if (i.Type == MediaStreamType.Audio && i.IsDefault)
                    {
                        return i;
                    }
                }

                foreach (MediaStream i in MediaStreams)
                {
                    if (i.Type == MediaStreamType.Audio)
                    {
                        return i;
                    }
                }

                return null;
            }
        }

        [IgnoreDataMember]
        public MediaStream VideoStream
        {
            get
            {
                foreach (MediaStream i in MediaStreams)
                {
                    if (i.Type == MediaStreamType.Video && StringHelper.IndexOfIgnoreCase((i.Codec ?? string.Empty), "jpeg") == -1)
                    {
                        return i;
                    }
                }

                return null;
            }
        }

        public MediaStream GetMediaStream(MediaStreamType type, int index)
        {
            foreach (MediaStream i in MediaStreams)
            {
                if (i.Type == type && i.Index == index)
                {
                    return i;
                }
            }

            return null;
        }
    }
}
