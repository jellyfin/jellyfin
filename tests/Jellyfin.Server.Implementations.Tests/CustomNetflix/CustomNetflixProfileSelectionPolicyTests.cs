using System;
using System.Collections.Generic;
using Jellyfin.Server.Implementations.CustomNetflix;
using MediaBrowser.Controller.CustomNetflix;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixProfileSelectionPolicyTests
{
    [Fact]
    public void SelectProfile_ReturnsPreferredProfileWhenAvailable()
    {
        var preferred = CreateProfile(isDefault: false);
        var profiles = new[] { CreateProfile(isDefault: true), preferred };

        var selected = CustomNetflixProfileSelectionPolicy.SelectProfile(profiles, preferred.Id);

        Assert.Same(preferred, selected);
    }

    [Fact]
    public void SelectProfile_FallsBackToDefaultWhenPreferredIsMissing()
    {
        var defaultProfile = CreateProfile(isDefault: true);
        var profiles = new[] { CreateProfile(isDefault: false), defaultProfile };

        var selected = CustomNetflixProfileSelectionPolicy.SelectProfile(profiles, Guid.NewGuid());

        Assert.Same(defaultProfile, selected);
    }

    [Fact]
    public void SelectProfile_FallsBackToFirstProfileWhenNoDefaultExists()
    {
        var first = CreateProfile(isDefault: false);
        var profiles = new[] { first, CreateProfile(isDefault: false) };

        var selected = CustomNetflixProfileSelectionPolicy.SelectProfile(profiles, null);

        Assert.Same(first, selected);
    }

    [Fact]
    public void SelectProfile_ReturnsNullForEmptyProfileList()
    {
        var selected = CustomNetflixProfileSelectionPolicy.SelectProfile(Array.Empty<CustomNetflixProfileDto>(), null);

        Assert.Null(selected);
    }

    private static CustomNetflixProfileDto CreateProfile(bool isDefault)
        => new()
        {
            Id = Guid.NewGuid(),
            JellyfinUserId = Guid.NewGuid(),
            Name = "Profile",
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
}
