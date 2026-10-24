using System;
using System.Globalization;
using System.Threading.Tasks;
using Emby.Server.Implementations.Localization;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers the language name in the display titles of the streams read by <see cref="MediaStreamRepository"/>.
/// </summary>
public sealed class MediaStreamRepositoryTests : SqliteDbTestFixture
{
    private readonly Guid _itemId = Guid.NewGuid();

    [Theory]
    [InlineData("eng", "English SDH", "English SDH - AAC")]
    [InlineData("spa", "Español Latino", "Español Latino - AAC")]
    [InlineData("spa", "Pista 1", "Pista 1 - Español - AAC")]
    [InlineData("spa", null, "Español - AAC")]
    public async Task GetMediaStreams_SpanishUICulture_NamesLanguageOnce(string language, string? title, string expected)
    {
        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(new BaseItemEntity { Id = _itemId, Type = "MediaBrowser.Controller.Entities.Movies.Movie", Name = "Movie" });
            context.MediaStreamInfos.Add(new MediaStreamInfo
            {
                ItemId = _itemId,
                StreamIndex = 0,
                StreamType = MediaStreamTypeEntity.Audio,
                Codec = "aac",
                Language = language,
                Title = title,
                Item = null!
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var configuration = new Mock<IServerConfigurationManager>();
        configuration.SetupGet(x => x.Configuration).Returns(new ServerConfiguration());
        var localizationManager = new LocalizationManager(configuration.Object, NullLogger<LocalizationManager>.Instance);
        await localizationManager.LoadAll();
        var repository = new MediaStreamRepository(CreateDbContextFactory(), Mock.Of<IServerApplicationHost>(), localizationManager);

        var previousCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("es");
            var stream = Assert.Single(repository.GetMediaStreams(new MediaStreamQuery { ItemId = _itemId }));
            Assert.Equal(expected, stream.DisplayTitle);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }
}
