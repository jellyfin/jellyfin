using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.FFMpeg;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;

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

            MediaStream stream = data.streams.First(s => s.codec_type.Equals("audio", StringComparison.OrdinalIgnoreCase));

            string bitrate = null;
            string duration = null;

            audio.Channels = stream.channels;

            if (!string.IsNullOrEmpty(stream.sample_rate))
            {
                audio.SampleRate = int.Parse(stream.sample_rate);
            }

            bitrate = stream.bit_rate;
            duration = stream.duration;

            if (string.IsNullOrEmpty(bitrate))
            {
                bitrate = data.format.bit_rate;
            }

            if (string.IsNullOrEmpty(duration))
            {
                duration = data.format.duration;
            }

            if (!string.IsNullOrEmpty(bitrate))
            {
                audio.BitRate = int.Parse(bitrate);
            }

            if (!string.IsNullOrEmpty(duration))
            {
                audio.RunTimeTicks = TimeSpan.FromSeconds(double.Parse(duration)).Ticks;
            }

            if (data.format.tags != null)
            {
                FetchDataFromTags(audio, data.format.tags);
            }
        }

        private void FetchDataFromTags(Audio audio, Dictionary<string, string> tags)
        {
            string title = GetDictionaryValue(tags, "title");

            if (!string.IsNullOrEmpty(title))
            {
                audio.Name = title;
            }

            audio.Album = GetDictionaryValue(tags, "album");
            audio.Composer = GetDictionaryValue(tags, "composer");
            audio.Artist = GetDictionaryValue(tags, "artist");
            audio.AlbumArtist = GetDictionaryValue(tags, "albumartist") ?? GetDictionaryValue(tags, "album artist") ?? GetDictionaryValue(tags, "album_artist");

            audio.IndexNumber = GetDictionaryNumericValue(tags, "track");
            audio.ParentIndexNumber = GetDictionaryDiscValue(tags);

            audio.Language = GetDictionaryValue(tags, "language");

            audio.ProductionYear = GetDictionaryNumericValue(tags, "date");

            audio.PremiereDate = GetDictionaryDateTime(tags, "retaildate") ?? GetDictionaryDateTime(tags, "retail date") ?? GetDictionaryDateTime(tags, "retail_date");
            
            MediaBrowser.Common.Logging.Logger.LogInfo(tags.Comparer.GetType().Name.ToString());
        }

        private int? GetDictionaryDiscValue(Dictionary<string, string> tags)
        {
            string[] keys = tags.Keys.ToArray();

            for (int i = 0; i < keys.Length; i++)
            {
                string currentKey = keys[i];

                if ("disc".Equals(currentKey, StringComparison.OrdinalIgnoreCase))
                {
                    string disc = tags[currentKey];

                    if (!string.IsNullOrEmpty(disc))
                    {
                        disc = disc.Split('/')[0];

                        int num;

                        if (int.TryParse(disc, out num))
                        {
                            return num;
                        }
                    }

                    break;
                }
            }

            return null;
        }
        
        private string GetDictionaryValue(Dictionary<string, string> tags, string key)
        {
            string[] keys = tags.Keys.ToArray();

            for (int i = 0; i < keys.Length; i++)
            {
                string currentKey = keys[i];

                if (key.Equals(currentKey, StringComparison.OrdinalIgnoreCase))
                {
                    return tags[currentKey];
                }
            }

            return null;
        }

        private int? GetDictionaryNumericValue(Dictionary<string, string> tags, string key)
        {
            string val = GetDictionaryValue(tags, key);

            if (!string.IsNullOrEmpty(val))
            {
                int i;

                if (int.TryParse(val, out i))
                {
                    return i;
                }
            }

            return null;
        }

        private DateTime? GetDictionaryDateTime(Dictionary<string, string> tags, string key)
        {
            string val = GetDictionaryValue(tags, key);

            if (!string.IsNullOrEmpty(val))
            {
                DateTime i;

                if (DateTime.TryParse(val, out i))
                {
                    return i;
                }
            }

            return null;
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
