using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Validators;

/// <summary>
/// Refreshes AudioBook folder metadata after all children have been scanned.
/// On initial library scan, AudioBook folders are refreshed before their child
/// tracks are probed, so metadata propagation from children is empty.
/// This post-scan task triggers a second refresh to populate the parent.
/// </summary>
public class AudioBookPostScanTask : ILibraryPostScanTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<AudioBookPostScanTask> _logger;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioBookPostScanTask"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="fileSystem">The file system.</param>
    public AudioBookPostScanTask(
        ILibraryManager libraryManager,
        ILogger<AudioBookPostScanTask> logger,
        IFileSystem fileSystem)
    {
        _libraryManager = libraryManager;
        _logger = logger;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var audioBooks = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.AudioBook],
            Recursive = true,
            DtoOptions = new DtoOptions(true)
        });

        if (audioBooks.Count == 0)
        {
            progress.Report(100);
            return;
        }

        _logger.LogInformation("Refreshing metadata for {Count} audiobooks", audioBooks.Count);

        var refreshOptions = new MetadataRefreshOptions(new DirectoryService(_fileSystem))
        {
            MetadataRefreshMode = MetadataRefreshMode.Default,
            ForceSave = true
        };

        for (var i = 0; i < audioBooks.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await audioBooks[i].RefreshMetadata(refreshOptions, cancellationToken).ConfigureAwait(false);

            progress.Report((double)(i + 1) / audioBooks.Count * 100);
        }
    }
}
