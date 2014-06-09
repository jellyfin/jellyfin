using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Chapters;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Chapters
{
    public class ChapterManager : IChapterManager
    {
        private IChapterProvider[] _providers;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;

        public ChapterManager(ILibraryManager libraryManager, ILogger logger, IServerConfigurationManager config)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _config = config;
        }

        public void AddParts(IEnumerable<IChapterProvider> chapterProviders)
        {
            _providers = chapterProviders.ToArray();
        }

        public Task<IEnumerable<RemoteChapterResult>> Search(Video video, CancellationToken cancellationToken)
        {
            VideoContentType mediaType;

            if (video is Episode)
            {
                mediaType = VideoContentType.Episode;
            }
            else if (video is Movie)
            {
                mediaType = VideoContentType.Movie;
            }
            else
            {
                // These are the only supported types
                return Task.FromResult<IEnumerable<RemoteChapterResult>>(new List<RemoteChapterResult>());
            }

            var request = new ChapterSearchRequest
            {
                ContentType = mediaType,
                IndexNumber = video.IndexNumber,
                //Language = language,
                MediaPath = video.Path,
                Name = video.Name,
                ParentIndexNumber = video.ParentIndexNumber,
                ProductionYear = video.ProductionYear,
                ProviderIds = video.ProviderIds,
                RuntimeTicks = video.RunTimeTicks
            };

            var episode = video as Episode;

            if (episode != null)
            {
                request.IndexNumberEnd = episode.IndexNumberEnd;
                request.SeriesName = episode.SeriesName;
            }

            return Search(request, cancellationToken);
        }

        public async Task<IEnumerable<RemoteChapterResult>> Search(ChapterSearchRequest request, CancellationToken cancellationToken)
        {
            var contentType = request.ContentType;
            var providers = GetInternalProviders(false)
                .Where(i => i.SupportedMediaTypes.Contains(contentType))
                .ToList();

            // If not searching all, search one at a time until something is found
            if (!request.SearchAllProviders)
            {
                foreach (var provider in providers)
                {
                    try
                    {
                        return await Search(request, provider, cancellationToken).ConfigureAwait(false);

                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error downloading subtitles from {0}", ex, provider.Name);
                    }
                }
                return new List<RemoteChapterResult>();
            }

            var tasks = providers.Select(async i =>
            {
                try
                {
                    return await Search(request, i, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error downloading subtitles from {0}", ex, i.Name);
                    return new List<RemoteChapterResult>();
                }
            });

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            return results.SelectMany(i => i);
        }

        private async Task<IEnumerable<RemoteChapterResult>> Search(ChapterSearchRequest request,
            IChapterProvider provider,
            CancellationToken cancellationToken)
        {
            var searchResults = await provider.Search(request, cancellationToken).ConfigureAwait(false);

            foreach (var result in searchResults)
            {
                result.Id = GetProviderId(provider.Name) + "_" + result.Id;
                result.ProviderName = provider.Name;
            }

            return searchResults;
        }

        public Task<ChapterResponse> GetChapters(string id, CancellationToken cancellationToken)
        {
            var parts = id.Split(new[] { '_' }, 2);

            var provider = GetProvider(parts.First());
            id = parts.Last();

            return provider.GetChapters(id, cancellationToken);
        }

        public IEnumerable<ChapterProviderInfo> GetProviders(string itemId)
        {
            var video = _libraryManager.GetItemById(itemId) as Video;
            VideoContentType mediaType;

            if (video is Episode)
            {
                mediaType = VideoContentType.Episode;
            }
            else if (video is Movie)
            {
                mediaType = VideoContentType.Movie;
            }
            else
            {
                // These are the only supported types
                return new List<ChapterProviderInfo>();
            }

            var providers = GetInternalProviders(false)
                .Where(i => i.SupportedMediaTypes.Contains(mediaType));

            return GetInfos(providers);
        }

        public IEnumerable<ChapterProviderInfo> GetProviders()
        {
            return GetInfos(GetInternalProviders(true));
        }

        private IEnumerable<IChapterProvider> GetInternalProviders(bool includeDisabledProviders)
        {
            var providers = _providers;

            if (!includeDisabledProviders)
            {
                providers = providers
                    .Where(i => _config.Configuration.ChapterOptions.DisabledFetchers.Contains(i.Name))
                    .ToArray();
            }

            return providers
                .OrderBy(GetConfiguredOrder)
                .ThenBy(GetDefaultOrder)
                .ToArray();
        }

        private IEnumerable<ChapterProviderInfo> GetInfos(IEnumerable<IChapterProvider> providers)
        {
            return providers.Select(i => new ChapterProviderInfo
            {
                Name = i.Name,
                Id = GetProviderId(i.Name)
            });
        }

        private string GetProviderId(string name)
        {
            return name.ToLower().GetMD5().ToString("N");
        }

        private IChapterProvider GetProvider(string id)
        {
            return _providers.First(i => string.Equals(id, GetProviderId(i.Name)));
        }

        private int GetConfiguredOrder(IChapterProvider provider)
        {
            // See if there's a user-defined order
            var index = Array.IndexOf(_config.Configuration.ChapterOptions.FetcherOrder, provider.Name);

            if (index != -1)
            {
                return index;
            }

            // Not configured. Just return some high number to put it at the end.
            return 100;
        }

        private int GetDefaultOrder(IChapterProvider provider)
        {
            var hasOrder = provider as IHasOrder;

            if (hasOrder != null)
            {
                return hasOrder.Order;
            }

            return 0;
        }
    }
}
