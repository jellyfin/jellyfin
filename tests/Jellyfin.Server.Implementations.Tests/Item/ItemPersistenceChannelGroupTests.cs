using System;
using System.Linq;
using System.Threading;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class ItemPersistenceChannelGroupTests : SqliteDbTestFixture
{
    private readonly ItemPersistenceService _service;
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IServerConfigurationManager? _previousConfigurationManager;

    public ItemPersistenceChannelGroupTests()
    {
        // BaseItem resolves these through process-wide statics; restored in Dispose.
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousConfigurationManager = BaseItem.ConfigurationManager;

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.GetCollectionFolders(It.IsAny<BaseItem>())).Returns([]);
        BaseItem.LibraryManager = libraryManager.Object;

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        BaseItem.ConfigurationManager = configurationManager.Object;

        _service = new ItemPersistenceService(
            CreateDbContextFactory(),
            new Mock<IServerApplicationHost>().Object,
            NullLogger<ItemPersistenceService>.Instance);
    }

    protected override void Dispose(bool disposing)
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.ConfigurationManager = _previousConfigurationManager!;
        base.Dispose(disposing);
    }

    [Fact]
    public void SaveItems_LiveTvChannel_WritesTheGroupAsAnItemValueAndFollowsChanges()
    {
        var id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        _service.SaveItems([CreateChannel(id, "Sports")], CancellationToken.None);
        Assert.Equal(["Sports"], GroupsOf(id));

        // The channel moved to another group: the old value is unlinked, the new one written.
        _service.SaveItems([CreateChannel(id, "News")], CancellationToken.None);
        Assert.Equal(["News"], GroupsOf(id));

        // The playlist dropped the group-title: no group value is left behind.
        _service.SaveItems([CreateChannel(id, null)], CancellationToken.None);
        Assert.Empty(GroupsOf(id));
    }

    [Fact]
    public void SaveItems_LiveTvChannel_GroupIsNotMistakenForATag()
    {
        var id = Guid.Parse("abababab-abab-abab-abab-abababababab");

        _service.SaveItems([CreateChannel(id, "Sports")], CancellationToken.None);

        using var ctx = CreateDbContext();
        var types = ctx.ItemValuesMap
            .Where(m => m.ItemId.Equals(id))
            .Select(m => m.ItemValue.Type)
            .ToList();
        Assert.Equal([ItemValueType.ChannelGroup], types);
    }

    private string[] GroupsOf(Guid id)
    {
        using var ctx = CreateDbContext();
        return ctx.ItemValuesMap
            .Where(m => m.ItemId.Equals(id) && m.ItemValue.Type == ItemValueType.ChannelGroup)
            .Select(m => m.ItemValue.Value)
            .ToArray();
    }

    private static LiveTvChannel CreateChannel(Guid id, string? channelGroup)
    {
        return new LiveTvChannel
        {
            Id = id,
            Name = "Channel",
            Number = "1",
            ServiceName = "Test",
            ChannelGroup = channelGroup
        };
    }
}
