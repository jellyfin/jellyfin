using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using ServiceStack.ServiceHost;
using ServiceStack.Text.Controller;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Images
{
    /// <summary>
    /// Class GetItemImage
    /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageService" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        public ImageService(IUserManager userManager, ILibraryManager libraryManager)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
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
            var item = _libraryManager.GetStudio(request.Name).Result;

            return GetImage(request, item);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPersonImage request)
        {
            var item = _libraryManager.GetPerson(request.Name).Result;

            return GetImage(request, item);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetGenreImage request)
        {
            var item = _libraryManager.GetGenre(request.Name).Result;

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
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(DeleteUserImage request)
        {
            var item = _userManager.Users.First(i => i.Id == request.Id);

            var task = item.DeleteImage(request.Type);

            Task.WaitAll(task);
        }
        
        /// <summary>
        /// Gets the image.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="item">The item.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="ResourceNotFoundException"></exception>
        private object GetImage(ImageRequest request, BaseItem item)
        {
            var kernel = Kernel.Instance;

            var index = request.Index ?? 0;

            var imagePath = GetImagePath(kernel, request, item);

            if (string.IsNullOrEmpty(imagePath))
            {
                throw new ResourceNotFoundException();
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
                        filename = "folder";
                        break;
                    default:
                        filename = imageType.ToString().ToLower();
                        break;
                }


                var extension = mimeType.Split(';').First().Split('/').Last();

                var oldImagePath = entity.GetImage(imageType);

                var imagePath = Path.Combine(entity.MetaLocation, filename + "." + extension);

                // Save to file system
                using (var fs = new FileStream(imagePath, FileMode.Create, FileAccess.Write, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, true))
                {
                    await fs.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                }

                // Set the image
                entity.SetImage(imageType, imagePath);

                // If the new and old paths are different, delete the old one
                if (!string.IsNullOrEmpty(oldImagePath) && !oldImagePath.Equals(imagePath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(oldImagePath);
                }

                // Directory watchers should repeat this, but do a quick refresh first
                await entity.RefreshMetadata(CancellationToken.None, forceSave: true, allowSlowProviders: false).ConfigureAwait(false);
            }
        }
    }
}
