using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace MediaBrowser.Api.Images
{
    /// <summary>
    /// Class GetItemImage.
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
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path")]
        public Guid Id { get; set; }
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
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
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
        [ApiMember(Name = "NewIndex", Description = "The new image index", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public int NewIndex { get; set; }
    }

    /// <summary>
    /// Class GetPersonImage
    /// </summary>
    [Route("/Artists/{Name}/Images/{Type}", "GET")]
    [Route("/Artists/{Name}/Images/{Type}/{Index}", "GET")]
    [Route("/Genres/{Name}/Images/{Type}", "GET")]
    [Route("/Genres/{Name}/Images/{Type}/{Index}", "GET")]
    [Route("/MusicGenres/{Name}/Images/{Type}", "GET")]
    [Route("/MusicGenres/{Name}/Images/{Type}/{Index}", "GET")]
    [Route("/Persons/{Name}/Images/{Type}", "GET")]
    [Route("/Persons/{Name}/Images/{Type}/{Index}", "GET")]
    [Route("/Studios/{Name}/Images/{Type}", "GET")]
    [Route("/Studios/{Name}/Images/{Type}/{Index}", "GET")]
    ////[Route("/Years/{Year}/Images/{Type}", "GET")]
    ////[Route("/Years/{Year}/Images/{Type}/{Index}", "GET")]
    [Route("/Artists/{Name}/Images/{Type}", "HEAD")]
    [Route("/Artists/{Name}/Images/{Type}/{Index}", "HEAD")]
    [Route("/Genres/{Name}/Images/{Type}", "HEAD")]
    [Route("/Genres/{Name}/Images/{Type}/{Index}", "HEAD")]
    [Route("/MusicGenres/{Name}/Images/{Type}", "HEAD")]
    [Route("/MusicGenres/{Name}/Images/{Type}/{Index}", "HEAD")]
    [Route("/Persons/{Name}/Images/{Type}", "HEAD")]
    [Route("/Persons/{Name}/Images/{Type}/{Index}", "HEAD")]
    [Route("/Studios/{Name}/Images/{Type}", "HEAD")]
    [Route("/Studios/{Name}/Images/{Type}/{Index}", "HEAD")]
    ////[Route("/Years/{Year}/Images/{Type}", "HEAD")]
    ////[Route("/Years/{Year}/Images/{Type}/{Index}", "HEAD")]
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
        public Guid Id { get; set; }
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
        public Guid Id { get; set; }
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

        private readonly IImageProcessor _imageProcessor;
        private readonly IFileSystem _fileSystem;
        private readonly IAuthorizationContext _authContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageService" /> class.
        /// </summary>
        public ImageService(
            ILogger<ImageService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IUserManager userManager,
            ILibraryManager libraryManager,
            IProviderManager providerManager,
            IImageProcessor imageProcessor,
            IFileSystem fileSystem,
            IAuthorizationContext authContext)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _providerManager = providerManager;
            _imageProcessor = imageProcessor;
            _fileSystem = fileSystem;
            _authContext = authContext;
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

            return ToOptimizedResult(result);
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

            if (itemImages.Length == 0)
            {
                // short-circuit
                return list;
            }

            _libraryManager.UpdateImages(item); // this makes sure dimensions and hashes are correct

            foreach (var image in itemImages)
            {
                if (!item.AllowsMultipleImages(image.Type))
                {
                    var info = GetImageInfo(item, image, null);

                    if (info != null)
                    {
                        list.Add(info);
                    }
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

        private ImageInfo GetImageInfo(BaseItem item, ItemImageInfo info, int? imageIndex)
        {
            int? width = null;
            int? height = null;
            string blurhash = null;
            long length = 0;

            try
            {
                if (info.IsLocalFile)
                {
                    var fileInfo = _fileSystem.GetFileInfo(info.Path);
                    length = fileInfo.Length;

                    blurhash = info.BlurHash;
                    width = info.Width;
                    height = info.Height;

                    if (width <= 0 || height <= 0)
                    {
                        width = null;
                        height = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error getting image information for {Item}", item.Name);
            }

            try
            {
                return new ImageInfo
                {
                    Path = info.Path,
                    ImageIndex = imageIndex,
                    ImageType = info.Type,
                    ImageTag = _imageProcessor.GetImageCacheTag(item, info),
                    Size = length,
                    BlurHash = blurhash,
                    Width = width,
                    Height = height
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error getting image information for {Path}", info.Path);

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
            return GetImage(request, request.Id, null, false);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Head(GetItemImage request)
        {
            return GetImage(request, request.Id, null, true);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetUserImage request)
        {
            var item = _userManager.GetUserById(request.Id);

            return GetImage(request, Guid.Empty, item, false);
        }

        public object Head(GetUserImage request)
        {
            var item = _userManager.GetUserById(request.Id);

            return GetImage(request, Guid.Empty, item, true);
        }

        public object Get(GetItemByNameImage request)
        {
            var type = GetPathValue(0).ToString();

            var item = GetItemByName(request.Name, type, _libraryManager, new DtoOptions(false));

            return GetImage(request, item.Id, item, false);
        }

        public object Head(GetItemByNameImage request)
        {
            var type = GetPathValue(0).ToString();

            var item = GetItemByName(request.Name, type, _libraryManager, new DtoOptions(false));

            return GetImage(request, item.Id, item, true);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public Task Post(PostUserImage request)
        {
            var id = Guid.Parse(GetPathValue(1));

            AssertCanUpdateUser(_authContext, _userManager, id, true);

            request.Type = Enum.Parse<ImageType>(GetPathValue(3).ToString(), true);

            var item = _userManager.GetUserById(id);

            return PostImage(item, request.RequestStream, request.Type, Request.ContentType);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public Task Post(PostItemImage request)
        {
            var id = Guid.Parse(GetPathValue(1));

            request.Type = Enum.Parse<ImageType>(GetPathValue(3).ToString(), true);

            var item = _libraryManager.GetItemById(id);

            return PostImage(item, request.RequestStream, request.Type, Request.ContentType);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(DeleteUserImage request)
        {
            var userId = request.Id;
            AssertCanUpdateUser(_authContext, _userManager, userId, true);

            var item = _userManager.GetUserById(userId);

            item.DeleteImage(request.Type, request.Index ?? 0);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(DeleteItemImage request)
        {
            var item = _libraryManager.GetItemById(request.Id);

            item.DeleteImage(request.Type, request.Index ?? 0);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(UpdateItemImageIndex request)
        {
            var item = _libraryManager.GetItemById(request.Id);

            UpdateItemIndex(item, request.Type, request.Index, request.NewIndex);
        }

        /// <summary>
        /// Updates the index of the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="type">The type.</param>
        /// <param name="currentIndex">Index of the current.</param>
        /// <param name="newIndex">The new index.</param>
        /// <returns>Task.</returns>
        private void UpdateItemIndex(BaseItem item, ImageType type, int currentIndex, int newIndex)
        {
            item.SwapImages(type, currentIndex, newIndex);
        }

        /// <summary>
        /// Gets the image.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="item">The item.</param>
        /// <param name="isHeadRequest">if set to <c>true</c> [is head request].</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="ResourceNotFoundException"></exception>
        public Task<object> GetImage(ImageRequest request, Guid itemId, BaseItem item, bool isHeadRequest)
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

            if (request.UnplayedCount.HasValue
                && request.UnplayedCount.Value <= 0)
            {
                request.UnplayedCount = null;
            }

            if (item == null)
            {
                item = _libraryManager.GetItemById(itemId);

                if (item == null)
                {
                    throw new ResourceNotFoundException(string.Format("Item {0} not found.", itemId.ToString("N", CultureInfo.InvariantCulture)));
                }
            }

            var imageInfo = GetImageInfo(request, item);
            if (imageInfo == null)
            {
                throw new ResourceNotFoundException(string.Format("{0} does not have an image of type {1}", item.Name, request.Type));
            }

            bool cropwhitespace;
            if (request.CropWhitespace.HasValue)
            {
                cropwhitespace = request.CropWhitespace.Value;
            }
            else
            {
                cropwhitespace = request.Type == ImageType.Logo || request.Type == ImageType.Art;
            }

            var outputFormats = GetOutputFormats(request);

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

            return GetImageResult(
                item,
                itemId,
                request,
                imageInfo,
                cropwhitespace,
                outputFormats,
                cacheDuration,
                responseHeaders,
                isHeadRequest);
        }

        private async Task<object> GetImageResult(
            BaseItem item,
            Guid itemId,
            ImageRequest request,
            ItemImageInfo image,
            bool cropwhitespace,
            IReadOnlyCollection<ImageFormat> supportedFormats,
            TimeSpan? cacheDuration,
            IDictionary<string, string> headers,
            bool isHeadRequest)
        {
            if (!image.IsLocalFile)
            {
                item ??= _libraryManager.GetItemById(itemId);
                image = await _libraryManager.ConvertImageToLocal(item, image, request.Index ?? 0).ConfigureAwait(false);
            }

            var options = new ImageProcessingOptions
            {
                CropWhiteSpace = cropwhitespace,
                Height = request.Height,
                ImageIndex = request.Index ?? 0,
                Image = image,
                Item = item,
                ItemId = itemId,
                MaxHeight = request.MaxHeight,
                MaxWidth = request.MaxWidth,
                Quality = request.Quality ?? 100,
                Width = request.Width,
                AddPlayedIndicator = request.AddPlayedIndicator,
                PercentPlayed = request.PercentPlayed ?? 0,
                UnplayedCount = request.UnplayedCount,
                Blur = request.Blur,
                BackgroundColor = request.BackgroundColor,
                ForegroundLayer = request.ForegroundLayer,
                SupportedOutputFormats = supportedFormats
            };

            var imageResult = await _imageProcessor.ProcessImage(options).ConfigureAwait(false);

            headers[HeaderNames.Vary] = HeaderNames.Accept;

            return await ResultFactory.GetStaticFileResult(Request, new StaticFileResultOptions
            {
                CacheDuration = cacheDuration,
                ResponseHeaders = headers,
                ContentType = imageResult.Item2,
                DateLastModified = imageResult.Item3,
                IsHeadRequest = isHeadRequest,
                Path = imageResult.Item1,

                FileShare = FileShare.Read

            }).ConfigureAwait(false);
        }

        private ImageFormat[] GetOutputFormats(ImageRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.Format)
                && Enum.TryParse(request.Format, true, out ImageFormat format))
            {
                return new[] { format };
            }

            return GetClientSupportedFormats();
        }

        private ImageFormat[] GetClientSupportedFormats()
        {
            var supportedFormats = Request.AcceptTypes ?? Array.Empty<string>();
            if (supportedFormats.Length > 0)
            {
                for (int i = 0; i < supportedFormats.Length; i++)
                {
                    int index = supportedFormats[i].IndexOf(';');
                    if (index != -1)
                    {
                        supportedFormats[i] = supportedFormats[i].Substring(0, index);
                    }
                }
            }

            var acceptParam = Request.QueryString["accept"];

            var supportsWebP = SupportsFormat(supportedFormats, acceptParam, "webp", false);

            if (!supportsWebP)
            {
                var userAgent = Request.UserAgent ?? string.Empty;
                if (userAgent.IndexOf("crosswalk", StringComparison.OrdinalIgnoreCase) != -1 &&
                    userAgent.IndexOf("android", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    supportsWebP = true;
                }
            }

            var formats = new List<ImageFormat>(4);

            if (supportsWebP)
            {
                formats.Add(ImageFormat.Webp);
            }

            formats.Add(ImageFormat.Jpg);
            formats.Add(ImageFormat.Png);

            if (SupportsFormat(supportedFormats, acceptParam, "gif", true))
            {
                formats.Add(ImageFormat.Gif);
            }

            return formats.ToArray();
        }

        private bool SupportsFormat(IEnumerable<string> requestAcceptTypes, string acceptParam, string format, bool acceptAll)
        {
            var mimeType = "image/" + format;

            if (requestAcceptTypes.Contains(mimeType))
            {
                return true;
            }

            if (acceptAll && requestAcceptTypes.Contains("*/*"))
            {
                return true;
            }

            return string.Equals(Request.QueryString["accept"], format, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the image path.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        private ItemImageInfo GetImageInfo(ImageRequest request, BaseItem item)
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
            using var reader = new StreamReader(inputStream);
            var text = await reader.ReadToEndAsync().ConfigureAwait(false);

            var bytes = Convert.FromBase64String(text);

            var memoryStream = new MemoryStream(bytes)
            {
                Position = 0
            };

            // Handle image/png; charset=utf-8
            mimeType = mimeType.Split(';').FirstOrDefault();

            await _providerManager.SaveImage(entity, memoryStream, mimeType, imageType, null, CancellationToken.None).ConfigureAwait(false);

            entity.UpdateToRepository(ItemUpdateType.ImageUpdate, CancellationToken.None);
        }
    }
}
