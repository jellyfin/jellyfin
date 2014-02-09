using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using ServiceStack;
using ServiceStack.Text.Controller;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Images
{
    public class BaseRemoteImageRequest : IReturn<RemoteImageResult>
    {
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

    [Route("/Items/{Id}/RemoteImages", "GET")]
    [Api(Description = "Gets available remote images for an item")]
    public class GetRemoteImages : BaseRemoteImageRequest
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Artists/{Name}/RemoteImages", "GET")]
    [Route("/Genres/{Name}/RemoteImages", "GET")]
    [Route("/GameGenres/{Name}/RemoteImages", "GET")]
    [Route("/MusicGenres/{Name}/RemoteImages", "GET")]
    [Route("/Persons/{Name}/RemoteImages", "GET")]
    [Route("/Studios/{Name}/RemoteImages", "GET")]
    [Api(Description = "Gets available remote images for an item")]
    public class GetItemByNameRemoteImages : BaseRemoteImageRequest
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Name", Description = "Name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    [Route("/Items/{Id}/RemoteImages/Providers", "GET")]
    [Api(Description = "Gets available remote image providers for an item")]
    public class GetRemoteImageProviders : IReturn<List<ImageProviderInfo>>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Artists/{Name}/RemoteImages/Providers", "GET")]
    [Route("/Genres/{Name}/RemoteImages/Providers", "GET")]
    [Route("/GameGenres/{Name}/RemoteImages/Providers", "GET")]
    [Route("/MusicGenres/{Name}/RemoteImages/Providers", "GET")]
    [Route("/Persons/{Name}/RemoteImages/Providers", "GET")]
    [Route("/Studios/{Name}/RemoteImages/Providers", "GET")]
    [Api(Description = "Gets available remote image providers for an item")]
    public class GetItemByNameRemoteImageProviders : IReturn<List<ImageProviderInfo>>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Name", Description = "Name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    public class BaseDownloadRemoteImage : IReturnVoid
    {
        [ApiMember(Name = "Type", Description = "The image type", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public ImageType Type { get; set; }

        [ApiMember(Name = "ProviderName", Description = "The image provider", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ProviderName { get; set; }

        [ApiMember(Name = "ImageUrl", Description = "The image url", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ImageUrl { get; set; }
    }

    [Route("/Items/{Id}/RemoteImages/Download", "POST")]
    [Api(Description = "Downloads a remote image for an item")]
    public class DownloadRemoteImage : BaseDownloadRemoteImage
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Artists/{Name}/RemoteImages/Download", "POST")]
    [Route("/Genres/{Name}/RemoteImages/Download", "POST")]
    [Route("/GameGenres/{Name}/RemoteImages/Download", "POST")]
    [Route("/MusicGenres/{Name}/RemoteImages/Download", "POST")]
    [Route("/Persons/{Name}/RemoteImages/Download", "POST")]
    [Route("/Studios/{Name}/RemoteImages/Download", "POST")]
    [Api(Description = "Downloads a remote image for an item")]
    public class DownloadItemByNameRemoteImage : BaseDownloadRemoteImage
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Name", Description = "Name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Name { get; set; }
    }

    [Route("/Images/Remote", "GET")]
    [Api(Description = "Gets a remote image")]
    public class GetRemoteImage
    {
        [ApiMember(Name = "ImageUrl", Description = "The image url", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ImageUrl { get; set; }
    }

    public class RemoteImageService : BaseApiService
    {
        private readonly IProviderManager _providerManager;

        private readonly IServerApplicationPaths _appPaths;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;

        private readonly IDtoService _dtoService;
        private readonly ILibraryManager _libraryManager;

        public RemoteImageService(IProviderManager providerManager, IDtoService dtoService, IServerApplicationPaths appPaths, IHttpClient httpClient, IFileSystem fileSystem, ILibraryManager libraryManager)
        {
            _providerManager = providerManager;
            _dtoService = dtoService;
            _appPaths = appPaths;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
            _libraryManager = libraryManager;
        }

        public object Get(GetRemoteImageProviders request)
        {
            var item = _dtoService.GetItemByDtoId(request.Id);

            var result = GetImageProviders(item);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        public object Get(GetItemByNameRemoteImageProviders request)
        {
            var pathInfo = PathInfo.Parse(Request.PathInfo);
            var type = pathInfo.GetArgumentValue<string>(0);

            var item = GetItemByName(request.Name, type, _libraryManager);

            var result = GetImageProviders(item);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        private List<ImageProviderInfo> GetImageProviders(BaseItem item)
        {
            return _providerManager.GetImageProviderInfo(item).ToList();
        }

        public object Get(GetRemoteImages request)
        {
            var item = _dtoService.GetItemByDtoId(request.Id);

            var result = GetRemoteImageResult(item, request);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        public object Get(GetItemByNameRemoteImages request)
        {
            var pathInfo = PathInfo.Parse(Request.PathInfo);
            var type = pathInfo.GetArgumentValue<string>(0);

            var item = GetItemByName(request.Name, type, _libraryManager);

            return GetRemoteImageResult(item, request);
        }

        private RemoteImageResult GetRemoteImageResult(BaseItem item, BaseRemoteImageRequest request)
        {
            var images = _providerManager.GetAvailableRemoteImages(item, CancellationToken.None, request.ProviderName, request.Type).Result;

            var imagesList = images.ToList();

            var result = new RemoteImageResult
            {
                TotalRecordCount = imagesList.Count,
                Providers = images.Select(i => i.ProviderName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
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

            return result;
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(DownloadRemoteImage request)
        {
            var item = _dtoService.GetItemByDtoId(request.Id);

            var task = DownloadRemoteImage(item, request);

            Task.WaitAll(task);
        }

        public void Post(DownloadItemByNameRemoteImage request)
        {
            var pathInfo = PathInfo.Parse(Request.PathInfo);
            var type = pathInfo.GetArgumentValue<string>(0);

            var item = GetItemByName(request.Name, type, _libraryManager);

            var task = DownloadRemoteImage(item, request);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Downloads the remote image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="request">The request.</param>
        /// <returns>Task.</returns>
        private async Task DownloadRemoteImage(BaseItem item, BaseDownloadRemoteImage request)
        {
            await _providerManager.SaveImage(item, request.ImageUrl, null, request.Type, null, CancellationToken.None).ConfigureAwait(false);

            await item.UpdateToRepository(ItemUpdateType.ImageUpdate, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetRemoteImage request)
        {
            var task = GetRemoteImage(request);

            return task.Result;
        }

        /// <summary>
        /// Gets the remote image.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{System.Object}.</returns>
        private async Task<object> GetRemoteImage(GetRemoteImage request)
        {
            var urlHash = request.ImageUrl.GetMD5();
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
            catch (DirectoryNotFoundException)
            {
                // Means the file isn't cached yet
            }
            catch (FileNotFoundException)
            {
                // Means the file isn't cached yet
            }

            await DownloadImage(request.ImageUrl, urlHash, pointerCachePath).ConfigureAwait(false);

            // Read the pointer file again
            using (var reader = new StreamReader(pointerCachePath))
            {
                contentPath = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            return ToStaticFileResult(contentPath);
        }

        /// <summary>
        /// Downloads the image.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="urlHash">The URL hash.</param>
        /// <param name="pointerCachePath">The pointer cache path.</param>
        /// <returns>Task.</returns>
        private async Task DownloadImage(string url, Guid urlHash, string pointerCachePath)
        {
            var result = await _httpClient.GetResponse(new HttpRequestOptions
            {
                Url = url

            }).ConfigureAwait(false);

            var ext = result.ContentType.Split('/').Last();

            var fullCachePath = GetFullCachePath(urlHash + "." + ext);

            Directory.CreateDirectory(Path.GetDirectoryName(fullCachePath));
            using (var stream = result.Content)
            {
                using (var filestream = _fileSystem.GetFileStream(fullCachePath, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                {
                    await stream.CopyToAsync(filestream).ConfigureAwait(false);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(pointerCachePath));
            using (var writer = new StreamWriter(pointerCachePath))
            {
                await writer.WriteAsync(fullCachePath).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the full cache path.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.String.</returns>
        private string GetFullCachePath(string filename)
        {
            return Path.Combine(_appPaths.CachePath, "remote-images", filename.Substring(0, 1), filename);
        }
    }
}
