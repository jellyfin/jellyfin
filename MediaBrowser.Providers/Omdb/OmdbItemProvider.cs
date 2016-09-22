using CommonIO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Omdb
{
    public class OmdbItemProvider : IRemoteMetadataProvider<Series, SeriesInfo>,
        IRemoteMetadataProvider<Movie, MovieInfo>, IRemoteMetadataProvider<Trailer, TrailerInfo>, IRemoteMetadataProvider<LiveTvProgram, LiveTvProgramLookupInfo>
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _configurationManager;

        public OmdbItemProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogger logger, ILibraryManager libraryManager, IFileSystem fileSystem, IServerConfigurationManager configurationManager)
        {
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
            _logger = logger;
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
            _configurationManager = configurationManager;
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            return GetSearchResults(searchInfo, "series", cancellationToken);
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            return GetSearchResults(searchInfo, "movie", cancellationToken);
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(LiveTvProgramLookupInfo searchInfo, CancellationToken cancellationToken)
        {
            if (!searchInfo.IsMovie)
            {
                return Task.FromResult<IEnumerable<RemoteSearchResult>>(new List<RemoteSearchResult>());
            }

            return GetSearchResults(searchInfo, "movie", cancellationToken);
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ItemLookupInfo searchInfo, string type, CancellationToken cancellationToken)
        {
            return GetSearchResultsInternal(searchInfo, type, true, cancellationToken);
        }

        private async Task<IEnumerable<RemoteSearchResult>> GetSearchResultsInternal(ItemLookupInfo searchInfo, string type, bool isSearch, CancellationToken cancellationToken)
        {
            var episodeSearchInfo = searchInfo as EpisodeInfo;

            var imdbId = searchInfo.GetProviderId(MetadataProviders.Imdb);

            var url = "https://www.omdbapi.com/?plot=full&r=json";
            if (type == "episode" && episodeSearchInfo != null)
            {
                episodeSearchInfo.SeriesProviderIds.TryGetValue(MetadataProviders.Imdb.ToString(), out imdbId);
            }

            var name = searchInfo.Name;
            var year = searchInfo.Year;

            if (!string.IsNullOrWhiteSpace(name))
            {
                var parsedName = _libraryManager.ParseName(name);
                var yearInName = parsedName.Year;
                name = parsedName.Name;
                year = year ?? yearInName;
            }

            if (string.IsNullOrWhiteSpace(imdbId))
            {
                if (year.HasValue)
                {
                    url += "&y=" + year.Value.ToString(CultureInfo.InvariantCulture);
                }

                // &s means search and returns a list of results as opposed to t
                if (isSearch)
                {
                    url += "&s=" + WebUtility.UrlEncode(name);
                }
                else
                {
                    url += "&t=" + WebUtility.UrlEncode(name);
                }
                url += "&type=" + type;
            }
            else
            {
                url += "&i=" + imdbId;
                isSearch = false;
            }

            if (type == "episode")
            {
                if (searchInfo.IndexNumber.HasValue)
                {
                    url += string.Format(CultureInfo.InvariantCulture, "&Episode={0}", searchInfo.IndexNumber);
                }
                if (searchInfo.ParentIndexNumber.HasValue)
                {
                    url += string.Format(CultureInfo.InvariantCulture, "&Season={0}", searchInfo.ParentIndexNumber);
                }
            }

            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = OmdbProvider.ResourcePool,
                CancellationToken = cancellationToken

            }).ConfigureAwait(false))
            {
                var resultList = new List<SearchResult>();

                if (isSearch)
                {
                    var searchResultList = _jsonSerializer.DeserializeFromStream<SearchResultList>(stream);
                    if (searchResultList != null && searchResultList.Search != null)
                    {
                        resultList.AddRange(searchResultList.Search);
                    }
                }
                else
                {
                    var result = _jsonSerializer.DeserializeFromStream<SearchResult>(stream);
                    if (string.Equals(result.Response, "true", StringComparison.OrdinalIgnoreCase))
                    {
                        resultList.Add(result);
                    }
                }

                return resultList.Select(result =>
                {
                    var item = new RemoteSearchResult
                    {
                        IndexNumber = searchInfo.IndexNumber,
                        Name = result.Title,
                        ParentIndexNumber = searchInfo.ParentIndexNumber,
                        SearchProviderName = Name
                    };

                    if (episodeSearchInfo != null && episodeSearchInfo.IndexNumberEnd.HasValue)
                    {
                        item.IndexNumberEnd = episodeSearchInfo.IndexNumberEnd.Value;
                    }

                    item.SetProviderId(MetadataProviders.Imdb, result.imdbID);

                    int parsedYear;
                    if (result.Year.Length > 0
                        && int.TryParse(result.Year.Substring(0, Math.Min(result.Year.Length, 4)), NumberStyles.Any, CultureInfo.InvariantCulture, out parsedYear))
                    {
                        item.ProductionYear = parsedYear;
                    }

                    DateTime released;
                    if (!string.IsNullOrEmpty(result.Released)
                        && DateTime.TryParse(result.Released, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out released))
                    {
                        item.PremiereDate = released;
                    }

                    if (!string.IsNullOrWhiteSpace(result.Poster) && !string.Equals(result.Poster, "N/A", StringComparison.OrdinalIgnoreCase))
                    {
                        item.ImageUrl = result.Poster;
                    }

                    return item;
                });
            }
        }

        public Task<MetadataResult<Trailer>> GetMetadata(TrailerInfo info, CancellationToken cancellationToken)
        {
            return GetMovieResult<Trailer>(info, cancellationToken);
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(TrailerInfo searchInfo, CancellationToken cancellationToken)
        {
            return GetSearchResults(searchInfo, "movie", cancellationToken);
        }

        public string Name
        {
            get { return "The Open Movie Database"; }
        }

        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Series>
            {
                Item = new Series(),
                QueriedById = true
            };

            var imdbId = info.GetProviderId(MetadataProviders.Imdb);
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                imdbId = await GetSeriesImdbId(info, cancellationToken).ConfigureAwait(false);
                result.QueriedById = false;
            }

            if (!string.IsNullOrEmpty(imdbId))
            {
                result.Item.SetProviderId(MetadataProviders.Imdb, imdbId);
                result.HasMetadata = true;

                await new OmdbProvider(_jsonSerializer, _httpClient, _fileSystem, _configurationManager).Fetch(result, imdbId, info.MetadataLanguage, info.MetadataCountryCode, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }

        public Task<MetadataResult<LiveTvProgram>> GetMetadata(LiveTvProgramLookupInfo info, CancellationToken cancellationToken)
        {
            if (!info.IsMovie)
            {
                return Task.FromResult(new MetadataResult<LiveTvProgram>());
            }
            return GetMovieResult<LiveTvProgram>(info, cancellationToken);
        }

        public Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            return GetMovieResult<Movie>(info, cancellationToken);
        }

        private async Task<MetadataResult<T>> GetMovieResult<T>(ItemLookupInfo info, CancellationToken cancellationToken)
            where T : BaseItem, new()
        {
            var result = new MetadataResult<T>
            {
                Item = new T(),
                QueriedById = true
            };

            var imdbId = info.GetProviderId(MetadataProviders.Imdb);
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                imdbId = await GetMovieImdbId(info, cancellationToken).ConfigureAwait(false);
                result.QueriedById = false;
            }

            if (!string.IsNullOrEmpty(imdbId))
            {
                result.Item.SetProviderId(MetadataProviders.Imdb, imdbId);
                result.HasMetadata = true;

                await new OmdbProvider(_jsonSerializer, _httpClient, _fileSystem, _configurationManager).Fetch(result, imdbId, info.MetadataLanguage, info.MetadataCountryCode, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }

        private async Task<string> GetMovieImdbId(ItemLookupInfo info, CancellationToken cancellationToken)
        {
            var results = await GetSearchResultsInternal(info, "movie", false, cancellationToken).ConfigureAwait(false);
            var first = results.FirstOrDefault();
            return first == null ? null : first.GetProviderId(MetadataProviders.Imdb);
        }

        private async Task<string> GetSeriesImdbId(SeriesInfo info, CancellationToken cancellationToken)
        {
            var results = await GetSearchResultsInternal(info, "series", false, cancellationToken).ConfigureAwait(false);
            var first = results.FirstOrDefault();
            return first == null ? null : first.GetProviderId(MetadataProviders.Imdb);
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                ResourcePool = OmdbProvider.ResourcePool
            });
        }

        class SearchResult
        {
            public string Title { get; set; }
            public string Year { get; set; }
            public string Rated { get; set; }
            public string Released { get; set; }
            public string Season { get; set; }
            public string Episode { get; set; }
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
            public string Metascore { get; set; }
            public string imdbRating { get; set; }
            public string imdbVotes { get; set; }
            public string imdbID { get; set; }
            public string seriesID { get; set; }
            public string Type { get; set; }
            public string Response { get; set; }
        }

        private class SearchResultList
        {
            /// <summary>
            /// Gets or sets the results.
            /// </summary>
            /// <value>The results.</value>
            public List<SearchResult> Search { get; set; }
        }

    }
}
