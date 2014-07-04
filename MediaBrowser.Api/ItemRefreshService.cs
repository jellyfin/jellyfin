using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Providers;
using ServiceStack;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    public class BaseRefreshRequest : IReturnVoid
    {
        [ApiMember(Name = "MetadataRefreshMode", Description = "Specifies the metadata refresh mode", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "POST")]
        public MetadataRefreshMode MetadataRefreshMode { get; set; }

        [ApiMember(Name = "ImageRefreshMode", Description = "Specifies the image refresh mode", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "POST")]
        public ImageRefreshMode ImageRefreshMode { get; set; }

        [ApiMember(Name = "ReplaceAllMetadata", Description = "Determines if metadata should be replaced. Only applicable if mode is FullRefresh", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "POST")]
        public bool ReplaceAllMetadata { get; set; }

        [ApiMember(Name = "ReplaceAllImages", Description = "Determines if images should be replaced. Only applicable if mode is FullRefresh", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "POST")]
        public bool ReplaceAllImages { get; set; }
    }

    [Route("/Items/{Id}/Refresh", "POST")]
    [Api(Description = "Refreshes metadata for an item")]
    public class RefreshItem : BaseRefreshRequest
    {
        [ApiMember(Name = "Recursive", Description = "Indicates if the refresh should occur recursively.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool Recursive { get; set; }

        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }
    }

    [Authenticated]
    public class ItemRefreshService : BaseApiService
    {
        private readonly ILibraryManager _libraryManager;

        public ItemRefreshService(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        private async Task RefreshArtist(RefreshItem request, MusicArtist item)
        {
            var cancellationToken = CancellationToken.None;

            var albums = _libraryManager.RootFolder
                                        .RecursiveChildren
                                        .OfType<MusicAlbum>()
                                        .Where(i => i.HasArtist(item.Name))
                                        .ToList();

            var musicArtists = albums
                .Select(i => i.Parent)
                .OfType<MusicArtist>()
                .ToList();

            var options = GetRefreshOptions(request);

            var musicArtistRefreshTasks = musicArtists.Select(i => i.ValidateChildren(new Progress<double>(), cancellationToken, options, true));

            await Task.WhenAll(musicArtistRefreshTasks).ConfigureAwait(false);

            try
            {
                await item.RefreshMetadata(options, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error refreshing library", ex);
            }
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(RefreshItem request)
        {
            var item = _libraryManager.GetItemById(request.Id);

            var task = item is MusicArtist ? RefreshArtist(request, (MusicArtist)item) : RefreshItem(request, item);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Refreshes the item.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task.</returns>
        private async Task RefreshItem(RefreshItem request, BaseItem item)
        {
            var options = GetRefreshOptions(request);

            try
            {
                await item.RefreshMetadata(options, CancellationToken.None).ConfigureAwait(false);

                if (item.IsFolder)
                {
                    // Collection folders don't validate their children so we'll have to simulate that here
                    var collectionFolder = item as CollectionFolder;

                    if (collectionFolder != null)
                    {
                        await RefreshCollectionFolderChildren(request, collectionFolder).ConfigureAwait(false);
                    }
                    else
                    {
                        var folder = (Folder)item;

                        await folder.ValidateChildren(new Progress<double>(), CancellationToken.None, options, request.Recursive).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error refreshing library", ex);
            }
        }

        /// <summary>
        /// Refreshes the collection folder children.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="collectionFolder">The collection folder.</param>
        /// <returns>Task.</returns>
        private async Task RefreshCollectionFolderChildren(RefreshItem request, CollectionFolder collectionFolder)
        {
            var options = GetRefreshOptions(request);

            foreach (var child in collectionFolder.Children.ToList())
            {
                await child.RefreshMetadata(options, CancellationToken.None).ConfigureAwait(false);

                if (child.IsFolder)
                {
                    var folder = (Folder)child;

                    await folder.ValidateChildren(new Progress<double>(), CancellationToken.None, options, request.Recursive).ConfigureAwait(false);
                }
            }
        }

        private MetadataRefreshOptions GetRefreshOptions(BaseRefreshRequest request)
        {
            return new MetadataRefreshOptions
            {
                MetadataRefreshMode = request.MetadataRefreshMode,
                ImageRefreshMode = request.ImageRefreshMode,
                ReplaceAllImages = request.ReplaceAllImages,
                ReplaceAllMetadata = request.ReplaceAllMetadata
            };
        }
    }
}
