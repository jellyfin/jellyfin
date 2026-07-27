using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Validators;

/// <summary>
/// Ensures top-level library folders have a primary poster after scans.
/// Poster extraction is attempted before library scanning. When a library is
/// empty at that point, no poster can be extracted. This post-scan task reruns
/// metadata extraction for top-level folders that are still missing images.
/// </summary>
public class CollectionPosterVerifyPostScanTask : ILibraryPostScanTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<CollectionPosterVerifyPostScanTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionPosterVerifyPostScanTask" /> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="logger">The logger.</param>
    public CollectionPosterVerifyPostScanTask(
        ILibraryManager libraryManager,
        ILogger<CollectionPosterVerifyPostScanTask> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <summary>
    /// Runs the specified progress.
    /// </summary>
    /// <param name="progress">The progress.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task.</returns>
    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var libraries = _libraryManager.GetUserRootFolder().Children.OfType<CollectionFolder>().ToList();
        var totalLibraries = libraries.Count;
        var processedLibraries = 0;

        foreach (var library in libraries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!library.HasImage(ImageType.Primary))
            {
                _logger.LogDebug("Library {LibraryName} is missing a primary image. Refreshing metadata.", library.Name);
                await library.RefreshMetadata(cancellationToken).ConfigureAwait(false);
            }

            processedLibraries++;
            progress.Report((double)processedLibraries / totalLibraries * 100);
        }

        progress.Report(100);
    }
}
