using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Images;

/// <summary>
/// Scheduled task that pre-generates downscaled processing intermediates for all library
/// images. Once an intermediate exists, all thumbnail requests for that image decode from
/// the small intermediate WebP instead of the full-resolution source, dramatically reducing
/// per-request CPU cost — especially beneficial on low-power hardware.
/// </summary>
public class GenerateImageIntermediatesTask : IScheduledTask
{
    private const int QueryPageLimit = 100;

    private static readonly ImageType[] _imageTypes = [ImageType.Primary, ImageType.Backdrop, ImageType.Thumb];

    private readonly ILogger<GenerateImageIntermediatesTask> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly ILocalizationManager _localization;
    private readonly IImageProcessor _imageProcessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateImageIntermediatesTask"/> class.
    /// </summary>
    public GenerateImageIntermediatesTask(
        ILogger<GenerateImageIntermediatesTask> logger,
        ILibraryManager libraryManager,
        ILocalizationManager localization,
        IImageProcessor imageProcessor)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _localization = localization;
        _imageProcessor = imageProcessor;
    }

    /// <inheritdoc />
    public string Name => _localization.GetLocalizedString("TaskGenerateImageIntermediates");

    /// <inheritdoc />
    public string Description => _localization.GetLocalizedString("TaskGenerateImageIntermediatesDescription");

    /// <inheritdoc />
    public string Key => "GenerateImageIntermediates";

    /// <inheritdoc />
    public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => [];

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var query = new InternalItemsQuery
        {
            SourceTypes = [SourceType.Library],
            IsVirtualItem = false,
            IsFolder = false,
            Recursive = true,
            Limit = QueryPageLimit
        };

        var totalItems = _libraryManager.GetCount(query);
        var startIndex = 0;
        var numComplete = 0;

        while (startIndex < totalItems)
        {
            query.StartIndex = startIndex;
            var items = _libraryManager.GetItemList(query);

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var imageType in _imageTypes)
                {
                    foreach (var image in item.GetImages(imageType))
                    {
                        if (!image.IsLocalFile)
                        {
                            continue;
                        }

                        try
                        {
                            await _imageProcessor.GenerateIntermediateAsync(image.Path, image.DateModified, cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to generate processing intermediate for {Path}", image.Path);
                        }
                    }
                }

                numComplete++;
                progress.Report(100d * numComplete / totalItems);
            }

            startIndex += QueryPageLimit;
        }

        progress.Report(100);
    }
}
