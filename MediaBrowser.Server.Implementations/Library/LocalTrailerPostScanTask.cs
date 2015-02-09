using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Library
{
    public class LocalTrailerPostScanTask : ILibraryPostScanTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IChannelManager _channelManager;

        public LocalTrailerPostScanTask(ILibraryManager libraryManager, IChannelManager channelManager)
        {
            _libraryManager = libraryManager;
            _channelManager = channelManager;
        }

        public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var items = _libraryManager.RootFolder
                .GetRecursiveChildren(i => i is IHasTrailers)
                .Cast<IHasTrailers>()
                .ToList();

            var channelTrailerResult = await _channelManager.GetAllMediaInternal(new AllChannelMediaQuery
            {
                ExtraTypes = new[] { ExtraType.Trailer }

            }, CancellationToken.None);
            var channelTrailers = channelTrailerResult.Items;

            var numComplete = 0;

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await AssignTrailers(item, channelTrailers).ConfigureAwait(false);

                numComplete++;
                double percent = numComplete;
                percent /= items.Count;
                progress.Report(percent * 100);
            }

            progress.Report(100);
        }

        private async Task AssignTrailers(IHasTrailers item, BaseItem[] channelTrailers)
        {
            if (item is Game)
            {
                return;
            }

            var imdbId = item.GetProviderId(MetadataProviders.Imdb);
            var tmdbId = item.GetProviderId(MetadataProviders.Tmdb);

            var trailers = channelTrailers.Where(i =>
            {
                if (!string.IsNullOrWhiteSpace(imdbId) &&
                    string.Equals(imdbId, i.GetProviderId(MetadataProviders.Imdb), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (!string.IsNullOrWhiteSpace(tmdbId) &&
                    string.Equals(tmdbId, i.GetProviderId(MetadataProviders.Tmdb), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                return false;
            });

            var trailerIds = trailers.Select(i => i.Id)
                .ToList();

            if (!trailerIds.SequenceEqual(item.RemoteTrailerIds))
            {
                item.RemoteTrailerIds = trailerIds;

                var baseItem = (BaseItem)item;
                await baseItem.UpdateToRepository(ItemUpdateType.MetadataImport, CancellationToken.None)
                        .ConfigureAwait(false);
            }
        }
    }
}
