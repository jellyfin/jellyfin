using System;
using Emby.Server.Implementations.Playlists;
using Jellyfin.Server.Implementations.Users;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Playlists
{
    public class PlaylistManagerTests
    {
        [Fact]
        public void DetermineAdjustedIndex()
        {
            // Arrange
            var priorIndexAllChildren = 0;
            var newIndex = 1;
            // Act
            var adjustedIndex = PlaylistManager.DetermineAdjustedIndex(priorIndexAllChildren, newIndex);

            // Assert
            Assert.Equal(1, adjustedIndex);
        }
    }
}
