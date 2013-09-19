using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using ServiceStack.ServiceHost;
using ServiceStack.Text.Controller;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Images
{
    /// <summary>
    /// Class GetItemImage
    /// </summary>
    [Route("/Items/{Id}/Images", "GET")]
    [Api(Description = "Gets information about an item's images")]
    public class GetItemImageInfos : IReturn<List<ImageInfo>>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Artists/{Name}/Images", "GET")]
    [Route("/Genres/{Name}/Images", "GET")]
    [Route("/GameGenres/{Name}/Images", "GET")]
    [Route("/MusicGenres/{Name}/Images", "GET")]
    [Route("/Persons/{Name}/Images", "GET")]
    [Route("/Studios/{Name}/Images", "GET")]
    [Api(Description = "Gets information about an item's images")]
    public class GetItemByNameImageInfos : IReturn<List<ImageInfo>>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Name", Description = "Name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    [Route("/Items/{Id}/Images/{Type}", "GET")]
    [Route("/Items/{Id}/Images/{Type}/{Index}", "GET")]
    [Api(Description = "Gets an item image")]
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
    [Route("/Items/{Id}/Images/{Type}/{Index}/Index", "POST")]
    [Api(Description = "Updates the index for an item image")]
    public class UpdateItemImageIndex : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid Id { get; set; }

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

    [Route("/Artists/{Name}/Images/{Type}/{Index}/Index", "POST")]
    [Route("/Genres/{Name}/Images/{Type}/{Index}/Index", "POST")]
    [Route("/GameGenres/{Name}/Images/{Type}/{Index}/Index", "POST")]
    [Route("/MusicGenres/{Name}/Images/{Type}/{Index}/Index", "POST")]
    [Route("/Persons/{Name}/Images/{Type}/{Index}/Index", "POST")]
    [Route("/Studios/{Name}/Images/{Type}/{Index}/Index", "POST")]
    [Route("/Years/{Year}/Images/{Type}/{Index}/Index", "POST")]
    [Api(Description = "Updates the index for an item image")]
    public class UpdateItemByNameImageIndex : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Name", Description = "Item name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Name { get; set; }

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
    [Api(Description = "Gets an item by name image")]
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
    [Api(Description = "Gets a user image")]
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
    [Api(Description = "Deletes an item image")]
    public class DeleteItemImage : DeleteImageRequest, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public Guid Id { get; set; }
    }

    [Route("/Artists/{Name}/Images/{Type}", "DELETE")]
    [Route("/Artists/{Name}/Images/{Type}/{Index}", "DELETE")]
    [Route("/Genres/{Name}/Images/{Type}", "DELETE")]
    [Route("/Genres/{Name}/Images/{Type}/{Index}", "DELETE")]
    [Route("/GameGenres/{Name}/Images/{Type}", "DELETE")]
    [Route("/GameGenres/{Name}/Images/{Type}/{Index}", "DELETE")]
    [Route("/MusicGenres/{Name}/Images/{Type}", "DELETE")]
    [Route("/MusicGenres/{Name}/Images/{Type}/{Index}", "DELETE")]
    [Route("/Persons/{Name}/Images/{Type}", "DELETE")]
    [Route("/Persons/{Name}/Images/{Type}/{Index}", "DELETE")]
    [Route("/Studios/{Name}/Images/{Type}", "DELETE")]
    [Route("/Studios/{Name}/Images/{Type}/{Index}", "DELETE")]
    [Route("/Years/{Year}/Images/{Type}", "DELETE")]
    [Route("/Years/{Year}/Images/{Type}/{Index}", "DELETE")]
    [Api(Description = "Deletes an item image")]
    public class DeleteItemByNameImage : DeleteImageRequest, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Name", Description = "Item name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Class DeleteUserImage
    /// </summary>
    [Route("/Users/{Id}/Images/{Type}", "DELETE")]
    [Route("/Users/{Id}/Images/{Type}/{Index}", "DELETE")]
    [Api(Description = "Deletes a user image")]
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
    [Api(Description = "Posts a user image")]
    public class PostUserImage : DeleteImageRequest, IRequiresRequestStream, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid Id { get; set; }

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

    [Route("/Artists/{Name}/Images/{Type}", "POST")]
    [Route("/Artists/{Name}/Images/{Type}/{Index}", "POST")]
    [Route("/Genres/{Name}/Images/{Type}", "POST")]
    [Route("/Genres/{Name}/Images/{Type}/{Index}", "POST")]
    [Route("/GameGenres/{Name}/Images/{Type}", "POST")]
    [Route("/GameGenres/{Name}/Images/{Type}/{Index}", "POST")]
    [Route("/MusicGenres/{Name}/Images/{Type}", "POST")]
    [Route("/MusicGenres/{Name}/Images/{Type}/{Index}", "POST")]
    [Route("/Persons/{Name}/Images/{Type}", "POST")]
    [Route("/Persons/{Name}/Images/{Type}/{Index}", "POST")]
    [Route("/Studios/{Name}/Images/{Type}", "POST")]
    [Route("/Studios/{Name}/Images/{Type}/{Index}", "POST")]
    [Route("/Years/{Year}/Images/{Type}", "POST")]
    [Route("/Years/{Year}/Images/{Type}/{Index}", "POST")]
    [Api(Description = "Posts an item image")]
    public class PostItemByNameImage : DeleteImageRequest, IRequiresRequestStream, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Name", Description = "Item name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Name { get; set; }

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
        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        private readonly IApplicationPaths _appPaths;

        private readonly IProviderManager _providerManager;

        private readonly IItemRepository _itemRepo;
        private readonly IDtoService _dtoService;
        private readonly IImageProcessor _imageProcessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageService" /> class.
        /// </summary>
        public ImageService(IUserManager userManager, ILibraryManager libraryManager, IApplicationPaths appPaths, IProviderManager providerManager, IItemRepository itemRepo, IDtoService dtoService, IImageProcessor imageProcessor)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _appPaths = appPaths;
            _providerManager = providerManager;
            _itemRepo = itemRepo;
            _dtoService = dtoService;
            _imageProcessor = imageProcessor;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetItemImageInfos request)
        {
            var item = _dtoService.GetItemByDtoId(request.Id);

            var result = GetItemImageInfos(item);

            return ToOptimizedResult(result);
        }

        public object Get(GetItemByNameImageInfos request)
        {
            var result = GetItemByNameImageInfos(request);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the item by name image infos.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{List{ImageInfo}}.</returns>
        private List<ImageInfo> GetItemByNameImageInfos(GetItemByNameImageInfos request)
        {
            var pathInfo = PathInfo.Parse(RequestContext.PathInfo);
            var type = pathInfo.GetArgumentValue<string>(0);

            var item = GetItemByName(request.Name, type, _libraryManager);

            return GetItemImageInfos(item);
        }

        /// <summary>
        /// Gets the item image infos.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Task{List{ImageInfo}}.</returns>
        public List<ImageInfo> GetItemImageInfos(BaseItem item)
        {
            var list = new List<ImageInfo>();

            foreach (var image in item.Images)
            {
                var path = image.Value;

                var fileInfo = new FileInfo(path);

                var size = _imageProcessor.GetImageSize(path);

                list.Add(new ImageInfo
                {
                    Path = path,
                    ImageType = image.Key,
                    ImageTag = _imageProcessor.GetImageCacheTag(item, image.Key, path),
                    Size = fileInfo.Length,
                    Width = Convert.ToInt32(size.Width),
                    Height = Convert.ToInt32(size.Height)
                });
            }

            var index = 0;

            foreach (var image in item.BackdropImagePaths)
            {
                var fileInfo = new FileInfo(image);

                var size = _imageProcessor.GetImageSize(image);

                list.Add(new ImageInfo
                {
                    Path = image,
                    ImageIndex = index,
                    ImageType = ImageType.Backdrop,
                    ImageTag = _imageProcessor.GetImageCacheTag(item, ImageType.Backdrop, image),
                    Size = fileInfo.Length,
                    Width = Convert.ToInt32(size.Width),
                    Height = Convert.ToInt32(size.Height)
                });

                index++;
            }

            index = 0;

            foreach (var image in item.ScreenshotImagePaths)
            {
                var fileInfo = new FileInfo(image);

                var size = _imageProcessor.GetImageSize(image);

                list.Add(new ImageInfo
                {
                    Path = image,
                    ImageIndex = index,
                    ImageType = ImageType.Screenshot,
                    ImageTag = _imageProcessor.GetImageCacheTag(item, ImageType.Screenshot, image),
                    Size = fileInfo.Length,
                    Width = Convert.ToInt32(size.Width),
                    Height = Convert.ToInt32(size.Height)
                });

                index++;
            }

            var video = item as Video;

            if (video != null)
            {
                index = 0;

                foreach (var chapter in _itemRepo.GetChapters(video.Id))
                {
                    if (!string.IsNullOrEmpty(chapter.ImagePath))
                    {
                        var image = chapter.ImagePath;

                        var fileInfo = new FileInfo(image);

                        var size = _imageProcessor.GetImageSize(image);

                        list.Add(new ImageInfo
                        {
                            Path = image,
                            ImageIndex = index,
                            ImageType = ImageType.Chapter,
                            ImageTag = _imageProcessor.GetImageCacheTag(item, ImageType.Chapter, image),
                            Size = fileInfo.Length,
                            Width = Convert.ToInt32(size.Width),
                            Height = Convert.ToInt32(size.Height)
                        });
                    }

                    index++;
                }
            }

            return list;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetItemImage request)
        {
            var item = string.IsNullOrEmpty(request.Id) ? _libraryManager.RootFolder : _dtoService.GetItemByDtoId(request.Id);

            return GetImage(request, item);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetUserImage request)
        {
            var item = _userManager.Users.First(i => i.Id == request.Id);

            return GetImage(request, item);
        }

        public object Get(GetItemByNameImage request)
        {
            var pathInfo = PathInfo.Parse(RequestContext.PathInfo);
            var type = pathInfo.GetArgumentValue<string>(0);

            var item = GetItemByName(request.Name, type, _libraryManager);

            return GetImage(request, item);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(PostUserImage request)
        {
            var pathInfo = PathInfo.Parse(RequestContext.PathInfo);
            var id = new Guid(pathInfo.GetArgumentValue<string>(1));

            request.Type = (ImageType)Enum.Parse(typeof(ImageType), pathInfo.GetArgumentValue<string>(3), true);

            var item = _userManager.Users.First(i => i.Id == id);

            var task = PostImage(item, request.RequestStream, request.Type, RequestContext.ContentType);

            Task.WaitAll(task);
        }

        public void Post(PostItemByNameImage request)
        {
            var pathInfo = PathInfo.Parse(RequestContext.PathInfo);
            var type = pathInfo.GetArgumentValue<string>(0);
            var name = pathInfo.GetArgumentValue<string>(1);

            request.Type = (ImageType)Enum.Parse(typeof(ImageType), pathInfo.GetArgumentValue<string>(3), true);

            var item = GetItemByName(name, type, _libraryManager);

            var task = PostImage(item, request.RequestStream, request.Type, RequestContext.ContentType);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(PostItemImage request)
        {
            var pathInfo = PathInfo.Parse(RequestContext.PathInfo);
            var id = new Guid(pathInfo.GetArgumentValue<string>(1));

            request.Type = (ImageType)Enum.Parse(typeof(ImageType), pathInfo.GetArgumentValue<string>(3), true);

            var item = _libraryManager.GetItemById(id);

            var task = PostImage(item, request.RequestStream, request.Type, RequestContext.ContentType);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(DeleteUserImage request)
        {
            var item = _userManager.Users.First(i => i.Id == request.Id);

            var task = item.DeleteImage(request.Type, request.Index);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(DeleteItemImage request)
        {
            var item = _libraryManager.GetItemById(request.Id);

            var task = item.DeleteImage(request.Type, request.Index);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(DeleteItemByNameImage request)
        {
            var pathInfo = PathInfo.Parse(RequestContext.PathInfo);
            var type = pathInfo.GetArgumentValue<string>(0);

            var item = GetItemByName(request.Name, type, _libraryManager);

            var task = item.DeleteImage(request.Type, request.Index);

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
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(UpdateItemByNameImageIndex request)
        {
            var pathInfo = PathInfo.Parse(RequestContext.PathInfo);
            var type = pathInfo.GetArgumentValue<string>(0);

            var item = GetItemByName(request.Name, type, _libraryManager);

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
        /// <exception cref="System.ArgumentException">The change index operation is only applicable to backdrops and screenshots</exception>
        private Task UpdateItemIndex(BaseItem item, ImageType type, int currentIndex, int newIndex)
        {
            string file1;
            string file2;

            if (type == ImageType.Screenshot)
            {
                file1 = item.ScreenshotImagePaths[currentIndex];
                file2 = item.ScreenshotImagePaths[newIndex];
            }
            else if (type == ImageType.Backdrop)
            {
                file1 = item.BackdropImagePaths[currentIndex];
                file2 = item.BackdropImagePaths[newIndex];
            }
            else
            {
                throw new ArgumentException("The change index operation is only applicable to backdrops and screenshots");
            }

            SwapFiles(file1, file2);

            // Directory watchers should repeat this, but do a quick refresh first
            return item.RefreshMetadata(CancellationToken.None, forceSave: true, allowSlowProviders: false);
        }

        /// <summary>
        /// Swaps the files.
        /// </summary>
        /// <param name="file1">The file1.</param>
        /// <param name="file2">The file2.</param>
        private void SwapFiles(string file1, string file2)
        {
            var temp1 = Path.Combine(_appPaths.TempDirectory, Guid.NewGuid() + ".tmp");
            var temp2 = Path.Combine(_appPaths.TempDirectory, Guid.NewGuid() + ".tmp");

            File.Copy(file1, temp1);
            File.Copy(file2, temp2);

            File.Copy(temp1, file2, true);
            File.Copy(temp2, file1, true);
        }

        /// <summary>
        /// Gets the image.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="item">The item.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="ResourceNotFoundException">
        /// </exception>
        private object GetImage(ImageRequest request, BaseItem item)
        {
            var imagePath = GetImagePath(request, item);

            if (string.IsNullOrEmpty(imagePath))
            {
                throw new ResourceNotFoundException(string.Format("{0} does not have an image of type {1}", item.Name, request.Type));
            }

            // See if we can avoid a file system lookup by looking for the file in ResolveArgs
            var originalFileImageDateModified = item.GetImageDateModified(imagePath);

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

            // If the file does not exist GetLastWriteTimeUtc will return jan 1, 1601 as opposed to throwing an exception
            // http://msdn.microsoft.com/en-us/library/system.io.file.getlastwritetimeutc.aspx
            if (originalFileImageDateModified.Year == 1601 && !File.Exists(imagePath))
            {
                throw new ResourceNotFoundException(string.Format("File not found: {0}", imagePath));
            }

            var contentType = MimeTypes.GetMimeType(imagePath);

            var cacheGuid = _imageProcessor.GetImageCacheTag(item, request.Type, imagePath, originalFileImageDateModified, supportedImageEnhancers);

            TimeSpan? cacheDuration = null;

            if (!string.IsNullOrEmpty(request.Tag) && cacheGuid == new Guid(request.Tag))
            {
                cacheDuration = TimeSpan.FromDays(365);
            }

            // Avoid implicitly captured closure
            var currentItem = item;
            var currentRequest = request;

            return ToCachedResult(cacheGuid, originalFileImageDateModified, cacheDuration, () => new ImageWriter
            {
                Item = currentItem,
                Request = currentRequest,
                OriginalImageDateModified = originalFileImageDateModified,
                Enhancers = supportedImageEnhancers,
                OriginalImagePath = imagePath,
                ImageProcessor = _imageProcessor

            }, contentType);
        }

        /// <summary>
        /// Gets the image path.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        private string GetImagePath(ImageRequest request, BaseItem item)
        {
            var index = request.Index ?? 0;

            return item.GetImagePath(request.Type, index);
        }

        /// <summary>
        /// Posts the image.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="inputStream">The input stream.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="mimeType">Type of the MIME.</param>
        /// <returns>Task.</returns>
        private async Task PostImage(BaseItem entity, Stream inputStream, ImageType imageType, string mimeType)
        {
            using (var reader = new StreamReader(inputStream))
            {
                var text = await reader.ReadToEndAsync().ConfigureAwait(false);

                var bytes = Convert.FromBase64String(text);

                // Validate first
                using (var validationStream = new MemoryStream(bytes))
                {
                    using (var image = Image.FromStream(validationStream))
                    {
                        Logger.Info("New image is {0}x{1}", image.Width, image.Height);
                    }
                }

                var memoryStream = new MemoryStream(bytes)
                {
                    Position = 0
                };

                var imageIndex = 0;

                if (imageType == ImageType.Screenshot)
                {
                    imageIndex = entity.ScreenshotImagePaths.Count;
                }
                else if (imageType == ImageType.Backdrop)
                {
                    imageIndex = entity.BackdropImagePaths.Count;
                }

                // Handle image/png; charset=utf-8
                mimeType = mimeType.Split(';').FirstOrDefault();

                await _providerManager.SaveImage(entity, memoryStream, mimeType, imageType, imageIndex, CancellationToken.None).ConfigureAwait(false);

                await entity.RefreshMetadata(CancellationToken.None, forceRefresh: true, forceSave: true, allowSlowProviders: false).ConfigureAwait(false);
            }
        }
    }
}
