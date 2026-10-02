using System;
using System.Globalization;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Migrations.Routines;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// Covers the recalculation of the stored parental rating levels, including items that only inherit one.
/// </summary>
public sealed class MigrateRatingLevelsTests : IDisposable
{
    private const string SeriesType = "MediaBrowser.Controller.Entities.TV.Series";
    private const string SeasonType = "MediaBrowser.Controller.Entities.TV.Season";
    private const string EpisodeType = "MediaBrowser.Controller.Entities.TV.Episode";
    private const string FolderType = "MediaBrowser.Controller.Entities.Folder";
    private const string CollectionFolderType = "MediaBrowser.Controller.Entities.CollectionFolder";
    private const string AggregateFolderType = "MediaBrowser.Controller.Entities.AggregateFolder";
    private const string LibraryPath = "/media/shows";

    private static readonly Guid _libraryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _seriesId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid _seasonId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid _episodeId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid _rootId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid _collectionFolderId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly IApplicationPaths _applicationPaths;
    private readonly Mock<ILocalizationManager> _localizationManager;
    private readonly Mock<ILibraryManager> _libraryManager;

    public MigrateRatingLevelsTests()
    {
        _applicationPaths = new Mock<IApplicationPaths>().Object;

        _localizationManager = new Mock<ILocalizationManager>();
        _localizationManager.Setup(m => m.GetRatingScore("TV-14", null)).Returns(new ParentalRatingScore(14, null));
        _localizationManager.Setup(m => m.GetRatingScore("TV-MA", null)).Returns(new ParentalRatingScore(17, 1));
        _localizationManager.Setup(m => m.GetRatingScore("TV-14", "DE")).Returns(new ParentalRatingScore(16, null));
        _localizationManager.Setup(m => m.GetRatingScore("TV-14", "GB")).Returns(new ParentalRatingScore(15, null));

        _libraryManager = new Mock<ILibraryManager>();
        _libraryManager.Setup(m => m.GetVirtualFolders(false)).Returns([]);

        // The connection owns the in-memory database, so it stays open for the whole test.
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    [Fact]
    public void Perform_UnratedSeasonAndEpisode_InheritTheSeriesRating()
    {
        SeedSeries(seriesRating: "TV-MA", seriesCustomRating: null, inherited: null);

        Perform();

        using var context = CreateDbContext();
        AssertRating(context, _seriesId, 17, 1);
        AssertRating(context, _seasonId, 17, 1);
        AssertRating(context, _episodeId, 17, 1);
        AssertRating(context, _libraryId, null, null);
    }

    [Fact]
    public void Perform_AlreadyMigrated_KeepsTheInheritedRating()
    {
        // A second run over a database the first run and a scan already brought up to date.
        SeedSeries(seriesRating: "TV-MA", seriesCustomRating: null, inherited: new ParentalRatingScore(17, 1));

        Perform();

        using var context = CreateDbContext();
        AssertRating(context, _seasonId, 17, 1);
        AssertRating(context, _episodeId, 17, 1);
    }

    [Fact]
    public void Perform_CustomRatingOnSeries_WinsOverTheOfficialOne()
    {
        SeedSeries(seriesRating: "TV-14", seriesCustomRating: "TV-MA", inherited: null);

        Perform();

        using var context = CreateDbContext();
        AssertRating(context, _seriesId, 17, 1);
        AssertRating(context, _episodeId, 17, 1);
    }

    [Fact]
    public void Perform_UnratedSeries_ClearsTheInheritedRating()
    {
        SeedSeries(seriesRating: null, seriesCustomRating: null, inherited: new ParentalRatingScore(14, null));

        Perform();

        using var context = CreateDbContext();
        AssertRating(context, _seriesId, null, null);
        AssertRating(context, _episodeId, null, null);
    }

    [Fact]
    public void Perform_LibraryCountry_ScoresInheritedRatingsInThatCountry()
    {
        SeedSeries(seriesRating: "TV-14", seriesCustomRating: null, inherited: null);
        SeedLibrary(folderCountryCode: null, optionsCountryCode: "DE");

        Perform();

        using var context = CreateDbContext();
        AssertRating(context, _seriesId, 16, null);
        AssertRating(context, _episodeId, 16, null);
    }

    [Fact]
    public void Perform_CountryOnTheSeries_WinsOverTheLibraryCountry()
    {
        SeedSeries(seriesRating: "TV-14", seriesCustomRating: null, inherited: null, seriesCountryCode: "GB");
        SeedLibrary(folderCountryCode: "DE", optionsCountryCode: "DE");

        Perform();

        using var context = CreateDbContext();
        AssertRating(context, _seriesId, 15, null);
        AssertRating(context, _seasonId, 15, null);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private static void AssertRating(JellyfinDbContext context, Guid id, int? score, int? subScore)
    {
        var item = context.BaseItems.AsNoTracking().First(e => e.Id.Equals(id));
        Assert.Equal(score, item.InheritedParentalRatingValue);
        Assert.Equal(subScore, item.InheritedParentalRatingSubValue);
    }

    private void Perform()
    {
        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);

        new MigrateRatingLevels(factory.Object, new StartupLogger<MigrateRatingLevels>(NullLogger<MigrateRatingLevels>.Instance), _localizationManager.Object, _libraryManager.Object).Perform();
    }

    private JellyfinDbContext CreateDbContext() => new(
        _dbOptions,
        NullLogger<JellyfinDbContext>.Instance,
        new SqliteDatabaseProvider(_applicationPaths, NullLogger<SqliteDatabaseProvider>.Instance),
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

    private void SeedLibrary(string? folderCountryCode, string? optionsCountryCode)
    {
        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(new BaseItemEntity { Id = _rootId, Type = AggregateFolderType, Name = "root" });
            context.BaseItems.Add(new BaseItemEntity
            {
                Id = _collectionFolderId,
                Type = CollectionFolderType,
                Name = "Shows",
                Path = "/config/root/default/Shows",
                PreferredMetadataCountryCode = folderCountryCode
            });
            context.SaveChanges();
            context.BaseItems.Where(e => e.Id.Equals(_libraryId)).ExecuteUpdate(f => f.SetProperty(e => e.ParentId, _rootId));
        }

        _libraryManager.Setup(m => m.GetVirtualFolders(false)).Returns(
        [
            new VirtualFolderInfo
            {
                Name = "Shows",
                ItemId = _collectionFolderId.ToString("N", CultureInfo.InvariantCulture),
                Locations = [LibraryPath],
                LibraryOptions = new LibraryOptions { MetadataCountryCode = optionsCountryCode }
            }
        ]);
    }

    private void SeedSeries(string? seriesRating, string? seriesCustomRating, ParentalRatingScore? inherited, string? seriesCountryCode = null)
    {
        using var context = CreateDbContext();

        context.BaseItems.Add(new BaseItemEntity { Id = _libraryId, Type = FolderType, Name = "Shows", Path = LibraryPath });
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = _seriesId,
            Type = SeriesType,
            Name = "Series",
            ParentId = _libraryId,
            OfficialRating = seriesRating,
            CustomRating = seriesCustomRating,
            PreferredMetadataCountryCode = seriesCountryCode,
            InheritedParentalRatingValue = inherited?.Score,
            InheritedParentalRatingSubValue = inherited?.SubScore
        });
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = _seasonId,
            Type = SeasonType,
            Name = "Season 1",
            ParentId = _seriesId,
            SeriesId = _seriesId,
            InheritedParentalRatingValue = inherited?.Score,
            InheritedParentalRatingSubValue = inherited?.SubScore
        });

        // The episode's display parent is its season, not the folder it was found in.
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = _episodeId,
            Type = EpisodeType,
            Name = "Episode 1",
            ParentId = _libraryId,
            SeasonId = _seasonId,
            SeriesId = _seriesId,
            OfficialRating = string.Empty,
            InheritedParentalRatingValue = inherited?.Score,
            InheritedParentalRatingSubValue = inherited?.SubScore
        });

        context.SaveChanges();
    }
}
