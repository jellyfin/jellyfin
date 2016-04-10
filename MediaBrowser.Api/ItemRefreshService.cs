using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Providers;
using ServiceStack;
using System.Threading;
using CommonIO;

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

    [Route("/Items/{Id}/Refresh", "POST", Summary = "Refreshes metadata for an item")]
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
        private readonly IProviderManager _providerManager;
        private readonly IFileSystem _fileSystem;

        public ItemRefreshService(ILibraryManager libraryManager, IProviderManager providerManager, IFileSystem fileSystem)
        {
            _libraryManager = libraryManager;
            _providerManager = providerManager;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(RefreshItem request)
        {
            var item = _libraryManager.GetItemById(request.Id);

            var options = GetRefreshOptions(request);

            if (item is Folder)
            {
                _providerManager.QueueRefresh(item.Id, options);
            }
            else
            {
                _providerManager.RefreshFullItem(item, options, CancellationToken.None);
            }
        }

        private MetadataRefreshOptions GetRefreshOptions(BaseRefreshRequest request)
        {
            return new MetadataRefreshOptions(new DirectoryService(_fileSystem))
            {
                MetadataRefreshMode = request.MetadataRefreshMode,
                ImageRefreshMode = request.ImageRefreshMode,
                ReplaceAllImages = request.ReplaceAllImages,
                ReplaceAllMetadata = request.ReplaceAllMetadata,
                ForceSave = true,
                IsAutomated = false
            };
        }
    }
}
