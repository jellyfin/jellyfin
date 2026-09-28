using System;
using System.IO;
using System.Threading.Tasks;
using Emby.Server.Implementations.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.IO;

public class FileRefresherTests
{
    [Fact]
    public async Task ProcessPathChanges_PathLookupThrows_StillRefreshesRemainingPaths()
    {
        var tempDir = Directory.CreateTempSubdirectory("filerefresher");
        try
        {
            // Ordered so the failing path is dequeued first.
            var failingPath = Path.Combine(tempDir.FullName, "failing", "episode.mkv");
            var workingPath = Directory.CreateDirectory(Path.Combine(tempDir.FullName, "working")).FullName;

            var workingItem = new Folder { Path = workingPath, Name = "working" };
            var workingItemFound = new TaskCompletionSource();

            var libraryManager = new Mock<ILibraryManager>(MockBehavior.Loose);
            libraryManager.Setup(x => x.FindByPath(failingPath, null))
                .Throws(new ObjectDisposedException("IServiceProvider"));
            libraryManager.Setup(x => x.FindByPath(workingPath, null))
                .Returns(workingItem)
                .Callback(() => workingItemFound.TrySetResult());

            var configurationManager = new Mock<IServerConfigurationManager>(MockBehavior.Loose);
            configurationManager.Setup(x => x.Configuration)
                .Returns(new ServerConfiguration { LibraryMonitorDelay = 1 });

            using var refresher = new FileRefresher(
                failingPath,
                configurationManager.Object,
                libraryManager.Object,
                NullLogger.Instance);
            refresher.AddPath(workingPath);

            await workingItemFound.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

            libraryManager.Verify(x => x.FindByPath(failingPath, null), Times.Once);
        }
        finally
        {
            tempDir.Delete(true);
        }
    }
}
