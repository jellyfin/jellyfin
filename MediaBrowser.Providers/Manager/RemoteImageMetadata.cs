using System;
using System.Linq;
using System.Net.Http;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Manager
{
    /// <summary>
    /// Records where a downloaded image came from, so later refreshes can detect content changes
    /// with a conditional request instead of re-downloading.
    /// </summary>
    internal static class RemoteImageMetadata
    {
        /// <summary>
        /// Stores the source URL and its HTTP cache validators on the given image.
        /// </summary>
        /// <param name="image">The image that was just saved, if any.</param>
        /// <param name="url">The URL the image was downloaded from.</param>
        /// <param name="etag">The ETag reported by the source, if any.</param>
        /// <param name="lastModified">The Last-Modified value reported by the source, if any.</param>
        internal static void Record(ItemImageInfo? image, string url, string? etag, DateTime? lastModified)
        {
            if (image is null)
            {
                return;
            }

            image.Source = url;
            image.ETag = etag;
            image.SourceLastModified = lastModified;
        }

        /// <summary>
        /// Stores the source URL and the cache validators taken from the download response.
        /// </summary>
        /// <param name="image">The image that was just saved, if any.</param>
        /// <param name="url">The URL the image was downloaded from.</param>
        /// <param name="response">The response the image was read from.</param>
        internal static void Record(ItemImageInfo? image, string url, HttpResponseMessage response)
            => Record(image, url, response.Headers.ETag?.ToString(), response.Content.Headers.LastModified?.UtcDateTime);

        /// <summary>
        /// Resolves the image a save just wrote.
        /// </summary>
        /// <remarks>
        /// Mirrors <see cref="ImageSaver"/>: for multi-image types a null index appends, so index 0
        /// would resolve to a different, pre-existing image.
        /// </remarks>
        /// <param name="item">The item that was saved to.</param>
        /// <param name="type">The image type that was saved.</param>
        /// <param name="imageIndex">The index that was passed to the saver, if any.</param>
        /// <returns>The saved image, or <c>null</c> when it cannot be resolved.</returns>
        internal static ItemImageInfo? GetSavedImage(BaseItem item, ImageType type, int? imageIndex)
        {
            if (imageIndex.HasValue)
            {
                return item.GetImageInfo(type, imageIndex.Value);
            }

            return item.AllowsMultipleImages(type)
                ? item.GetImages(type).LastOrDefault()
                : item.GetImageInfo(type, 0);
        }
    }
}
