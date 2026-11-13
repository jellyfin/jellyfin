using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.EntryPoints;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.EntryPoints;

// TranscodeCacheCleaner wipes the transcode folder once at server startup. A filesystem problem on that
// folder (permissions, a locked file, a missing mount) must not stop the server from starting, so any
// exception from the wipe is logged and swallowed. The wipe itself still has to run at startup, before
// anything can open a file there (jellyfin/jellyfin#17593).
public sealed class TranscodeCacheCleanerTests
{
    private readonly Mock<ITranscodeManager> _transcodeManager = new();
    private readonly Mock<ILogger<TranscodeCacheCleaner>> _logger = new();

    [Fact]
    public async Task StartAsync_WhenWipeThrows_DoesNotThrowAndLogsError()
    {
        _transcodeManager.Setup(m => m.DeleteEncodedMediaCache()).Throws(new IOException("boom"));
        var cleaner = new TranscodeCacheCleaner(_transcodeManager.Object, _logger.Object);

        await cleaner.StartAsync(CancellationToken.None);

        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.Is<Exception?>(e => e is IOException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenWipeSucceeds_WipesOnceAndLogsNoError()
    {
        var cleaner = new TranscodeCacheCleaner(_transcodeManager.Object, _logger.Object);

        await cleaner.StartAsync(CancellationToken.None);

        _transcodeManager.Verify(m => m.DeleteEncodedMediaCache(), Times.Once);
        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
