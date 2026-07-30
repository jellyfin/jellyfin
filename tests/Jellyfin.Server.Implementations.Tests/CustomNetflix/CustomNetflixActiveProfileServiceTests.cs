using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.CustomNetflix;
using MediaBrowser.Controller.CustomNetflix;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public sealed class CustomNetflixActiveProfileServiceTests
{
    [Fact]
    public async Task ActiveProfiles_AreIsolatedByHashedTokenWithoutRedis()
    {
        var userId = Guid.NewGuid();
        var profiles = new[]
        {
            CreateProfile(userId, true),
            CreateProfile(userId, false)
        };
        var selectedProfiles = new Dictionary<string, Guid>();
        var repository = new Mock<ICustomNetflixRepository>();
        repository
            .Setup(mock => mock.GetActiveProfileAsync(userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((Guid _, string tokenHash, CancellationToken _) =>
                Task.FromResult(selectedProfiles.TryGetValue(tokenHash, out var profileId) ? (Guid?)profileId : null));
        repository
            .Setup(mock => mock.SetActiveProfileAsync(userId, It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback((Guid _, string tokenHash, Guid profileId, CancellationToken _) => selectedProfiles[tokenHash] = profileId)
            .Returns(Task.CompletedTask);
        var profileService = new Mock<ICustomNetflixProfileService>();
        profileService
            .Setup(mock => mock.GetProfilesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profiles);
        profileService
            .Setup(mock => mock.GetOwnedProfileAsync(userId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns((Guid _, Guid profileId, CancellationToken _) =>
                Task.FromResult<CustomNetflixProfileDto?>(profiles.FirstOrDefault(profile => profile.Id.Equals(profileId))));
        var service = new CustomNetflixActiveProfileService(
            profileService.Object,
            repository.Object,
            new NullCustomNetflixCacheService());

        await service.SetActiveProfileAsync(userId, "token-a", profiles[0].Id, TestContext.Current.CancellationToken);
        await service.SetActiveProfileAsync(userId, "token-b", profiles[1].Id, TestContext.Current.CancellationToken);
        var tokenASelection = await service.GetActiveProfileAsync(userId, "token-a", TestContext.Current.CancellationToken);
        var tokenBSelection = await service.GetActiveProfileAsync(userId, "token-b", TestContext.Current.CancellationToken);
        var tokenAWriteProfile = await service.GetActiveProfileForWriteAsync(
            userId,
            "token-a",
            profiles[0].Id,
            TestContext.Current.CancellationToken);
        var mismatchedWriteProfile = await service.GetActiveProfileForWriteAsync(
            userId,
            "token-a",
            profiles[1].Id,
            TestContext.Current.CancellationToken);
        var missingTokenWriteProfile = await service.GetActiveProfileForWriteAsync(
            userId,
            null,
            profiles[0].Id,
            TestContext.Current.CancellationToken);

        Assert.Equal(profiles[0].Id, tokenASelection.ProfileId);
        Assert.Equal(profiles[1].Id, tokenBSelection.ProfileId);
        Assert.Same(profiles[0], tokenAWriteProfile);
        Assert.Null(mismatchedWriteProfile);
        Assert.Null(missingTokenWriteProfile);
        Assert.Equal(2, selectedProfiles.Count);
        Assert.DoesNotContain("token-a", selectedProfiles.Keys);
        Assert.DoesNotContain("token-b", selectedProfiles.Keys);
    }

    private static CustomNetflixProfileDto CreateProfile(Guid userId, bool isDefault)
        => new()
        {
            Id = Guid.NewGuid(),
            JellyfinUserId = userId,
            Name = "Profile",
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
}
