using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.ScheduledTasks.Tasks;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Metadata;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Metadata;

/// <inheritdoc />
public sealed class LibraryRefreshManager : ILibraryRefreshManager
{
    private readonly ILogger<LibraryRefreshManager> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly ITaskManager _taskManager;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryRefreshManager"/> class.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="fileSystem">The <see cref="IFileSystem"/>.</param>
    /// <param name="taskManager">The <see cref="ITaskManager"/>.</param>
    /// <param name="libraryManager">The <see cref="ILibraryManager"/>.</param>
    public LibraryRefreshManager(
        ILogger<LibraryRefreshManager> logger,
        IFileSystem fileSystem,
        ITaskManager taskManager,
        ILibraryManager libraryManager)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _taskManager = taskManager;
        _libraryManager = libraryManager;
    }

    /// <inheritdoc />
    public void StartScan()
    {
        // Just run the scheduled task so that the user can see it
        _taskManager.CancelIfRunningAndQueue<RefreshMediaLibraryTask>();
    }

    /// <inheritdoc />
    public async Task ValidateMediaLibrary(IProgress<double> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);

        _logger.LogInformation("Validating media library");
        await ValidateTopLibraryFolders(cancellationToken).ConfigureAwait(false);

        var innerProgress = new ActionableProgress<double>();
        innerProgress.RegisterAction(pct => progress.Report(pct * 0.96));

        // Validate the entire media library
        await _libraryManager.RootFolder
            .ValidateChildren(
                innerProgress,
                new MetadataRefreshOptions(new DirectoryService(_fileSystem)),
                recursive: true,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ValidateTopLibraryFolders(CancellationToken cancellationToken)
    {
        await _libraryManager.RootFolder.RefreshMetadata(cancellationToken).ConfigureAwait(false);

        // Start by just validating the children of the root, but go no further
        await _libraryManager.RootFolder.ValidateChildren(
            new SimpleProgress<double>(),
            new MetadataRefreshOptions(new DirectoryService(_fileSystem)),
            recursive: false,
            cancellationToken).ConfigureAwait(false);

        await _libraryManager.GetUserRootFolder().RefreshMetadata(cancellationToken).ConfigureAwait(false);

        await _libraryManager.GetUserRootFolder().ValidateChildren(
            new SimpleProgress<double>(),
            new MetadataRefreshOptions(new DirectoryService(_fileSystem)),
            recursive: false,
            cancellationToken).ConfigureAwait(false);

        // Quickly scan CollectionFolders for changes
        foreach (var folder in _libraryManager.GetUserRootFolder().Children.OfType<Folder>())
        {
            await folder.RefreshMetadata(cancellationToken).ConfigureAwait(false);
        }
    }
}
