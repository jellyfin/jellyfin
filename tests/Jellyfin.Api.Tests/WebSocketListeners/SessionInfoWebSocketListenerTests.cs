using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Api.WebSocketListeners;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.WebSocketListeners;

public class SessionInfoWebSocketListenerTests
{
    [Fact]
    public async Task GetDataToSendForConnection_IncludesOtherUsersSessionsWhenRemoteControlIsAllowed()
    {
        var requestingUser = CreateUser("controller", canControlOtherUsers: true);
        await using var otherUserSession = CreateSession(Guid.NewGuid(), "other-user-session", supportsRemoteControl: true);
        await using var listener = CreateListener(otherUserSession);

        var sessions = await listener.GetDataForConnection(CreateConnection(requestingUser));

        Assert.Collection(sessions, session => Assert.Equal("other-user-session", session.Id));
    }

    [Fact]
    public async Task GetDataToSendForConnection_ExcludesOtherUsersSessionsWhenRemoteControlIsNotAllowed()
    {
        var requestingUser = CreateUser("viewer", canControlOtherUsers: false);
        await using var otherUserSession = CreateSession(Guid.NewGuid(), "other-user-session", supportsRemoteControl: true);
        await using var listener = CreateListener(otherUserSession);

        var sessions = await listener.GetDataForConnection(CreateConnection(requestingUser));

        Assert.Empty(sessions);
    }

    [Fact]
    public async Task GetDataToSendForConnection_ExcludesOtherUsersSessionsThatCannotBeRemoteControlled()
    {
        var requestingUser = CreateUser("controller", canControlOtherUsers: true);
        await using var otherUserSession = CreateSession(Guid.NewGuid(), "other-user-session");
        await using var listener = CreateListener(otherUserSession);

        var sessions = await listener.GetDataForConnection(CreateConnection(requestingUser));

        Assert.Empty(sessions);
    }

    [Fact]
    public async Task GetDataToSendForConnection_IncludesOwnSessionsWithoutRemoteControlPermission()
    {
        var requestingUser = CreateUser("viewer", canControlOtherUsers: false);
        await using var ownSession = CreateSession(requestingUser.Id, "own-session");
        await using var listener = CreateListener(ownSession);

        var sessions = await listener.GetDataForConnection(CreateConnection(requestingUser));

        Assert.Collection(sessions, session => Assert.Equal("own-session", session.Id));
    }

    private static User CreateUser(string name, bool canControlOtherUsers)
    {
        var user = new User(name, "auth", "reset");
        user.AddDefaultPermissions();
        user.SetPermission(PermissionKind.EnableRemoteControlOfOtherUsers, canControlOtherUsers);
        return user;
    }

    private static SessionInfo CreateSession(Guid userId, string id, bool supportsRemoteControl = false)
    {
        var session = new SessionInfo(Mock.Of<ISessionManager>(), Mock.Of<ILogger>())
        {
            Id = id,
            UserId = userId,
            Capabilities = new ClientCapabilities { SupportsMediaControl = supportsRemoteControl }
        };

        if (supportsRemoteControl)
        {
            session.AddController(Mock.Of<ISessionController>(controller => controller.SupportsMediaControl));
        }

        return session;
    }

    private static TestSessionInfoWebSocketListener CreateListener(params SessionInfo[] sessions)
    {
        var sessionManager = new Mock<ISessionManager>();
        sessionManager.SetupGet(manager => manager.Sessions).Returns(sessions);
        sessionManager.Setup(manager => manager.ToSessionInfoDto(It.IsAny<SessionInfo>()))
            .Returns<SessionInfo>(session => new SessionInfoDto { Id = session.Id });

        return new TestSessionInfoWebSocketListener(
            Mock.Of<ILogger<SessionInfoWebSocketListener>>(),
            sessionManager.Object);
    }

    private static IWebSocketConnection CreateConnection(User user)
    {
        var authorizationInfo = new AuthorizationInfo { User = user };
        var connection = new Mock<IWebSocketConnection>();
        connection.SetupGet(item => item.AuthorizationInfo).Returns(authorizationInfo);
        return connection.Object;
    }

    private sealed class TestSessionInfoWebSocketListener : SessionInfoWebSocketListener
    {
        public TestSessionInfoWebSocketListener(
            ILogger<SessionInfoWebSocketListener> logger,
            ISessionManager sessionManager)
            : base(logger, sessionManager)
        {
        }

        public Task<IEnumerable<SessionInfoDto>> GetDataForConnection(IWebSocketConnection connection)
            => GetDataToSendForConnection(connection);
    }
}
