using System;
using System.Threading.Tasks;
using Jellyfin.Api.Controllers;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class ItemUpdateControllerTests
{
    private readonly ItemUpdateController _subject;

    public ItemUpdateControllerTests()
    {
        _subject = new ItemUpdateController(
            Mock.Of<IFileSystem>(),
            Mock.Of<ILibraryManager>(),
            Mock.Of<IProviderManager>(),
            Mock.Of<ILocalizationManager>(),
            Mock.Of<IServerConfigurationManager>());
    }

    [Fact]
    public async Task UpdateItem_WhenOnlyTagsFieldSupplied_DoesNotThrowAndAppliesTags()
    {
        // Regression test for https://github.com/jellyfin/jellyfin/issues/17366
        // A partial update payload that only sets "Tags" leaves every other
        // BaseItemDto collection property null (they have no default
        // initializer). Genres and ProviderIds used to be fed straight into
        // Distinct()/ToList() without a null check, so this call used to throw
        // ArgumentNullException before the fix below was applied.
        var movie = new Movie();
        var request = new BaseItemDto
        {
            Tags = new[] { "new-tag-1", "new-tag-2" }
        };

        await InvokeUpdateItem(request, movie);

        Assert.Equal(new[] { "new-tag-1", "new-tag-2" }, movie.Tags);
        Assert.Empty(movie.Genres);
        Assert.Empty(movie.ProviderIds);
    }

    [Fact]
    public async Task UpdateItem_WhenGenresAndProviderIdsOmitted_LeavesExistingValuesUnchanged()
    {
        var movie = new Movie
        {
            Genres = new[] { "Action" }
        };
        movie.ProviderIds["Imdb"] = "tt1234567";

        var request = new BaseItemDto
        {
            Tags = Array.Empty<string>()
        };

        await InvokeUpdateItem(request, movie);

        Assert.Equal(new[] { "Action" }, movie.Genres);
        Assert.Equal("tt1234567", movie.ProviderIds["Imdb"]);
    }

    private Task InvokeUpdateItem(BaseItemDto request, BaseItem item)
    {
        return _subject.UpdateItem(request, item);
    }
}
