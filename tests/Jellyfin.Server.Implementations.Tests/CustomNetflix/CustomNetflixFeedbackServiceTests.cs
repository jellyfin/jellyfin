using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.CustomNetflix;
using MediaBrowser.Controller.CustomNetflix;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixFeedbackServiceTests
{
    [Fact]
    public async Task Set_NormalizesFeedbackAndInvalidatesHomeSnapshots()
    {
        var user = new User("feedback", "auth", "reset");
        var profileId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var updatedAt = DateTime.UtcNow;
        var profileService = new Mock<ICustomNetflixProfileService>();
        profileService
            .Setup(mock => mock.GetOwnedProfileAsync(
                user.Id,
                profileId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomNetflixProfileDto { Id = profileId, JellyfinUserId = user.Id });
        var repository = new Mock<ICustomNetflixRepository>();
        repository
            .Setup(mock => mock.UpsertItemFeedbackAsync(
                profileId,
                itemId,
                CustomNetflixFeedbackPolicy.NotInterested,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemFeedbackRow(
                profileId,
                itemId,
                CustomNetflixFeedbackPolicy.NotInterested,
                updatedAt));
        repository
            .Setup(mock => mock.DeleteHomeSnapshotsAsync(profileId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var cache = new Mock<ICustomNetflixCacheService>();
        cache
            .Setup(mock => mock.RemoveAsync(
                It.Is<IReadOnlyList<string>>(keys => keys.Count == 50),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var userManager = new Mock<IUserManager>();
        userManager.Setup(mock => mock.GetUserById(user.Id)).Returns(user);
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager
            .Setup(mock => mock.GetItemById<BaseItem>(itemId, user))
            .Returns(new Movie { Id = itemId });
        var service = new CustomNetflixFeedbackService(
            profileService.Object,
            repository.Object,
            cache.Object,
            userManager.Object,
            libraryManager.Object);

        var result = await service.SetAsync(
            user.Id,
            profileId,
            itemId,
            new CustomNetflixItemFeedbackRequest { Feedback = "NOT_INTERESTED" },
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(CustomNetflixFeedbackPolicy.NotInterested, result.Feedback);
        Assert.Equal(updatedAt, result.UpdatedAt);
        profileService.VerifyAll();
        repository.VerifyAll();
        cache.VerifyAll();
        userManager.VerifyAll();
        libraryManager.VerifyAll();
    }
}
