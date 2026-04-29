using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LibraryTaskScheduler;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

public class FolderTests
{
    /// <summary>
    /// Verifies that a <see cref="DirectoryNotFoundException"/> thrown from
    /// <see cref="Folder.GetNonCachedChildren"/> does not abort the library scan.
    ///
    /// Previously the exception fell through to a catch-all handler that executed
    /// an early <c>return</c>, preventing the orphaned database entry from being
    /// cleaned up and blocking discovery of any new media on disk.
    /// </summary>
    [Fact]
    public async Task ValidateChildren_DirectoryNotFound_CompletesWithoutThrowing()
    {
        // Arrange — wire up the static dependencies that BaseItem uses.
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager
            .Setup(m => m.DeleteItem(It.IsAny<BaseItem>(), It.IsAny<DeleteOptions>(), It.IsAny<BaseItem>(), It.IsAny<bool>()))
            .Verifiable();

        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager
            .Setup(m => m.GetPathProtocol(It.IsAny<string>()))
            .Returns(MediaProtocol.File);

        var directoryService = new Mock<IDirectoryService>();
        directoryService
            .Setup(d => d.IsAccessible(It.IsAny<string>()))
            .Returns(true);

        var scheduler = new Mock<ILimitedConcurrencyLibraryScheduler>();
        scheduler
            .Setup(s => s.Enqueue(It.IsAny<BaseItem[]>(), It.IsAny<Func<BaseItem, IProgress<double>, Task>>(), It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        BaseItem.Logger = NullLogger<BaseItem>.Instance;
        BaseItem.LibraryManager = libraryManager.Object;
        BaseItem.MediaSourceManager = mediaSourceManager.Object;
        Folder.LimitedConcurrencyLibraryScheduler = scheduler.Object;

        var folder = new DirectoryNotFoundFolder
        {
            Path = "/mnt/tv/Deleted Show"
        };

        var refreshOptions = new MetadataRefreshOptions(directoryService.Object);

        // Act — should not throw despite GetNonCachedChildren throwing DirectoryNotFoundException.
        var exception = await Record.ExceptionAsync(
            () => folder.ValidateChildren(new Progress<double>(), refreshOptions, recursive: false, cancellationToken: CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// A <see cref="Folder"/> whose <see cref="Folder.GetNonCachedChildren"/> always throws
    /// <see cref="DirectoryNotFoundException"/>, simulating a folder that has been deleted from
    /// disk while its database entry remains in Jellyfin.
    /// </summary>
    private sealed class DirectoryNotFoundFolder : Folder
    {
        // Return an empty list from Children so GetActualChildrenDictionary() works
        // without needing a real LibraryManager/ItemRepository.
        public override IEnumerable<BaseItem> Children
        {
            get => [];
            set { }
        }

        protected override IEnumerable<BaseItem> GetNonCachedChildren(IDirectoryService directoryService)
            => throw new DirectoryNotFoundException("Directory not found: /mnt/tv/Deleted Show");
    }
}
