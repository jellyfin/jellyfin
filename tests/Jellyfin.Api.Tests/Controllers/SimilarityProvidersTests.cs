using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
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
}

public class MockSimilarityProvider : IItemSimilarityProvider<Audio>
{
    private readonly IEnumerable<Guid> _results;

    public MockSimilarityProvider(IEnumerable<Guid> results)
    {
        _results = results;
    }

    public string Name => "MockProvider";

    public Task<IEnumerable<Guid>> GetSimilarItems(
        Audio item,
        int limit,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_results.Take(limit));
    }
}
