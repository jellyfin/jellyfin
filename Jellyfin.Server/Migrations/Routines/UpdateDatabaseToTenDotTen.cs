using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Updates the library.db to version 10.10.z; replaces the migration code from the old SqliteItemRepository and SqliteUserDataRepository.
/// </summary>
[JellyfinMigration("2025-04-19T00:00:00", nameof(UpdateDatabaseToTenDotTen))]
internal class UpdateDatabaseToTenDotTen : IDatabaseMigrationRoutine
{
    private const string DbFilename = "library.db";
    private readonly ILogger<UpdateDatabaseToTenDotTen> _logger;
    private readonly IServerApplicationPaths _paths;
    private readonly IUserManager _userManager;

    public UpdateDatabaseToTenDotTen(
        ILogger<UpdateDatabaseToTenDotTen> logger,
        IServerApplicationPaths paths,
        IUserManager userManager)
    {
        _logger = logger;
        _paths = paths;
        _userManager = userManager;
    }

    /// <inheritdoc/>
    public void Perform()
    {
        const string ChaptersTableName = "Chapters2";

        string[] queries =
        [
            "create table if not exists TypedBaseItems (guid GUID primary key NOT NULL, type TEXT NOT NULL, data BLOB NULL, ParentId GUID NULL, Path TEXT NULL)",

            "create table if not exists AncestorIds (ItemId GUID NOT NULL, AncestorId GUID NOT NULL, AncestorIdText TEXT NOT NULL, PRIMARY KEY (ItemId, AncestorId))",

            "create table if not exists ItemValues (ItemId GUID NOT NULL, Type INT NOT NULL, Value TEXT NOT NULL, CleanValue TEXT NOT NULL)",

            "create table if not exists People (ItemId GUID, Name TEXT NOT NULL, Role TEXT, PersonType TEXT, SortOrder int, ListOrder int)",

            "create table if not exists " + ChaptersTableName + " (ItemId GUID, ChapterIndex INT NOT NULL, StartPositionTicks BIGINT NOT NULL, Name TEXT, ImagePath TEXT, PRIMARY KEY (ItemId, ChapterIndex))",

            "create table if not exists mediastreams (ItemId GUID, StreamIndex INT, StreamType TEXT, Codec TEXT, Language TEXT, ChannelLayout TEXT, Profile TEXT, AspectRatio TEXT, Path TEXT, IsInterlaced BIT, BitRate INT NULL, Channels INT NULL, SampleRate INT NULL, IsDefault BIT, IsForced BIT, IsExternal BIT, Height INT NULL, Width INT NULL, AverageFrameRate FLOAT NULL, RealFrameRate FLOAT NULL, Level FLOAT NULL, PixelFormat TEXT, BitDepth INT NULL, IsAnamorphic BIT NULL, RefFrames INT NULL, CodecTag TEXT NULL, Comment TEXT NULL, NalLengthSize TEXT NULL, IsAvc BIT NULL, Title TEXT NULL, TimeBase TEXT NULL, CodecTimeBase TEXT NULL, ColorPrimaries TEXT NULL, ColorSpace TEXT NULL, ColorTransfer TEXT NULL, DvVersionMajor INT NULL, DvVersionMinor INT NULL, DvProfile INT NULL, DvLevel INT NULL, RpuPresentFlag INT NULL, ElPresentFlag INT NULL, BlPresentFlag INT NULL, DvBlSignalCompatibilityId INT NULL, IsHearingImpaired BIT NULL, Rotation INT NULL, PRIMARY KEY (ItemId, StreamIndex))",
            "create table if not exists mediaattachments (ItemId GUID, AttachmentIndex INT, Codec TEXT, CodecTag TEXT NULL, Comment TEXT NULL, Filename TEXT NULL, MIMEType TEXT NULL, PRIMARY KEY (ItemId, AttachmentIndex))",

            "pragma shrink_memory"
        ];

        var dataPath = _paths.DataPath;

        var dbPath = Path.Combine(dataPath, DbFilename);

        // Back up the database before making any changes
        for (var i = 1; ; i++)
        {
            var bakPath = string.Format(CultureInfo.InvariantCulture, "{0}.bak{1}", dbPath, i);
            if (!File.Exists(bakPath))
            {
                try
                {
                    File.Copy(dbPath, bakPath);
                    _logger.LogInformation("Library database backed up to {BackupPath}", bakPath);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cannot make a backup of {Library} at path {BackupPath}", DbFilename, bakPath);
                    throw;
                }
            }
        }

        _logger.LogInformation("Prepare database for EFCore migration: update to version 10.10.z");

        using var connection = new SqliteConnection($"Filename={dbPath}");
        connection.Open();
        using (var transaction = connection.BeginTransaction())
        {
            connection.Execute(string.Join(';', queries));

            var existingColumnNames = GetColumnNames(connection, "AncestorIds");
            AddColumn(connection, "AncestorIds", "AncestorIdText", "Text", existingColumnNames);

            existingColumnNames = GetColumnNames(connection, "TypedBaseItems");
            AddColumn(connection, "TypedBaseItems", "Path", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "StartDate", "DATETIME", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "EndDate", "DATETIME", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "ChannelId", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "IsMovie", "BIT", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "CommunityRating", "Float", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "CustomRating", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "IndexNumber", "INT", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "IsLocked", "BIT", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "Name", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "OfficialRating", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "MediaType", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "Overview", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "ParentIndexNumber", "INT", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "PremiereDate", "DATETIME", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "ProductionYear", "INT", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "ParentId", "GUID", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "Genres", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "SortName", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "ForcedSortName", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "RunTimeTicks", "BIGINT", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "DateCreated", "DATETIME", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "DateModified", "DATETIME", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "IsSeries", "BIT", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "EpisodeTitle", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "IsRepeat", "BIT", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "PreferredMetadataLanguage", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "PreferredMetadataCountryCode", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "DateLastRefreshed", "DATETIME", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "DateLastSaved", "DATETIME", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "IsInMixedFolder", "BIT", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "LockedFields", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "Studios", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "Audio", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "ExternalServiceId", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "Tags", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "IsFolder", "BIT", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "InheritedParentalRatingValue", "INT", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "UnratedType", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "TopParentId", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "TrailerTypes", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "CriticRating", "Float", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "CleanName", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "PresentationUniqueKey", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "OriginalTitle", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "PrimaryVersionId", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "DateLastMediaAdded", "DATETIME", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "Album", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "LUFS", "Float", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "NormalizationGain", "Float", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "IsVirtualItem", "BIT", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "SeriesName", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "UserDataKey", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "SeasonName", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "SeasonId", "GUID", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "SeriesId", "GUID", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "ExternalSeriesId", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "Tagline", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "ProviderIds", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "Images", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "ProductionLocations", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "ExtraIds", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "TotalBitrate", "INT", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "ExtraType", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "Artists", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "AlbumArtists", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "ExternalId", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "SeriesPresentationUniqueKey", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "ShowId", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "OwnerId", "Text", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "Width", "INT", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "Height", "INT", existingColumnNames);
            AddColumn(connection, "TypedBaseItems", "Size", "BIGINT", existingColumnNames);

            existingColumnNames = GetColumnNames(connection, "ItemValues");
            AddColumn(connection, "ItemValues", "CleanValue", "Text", existingColumnNames);

            existingColumnNames = GetColumnNames(connection, ChaptersTableName);
            AddColumn(connection, ChaptersTableName, "ImageDateModified", "DATETIME", existingColumnNames);

            existingColumnNames = GetColumnNames(connection, "MediaStreams");
            AddColumn(connection, "MediaStreams", "IsAvc", "BIT", existingColumnNames);
            AddColumn(connection, "MediaStreams", "TimeBase", "TEXT", existingColumnNames);
            AddColumn(connection, "MediaStreams", "CodecTimeBase", "TEXT", existingColumnNames);
            AddColumn(connection, "MediaStreams", "Title", "TEXT", existingColumnNames);
            AddColumn(connection, "MediaStreams", "NalLengthSize", "TEXT", existingColumnNames);
            AddColumn(connection, "MediaStreams", "Comment", "TEXT", existingColumnNames);
            AddColumn(connection, "MediaStreams", "CodecTag", "TEXT", existingColumnNames);
            AddColumn(connection, "MediaStreams", "PixelFormat", "TEXT", existingColumnNames);
            AddColumn(connection, "MediaStreams", "BitDepth", "INT", existingColumnNames);
            AddColumn(connection, "MediaStreams", "RefFrames", "INT", existingColumnNames);
            AddColumn(connection, "MediaStreams", "KeyFrames", "TEXT", existingColumnNames);
            AddColumn(connection, "MediaStreams", "IsAnamorphic", "BIT", existingColumnNames);

            AddColumn(connection, "MediaStreams", "ColorPrimaries", "TEXT", existingColumnNames);
            AddColumn(connection, "MediaStreams", "ColorSpace", "TEXT", existingColumnNames);
            AddColumn(connection, "MediaStreams", "ColorTransfer", "TEXT", existingColumnNames);

            AddColumn(connection, "MediaStreams", "DvVersionMajor", "INT", existingColumnNames);
            AddColumn(connection, "MediaStreams", "DvVersionMinor", "INT", existingColumnNames);
            AddColumn(connection, "MediaStreams", "DvProfile", "INT", existingColumnNames);
            AddColumn(connection, "MediaStreams", "DvLevel", "INT", existingColumnNames);
            AddColumn(connection, "MediaStreams", "RpuPresentFlag", "INT", existingColumnNames);
            AddColumn(connection, "MediaStreams", "ElPresentFlag", "INT", existingColumnNames);
            AddColumn(connection, "MediaStreams", "BlPresentFlag", "INT", existingColumnNames);
            AddColumn(connection, "MediaStreams", "DvBlSignalCompatibilityId", "INT", existingColumnNames);

            AddColumn(connection, "MediaStreams", "IsHearingImpaired", "BIT", existingColumnNames);

            AddColumn(connection, "MediaStreams", "Rotation", "INT", existingColumnNames);

            transaction.Commit();
        }

        using (var transaction = connection.BeginTransaction())
        {
            var userDatasTableExists = TableExists(connection, "UserDatas");
            var userDataTableExists = TableExists(connection, "userdata");

            var users = userDatasTableExists ? [] : _userManager.Users;

            connection.Execute("create table if not exists UserDatas (key nvarchar not null, userId INT not null, rating float null, played bit not null, playCount int not null, isFavorite bit not null, playbackPositionTicks bigint not null, lastPlayedDate datetime null, AudioStreamIndex INT, SubtitleStreamIndex INT)");

            if (userDataTableExists)
            {
                var existingColumnNames = GetColumnNames(connection, "userdata");

                AddColumn(connection, "userdata", "InternalUserId", "int", existingColumnNames);
                AddColumn(connection, "userdata", "AudioStreamIndex", "int", existingColumnNames);
                AddColumn(connection, "userdata", "SubtitleStreamIndex", "int", existingColumnNames);

                if (!userDatasTableExists)
                {
                    ImportUserIds(connection, users);
                    connection.Execute("INSERT INTO UserDatas (key, userId, rating, played, playCount, isFavorite, playbackPositionTicks, lastPlayedDate, AudioStreamIndex, SubtitleStreamIndex) SELECT key, InternalUserId, rating, played, playCount, isFavorite, playbackPositionTicks, lastPlayedDate, AudioStreamIndex, SubtitleStreamIndex from userdata where InternalUserId not null");
                }
            }

            transaction.Commit();
        }

        _logger.LogInformation("Database was successfully updated");
    }

