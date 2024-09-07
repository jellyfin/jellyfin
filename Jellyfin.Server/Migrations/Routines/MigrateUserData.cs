using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Entities;
using Jellyfin.Server.Implementations;
using MediaBrowser.Controller;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// The migration routine for migrating the userdata database to EF Core.
/// </summary>
public class MigrateUserData : IMigrationRoutine
{
    private const string DbFilename = "library.db";

    private readonly ILogger<MigrateUserDb> _logger;
    private readonly IServerApplicationPaths _paths;
    private readonly IDbContextFactory<JellyfinDbContext> _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrateUserData"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The database provider.</param>
    /// <param name="paths">The server application paths.</param>
    public MigrateUserData(
        ILogger<MigrateUserDb> logger,
        IDbContextFactory<JellyfinDbContext> provider,
        IServerApplicationPaths paths)
    {
        _logger = logger;
        _provider = provider;
        _paths = paths;
    }

    /// <inheritdoc/>
    public Guid Id => Guid.Parse("5bcb4197-e7c0-45aa-9902-963bceab5798");

    /// <inheritdoc/>
    public string Name => "MigrateUserData";

    /// <inheritdoc/>
    public bool PerformOnNewInstall => false;

    /// <inheritdoc/>
    public void Perform()
    {
        _logger.LogInformation("Migrating the userdata from library.db may take a while, do not stop Jellyfin.");

        var dataPath = _paths.DataPath;
        using var connection = new SqliteConnection($"Filename={Path.Combine(dataPath, DbFilename)}");

        connection.Open();
        using var dbContext = _provider.CreateDbContext();

        var queryResult = connection.Query("SELECT key, userId, rating, played, playCount, isFavorite, playbackPositionTicks, lastPlayedDate, AudioStreamIndex, SubtitleStreamIndex FROM UserDatas");

        dbContext.UserData.ExecuteDelete();

        var users = dbContext.Users.AsNoTracking().ToImmutableArray();

        foreach (SqliteDataReader dto in queryResult)
        {
            var entity = new UserData()
            {
                Key = dto.GetString(0),
                UserId = users.ElementAt(dto.GetInt32(1)).Id,
                Rating = dto.IsDBNull(2) ? null : dto.GetDouble(2),
                Played = dto.GetBoolean(3),
                PlayCount = dto.GetInt32(4),
                IsFavorite = dto.GetBoolean(5),
                PlaybackPositionTicks = dto.GetInt64(6),
                LastPlayedDate = dto.IsDBNull(7) ? null : dto.GetDateTime(7),
                AudioStreamIndex = dto.IsDBNull(8) ? null : dto.GetInt32(8),
                SubtitleStreamIndex = dto.IsDBNull(9) ? null : dto.GetInt32(9),
            };

            dbContext.UserData.Add(entity);
        }

        dbContext.SaveChanges();
    }
}
