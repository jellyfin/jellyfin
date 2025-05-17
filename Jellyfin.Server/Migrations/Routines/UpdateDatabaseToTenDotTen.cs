using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Emby.Server.Implementations.Data;
using MediaBrowser.Controller;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Updates the library.db to version 10.10.z in preperation for the EFCore migration.
/// Replaces the migration code from the old SqliteItemRepository and SqliteUserDataRepository.
/// </summary>
[JellyfinMigration("2025-04-19T00:00:00", nameof(UpdateDatabaseToTenDotTen))]
internal class UpdateDatabaseToTenDotTen : IDatabaseMigrationRoutine
{
    private const string DbFilename = "library.db";
    private readonly ILogger<UpdateDatabaseToTenDotTen> _logger;
    private readonly IServerApplicationPaths _paths;
    private readonly List<(string TableName, string ColumnName, string Type)> _libraryDbTableColumns =
    [
        ("AncestorIds", "AncestorIdText", "Text"),

        ("TypedBaseItems", "Path", "Text"),
        ("TypedBaseItems", "StartDate", "DATETIME"),
        ("TypedBaseItems", "EndDate", "DATETIME"),
        ("TypedBaseItems", "ChannelId", "Text"),
        ("TypedBaseItems", "IsMovie", "BIT"),
        ("TypedBaseItems", "CommunityRating", "Float"),
        ("TypedBaseItems", "CustomRating", "Text"),
        ("TypedBaseItems", "IndexNumber", "INT"),
        ("TypedBaseItems", "IsLocked", "BIT"),
        ("TypedBaseItems", "Name", "Text"),
        ("TypedBaseItems", "OfficialRating", "Text"),
        ("TypedBaseItems", "MediaType", "Text"),
        ("TypedBaseItems", "Overview", "Text"),
        ("TypedBaseItems", "ParentIndexNumber", "INT"),
        ("TypedBaseItems", "PremiereDate", "DATETIME"),
        ("TypedBaseItems", "ProductionYear", "INT"),
        ("TypedBaseItems", "ParentId", "GUID"),
        ("TypedBaseItems", "Genres", "Text"),
        ("TypedBaseItems", "SortName", "Text"),
        ("TypedBaseItems", "ForcedSortName", "Text"),
        ("TypedBaseItems", "RunTimeTicks", "BIGINT"),
        ("TypedBaseItems", "DateCreated", "DATETIME"),
        ("TypedBaseItems", "DateModified", "DATETIME"),
        ("TypedBaseItems", "IsSeries", "BIT"),
        ("TypedBaseItems", "EpisodeTitle", "Text"),
        ("TypedBaseItems", "IsRepeat", "BIT"),
        ("TypedBaseItems", "PreferredMetadataLanguage", "Text"),
        ("TypedBaseItems", "PreferredMetadataCountryCode", "Text"),
        ("TypedBaseItems", "DateLastRefreshed", "DATETIME"),
        ("TypedBaseItems", "DateLastSaved", "DATETIME"),
        ("TypedBaseItems", "IsInMixedFolder", "BIT"),
        ("TypedBaseItems", "LockedFields", "Text"),
        ("TypedBaseItems", "Studios", "Text"),
        ("TypedBaseItems", "Audio", "Text"),
        ("TypedBaseItems", "ExternalServiceId", "Text"),
        ("TypedBaseItems", "Tags", "Text"),
        ("TypedBaseItems", "IsFolder", "BIT"),
        ("TypedBaseItems", "InheritedParentalRatingValue", "INT"),
        ("TypedBaseItems", "UnratedType", "Text"),
        ("TypedBaseItems", "TopParentId", "Text"),
        ("TypedBaseItems", "TrailerTypes", "Text"),
        ("TypedBaseItems", "CriticRating", "Float"),
        ("TypedBaseItems", "CleanName", "Text"),
        ("TypedBaseItems", "PresentationUniqueKey", "Text"),
        ("TypedBaseItems", "OriginalTitle", "Text"),
        ("TypedBaseItems", "PrimaryVersionId", "Text"),
        ("TypedBaseItems", "DateLastMediaAdded", "DATETIME"),
        ("TypedBaseItems", "Album", "Text"),
        ("TypedBaseItems", "LUFS", "Float"),
        ("TypedBaseItems", "NormalizationGain", "Float"),
        ("TypedBaseItems", "IsVirtualItem", "BIT"),
        ("TypedBaseItems", "SeriesName", "Text"),
        ("TypedBaseItems", "UserDataKey", "Text"),
        ("TypedBaseItems", "SeasonName", "Text"),
        ("TypedBaseItems", "SeasonId", "GUID"),
        ("TypedBaseItems", "SeriesId", "GUID"),
        ("TypedBaseItems", "ExternalSeriesId", "Text"),
        ("TypedBaseItems", "Tagline", "Text"),
        ("TypedBaseItems", "ProviderIds", "Text"),
        ("TypedBaseItems", "Images", "Text"),
        ("TypedBaseItems", "ProductionLocations", "Text"),
        ("TypedBaseItems", "ExtraIds", "Text"),
        ("TypedBaseItems", "TotalBitrate", "INT"),
        ("TypedBaseItems", "ExtraType", "Text"),
        ("TypedBaseItems", "Artists", "Text"),
        ("TypedBaseItems", "AlbumArtists", "Text"),
        ("TypedBaseItems", "ExternalId", "Text"),
        ("TypedBaseItems", "SeriesPresentationUniqueKey", "Text"),
        ("TypedBaseItems", "ShowId", "Text"),
        ("TypedBaseItems", "OwnerId", "Text"),
        ("TypedBaseItems", "Width", "INT"),
        ("TypedBaseItems", "Height", "INT"),
        ("TypedBaseItems", "Size", "BIGINT"),

        ("ItemValues", "CleanValue", "Text"),

        ("Chapters2", "ImageDateModified", "DATETIME"),

        ("mediastreams", "IsAvc", "BIT"),
        ("mediastreams", "TimeBase", "TEXT"),
        ("mediastreams", "CodecTimeBase", "TEXT"),
        ("mediastreams", "Title", "TEXT"),
        ("mediastreams", "NalLengthSize", "TEXT"),
        ("mediastreams", "Comment", "TEXT"),
        ("mediastreams", "CodecTag", "TEXT"),
        ("mediastreams", "PixelFormat", "TEXT"),
        ("mediastreams", "BitDepth", "INT"),
        ("mediastreams", "RefFrames", "INT"),
        ("mediastreams", "KeyFrames", "TEXT"),
        ("mediastreams", "IsAnamorphic", "BIT"),
        ("mediastreams", "ColorPrimaries", "TEXT"),
        ("mediastreams", "ColorSpace", "TEXT"),
        ("mediastreams", "ColorTransfer", "TEXT"),
        ("mediastreams", "DvVersionMajor", "INT"),
        ("mediastreams", "DvVersionMinor", "INT"),
        ("mediastreams", "DvProfile", "INT"),
        ("mediastreams", "DvLevel", "INT"),
        ("mediastreams", "RpuPresentFlag", "INT"),
        ("mediastreams", "ElPresentFlag", "INT"),
        ("mediastreams", "BlPresentFlag", "INT"),
        ("mediastreams", "DvBlSignalCompatibilityId", "INT"),
        ("mediastreams", "IsHearingImpaired", "BIT"),
        ("mediastreams", "Rotation", "INT")
    ];

