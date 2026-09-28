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

public sealed class ChannelManagerTests : IDisposable
{
    private const string ChannelName = "Test Channel";

    private readonly string _cachePath = Path.Combine(Path.GetTempPath(), "jellyfin-channel-tests-" + Guid.NewGuid().ToString("N"));
    private readonly ConcurrentDictionary<string, Guid> _itemIds = new();
    private readonly ConcurrentDictionary<Guid, BaseItem> _library = new();
    private readonly ManualResetEventSlim _creatingItem = new();
    private readonly ManualResetEventSlim _libraryRead = new();
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly Mock<ILibraryManager> _libraryManager = new();
    private readonly Mock<IChannel> _channel = new();
    private readonly ChannelManager _channelManager;
    private readonly Channel _channelItem;
    private readonly ILibraryManager _previousLibraryManager = BaseItem.LibraryManager;
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
                // Pause the first creation until another request reads the library, if one does.
                if (Interlocked.Increment(ref _pausedCreations) == 1)
                {
                    _creatingItem.Set();
                    _libraryRead.Wait(TimeSpan.FromSeconds(1));
                }

                _library[item.Id] = item;
            });
        _libraryManager
            .Setup(x => x.GetItemIds(It.IsAny<InternalItemsQuery>()))
            .Returns(() => ChildrenOfChannel().Select(i => i.Id).ToList());
        _libraryManager
            .Setup(x => x.GetItemsResult(It.IsAny<InternalItemsQuery>()))
            .Returns(() =>
            {
                var items = ChildrenOfChannel();
                _libraryRead.Set();
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
            .ReturnsAsync(() => new ChannelItemResult
            {
                Items =
                [
                    new ChannelItemInfo { Id = "a", Name = "A", Type = ChannelItemType.Folder },
                    new ChannelItemInfo { Id = "b", Name = "B", Type = ChannelItemType.Folder },
                ],
                TotalRecordCount = 2,
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
        // On its own thread, since creating an item blocks while paused.
        var first = Task.Run(() => _channelManager.GetChannelItemsInternal(
            new InternalItemsQuery { ChannelIds = [_channelItem.Id] },
            new Progress<double>(),
            TestContext.Current.CancellationToken));

        // Start the second request while the first one creates the items.
        if (!_creatingItem.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken))
        {
            // Surfaces the failure of the first request, if that is why no item is being created.
            await first;
            Assert.Fail("The first request did not create an item.");
        }

        var second = await _channelManager.GetChannelItemsInternal(
            new InternalItemsQuery { ChannelIds = [_channelItem.Id] },
            new Progress<double>(),
            TestContext.Current.CancellationToken);

        Assert.Equal(2, (await first).Items.Count);
        Assert.Equal(2, second.Items.Count);
        _channel.Verify(x => x.GetChannelItems(It.IsAny<InternalChannelItemQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    public void Dispose()
    {
        BaseItem.LibraryManager = _previousLibraryManager;
        _channelManager.Dispose();
        _memoryCache.Dispose();
        _creatingItem.Dispose();
        _libraryRead.Dispose();
        if (Directory.Exists(_cachePath))
        {
            Directory.Delete(_cachePath, true);
        }
    }

    private List<BaseItem> ChildrenOfChannel()
        => _library.Values.Where(i => i.ParentId.Equals(_channelItem.Id)).ToList();
}
