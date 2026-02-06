using System;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.SyncPlay;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay.PlaybackRequests;
using MediaBrowser.Controller.SyncPlay.Requests;
using MediaBrowser.Model.SyncPlay;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SyncPlay;

public class GroupV2StateTests
{
    [Fact]
    public void GetStateV2_RevisionMatchesGroupAndSnapshot()
    {
        var sessionManagerMock = CreateSessionManagerMock();
        var group = CreateGroup(sessionManagerMock.Object);
        var session = CreateSession(sessionManagerMock.Object, Guid.NewGuid(), "session-revision");

        group.CreateGroup(session, new NewGroupRequest("Group"), CancellationToken.None);
        var state = group.GetStateV2();

        Assert.True(state.Revision > 0);
        Assert.Equal(group.Revision, state.Revision);
        Assert.Equal(group.Revision, state.Snapshot.Revision);
        Assert.Equal(group.GroupId, state.GroupId);
    }

    [Fact]
    public void SetIgnoreGroupWait_IncrementsRevisionOnlyOnValueChange()
    {
        var sessionManagerMock = CreateSessionManagerMock();
        var group = CreateGroup(sessionManagerMock.Object);
        var session = CreateSession(sessionManagerMock.Object, Guid.NewGuid(), "session-ignore");
        group.CreateGroup(session, new NewGroupRequest("Group"), CancellationToken.None);

        var initialRevision = group.Revision;

        group.SetIgnoreGroupWait(session, false);
        Assert.Equal(initialRevision, group.Revision);

        group.SetIgnoreGroupWait(session, true);
        Assert.Equal(initialRevision + 1, group.Revision);

        group.SetIgnoreGroupWait(session, true);
        Assert.Equal(initialRevision + 1, group.Revision);
    }

    [Fact]
    public void SetAllBuffering_IncrementsRevisionOnlyOnValueChange()
    {
        var sessionManagerMock = CreateSessionManagerMock();
        var group = CreateGroup(sessionManagerMock.Object);
        var session = CreateSession(sessionManagerMock.Object, Guid.NewGuid(), "session-buffer");
        group.CreateGroup(session, new NewGroupRequest("Group"), CancellationToken.None);

        var initialRevision = group.Revision;

        group.SetAllBuffering(false);
        Assert.Equal(initialRevision, group.Revision);

        group.SetAllBuffering(true);
        Assert.Equal(initialRevision + 1, group.Revision);

        group.SetAllBuffering(true);
        Assert.Equal(initialRevision + 1, group.Revision);
    }

