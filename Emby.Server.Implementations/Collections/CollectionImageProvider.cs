using System.Collections.Generic;
using System.Linq;
using Emby.Server.Implementations.Images;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Collections
{
    /// <summary>
    /// A collection image provider.
    /// </summary>
    public class CollectionImageProvider : BaseDynamicImageProvider<BoxSet>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionImageProvider"/> class.
        /// </summary>
        /// <param name="fileSystem">The filesystem.</param>
        /// <param name="providerManager">The provider manager.</param>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="imageProcessor">The image processor.</param>
        public CollectionImageProvider(
            IFileSystem fileSystem,
            IProviderManager providerManager,
            IApplicationPaths applicationPaths,
            IImageProcessor imageProcessor)
            : base(fileSystem, providerManager, applicationPaths, imageProcessor)
        {
        }

        /// <inheritdoc />
        protected override bool Supports(BaseItem item)
        {
            // If the collection is locked, always allow dynamic images
            if (item.IsLocked)
            {
                return base.Supports(item);
            }

            // If not locked, only allow dynamic images if there's no local Primary image
            // This allows fallback generation when remote providers don't provide images
            // while still preventing it from running ahead of internet image providers
            var image = item.GetImageInfo(ImageType.Primary, 0);
            if (image is not null)
            {
                // If there's a local Primary image in the metadata folder, don't generate
                if (image.IsLocalFile && FileSystem.ContainsSubPath(item.GetInternalMetadataPath(), image.Path))
                {
                    return false;
                }
            }

            return base.Supports(item);
        }

        /// <inheritdoc />
        protected override IReadOnlyList<BaseItem> GetItemsWithImages(BaseItem item)
        {
            var playlist = (BoxSet)item;

            return playlist.Children.Concat(playlist.GetLinkedChildren())
                .Select(i =>
                {
                    var subItem = i;

                    var episode = subItem as Episode;

                    var series = episode?.Series;
                    if (series is not null && series.HasImage(ImageType.Primary))
                    {
                        return series;
                    }

                    if (subItem.HasImage(ImageType.Primary))
                    {
                        return subItem;
                    }

                    var parent = subItem.GetOwner() ?? subItem.GetParent();

                    if (parent is not null && parent.HasImage(ImageType.Primary))
                    {
                        if (parent is MusicAlbum)
                        {
                            return parent;
                        }
                    }

                    return null;
                })
                .Where(i => i is not null)
                .GroupBy(x => x!.Id) // We removed the null values
                .Select(x => x.First())
                .ToList()!; // Again... the list doesn't contain any null values
        }

        /// <inheritdoc />
        protected override string CreateImage(BaseItem item, IReadOnlyCollection<BaseItem> itemsWithImages, string outputPathWithoutExtension, ImageType imageType, int imageIndex)
        {
            return CreateSingleImage(itemsWithImages, outputPathWithoutExtension, ImageType.Primary);
        }
    }
}
