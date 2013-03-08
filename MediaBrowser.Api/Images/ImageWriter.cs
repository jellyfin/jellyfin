using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using ServiceStack.Service;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Images
{
    /// <summary>
    /// Class ImageWriter
    /// </summary>
    public class ImageWriter : IStreamWriter
    {
        /// <summary>
        /// Gets or sets the request.
        /// </summary>
        /// <value>The request.</value>
        public ImageRequest Request { get; set; }
        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>The item.</value>
        public BaseItem Item { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [crop white space].
        /// </summary>
        /// <value><c>true</c> if [crop white space]; otherwise, <c>false</c>.</value>
        public bool CropWhiteSpace { get; set; }
        /// <summary>
        /// The original image date modified
        /// </summary>
        public DateTime OriginalImageDateModified;

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
            return Kernel.Instance.ImageManager.ProcessImage(Item, Request.Type, Request.Index ?? 0, CropWhiteSpace,
                                                    OriginalImageDateModified, responseStream, Request.Width, Request.Height, Request.MaxWidth,
                                                    Request.MaxHeight, Request.Quality);
        }
    }
}
