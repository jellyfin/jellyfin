using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Logging;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.FFMpeg;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Providers
{
    [Export(typeof(BaseMetadataProvider))]
    public class VideoInfoProvider : BaseMetadataProvider
    {
        public override bool Supports(BaseEntity item)
        {
            return item is Video;
        }

        public override MetadataProviderPriority Priority
        {
            // Give this second priority
            // Give metadata xml providers a chance to fill in data first, so that we can skip this whenever possible
            get { return MetadataProviderPriority.Second; }
        }

        public override async Task FetchAsync(BaseEntity item, ItemResolveEventArgs args)
        {
            await Task.Run(() =>
            {
                Video video = item as Video;

                if (video.VideoType != VideoType.VideoFile)
                {
                    // Not supported yet
                    return;
                }

                if (CanSkip(video))
                {
                    return;
                }

                Fetch(video, FFProbe.Run(video));
            });
        }

        private void Fetch(Video video, FFProbeResult data)
        {
            if (data == null)
            {
                Logger.LogInfo("Null FFProbeResult for {0} {1}", video.Id, video.Name);
                return;
            }

            if (!string.IsNullOrEmpty(data.format.duration))
            {
                video.RunTimeTicks = TimeSpan.FromSeconds(double.Parse(data.format.duration)).Ticks;
            }

            if (!string.IsNullOrEmpty(data.format.bit_rate))
            {
                video.BitRate = int.Parse(data.format.bit_rate);
            }

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
            AudioStream audio = new AudioStream();

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

            audio.Language = AudioInfoProvider.GetDictionaryValue(stream.tags, "language");

            List<AudioStream> streams = (video.AudioStreams ?? new AudioStream[] { }).ToList();
            streams.Add(audio);
            video.AudioStreams = streams;
        }
        
        /// <summary>
        /// Determines if there's already enough info in the Video object to allow us to skip running ffprobe
        /// </summary>
        private bool CanSkip(Video video)
        {
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

            if (video.FrameRate == 0 || video.Height == 0 || video.Width == 0 || video.BitRate == 0)
            {
                return false;
            }
            
            return true;
        }

        public override void Init()
        {
            base.Init();

            AudioInfoProvider.EnsureCacheSubFolders(Kernel.Instance.ApplicationPaths.FFProbeVideoCacheDirectory);
        }
    }
}
