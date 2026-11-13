using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Library.Search;
using Jellyfin.Database.Implementations;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class SearchManagerTests
{
    private static readonly SearchProviderQuery _query = new() { SearchTerm = "seven" };

    [Fact]
    public async Task GetSearchResultsAsync_ExternalResults_SkipsInternalProviders()
    {
        var externalId = Guid.NewGuid();
        var external = ExternalProvider(new SearchResult(externalId, 1f));
        var @internal = InternalProvider(new SearchResult(Guid.NewGuid(), 1f));
        var manager = CreateManager(external.Object, @internal.Object);

        var results = await manager.GetSearchResultsAsync(_query, TestContext.Current.CancellationToken);

        Assert.Equal(new[] { externalId }, results.Select(r => r.ItemId));
        @internal.Verify(p => p.SearchAsync(It.IsAny<SearchProviderQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetSearchResultsAsync_NoExternalResults_UsesInternalProviders()
    {
        var internalId = Guid.NewGuid();
        var external = ExternalProvider();
        var @internal = InternalProvider(new SearchResult(internalId, 1f));
        var manager = CreateManager(external.Object, @internal.Object);

        var results = await manager.GetSearchResultsAsync(_query, TestContext.Current.CancellationToken);

        Assert.Equal(new[] { internalId }, results.Select(r => r.ItemId));
    }

    [Fact]
    public async Task GetSearchResultsAsync_NoExternalProviders_UsesInternalProviders()
    {
        var internalId = Guid.NewGuid();
        var @internal = InternalProvider(new SearchResult(internalId, 1f));
        var manager = CreateManager(@internal.Object);

        var results = await manager.GetSearchResultsAsync(_query, TestContext.Current.CancellationToken);

        Assert.Equal(new[] { internalId }, results.Select(r => r.ItemId));
    }

    [Fact]
    public async Task GetSearchResultsAsync_NoProviderResults_ReturnsEmpty()
    {
        var manager = CreateManager(ExternalProvider().Object, InternalProvider().Object);

        var results = await manager.GetSearchResultsAsync(_query, TestContext.Current.CancellationToken);

        Assert.Empty(results);
    }

    private static SearchManager CreateManager(params ISearchProvider[] providers)
    {
        var manager = new SearchManager(
            Mock.Of<ILibraryManager>(),
            Mock.Of<IUserManager>(),
            Mock.Of<IDbContextFactory<JellyfinDbContext>>(),
            Mock.Of<IItemQueryHelpers>(),
            NullLogger<SearchManager>.Instance);
        manager.AddParts(providers);
        return manager;
    }

    private static Mock<IExternalSearchProvider> ExternalProvider(params SearchResult[] results)
    {
        var provider = new Mock<IExternalSearchProvider>();
        provider.Setup(p => p.Name).Returns("external");
        provider.Setup(p => p.CanSearch(It.IsAny<SearchProviderQuery>())).Returns(true);
        provider.Setup(p => p.SearchAsync(It.IsAny<SearchProviderQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() => results.ToAsyncEnumerable());
        return provider;
    }

    private static Mock<IInternalSearchProvider> InternalProvider(params SearchResult[] results)
    {
        var provider = new Mock<IInternalSearchProvider>();
        provider.Setup(p => p.Name).Returns("internal");
        provider.Setup(p => p.CanSearch(It.IsAny<SearchProviderQuery>())).Returns(true);
        provider.Setup(p => p.SearchAsync(It.IsAny<SearchProviderQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SearchResult>)results);
        return provider;
    }
}
