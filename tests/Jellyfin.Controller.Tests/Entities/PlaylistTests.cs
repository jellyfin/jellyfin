using System;
using System.Linq;
using Jellyfin.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Querying;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

public class PlaylistTests
{
    [Fact]
    public void IsVisible_PlaylistWithNothingLeftInIt_IsHidden()
    {
        // The SQL parental filter hides a container whose every member is blocked, so a listing
        // built in memory has to reach the same answer.
        var blocked = new Movie { Id = Guid.NewGuid(), Name = "Movie" };
        SetupLibrary(blocked);

        Assert.False(BuildPlaylist(blocked).IsVisible(BuildRestrictedUser()));
    }

    [Fact]
    public void IsVisible_PlaylistWithOneAllowedItem_StaysVisible()
    {
        var blocked = new Movie { Id = Guid.NewGuid(), Name = "Movie" };
        var allowed = new Audio { Id = Guid.NewGuid(), Name = "Song" };
        SetupLibrary(blocked, allowed);

        Assert.True(BuildPlaylist(blocked, allowed).IsVisible(BuildRestrictedUser()));
    }

    [Fact]
    public void IsVisible_UnrestrictedUser_LeavesTheItemsUnresolved()
    {
        var blocked = new Movie { Id = Guid.NewGuid(), Name = "Movie" };
        var libraryManager = SetupLibrary(blocked);
        var user = new User("user", "auth-provider", "reset-provider");

        Assert.True(BuildPlaylist(blocked).IsVisible(user));

        // Resolving a playlist's items is a query per playlist; nothing may run it for a user no
        // rating keeps anything from.
        libraryManager.Verify(x => x.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Never);
    }

    private static Mock<ILibraryManager> SetupLibrary(params BaseItem[] items)
    {
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager
            .Setup(x => x.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns(items);
        BaseItem.LibraryManager = libraryManager.Object;

        return libraryManager;
    }

    private static Playlist BuildPlaylist(params BaseItem[] items)
    {
        // An empty path keeps the playlist out of the shared-playlist branch.
        return new Playlist
        {
            Id = Guid.NewGuid(),
            Name = "Playlist",
            LinkedChildren = items.Select(LinkedChild.Create).ToArray()
        };
    }

    private static User BuildRestrictedUser()
    {
        var user = new User("user", "auth-provider", "reset-provider") { MaxParentalRatingScore = 5 };
        user.SetPreference(PreferenceKind.BlockUnratedItems, new[] { UnratedItem.Movie });

        return user;
    }
}
