using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using ServiceStack.ServiceHost;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    [Route("/Items/{Id}/RemoteImages", "GET")]
    [Api(Description = "Gets available remote images for an item")]
    public class GetRemoteImages : IReturn<RemoteImageResult>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "Type", Description = "The image type", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public ImageType? Type { get; set; }

        /// <summary>
        /// Skips over a given number of items within the results. Use for paging.
        /// </summary>
        /// <value>The start index.</value>
        [ApiMember(Name = "StartIndex", Description = "Optional. The record index to start at. All items with a lower index will be dropped from the results.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? StartIndex { get; set; }

        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        /// <value>The limit.</value>
        [ApiMember(Name = "Limit", Description = "Optional. The maximum number of records to return", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Limit { get; set; }

        [ApiMember(Name = "ProviderName", Description = "Optional. The image provider to use", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ProviderName { get; set; }
    }

    [Route("/Items/{Id}/RemoteImages/Download", "POST")]
    [Api(Description = "Downloads a remote image for an item")]
    public class DownloadRemoteImage : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "Type", Description = "The image type", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public ImageType Type { get; set; }

        [ApiMember(Name = "ProviderName", Description = "The image provider", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ProviderName { get; set; }

        [ApiMember(Name = "ImageUrl", Description = "The image url", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ImageUrl { get; set; }
    }

    public class RemoteImageService : BaseApiService
    {
        private readonly IProviderManager _providerManager;

        private readonly IDtoService _dtoService;

        public RemoteImageService(IProviderManager providerManager, IDtoService dtoService)
        {
            _providerManager = providerManager;
            _dtoService = dtoService;
        }

        public object Get(GetRemoteImages request)
        {
            var item = _dtoService.GetItemByDtoId(request.Id);

            var images = _providerManager.GetAvailableRemoteImages(item, CancellationToken.None, request.ProviderName, request.Type).Result;

            var imagesList = images.ToList();

            var result = new RemoteImageResult
            {
                TotalRecordCount = imagesList.Count,
                Providers = _providerManager.GetImageProviders(item).Select(i => i.Name).ToList()
            };

            if (request.StartIndex.HasValue)
            {
                imagesList = imagesList.Skip(request.StartIndex.Value)
                    .ToList();
            }

            if (request.Limit.HasValue)
            {
                imagesList = imagesList.Take(request.Limit.Value)
                    .ToList();
            }

            result.Images = imagesList;

            return ToOptimizedResult(result);
        }

        public void Post(DownloadRemoteImage request)
        {
            var task = DownloadRemoteImage(request);

            Task.WaitAll(task);
        }

        private async Task DownloadRemoteImage(DownloadRemoteImage request)
        {
            var item = _dtoService.GetItemByDtoId(request.Id);

            int? index = null;

            if (request.Type == ImageType.Backdrop)
            {
                index = item.BackdropImagePaths.Count;
            }

            await _providerManager.SaveImage(item, request.ImageUrl, null, request.Type, index, CancellationToken.None).ConfigureAwait(false);

            await item.RefreshMetadata(CancellationToken.None, forceSave: true, allowSlowProviders: false)
                    .ConfigureAwait(false);
        }
    }
}
