using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Jellyfin.Extensions;
using Jellyfin.Server.Implementations;
using Jellyfin.Server.Migrations.Routines;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// The legacy library database did not limit the length of its text columns, so it can hold values that are longer
/// than the columns of the new database. The import stores them cut, normalised or not at all.
/// </summary>
public sealed class MigrateLibraryDbKeyLengthTests : DatabaseTestFixture
{
    private const string TypedBaseItemsTable =
        """
        CREATE TABLE TypedBaseItems (guid, type, data, StartDate, EndDate, ChannelId, IsMovie,
        IsSeries, EpisodeTitle, IsRepeat, CommunityRating, CustomRating, IndexNumber, IsLocked, PreferredMetadataLanguage,
        PreferredMetadataCountryCode, Width, Height, DateLastRefreshed, Name, Path, PremiereDate, Overview, ParentIndexNumber,
        ProductionYear, OfficialRating, ForcedSortName, RunTimeTicks, Size, DateCreated, DateModified, Genres, ParentId, TopParentId,
        Audio, ExternalServiceId, IsInMixedFolder, DateLastSaved, LockedFields, Studios, Tags, TrailerTypes, OriginalTitle, PrimaryVersionId,
        DateLastMediaAdded, Album, LUFS, NormalizationGain, CriticRating, IsVirtualItem, SeriesName, UserDataKey, SeasonName, SeasonId, SeriesId,
        PresentationUniqueKey, InheritedParentalRatingValue, ExternalSeriesId, Tagline, ProviderIds, Images, ProductionLocations, ExtraIds, TotalBitrate,
        ExtraType, Artists, AlbumArtists, ExternalId, SeriesPresentationUniqueKey, ShowId, OwnerId, MediaType, SortName, CleanName, UnratedType, IsFolder)
        """;

    private const string MediaStreamsTable =
        """
        CREATE TABLE MediaStreams (ItemId, StreamIndex, StreamType, Codec, Language, ChannelLayout, Profile, AspectRatio, Path,
        IsInterlaced, BitRate, Channels, SampleRate, IsDefault, IsForced, IsExternal, Height, Width,
        AverageFrameRate, RealFrameRate, Level, PixelFormat, BitDepth, IsAnamorphic, RefFrames, CodecTag,
        Comment, NalLengthSize, IsAvc, Title, TimeBase, CodecTimeBase, ColorPrimaries, ColorSpace, ColorTransfer,
        DvVersionMajor, DvVersionMinor, DvProfile, DvLevel, RpuPresentFlag, ElPresentFlag, BlPresentFlag, DvBlSignalCompatibilityId, IsHearingImpaired)
        """;

    private static readonly Guid _firstItemId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _secondItemId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly string _dataPath = Path.Combine(AppContext.BaseDirectory, "jellyfin-library-db-tests-" + Guid.NewGuid().ToString("N"));
    private readonly CapturingLogger<MigrateLibraryDb> _log = new();

    public MigrateLibraryDbKeyLengthTests()
    {
        Directory.CreateDirectory(_dataPath);
    }

