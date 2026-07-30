using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.CustomNetflix;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.CustomNetflix.Tests;

public sealed class CustomNetflixProfileServiceTests
{
    [Fact]
    public async Task CreateProfile_UsesConfiguredTransactionalLimit()
    {
        var user = new User("profile-owner", "auth", "reset");
        var repository = new Mock<ICustomNetflixRepository>();
        repository
            .Setup(mock => mock.CreateProfileAsync(
                user.Id,
                "Second",
                null,
                false,
                false,
                7,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProfileRow(user.Id, "Second"));
        var userManager = new Mock<IUserManager>();
        userManager.Setup(mock => mock.GetUserById(user.Id)).Returns(user);
        var service = new CustomNetflixProfileService(
            repository.Object,
            userManager.Object,
            Options.Create(new CustomNetflixOptions { MaxProfilesPerAccount = 7 }));

        var profile = await service.CreateProfileAsync(
            user.Id,
            new CustomNetflixCreateProfileRequest { Name = " Second " },
            TestContext.Current.CancellationToken);

        Assert.Equal("Second", profile.Name);
        repository.VerifyAll();
    }

    [Fact]
    public async Task ChildProfiles_AreRejectedBeforePersistence()
    {
        var user = new User("profile-owner", "auth", "reset");
        var profile = CreateProfileRow(user.Id, "Default");
        var repository = new Mock<ICustomNetflixRepository>();
        repository
            .Setup(mock => mock.GetProfileAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        var userManager = new Mock<IUserManager>();
        userManager.Setup(mock => mock.GetUserById(user.Id)).Returns(user);
        var service = new CustomNetflixProfileService(
            repository.Object,
            userManager.Object,
            Options.Create(new CustomNetflixOptions()));

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateProfileAsync(
            user.Id,
            new CustomNetflixCreateProfileRequest { Name = "Child", IsChild = true },
            TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateProfileAsync(
            user.Id,
            profile.Id,
            new CustomNetflixUpdateProfileRequest { IsChild = true },
            TestContext.Current.CancellationToken));

        repository.Verify(
            mock => mock.CreateProfileAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        repository.Verify(
            mock => mock.UpdateProfileAsync(
                It.IsAny<Guid>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<ProfileSettingsRow?>(),
                It.IsAny<PlaybackPreferencesRow?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateProfile_PersistsPlaybackPreferences()
    {
        var userId = Guid.NewGuid();
        var profile = CreateProfileRow(userId, "Default");
        var expectedPreferences = new PlaybackPreferencesRow(
            profile.Id,
            false,
            true,
            false,
            true,
            false,
            20_000_000,
            "fr-FR",
            "en",
            true,
            true,
            false,
            true);
        var repository = new Mock<ICustomNetflixRepository>();
        repository
            .Setup(mock => mock.GetProfileAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        repository
            .Setup(mock => mock.UpdateProfileAsync(
                profile.Id,
                null,
                null,
                null,
                expectedPreferences,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile with { PlaybackPreferences = expectedPreferences });
        var service = new CustomNetflixProfileService(
            repository.Object,
            Mock.Of<IUserManager>(),
            Options.Create(new CustomNetflixOptions()));

        var result = await service.UpdateProfileAsync(
            userId,
            profile.Id,
            new CustomNetflixUpdateProfileRequest
            {
                PlaybackPreferences = new CustomNetflixPlaybackPreferencesDto
                {
                    PreferDirectPlay = false,
                    AllowVideoTranscoding = false,
                    PreferHardwareTranscoding = false,
                    MaxStreamingBitrate = 20_000_000,
                    PreferredAudioLanguage = " fr-FR ",
                    PreferredSubtitleLanguage = "en",
                    SubtitlesEnabled = true,
                    AudioDescriptionEnabled = true,
                    SkipCreditsEnabled = true
                }
            },
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.False(result.PlaybackPreferences.PreferDirectPlay);
        Assert.True(result.PlaybackPreferences.AllowContainerRemuxing);
        Assert.False(result.PlaybackPreferences.AllowVideoTranscoding);
        Assert.True(result.PlaybackPreferences.AllowAudioTranscoding);
        Assert.False(result.PlaybackPreferences.PreferHardwareTranscoding);
        Assert.Equal(20_000_000, result.PlaybackPreferences.MaxStreamingBitrate);
        Assert.Equal("fr-FR", result.PlaybackPreferences.PreferredAudioLanguage);
        Assert.Equal("en", result.PlaybackPreferences.PreferredSubtitleLanguage);
        Assert.True(result.PlaybackPreferences.SubtitlesEnabled);
        Assert.True(result.PlaybackPreferences.AudioDescriptionEnabled);
        Assert.False(result.PlaybackPreferences.ClosedCaptionsEnabled);
        Assert.True(result.PlaybackPreferences.SkipCreditsEnabled);
        repository.VerifyAll();
    }

    [Fact]
    public async Task GetOwnedProfile_CachesOnlyWithinServiceScope()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var row = new ProfileRow(
            profileId,
            userId,
            "Profile",
            null,
            true,
            false,
            DateTime.UtcNow,
            DateTime.UtcNow,
            new ProfileSettingsRow(profileId, true, 5, true, true),
            DefaultPlaybackPreferences(profileId));
        var repository = new Mock<ICustomNetflixRepository>();
        repository
            .Setup(mock => mock.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(row);
        var userManager = new Mock<IUserManager>();
        var options = Options.Create(new CustomNetflixOptions());
        var firstScope = new CustomNetflixProfileService(repository.Object, userManager.Object, options);
        var secondScope = new CustomNetflixProfileService(repository.Object, userManager.Object, options);

        var first = await firstScope.GetOwnedProfileAsync(userId, profileId, TestContext.Current.CancellationToken);
        var repeated = await firstScope.GetOwnedProfileAsync(userId, profileId, TestContext.Current.CancellationToken);
        var nextRequest = await secondScope.GetOwnedProfileAsync(userId, profileId, TestContext.Current.CancellationToken);

        Assert.NotNull(first);
        Assert.Same(first, repeated);
        Assert.NotNull(nextRequest);
        Assert.NotSame(first, nextRequest);
        repository.Verify(
            mock => mock.GetProfileAsync(profileId, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public void MaxProfilesPerAccount_DefaultsToFive()
        => Assert.Equal(5, new CustomNetflixOptions().MaxProfilesPerAccount);

    private static ProfileRow CreateProfileRow(Guid userId, string name)
        => new(
            Guid.NewGuid(),
            userId,
            name,
            null,
            false,
            false,
            DateTime.UtcNow,
            DateTime.UtcNow,
            new ProfileSettingsRow(Guid.NewGuid(), true, 8, true, true),
            DefaultPlaybackPreferences(Guid.NewGuid()));

    private static PlaybackPreferencesRow DefaultPlaybackPreferences(Guid profileId)
        => new(profileId, true, true, true, true, true, null, null, null, false, false, false, false);
}
