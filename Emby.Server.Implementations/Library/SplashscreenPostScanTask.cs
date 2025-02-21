using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library;

/// <summary>
/// The splashscreen post scan task.
/// </summary>
public class SplashscreenPostScanTask : ILibraryPostScanTask
{
    private readonly IItemRepository _itemRepository;
    private readonly IImageEncoder _imageEncoder;
    private readonly ILogger<SplashscreenPostScanTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SplashscreenPostScanTask"/> class.
    /// </summary>
    /// <param name="itemRepository">Instance of the <see cref="IItemRepository"/> interface.</param>
    /// <param name="imageEncoder">Instance of the <see cref="IImageEncoder"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{SplashscreenPostScanTask}"/> interface.</param>
    public SplashscreenPostScanTask(
        IItemRepository itemRepository,
        IImageEncoder imageEncoder,
        ILogger<SplashscreenPostScanTask> logger)
    {
        _itemRepository = itemRepository;
        _imageEncoder = imageEncoder;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var posters = GetItemsWithImageType(ImageType.Primary)
            .Select(x => x.GetImages(ImageType.Primary).FirstOrDefault()?.Path)
            .Where(path => !string.IsNullOrEmpty(path))
            .Select(path => path!)
            .ToList();
        var backdrops = GetItemsWithImageType(ImageType.Thumb)
            .Select(x => x.GetImages(ImageType.Thumb).FirstOrDefault()?.Path)
            .Where(path => !string.IsNullOrEmpty(path))
            .Select(path => path!)
            .ToList();
        if (backdrops.Count == 0)
        {
            // Thumb images fit better because they include the title in the image but are not provided with TMDb.
            // Using backdrops as a fallback to generate an image at all
            _logger.LogDebug("No thumb images found. Using backdrops to generate splashscreen");
            backdrops = GetItemsWithImageType(ImageType.Backdrop)
                .Select(x => x.GetImages(ImageType.Backdrop).FirstOrDefault()?.Path)
                .Where(path => !string.IsNullOrEmpty(path))
                .Select(path => path!)
                .ToList();
        }

        _imageEncoder.CreateSplashscreen(posters, backdrops);
        return Task.CompletedTask;
    }

    private IReadOnlyList<BaseItem> GetItemsWithImageType(ImageType imageType)
    {
        // TODO make included libraries configurable
        return _itemRepository.GetItemList(new InternalItemsQuery
        {
            CollapseBoxSetItems = false,
            Recursive = true,
            DtoOptions = new DtoOptions(false),
            ImageTypes = new[] { imageType },
            Limit = 30,
            // TODO max parental rating configurable
            MaxParentalRating = 10,
            OrderBy = new[]
            {
                (ItemSortBy.Random, SortOrder.Ascending)
            },
            IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series }
        });
    }
}
