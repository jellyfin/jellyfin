using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using ServiceStack.ServiceHost;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Images
{
    /// <summary>
    /// Class GetItemImage
    /// </summary>
    [Route("/Items/{Id}/Images/{Type}", "GET")]
    [Route("/Items/{Id}/Images/{Type}/{Index}", "GET")]
    public class GetItemImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }
    }

    /// <summary>
    /// Class GetPersonImage
    /// </summary>
    [Route("/Persons/{Name}/Images/{Type}", "GET")]
    [Route("/Persons/{Name}/Images/{Type}/{Index}", "GET")]
    public class GetPersonImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
    }

    /// <summary>
    /// Class GetStudioImage
    /// </summary>
    [Route("/Studios/{Name}/Images/{Type}", "GET")]
    [Route("/Studios/{Name}/Images/{Type}/{Index}", "GET")]
    public class GetStudioImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
    }

    /// <summary>
    /// Class GetGenreImage
    /// </summary>
    [Route("/Genres/{Name}/Images/{Type}", "GET")]
    [Route("/Genres/{Name}/Images/{Type}/{Index}", "GET")]
    public class GetGenreImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
    }

    /// <summary>
    /// Class GetYearImage
    /// </summary>
    [Route("/Years/{Year}/Images/{Type}", "GET")]
    [Route("/Years/{Year}/Images/{Type}/{Index}", "GET")]
    public class GetYearImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the year.
        /// </summary>
        /// <value>The year.</value>
        public int Year { get; set; }
    }

    /// <summary>
    /// Class GetUserImage
    /// </summary>
    [Route("/Users/{Id}/Images/{Type}", "GET")]
    [Route("/Users/{Id}/Images/{Type}/{Index}", "GET")]
    public class GetUserImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// Class DeleteUserImage
    /// </summary>
    [Route("/Users/{Id}/Images/{Type}", "DELETE")]
    [Route("/Users/{Id}/Images/{Type}/{Index}", "DELETE")]
    public class DeleteUserImage : DeleteImageRequest
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public Guid Id { get; set; }
    }
    
    /// <summary>
    /// Class ImageService
    /// </summary>
    [Export(typeof(IRestfulService))]
    public class ImageService : BaseRestService
    {
        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetItemImage request)
        {
            var item = DtoBuilder.GetItemByClientId(request.Id);

            return GetImage(request, item);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetUserImage request)
        {
            var kernel = (Kernel)Kernel;

            var item = kernel.Users.First(i => i.Id == request.Id);

            return GetImage(request, item);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetYearImage request)
        {
            var kernel = (Kernel)Kernel;

            var item = kernel.LibraryManager.GetYear(request.Year).Result;

            return GetImage(request, item);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetStudioImage request)
        {
            var kernel = (Kernel)Kernel;

            var item = kernel.LibraryManager.GetStudio(request.Name).Result;

            return GetImage(request, item);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPersonImage request)
        {
            var kernel = (Kernel)Kernel;

            var item = kernel.LibraryManager.GetPerson(request.Name).Result;

            return GetImage(request, item);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetGenreImage request)
        {
            var kernel = (Kernel)Kernel;

            var item = kernel.LibraryManager.GetGenre(request.Name).Result;

            return GetImage(request, item);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(DeleteUserImage request)
        {
            var kernel = (Kernel)Kernel;

            var item = kernel.Users.First(i => i.Id == request.Id);

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
            var kernel = (Kernel)Kernel;

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
                OriginalImageDateModified = originalFileImageDateModified,
                ContentType = contentType

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
    }
}
