using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaSegments;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

[Collection("LibraryManagerTests")]
public class AudioShortcutTests
{
    [Theory]
    [InlineData("https://example.test/audio.flac", true)]
    [InlineData("/etc/passwd", false)]
    [InlineData("file:///etc/passwd", false)]
    public void GetMediaSources_ResolvesRemoteAudioShortcutsButRejectsLocalTargets(string target, bool remote)
    {
        var previousSources = BaseItem.MediaSourceManager;
        var previousSegments = BaseItem.MediaSegmentManager;
        var sources = new Mock<IMediaSourceManager>();
        sources.Setup(value => value.GetPathProtocol(It.IsAny<string>()))
            .Returns((string path) => path.StartsWith("https://", StringComparison.Ordinal) ? MediaProtocol.Http : MediaProtocol.File);
        sources.Setup(value => value.GetMediaStreams(It.IsAny<Guid>())).Returns(Array.Empty<MediaStream>());
        sources.Setup(value => value.GetMediaAttachments(It.IsAny<Guid>())).Returns(Array.Empty<MediaAttachment>());
        try
        {
            BaseItem.MediaSourceManager = sources.Object;
            BaseItem.MediaSegmentManager = Mock.Of<IMediaSegmentManager>();
            var item = new Audio { Id = Guid.NewGuid(), Path = "/music/track.strm", Container = "flac", IsShortcut = true, ShortcutPath = target };

            var source = Assert.Single(item.GetMediaSources(false));

            Assert.Equal(remote ? target : item.Path, source.Path);
            Assert.Equal(remote ? MediaProtocol.Http : MediaProtocol.File, source.Protocol);
            Assert.Equal(remote, source.IsRemote);
        }
        finally
        {
            BaseItem.MediaSourceManager = previousSources;
            BaseItem.MediaSegmentManager = previousSegments;
        }
    }
}
