using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.CustomNetflix;
using MediaBrowser.Controller.CustomNetflix;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public sealed class CustomNetflixAutoplayServiceTests
{
    [Fact]
    public async Task GetNextEpisode_RequiresConfirmationAfterThirdCompletedAutoplay()
    {
        var user = new User("autoplay", "auth", "reset");
        var profileId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var currentItemId = Guid.NewGuid();
        var nextItemId = Guid.NewGuid();
        var currentEpisode = new Episode
        {
            Id = currentItemId,
            SeriesId = seriesId,
            ParentIndexNumber = 1,
            IndexNumber = 1
        };
        var nextEpisode = new Episode
        {
            Id = nextItemId,
            SeriesId = seriesId,
            ParentIndexNumber = 1,
            IndexNumber = 2
        };
        var profileService = new Mock<ICustomNetflixProfileService>();
        profileService
            .Setup(mock => mock.GetOwnedProfileAsync(
                user.Id,
                profileId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProfile(user.Id, profileId));
        var repository = new Mock<ICustomNetflixRepository>();
        repository
            .Setup(mock => mock.GetProgressAsync(
                profileId,
                nextItemId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WatchProgressRow?)null);
        repository
            .Setup(mock => mock.GetProgressAsync(
                profileId,
                currentItemId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WatchProgressRow(
                profileId,
                currentItemId,
                null,
                1_800,
                1_800,
                100,
                true,
                1,
                DateTime.UtcNow));
        repository
            .Setup(mock => mock.TrackAutoplayAsync(
                profileId,
                currentItemId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AutoplayStateRow(
                profileId,
                3,
                currentItemId,
                true,
                null));
        var userManager = new Mock<IUserManager>();
        userManager.Setup(mock => mock.GetUserById(user.Id)).Returns(user);
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager
            .Setup(mock => mock.GetItemById<Episode>(currentItemId, user))
            .Returns(currentEpisode);
        libraryManager
            .Setup(mock => mock.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns(new BaseItem[] { nextEpisode });
        var service = new CustomNetflixAutoplayService(
            profileService.Object,
            repository.Object,
            userManager.Object,
            libraryManager.Object,
            Mock.Of<IDtoService>());

        var result = await service.GetNextEpisodeAsync(
            user.Id,
            profileId,
            currentItemId,
            TestContext.Current.CancellationToken);

        Assert.False(result.HasNext);
        Assert.Null(result.Item);
        Assert.Equal(0, result.DelaySeconds);
        Assert.True(result.RequiresStillWatchingConfirmation);
        Assert.Equal("still_watching_confirmation_required", result.Reason);
        Assert.Equal(
            "customnetflix.autoplay.reason.still_watching_confirmation_required",
            result.ReasonKey);
        Assert.Equal("customnetflix.autoplay.still_watching", result.TitleKey);
        profileService.VerifyAll();
        repository.VerifyAll();
        userManager.VerifyAll();
        libraryManager.VerifyAll();
    }

    [Fact]
    public async Task ConfirmStillWatching_ResetsOwnedProfileState()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var confirmedAt = DateTime.UtcNow;
        var profileService = new Mock<ICustomNetflixProfileService>();
        profileService
            .Setup(mock => mock.GetOwnedProfileAsync(
                userId,
                profileId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProfile(userId, profileId));
        var repository = new Mock<ICustomNetflixRepository>();
        repository
            .Setup(mock => mock.ConfirmStillWatchingAsync(
                profileId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmedAt);
        var service = new CustomNetflixAutoplayService(
            profileService.Object,
            repository.Object,
            Mock.Of<IUserManager>(),
            Mock.Of<ILibraryManager>(),
            Mock.Of<IDtoService>());

        var result = await service.ConfirmStillWatchingAsync(
            userId,
            profileId,
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(profileId, result.ProfileId);
        Assert.False(result.Required);
        Assert.Equal(confirmedAt, result.ConfirmedAt);
        profileService.VerifyAll();
        repository.VerifyAll();
    }

    [Fact]
    public async Task GetNextEpisode_ReturnsStableKeysWhenProfileIsMissing()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var profileService = new Mock<ICustomNetflixProfileService>();
        profileService
            .Setup(mock => mock.GetOwnedProfileAsync(
                userId,
                profileId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomNetflixProfileDto?)null);
        var service = new CustomNetflixAutoplayService(
            profileService.Object,
            Mock.Of<ICustomNetflixRepository>(),
            Mock.Of<IUserManager>(),
            Mock.Of<ILibraryManager>(),
            Mock.Of<IDtoService>());

        var result = await service.GetNextEpisodeAsync(
            userId,
            profileId,
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);

        Assert.False(result.HasNext);
        Assert.Equal("profile_not_found", result.Reason);
        Assert.Equal("customnetflix.autoplay.reason.profile_not_found", result.ReasonKey);
        Assert.Equal("customnetflix.autoplay.title.profile_not_found", result.TitleKey);
        profileService.VerifyAll();
    }

    private static CustomNetflixProfileDto CreateProfile(Guid userId, Guid profileId)
        => new()
        {
            Id = profileId,
            JellyfinUserId = userId,
            Settings = new CustomNetflixProfileSettingsDto
            {
                AutoplayEnabled = true,
                AutoplayDelaySeconds = 8
            }
        };
}
