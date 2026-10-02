using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.LiveTv.Channels;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.LiveTv.Tests.Channels;

// Sets the static library manager of BaseItem.
[Collection("LibraryManagerTests")]
public sealed class ChannelManagerTests : IDisposable
{
    private const string ChannelName = "Test Channel";

    private readonly string _cachePath = Path.Combine(Path.GetTempPath(), "jellyfin-channel-tests-" + Guid.NewGuid().ToString("N"));
    private readonly ConcurrentDictionary<string, Guid> _itemIds = new();
    private readonly ConcurrentDictionary<Guid, BaseItem> _library = new();
    private readonly ManualResetEventSlim _creatingItem = new();
    private readonly ManualResetEventSlim _releaseCreation = new();
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly Mock<ILibraryManager> _libraryManager = new();
    private readonly Mock<IChannel> _channel = new();
    private readonly ChannelManager _channelManager;
    private readonly Channel _channelItem;
    private readonly ILibraryManager _previousLibraryManager = BaseItem.LibraryManager;
    private readonly Dictionary<string, List<ChannelItemInfo>> _folders = new()
    {
        [string.Empty] =
        [
            new ChannelItemInfo { Id = "a", Name = "A", Type = ChannelItemType.Folder },
            new ChannelItemInfo { Id = "b", Name = "B", Type = ChannelItemType.Folder },
        ],
    };

    private Func<BaseItem, bool> _pauseCreationOf = _ => false;
    private bool _releaseCreationOnLibraryRead;
    private int _pausedCreations;

    public ChannelManagerTests()
    {
        _libraryManager
            .Setup(x => x.GetNewItemId(It.IsAny<string>(), It.IsAny<Type>()))
            .Returns((string key, Type type) => _itemIds.GetOrAdd(type.FullName + key, _ => Guid.NewGuid()));
        _libraryManager
            .Setup(x => x.GetItemById(It.IsAny<Guid>()))
            .Returns((Guid id) => _library.GetValueOrDefault(id));
        _libraryManager
            .Setup(x => x.CreateItem(It.IsAny<BaseItem>(), It.IsAny<BaseItem>()))
            .Callback((BaseItem item, BaseItem _) =>
            {
                // Pause the creation of the first matching item until it is released, or a second has passed.
                if (_pauseCreationOf(item) && Interlocked.Increment(ref _pausedCreations) == 1)
                {
                    _creatingItem.Set();
                    _releaseCreation.Wait(TimeSpan.FromSeconds(1));
                }

                _library[item.Id] = item;
            });
        _libraryManager
            .Setup(x => x.GetItemIds(It.IsAny<InternalItemsQuery>()))
            .Returns((InternalItemsQuery query) => ChildrenOf(query.ParentId).Select(i => i.Id).ToList());
        _libraryManager
            .Setup(x => x.GetItemsResult(It.IsAny<InternalItemsQuery>()))
            .Returns((InternalItemsQuery query) =>
            {
                var items = ChildrenOf(query.ParentId);
                if (_releaseCreationOnLibraryRead)
                {
                    _releaseCreation.Set();
                }

                return new QueryResult<BaseItem>(items);
            });

        BaseItem.LibraryManager = _libraryManager.Object;

        var channelId = _libraryManager.Object.GetNewItemId("Channel " + ChannelName, typeof(Channel));
        _channelItem = new Channel { Id = channelId, ChannelId = channelId, Name = ChannelName };
        _library[channelId] = _channelItem;

        _channel.SetupGet(x => x.Name).Returns(ChannelName);
        _channel.SetupGet(x => x.DataVersion).Returns("1");
        _channel
            .Setup(x => x.GetChannelItems(It.IsAny<InternalChannelItemQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InternalChannelItemQuery query, CancellationToken _) =>
            {
                var items = _folders[query.FolderId ?? string.Empty];
                return new ChannelItemResult { Items = items, TotalRecordCount = items.Count };
            });

        var config = new Mock<IServerConfigurationManager>();
        config.SetupGet(x => x.ApplicationPaths.CachePath).Returns(_cachePath);

        var fileSystem = new Mock<IFileSystem>();
        fileSystem
            .Setup(x => x.GetLastWriteTimeUtc(It.IsAny<string>()))
            .Returns((string path) => File.GetLastWriteTimeUtc(path));

        _channelManager = new ChannelManager(
            Mock.Of<IUserManager>(),
            Mock.Of<MediaBrowser.Controller.Dto.IDtoService>(),
            _libraryManager.Object,
            NullLogger<ChannelManager>.Instance,
            config.Object,
            fileSystem.Object,
            Mock.Of<IUserDataManager>(),
            Mock.Of<IProviderManager>(),
            _memoryCache,
            [_channel.Object]);
    }

