using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using ServiceStack.ServiceHost;
using ServiceStack.Text.Controller;
using System;
using System.Collections.Generic;
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
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid Id { get; set; }
    }

    /// <summary>
    /// Class GetPersonImage
    /// </summary>
    [Route("/Persons/{Name}/Images/{Type}", "GET")]
    [Route("/Persons/{Name}/Images/{Type}/{Index}", "GET")]
    [Api(Description = "Gets a person image")]
    public class GetPersonImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "Person name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Class GetArtistImage
    /// </summary>
    [Route("/Artists/{Name}/Images/{Type}", "GET")]
    [Route("/Artists/{Name}/Images/{Type}/{Index}", "GET")]
    [Api(Description = "Gets an artist image")]
    public class GetArtistImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "Artist name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Class GetStudioImage
    /// </summary>
    [Route("/Studios/{Name}/Images/{Type}", "GET")]
    [Route("/Studios/{Name}/Images/{Type}/{Index}", "GET")]
    [Api(Description = "Gets a studio image")]
    public class GetStudioImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "Studio name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Class GetGenreImage
    /// </summary>
    [Route("/Genres/{Name}/Images/{Type}", "GET")]
    [Route("/Genres/{Name}/Images/{Type}/{Index}", "GET")]
    [Api(Description = "Gets a genre image")]
    public class GetGenreImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "Genre name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Class GetYearImage
    /// </summary>
    [Route("/Years/{Year}/Images/{Type}", "GET")]
    [Route("/Years/{Year}/Images/{Type}/{Index}", "GET")]
    [Api(Description = "Gets a year image")]
    public class GetYearImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the year.
        /// </summary>
        /// <value>The year.</value>
        [ApiMember(Name = "Year", Description = "Year", IsRequired = true, DataType = "int", ParameterType = "path", Verb = "GET")]
        public int Year { get; set; }
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageService" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="providerManager">The provider manager.</param>
        public ImageService(IUserManager userManager, ILibraryManager libraryManager, IApplicationPaths appPaths, IProviderManager providerManager)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _appPaths = appPaths;
            _providerManager = providerManager;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetItemImageInfos request)
        {
            var item = DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager);

            var result = GetItemImageInfos(item).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the item image infos.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Task{List{ImageInfo}}.</returns>
        public async Task<List<ImageInfo>> GetItemImageInfos(BaseItem item)
        {
            var list = new List<ImageInfo>();

            foreach (var image in item.Images)
            {
                var path = image.Value;

                var fileInfo = new FileInfo(path);

                var dateModified = Kernel.Instance.ImageManager.GetImageDateModified(item, path);

                var size = await Kernel.Instance.ImageManager.GetImageSize(path, dateModified).ConfigureAwait(false);

                list.Add(new ImageInfo
                {
                    Path = path,
                    ImageType = image.Key,
                    ImageTag = Kernel.Instance.ImageManager.GetImageCacheTag(item, image.Key, path),
                    Size = fileInfo.Length,
                    Width = Convert.ToInt32(size.Width),
                    Height = Convert.ToInt32(size.Height)
                });
            }

            var index = 0;

            foreach (var image in item.BackdropImagePaths)
            {
                var fileInfo = new FileInfo(image);

                var dateModified = Kernel.Instance.ImageManager.GetImageDateModified(item, image);

                var size = await Kernel.Instance.ImageManager.GetImageSize(image, dateModified).ConfigureAwait(false);

                list.Add(new ImageInfo
                {
                    Path = image,
                    ImageIndex = index,
                    ImageType = ImageType.Backdrop,
                    ImageTag = Kernel.Instance.ImageManager.GetImageCacheTag(item, ImageType.Backdrop, image),
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

                var dateModified = Kernel.Instance.ImageManager.GetImageDateModified(item, image);

                var size = await Kernel.Instance.ImageManager.GetImageSize(image, dateModified).ConfigureAwait(false);

                list.Add(new ImageInfo
                {
                    Path = image,
                    ImageIndex = index,
                    ImageType = ImageType.Screenshot,
                    ImageTag = Kernel.Instance.ImageManager.GetImageCacheTag(item, ImageType.Screenshot, image),
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

                foreach (var chapter in video.Chapters)
                {
                    if (!string.IsNullOrEmpty(chapter.ImagePath))
                    {
                        var image = chapter.ImagePath;

                        var fileInfo = new FileInfo(image);

                        var dateModified = Kernel.Instance.ImageManager.GetImageDateModified(item, image);

                        var size = await Kernel.Instance.ImageManager.GetImageSize(image, dateModified).ConfigureAwait(false);

                        list.Add(new ImageInfo
                        {
                            Path = image,
                            ImageIndex = index,
                            ImageType = ImageType.Chapter,
                            ImageTag = Kernel.Instance.ImageManager.GetImageCacheTag(item, ImageType.Chapter, image),
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
            var item = string.IsNullOrEmpty(request.Id) ? _libraryManager.RootFolder : DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager);

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

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetYearImage request)
        {
            var item = _libraryManager.GetYear(request.Year).Result;

            return GetImage(request, item);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetStudioImage request)
        {
            var item = GetStudio(request.Name, _libraryManager).Result;

            return GetImage(request, item);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPersonImage request)
        {
            var item = GetPerson(request.Name, _libraryManager).Result;

            return GetImage(request, item);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetArtistImage request)
        {
            var item = GetArtist(request.Name, _libraryManager).Result;

            return GetImage(request, item);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetGenreImage request)
        {
            var item = GetGenre(request.Name, _libraryManager).Result;

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
            var kernel = Kernel.Instance;

            var index = request.Index ?? 0;

            var imagePath = GetImagePath(kernel, request, item);

            if (string.IsNullOrEmpty(imagePath))
            {
                throw new ResourceNotFoundException(string.Format("{0} does not have an image of type {1}", item.Name, request.Type));
            }

            // See if we can avoid a file system lookup by looking for the file in ResolveArgs
            var originalFileImageDateModified = kernel.ImageManager.GetImageDateModified(item, request.Type, index);

            var supportedImageEnhancers = kernel.ImageEnhancers.Where(i => i.Supports(item, request.Type)).ToList();

            // If the file does not exist GetLastWriteTimeUtc will return jan 1, 1601 as opposed to throwing an exception
            // http://msdn.microsoft.com/en-us/library/system.io.file.getlastwritetimeutc.aspx
            if (originalFileImageDateModified.Year == 1601 && !File.Exists(imagePath))
            {
                throw new ResourceNotFoundException(string.Format("File not found: {0}", imagePath));
            }

            var contentType = MimeTypes.GetMimeType(imagePath);
            var dateLastModified = (supportedImageEnhancers.Select(e => e.LastConfigurationChange(item, request.Type)).Concat(new[] { originalFileImageDateModified })).Max();

            var cacheGuid = kernel.ImageManager.GetImageCacheTag(imagePath, originalFileImageDateModified, supportedImageEnhancers, item, request.Type);

            TimeSpan? cacheDuration = null;

            if (!string.IsNullOrEmpty(request.Tag) && cacheGuid == new Guid(request.Tag))
            {
                cacheDuration = TimeSpan.FromDays(365);
            }

            return ToCachedResult(cacheGuid, dateLastModified, cacheDuration, () => new ImageWriter
            {
                Item = item,
                Request = request,
                CropWhiteSpace = request.Type == ImageType.Logo || request.Type == ImageType.Art,
                OriginalImageDateModified = originalFileImageDateModified

            }, contentType);
        }

        /// <summary>
        /// Gets the image path.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        /// <param name="request">The request.</param>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        private string GetImagePath(Kernel kernel, ImageRequest request, BaseItem item)
        {
            var index = request.Index ?? 0;

            return kernel.ImageManager.GetImagePath(item, request.Type, index);
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

                string filename;

                switch (imageType)
                {
                    case ImageType.Art:
                        filename = "clearart";
                        break;
                    case ImageType.Primary:
                        filename = entity is Episode ? Path.GetFileNameWithoutExtension(entity.Path) : "folder";
                        break;
                    case ImageType.Backdrop:
                        filename = GetBackdropFilenameToSave(entity);
                        break;
                    case ImageType.Screenshot:
                        filename = GetScreenshotFilenameToSave(entity);
                        break;
                    default:
                        filename = imageType.ToString().ToLower();
                        break;
                }


                var extension = mimeType.Split(';').First().Split('/').Last();

                string oldImagePath;
                switch (imageType)
                {
                    case ImageType.Backdrop:
                    case ImageType.Screenshot:
                        oldImagePath = null;
                        break;
                    default:
                        oldImagePath = entity.GetImage(imageType);
                        break;
                }

                // Don't save locally if there's no parent (special feature, trailer, etc)
                var saveLocally = (!(entity is Audio) && entity.Parent != null && !string.IsNullOrEmpty(entity.MetaLocation)) || entity is User;

                if (imageType != ImageType.Primary)
                {
                    if (entity is Episode)
                    {
                        saveLocally = false;
                    }
                }

                var imagePath = _providerManager.GetSavePath(entity, filename + "." + extension, saveLocally);

                // Save to file system
                using (var fs = new FileStream(imagePath, FileMode.Create, FileAccess.Write, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, true))
                {
                    await fs.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                }

                if (imageType == ImageType.Screenshot)
                {
                    entity.ScreenshotImagePaths.Add(imagePath);
                }
                else if (imageType == ImageType.Backdrop)
                {
                    entity.BackdropImagePaths.Add(imagePath);
                }
                else
                {
                    // Set the image
                    entity.SetImage(imageType, imagePath);
                }

                // If the new and old paths are different, delete the old one
                if (!string.IsNullOrEmpty(oldImagePath) && !oldImagePath.Equals(imagePath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(oldImagePath);
                }

                // Directory watchers should repeat this, but do a quick refresh first
                await entity.RefreshMetadata(CancellationToken.None, forceSave: true, allowSlowProviders: false).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the backdrop filename to save.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        private string GetBackdropFilenameToSave(BaseItem item)
        {
            var paths = item.BackdropImagePaths.ToList();

            if (!paths.Any(i => string.Equals(Path.GetFileNameWithoutExtension(i), "backdrop", StringComparison.OrdinalIgnoreCase)))
            {
                return "screenshot";
            }

            var index = 1;

            while (paths.Any(i => string.Equals(Path.GetFileNameWithoutExtension(i), "backdrop" + index, StringComparison.OrdinalIgnoreCase)))
            {
                index++;
            }

            return "backdrop" + index;
        }

        /// <summary>
        /// Gets the screenshot filename to save.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        private string GetScreenshotFilenameToSave(BaseItem item)
        {
            var paths = item.ScreenshotImagePaths.ToList();

            if (!paths.Any(i => string.Equals(Path.GetFileNameWithoutExtension(i), "screenshot", StringComparison.OrdinalIgnoreCase)))
            {
                return "screenshot";
            }

            var index = 1;

            while (paths.Any(i => string.Equals(Path.GetFileNameWithoutExtension(i), "screenshot" + index, StringComparison.OrdinalIgnoreCase)))
            {
                index++;
            }

            return "screenshot" + index;
        }
    }
}
