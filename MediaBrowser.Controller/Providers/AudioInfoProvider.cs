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
    public class AudioInfoProvider : BaseMetadataProvider
    {
        public override bool Supports(BaseEntity item)
        {
            return item is Audio;
        }

        public async override Task Fetch(BaseEntity item, ItemResolveEventArgs args)
        {
            Audio audio = item as Audio;

            string outputDirectory = Path.Combine(Kernel.Instance.ApplicationPaths.FFProbeAudioCacheDirectory, item.Id.ToString().Substring(0, 1));

            string outputPath = Path.Combine(outputDirectory, item.Id + "-" + item.DateModified.Ticks + ".js");

            FFProbeResult data = await FFProbe.Run(audio, outputPath);

            MediaStream stream = data.streams.FirstOrDefault(s => s.codec_type.Equals("audio", StringComparison.OrdinalIgnoreCase));

            audio.Channels = stream.channels;

            string bitrate = null;
            
            if (!string.IsNullOrEmpty(stream.sample_rate))
            {
                audio.SampleRate = int.Parse(stream.sample_rate);

                bitrate = stream.bit_rate;
            }

            if (string.IsNullOrEmpty(bitrate))
            {
                bitrate = data.format.bit_rate;
            }

            if (!string.IsNullOrEmpty(bitrate))
            {
                audio.BitRate = int.Parse(bitrate);
            }
        }

        private string GetOutputCachePath(BaseItem item)
        {
            string outputDirectory = Path.Combine(Kernel.Instance.ApplicationPaths.FFProbeAudioCacheDirectory, item.Id.ToString().Substring(0, 1));

            return Path.Combine(outputDirectory, item.Id + "-" + item.DateModified.Ticks + ".js");
        }

        public override void Init()
        {
            base.Init();

            // Do this now so that we don't have to do this on every operation, which would require us to create a lock in order to maintain thread-safety
            for (int i = 0; i <= 9; i++)
            {
                EnsureDirectory(Path.Combine(Kernel.Instance.ApplicationPaths.FFProbeAudioCacheDirectory, i.ToString()));
            }

            EnsureDirectory(Path.Combine(Kernel.Instance.ApplicationPaths.FFProbeAudioCacheDirectory, "a"));
            EnsureDirectory(Path.Combine(Kernel.Instance.ApplicationPaths.FFProbeAudioCacheDirectory, "b"));
            EnsureDirectory(Path.Combine(Kernel.Instance.ApplicationPaths.FFProbeAudioCacheDirectory, "c"));
            EnsureDirectory(Path.Combine(Kernel.Instance.ApplicationPaths.FFProbeAudioCacheDirectory, "d"));
            EnsureDirectory(Path.Combine(Kernel.Instance.ApplicationPaths.FFProbeAudioCacheDirectory, "e"));
            EnsureDirectory(Path.Combine(Kernel.Instance.ApplicationPaths.FFProbeAudioCacheDirectory, "f"));
        }

        private void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