    [Fact]
    public void Perform_ValuesLongerThanTheirColumns_AreStoredWithinTheBounds()
    {
        var userDataKey = new string('k', KeyNormalizer.UserDataKeyMaxLength + 44);
        var role = new string('r', KeyNormalizer.PersonRoleMaxLength - 1) + " " + new string('z', 50);
        var valuePrefix = new string('v', KeyNormalizer.ItemValueMaxLength);
        var longProviderId = new string('p', KeyNormalizer.ProviderIdMaxLength + 72);
        long internalUserId;
        using (var ctx = CreateDbContext())
        {
            var user = new User("user", "auth", "reset") { Id = Guid.NewGuid(), InternalId = 1 };
            internalUserId = user.InternalId;
            ctx.Users.Add(user);
            ctx.SaveChanges();
        }

        CreateLibraryDb(
            [
                Item(_firstItemId, userDataKey, $"Imdb=tt0001|{longProviderId}=value"),
                Item(_secondItemId, "tt0002", null)
            ],
            itemValues:
            [
                (_firstItemId, valuePrefix + "first"),
                (_secondItemId, valuePrefix + "second")
            ],
            people: [(_firstItemId, "Some Person", role)],
            userDatas: [(userDataKey, internalUserId)]);

        CreateMigration().Perform();

        using var context = CreateDbContext();
        Assert.Equal(2, context.BaseItems.Count());

        var itemValue = Assert.Single(context.ItemValues.AsNoTracking().ToList());
        Assert.Equal(valuePrefix, itemValue.Value);
        Assert.Equal(valuePrefix.GetCleanValue(), itemValue.CleanValue);
        Assert.Equal(
            new[] { _firstItemId, _secondItemId }.Order(),
            context.ItemValuesMap.AsNoTracking().Select(e => e.ItemId).ToList().Order());

        var map = Assert.Single(context.PeopleBaseItemMap.AsNoTracking().ToList());
        Assert.Equal(new string('r', KeyNormalizer.PersonRoleMaxLength - 1), map.Role);

        var userData = Assert.Single(context.UserData.AsNoTracking().ToList());
        Assert.Equal(KeyNormalizer.NormalizeUserDataKey(userDataKey), userData.CustomDataKey);
        Assert.Equal(_firstItemId, userData.ItemId);

        var provider = Assert.Single(context.BaseItemProviders.AsNoTracking().ToList());
        Assert.Equal("Imdb", provider.ProviderId);
        Assert.Contains(
            _log.Entries,
            e => e.Level == LogLevel.Warning && e.Message.Contains($"{longProviderId.Length} characters long", StringComparison.Ordinal));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReseedFolderFlag.RerunGuardFlag = false;
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(_dataPath))
            {
                Directory.Delete(_dataPath, true);
            }
        }

        base.Dispose(disposing);
    }

    private static (Guid Id, string UserDataKey, string? ProviderIds) Item(Guid id, string userDataKey, string? providerIds)
        => (id, userDataKey, providerIds);

    private MigrateLibraryDb CreateMigration()
    {
        var paths = new Mock<IServerApplicationPaths>();
        paths.Setup(e => e.DataPath).Returns(_dataPath);

        return new MigrateLibraryDb(
            new StartupLogger<MigrateLibraryDb>(_log),
            CreateDbContextFactory(),
            paths.Object,
            Mock.Of<IJellyfinDatabaseProvider>());
    }

    private void CreateLibraryDb(
        IReadOnlyList<(Guid Id, string UserDataKey, string? ProviderIds)> items,
        IReadOnlyList<(Guid ItemId, string Value)> itemValues,
        IReadOnlyList<(Guid ItemId, string Name, string Role)> people,
        IReadOnlyList<(string Key, long UserId)> userDatas)
    {
#pragma warning disable RS0030 // Do not use banned APIs: The file to import is the legacy SQLite library database whichever provider is selected.
        using var connection = new SqliteConnection($"Filename={Path.Combine(_dataPath, "library.db")};Pooling=False");
#pragma warning restore RS0030
        connection.Open();

        Execute(connection, TypedBaseItemsTable);
        Execute(connection, "CREATE TABLE ItemValues (ItemId, Type, Value, CleanValue)");
        Execute(connection, "CREATE TABLE UserDatas (key, userId, rating, played, playCount, isFavorite, playbackPositionTicks, lastPlayedDate, AudioStreamIndex, SubtitleStreamIndex)");
        Execute(connection, "CREATE TABLE People (ItemId, Name, Role, PersonType, SortOrder, ListOrder)");
        Execute(connection, "CREATE TABLE Chapters2 (ItemId, StartPositionTicks, Name, ImagePath, ImageDateModified, ChapterIndex)");
        Execute(connection, "CREATE TABLE AncestorIds (ItemId, AncestorId, AncestorIdText)");
        Execute(connection, MediaStreamsTable);
        Execute(connection, "CREATE TABLE mediaattachments (ItemId, AttachmentIndex, Codec, CodecTag, Comment, filename, MIMEType)");

        foreach (var item in items)
        {
            Execute(
                connection,
                "INSERT INTO TypedBaseItems (guid, type, UserDataKey, ProviderIds, Name, IsFolder) VALUES ($id, $type, $key, $providers, $name, 0)",
                ("$id", item.Id.ToString()),
                ("$type", typeof(Folder).FullName),
                ("$key", item.UserDataKey),
                ("$providers", item.ProviderIds),
                ("$name", "Item"));
        }

        foreach (var itemValue in itemValues)
        {
            Execute(
                connection,
                "INSERT INTO ItemValues (ItemId, Type, Value, CleanValue) VALUES ($id, 2, $value, $clean)",
                ("$id", itemValue.ItemId.ToString()),
                ("$value", itemValue.Value),
                ("$clean", itemValue.Value.ToLowerInvariant()));
        }

        foreach (var person in people)
        {
            Execute(
                connection,
                "INSERT INTO People (ItemId, Name, Role, PersonType, SortOrder, ListOrder) VALUES ($id, $name, $role, 'Actor', 0, 0)",
                ("$id", person.ItemId.ToString()),
                ("$name", person.Name),
                ("$role", person.Role));
        }

        foreach (var userData in userDatas)
        {
            Execute(
                connection,
                "INSERT INTO UserDatas (key, userId, played, playCount, isFavorite, playbackPositionTicks) VALUES ($key, $user, 1, 1, 0, 0)",
                ("$key", userData.Key),
                ("$user", userData.UserId));
        }
    }

    private static void Execute(SqliteConnection connection, string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
#pragma warning disable CA2100 // The statements are fixed in this file and their values are parameters.
        command.CommandText = sql;
#pragma warning restore CA2100
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        command.ExecuteNonQuery();
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }
}
