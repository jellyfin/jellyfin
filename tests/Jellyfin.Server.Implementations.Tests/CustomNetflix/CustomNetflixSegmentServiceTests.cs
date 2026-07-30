using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.CustomNetflix;
using MediaBrowser.Controller.CustomNetflix;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaSegments;
using MediaBrowser.Model.Configuration;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixSegmentServiceTests
{
    [Fact]
    public async Task GetCoverage_UsesCountQueriesWithoutLoadingTheLibrary()
    {
        var repository = new Mock<ICustomNetflixRepository>();
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager
            .Setup(mock => mock.GetCount(It.Is<InternalItemsQuery>(query =>
                query.Recursive
                && query.IsFolder == false
                && query.IsVirtualItem == false
                && query.MediaTypes.SequenceEqual(new[] { MediaType.Video })
                && !query.GroupByPresentationUniqueKey)))
            .Returns(80);
        var mediaSegmentManager = new Mock<IMediaSegmentManager>();
        mediaSegmentManager
            .Setup(mock => mock.GetSegmentedItemCountsAsync(
                CustomNetflixSegmentCoveragePolicy.SegmentTypes,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<MediaSegmentType, int>
            {
                [MediaSegmentType.Intro] = 40
            });
        var service = new CustomNetflixSegmentService(
            repository.Object,
            new Mock<IUserManager>().Object,
            libraryManager.Object,
            mediaSegmentManager.Object);

        var result = await service.GetCoverageAsync(TestContext.Current.CancellationToken);

        Assert.Equal(80, result.EligibleItems);
        Assert.Equal(40, result.Types.Single(type => type.Type == "intro").CoveredItems);
        libraryManager.Verify(mock => mock.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Never);
        libraryManager.VerifyAll();
        mediaSegmentManager.VerifyAll();
    }

    [Fact]
    public async Task ReplaceManualSegments_PreservesNativeSegmentsAndRunsOnlyManualProvider()
    {
        var itemId = Guid.NewGuid();
        var user = new User("segments", "auth", "reset");
        var item = new Movie { Id = itemId };
        var repository = new Mock<ICustomNetflixRepository>();
        repository
            .Setup(mock => mock.ReplaceManualMediaSegmentsAsync(
                itemId,
                It.IsAny<IReadOnlyList<CustomMediaSegmentRow>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var userManager = new Mock<IUserManager>();
        userManager.Setup(mock => mock.GetUserById(user.Id)).Returns(user);
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(mock => mock.GetItemById<BaseItem>(itemId, user)).Returns(item);
        var mediaSegmentManager = new Mock<IMediaSegmentManager>();
        mediaSegmentManager.Setup(mock => mock.IsTypeSupported(item)).Returns(true);
        mediaSegmentManager
            .Setup(mock => mock.GetSupportedProviders(item))
            .Returns(
            [
                (CustomNetflixManualMediaSegmentProvider.ProviderName, "manual"),
                ("Automatic", "automatic")
            ]);
        mediaSegmentManager
            .Setup(mock => mock.RunSegmentPluginProviders(
                item,
                It.Is<LibraryOptions>(options =>
                    options.DisabledMediaSegmentProviders.SequenceEqual(new[] { "Automatic" })
                    && options.MediaSegmentProviderOrder.SequenceEqual(new[] { CustomNetflixManualMediaSegmentProvider.ProviderName })),
                false,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = new CustomNetflixSegmentService(
            repository.Object,
            userManager.Object,
            libraryManager.Object,
            mediaSegmentManager.Object);

        var result = await service.ReplaceManualSegmentsAsync(
            user.Id,
            itemId,
            new CustomNetflixManualMediaSegmentsRequest
            {
                Segments =
                [
                    new CustomNetflixManualMediaSegmentRequest
                    {
                        Type = "intro",
                        StartSeconds = 10,
                        EndSeconds = 20
                    }
                ]
            },
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Single(result.Segments);
        mediaSegmentManager.Verify(mock => mock.DeleteSegmentAsync(It.IsAny<Guid>()), Times.Never);
        mediaSegmentManager.VerifyAll();
    }
}
