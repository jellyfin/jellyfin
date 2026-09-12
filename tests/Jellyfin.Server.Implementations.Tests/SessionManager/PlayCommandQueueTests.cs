using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SessionManager;

public class PlayCommandQueueTests : IDisposable
{
    private readonly ILibraryManager? _previousLibraryManager;

    public PlayCommandQueueTests()
    {
        _previousLibraryManager = BaseItem.LibraryManager;
    }

    /// <summary>
    /// A music genre tags its artists as well as their songs, and a by-name artist row is not a
    /// folder, so the queue query cannot exclude it. Such an item has no media sources, and a
    /// client that reaches it in the queue gets an error instead of the next track.
    /// </summary>
    [Fact]
    public async Task SendPlayCommand_GenreTaggingAnArtist_QueuesOnlyPlayableItems()
    {
        var genre = new MusicGenre { Id = Guid.NewGuid(), Name = "Reggaeton" };
        var song = new Audio { Id = Guid.NewGuid(), Name = "Me Porto Bonito" };
        var artist = new MusicArtist { Id = Guid.NewGuid(), Name = "NATTI NATASHA" };

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(i => i.GetItemById(genre.Id)).Returns(genre);
        libraryManager
            .Setup(i => i.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns(new List<BaseItem> { artist, song });
        BaseItem.LibraryManager = libraryManager.Object;

        await using var sessionManager = new Emby.Server.Implementations.Session.SessionManager(
            NullLogger<Emby.Server.Implementations.Session.SessionManager>.Instance,
            Mock.Of<IEventManager>(),
            Mock.Of<IUserDataManager>(),
            Mock.Of<IServerConfigurationManager>(),
            libraryManager.Object,
            Mock.Of<IUserManager>(),
            Mock.Of<IMusicManager>(),
            Mock.Of<IDtoService>(),
            Mock.Of<IImageProcessor>(),
            Mock.Of<IServerApplicationHost>(),
            Mock.Of<IDeviceManager>(),
            Mock.Of<IMediaSourceManager>(),
            Mock.Of<IHostApplicationLifetime>(),
            Mock.Of<IPlaybackHistoryManager>());

        var session = await sessionManager.LogSessionActivity("app_name", "0.0.0", "device_id", "device_name", "127.0.0.1", null);

        var command = new PlayRequest
        {
            ItemIds = new[] { genre.Id },
            PlayCommand = PlayCommand.PlayNow
        };

        await sessionManager.SendPlayCommand(null, session.Id, command, CancellationToken.None);

        Assert.Equal(new[] { song.Id }, command.ItemIds);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            BaseItem.LibraryManager = _previousLibraryManager!;
        }
    }
}
