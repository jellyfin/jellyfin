using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.CustomNetflix;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixManualMediaSegmentProviderTests
{
    [Fact]
    public async Task GetMediaSegments_MapsManualRowsAfterSchemaIsReady()
    {
        var itemId = Guid.NewGuid();
        var repository = new Mock<ICustomNetflixRepository>();
        repository.SetupGet(mock => mock.IsEnabled).Returns(true);
        repository
            .Setup(mock => mock.GetManualMediaSegmentsAsync(itemId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new CustomMediaSegmentRow(
                    Guid.NewGuid(),
                    itemId,
                    "intro",
                    10,
                    20,
                    CustomNetflixManualSegmentPolicy.ManualSource,
                    DateTime.UtcNow)
            ]);
        var schemaState = new CustomNetflixSchemaState();
        var provider = new CustomNetflixManualMediaSegmentProvider(repository.Object, schemaState);
        var item = new Movie();

        Assert.False(await provider.Supports(item));
        schemaState.MarkReady();
        Assert.True(await provider.Supports(item));

        var segments = await provider.GetMediaSegments(
            new MediaSegmentGenerationRequest { ItemId = itemId, ExistingSegments = [] },
            TestContext.Current.CancellationToken);

        var segment = Assert.Single(segments);
        Assert.Equal(MediaSegmentType.Intro, segment.Type);
        Assert.Equal(TimeSpan.FromSeconds(10).Ticks, segment.StartTicks);
        Assert.Equal(TimeSpan.FromSeconds(20).Ticks, segment.EndTicks);
        Assert.True(provider.OverridesOtherProviders);
    }
}
