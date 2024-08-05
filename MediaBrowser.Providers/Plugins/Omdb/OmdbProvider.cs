#nullable disable

#pragma warning disable CS159, SA1300

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Providers.Plugins.Omdb
{
    /// <summary>Provider for OMDB service.</summary>
    public class OmdbProvider
    {
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>Initializes a new instance of the <see cref="OmdbProvider"/> class.</summary>
        /// <param name="httpClientFactory">HttpClientFactory to use for calls to OMDB service.</param>
        /// <param name="fileSystem">IFileSystem to use for store OMDB data.</param>
        /// <param name="configurationManager">IServerConfigurationManager to use.</param>
        public OmdbProvider(IHttpClientFactory httpClientFactory, IFileSystem fileSystem, IServerConfigurationManager configurationManager)
        {
            _httpClientFactory = httpClientFactory;
            _fileSystem = fileSystem;
            _configurationManager = configurationManager;

            _jsonOptions = new JsonSerializerOptions(JsonDefaults.Options);
            // These converters need to take priority
            _jsonOptions.Converters.Insert(0, new JsonOmdbNotAvailableStringConverter());
            _jsonOptions.Converters.Insert(0, new JsonOmdbNotAvailableInt32Converter());
        }

        /// <summary>Fetches data from OMDB service.</summary>
        /// <param name="itemResult">Metadata about media item.</param>
        /// <param name="imdbId">IMDB ID for media.</param>
        /// <param name="language">Media language.</param>
        /// <param name="country">Country of origin.</param>
        /// <param name="cancellationToken">CancellationToken to use for operation.</param>
        /// <typeparam name="T">The first generic type parameter.</typeparam>
        /// <returns>Returns a Task object that can be awaited.</returns>
        public async Task Fetch<T>(MetadataResult<T> itemResult, string imdbId, string language, string country, CancellationToken cancellationToken)
            where T : BaseItem
        {
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                throw new ArgumentNullException(nameof(imdbId));
            }

            var item = itemResult.Item;

            var result = await GetRootObject(imdbId, cancellationToken).ConfigureAwait(false);

            var isEnglishRequested = IsConfiguredForEnglish(item, language);
            // Only take the name and rating if the user's language is set to English, since Omdb has no localization
            if (isEnglishRequested)
            {
                item.Name = result.Title;

                if (string.Equals(country, "us", StringComparison.OrdinalIgnoreCase))
                {
                    item.OfficialRating = result.Rated;
                }
            }

            if (TryParseYear(result.Year, out var year))
            {
                item.ProductionYear = year;
            }

            var tomatoScore = result.GetRottenTomatoScore();

            if (tomatoScore.HasValue)
            {
                item.CriticRating = tomatoScore;
            }

            if (!string.IsNullOrEmpty(result.imdbVotes)
                && int.TryParse(result.imdbVotes, NumberStyles.Number, CultureInfo.InvariantCulture, out var voteCount)
                && voteCount >= 0)
            {
                // item.VoteCount = voteCount;
            }

            if (float.TryParse(result.imdbRating, CultureInfo.InvariantCulture, out var imdbRating)
                && imdbRating >= 0)
            {
                item.CommunityRating = imdbRating;
            }

            if (!string.IsNullOrEmpty(result.Website))
            {
                item.HomePageUrl = result.Website;
            }

            if (!string.IsNullOrWhiteSpace(result.imdbID))
            {
                item.SetProviderId(MetadataProvider.Imdb, result.imdbID);
            }

            ParseAdditionalMetadata(itemResult, result, isEnglishRequested);
        }

        /// <summary>Gets data about an episode.</summary>
        /// <param name="itemResult">Metadata about episode.</param>
        /// <param name="episodeNumber">Episode number.</param>
        /// <param name="seasonNumber">Season number.</param>
        /// <param name="episodeImdbId">Episode ID.</param>
        /// <param name="seriesImdbId">Season ID.</param>
        /// <param name="language">Episode language.</param>
        /// <param name="country">Country of origin.</param>
        /// <param name="cancellationToken">CancellationToken to use for operation.</param>
        /// <typeparam name="T">The first generic type parameter.</typeparam>
        /// <returns>Whether operation was successful.</returns>
        public async Task<bool> FetchEpisodeData<T>(MetadataResult<T> itemResult, int episodeNumber, int seasonNumber, string episodeImdbId, string seriesImdbId, string language, string country, CancellationToken cancellationToken)
            where T : BaseItem
        {
            if (string.IsNullOrWhiteSpace(seriesImdbId))
            {
                throw new ArgumentNullException(nameof(seriesImdbId));
            }

            var item = itemResult.Item;

            var seasonResult = await GetSeasonRootObject(seriesImdbId, seasonNumber, cancellationToken).ConfigureAwait(false);

            if (seasonResult?.Episodes is null)
            {
                return false;
            }

            RootObject result = null;

            if (!string.IsNullOrWhiteSpace(episodeImdbId))
            {
                foreach (var episode in seasonResult.Episodes)
                {
                    if (string.Equals(episodeImdbId, episode.imdbID, StringComparison.OrdinalIgnoreCase))
                    {
                        result = episode;
                        break;
                    }
                }
            }

            // finally, search by numbers
            if (result is null)
            {
                foreach (var episode in seasonResult.Episodes)
                {
                    if (episode.Episode == episodeNumber)
                    {
                        result = episode;
                        break;
                    }
                }
            }

            if (result is null)
            {
                return false;
            }

            var isEnglishRequested = IsConfiguredForEnglish(item, language);
            // Only take the name and rating if the user's language is set to English, since Omdb has no localization
            if (isEnglishRequested)
            {
                item.Name = result.Title;

                if (string.Equals(country, "us", StringComparison.OrdinalIgnoreCase))
                {
                    item.OfficialRating = result.Rated;
                }
            }

            if (TryParseYear(result.Year, out var year))
            {
                item.ProductionYear = year;
            }

            var tomatoScore = result.GetRottenTomatoScore();

            if (tomatoScore.HasValue)
            {
                item.CriticRating = tomatoScore;
            }

            if (!string.IsNullOrEmpty(result.imdbVotes)
                && int.TryParse(result.imdbVotes, NumberStyles.Number, CultureInfo.InvariantCulture, out var voteCount)
                && voteCount >= 0)
            {
                // item.VoteCount = voteCount;
            }

            if (float.TryParse(result.imdbRating, CultureInfo.InvariantCulture, out var imdbRating)
                && imdbRating >= 0)
            {
                item.CommunityRating = imdbRating;
            }

            if (!string.IsNullOrEmpty(result.Website))
            {
                item.HomePageUrl = result.Website;
            }

            item.TrySetProviderId(MetadataProvider.Imdb, result.imdbID);

            ParseAdditionalMetadata(itemResult, result, isEnglishRequested);

            return true;
        }

        internal async Task<RootObject> GetRootObject(string imdbId, CancellationToken cancellationToken)
        {
            var path = await EnsureItemInfo(imdbId, cancellationToken).ConfigureAwait(false);
            var stream = AsyncFile.OpenRead(path);
            await using (stream.ConfigureAwait(false))
            {
                return await JsonSerializer.DeserializeAsync<RootObject>(stream, _jsonOptions, cancellationToken).ConfigureAwait(false);
            }
        }

        internal async Task<SeasonRootObject> GetSeasonRootObject(string imdbId, int seasonId, CancellationToken cancellationToken)
        {
            var path = await EnsureSeasonInfo(imdbId, seasonId, cancellationToken).ConfigureAwait(false);
            var stream = AsyncFile.OpenRead(path);
            await using (stream.ConfigureAwait(false))
            {
                return await JsonSerializer.DeserializeAsync<SeasonRootObject>(stream, _jsonOptions, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>Gets OMDB URL.</summary>
        /// <param name="query">Appends query string to URL.</param>
        /// <returns>OMDB URL with optional query string.</returns>
        public static string GetOmdbUrl(string query)
        {
            const string Url = "https://www.omdbapi.com?apikey=2c9d9507";

            if (string.IsNullOrWhiteSpace(query))
            {
                return Url;
            }

            return Url + "&" + query;
        }

        /// <summary>
        /// Extract the year from a string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="year">The year.</param>
        /// <returns>A value indicating whether the input could successfully be parsed as a year.</returns>
        public static bool TryParseYear(string input, [NotNullWhen(true)] out int? year)
        {
            if (string.IsNullOrEmpty(input))
            {
                year = 0;
                return false;
            }

            if (int.TryParse(input.AsSpan(0, 4), NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
            {
                year = result;
                return true;
            }

            year = 0;
            return false;
        }

        private async Task<string> EnsureItemInfo(string imdbId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                throw new ArgumentNullException(nameof(imdbId));
            }

            var imdbParam = imdbId.StartsWith("tt", StringComparison.OrdinalIgnoreCase) ? imdbId : "tt" + imdbId;

            var path = GetDataFilePath(imdbParam);

            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            if (fileInfo.Exists)
            {
                // If it's recent or automatic updates are enabled, don't re-download
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 1)
                {
                    return path;
                }
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            var url = GetOmdbUrl(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "i={0}&plot=short&tomatoes=true&r=json",
                    imdbParam));

            var rootObject = await _httpClientFactory.CreateClient(NamedClient.Default).GetFromJsonAsync<RootObject>(url, _jsonOptions, cancellationToken).ConfigureAwait(false);
            FileStream jsonFileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, IODefaults.FileStreamBufferSize, FileOptions.Asynchronous);
            await using (jsonFileStream.ConfigureAwait(false))
            {
                await JsonSerializer.SerializeAsync(jsonFileStream, rootObject, _jsonOptions, cancellationToken).ConfigureAwait(false);
            }

            return path;
        }

        private async Task<string> EnsureSeasonInfo(string seriesImdbId, int seasonId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(seriesImdbId))
            {
                throw new ArgumentException("The series IMDb ID was null or whitespace.", nameof(seriesImdbId));
            }

            var imdbParam = seriesImdbId.StartsWith("tt", StringComparison.OrdinalIgnoreCase) ? seriesImdbId : "tt" + seriesImdbId;

            var path = GetSeasonFilePath(imdbParam, seasonId);

            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            if (fileInfo.Exists)
            {
                // If it's recent or automatic updates are enabled, don't re-download
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 1)
                {
                    return path;
                }
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            var url = GetOmdbUrl(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "i={0}&season={1}&detail=full",
                    imdbParam,
                    seasonId));

            var rootObject = await _httpClientFactory.CreateClient(NamedClient.Default).GetFromJsonAsync<SeasonRootObject>(url, _jsonOptions, cancellationToken).ConfigureAwait(false);
            FileStream jsonFileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, IODefaults.FileStreamBufferSize, FileOptions.Asynchronous);
            await using (jsonFileStream.ConfigureAwait(false))
            {
                await JsonSerializer.SerializeAsync(jsonFileStream, rootObject, _jsonOptions, cancellationToken).ConfigureAwait(false);
            }

            return path;
        }

        internal string GetDataFilePath(string imdbId)
        {
            ArgumentException.ThrowIfNullOrEmpty(imdbId);

            var dataPath = Path.Combine(_configurationManager.ApplicationPaths.CachePath, "omdb");

            var filename = string.Format(CultureInfo.InvariantCulture, "{0}.json", imdbId);

            return Path.Combine(dataPath, filename);
        }

        internal string GetSeasonFilePath(string imdbId, int seasonId)
        {
            ArgumentException.ThrowIfNullOrEmpty(imdbId);

            var dataPath = Path.Combine(_configurationManager.ApplicationPaths.CachePath, "omdb");

            var filename = string.Format(CultureInfo.InvariantCulture, "{0}_season_{1}.json", imdbId, seasonId);

            return Path.Combine(dataPath, filename);
        }

        private static void ParseAdditionalMetadata<T>(MetadataResult<T> itemResult, RootObject result, bool isEnglishRequested)
            where T : BaseItem
        {
            var item = itemResult.Item;

            // Grab series genres because IMDb data is better than TVDB. Leave movies alone
            // But only do it if English is the preferred language because this data will not be localized
            if (isEnglishRequested && !string.IsNullOrWhiteSpace(result.Genre))
            {
                item.Genres = Array.Empty<string>();

                foreach (var genre in result.Genre.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    item.AddGenre(genre);
                }
            }

            item.Overview = result.Plot;

            if (!Plugin.Instance.Configuration.CastAndCrew)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(result.Director))
            {
                var person = new PersonInfo
                {
                    Name = result.Director,
                    Type = PersonKind.Director
                };

                itemResult.AddPerson(person);
            }

            if (!string.IsNullOrWhiteSpace(result.Writer))
            {
                var person = new PersonInfo
                {
                    Name = result.Writer,
                    Type = PersonKind.Writer
                };

                itemResult.AddPerson(person);
            }

            if (!string.IsNullOrWhiteSpace(result.Actors))
            {
                var actorList = result.Actors.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var actor in actorList)
                {
                    var person = new PersonInfo
                    {
                        Name = actor,
                        Type = PersonKind.Actor
                    };

                    itemResult.AddPerson(person);
                }
            }
        }

        private static bool IsConfiguredForEnglish(BaseItem item, string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                language = item.GetPreferredMetadataLanguage();
            }

            // The data isn't localized and so can only be used for English users
            return string.Equals(language, "en", StringComparison.OrdinalIgnoreCase);
        }

        internal class SeasonRootObject
        {
            public string Title { get; set; }

            public string seriesID { get; set; }

            public int? Season { get; set; }

            public int? totalSeasons { get; set; }

            public RootObject[] Episodes { get; set; }

            public string Response { get; set; }
        }

        internal class RootObject
        {
            public string Title { get; set; }

            public string Year { get; set; }

            public string Rated { get; set; }

            public string Released { get; set; }

            public string Runtime { get; set; }

            public string Genre { get; set; }

            public string Director { get; set; }

            public string Writer { get; set; }

            public string Actors { get; set; }

            public string Plot { get; set; }

            public string Language { get; set; }

            public string Country { get; set; }

            public string Awards { get; set; }

            public string Poster { get; set; }

            public List<OmdbRating> Ratings { get; set; }

            public string Metascore { get; set; }

            public string imdbRating { get; set; }

            public string imdbVotes { get; set; }

            public string imdbID { get; set; }

            public string Type { get; set; }

            public string DVD { get; set; }

            public string BoxOffice { get; set; }

            public string Production { get; set; }

            public string Website { get; set; }

            public string Response { get; set; }

            public int? Episode { get; set; }

            public float? GetRottenTomatoScore()
            {
                if (Ratings is not null)
                {
                    var rating = Ratings.FirstOrDefault(i => string.Equals(i.Source, "Rotten Tomatoes", StringComparison.OrdinalIgnoreCase));
                    if (rating?.Value is not null)
                    {
                        var value = rating.Value.TrimEnd('%');
                        if (float.TryParse(value, CultureInfo.InvariantCulture, out var score))
                        {
                            return score;
                        }
                    }
                }

                return null;
            }
        }

#pragma warning disable CA1034
        /// <summary>Describes OMDB rating.</summary>
        public class OmdbRating
        {
            /// <summary>Gets or sets rating source.</summary>
            public string Source { get; set; }

            /// <summary>Gets or sets rating value.</summary>
            public string Value { get; set; }
        }
    }
}
