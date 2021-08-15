using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Api.Attributes;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Splashscreen controller.
    /// </summary>
    [Route("Splashscreen")]
    public class SplashscreenController : BaseJellyfinApiController
    {
        private readonly IImageEncoder _imageEncoder;
        private readonly IItemRepository _itemRepository;
        private readonly IApplicationPaths _appPaths;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SplashscreenController"/> class.
        /// </summary>
        /// <param name="imageEncoder">Instance of the <see cref="IImageEncoder"/> interface.</param>
        /// <param name="itemRepository">Instance of the <see cref="IItemRepository"/> interface.</param>
        /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        public SplashscreenController(
            IImageEncoder imageEncoder,
            IItemRepository itemRepository,
            IApplicationPaths applicationPaths,
            ILogger<SplashscreenController> logger)
        {
            _imageEncoder = imageEncoder;
            _itemRepository = itemRepository;
            _appPaths = applicationPaths;
            _logger = logger;
        }

        /// <summary>
        /// Generates or gets the splashscreen.
        /// </summary>
        /// <param name="darken">Darken the generated image.</param>
        /// <param name="width">The image width.</param>
        /// <param name="height">The image height.</param>
        /// <param name="regenerate">Whether to regenerate the image, regardless if one already exists.</param>
        /// <returns>The splashscreen.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesImageFile]
        public ActionResult GetSplashscreen(
            [FromQuery] bool? darken = false,
            [FromQuery] int? width = 1920,
            [FromQuery] int? height = 1080,
            [FromQuery] bool? regenerate = false)
        {
            var outputPath = Path.Combine(_appPaths.DataPath, $"splashscreen-{width}x{height}-{darken}.jpg");

            if (!System.IO.File.Exists(outputPath) || (regenerate ?? false))
            {
                var posters = GetItemsWithImageType(ImageType.Primary).Select(x => x.GetImages(ImageType.Primary).First().Path).ToList();
                var landscape = GetItemsWithImageType(ImageType.Thumb).Select(x => x.GetImages(ImageType.Thumb).First().Path).ToList();
                if (landscape.Count == 0)
                {
                    _logger.LogDebug("No thumb images found. Using backdrops to generate splashscreen.");
                    landscape = GetItemsWithImageType(ImageType.Backdrop).Select(x => x.GetImages(ImageType.Backdrop).First().Path).ToList();
                }

                _imageEncoder.CreateSplashscreen(new SplashscreenOptions(posters, landscape, outputPath, width!.Value, height!.Value, darken!.Value));
            }

            return PhysicalFile(outputPath, MimeTypes.GetMimeType(outputPath));
        }

        private IReadOnlyList<BaseItem> GetItemsWithImageType(ImageType imageType)
        {
            return _itemRepository.GetItemList(new InternalItemsQuery
            {
                CollapseBoxSetItems = false,
                Recursive = true,
                DtoOptions = new DtoOptions(false),
                ImageTypes = new ImageType[] { imageType },
                Limit = 8,
                OrderBy = new ValueTuple<string, SortOrder>[]
                {
                    new ValueTuple<string, SortOrder>(ItemSortBy.Random, SortOrder.Ascending)
                },
                IncludeItemTypes = new string[] { "Movie", "Series" }
            });
        }
    }
}
