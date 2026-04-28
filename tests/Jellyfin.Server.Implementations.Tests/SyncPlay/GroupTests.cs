using System;
using Emby.Server.Implementations.SyncPlay;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SyncPlay
{
    public sealed class GroupTests : IDisposable
    {
        private readonly NullLoggerFactory _loggerFactory = new();

        public void Dispose()
        {
            _loggerFactory.Dispose();
        }

        private Group CreateGroup()
        {
            var userManager = new Mock<IUserManager>();
            var sessionManager = new Mock<ISessionManager>();
            var libraryManager = new Mock<ILibraryManager>();

            return new Group(
                _loggerFactory,
                userManager.Object,
                sessionManager.Object,
                libraryManager.Object);
        }

        [Fact]
        public void SanitizePositionTicks_ClampsToRunTimeTicks()
        {
            var group = CreateGroup();
            // RunTimeTicks defaults to 0 when no item is playing.
            // SanitizePositionTicks should clamp to [0, RunTimeTicks].
            var result = group.SanitizePositionTicks(100);
            Assert.Equal(0, result);
        }

        [Fact]
        public void SanitizePositionTicks_ClampsNegativeToZero()
        {
            var group = CreateGroup();
            var result = group.SanitizePositionTicks(-100);
            Assert.Equal(0, result);
        }

        /// <summary>
        /// Regression test: GetPlayQueueUpdate should clamp startPositionTicks
        /// to [0, RunTimeTicks] to prevent overshoot past media duration.
        /// </summary>
        [Fact]
        public void GetPlayQueueUpdate_ClampsPositionToRunTimeTicks()
        {
            var group = CreateGroup();

            // Set position beyond what RunTimeTicks would allow.
            // RunTimeTicks is 0 (no item loaded), so any positive PositionTicks should be clamped.
            group.PositionTicks = 600_000_000_0; // 60 seconds in ticks
            group.LastActivity = DateTime.UtcNow;

            var update = group.GetPlayQueueUpdate(PlayQueueUpdateReason.NewPlaylist);

            // Since RunTimeTicks is 0, startPositionTicks should be clamped to 0.
            Assert.Equal(0, update.StartPositionTicks);
        }

        [Fact]
        public void GetPlayQueueUpdate_DoesNotClampWithinValidRange()
        {
            var group = CreateGroup();

            // PositionTicks is 0 and RunTimeTicks is 0, so it should stay 0.
            group.PositionTicks = 0;
            group.LastActivity = DateTime.UtcNow;

            var update = group.GetPlayQueueUpdate(PlayQueueUpdateReason.NewPlaylist);
            Assert.Equal(0, update.StartPositionTicks);
        }
    }
}
