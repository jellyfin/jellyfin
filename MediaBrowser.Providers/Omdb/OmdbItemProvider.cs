using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
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
        IRemoteMetadataProvider<Movie, MovieInfo>, IRemoteMetadataProvider<ChannelVideoItem, ChannelItemLookupInfo>, IRemoteMetadataProvider<LiveTvProgram, LiveTvProgramLookupInfo>
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;

        public OmdbItemProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogger logger, ILibraryManager libraryManager)
        {
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
            _logger = logger;
            _libraryManager = libraryManager;
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

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ItemLookupInfo searchInfo, string type, CancellationToken cancellationToken)
        {
            bool isSearch = false;

            var list = new List<RemoteSearchResult>();

            var imdbId = searchInfo.GetProviderId(MetadataProviders.Imdb);

            var url = "http://www.omdbapi.com/?plot=short&r=json";

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
                url += "&s=" + WebUtility.UrlEncode(name);
                url += "&type=" + type;
                isSearch = true;
            }
            else
            {
                url += "&i=" + imdbId;
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

                foreach (var result in resultList)
                {
                    var item = new RemoteSearchResult();

                    item.SearchProviderName = Name;
                    item.Name = result.Title;
                    item.SetProviderId(MetadataProviders.Imdb, result.imdbID);

                    int parsedYear;
                    if (result.Year.Length > 0
                        && int.TryParse(result.Year.Substring(0, Math.Min(result.Year.Length, 4)), NumberStyles.Any, CultureInfo.InvariantCulture, out parsedYear))
                    {
                        item.ProductionYear = parsedYear;
                    }

                    if (!string.IsNullOrWhiteSpace(result.Poster) && !string.Equals(result.Poster, "N/A", StringComparison.OrdinalIgnoreCase))
                    {
                        item.ImageUrl = result.Poster;
                    }

                    list.Add(item);
                }
            }

            return list;
        }

        public Task<MetadataResult<ChannelVideoItem>> GetMetadata(ChannelItemLookupInfo info, CancellationToken cancellationToken)
        {
            if (info.ContentType != ChannelMediaContentType.MovieExtra || info.ExtraType != ExtraType.Trailer)
            {
                return Task.FromResult(new MetadataResult<ChannelVideoItem>());
            }

            return GetMovieResult<ChannelVideoItem>(info, cancellationToken);
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ChannelItemLookupInfo searchInfo, CancellationToken cancellationToken)
        {
            if (searchInfo.ContentType != ChannelMediaContentType.MovieExtra || searchInfo.ExtraType != ExtraType.Trailer)
            {
                return Task.FromResult<IEnumerable<RemoteSearchResult>>(new List<RemoteSearchResult>());
            }

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
                Item = new Series()
            };

            var imdbId = info.GetProviderId(MetadataProviders.Imdb);
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                imdbId = await GetSeriesImdbId(info, cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(imdbId))
            {
                result.Item.SetProviderId(MetadataProviders.Imdb, imdbId);
                result.HasMetadata = true;

                await new OmdbProvider(_jsonSerializer, _httpClient).Fetch(result.Item, imdbId, info.MetadataLanguage, info.MetadataCountryCode, cancellationToken).ConfigureAwait(false);
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
                Item = new T()
            };

            var imdbId = info.GetProviderId(MetadataProviders.Imdb);
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                imdbId = await GetMovieImdbId(info, cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(imdbId))
            {
                result.Item.SetProviderId(MetadataProviders.Imdb, imdbId);
                result.HasMetadata = true;

                await new OmdbProvider(_jsonSerializer, _httpClient).Fetch(result.Item, imdbId, info.MetadataLanguage, info.MetadataCountryCode, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }

        private async Task<string> GetMovieImdbId(ItemLookupInfo info, CancellationToken cancellationToken)
        {
            var results = await GetSearchResults(info, "movie", cancellationToken).ConfigureAwait(false);
            var first = results.FirstOrDefault();
            return first == null ? null : first.GetProviderId(MetadataProviders.Imdb);
        }

        private async Task<string> GetSeriesImdbId(SeriesInfo info, CancellationToken cancellationToken)
        {
            var results = await GetSearchResults(info, cancellationToken).ConfigureAwait(false);
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
