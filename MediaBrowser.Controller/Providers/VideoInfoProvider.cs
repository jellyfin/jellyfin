using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.FFMpeg;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Providers
{
    //[Export(typeof(BaseMetadataProvider))]
    public class VideoInfoProvider : BaseMetadataProvider
    {
        public override bool Supports(BaseEntity item)
        {
            return item is Video;
        }

        public override MetadataProviderPriority Priority
        {
            // Give this second priority
            // Give metadata xml providers a chance to fill in data first
            // Then we can skip this step whenever possible
            get { return MetadataProviderPriority.Second; }
        }

        public override async Task Fetch(BaseEntity item, ItemResolveEventArgs args)
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

            string outputDirectory = Path.Combine(Kernel.Instance.ApplicationPaths.FFProbeVideoCacheDirectory, item.Id.ToString().Substring(0, 1));

            string outputPath = Path.Combine(outputDirectory, item.Id + "-" + item.DateModified.Ticks + ".js");

            FFProbeResult data = await FFProbe.Run(video, outputPath);
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

            if (string.IsNullOrEmpty(video.Codec))
            {
                return false;
            }

            if (string.IsNullOrEmpty(video.ScanType))
            {
                return false;
            }

            if (string.IsNullOrEmpty(video.FrameRate))
            {
                return false;
            }

            if (video.Height == 0 || video.Width == 0 || video.BitRate == 0)
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
