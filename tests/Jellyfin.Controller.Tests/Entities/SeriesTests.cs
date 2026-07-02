using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Querying;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

public class SeriesTests
{
    [Fact]
    public void GetSeasonEpisodes_UsesAscendingOrderByDefault()
    {
        var series = new Series
        {
            Id = Guid.NewGuid()
        };
        var season = new Season
        {
            IndexNumber = 1,
            SeriesId = series.Id
        };

        var episodes = new List<BaseItem>
        {
            new Episode { ParentIndexNumber = 1, IndexNumber = 1, SeriesId = series.Id },
            new Episode { ParentIndexNumber = 1, IndexNumber = 2, SeriesId = series.Id }
        };

        var options = new DtoOptions(true);
        var user = new User();

        var libraryManagerMock = new Mock<ILibraryManager>();
        SortOrder capturedSortOrder = SortOrder.Ascending;
        libraryManagerMock
            .Setup(library => library.Sort(It.IsAny<IEnumerable<BaseItem>>(), It.IsAny<User>(), It.IsAny<IEnumerable<ItemSortBy>>(), It.IsAny<SortOrder>()))
            .Returns((IEnumerable<BaseItem> items, User _, IEnumerable<ItemSortBy> _, SortOrder sortOrder) =>
            {
                capturedSortOrder = sortOrder;
                return items;
            });

        libraryManagerMock.Setup(library => library.GetItemById(series.Id)).Returns(series);

        var configuration = new ServerConfiguration
        {
            DisplaySpecialsWithinSeasons = false
        };

        var configManagerMock = new Mock<IServerConfigurationManager>();
        configManagerMock.SetupGet(c => c.Configuration).Returns(configuration);

        var originalLibraryManager = BaseItem.LibraryManager;
        var originalConfigurationManager = BaseItem.ConfigurationManager;

        try
        {
            BaseItem.LibraryManager = libraryManagerMock.Object;
            BaseItem.ConfigurationManager = configManagerMock.Object;

            _ = series.GetSeasonEpisodes(season, user, episodes, options, shouldIncludeMissingEpisodes: true);

            Assert.Equal(SortOrder.Ascending, capturedSortOrder);
        }
        finally
        {
            BaseItem.LibraryManager = originalLibraryManager;
            BaseItem.ConfigurationManager = originalConfigurationManager;
        }
    }

    [Fact]
    public void GetSeasonEpisodes_UsesDescendingOrderWhenInverted()
    {
        var series = new Series
        {
            Id = Guid.NewGuid(),
            InvertEpisodeOrder = true
        };

        var season = new Season
        {
            IndexNumber = 1,
            SeriesId = series.Id
        };

        var episodes = new List<BaseItem>
        {
            new Episode { ParentIndexNumber = 1, IndexNumber = 1, SeriesId = series.Id },
            new Episode { ParentIndexNumber = 1, IndexNumber = 2, SeriesId = series.Id }
        };

        var options = new DtoOptions(true);
        var user = new User();

        var libraryManagerMock = new Mock<ILibraryManager>();
        SortOrder capturedSortOrder = SortOrder.Ascending;
        libraryManagerMock
            .Setup(library => library.Sort(It.IsAny<IEnumerable<BaseItem>>(), It.IsAny<User>(), It.IsAny<IEnumerable<ItemSortBy>>(), It.IsAny<SortOrder>()))
            .Returns((IEnumerable<BaseItem> items, User _, IEnumerable<ItemSortBy> _, SortOrder sortOrder) =>
            {
                capturedSortOrder = sortOrder;
                return items;
            });

        libraryManagerMock.Setup(library => library.GetItemById(series.Id)).Returns(series);

        var configuration = new ServerConfiguration
        {
            DisplaySpecialsWithinSeasons = false
        };

        var configManagerMock = new Mock<IServerConfigurationManager>();
        configManagerMock.SetupGet(c => c.Configuration).Returns(configuration);

        var originalLibraryManager = BaseItem.LibraryManager;
        var originalConfigurationManager = BaseItem.ConfigurationManager;

        try
        {
            BaseItem.LibraryManager = libraryManagerMock.Object;
            BaseItem.ConfigurationManager = configManagerMock.Object;

            _ = series.GetSeasonEpisodes(season, user, episodes, options, shouldIncludeMissingEpisodes: true);

            Assert.Equal(SortOrder.Descending, capturedSortOrder);
        }
        finally
        {
            BaseItem.LibraryManager = originalLibraryManager;
            BaseItem.ConfigurationManager = originalConfigurationManager;
        }
    }
}