    [Fact]
    public void SessionJoin_SendsSnapshotToAllParticipants()
    {
        var sessionManagerMock = CreateSessionManagerMock();
        var group = CreateGroup(sessionManagerMock.Object);
        var userOne = Guid.NewGuid();
        var userTwo = Guid.NewGuid();
        var sessionOne = CreateSession(sessionManagerMock.Object, userOne, "session-one");
        var sessionTwo = CreateSession(sessionManagerMock.Object, userTwo, "session-two");

        group.CreateGroup(sessionOne, new NewGroupRequest("Group"), CancellationToken.None);
        sessionManagerMock.Invocations.Clear();

        group.SessionJoin(sessionTwo, new JoinGroupRequest(group.GroupId), CancellationToken.None);

        sessionManagerMock.Verify(
            x => x.SendSyncPlayGroupUpdate(
                sessionOne.Id,
                It.Is<GroupUpdate<SyncPlayGroupSnapshotDto>>(update => update.Type == GroupUpdateType.GroupSnapshot),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        sessionManagerMock.Verify(
            x => x.SendSyncPlayGroupUpdate(
                sessionTwo.Id,
                It.Is<GroupUpdate<SyncPlayGroupSnapshotDto>>(update => update.Type == GroupUpdateType.GroupSnapshot),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void SessionLeave_SendsSnapshotToRemainingParticipantsOnly()
    {
        var sessionManagerMock = CreateSessionManagerMock();
        var group = CreateGroup(sessionManagerMock.Object);
        var userOne = Guid.NewGuid();
        var userTwo = Guid.NewGuid();
        var sessionOne = CreateSession(sessionManagerMock.Object, userOne, "session-leave-one");
        var sessionTwo = CreateSession(sessionManagerMock.Object, userTwo, "session-leave-two");

        group.CreateGroup(sessionOne, new NewGroupRequest("Group"), CancellationToken.None);
        group.SessionJoin(sessionTwo, new JoinGroupRequest(group.GroupId), CancellationToken.None);
        sessionManagerMock.Invocations.Clear();

        group.SessionLeave(sessionTwo, new LeaveGroupRequest(), CancellationToken.None);

        sessionManagerMock.Verify(
            x => x.SendSyncPlayGroupUpdate(
                sessionOne.Id,
                It.Is<GroupUpdate<SyncPlayGroupSnapshotDto>>(update => update.Type == GroupUpdateType.GroupSnapshot),
                It.IsAny<CancellationToken>()),
            Times.Once);
        sessionManagerMock.Verify(
            x => x.SendSyncPlayGroupUpdate(
                sessionTwo.Id,
                It.Is<GroupUpdate<SyncPlayGroupSnapshotDto>>(update => update.Type == GroupUpdateType.GroupSnapshot),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void HandleRequest_WhenRevisionChanges_SendsSnapshotToGroup()
    {
        var sessionManagerMock = CreateSessionManagerMock();
        var group = CreateGroup(sessionManagerMock.Object);
        var userOne = Guid.NewGuid();
        var userTwo = Guid.NewGuid();
        var sessionOne = CreateSession(sessionManagerMock.Object, userOne, "session-request-one");
        var sessionTwo = CreateSession(sessionManagerMock.Object, userTwo, "session-request-two");

        group.CreateGroup(sessionOne, new NewGroupRequest("Group"), CancellationToken.None);
        group.SessionJoin(sessionTwo, new JoinGroupRequest(group.GroupId), CancellationToken.None);
        sessionManagerMock.Invocations.Clear();

        group.HandleRequest(sessionOne, new IgnoreWaitGroupRequest(true), CancellationToken.None);

        sessionManagerMock.Verify(
            x => x.SendSyncPlayGroupUpdate(
                sessionOne.Id,
                It.Is<GroupUpdate<SyncPlayGroupSnapshotDto>>(update => update.Type == GroupUpdateType.GroupSnapshot),
                It.IsAny<CancellationToken>()),
            Times.Once);
        sessionManagerMock.Verify(
            x => x.SendSyncPlayGroupUpdate(
                sessionTwo.Id,
                It.Is<GroupUpdate<SyncPlayGroupSnapshotDto>>(update => update.Type == GroupUpdateType.GroupSnapshot),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendCommand_EmitsSnapshotAndDoesNotUseLegacyCommandChannel()
    {
        var sessionManagerMock = CreateSessionManagerMock();
        var group = CreateGroup(sessionManagerMock.Object);
        var session = CreateSession(sessionManagerMock.Object, Guid.NewGuid(), "session-command");

        group.CreateGroup(session, new NewGroupRequest("Group"), CancellationToken.None);
        sessionManagerMock.Invocations.Clear();

        var command = group.NewSyncPlayCommand(SendCommandType.Pause);
        await group.SendCommand(session, SyncPlayBroadcastType.CurrentSession, command, CancellationToken.None);

        sessionManagerMock.Verify(
            x => x.SendSyncPlayGroupUpdate(
                session.Id,
                It.Is<GroupUpdate<SyncPlayGroupSnapshotDto>>(update =>
                    update.Type == GroupUpdateType.GroupSnapshot
                    && update.Data.PlayingCommand != null
                    && update.Data.PlayingCommand.Command == SendCommandType.Pause),
                It.IsAny<CancellationToken>()),
            Times.Once);
        sessionManagerMock.Verify(
            x => x.SendSyncPlayCommand(
                It.IsAny<string>(),
                It.IsAny<SendCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendGroupUpdate_PlayQueueUpdate_EmitsSnapshotInsteadOfLegacyPayload()
    {
        var sessionManagerMock = CreateSessionManagerMock();
        var group = CreateGroup(sessionManagerMock.Object);
        var session = CreateSession(sessionManagerMock.Object, Guid.NewGuid(), "session-queue");

        group.CreateGroup(session, new NewGroupRequest("Group"), CancellationToken.None);
        sessionManagerMock.Invocations.Clear();

        var playQueueUpdate = group.GetPlayQueueUpdate(PlayQueueUpdateReason.NewPlaylist);
        var legacyUpdate = new SyncPlayPlayQueueUpdate(group.GroupId, playQueueUpdate);
        await group.SendGroupUpdate(session, SyncPlayBroadcastType.CurrentSession, legacyUpdate, CancellationToken.None);

        sessionManagerMock.Verify(
            x => x.SendSyncPlayGroupUpdate(
                session.Id,
                It.Is<GroupUpdate<SyncPlayGroupSnapshotDto>>(update => update.Type == GroupUpdateType.GroupSnapshot),
                It.IsAny<CancellationToken>()),
            Times.Once);
        sessionManagerMock.Verify(
            x => x.SendSyncPlayGroupUpdate(
                It.IsAny<string>(),
                It.IsAny<GroupUpdate<PlayQueueUpdate>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Group CreateGroup(ISessionManager sessionManager)
    {
        return new Group(
            NullLoggerFactory.Instance,
            Mock.Of<IUserManager>(),
            sessionManager,
            Mock.Of<ILibraryManager>());
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

    private static Mock<ISessionManager> CreateSessionManagerMock()
    {
        var sessionManagerMock = new Mock<ISessionManager>();
        sessionManagerMock
            .Setup(x => x.SendSyncPlayGroupUpdate(
                It.IsAny<string>(),
                It.IsAny<MediaBrowser.Model.SyncPlay.GroupUpdate<MediaBrowser.Model.SyncPlay.GroupInfoDto>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sessionManagerMock
            .Setup(x => x.SendSyncPlayGroupUpdate(
                It.IsAny<string>(),
                It.IsAny<MediaBrowser.Model.SyncPlay.GroupUpdate<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sessionManagerMock
            .Setup(x => x.SendSyncPlayGroupUpdate(
                It.IsAny<string>(),
                It.IsAny<MediaBrowser.Model.SyncPlay.GroupUpdate<MediaBrowser.Model.SyncPlay.SyncPlayGroupSnapshotDto>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sessionManagerMock
            .Setup(x => x.SendSyncPlayGroupUpdate(
                It.IsAny<string>(),
                It.IsAny<MediaBrowser.Model.SyncPlay.GroupUpdate<MediaBrowser.Model.SyncPlay.PlayQueueUpdate>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sessionManagerMock
            .Setup(x => x.SendSyncPlayGroupUpdate(
                It.IsAny<string>(),
                It.IsAny<MediaBrowser.Model.SyncPlay.GroupUpdate<MediaBrowser.Model.SyncPlay.GroupStateUpdate>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sessionManagerMock
            .Setup(x => x.SendSyncPlayCommand(
                It.IsAny<string>(),
                It.IsAny<MediaBrowser.Model.SyncPlay.SendCommand>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return sessionManagerMock;
    }
}
