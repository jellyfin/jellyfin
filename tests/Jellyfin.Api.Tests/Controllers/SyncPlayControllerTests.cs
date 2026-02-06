using System;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using Jellyfin.Api.Results;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay;
using MediaBrowser.Model.SyncPlay;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class SyncPlayControllerTests
{
    [Fact]
    public async Task SyncPlayGetJoinedGroupStateV2_WhenStateExists_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var sessionManagerMock = new Mock<ISessionManager>();
        var syncPlayManagerMock = new Mock<ISyncPlayManager>();
        var userManagerMock = new Mock<IUserManager>();
        var session = CreateSession(sessionManagerMock.Object, userId, "session-joined");
        var state = CreateGroupState(groupId, 7);

        SetupRequestSession(sessionManagerMock, userManagerMock, session, userId);
        syncPlayManagerMock
            .Setup(x => x.GetJoinedGroupStateV2(session))
            .Returns(state);

        var subject = CreateSubject(sessionManagerMock, syncPlayManagerMock, userManagerMock, userId);

        var result = await subject.SyncPlayGetJoinedGroupStateV2();

        var okResult = Assert.IsType<OkResult<SyncPlayGroupStateV2Dto>>(result.Result);
        var payload = Assert.IsType<SyncPlayGroupStateV2Dto>(okResult.Value);
        Assert.Equal(groupId, payload.GroupId);
        Assert.Equal(7, payload.Revision);
    }

    [Fact]
    public async Task SyncPlayGetJoinedGroupStateV2_WhenStateMissing_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var sessionManagerMock = new Mock<ISessionManager>();
        var syncPlayManagerMock = new Mock<ISyncPlayManager>();
        var userManagerMock = new Mock<IUserManager>();
        var session = CreateSession(sessionManagerMock.Object, userId, "session-notfound");

        SetupRequestSession(sessionManagerMock, userManagerMock, session, userId);
        syncPlayManagerMock
            .Setup(x => x.GetJoinedGroupStateV2(session))
            .Returns((SyncPlayGroupStateV2Dto)null!);

        var subject = CreateSubject(sessionManagerMock, syncPlayManagerMock, userManagerMock, userId);

        var result = await subject.SyncPlayGetJoinedGroupStateV2();

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task SyncPlayGetGroupStateV2_WhenStateExists_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var sessionManagerMock = new Mock<ISessionManager>();
        var syncPlayManagerMock = new Mock<ISyncPlayManager>();
        var userManagerMock = new Mock<IUserManager>();
        var session = CreateSession(sessionManagerMock.Object, userId, "session-group");
        var state = CreateGroupState(groupId, 12);

        SetupRequestSession(sessionManagerMock, userManagerMock, session, userId);
        syncPlayManagerMock
            .Setup(x => x.GetGroupStateV2(session, groupId))
            .Returns(state);

        var subject = CreateSubject(sessionManagerMock, syncPlayManagerMock, userManagerMock, userId);

        var result = await subject.SyncPlayGetGroupStateV2(groupId);

        var okResult = Assert.IsType<OkResult<SyncPlayGroupStateV2Dto>>(result.Result);
        var payload = Assert.IsType<SyncPlayGroupStateV2Dto>(okResult.Value);
        Assert.Equal(groupId, payload.GroupId);
        Assert.Equal(12, payload.Revision);
    }

    [Fact]
    public async Task SyncPlayGetGroupStateV2_WhenStateMissing_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var sessionManagerMock = new Mock<ISessionManager>();
        var syncPlayManagerMock = new Mock<ISyncPlayManager>();
        var userManagerMock = new Mock<IUserManager>();
        var session = CreateSession(sessionManagerMock.Object, userId, "session-group-notfound");

        SetupRequestSession(sessionManagerMock, userManagerMock, session, userId);
        syncPlayManagerMock
            .Setup(x => x.GetGroupStateV2(session, groupId))
            .Returns((SyncPlayGroupStateV2Dto)null!);

        var subject = CreateSubject(sessionManagerMock, syncPlayManagerMock, userManagerMock, userId);

        var result = await subject.SyncPlayGetGroupStateV2(groupId);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    private static SyncPlayController CreateSubject(
        Mock<ISessionManager> sessionManagerMock,
        Mock<ISyncPlayManager> syncPlayManagerMock,
        Mock<IUserManager> userManagerMock,
        Guid userId)
    {
        var httpContext = BuildHttpContext(userId);
        return new SyncPlayController(sessionManagerMock.Object, syncPlayManagerMock.Object, userManagerMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    private static void SetupRequestSession(
        Mock<ISessionManager> sessionManagerMock,
        Mock<IUserManager> userManagerMock,
        SessionInfo session,
        Guid userId)
    {
        userManagerMock
            .Setup(x => x.GetUserById(userId))
            .Returns(new User("jellyfin", "default", "default"));

        sessionManagerMock
            .Setup(x => x.LogSessionActivity(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<User>()))
            .ReturnsAsync(session);
    }

    private static SessionInfo CreateSession(ISessionManager sessionManager, Guid userId, string sessionId)
    {
        return new SessionInfo(sessionManager, NullLogger<SessionInfo>.Instance)
        {
            Id = sessionId,
            UserId = userId,
            UserName = "jellyfin",
            Client = "web",
            DeviceId = "device-id",
            DeviceName = "device-name"
        };
    }

    private static DefaultHttpContext BuildHttpContext(Guid userId)
    {
        var claims = new[]
        {
            new Claim(InternalClaimTypes.UserId, userId.ToString("N")),
            new Claim(InternalClaimTypes.Client, "web"),
            new Claim(InternalClaimTypes.Version, "10.12.0"),
            new Claim(InternalClaimTypes.DeviceId, "device-id"),
            new Claim(InternalClaimTypes.Device, "device-name")
        };

        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        return context;
    }

    private static SyncPlayGroupStateV2Dto CreateGroupState(Guid groupId, long revision)
    {
        var groupInfo = new GroupInfoDto(groupId, "Test Group", GroupStateType.Idle, ["jellyfin"], DateTime.UtcNow);
        var queueUpdate = new PlayQueueUpdate(
            PlayQueueUpdateReason.NewPlaylist,
            DateTime.UtcNow,
            Array.Empty<SyncPlayQueueItem>(),
            -1,
            0,
            false,
            GroupShuffleMode.Sorted,
            GroupRepeatMode.RepeatNone);
        var snapshot = new SyncPlayGroupSnapshotDto(groupInfo, queueUpdate, null, revision, DateTime.UtcNow);
        return new SyncPlayGroupStateV2Dto(groupId, revision, snapshot, DateTime.UtcNow);
    }
}
