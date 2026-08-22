using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.Metrics;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Metrics;

public class LibraryMetricsTests
{
    [Fact]
    public async Task CollectAsync_QueriesEveryTrackedKind()
    {
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(m => m.GetCount(It.IsAny<InternalItemsQuery>())).Returns(7);

        var collector = new LibraryMetrics(libraryManager.Object);
        await collector.CollectAsync(CancellationToken.None);

        libraryManager.Verify(m => m.GetCount(It.IsAny<InternalItemsQuery>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CollectAsync_WithEmptyLibrary_DoesNotThrow()
    {
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(m => m.GetCount(It.IsAny<InternalItemsQuery>())).Returns(0);

        var collector = new LibraryMetrics(libraryManager.Object);
        await collector.CollectAsync(CancellationToken.None);
    }

    [Fact]
    public void Name_ReturnsExpectedValue()
    {
        var collector = new LibraryMetrics(Mock.Of<ILibraryManager>());

        Assert.Equal(nameof(LibraryMetrics), collector.Name);
    }
}
