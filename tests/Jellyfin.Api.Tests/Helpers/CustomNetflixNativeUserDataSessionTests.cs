using System;
using Jellyfin.Api.Helpers;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers;

public sealed class CustomNetflixNativeUserDataSessionTests
{
    [Fact]
    public void NewResolutionInvalidatesAnOlderConcurrentResolution()
    {
        var session = new SessionInfo(Mock.Of<ISessionManager>(), NullLogger.Instance);
        var firstUser = Guid.NewGuid();
        var secondUser = Guid.NewGuid();
        var firstGeneration = RequestHelpers.BeginCustomNetflixProfileResolution(session);
        var secondGeneration = RequestHelpers.BeginCustomNetflixProfileResolution(session);

        Assert.False(RequestHelpers.TrySetCustomNetflixProfileResolution(
            session,
            firstGeneration,
            firstUser,
            "first-token",
            enabled: true));
        Assert.True(RequestHelpers.TrySetCustomNetflixProfileResolution(
            session,
            secondGeneration,
            secondUser,
            "second-token",
            enabled: false));

        session.SynchronizeCustomNetflixProfile(() =>
        {
            Assert.False(RequestHelpers.IsCustomNetflixProfileResolutionCurrentUnsafe(
                session,
                firstGeneration,
                firstUser,
                "first-token",
                enabled: true));
            Assert.True(RequestHelpers.IsCustomNetflixProfileResolutionCurrentUnsafe(
                session,
                secondGeneration,
                secondUser,
                "second-token",
                enabled: false));
        });
    }
}
