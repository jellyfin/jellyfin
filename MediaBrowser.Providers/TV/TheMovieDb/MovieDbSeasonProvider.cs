using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Movies;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Providers.TV
{
    public class MovieDbSeasonProvider : IRemoteMetadataProvider<Season, SeasonInfo>
    {
        private const string GetTvInfo3 = @"https://api.themoviedb.org/3/tv/{0}/season/{1}?api_key={2}&append_to_response=images,keywords,external_ids,credits,videos";
        private readonly IHttpClient _httpClient;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly ILocalizationManager _localization;
        private readonly ILogger _logger;

        public MovieDbSeasonProvider(IHttpClient httpClient, IServerConfigurationManager configurationManager, IFileSystem fileSystem, ILocalizationManager localization, IJsonSerializer jsonSerializer, ILogManager logManager)
        {
            _httpClient = httpClient;
            _configurationManager = configurationManager;
            _fileSystem = fileSystem;
            _localization = localization;
            _jsonSerializer = jsonSerializer;
            _logger = logManager.GetLogger(GetType().Name);
        }

        public async Task<MetadataResult<Season>> GetMetadata(SeasonInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Season>();

            string seriesTmdbId;
            info.SeriesProviderIds.TryGetValue(MetadataProviders.Tmdb.ToString(), out seriesTmdbId);

            var seasonNumber = info.IndexNumber;

            if (!string.IsNullOrWhiteSpace(seriesTmdbId) && seasonNumber.HasValue)
            {
                try
                {
                    var seasonInfo = await GetSeasonInfo(seriesTmdbId, seasonNumber.Value, info.MetadataLanguage, cancellationToken)
                      .ConfigureAwait(false);

                    result.HasMetadata = true;
                    result.Item = new Season();
                    result.Item.Name = seasonInfo.name;
                    result.Item.IndexNumber = seasonNumber;

                    result.Item.Overview = seasonInfo.overview;

                    if (seasonInfo.external_ids.tvdb_id > 0)
                    {
                        result.Item.SetProviderId(MetadataProviders.Tvdb, seasonInfo.external_ids.tvdb_id.ToString(CultureInfo.InvariantCulture));
                    }

                    var credits = seasonInfo.credits;
                    if (credits != null)
                    {
                        //Actors, Directors, Writers - all in People
                        //actors come from cast
                        if (credits.cast != null)
                        {
                            //foreach (var actor in credits.cast.OrderBy(a => a.order)) result.Item.AddPerson(new PersonInfo { Name = actor.name.Trim(), Role = actor.character, Type = PersonType.Actor, SortOrder = actor.order });
                        }

                        //and the rest from crew
                        if (credits.crew != null)
                        {
                            //foreach (var person in credits.crew) result.Item.AddPerson(new PersonInfo { Name = person.name.Trim(), Role = person.job, Type = person.department });
                        }
                    }

                    result.Item.PremiereDate = seasonInfo.air_date;
                    result.Item.ProductionYear = result.Item.PremiereDate.Value.Year;
                }
                catch (HttpException ex)
                {
                    _logger.Error("No metadata found for {0}", seasonNumber.Value);

                    if (ex.StatusCode.HasValue && ex.StatusCode.Value == HttpStatusCode.NotFound)
                    {
                        return result;
                    }

                    throw;
                }
            }

            return result;
        }

        public string Name
        {
            get { return "TheMovieDb"; }
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeasonInfo searchInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<RemoteSearchResult>>(new List<RemoteSearchResult>());
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                ResourcePool = MovieDbProvider.Current.MovieDbResourcePool
            });
        }

        private async Task<RootObject> GetSeasonInfo(string seriesTmdbId, int season, string preferredMetadataLanguage,
            CancellationToken cancellationToken)
        {
            await EnsureSeasonInfo(seriesTmdbId, season, preferredMetadataLanguage, cancellationToken)
                    .ConfigureAwait(false);

            var dataFilePath = GetDataFilePath(seriesTmdbId, season, preferredMetadataLanguage);

            return _jsonSerializer.DeserializeFromFile<RootObject>(dataFilePath);
        }

        private readonly Task _cachedTask = Task.FromResult(true);
        internal Task EnsureSeasonInfo(string tmdbId, int seasonNumber, string language, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(tmdbId))
            {
                throw new ArgumentNullException("tmdbId");
            }
            if (string.IsNullOrEmpty(language))
            {
                throw new ArgumentNullException("language");
            }

            var path = GetDataFilePath(tmdbId, seasonNumber, language);

            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            if (fileInfo.Exists)
            {
                // If it's recent or automatic updates are enabled, don't re-download
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 3)
                {
                    return _cachedTask;
                }
            }

            return DownloadSeasonInfo(tmdbId, seasonNumber, language, cancellationToken);
        }

        internal string GetDataFilePath(string tmdbId, int seasonNumber, string preferredLanguage)
        {
            if (string.IsNullOrEmpty(tmdbId))
            {
                throw new ArgumentNullException("tmdbId");
            }
            if (string.IsNullOrEmpty(preferredLanguage))
            {
                throw new ArgumentNullException("preferredLanguage");
            }

            var path = MovieDbSeriesProvider.GetSeriesDataPath(_configurationManager.ApplicationPaths, tmdbId);

            var filename = string.Format("season-{0}-{1}.json",
                seasonNumber.ToString(CultureInfo.InvariantCulture),
                preferredLanguage);

            return Path.Combine(path, filename);
        }

        internal async Task DownloadSeasonInfo(string id, int seasonNumber, string preferredMetadataLanguage, CancellationToken cancellationToken)
        {
            var mainResult = await FetchMainResult(id, seasonNumber, preferredMetadataLanguage, cancellationToken).ConfigureAwait(false);

            var dataFilePath = GetDataFilePath(id, seasonNumber, preferredMetadataLanguage);

			_fileSystem.CreateDirectory(Path.GetDirectoryName(dataFilePath));
            _jsonSerializer.SerializeToFile(mainResult, dataFilePath);
        }

        internal async Task<RootObject> FetchMainResult(string id, int seasonNumber, string language, CancellationToken cancellationToken)
        {
            var url = string.Format(GetTvInfo3, id, seasonNumber.ToString(CultureInfo.InvariantCulture), MovieDbProvider.ApiKey);

            if (!string.IsNullOrEmpty(language))
            {
                url += string.Format("&language={0}", MovieDbProvider.NormalizeLanguage(language));
            }

            var includeImageLanguageParam = MovieDbProvider.GetImageLanguagesParam(language);
            // Get images in english and with no language
            url += "&include_image_language=" + includeImageLanguageParam;

            cancellationToken.ThrowIfCancellationRequested();

            using (var json = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                AcceptHeader = MovieDbProvider.AcceptHeader

            }).ConfigureAwait(false))
            {
                return _jsonSerializer.DeserializeFromStream<RootObject>(json);
            }
        }

        public class Episode
        {
            public string air_date { get; set; }
            public int episode_number { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public string overview { get; set; }
            public string still_path { get; set; }
            public double vote_average { get; set; }
            public int vote_count { get; set; }
        }

        public class Cast
        {
            public string character { get; set; }
            public string credit_id { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public string profile_path { get; set; }
            public int order { get; set; }
        }

        public class Crew
        {
            public string credit_id { get; set; }
            public string department { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public string job { get; set; }
            public string profile_path { get; set; }
        }

        public class Credits
        {
            public List<Cast> cast { get; set; }
            public List<Crew> crew { get; set; }
        }

        public class Poster
        {
            public double aspect_ratio { get; set; }
            public string file_path { get; set; }
            public int height { get; set; }
            public string id { get; set; }
            public string iso_639_1 { get; set; }
            public double vote_average { get; set; }
            public int vote_count { get; set; }
            public int width { get; set; }
        }

        public class Images
        {
            public List<Poster> posters { get; set; }
        }

        public class ExternalIds
        {
            public string freebase_id { get; set; }
            public string freebase_mid { get; set; }
            public int tvdb_id { get; set; }
            public object tvrage_id { get; set; }
        }

        public class Videos
        {
            public List<object> results { get; set; }
        }

        public class RootObject
        {
            public DateTime air_date { get; set; }
            public List<Episode> episodes { get; set; }
            public string name { get; set; }
            public string overview { get; set; }
            public int id { get; set; }
            public string poster_path { get; set; }
            public int season_number { get; set; }
            public Credits credits { get; set; }
            public Images images { get; set; }
            public ExternalIds external_ids { get; set; }
            public Videos videos { get; set; }
        }
    }
}
