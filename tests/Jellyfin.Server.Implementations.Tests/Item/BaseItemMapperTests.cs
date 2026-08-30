using System;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Configuration;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public class BaseItemMapperTests
{
    [Theory]
    [InlineData(null, "ω")]
    [InlineData("", "ω")]
    [InlineData("Ψυχή", "ψ")]
    public void Map_UsesEffectiveSortNameSourceForInitial(string? forcedSortName, string expected)
    {
        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.SetupGet(manager => manager.Configuration).Returns(new ServerConfiguration());
        BaseItem.ConfigurationManager = configurationManager.Object;

        var item = new Movie
        {
            Id = Guid.NewGuid(),
            Name = "Ωμέγα",
            ForcedSortName = forcedSortName
        };

        var entity = BaseItemMapper.Map(item, Mock.Of<IServerApplicationHost>());

        Assert.Equal(expected, entity.SortNameInitial);
    }
}
