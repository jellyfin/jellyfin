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

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            return new List<RemoteSearchResult>();
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(TrailerInfo searchInfo, CancellationToken cancellationToken)
        {
            return new List<RemoteSearchResult>();
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            return new List<RemoteSearchResult>();
        }

        public Task<MetadataResult<ChannelVideoItem>> GetMetadata(ChannelItemLookupInfo info, CancellationToken cancellationToken)
        {
            if (info.ContentType != ChannelMediaContentType.MovieExtra || info.ExtraType != ExtraType.Trailer)
            {
                return Task.FromResult(new MetadataResult<ChannelVideoItem>());
            }

            return GetMovieResult<ChannelVideoItem>(info, cancellationToken);
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ChannelItemLookupInfo searchInfo, CancellationToken cancellationToken)
        {
            return new List<RemoteSearchResult>();
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
            throw new NotImplementedException();
        }
    }
}
