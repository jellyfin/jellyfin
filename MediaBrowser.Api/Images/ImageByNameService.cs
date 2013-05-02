using MediaBrowser.Controller;
using ServiceStack.ServiceHost;
using System.IO;

namespace MediaBrowser.Api.Images
{
    /// <summary>
    /// Class GetGeneralImage
    /// </summary>
    [Route("/Images/General/{Name}", "GET")]
    [Api(Description = "Gets a general image by name")]
    public class GetGeneralImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The name of the image", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Class GetRatingImage
    /// </summary>
    [Route("/Images/{Theme}/Ratings/{Name}", "GET")]
    [Api(Description = "Gets a rating image by name")]
    public class GetRatingImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The name of the image", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the theme.
        /// </summary>
        /// <value>The theme.</value>
        [ApiMember(Name = "Theme", Description = "The theme to get the image from", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Theme { get; set; }
    }

    /// <summary>
    /// Class GetMediaInfoImage
    /// </summary>
    [Route("/Images/{Theme}/MediaInfo/{Name}", "GET")]
    [Api(Description = "Gets a media info image by name")]
    public class GetMediaInfoImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The name of the image", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the theme.
        /// </summary>
        /// <value>The theme.</value>
        [ApiMember(Name = "Theme", Description = "The theme to get the image from", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Theme { get; set; }
    }

    /// <summary>
    /// Class ImageByNameService
    /// </summary>
    public class ImageByNameService : BaseApiService
    {
        /// <summary>
        /// The _app paths
        /// </summary>
        private readonly IServerApplicationPaths _appPaths;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageByNameService" /> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        public ImageByNameService(IServerApplicationPaths appPaths)
        {
            _appPaths = appPaths;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetGeneralImage request)
        {
            var file = Path.Combine(_appPaths.GeneralPath, request.Name, "folder.jpg");

            return ToStaticFileResult(File.Exists(file) ? file : Path.ChangeExtension(file, ".png"));
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetRatingImage request)
        {
            var file = Path.Combine(_appPaths.GeneralPath, request.Theme);
            
            return GetImageByName(_appPaths.RatingsPath, request.Name);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetMediaInfoImage request)
        {
            return GetImageByName(_appPaths.MediaInfoImagesPath, request.Name);
        }

        /// <summary>
        /// Gets the name of the image by.
        /// </summary>
        /// <param name="directory">The directory.</param>
        /// <param name="name">The name.</param>
        /// <returns>System.Object.</returns>
        private object GetImageByName(string directory, string name)
        {
            var file = Path.Combine(directory, name, "folder.jpg");

            return ToStaticFileResult(File.Exists(file) ? file : Path.ChangeExtension(file, ".png"));
        }
    }
}