    public UpdateDatabaseToTenDotTen(
        ILogger<UpdateDatabaseToTenDotTen> logger,
        IServerApplicationPaths paths)
    {
        _logger = logger;
        _paths = paths;
    }

    /// <inheritdoc/>
    public void Perform()
    {
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

        // add missing columns
        using var connection = new SqliteConnection($"Filename={dbPath}");
        connection.Open();
        using (var transaction = connection.BeginTransaction())
        {
            var existingColumns = GetExistingColumns(connection);

            _libraryDbTableColumns
                .Where(col => !existingColumns.Exists(exist => col.TableName == exist.TableName && col.ColumnName == exist.ColumnName))
                .ToList()
                .ForEach(col => connection.Execute($"ALTER TABLE {col.TableName} ADD COLUMN {col.ColumnName} {col.Type} NULL"));

            transaction.Commit();
        }

        _logger.LogInformation("Database was successfully updated");
    }

    /// <summary>
    /// Returns a list of all existing columns.
    /// </summary>
    private List<(string TableName, string ColumnName)> GetExistingColumns(SqliteConnection connection)
    {
        var existingColumns = new List<(string TableName, string ColumnName)>();
        var existingColumnsQuery = @"SELECT t.name AS table_name, c.name AS column_name
                                   FROM sqlite_master t
                                   JOIN pragma_table_info(t.name) c
                                   WHERE t.name IN ('AncestorIds', 'TypedBaseItems', 'ItemValues', 'Chapters2', 'mediastreams')";

        foreach (var row in connection.Query(existingColumnsQuery))
        {
            if (row.TryGetString(0, out var tableName) && row.TryGetString(1, out var columnName))
            {
                existingColumns.Add((tableName, columnName));
            }
        }

        return existingColumns;
    }
}
