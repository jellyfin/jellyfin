using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.Audio;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class SimilarityProvidersTests
{
    [Fact]
    public async Task GetSimilarItems_WithValidProvider_ReturnsProviderResults()
    {
        // Arrange
        var item = new Audio { Id = Guid.NewGuid(), Name = "Test Song" };
        var expectedIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var provider = new MockSimilarityProvider(expectedIds);

        // Act
        var result = await provider.GetSimilarItems(item, 10, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedIds, result);
    }

    [Fact]
    public async Task GetSimilarItems_WithProviderReturningEmpty_ReturnsEmpty()
    {
        // Arrange
        var item = new Audio { Id = Guid.NewGuid(), Name = "Test Song" };
        var provider = new MockSimilarityProvider(Array.Empty<Guid>());

        // Act
        var result = await provider.GetSimilarItems(item, 10, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSimilarItems_WithProviderThrowing_Throws()
    {
        // Arrange
        var item = new Audio { Id = Guid.NewGuid(), Name = "Test Song" };
        var provider = new FailingSimilarityProvider();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetSimilarItems(item, 10, CancellationToken.None));
    }
}
