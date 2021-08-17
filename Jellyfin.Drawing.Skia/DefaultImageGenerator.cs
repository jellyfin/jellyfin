using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Drawing.Skia
{
    /// <summary>
    /// The default image generator.
    /// </summary>
    public class DefaultImageGenerator : IImageGenerator
    {
        private readonly IImageEncoder _imageEncoder;
        private readonly IItemRepository _itemRepository;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultImageGenerator"/> class.
        /// </summary>
        /// <param name="imageEncoder">Instance of the <see cref="IImageEncoder"/> interface.</param>
        /// <param name="itemRepository">Instance of the <see cref="IItemRepository"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        public DefaultImageGenerator(
            IImageEncoder imageEncoder,
            IItemRepository itemRepository,
            ILogger<DefaultImageGenerator> logger)
        {
            _imageEncoder = imageEncoder;
            _itemRepository = itemRepository;
            _logger = logger;
        }

        /// <inheritdoc/>
        public GeneratedImages[] GetSupportedImages()
        {
            return new[] { GeneratedImages.Splashscreen };
        }

        /// <inheritdoc/>
        public void GenerateSplashscreen(string outputPath)
        {
            var posters = GetItemsWithImageType(ImageType.Primary).Select(x => x.GetImages(ImageType.Primary).First().Path).ToList();
            var landscape = GetItemsWithImageType(ImageType.Thumb).Select(x => x.GetImages(ImageType.Thumb).First().Path).ToList();
            if (landscape.Count == 0)
            {
                // Thumb images fit better because they include the title in the image but are not provided with TMDb.
                // Using backdrops as a fallback to generate an image at all
                _logger.LogDebug("No thumb images found. Using backdrops to generate splashscreen.");
                landscape = GetItemsWithImageType(ImageType.Backdrop).Select(x => x.GetImages(ImageType.Backdrop).First().Path).ToList();
            }

            var splashBuilder = new SplashscreenBuilder((SkiaEncoder)_imageEncoder);
            splashBuilder.GenerateSplash(posters, landscape, outputPath);
        }

        private IReadOnlyList<BaseItem> GetItemsWithImageType(ImageType imageType)
        {
            // todo make included libraries configurable
            return _itemRepository.GetItemList(new InternalItemsQuery
            {
                CollapseBoxSetItems = false,
                Recursive = true,
                DtoOptions = new DtoOptions(false),
                ImageTypes = new ImageType[] { imageType },
                Limit = 30,
                // todo max parental rating configurable
                MaxParentalRating = 10,
                OrderBy = new ValueTuple<string, SortOrder>[]
                {
                    new ValueTuple<string, SortOrder>(ItemSortBy.Random, SortOrder.Ascending)
                },
                IncludeItemTypes = new string[] { "Movie", "Series" }
            });
        }
    }
}
