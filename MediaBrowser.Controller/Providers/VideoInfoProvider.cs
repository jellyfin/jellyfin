using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.FFMpeg;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace MediaBrowser.Controller.Providers
{
    [Export(typeof(BaseMetadataProvider))]
    public class VideoInfoProvider : BaseMediaInfoProvider<Video>
    {
        public override MetadataProviderPriority Priority
        {
            // Give this second priority
            // Give metadata xml providers a chance to fill in data first, so that we can skip this whenever possible
            get { return MetadataProviderPriority.Second; }
        }

        protected override string CacheDirectory
        {
            get { return Kernel.Instance.ApplicationPaths.FFProbeVideoCacheDirectory; }
        }
        
        protected override void Fetch(Video video, FFProbeResult data)
        {
            if (data.format != null)
            {
                if (!string.IsNullOrEmpty(data.format.duration))
                {
                    video.RunTimeTicks = TimeSpan.FromSeconds(double.Parse(data.format.duration)).Ticks;
                }

                if (!string.IsNullOrEmpty(data.format.bit_rate))
                {
                    video.BitRate = int.Parse(data.format.bit_rate);
                }
            }

            if (data.streams != null)
            {
                // For now, only read info about first video stream
                // Files with multiple video streams are possible, but extremely rare
                bool foundVideo = false;

                foreach (MediaStream stream in data.streams)
                {
                    if (stream.codec_type.Equals("video", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!foundVideo)
                        {
                            FetchFromVideoStream(video, stream);
                        }

                        foundVideo = true;
                    }
                    else if (stream.codec_type.Equals("audio", StringComparison.OrdinalIgnoreCase))
                    {
                        FetchFromAudioStream(video, stream);
                    }
                }
            }
        }

        private void FetchFromVideoStream(Video video, MediaStream stream)
        {
            video.Codec = stream.codec_name;
            video.Width = stream.width;
            video.Height = stream.height;
            video.AspectRatio = stream.display_aspect_ratio;

            if (!string.IsNullOrEmpty(stream.avg_frame_rate))
            {
                string[] parts = stream.avg_frame_rate.Split('/');

                if (parts.Length == 2)
                {
                    video.FrameRate = float.Parse(parts[0]) / float.Parse(parts[1]);
                }
                else
                {
                    video.FrameRate = float.Parse(parts[0]);
                }
            }
        }

        private void FetchFromAudioStream(Video video, MediaStream stream)
        {
            var audio = new AudioStream{};

            audio.Codec = stream.codec_name;

            if (!string.IsNullOrEmpty(stream.bit_rate))
            {
                audio.BitRate = int.Parse(stream.bit_rate);
            }

            audio.Channels = stream.channels;

            if (!string.IsNullOrEmpty(stream.sample_rate))
            {
                audio.SampleRate = int.Parse(stream.sample_rate);
            }

            audio.Language = GetDictionaryValue(stream.tags, "language");

            List<AudioStream> streams = video.AudioStreams ?? new List<AudioStream>();
            streams.Add(audio);
            video.AudioStreams = streams;
        }

        private void FetchFromSubtitleStream(Video video, MediaStream stream)
        {
            var subtitle = new SubtitleStream{};

            subtitle.Language = GetDictionaryValue(stream.tags, "language");

            List<SubtitleStream> streams = video.Subtitles ?? new List<SubtitleStream>();
            streams.Add(subtitle);
            video.Subtitles = streams;
        }
        
        /// <summary>
        /// Determines if there's already enough info in the Video object to allow us to skip running ffprobe
        /// </summary>
        protected override bool CanSkipFFProbe(Video video)
        {
            if (video.VideoType != VideoType.VideoFile)
            {
                // Not supported yet
                return true;
            }
            
            if (video.AudioStreams == null || !video.AudioStreams.Any())
            {
                return false;
            }

            if (string.IsNullOrEmpty(video.AspectRatio))
            {
                return false;
            }

            if (string.IsNullOrEmpty(video.Codec))
            {
                return false;
            }

            if (string.IsNullOrEmpty(video.ScanType))
            {
                return false;
            }

            if (!video.RunTimeTicks.HasValue || video.RunTimeTicks.Value == 0)
            {
                return false;
            }

            if (Convert.ToInt32(video.FrameRate) == 0 || video.Height == 0 || video.Width == 0 || video.BitRate == 0)
            {
                return false;
            }
            
            return true;
        }
    }
}
