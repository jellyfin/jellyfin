using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay;
using MediaBrowser.Controller.SyncPlay.GroupStates;
using MediaBrowser.Model.SyncPlay;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SyncPlay
{
    public sealed class WaitingGroupStateTests : IDisposable
    {
        private readonly NullLoggerFactory _loggerFactory = new();

        public void Dispose()
        {
            _loggerFactory.Dispose();
        }

        private Mock<IGroupStateContext> CreateMockContext(
            long positionTicks,
            long runTimeTicks,
            DateTime lastActivity)
        {
            var context = new Mock<IGroupStateContext>();

            context.SetupProperty(c => c.PositionTicks, positionTicks);
            context.SetupProperty(c => c.LastActivity, lastActivity);

            // SanitizePositionTicks clamps to [0, runTimeTicks].
            context.Setup(c => c.SanitizePositionTicks(It.IsAny<long>()))
                .Returns((long ticks) => Math.Clamp(ticks, 0, runTimeTicks));

            // GetPlayQueueUpdate returns a minimal update.
            var playQueueUpdate = new PlayQueueUpdate(
                PlayQueueUpdateReason.NewPlaylist,
                DateTime.UtcNow,
                Array.Empty<SyncPlayQueueItem>(),
                -1,
                0,
                false,
                GroupShuffleMode.Sorted,
                GroupRepeatMode.RepeatNone);
            context.Setup(c => c.GetPlayQueueUpdate(It.IsAny<PlayQueueUpdateReason>()))
                .Returns(playQueueUpdate);

            context.Setup(c => c.GroupId).Returns(Guid.NewGuid());
            context.Setup(c => c.SendGroupUpdate(
                It.IsAny<SessionInfo>(),
                It.IsAny<SyncPlayBroadcastType>(),
                It.IsAny<GroupUpdate<PlayQueueUpdate>>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            context.Setup(c => c.SetBuffering(It.IsAny<SessionInfo>(), It.IsAny<bool>()));
            context.Setup(c => c.NewSyncPlayCommand(It.IsAny<SendCommandType>()))
                .Returns(new SendCommand(
                    Guid.NewGuid(),
                    Guid.Empty,
                    DateTime.UtcNow,
                    SendCommandType.Pause,
                    0,
                    DateTime.UtcNow));
            context.Setup(c => c.SendCommand(
                It.IsAny<SessionInfo>(),
                It.IsAny<SyncPlayBroadcastType>(),
                It.IsAny<SendCommand>(),
                It.IsAny<CancellationToken>()));

            return context;
        }

        private static SessionInfo CreateSession()
        {
            return new SessionInfo(null, new NullLogger<SessionInfo>()) { Id = "test-session" };
        }

        /// <summary>
        /// Regression test: when a session joins a group that was in Playing state,
        /// the position should be clamped to RunTimeTicks to prevent overshoot.
        /// Before the fix, network delay could cause PositionTicks to exceed
        /// the media duration, causing the client to seek past the end of the video.
        /// </summary>
        [Fact]
        public void SessionJoined_FromPlaying_ClampsPositionToRunTimeTicks()
        {
            var state = new WaitingGroupState(_loggerFactory);

            long videoDurationTicks = 600_000_000_0; // 60 seconds
            long initialPositionTicks = 550_000_000_0; // 55 seconds

            // Set LastActivity to 10 seconds ago, simulating network delay.
            // After adding elapsed time, position would be ~65 seconds - past the end.
            var context = CreateMockContext(
                initialPositionTicks,
                videoDurationTicks,
                DateTime.UtcNow.AddSeconds(-10));

            var session = CreateSession();

            // Act: session joins from Playing state.
            state.SessionJoined(context.Object, GroupStateType.Playing, session, CancellationToken.None);

            // Assert: position should be clamped to video duration.
            Assert.True(
                context.Object.PositionTicks <= videoDurationTicks,
                $"PositionTicks ({context.Object.PositionTicks}) should not exceed RunTimeTicks ({videoDurationTicks})");

            // Verify SanitizePositionTicks was called (our fix).
            context.Verify(c => c.SanitizePositionTicks(It.IsAny<long>()), Times.Once);
        }

        [Fact]
        public void SessionJoined_FromPaused_DoesNotModifyPosition()
        {
            var state = new WaitingGroupState(_loggerFactory);

            long initialPositionTicks = 300_000_000_0; // 30 seconds

            var context = CreateMockContext(
                initialPositionTicks,
                600_000_000_0, // 60 seconds
                DateTime.UtcNow);

            var session = CreateSession();

            // Act: session joins from Paused state (no position update expected).
            state.SessionJoined(context.Object, GroupStateType.Paused, session, CancellationToken.None);

            // Assert: position should remain unchanged.
            Assert.Equal(initialPositionTicks, context.Object.PositionTicks);

            // SanitizePositionTicks should NOT be called since we're not in Playing state.
            context.Verify(c => c.SanitizePositionTicks(It.IsAny<long>()), Times.Never);
        }
    }
}
