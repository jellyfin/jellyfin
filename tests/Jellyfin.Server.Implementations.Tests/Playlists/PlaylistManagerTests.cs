using Emby.Server.Implementations.Playlists;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Playlists;

public class PlaylistManagerTests
{
    [Fact]
    public void DetermineAdjustedIndexMoveToFirstPositionNoPriorInAllList()
    {
        var priorIndexAllChildren = 0;
        var newIndex = 0;

        var adjustedIndex = PlaylistManager.DetermineAdjustedIndex(priorIndexAllChildren, newIndex);

        Assert.Equal(0, adjustedIndex);
    }

    [Fact]
    public void DetermineAdjustedIndexPriorInMiddleOfAllList()
    {
        var priorIndexAllChildren = 2;
        var newIndex = 0;

        var adjustedIndex = PlaylistManager.DetermineAdjustedIndex(priorIndexAllChildren, newIndex);

        Assert.Equal(1, adjustedIndex);
    }

    [Fact]
    public void DetermineAdjustedIndexMoveMiddleOfPlaylist()
    {
        var priorIndexAllChildren = 2;
        var newIndex = 1;

        var adjustedIndex = PlaylistManager.DetermineAdjustedIndex(priorIndexAllChildren, newIndex);

        Assert.Equal(3, adjustedIndex);
    }
}
