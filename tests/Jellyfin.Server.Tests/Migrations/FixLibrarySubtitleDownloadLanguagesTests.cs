using System;
using System.Threading.Tasks;
using Emby.Server.Implementations.Localization;
using Jellyfin.Server.Migrations.Routines;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

public class FixLibrarySubtitleDownloadLanguagesTests
{
    [Theory]
    // 12.0 stored every variant as its base code, the metadata language tells which one was picked.
    [InlineData("por", "pt-br", "en", "pt-br")]
    [InlineData("por", "pt-pt", "en", "pt-pt")]
    [InlineData("por", null, "pt-br", "pt-br")]
    [InlineData("por", "en", "pt-br", "por")]
    [InlineData("por", "pt", "en", "por")]
    [InlineData("spa", "es-419", "en", "es-419")]
    [InlineData("spa", "es", "en", "spa")]
    [InlineData("chi", "zh-tw", "en", "zh-tw")]
    // Explicit values are never overridden by the metadata language.
    [InlineData("pt", "pt-br", "en", "por")]
    [InlineData("pob", "pt-pt", "en", "pt-br")]
    [InlineData("pt-pt", "pt-br", "en", "pt-pt")]
    [InlineData("ger", "de", "en", "deu")]
    [InlineData("xyz", "pt-br", "en", "xyz")]
    public async Task PerformAsync_InfersRegionalVariantFromMetadataLanguage(string stored, string? libraryLanguage, string serverLanguage, string expected)
    {
        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.SetupGet(x => x.Configuration).Returns(new ServerConfiguration
        {
            UICulture = "en-US",
            PreferredMetadataLanguage = serverLanguage
        });
        var localizationManager = new LocalizationManager(configurationManager.Object, NullLogger<LocalizationManager>.Instance);
        await localizationManager.LoadAll();

        var folderId = Guid.NewGuid();
        var options = new LibraryOptions
        {
            PreferredMetadataLanguage = libraryLanguage,
            SubtitleDownloadLanguages = [stored]
        };
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(x => x.GetVirtualFolders(false)).Returns(
        [
            new VirtualFolderInfo { Name = "Movies", ItemId = folderId.ToString(), LibraryOptions = options }
        ]);
        libraryManager.Setup(x => x.GetItemById<CollectionFolder>(folderId)).Returns(new CollectionFolder { Path = "/config/root/movies" });
        CollectionFolder.XmlSerializer = Mock.Of<IXmlSerializer>();

        var migration = new FixLibrarySubtitleDownloadLanguages(
            localizationManager,
            configurationManager.Object,
            new StartupLogger<FixLibrarySubtitleDownloadLanguages>(NullLogger<FixLibrarySubtitleDownloadLanguages>.Instance),
            libraryManager.Object,
            NullLogger<FixLibrarySubtitleDownloadLanguages>.Instance);
        await migration.PerformAsync(TestContext.Current.CancellationToken);

        Assert.Equal([expected], options.SubtitleDownloadLanguages);
    }
}
