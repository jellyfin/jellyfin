using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.LiveTv.Configuration;
using Jellyfin.LiveTv.Listings;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.LiveTv.Tests.Listings;

public class ListingsManagerTests
{
    private readonly IConfigurationManager _config;
    private readonly IListingsProvider[] _listingsProviders;
    private readonly ILogger<ListingsManager> _logger;
    private readonly ITaskManager _taskManager;
    private readonly ITunerHostManager _tunerHostManager;

    public ListingsManagerTests()
    {
        _logger = Mock.Of<ILogger<ListingsManager>>();
        _config = Mock.Of<IConfigurationManager>();
        _taskManager = Mock.Of<ITaskManager>();
        _tunerHostManager = Mock.Of<ITunerHostManager>();
        _listingsProviders = new[] { Mock.Of<IListingsProvider>() };
    }

    [Fact]
    public void DeleteListingsProvider_DeletesProvider()
    {
        // Arrange
        var id = "MockId";
        var manager = new ListingsManager(_logger, _config, _taskManager, _tunerHostManager, _listingsProviders);

        Mock.Get(_config)
            .Setup(x => x.GetConfiguration(It.IsAny<string>()))
            .Returns(new LiveTvOptions { ListingProviders = [new ListingsProviderInfo { Id = id }] });

        // Act
        manager.DeleteListingsProvider(id);

        // Assert
        Assert.DoesNotContain(
            _config.GetLiveTvConfiguration().ListingProviders,
            p => p.Id.Equals(id, StringComparison.Ordinal));
    }

    [Fact]
    public async Task AddProviderMetadata_UseGuideChannelNames_RenamesExplicitlyMappedChannel()
    {
        var tunerChannel = new ChannelInfo
        {
            Id = "hdhr_5024",
            Name = "PRIMA HISTORY H"
        };
        var manager = CreateManager(
            useGuideChannelNames: true,
            mappings:
            [
                new NameValuePair { Name = "hdhr_5024", Value = "prima-history" }
            ],
            epgChannels:
            [
                new ChannelInfo { Id = "prima-history", Name = "PRIMA HISTORY HD", ImageUrl = "http://guide/prima.png" }
            ]);

        await manager.AddProviderMetadata([tunerChannel], false, CancellationToken.None);

        Assert.Equal("PRIMA HISTORY HD", tunerChannel.Name);
        Assert.Equal("http://guide/prima.png", tunerChannel.ImageUrl);
    }

    [Fact]
    public async Task AddProviderMetadata_UseGuideChannelNamesDisabled_KeepsTunerName()
    {
        var tunerChannel = new ChannelInfo
        {
            Id = "hdhr_5024",
            Name = "PRIMA HISTORY H"
        };
        var manager = CreateManager(
            useGuideChannelNames: false,
            mappings:
            [
                new NameValuePair { Name = "hdhr_5024", Value = "prima-history" }
            ],
            epgChannels:
            [
                new ChannelInfo { Id = "prima-history", Name = "PRIMA HISTORY HD" }
            ]);

        await manager.AddProviderMetadata([tunerChannel], false, CancellationToken.None);

        Assert.Equal("PRIMA HISTORY H", tunerChannel.Name);
    }

    [Fact]
    public async Task AddProviderMetadata_UseGuideChannelNames_KeepsTunerNameWithoutExplicitMapping()
    {
        var tunerChannel = new ChannelInfo
        {
            Id = "hdhr_5002",
            Name = "Nat Geo HD"
        };
        var manager = CreateManager(
            useGuideChannelNames: true,
            mappings: [],
            epgChannels:
            [
                new ChannelInfo { Id = "nat-geo", Name = "NatGeoHD", ImageUrl = "http://guide/natgeo.png" }
            ]);

        await manager.AddProviderMetadata([tunerChannel], false, CancellationToken.None);

        Assert.Equal("Nat Geo HD", tunerChannel.Name);
        Assert.Equal("http://guide/natgeo.png", tunerChannel.ImageUrl);
    }

    private ListingsManager CreateManager(bool useGuideChannelNames, NameValuePair[] mappings, List<ChannelInfo> epgChannels)
    {
        var provider = new Mock<IListingsProvider>();
        provider.Setup(p => p.Type).Returns("xmltv");
        provider.Setup(p => p.GetChannels(It.IsAny<ListingsProviderInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epgChannels);

        var config = new Mock<IConfigurationManager>();
        config.Setup(c => c.GetConfiguration(It.IsAny<string>()))
            .Returns(new LiveTvOptions
            {
                ListingProviders =
                [
                    new ListingsProviderInfo
                    {
                        Id = "provider",
                        Type = "xmltv",
                        EnableAllTuners = true,
                        UseGuideChannelNames = useGuideChannelNames,
                        ChannelMappings = mappings
                    }
                ]
            });

        return new ListingsManager(
            Mock.Of<ILogger<ListingsManager>>(),
            config.Object,
            Mock.Of<ITaskManager>(),
            Mock.Of<ITunerHostManager>(),
            [provider.Object]);
    }
}
