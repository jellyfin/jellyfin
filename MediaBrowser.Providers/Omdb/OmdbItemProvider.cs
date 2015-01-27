using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Movies;
using MediaBrowser.Providers.TV;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Omdb
{
    public class OmdbItemProvider : IRemoteMetadataProvider<Series, SeriesInfo>,
        IRemoteMetadataProvider<Movie, MovieInfo>, IRemoteMetadataProvider<ChannelVideoItem, ChannelItemLookupInfo>
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

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ItemLookupInfo searchInfo, string type, CancellationToken cancellationToken)
        {
            var list = new List<RemoteSearchResult>();

            var imdbId = searchInfo.GetProviderId(MetadataProviders.Imdb);
            if (!string.IsNullOrWhiteSpace(imdbId))
            {
                return list;
            }

            var url = "http://www.omdbapi.com/?plot=short&r=json";

            var name = searchInfo.Name;
            var year = searchInfo.Year;

            if (year.HasValue)
            {
                url += "&y=" + year.Value.ToString(CultureInfo.InvariantCulture);
            }

            url += "&t=" + WebUtility.UrlEncode(name);
            url += "&type=" + type;

            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = OmdbProvider.ResourcePool,
                CancellationToken = cancellationToken,
                CacheMode = CacheMode.Unconditional,
                CacheLength = TimeSpan.FromDays(7)

            }).ConfigureAwait(false))
            {
                var result = _jsonSerializer.DeserializeFromStream<SearchResult>(stream);

                if (string.Equals(result.Response, "true", StringComparison.OrdinalIgnoreCase))
                {
                    var item = new RemoteSearchResult();

                    item.SearchProviderName = Name;
                    item.Name = result.Title;
                    item.SetProviderId(MetadataProviders.Imdb, result.imdbID);

                    int parsedYear;
                    if (int.TryParse(result.Year, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedYear))
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

            var searchResult = await GetSeriesImdbId(info, cancellationToken).ConfigureAwait(false);
            result.Item.Name = searchResult.Item4;

            if (!string.IsNullOrEmpty(searchResult.Item1))
            {
                result.Item.SetProviderId(MetadataProviders.Imdb, searchResult.Item1);
            }

            if (!string.IsNullOrEmpty(searchResult.Item2))
            {
                result.Item.SetProviderId(MetadataProviders.Tmdb, searchResult.Item2);
            }

            if (!string.IsNullOrEmpty(searchResult.Item3))
            {
                result.Item.SetProviderId(MetadataProviders.Tvdb, searchResult.Item3);
            }

            var imdbId = result.Item.GetProviderId(MetadataProviders.Imdb);

            if (!string.IsNullOrEmpty(info.GetProviderId(MetadataProviders.Imdb)))
            {
                result.Item.SetProviderId(MetadataProviders.Imdb, imdbId);
                result.HasMetadata = true;

                await new OmdbProvider(_jsonSerializer, _httpClient).Fetch(result.Item, imdbId, cancellationToken)
                        .ConfigureAwait(false);
            }

            return result;
        }

        public Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            return GetMovieResult<Movie>(info, cancellationToken);
        }

        private async Task<MetadataResult<T>> GetMovieResult<T>(ItemLookupInfo info, CancellationToken cancellationToken)
            where T : Video, new()
        {
            var result = new MetadataResult<T>
            {
                Item = new T()
            };

            var imdbId = info.GetProviderId(MetadataProviders.Imdb);

            var searchResult = await GetMovieImdbId(info, cancellationToken).ConfigureAwait(false);
            result.Item.Name = searchResult.Item3;

            if (string.IsNullOrEmpty(imdbId))
            {
                imdbId = searchResult.Item1;

                if (!string.IsNullOrEmpty(searchResult.Item2))
                {
                    result.Item.SetProviderId(MetadataProviders.Tmdb, searchResult.Item2);
                }
            }

            if (!string.IsNullOrEmpty(imdbId))
            {
                result.Item.SetProviderId(MetadataProviders.Imdb, imdbId);
                result.HasMetadata = true;

                await new OmdbProvider(_jsonSerializer, _httpClient).Fetch(result.Item, imdbId, cancellationToken)
                        .ConfigureAwait(false);
            }

            return result;
        }

        private async Task<Tuple<string, string, string>> GetMovieImdbId(ItemLookupInfo info, CancellationToken cancellationToken)
        {
            var result = await new GenericMovieDbInfo<Movie>(_logger, _jsonSerializer, _libraryManager).GetMetadata(info, cancellationToken)
                        .ConfigureAwait(false);

            var imdb = result.HasMetadata ? result.Item.GetProviderId(MetadataProviders.Imdb) : null;
            var tmdb = result.HasMetadata ? result.Item.GetProviderId(MetadataProviders.Tmdb) : null;
            var name = result.HasMetadata ? result.Item.Name : null;

            return new Tuple<string, string, string>(imdb, tmdb, name);
        }

        private async Task<Tuple<string, string, string, string>> GetSeriesImdbId(SeriesInfo info, CancellationToken cancellationToken)
        {
            //var result = await TvdbSeriesProvider.Current.GetMetadata(info, cancellationToken)
            //       .ConfigureAwait(false);

            //var imdb = result.HasMetadata ? result.Item.GetProviderId(MetadataProviders.Imdb) : null;
            //var tvdb = result.HasMetadata ? result.Item.GetProviderId(MetadataProviders.Tvdb) : null;
            //var name = result.HasMetadata ? result.Item.Name : null;

            //return new Tuple<string, string, string>(imdb, tvdb, name);

            var result = await MovieDbSeriesProvider.Current.GetMetadata(info, cancellationToken)
                        .ConfigureAwait(false);

            var imdb = result.HasMetadata ? result.Item.GetProviderId(MetadataProviders.Imdb) : null;
            var tmdb = result.HasMetadata ? result.Item.GetProviderId(MetadataProviders.Tmdb) : null;
            var tvdb = result.HasMetadata ? result.Item.GetProviderId(MetadataProviders.Tvdb) : null;
            var name = result.HasMetadata ? result.Item.Name : null;

            return new Tuple<string, string, string, string>(imdb, tmdb, tvdb, name);
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
    }
}
