using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Images;
using Jellyfin.Data;
using Jellyfin.Data.Events;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Images;

/// <summary>
/// The library collages are otherwise only rebuilt every few days, so blocking an item would leave it pictured
/// until then. The watcher re-checks every library as soon as something blocked is saved or a user's
/// restrictions change, never loses a change made while a re-check runs, and keeps going when one fails. It
/// must not react to a library being saved, which its own re-check does.
/// </summary>
[Collection("LibraryManagerTests")]
public sealed class CollectionFolderImageBlockWatcherTests : IDisposable
{
    private const string BlockedTag = "Adult";

    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    private readonly Mock<ILibraryManager> _libraryManager = new();
    private readonly Mock<IUserManager> _userManager = new();
    private readonly Mock<IProviderManager> _providerManager = new();
    private readonly CollectionFolder[] _libraries =
    [
        new() { Id = Guid.NewGuid(), Name = "Movies" },
        new() { Id = Guid.NewGuid(), Name = "Shows" }
    ];

    private readonly ConcurrentQueue<BaseItem> _refreshed = new();
    private readonly SemaphoreSlim _refreshCount = new(0);
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly CollectionFolderImageBlockWatcher _watcher;

    private Func<BaseItem, Task> _onRefresh = _ => Task.CompletedTask;

    public CollectionFolderImageBlockWatcherTests()
    {
        // An item looks up the libraries it inherits tags from through this process-wide static; restored in Dispose.
        _previousLibraryManager = BaseItem.LibraryManager;
        BaseItem.LibraryManager = _libraryManager.Object;

        _libraryManager.Setup(l => l.GetCollectionFolders(It.IsAny<BaseItem>())).Returns([]);
        _libraryManager.Setup(l => l.GetUserRootFolder()).Returns(new UserRootFolder { Children = _libraries });

        var user = new User("restricted", "auth", "reset");
        user.SetPreference(PreferenceKind.BlockedTags, [BlockedTag]);
        _userManager.Setup(u => u.GetUsers()).Returns([user]);

        _providerManager
            .Setup(p => p.RefreshSingleItem(It.IsAny<BaseItem>(), It.IsAny<MetadataRefreshOptions>(), It.IsAny<CancellationToken>()))
            .Returns<BaseItem, MetadataRefreshOptions, CancellationToken>(async (item, _, _) =>
            {
                _refreshed.Enqueue(item);
                _refreshCount.Release();
                await _onRefresh(item).ConfigureAwait(false);
                return ItemUpdateType.None;
            });

        _watcher = new CollectionFolderImageBlockWatcher(
            _libraryManager.Object,
            _userManager.Object,
            _providerManager.Object,
            Mock.Of<IFileSystem>(),
            NullLogger<CollectionFolderImageBlockWatcher>.Instance,
            TimeSpan.Zero);
    }

    public void Dispose()
    {
        _watcher.Dispose();
        _refreshCount.Dispose();
        BaseItem.LibraryManager = _previousLibraryManager!;
    }

    [Fact]
    public void MayAffectCollages_ItemCarryingABlockedTag_IsTrue()
    {
        Assert.True(CollectionFolderImageBlockWatcher.MayAffectCollages(new Movie { Tags = [BlockedTag] }, [BlockedTag.GetCleanValue()]));
    }

    [Fact]
    public void MayAffectCollages_ItemCarryingNoBlockedTag_IsFalse()
    {
        Assert.False(CollectionFolderImageBlockWatcher.MayAffectCollages(new Movie { Tags = ["Family"] }, [BlockedTag.GetCleanValue()]));
    }

    [Fact]
    public void MayAffectCollages_LibraryCarryingABlockedTag_IsFalse()
    {
        // Re-checking saves the libraries, so reacting to that would re-check forever.
        Assert.False(CollectionFolderImageBlockWatcher.MayAffectCollages(new CollectionFolder { Tags = [BlockedTag] }, [BlockedTag.GetCleanValue()]));
    }

    [Fact]
    public async Task ItemUpdated_ItemCarryingABlockedTag_RechecksEveryLibrary()
    {
        await _watcher.StartAsync(TestContext.Current.CancellationToken);

        SaveBlockedItem();

        var refreshed = await WaitForRefreshesAsync(2);

        Assert.Equal(_libraries, refreshed);
    }

    [Fact]
    public async Task OnUserUpdated_RechecksEveryLibrary()
    {
        await _watcher.StartAsync(TestContext.Current.CancellationToken);

        _userManager.Raise(u => u.OnUserUpdated += null, new GenericEventArgs<User>(new User("restricted", "auth", "reset")));

        var refreshed = await WaitForRefreshesAsync(2);

        Assert.Equal(_libraries, refreshed);
    }

    [Fact]
    public async Task ItemUpdated_WhileARecheckRuns_RechecksAgainOnceItIsDone()
    {
        var firstRefreshCanFinish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _onRefresh = _ => firstRefreshCanFinish.Task;
        await _watcher.StartAsync(TestContext.Current.CancellationToken);

        SaveBlockedItem();
        await WaitForRefreshesAsync(1);

        // The running re-check may already be past whatever this changes.
        SaveBlockedItem();
        firstRefreshCanFinish.SetResult();

        var refreshed = await WaitForRefreshesAsync(3);

        Assert.Equal([.. _libraries, .. _libraries], refreshed);
    }

    [Fact]
    public async Task Recheck_LibraryFailingToRefresh_StillRechecksTheOthersAndKeepsWatching()
    {
        // A provider that gives up on its own, such as on an HTTP timeout, cancels without shutdown being asked for.
        _onRefresh = item => item == _libraries[0] ? throw new TaskCanceledException() : Task.CompletedTask;
        await _watcher.StartAsync(TestContext.Current.CancellationToken);

        SaveBlockedItem();
        var firstRecheck = await WaitForRefreshesAsync(2);
        Assert.Equal(_libraries, firstRecheck);

        SaveBlockedItem();
        var bothRechecks = await WaitForRefreshesAsync(2);
        Assert.Equal([.. _libraries, .. _libraries], bothRechecks);
    }

    [Fact]
    public async Task StopAsync_ChangesMadeAfterwards_AreIgnored()
    {
        await _watcher.StartAsync(TestContext.Current.CancellationToken);
        await _watcher.StopAsync(TestContext.Current.CancellationToken);

        SaveBlockedItem();

        _providerManager.Verify(
            p => p.RefreshSingleItem(It.IsAny<BaseItem>(), It.IsAny<MetadataRefreshOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void SaveBlockedItem()
        => _libraryManager.Raise(
            l => l.ItemUpdated += null,
            _libraryManager.Object,
            new ItemChangeEventArgs { Item = new Movie { Id = Guid.NewGuid(), Tags = [BlockedTag] } });

    /// <summary>
    /// Waits for that many more libraries to be refreshed.
    /// </summary>
    /// <param name="count">The number of refreshes to wait for.</param>
    /// <returns>Every library refreshed so far, in order.</returns>
    private async Task<BaseItem[]> WaitForRefreshesAsync(int count)
    {
        for (var i = 0; i < count; i++)
        {
            Assert.True(await _refreshCount.WaitAsync(_timeout, TestContext.Current.CancellationToken), "Timed out waiting for a library to be refreshed.");
        }

        return [.. _refreshed];
    }
}
