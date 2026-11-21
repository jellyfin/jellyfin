using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Providers.MediaInfo;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

public class SubtitleExtractionTaskTests
{
    [Fact]
    public async Task ExecuteAsync_QueriesNewestVideosFirst()
    {
        InternalItemsQuery? query = null;
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(m => m.GetCount(It.IsAny<InternalItemsQuery>()))
            .Callback<InternalItemsQuery>(q => query = q)
            .Returns(0);

        var task = new SubtitleExtractionTask(
            NullLogger<SubtitleExtractionTask>.Instance,
            libraryManager.Object,
            Mock.Of<ILocalizationManager>(),
            Mock.Of<ISubtitleEncoder>());

        await task.ExecuteAsync(new Progress<double>(), CancellationToken.None);

        Assert.NotNull(query);
        Assert.Equal([(ItemSortBy.DateCreated, SortOrder.Descending)], query.OrderBy);
    }
}
