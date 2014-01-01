using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using ServiceStack.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Images
{
    /// <summary>
    /// Class ImageWriter
    /// </summary>
    public class ImageWriter : IStreamWriter, IHasOptions
    {
        public List<IImageEnhancer> Enhancers;

        /// <summary>
        /// Gets or sets the request.
        /// </summary>
        /// <value>The request.</value>
        public ImageRequest Request { get; set; }
        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>The item.</value>
        public IHasImages Item { get; set; }
        /// <summary>
        /// The original image date modified
        /// </summary>
        public DateTime OriginalImageDateModified;

        public string OriginalImagePath;

        public IImageProcessor ImageProcessor { get; set; }

        /// <summary>
        /// The _options
        /// </summary>
        private readonly IDictionary<string, string> _options = new Dictionary<string, string>();
        /// <summary>
        /// Gets the options.
        /// </summary>
        /// <value>The options.</value>
        public IDictionary<string, string> Options
        {
            get { return _options; }
        }

        /// <summary>
        /// Writes to.
        /// </summary>
        /// <param name="responseStream">The response stream.</param>
        public void WriteTo(Stream responseStream)
        {
            var task = WriteToAsync(responseStream);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Writes to async.
        /// </summary>
        /// <param name="responseStream">The response stream.</param>
        /// <returns>Task.</returns>
        private Task WriteToAsync(Stream responseStream)
        {
            var cropwhitespace = Request.Type == ImageType.Logo || Request.Type == ImageType.Art;

            if (Request.CropWhitespace.HasValue)
            {
                cropwhitespace = Request.CropWhitespace.Value;
            }

            var options = new ImageProcessingOptions
            {
                CropWhiteSpace = cropwhitespace,
                Enhancers = Enhancers,
                Height = Request.Height,
                ImageIndex = Request.Index ?? 0,
                ImageType = Request.Type,
                Item = Item,
                MaxHeight = Request.MaxHeight,
                MaxWidth = Request.MaxWidth,
                OriginalImageDateModified = OriginalImageDateModified,
                OriginalImagePath = OriginalImagePath,
                Quality = Request.Quality,
                Width = Request.Width,
                OutputFormat = Request.Format,
                AddPlayedIndicator = Request.AddPlayedIndicator,
                PercentPlayed = Request.PercentPlayed,
                UnplayedCount = Request.UnplayedCount,
                BackgroundColor = Request.BackgroundColor
            };

            return ImageProcessor.ProcessImage(options, responseStream);
        }
    }
}
