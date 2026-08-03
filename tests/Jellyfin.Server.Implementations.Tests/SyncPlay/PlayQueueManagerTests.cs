using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.SyncPlay.Queue;
using MediaBrowser.Model.SyncPlay;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SyncPlay;

public class PlayQueueManagerTests
{
    private static PlayQueueManager CreateQueue(int itemCount)
    {
        var items = Enumerable.Range(0, itemCount).Select(_ => Guid.NewGuid()).ToList();
        var queue = new PlayQueueManager();
        queue.SetPlaylist(items);
        return queue;
    }

    [Fact]
    public void RemoveFromPlaylist_PlayingItemAndPrecedingItemRemoved_PicksPreviousItem()
    {
        var queue = CreateQueue(5);
        queue.SetPlayingItemByIndex(3);

        var playlist = queue.GetPlaylist();
        var expectedItemId = playlist[2].ItemId;
        var toRemove = new List<Guid> { playlist[0].PlaylistItemId, playlist[3].PlaylistItemId };
        var playingItemRemoved = queue.RemoveFromPlaylist(toRemove);

        Assert.True(playingItemRemoved);
        Assert.Equal(3, queue.GetPlaylist().Count);
        Assert.Equal(1, queue.PlayingItemIndex);
        Assert.Equal(expectedItemId, queue.GetPlayingItemId());
    }

    [Fact]
    public void RemoveFromPlaylist_PlayingItemAndAllPrecedingItemsRemoved_PicksFirstRemainingItem()
    {
        var queue = CreateQueue(3);
        queue.SetPlayingItemByIndex(2);

        var playlist = queue.GetPlaylist();
        var expectedItemId = playlist[1].ItemId;
        var toRemove = new List<Guid> { playlist[0].PlaylistItemId, playlist[2].PlaylistItemId };
        var playingItemRemoved = queue.RemoveFromPlaylist(toRemove);

        Assert.True(playingItemRemoved);
        Assert.Single(queue.GetPlaylist());
        Assert.Equal(0, queue.PlayingItemIndex);
        Assert.Equal(expectedItemId, queue.GetPlayingItemId());
    }

    [Fact]
    public void RemoveFromPlaylist_AllItemsRemoved_ResetsPlayingItem()
    {
        var queue = CreateQueue(2);
        queue.SetPlayingItemByIndex(1);

        var toRemove = queue.GetPlaylist().Select(item => item.PlaylistItemId).ToList();
        var playingItemRemoved = queue.RemoveFromPlaylist(toRemove);

        Assert.True(playingItemRemoved);
        Assert.Empty(queue.GetPlaylist());
        Assert.False(queue.IsItemPlaying());
        Assert.Equal(Guid.Empty, queue.GetPlayingItemPlaylistId());
    }

    [Fact]
    public void RemoveFromPlaylist_ShuffleMode_PicksPreviousItem()
    {
        var queue = CreateQueue(5);
        queue.SetShuffleMode(GroupShuffleMode.Shuffle);
        queue.SetPlayingItemByIndex(3);

        var playlist = queue.GetPlaylist();
        var expectedItemId = playlist[2].ItemId;
        var toRemove = new List<Guid> { playlist[0].PlaylistItemId, playlist[3].PlaylistItemId };
        var playingItemRemoved = queue.RemoveFromPlaylist(toRemove);

        Assert.True(playingItemRemoved);
        Assert.Equal(3, queue.GetPlaylist().Count);
        Assert.Equal(1, queue.PlayingItemIndex);
        Assert.Equal(expectedItemId, queue.GetPlayingItemId());
    }

    [Fact]
    public void RemoveFromPlaylist_PlayingItemNotRemoved_RestoresPlayingItem()
    {
        var queue = CreateQueue(3);
        queue.SetPlayingItemByIndex(2);

        var playlist = queue.GetPlaylist();
        var expectedItemId = playlist[2].ItemId;
        var toRemove = new List<Guid> { playlist[0].PlaylistItemId };
        var playingItemRemoved = queue.RemoveFromPlaylist(toRemove);

        Assert.False(playingItemRemoved);
        Assert.Equal(1, queue.PlayingItemIndex);
        Assert.Equal(expectedItemId, queue.GetPlayingItemId());
    }

    [Theory]
    [InlineData(GroupRepeatMode.RepeatNone)]
    [InlineData(GroupRepeatMode.RepeatOne)]
    [InlineData(GroupRepeatMode.RepeatAll)]
    public void Next_EmptyPlaylist_ReturnsFalse(GroupRepeatMode repeatMode)
    {
        var queue = new PlayQueueManager();
        queue.SetRepeatMode(repeatMode);

        Assert.False(queue.Next());
        Assert.False(queue.IsItemPlaying());
        Assert.Equal(Guid.Empty, queue.GetPlayingItemPlaylistId());
    }

    [Theory]
    [InlineData(GroupRepeatMode.RepeatNone)]
    [InlineData(GroupRepeatMode.RepeatOne)]
    [InlineData(GroupRepeatMode.RepeatAll)]
    public void Previous_EmptyPlaylist_ReturnsFalse(GroupRepeatMode repeatMode)
    {
        var queue = new PlayQueueManager();
        queue.SetRepeatMode(repeatMode);

        Assert.False(queue.Previous());
        Assert.False(queue.IsItemPlaying());
        Assert.Equal(Guid.Empty, queue.GetPlayingItemPlaylistId());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(3)]
    public void SetPlayingItemByIndex_OutOfBounds_ResetsPlayingItem(int playlistIndex)
    {
        var queue = CreateQueue(2);

        queue.SetPlayingItemByIndex(playlistIndex);

        Assert.False(queue.IsItemPlaying());
        Assert.Equal(Guid.Empty, queue.GetPlayingItemPlaylistId());
    }

    [Fact]
    public void SetPlayingItemByIndex_InBounds_SetsPlayingItem()
    {
        var queue = CreateQueue(2);
        var expectedItemId = queue.GetPlaylist()[1].ItemId;

        queue.SetPlayingItemByIndex(1);

        Assert.True(queue.IsItemPlaying());
        Assert.Equal(expectedItemId, queue.GetPlayingItemId());
    }
}
