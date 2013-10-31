using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using ServiceStack.ServiceHost;
using System.Linq;
using System.Threading;

namespace MediaBrowser.Api
{
    [Route("/Items/{Id}/RemoteImages/{Type}", "GET")]
    [Api(Description = "Gets available remote images for an item")]
    public class GetRemoteImages : IReturn<RemoteImageResult>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "Type", Description = "The image type", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public ImageType Type { get; set; }

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

            var images = _providerManager.GetAvailableRemoteImages(item, request.Type, CancellationToken.None).Result;

            var imagesList = images.ToList();

            var result = new RemoteImageResult
            {
                TotalRecordCount = imagesList.Count
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
    }
}
