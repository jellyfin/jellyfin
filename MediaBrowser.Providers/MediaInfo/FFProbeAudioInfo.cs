using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.MediaInfo
{
    class FFProbeAudioInfo
    {
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IItemRepository _itemRepo;
        private readonly IApplicationPaths _appPaths;
        private readonly IJsonSerializer _json;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public FFProbeAudioInfo(IMediaEncoder mediaEncoder, IItemRepository itemRepo, IApplicationPaths appPaths, IJsonSerializer json)
        {
            _mediaEncoder = mediaEncoder;
            _itemRepo = itemRepo;
            _appPaths = appPaths;
            _json = json;
        }

        public async Task<ItemUpdateType> Probe<T>(T item, CancellationToken cancellationToken)
            where T : Audio
        {
            if (item.IsArchive)
            {
                var ext = Path.GetExtension(item.Path) ?? string.Empty;
                item.Container = ext.TrimStart('.');
                return ItemUpdateType.MetadataImport;
            }

            var result = await GetMediaInfo(item, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            FFProbeHelpers.NormalizeFFProbeResult(result);

            cancellationToken.ThrowIfCancellationRequested();

            await Fetch(item, cancellationToken, result).ConfigureAwait(false);

            return ItemUpdateType.MetadataImport;
        }

        private const string SchemaVersion = "1";

        private async Task<InternalMediaInfoResult> GetMediaInfo(BaseItem item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var idString = item.Id.ToString("N");
            var cachePath = Path.Combine(_appPaths.CachePath,
                "ffprobe-audio",
                idString.Substring(0, 2), idString, "v" + SchemaVersion + _mediaEncoder.Version + item.DateModified.Ticks.ToString(_usCulture) + ".json");

            try
            {
                return _json.DeserializeFromFile<InternalMediaInfoResult>(cachePath);
            }
            catch (FileNotFoundException)
            {

            }
            catch (DirectoryNotFoundException)
            {
            }

            var inputPath = new[] { item.Path };

            var result = await _mediaEncoder.GetMediaInfo(inputPath, MediaProtocol.File, false, cancellationToken).ConfigureAwait(false);

            Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
            _json.SerializeToFile(result, cachePath);

            return result;
        }

        /// <summary>
        /// Fetches the specified audio.
        /// </summary>
        /// <param name="audio">The audio.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="data">The data.</param>
        /// <returns>Task.</returns>
        protected Task Fetch(Audio audio, CancellationToken cancellationToken, InternalMediaInfoResult data)
        {
            var mediaInfo = MediaEncoderHelpers.GetMediaInfo(data);
            var mediaStreams = mediaInfo.MediaStreams;

            audio.FormatName = mediaInfo.Format;
            audio.TotalBitrate = mediaInfo.TotalBitrate;
            audio.HasEmbeddedImage = mediaStreams.Any(i => i.Type == MediaStreamType.EmbeddedImage);

            if (data.streams != null)
            {
                // Get the first audio stream
                var stream = data.streams.FirstOrDefault(s => string.Equals(s.codec_type, "audio", StringComparison.OrdinalIgnoreCase));

                if (stream != null)
                {
                    // Get duration from stream properties
                    var duration = stream.duration;

                    // If it's not there go into format properties
                    if (string.IsNullOrEmpty(duration))
                    {
                        duration = data.format.duration;
                    }

                    // If we got something, parse it
                    if (!string.IsNullOrEmpty(duration))
                    {
                        audio.RunTimeTicks = TimeSpan.FromSeconds(double.Parse(duration, _usCulture)).Ticks;
                    }
                }
            }

            if (data.format != null)
            {
                var extension = (Path.GetExtension(audio.Path) ?? string.Empty).TrimStart('.');

                audio.Container = extension;

                if (!string.IsNullOrEmpty(data.format.size))
                {
                    audio.Size = long.Parse(data.format.size, _usCulture);
                }
                else
                {
                    audio.Size = null;
                }

                if (data.format.tags != null)
                {
                    FetchDataFromTags(audio, data.format.tags);
                }
            }

            return _itemRepo.SaveMediaStreams(audio.Id, mediaStreams, cancellationToken);
        }

        /// <summary>
        /// Fetches data from the tags dictionary
        /// </summary>
        /// <param name="audio">The audio.</param>
        /// <param name="tags">The tags.</param>
        private void FetchDataFromTags(Audio audio, Dictionary<string, string> tags)
        {
            var title = FFProbeHelpers.GetDictionaryValue(tags, "title");

            // Only set Name if title was found in the dictionary
            if (!string.IsNullOrEmpty(title))
            {
                audio.Name = title;
            }

            if (!audio.LockedFields.Contains(MetadataFields.Cast))
            {
                audio.People.Clear();

                var composer = FFProbeHelpers.GetDictionaryValue(tags, "composer");

                if (!string.IsNullOrWhiteSpace(composer))
                {
                    foreach (var person in Split(composer, false))
                    {
                        audio.AddPerson(new PersonInfo { Name = person, Type = PersonType.Composer });
                    }
                }
            }

            audio.Album = FFProbeHelpers.GetDictionaryValue(tags, "album");

            var artist = FFProbeHelpers.GetDictionaryValue(tags, "artist");

            if (string.IsNullOrWhiteSpace(artist))
            {
                audio.Artists.Clear();
            }
            else
            {
                audio.Artists = SplitArtists(artist)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            }

            var albumArtist = FFProbeHelpers.GetDictionaryValue(tags, "albumartist") ?? FFProbeHelpers.GetDictionaryValue(tags, "album artist") ?? FFProbeHelpers.GetDictionaryValue(tags, "album_artist");

            if (string.IsNullOrWhiteSpace(albumArtist))
            {
                audio.AlbumArtists = new List<string>();
            }
            else
            {
                audio.AlbumArtists = SplitArtists(albumArtist)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            }

            // Track number
            audio.IndexNumber = GetDictionaryDiscValue(tags, "track");

            // Disc number
            audio.ParentIndexNumber = GetDictionaryDiscValue(tags, "disc");

            audio.ProductionYear = FFProbeHelpers.GetDictionaryNumericValue(tags, "date");

            // Several different forms of retaildate
            audio.PremiereDate = FFProbeHelpers.GetDictionaryDateTime(tags, "retaildate") ??
                FFProbeHelpers.GetDictionaryDateTime(tags, "retail date") ??
                FFProbeHelpers.GetDictionaryDateTime(tags, "retail_date") ??
                FFProbeHelpers.GetDictionaryDateTime(tags, "date");

            // If we don't have a ProductionYear try and get it from PremiereDate
            if (audio.PremiereDate.HasValue && !audio.ProductionYear.HasValue)
            {
                audio.ProductionYear = audio.PremiereDate.Value.ToLocalTime().Year;
            }

            if (!audio.LockedFields.Contains(MetadataFields.Genres))
            {
                FetchGenres(audio, tags);
            }

            if (!audio.LockedFields.Contains(MetadataFields.Studios))
            {
                audio.Studios.Clear();

                // There's several values in tags may or may not be present
                FetchStudios(audio, tags, "organization");
                FetchStudios(audio, tags, "ensemble");
                FetchStudios(audio, tags, "publisher");
            }

            audio.SetProviderId(MetadataProviders.MusicBrainzAlbumArtist, FFProbeHelpers.GetDictionaryValue(tags, "MusicBrainz Album Artist Id"));
            audio.SetProviderId(MetadataProviders.MusicBrainzArtist, FFProbeHelpers.GetDictionaryValue(tags, "MusicBrainz Artist Id"));
            audio.SetProviderId(MetadataProviders.MusicBrainzAlbum, FFProbeHelpers.GetDictionaryValue(tags, "MusicBrainz Album Id"));
            audio.SetProviderId(MetadataProviders.MusicBrainzReleaseGroup, FFProbeHelpers.GetDictionaryValue(tags, "MusicBrainz Release Group Id"));
        }

        private readonly char[] _nameDelimiters = { '/', '|', ';', '\\' };

        /// <summary>
        /// Splits the specified val.
        /// </summary>
        /// <param name="val">The val.</param>
        /// <param name="allowCommaDelimiter">if set to <c>true</c> [allow comma delimiter].</param>
        /// <returns>System.String[][].</returns>
        private IEnumerable<string> Split(string val, bool allowCommaDelimiter)
        {
            // Only use the comma as a delimeter if there are no slashes or pipes. 
            // We want to be careful not to split names that have commas in them
            var delimeter = !allowCommaDelimiter || _nameDelimiters.Any(i => val.IndexOf(i) != -1) ?
                _nameDelimiters :
                new[] { ',' };

            return val.Split(delimeter, StringSplitOptions.RemoveEmptyEntries)
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Select(i => i.Trim());
        }

        private const string ArtistReplaceValue = " | ";

        private IEnumerable<string> SplitArtists(string val)
        {
            val = val.Replace(" featuring ", ArtistReplaceValue, StringComparison.OrdinalIgnoreCase)
                .Replace(" feat. ", ArtistReplaceValue, StringComparison.OrdinalIgnoreCase);

            var artistsFound = new List<string>();

            foreach (var whitelistArtist in GetSplitWhitelist())
            {
                var originalVal = val;
                val = val.Replace(whitelistArtist, "|", StringComparison.OrdinalIgnoreCase);

                if (!string.Equals(originalVal, val, StringComparison.OrdinalIgnoreCase))
                {
                    artistsFound.Add(whitelistArtist);
                }
            }

            // Only use the comma as a delimeter if there are no slashes or pipes. 
            // We want to be careful not to split names that have commas in them
            var delimeter = _nameDelimiters;

            var artists = val.Split(delimeter, StringSplitOptions.RemoveEmptyEntries)
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Select(i => i.Trim());

            artistsFound.AddRange(artists);
            return artistsFound;
        }


        private List<string> _splitWhiteList = null;

        private IEnumerable<string> GetSplitWhitelist()
        {
            if (_splitWhiteList == null)
            {
                var file = GetType().Namespace + ".whitelist.txt";

                using (var stream = GetType().Assembly.GetManifestResourceStream(file))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var list = new List<string>();

                        while (!reader.EndOfStream)
                        {
                            var val = reader.ReadLine();

                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                list.Add(val);
                            }
                        }

                        _splitWhiteList = list;
                    }
                }
            }

            return _splitWhiteList;
        }

        /// <summary>
        /// Gets the studios from the tags collection
        /// </summary>
        /// <param name="audio">The audio.</param>
        /// <param name="tags">The tags.</param>
        /// <param name="tagName">Name of the tag.</param>
        private void FetchStudios(Audio audio, Dictionary<string, string> tags, string tagName)
        {
            var val = FFProbeHelpers.GetDictionaryValue(tags, tagName);

            if (!string.IsNullOrEmpty(val))
            {
                // Sometimes the artist name is listed here, account for that
                var studios = Split(val, true).Where(i => !audio.HasArtist(i));

                foreach (var studio in studios)
                {
                    audio.AddStudio(studio);
                }
            }
        }

        /// <summary>
        /// Gets the genres from the tags collection
        /// </summary>
        /// <param name="audio">The audio.</param>
        /// <param name="tags">The tags.</param>
        private void FetchGenres(Audio audio, Dictionary<string, string> tags)
        {
            var val = FFProbeHelpers.GetDictionaryValue(tags, "genre");

            if (!string.IsNullOrEmpty(val))
            {
                audio.Genres.Clear();

                foreach (var genre in Split(val, true))
                {
                    audio.AddGenre(genre);
                }
            }
        }

        /// <summary>
        /// Gets the disc number, which is sometimes can be in the form of '1', or '1/3'
        /// </summary>
        /// <param name="tags">The tags.</param>
        /// <param name="tagName">Name of the tag.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        private int? GetDictionaryDiscValue(Dictionary<string, string> tags, string tagName)
        {
            var disc = FFProbeHelpers.GetDictionaryValue(tags, tagName);

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
}