    private void ImportUserIds(SqliteConnection connection, IEnumerable<User> users)
    {
        var userIdsWithUserData = GetAllUserIdsWithUserData(connection);

        using (var statement = connection.PrepareStatement("update userdata set InternalUserId=@InternalUserId where UserId=@UserId"))
        {
            foreach (var user in users)
            {
                if (!userIdsWithUserData.Contains(user.Id))
                {
                    continue;
                }

                statement.TryBind("@UserId", user.Id);
                statement.TryBind("@InternalUserId", user.InternalId);

                _ = statement.ExecuteNonQuery();
            }
        }
    }

    private List<Guid> GetAllUserIdsWithUserData(SqliteConnection connection)
    {
        var list = new List<Guid>();

        using (var statement = connection.CreateCommand())
        {
            statement.CommandText = "select DISTINCT UserId from UserData where UserId not null";
            foreach (var row in statement.ExecuteQuery())
            {
                try
                {
                    list.Add(row.GetGuid(0));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while getting user");
                }
            }
        }

        return list;
    }

    private List<string> GetColumnNames(SqliteConnection connection, string table)
    {
        var columnNames = new List<string>();

        foreach (var row in connection.Query("PRAGMA table_info(" + table + ")"))
        {
            if (row.TryGetString(1, out var columnName))
            {
                columnNames.Add(columnName);
            }
        }

        return columnNames;
    }

    private void AddColumn(SqliteConnection connection, string table, string columnName, string type, List<string> existingColumnNames)
    {
        if (existingColumnNames.Contains(columnName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        connection.Execute("alter table " + table + " add column " + columnName + " " + type + " NULL");
    }

    private bool TableExists(SqliteConnection connection, string name)
    {
        using var statement = connection.CreateCommand();
        statement.CommandText = "select DISTINCT tbl_name from sqlite_master";

        foreach (var row in statement.ExecuteQuery())
        {
            if (string.Equals(name, row.GetString(0), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