    [Fact]
    public async Task GetChannelItemsInternal_RequestDuringItemCreation_ReturnsAllItems()
    {
        _pauseCreationOf = _ => true;
        _releaseCreationOnLibraryRead = true;

        // On its own thread, since creating an item blocks while paused.
        var first = Task.Run(() => ListAsync(null));

        // Start the second request while the first one creates the items.
        await WaitForPausedCreationAsync(first);
        var second = await ListAsync(null);

        Assert.Equal(2, (await first).Items.Count);
        Assert.Equal(2, second.Items.Count);
        VerifyFetches(Times.Once);
    }

    [Fact]
    public async Task GetChannelItemsInternal_OtherFolderDuringItemCreation_IsNotBlocked()
    {
        _folders["a"] = [new ChannelItemInfo { Id = "a1", Name = "A1", Type = ChannelItemType.Folder }];
        _folders["b"] = [new ChannelItemInfo { Id = "b1", Name = "B1", Type = ChannelItemType.Folder }];
        var root = await ListAsync(null);
        var folderA = root.Items.Single(i => i.Name == "A");
        var folderB = root.Items.Single(i => i.Name == "B");

        _pauseCreationOf = item => item.Name == "A1";
        var first = Task.Run(() => ListAsync(folderA.Id));
        await WaitForPausedCreationAsync(first);

        var second = await ListAsync(folderB.Id);

        // Folder B was listed while the items of folder A were still being created.
        Assert.False(first.IsCompleted);
        Assert.Equal("B1", Assert.Single(second.Items).Name);

        _releaseCreation.Set();
        Assert.Equal("A1", Assert.Single((await first).Items).Name);
    }

    [Fact]
    public async Task GetChannelItemsInternal_UnreadableCacheFile_FetchesTheFolderAgain()
    {
        Assert.Equal(2, (await ListAsync(null)).Items.Count);

        // As left by a crash while the file was written.
        var cacheFile = Assert.Single(Directory.GetFiles(_cachePath, "*", SearchOption.AllDirectories));
        await File.WriteAllTextAsync(cacheFile, "{\"Items\":[", TestContext.Current.CancellationToken);

        Assert.Equal(2, (await ListAsync(null)).Items.Count);
        VerifyFetches(() => Times.Exactly(2));
    }

    public void Dispose()
    {
        BaseItem.LibraryManager = _previousLibraryManager;
        _channelManager.Dispose();
        _memoryCache.Dispose();
        _creatingItem.Dispose();
        _releaseCreation.Dispose();
        if (Directory.Exists(_cachePath))
        {
            Directory.Delete(_cachePath, true);
        }
    }

    private Task<QueryResult<BaseItem>> ListAsync(Guid? parentId)
        => _channelManager.GetChannelItemsInternal(
            new InternalItemsQuery { ChannelIds = [_channelItem.Id], ParentId = parentId ?? Guid.Empty },
            new Progress<double>(),
            TestContext.Current.CancellationToken);

    private async Task WaitForPausedCreationAsync(Task first)
    {
        if (!_creatingItem.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken))
        {
            // Surfaces the failure of the first request, if that is why no item is being created.
            await first;
            Assert.Fail("The first request did not create an item.");
        }
    }

    private void VerifyFetches(Func<Times> times)
        => _channel.Verify(x => x.GetChannelItems(It.IsAny<InternalChannelItemQuery>(), It.IsAny<CancellationToken>()), times);

    private List<BaseItem> ChildrenOf(Guid parentId)
        => _library.Values.Where(i => i.ParentId.Equals(parentId)).ToList();
}
