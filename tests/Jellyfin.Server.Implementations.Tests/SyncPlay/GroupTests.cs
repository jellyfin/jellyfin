using System;
using System.Collections.Generic;
using System.Reflection;
using Emby.Server.Implementations.SyncPlay;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SyncPlay;

public class GroupTests
{
    public GroupTests()
    {
        var mockLogger = new Mock<ILogger<Emby.Server.Implementations.SyncPlay.Group>>();
        MockLoggerFactory = new Mock<ILoggerFactory>();
        MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);

        MockUserManager = new Mock<IUserManager>();
        MockSessionManager = new Mock<ISessionManager>();
        MockLibraryManager = new Mock<ILibraryManager>();
        MockItem = new Mock<BaseItem>();
        MockItem.Setup(i => i.IsVisibleStandalone(It.IsAny<User>())).Returns(true);
    }

    private Mock<ILoggerFactory> MockLoggerFactory { get; }

    private Mock<IUserManager> MockUserManager { get; }

    private Mock<ISessionManager> MockSessionManager { get; }

    private Mock<ILibraryManager> MockLibraryManager { get; }

    private Mock<BaseItem> MockItem { get; }

    [Fact]
    public void HasAccessToPlayQueue_ReturnsTrue_WhenItemsAreVisible()
    {
        MockLibraryManager.Setup(m => m.GetItemById(It.IsAny<Guid>())).Returns(MockItem.Object);

        var group = new Emby.Server.Implementations.SyncPlay.Group(MockLoggerFactory.Object, MockUserManager.Object, MockSessionManager.Object, MockLibraryManager.Object);
        var itemId = Guid.NewGuid();
        var playlist = new List<Guid> { itemId };
        group.PlayQueue.Reset();
        group.PlayQueue.SetPlaylist(playlist);

        Assert.Single(group.PlayQueue.GetPlaylist());
        Assert.Equal(itemId, group.PlayQueue.GetPlaylist()[0].ItemId);

        var user = new User("test-user", "auth-provider", "pwdreset-provider");
        var result = group.HasAccessToPlayQueue(user);

        Assert.True(result);
    }

    [Fact]
    public void HasAccessToPlayQueue_ReturnsFalse_WhenLibraryReturnsNullForItem()
    {
        MockLibraryManager.Setup(m => m.GetItemById(It.IsAny<Guid>())).Returns((BaseItem?)null);

        Assert.Null(MockLibraryManager.Object.GetItemById(Guid.NewGuid()));

        var group = new Emby.Server.Implementations.SyncPlay.Group(MockLoggerFactory.Object, MockUserManager.Object, MockSessionManager.Object, MockLibraryManager.Object);
        var itemId = Guid.NewGuid();
        var playlist = new List<Guid> { itemId };
        group.PlayQueue.Reset();
        group.PlayQueue.SetPlaylist(playlist);

        Assert.Single(group.PlayQueue.GetPlaylist());
        Assert.Equal(itemId, group.PlayQueue.GetPlaylist()[0].ItemId);

        var user = new User("test-user", "auth-provider", "pwdreset-provider");
        var result = group.HasAccessToPlayQueue(user);

        Assert.False(result);
    }

    [Fact]
    public void UpdatePing_UsesRawValue_ForFirstSample()
    {
        var group = new Emby.Server.Implementations.SyncPlay.Group(MockLoggerFactory.Object, MockUserManager.Object, MockSessionManager.Object, MockLibraryManager.Object);
        var session = CreateSession();
        AddParticipant(group, session);

        group.UpdatePing(session, 100);

        Assert.Equal(100, group.GetHighestPing());
    }

    [Fact]
    public void UpdatePing_SmoothsIncreasingPing_WithFasterAttack()
    {
        var group = new Emby.Server.Implementations.SyncPlay.Group(MockLoggerFactory.Object, MockUserManager.Object, MockSessionManager.Object, MockLibraryManager.Object);
        var session = CreateSession();
        AddParticipant(group, session);

        group.UpdatePing(session, 100);
        group.UpdatePing(session, 500);

        // Rising ping uses the 0.5 "attack" factor: 0.5 * 500 + 0.5 * 100 = 300, not the raw 500.
        Assert.Equal(300, group.GetHighestPing());
    }

    [Fact]
    public void UpdatePing_SmoothsDecreasingPing_WithSlowerDecay()
    {
        var group = new Emby.Server.Implementations.SyncPlay.Group(MockLoggerFactory.Object, MockUserManager.Object, MockSessionManager.Object, MockLibraryManager.Object);
        var session = CreateSession();
        AddParticipant(group, session);

        group.UpdatePing(session, 500);
        group.UpdatePing(session, 100);

        // Falling ping uses the 0.2 "decay" factor: 0.2 * 100 + 0.8 * 500 = 420, not the raw 100.
        Assert.Equal(420, group.GetHighestPing());
    }

    [Fact]
    public void UpdatePing_UsesRawValue_WhenPriorSampleIsStale()
    {
        var group = new Emby.Server.Implementations.SyncPlay.Group(MockLoggerFactory.Object, MockUserManager.Object, MockSessionManager.Object, MockLibraryManager.Object);
        var session = CreateSession();
        AddParticipant(group, session);

        group.UpdatePing(session, 2000);
        var member = GetParticipant(group, session.Id);
        member.LastPingUpdate = DateTime.UtcNow.AddSeconds(-91);

        group.UpdatePing(session, 10);

        // A fresh sample after a stale gap should replace the old value outright, not decay
        // slowly toward it (0.2 * 10 + 0.8 * 2000 = 1602) - nothing was trusting that stale
        // 2000ms reading anymore, so there is nothing meaningful to smooth against.
        Assert.Equal(10, group.GetHighestPing());
    }

    [Fact]
    public void GetHighestPing_IgnoresStalePing_AndFallsBackToDefault()
    {
        var group = new Emby.Server.Implementations.SyncPlay.Group(MockLoggerFactory.Object, MockUserManager.Object, MockSessionManager.Object, MockLibraryManager.Object);
        var session = CreateSession();
        AddParticipant(group, session);

        group.UpdatePing(session, 5000);
        Assert.Equal(5000, group.GetHighestPing());

        var member = GetParticipant(group, session.Id);
        member.LastPingUpdate = DateTime.UtcNow.AddSeconds(-91);

        Assert.Equal(group.DefaultPing, group.GetHighestPing());
    }

    [Fact]
    public void GetHighestPing_UsesReportedPing_WhenRecent()
    {
        var group = new Emby.Server.Implementations.SyncPlay.Group(MockLoggerFactory.Object, MockUserManager.Object, MockSessionManager.Object, MockLibraryManager.Object);
        var session = CreateSession();
        AddParticipant(group, session);

        group.UpdatePing(session, 5000);
        var member = GetParticipant(group, session.Id);
        member.LastPingUpdate = DateTime.UtcNow.AddSeconds(-89);

        Assert.Equal(5000, group.GetHighestPing());
    }

    private SessionInfo CreateSession()
    {
        return new SessionInfo(MockSessionManager.Object, Mock.Of<ILogger>())
        {
            Id = Guid.NewGuid().ToString(),
            UserId = Guid.NewGuid(),
            UserName = "test-user"
        };
    }

    private static void AddParticipant(Emby.Server.Implementations.SyncPlay.Group group, SessionInfo session)
    {
        var method = typeof(Emby.Server.Implementations.SyncPlay.Group).GetMethod("AddSession", BindingFlags.NonPublic | BindingFlags.Instance);
        method!.Invoke(group, [session]);
    }

    private static GroupMember GetParticipant(Emby.Server.Implementations.SyncPlay.Group group, string sessionId)
    {
        var field = typeof(Emby.Server.Implementations.SyncPlay.Group).GetField("_participants", BindingFlags.NonPublic | BindingFlags.Instance);
        var dict = (Dictionary<string, GroupMember>)field!.GetValue(group)!;
        return dict[sessionId];
    }
}
