using System.IO;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaInfo;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.MediaInfo
{
    class FFProbeAudioInfo
    {
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IItemRepository _itemRepo;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public FFProbeAudioInfo(IMediaEncoder mediaEncoder, IItemRepository itemRepo)
        {
            _mediaEncoder = mediaEncoder;
            _itemRepo = itemRepo;
        }

        public async Task<ItemUpdateType> Probe<T>(T item, CancellationToken cancellationToken)
            where T : Audio
        {
            var result = await GetMediaInfo(item, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            FFProbeHelpers.NormalizeFFProbeResult(result);

            cancellationToken.ThrowIfCancellationRequested();

            await Fetch(item, cancellationToken, result).ConfigureAwait(false);

            return ItemUpdateType.MetadataImport;
        }

        private async Task<InternalMediaInfoResult> GetMediaInfo(BaseItem item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            const InputType type = InputType.File;
            var inputPath = new[] { item.Path };

            return await _mediaEncoder.GetMediaInfo(inputPath, type, false, cancellationToken).ConfigureAwait(false);
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
            var mediaStreams = MediaEncoderHelpers.GetMediaInfo(data).MediaStreams;

            audio.HasEmbeddedImage = mediaStreams.Any(i => i.Type == MediaStreamType.Video);

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

            if (data.format.tags != null)
            {
                FetchDataFromTags(audio, data.format.tags);
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
                    foreach (var person in Split(composer))
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

            // Several different forms of albumartist
            audio.AlbumArtist = FFProbeHelpers.GetDictionaryValue(tags, "albumartist") ?? FFProbeHelpers.GetDictionaryValue(tags, "album artist") ?? FFProbeHelpers.GetDictionaryValue(tags, "album_artist");

            // Track number
            audio.IndexNumber = GetDictionaryDiscValue(tags, "track");

            // Disc number
            audio.ParentIndexNumber = GetDictionaryDiscValue(tags, "disc");

            audio.ProductionYear = FFProbeHelpers.GetDictionaryNumericValue(tags, "date");

            // Several different forms of retaildate
            audio.PremiereDate = FFProbeHelpers.GetDictionaryDateTime(tags, "retaildate") ?? FFProbeHelpers.GetDictionaryDateTime(tags, "retail date") ?? FFProbeHelpers.GetDictionaryDateTime(tags, "retail_date");

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
        /// <returns>System.String[][].</returns>
        private IEnumerable<string> Split(string val)
        {
            // Only use the comma as a delimeter if there are no slashes or pipes. 
            // We want to be careful not to split names that have commas in them
            var delimeter = _nameDelimiters.Any(i => val.IndexOf(i) != -1) ? _nameDelimiters : new[] { ',' };

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
                if (val.IndexOf(whitelistArtist, StringComparison.OrdinalIgnoreCase) != -1)
                {
                    val = val.Replace(whitelistArtist, "|", StringComparison.OrdinalIgnoreCase);

                    // TODO: Preserve casing from original tag
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
                var studios = Split(val).Where(i => !audio.HasArtist(i));

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

                foreach (var genre in Split(val))
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
