using System;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using MediaBrowser.MediaEncoding.Transcoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.Transcoding;

// TranscodeManager used to wipe its whole transcode folder from its constructor. Because it's a
// lazily-constructed DI singleton, that wipe actually ran on whatever stream was first opened after
// a server restart, not at startup. If that first stream was a Live TV shared stream, the wipe
// deleted the buffer file SharedHttpStream had just opened for writing (jellyfin/jellyfin#17593).
// This pins down that constructing TranscodeManager must never delete files already sitting in the
// transcode folder, the wipe belongs at eager startup, before any stream can write there.
public sealed class TranscodeManagerCacheWipeTests : IDisposable
{
    private readonly string _transcodePath;

    public TranscodeManagerCacheWipeTests()
    {
        _transcodePath = Path.Combine(Path.GetTempPath(), "jellyfin-transcode-wipe-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_transcodePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_transcodePath))
        {
            Directory.Delete(_transcodePath, true);
        }
    }

    [Fact]
    public void Constructor_DoesNotDeleteFilesAlreadyInTranscodePath()
    {
        // Simulates a Live TV buffer file SharedHttpStream already opened and wrote into the
        // transcode folder before anything resolves the ITranscodeManager singleton for the first time.
        var foreignFile = Path.Combine(_transcodePath, "livetv-buffer.ts");
        File.WriteAllText(foreignFile, "already being written by SharedHttpStream");

        var config = new Mock<IServerConfigurationManager>();
        config.Setup(c => c.GetConfiguration("encoding"))
            .Returns(new EncodingOptions { TranscodingTempPath = _transcodePath });
        config.SetupGet(c => c.CommonApplicationPaths).Returns(Mock.Of<IApplicationPaths>());

        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(f => f.GetFilePaths(It.IsAny<string>(), true))
            .Returns<string, bool>((path, _) => Directory.GetFiles(path, "*", SearchOption.AllDirectories));
        fileSystem.Setup(f => f.DeleteFile(It.IsAny<string>()))
            .Callback<string>(File.Delete);

        var encodingHelper = new EncodingHelper(
            Mock.Of<IApplicationPaths>(),
            Mock.Of<IMediaEncoder>(),
            Mock.Of<ISubtitleEncoder>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<MediaBrowser.Common.Configuration.IConfigurationManager>(),
            Mock.Of<IPathManager>());

        _ = new TranscodeManager(
            NullLoggerFactory.Instance,
            fileSystem.Object,
            Mock.Of<IApplicationPaths>(),
            config.Object,
            Mock.Of<IUserManager>(),
            Mock.Of<ISessionManager>(),
            encodingHelper,
            Mock.Of<IMediaEncoder>(),
            Mock.Of<IMediaSourceManager>(),
            Mock.Of<IAttachmentExtractor>());

        Assert.True(File.Exists(foreignFile));
    }
}
