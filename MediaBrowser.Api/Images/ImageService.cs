using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using ServiceStack;
using ServiceStack.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Api.Images
{
    /// <summary>
    /// Class GetItemImage
    /// </summary>
    [Route("/Items/{Id}/Images", "GET", Summary = "Gets information about an item's images")]
    [Authenticated]
    public class GetItemImageInfos : IReturn<List<ImageInfo>>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Items/{Id}/Images/{Type}", "GET")]
    [Route("/Items/{Id}/Images/{Type}/{Index}", "GET")]
    [Route("/Items/{Id}/Images/{Type}", "HEAD")]
    [Route("/Items/{Id}/Images/{Type}/{Index}", "HEAD")]
    [Route("/Items/{Id}/Images/{Type}/{Index}/{Tag}/{Format}/{MaxWidth}/{MaxHeight}/{PercentPlayed}/{UnplayedCount}", "GET")]
    [Route("/Items/{Id}/Images/{Type}/{Index}/{Tag}/{Format}/{MaxWidth}/{MaxHeight}/{PercentPlayed}/{UnplayedCount}", "HEAD")]
    public class GetItemImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class UpdateItemImageIndex
    /// </summary>
    [Route("/Items/{Id}/Images/{Type}/{Index}/Index", "POST", Summary = "Updates the index for an item image")]
    [Authenticated(Roles = "admin")]
    public class UpdateItemImageIndex : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the type of the image.
        /// </summary>
        /// <value>The type of the image.</value>
        [ApiMember(Name = "Type", Description = "Image Type", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public ImageType Type { get; set; }

        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        /// <value>The index.</value>
        [ApiMember(Name = "Index", Description = "Image Index", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "POST")]
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the new index.
        /// </summary>
        /// <value>The new index.</value>
        [ApiMember(Name = "NewIndex", Description = "The new image index", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public int NewIndex { get; set; }
    }

    /// <summary>
    /// Class GetPersonImage
    /// </summary>
    [Route("/Artists/{Name}/Images/{Type}", "GET")]
    [Route("/Artists/{Name}/Images/{Type}/{Index}", "GET")]
    [Route("/Genres/{Name}/Images/{Type}", "GET")]
    [Route("/Genres/{Name}/Images/{Type}/{Index}", "GET")]
    [Route("/GameGenres/{Name}/Images/{Type}", "GET")]
    [Route("/GameGenres/{Name}/Images/{Type}/{Index}", "GET")]
    [Route("/MusicGenres/{Name}/Images/{Type}", "GET")]
    [Route("/MusicGenres/{Name}/Images/{Type}/{Index}", "GET")]
    [Route("/Persons/{Name}/Images/{Type}", "GET")]
    [Route("/Persons/{Name}/Images/{Type}/{Index}", "GET")]
    [Route("/Studios/{Name}/Images/{Type}", "GET")]
    [Route("/Studios/{Name}/Images/{Type}/{Index}", "GET")]
    [Route("/Years/{Year}/Images/{Type}", "GET")]
    [Route("/Years/{Year}/Images/{Type}/{Index}", "GET")]
    [Route("/Artists/{Name}/Images/{Type}", "HEAD")]
    [Route("/Artists/{Name}/Images/{Type}/{Index}", "HEAD")]
    [Route("/Genres/{Name}/Images/{Type}", "HEAD")]
    [Route("/Genres/{Name}/Images/{Type}/{Index}", "HEAD")]
    [Route("/GameGenres/{Name}/Images/{Type}", "HEAD")]
    [Route("/GameGenres/{Name}/Images/{Type}/{Index}", "HEAD")]
    [Route("/MusicGenres/{Name}/Images/{Type}", "HEAD")]
    [Route("/MusicGenres/{Name}/Images/{Type}/{Index}", "HEAD")]
    [Route("/Persons/{Name}/Images/{Type}", "HEAD")]
    [Route("/Persons/{Name}/Images/{Type}/{Index}", "HEAD")]
    [Route("/Studios/{Name}/Images/{Type}", "HEAD")]
    [Route("/Studios/{Name}/Images/{Type}/{Index}", "HEAD")]
    [Route("/Years/{Year}/Images/{Type}", "HEAD")]
    [Route("/Years/{Year}/Images/{Type}/{Index}", "HEAD")]
    public class GetItemByNameImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "Item name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Class GetUserImage
    /// </summary>
    [Route("/Users/{Id}/Images/{Type}", "GET")]
    [Route("/Users/{Id}/Images/{Type}/{Index}", "GET")]
    [Route("/Users/{Id}/Images/{Type}", "HEAD")]
    [Route("/Users/{Id}/Images/{Type}/{Index}", "HEAD")]
    public class GetUserImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class DeleteItemImage
    /// </summary>
    [Route("/Items/{Id}/Images/{Type}", "DELETE")]
    [Route("/Items/{Id}/Images/{Type}/{Index}", "DELETE")]
    [Authenticated(Roles = "admin")]
    public class DeleteItemImage : DeleteImageRequest, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class DeleteUserImage
    /// </summary>
    [Route("/Users/{Id}/Images/{Type}", "DELETE")]
    [Route("/Users/{Id}/Images/{Type}/{Index}", "DELETE")]
    [Authenticated]
    public class DeleteUserImage : DeleteImageRequest, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class PostUserImage
    /// </summary>
    [Route("/Users/{Id}/Images/{Type}", "POST")]
    [Route("/Users/{Id}/Images/{Type}/{Index}", "POST")]
    [Authenticated]
    public class PostUserImage : DeleteImageRequest, IRequiresRequestStream, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        /// <summary>
        /// The raw Http Request Input Stream
        /// </summary>
        /// <value>The request stream.</value>
        public Stream RequestStream { get; set; }
    }

    /// <summary>
    /// Class PostItemImage
    /// </summary>
    [Route("/Items/{Id}/Images/{Type}", "POST")]
    [Route("/Items/{Id}/Images/{Type}/{Index}", "POST")]
    [Api(Description = "Posts an item image")]
    [Authenticated(Roles = "admin")]
    public class PostItemImage : DeleteImageRequest, IRequiresRequestStream, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        /// <summary>
        /// The raw Http Request Input Stream
        /// </summary>
        /// <value>The request stream.</value>
        public Stream RequestStream { get; set; }
    }

    /// <summary>
    /// Class ImageService
    /// </summary>
    public class ImageService : BaseApiService
    {
        private readonly IUserManager _userManager;

        private readonly ILibraryManager _libraryManager;

        private readonly IProviderManager _providerManager;

        private readonly IItemRepository _itemRepo;
        private readonly IImageProcessor _imageProcessor;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageService" /> class.
        /// </summary>
        public ImageService(IUserManager userManager, ILibraryManager libraryManager, IProviderManager providerManager, IItemRepository itemRepo, IImageProcessor imageProcessor, IFileSystem fileSystem)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _providerManager = providerManager;
            _itemRepo = itemRepo;
            _imageProcessor = imageProcessor;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetItemImageInfos request)
        {
            var item = _libraryManager.GetItemById(request.Id);

            var result = GetItemImageInfos(item);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        /// <summary>
        /// Gets the item image infos.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Task{List{ImageInfo}}.</returns>
        public List<ImageInfo> GetItemImageInfos(BaseItem item)
        {
            var list = new List<ImageInfo>();

            var itemImages = item.ImageInfos;

            foreach (var image in itemImages.Where(i => !item.AllowsMultipleImages(i.Type)))
            {
                var info = GetImageInfo(item, image, null);

                if (info != null)
                {
                    list.Add(info);
                }
            }

            foreach (var imageType in itemImages.Select(i => i.Type).Distinct().Where(item.AllowsMultipleImages))
            {
                var index = 0;

                // Prevent implicitly captured closure
                var currentImageType = imageType;

                foreach (var image in itemImages.Where(i => i.Type == currentImageType))
                {
                    var info = GetImageInfo(item, image, index);

                    if (info != null)
                    {
                        list.Add(info);
                    }

                    index++;
                }
            }

            return list;
        }

        private ImageInfo GetImageInfo(IHasImages item, ItemImageInfo info, int? imageIndex)
        {
            try
            {
                int? width = null;
                int? height = null;
                long length = 0;

                try
                {
                    if (info.IsLocalFile)
                    {
                        var fileInfo = new FileInfo(info.Path);
                        length = fileInfo.Length;

                        var size = _imageProcessor.GetImageSize(info);

                        width = Convert.ToInt32(size.Width);
                        height = Convert.ToInt32(size.Height);

                    }
                }
                catch
                {

                }
                return new ImageInfo
                {
                    Path = info.Path,
                    ImageIndex = imageIndex,
                    ImageType = info.Type,
                    ImageTag = _imageProcessor.GetImageCacheTag(item, info),
                    Size = length,
                    Width = width,
                    Height = height
                };
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error getting image information for {0}", ex, info.Path);

                return null;
            }
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetItemImage request)
        {
            var item = string.IsNullOrEmpty(request.Id) ?
                _libraryManager.RootFolder :
                _libraryManager.GetItemById(request.Id);

            return GetImage(request, item, false);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Head(GetItemImage request)
        {
            var item = string.IsNullOrEmpty(request.Id) ?
                _libraryManager.RootFolder :
                _libraryManager.GetItemById(request.Id);

            return GetImage(request, item, true);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetUserImage request)
        {
            var item = _userManager.GetUserById(request.Id);

            return GetImage(request, item, false);
        }

        public object Head(GetUserImage request)
        {
            var item = _userManager.GetUserById(request.Id);

            return GetImage(request, item, true);
        }

        public object Get(GetItemByNameImage request)
        {
            var type = GetPathValue(0);

            var item = GetItemByName(request.Name, type, _libraryManager);

            return GetImage(request, item, false);
        }

        public object Head(GetItemByNameImage request)
        {
            var type = GetPathValue(0);

            var item = GetItemByName(request.Name, type, _libraryManager);

            return GetImage(request, item, true);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(PostUserImage request)
        {
            var userId = GetPathValue(1);
            AssertCanUpdateUser(_userManager, userId);

            request.Type = (ImageType)Enum.Parse(typeof(ImageType), GetPathValue(3), true);

            var item = _userManager.GetUserById(userId);

            var task = PostImage(item, request.RequestStream, request.Type, Request.ContentType);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(PostItemImage request)
        {
            var id = GetPathValue(1);

            request.Type = (ImageType)Enum.Parse(typeof(ImageType), GetPathValue(3), true);

            var item = _libraryManager.GetItemById(id);

            var task = PostImage(item, request.RequestStream, request.Type, Request.ContentType);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(DeleteUserImage request)
        {
            var userId = request.Id;
            AssertCanUpdateUser(_userManager, userId);

            var item = _userManager.GetUserById(userId);

            var task = item.DeleteImage(request.Type, request.Index ?? 0);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(DeleteItemImage request)
        {
            var item = _libraryManager.GetItemById(request.Id);

            var task = item.DeleteImage(request.Type, request.Index ?? 0);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(UpdateItemImageIndex request)
        {
            var item = _libraryManager.GetItemById(request.Id);

            var task = UpdateItemIndex(item, request.Type, request.Index, request.NewIndex);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Updates the index of the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="type">The type.</param>
        /// <param name="currentIndex">Index of the current.</param>
        /// <param name="newIndex">The new index.</param>
        /// <returns>Task.</returns>
        private Task UpdateItemIndex(IHasImages item, ImageType type, int currentIndex, int newIndex)
        {
            return item.SwapImages(type, currentIndex, newIndex);
        }

        /// <summary>
        /// Gets the image.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="item">The item.</param>
        /// <param name="isHeadRequest">if set to <c>true</c> [is head request].</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="ResourceNotFoundException"></exception>
        public Task<object> GetImage(ImageRequest request, IHasImages item, bool isHeadRequest)
        {
            if (request.PercentPlayed.HasValue)
            {
                if (request.PercentPlayed.Value <= 0)
                {
                    request.PercentPlayed = null;
                }
                else if (request.PercentPlayed.Value >= 100)
                {
                    request.PercentPlayed = null;
                    request.AddPlayedIndicator = true;
                }
            }
            if (request.PercentPlayed.HasValue)
            {
                request.UnplayedCount = null;
            }
            if (request.UnplayedCount.HasValue)
            {
                if (request.UnplayedCount.Value <= 0)
                {
                    request.UnplayedCount = null;
                }
            }

            var imageInfo = GetImageInfo(request, item);

            if (imageInfo == null)
            {
                throw new ResourceNotFoundException(string.Format("{0} does not have an image of type {1}", item.Name, request.Type));
            }

            var supportedImageEnhancers = request.EnableImageEnhancers ? _imageProcessor.ImageEnhancers.Where(i =>
            {
                try
                {
                    return i.Supports(item, request.Type);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error in image enhancer: {0}", ex, i.GetType().Name);

                    return false;
                }

            }).ToList() : new List<IImageEnhancer>();

            var cropwhitespace = request.Type == ImageType.Logo || request.Type == ImageType.Art;

            if (request.CropWhitespace.HasValue)
            {
                cropwhitespace = request.CropWhitespace.Value;
            }

            var outputFormats = GetOutputFormats(request, imageInfo, cropwhitespace, supportedImageEnhancers);

            TimeSpan? cacheDuration = null;

            if (!string.IsNullOrEmpty(request.Tag))
            {
                cacheDuration = TimeSpan.FromDays(365);
            }

            var responseHeaders = new Dictionary<string, string>
            {
                {"transferMode.dlna.org", "Interactive"},
                {"realTimeInfo.dlna.org", "DLNA.ORG_TLAG=*"}
            };

            return GetImageResult(item,
                request,
                imageInfo,
                cropwhitespace,
                outputFormats,
                supportedImageEnhancers,
                cacheDuration,
                responseHeaders,
                isHeadRequest);
        }

        private async Task<object> GetImageResult(IHasImages item,
            ImageRequest request,
            ItemImageInfo image,
            bool cropwhitespace,
            List<ImageFormat> supportedFormats,
            List<IImageEnhancer> enhancers,
            TimeSpan? cacheDuration,
            IDictionary<string, string> headers,
            bool isHeadRequest)
        {
            var options = new ImageProcessingOptions
            {
                CropWhiteSpace = cropwhitespace,
                Enhancers = enhancers,
                Height = request.Height,
                ImageIndex = request.Index ?? 0,
                Image = image,
                Item = item,
                MaxHeight = request.MaxHeight,
                MaxWidth = request.MaxWidth,
                Quality = request.Quality ?? 100,
                Width = request.Width,
                AddPlayedIndicator = request.AddPlayedIndicator,
                PercentPlayed = request.PercentPlayed ?? 0,
                UnplayedCount = request.UnplayedCount,
                BackgroundColor = request.BackgroundColor,
                ForegroundLayer = request.ForegroundLayer,
                SupportedOutputFormats = supportedFormats
            };

            var imageResult = await _imageProcessor.ProcessImage(options).ConfigureAwait(false);

            headers["Vary"] = "Accept";

            return await ResultFactory.GetStaticFileResult(Request, new StaticFileResultOptions
            {
                CacheDuration = cacheDuration,
                ResponseHeaders = headers,
                ContentType = imageResult.Item2,
                DateLastModified = imageResult.Item3,
                IsHeadRequest = isHeadRequest,
                Path = imageResult.Item1,

                // Sometimes imagemagick keeps a hold on the file briefly even after it's done writing to it.
                // I'd rather do this than add a delay after saving the file
                FileShare = FileShare.ReadWrite

            }).ConfigureAwait(false);
        }

        private List<ImageFormat> GetOutputFormats(ImageRequest request, ItemImageInfo image, bool cropwhitespace, List<IImageEnhancer> enhancers)
        {
            if (!string.IsNullOrWhiteSpace(request.Format))
            {
                ImageFormat format;
                if (Enum.TryParse(request.Format, true, out format))
                {
                    return new List<ImageFormat> { format };
                }
            }

            var extension = Path.GetExtension(image.Path);
            ImageFormat? inputFormat = null;

            if (string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                inputFormat = ImageFormat.Jpg;
            }
            else if (string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase))
            {
                inputFormat = ImageFormat.Png;
            }

            var clientSupportedFormats = GetClientSupportedFormats();

            var serverFormats = _imageProcessor.GetSupportedImageOutputFormats();
            var outputFormats = new List<ImageFormat>();

            // Client doesn't care about format, so start with webp if supported
            if (serverFormats.Contains(ImageFormat.Webp) && clientSupportedFormats.Contains(ImageFormat.Webp))
            {
                outputFormats.Add(ImageFormat.Webp);
            }

            if (enhancers.Count > 0)
            {
                outputFormats.Add(ImageFormat.Png);
            }

            if (inputFormat.HasValue && inputFormat.Value == ImageFormat.Jpg)
            {
                outputFormats.Add(ImageFormat.Jpg);
            }

            // We can't predict if there will be transparency or not, so play it safe
            outputFormats.Add(ImageFormat.Png);

            return outputFormats;
        }

        private ImageFormat[] GetClientSupportedFormats()
        {
            //Logger.Debug("Request types: {0}", string.Join(",", Request.AcceptTypes ?? new string[] { }));
            var supportsWebP = (Request.AcceptTypes ?? new string[] { }).Contains("image/webp", StringComparer.OrdinalIgnoreCase);

            var userAgent = Request.UserAgent ?? string.Empty;

            if (!supportsWebP)
            {
                if (string.Equals(Request.QueryString["accept"], "webp", StringComparison.OrdinalIgnoreCase))
                {
                    supportsWebP = true;
                }
            }

            if (!supportsWebP)
            {
                if (userAgent.IndexOf("crosswalk", StringComparison.OrdinalIgnoreCase) != -1 &&
                    userAgent.IndexOf("android", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    supportsWebP = true;
                }
            }

            if (supportsWebP)
            {
                // Not displaying properly on iOS
                if (userAgent.IndexOf("cfnetwork", StringComparison.OrdinalIgnoreCase) == -1)
                {
                    return new[] { ImageFormat.Webp, ImageFormat.Jpg, ImageFormat.Png };
                }
            }

            return new[] { ImageFormat.Jpg, ImageFormat.Png };
        }

        /// <summary>
        /// Gets the image path.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        private ItemImageInfo GetImageInfo(ImageRequest request, IHasImages item)
        {
            var index = request.Index ?? 0;

            return item.GetImageInfo(request.Type, index);
        }

        /// <summary>
        /// Posts the image.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="inputStream">The input stream.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="mimeType">Type of the MIME.</param>
        /// <returns>Task.</returns>
        public async Task PostImage(BaseItem entity, Stream inputStream, ImageType imageType, string mimeType)
        {
            using (var reader = new StreamReader(inputStream))
            {
                var text = await reader.ReadToEndAsync().ConfigureAwait(false);

                var bytes = Convert.FromBase64String(text);

                var memoryStream = new MemoryStream(bytes)
                {
                    Position = 0
                };

                // Handle image/png; charset=utf-8
                mimeType = mimeType.Split(';').FirstOrDefault();

                await _providerManager.SaveImage(entity, memoryStream, mimeType, imageType, null, CancellationToken.None).ConfigureAwait(false);

                await entity.UpdateToRepository(ItemUpdateType.ImageUpdate, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
