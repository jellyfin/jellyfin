using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Entities;

public sealed class UserViewBuilderTests
{
    private static readonly User _user = new("view-filter-test", "provider", "reset");

    [Fact]
    public void Filter_IsPlayed_CountsAMovieWatchedOnAnAlternateVersionAsPlayed()
    {
        // The primary carries no played row of its own; the version that was watched is another file.
        var onlyWatchedOnAlternate = new Movie { Id = Guid.NewGuid(), Name = "Watched as a second cut" };
        var watched = new Movie { Id = Guid.NewGuid(), Name = "Watched outright" };
        var unwatched = new Movie { Id = Guid.NewGuid(), Name = "Not watched" };

        var items = new BaseItem[] { onlyWatchedOnAlternate, watched, unwatched };

        var userDataManager = new Mock<IUserDataManager>();
        userDataManager
            .Setup(m => m.GetUserData(_user, It.IsAny<BaseItem>()))
            .Returns((User _, BaseItem item) => new UserItemData { Key = item.Id.ToString("N"), Played = item.Id.Equals(watched.Id) });
        userDataManager
            .Setup(m => m.GetResumeUserDataBatch(It.IsAny<IReadOnlyList<BaseItem>>(), _user))
            .Returns(new Dictionary<Guid, VersionResumeData>
            {
                [onlyWatchedOnAlternate.Id] = new(Guid.NewGuid(), new UserItemData { Key = "alternate", Played = true })
            });

        var libraryManager = new Mock<ILibraryManager>();

        var played = UserViewBuilder.Filter(
            items,
            _user,
            new InternalItemsQuery(_user) { IsPlayed = true },
            userDataManager.Object,
            libraryManager.Object).ToList();

        var unplayed = UserViewBuilder.Filter(
            items,
            _user,
            new InternalItemsQuery(_user) { IsPlayed = false },
            userDataManager.Object,
            libraryManager.Object).ToList();

        // The alternate's playback settles the movie, exactly as the item's own dto reports it.
        Assert.Equal([onlyWatchedOnAlternate.Id, watched.Id], played.Select(i => i.Id));
        Assert.Equal([unwatched.Id], unplayed.Select(i => i.Id));
    }
}
