using MediaBrowser.Model.Entities;
using ServiceStack;

namespace MediaBrowser.Api.Images
{
    /// <summary>
    /// Class ImageRequest
    /// </summary>
    public class ImageRequest : DeleteImageRequest
    {
        /// <summary>
        /// The max width
        /// </summary>
        [ApiMember(Name = "MaxWidth", Description = "The maximum image width to return.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? MaxWidth { get; set; }

        /// <summary>
        /// The max height
        /// </summary>
        [ApiMember(Name = "MaxHeight", Description = "The maximum image height to return.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? MaxHeight { get; set; }

        /// <summary>
        /// The width
        /// </summary>
        [ApiMember(Name = "Width", Description = "The fixed image width to return.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Width { get; set; }

        /// <summary>
        /// The height
        /// </summary>
        [ApiMember(Name = "Height", Description = "The fixed image height to return.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Height { get; set; }

        /// <summary>
        /// Gets or sets the quality.
        /// </summary>
        /// <value>The quality.</value>
        [ApiMember(Name = "Quality", Description = "Optional quality setting, from 0-100. Defaults to 90 and should suffice in most cases.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Quality { get; set; }

        /// <summary>
        /// Gets or sets the tag.
        /// </summary>
        /// <value>The tag.</value>
        [ApiMember(Name = "Tag", Description = "Optional. Supply the cache tag from the item object to receive strong caching headers.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Tag { get; set; }

        [ApiMember(Name = "CropWhitespace", Description = "Specify if whitespace should be cropped out of the image. True/False. If unspecified, whitespace will be cropped from logos and clear art.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? CropWhitespace { get; set; }

        [ApiMember(Name = "EnableImageEnhancers", Description = "Enable or disable image enhancers such as cover art.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool EnableImageEnhancers { get; set; }

        [ApiMember(Name = "Format", Description = "Determines the output foramt of the image - original,gif,jpg,png", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public string Format { get; set; }

        [ApiMember(Name = "AddPlayedIndicator", Description = "Optional. Add a played indicator", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool AddPlayedIndicator { get; set; }

        [ApiMember(Name = "PercentPlayed", Description = "Optional percent to render for the percent played overlay", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public double? PercentPlayed { get; set; }

        [ApiMember(Name = "UnplayedCount", Description = "Optional unplayed count overlay to render", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? UnplayedCount { get; set; }

        [ApiMember(Name = "BackgroundColor", Description = "Optional. Apply a background color for transparent images.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string BackgroundColor { get; set; }

        [ApiMember(Name = "ForegroundLayer", Description = "Optional. Apply a foreground layer on top of the image.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ForegroundLayer { get; set; }

        public ImageRequest()
        {
            EnableImageEnhancers = true;
        }
    }

    /// <summary>
    /// Class DeleteImageRequest
    /// </summary>
    public class DeleteImageRequest
    {
        /// <summary>
        /// Gets or sets the type of the image.
        /// </summary>
        /// <value>The type of the image.</value>
        [ApiMember(Name = "Type", Description = "Image Type", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "Type", Description = "Image Type", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        [ApiMember(Name = "Type", Description = "Image Type", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public ImageType Type { get; set; }

        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        /// <value>The index.</value>
        [ApiMember(Name = "Index", Description = "Image Index", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        [ApiMember(Name = "Index", Description = "Image Index", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "POST")]
        [ApiMember(Name = "Index", Description = "Image Index", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "DELETE")]
        public int? Index { get; set; }
    }
}
