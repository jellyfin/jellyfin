using System;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
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

        [ApiMember(Name = "Type", Description = "The image type", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
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

    [Route("/Images/Remote", "GET")]
    [Api(Description = "Gets a remote image")]
    public class GetRemoteImage
    {
        [ApiMember(Name = "Url", Description = "The image url", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Url { get; set; }
    }

    public class RemoteImageService : BaseApiService
    {
        private readonly IProviderManager _providerManager;

        private readonly IServerApplicationPaths _appPaths;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;

        private readonly IDtoService _dtoService;

        public RemoteImageService(IProviderManager providerManager, IDtoService dtoService, IServerApplicationPaths appPaths, IHttpClient httpClient, IFileSystem fileSystem)
        {
            _providerManager = providerManager;
            _dtoService = dtoService;
            _appPaths = appPaths;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
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

        public object Get(GetRemoteImage request)
        {
            var task = GetRemoteImage(request);

            return task.Result;
        }

        private async Task<object> GetRemoteImage(GetRemoteImage request)
        {
            var urlHash = request.Url.GetMD5();
            var pointerCachePath = GetFullCachePath(urlHash.ToString());

            string contentPath;

            try
            {
                using (var reader = new StreamReader(pointerCachePath))
                {
                    contentPath = await reader.ReadToEndAsync().ConfigureAwait(false);
                }

                if (File.Exists(contentPath))
                {
                    return ToStaticFileResult(contentPath);
                }
            }
            catch (FileNotFoundException)
            {
                // Means the file isn't cached yet
            }

            await DownloadImage(request.Url, urlHash, pointerCachePath).ConfigureAwait(false);

            // Read the pointer file again
            using (var reader = new StreamReader(pointerCachePath))
            {
                contentPath = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            return ToStaticFileResult(contentPath);
        }

        private async Task DownloadImage(string url, Guid urlHash, string pointerCachePath)
        {
            var result = await _httpClient.GetResponse(new HttpRequestOptions
            {
                Url = url

            }).ConfigureAwait(false);

            var ext = result.ContentType.Split('/').Last();

            var fullCachePath = GetFullCachePath(urlHash + "." + ext);

            using (var stream = result.Content)
            {
                using (var filestream = _fileSystem.GetFileStream(fullCachePath, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                {
                    await stream.CopyToAsync(filestream).ConfigureAwait(false);
                }
            }

            using (var writer = new StreamWriter(pointerCachePath))
            {
                await writer.WriteAsync(fullCachePath).ConfigureAwait(false);
            }
        }

        private string GetFullCachePath(string filename)
        {
            return Path.Combine(_appPaths.DownloadedImagesDataPath, filename.Substring(0, 1), filename);
        }
    }
}
