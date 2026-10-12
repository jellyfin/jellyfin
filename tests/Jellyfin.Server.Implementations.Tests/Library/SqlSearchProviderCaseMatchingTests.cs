using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Data;
using Emby.Server.Implementations.Library.Search;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Tests.Item;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using Moq;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Library;

/// <summary>
/// Items found through <c>OriginalTitle</c> or <c>SortName</c>, which are stored in the case the metadata
/// provider wrote them in. The match ignores ASCII case only, treats <c>\</c> as an ordinary character and
/// leaves <c>%</c> and <c>_</c> as wildcards.
/// </summary>
public sealed class SqlSearchProviderCaseMatchingTests : JellyfinDbTestFixture
{
    private readonly SqlSearchProvider _provider;

    public SqlSearchProviderCaseMatchingTests()
    {
        var itemTypeLookup = new ItemTypeLookup();
        var movieTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie]!;

        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(CreateMovie(movieTypeName, "Item One", originalTitle: "hello World"));
            context.BaseItems.Add(CreateMovie(movieTypeName, "Item Two", originalTitle: "Émile Zola"));
            context.BaseItems.Add(CreateMovie(movieTypeName, "Item Three", originalTitle: "AC\\DC"));
            context.BaseItems.Add(CreateMovie(movieTypeName, "Item Four", originalTitle: "ACDC"));
            context.BaseItems.Add(CreateMovie(movieTypeName, "Item Five", originalTitle: "Alpha Omega"));
            context.BaseItems.Add(CreateMovie(movieTypeName, "Item Six", originalTitle: "Beta"));
            context.BaseItems.Add(CreateMovie(movieTypeName, "Item Seven", sortName: "Mixed Sort Name"));
            context.SaveChanges();
        }

        var serverConfigurationManager = new Mock<IServerConfigurationManager>();
        serverConfigurationManager.Setup(scm => scm.Configuration)
            .Returns(new ServerConfiguration());

        _provider = new SqlSearchProvider(
            CreateDbContextFactory(),
            itemTypeLookup,
            new Mock<ILibraryManager>().Object,
            new Mock<IUserManager>().Object,
            CreateBaseItemRepository(itemTypeLookup),
            serverConfigurationManager.Object);
    }

    [Theory]
    [InlineData("WORLD")]
    [InlineData("world")]
    [InlineData("wOrLd")]
    public async Task SearchAsync_OriginalTitleInMixedCase_IgnoresAsciiCase(string term)
    {
        Assert.Equal(["hello World"], await SearchAsync(term).ConfigureAwait(true));
    }

    [Theory]
    [InlineData("SORT")]
    [InlineData("sort")]
    public async Task SearchAsync_SortNameInMixedCase_IgnoresAsciiCase(string term)
    {
        Assert.Equal(["Mixed Sort Name"], await SearchAsync(term).ConfigureAwait(true));
    }

    [Fact]
    public async Task SearchAsync_BackslashInTerm_IsLiteral()
    {
        Assert.Equal(["AC\\DC"], await SearchAsync("AC\\DC").ConfigureAwait(true));
    }

    [Theory]
    [InlineData("Al%ga", "Alpha Omega")]
    [InlineData("Al_ha", "Alpha Omega")]
    [InlineData("%eta", "Beta")]
    public async Task SearchAsync_PercentAndUnderscoreInTerm_StayWildcards(string term, string expected)
    {
        Assert.Equal([expected], await SearchAsync(term).ConfigureAwait(true));
    }

    private async Task<List<string>> SearchAsync(string term)
    {
        var results = await _provider.SearchAsync(
            new SearchProviderQuery { SearchTerm = term, Limit = 10 },
            CancellationToken.None).ConfigureAwait(false);

        var ids = results.Select(r => r.ItemId).ToList();
        using var context = CreateDbContext();
        return context.BaseItems
            .Where(e => ids.Contains(e.Id))
            .Select(e => e.OriginalTitle ?? e.SortName ?? string.Empty)
            .AsEnumerable()
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    private static BaseItemEntity CreateMovie(string typeName, string name, string? originalTitle = null, string? sortName = null)
    {
        var id = Guid.NewGuid();
        return new BaseItemEntity
        {
            Id = id,
            Type = typeName,
            Name = name,
            CleanName = name.ToLowerInvariant(),
            SortName = sortName ?? name.ToLowerInvariant(),
            OriginalTitle = originalTitle,
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false,
            PresentationUniqueKey = id.ToString("N")
        };
    }
}
