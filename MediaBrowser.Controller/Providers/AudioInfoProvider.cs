using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.FFMpeg;
using MediaBrowser.Controller.Library;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    [Export(typeof(BaseMetadataProvider))]
    public class AudioInfoProvider : BaseMediaInfoProvider<Audio>
    {
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.First; }
        }

        protected override string CacheDirectory
        {
            get { return Kernel.Instance.ApplicationPaths.FFProbeAudioCacheDirectory; }
        }

        protected override void Fetch(Audio audio, FFProbeResult data)
        {
            MediaStream stream = data.streams.First(s => s.codec_type.Equals("audio", StringComparison.OrdinalIgnoreCase));

            audio.Channels = stream.channels;

            if (!string.IsNullOrEmpty(stream.sample_rate))
            {
                audio.SampleRate = int.Parse(stream.sample_rate);
            }

            string bitrate = stream.bit_rate;
            string duration = stream.duration;

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

            string composer = GetDictionaryValue(tags, "composer");

            if (!string.IsNullOrEmpty(composer))
            {
                audio.AddPerson(new PersonInfo { Name = composer, Type = "Composer" });
            }

            audio.Album = GetDictionaryValue(tags, "album");
            audio.Artist = GetDictionaryValue(tags, "artist");
            audio.AlbumArtist = GetDictionaryValue(tags, "albumartist") ?? GetDictionaryValue(tags, "album artist") ?? GetDictionaryValue(tags, "album_artist");

            audio.IndexNumber = GetDictionaryNumericValue(tags, "track");
            audio.ParentIndexNumber = GetDictionaryDiscValue(tags);

            audio.Language = GetDictionaryValue(tags, "language");

            audio.ProductionYear = GetDictionaryNumericValue(tags, "date");

            audio.PremiereDate = GetDictionaryDateTime(tags, "retaildate") ?? GetDictionaryDateTime(tags, "retail date") ?? GetDictionaryDateTime(tags, "retail_date");

            FetchGenres(audio, tags);

            FetchStudios(audio, tags, "organization");
            FetchStudios(audio, tags, "ensemble");
            FetchStudios(audio, tags, "publisher");
        }

        private void FetchStudios(Audio audio, Dictionary<string, string> tags, string tagName)
        {
            string val = GetDictionaryValue(tags, tagName);

            if (!string.IsNullOrEmpty(val))
            {
                var list = audio.Studios ?? new List<string>();
                list.AddRange(val.Split('/'));
                audio.Studios = list;
            }
        }

        private void FetchGenres(Audio audio, Dictionary<string, string> tags)
        {
            string val = GetDictionaryValue(tags, "genre");

            if (!string.IsNullOrEmpty(val))
            {
                var list = audio.Genres ?? new List<string>();
                list.AddRange(val.Split('/'));
                audio.Genres = list;
            }
        }

        private int? GetDictionaryDiscValue(Dictionary<string, string> tags)
        {
            string disc = GetDictionaryValue(tags, "disc");

            if (!string.IsNullOrEmpty(disc))
            {
                disc = disc.Split('/')[0];

                int num;

                if (int.TryParse(disc, out num))
                {
                    return num;
                }
            }

            return null;
        }
    }

    public abstract class BaseMediaInfoProvider<T> : BaseMetadataProvider
        where T : BaseItem
    {
        protected abstract string CacheDirectory { get; }

        public override bool Supports(BaseEntity item)
        {
            return item is T;
        }

        public override async Task FetchAsync(BaseEntity item, ItemResolveEventArgs args)
        {
            await Task.Run(() =>
            {
                /*T myItem = item as T;

                if (CanSkipFFProbe(myItem))
                {
                    return;
                }

                FFProbeResult result = FFProbe.Run(myItem, CacheDirectory);

                if (result == null)
                {
                    Logger.LogInfo("Null FFProbeResult for {0} {1}", item.Id, item.Name);
                    return;
                }

                if (result.format != null && result.format.tags != null)
                {
                    result.format.tags = ConvertDictionaryToCaseInSensitive(result.format.tags);
                }

                if (result.streams != null)
                {
                    foreach (MediaStream stream in result.streams)
                    {
                        if (stream.tags != null)
                        {
                            stream.tags = ConvertDictionaryToCaseInSensitive(stream.tags);
                        }
                    }
                }

                Fetch(myItem, result);*/
            });
        }

        protected abstract void Fetch(T item, FFProbeResult result);

        protected virtual bool CanSkipFFProbe(T item)
        {
            return false;
        }

        protected string GetDictionaryValue(Dictionary<string, string> tags, string key)
        {
            if (tags == null)
            {
                return null;
            }

            if (!tags.ContainsKey(key))
            {
                return null;
            }

            return tags[key];
        }

        protected int? GetDictionaryNumericValue(Dictionary<string, string> tags, string key)
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

        protected DateTime? GetDictionaryDateTime(Dictionary<string, string> tags, string key)
        {
            string val = GetDictionaryValue(tags, key);

            if (!string.IsNullOrEmpty(val))
            {
                DateTime i;

                if (DateTime.TryParse(val, out i))
                {
                    return i.ToUniversalTime();
                }
            }

            return null;
        }
        
        private Dictionary<string, string> ConvertDictionaryToCaseInSensitive(Dictionary<string, string> dict)
        {
            var newDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string key in dict.Keys)
            {
                newDict[key] = dict[key];
            }

            return newDict;
        }
    }
}
