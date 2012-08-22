using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

            Fetch(video, await FFProbe.Run(video, GetFFProbeOutputPath(video)).ConfigureAwait(false));
        }

        private string GetFFProbeOutputPath(Video item)
        {
            string outputDirectory = Path.Combine(Kernel.Instance.ApplicationPaths.FFProbeVideoCacheDirectory, item.Id.ToString().Substring(0, 1));

            return Path.Combine(outputDirectory, item.Id + "-" + item.DateModified.Ticks + ".js");
        }

        private void Fetch(Video video, FFProbeResult data)
        {
            if (!string.IsNullOrEmpty(data.format.duration))
            {
                video.RunTimeTicks = TimeSpan.FromSeconds(double.Parse(data.format.duration)).Ticks;
            }

            if (!string.IsNullOrEmpty(data.format.bit_rate))
            {
                video.BitRate = int.Parse(data.format.bit_rate);
            }

            MediaStream videoStream = data.streams.FirstOrDefault(s => s.codec_type.Equals("video", StringComparison.OrdinalIgnoreCase));

            if (videoStream != null)
            {
                FetchFromVideoStream(video, videoStream);
            }
        }

        private void FetchFromVideoStream(Video video, MediaStream stream)
        {
            video.Codec = stream.codec_name;
            video.Width = stream.width;
            video.Height = stream.height;
            video.AspectRatio = stream.display_aspect_ratio;
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
