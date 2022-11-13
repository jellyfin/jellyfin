using System;
using System.Collections.Generic;
using System.IO;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Entities.Security;
using Jellyfin.Server.Implementations;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SQLitePCL.pretty;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// A migration that moves data from the authentication database into the new schema.
    /// </summary>
    public class MigrateAuthenticationDb : IMigrationRoutine
    {
        private const string DbFilename = "authentication.db";

        private readonly ILogger<MigrateAuthenticationDb> _logger;
        private readonly IDbContextFactory<JellyfinDb> _dbProvider;
        private readonly IServerApplicationPaths _appPaths;
        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrateAuthenticationDb"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="dbProvider">The database provider.</param>
        /// <param name="appPaths">The server application paths.</param>
        /// <param name="userManager">The user manager.</param>
        public MigrateAuthenticationDb(
            ILogger<MigrateAuthenticationDb> logger,
            IDbContextFactory<JellyfinDb> dbProvider,
            IServerApplicationPaths appPaths,
            IUserManager userManager)
        {
            _logger = logger;
            _dbProvider = dbProvider;
            _appPaths = appPaths;
            _userManager = userManager;
        }

        /// <inheritdoc />
        public Guid Id => Guid.Parse("5BD72F41-E6F3-4F60-90AA-09869ABE0E22");

        /// <inheritdoc />
        public string Name => "MigrateAuthenticationDatabase";

        /// <inheritdoc />
        public bool PerformOnNewInstall => false;

        /// <inheritdoc />
        public void Perform()
        {
            var dataPath = _appPaths.DataPath;
            using (var connection = SQLite3.Open(
                Path.Combine(dataPath, DbFilename),
                ConnectionFlags.ReadOnly,
                null))
            {
                using var dbContext = _dbProvider.CreateDbContext();

                var authenticatedDevices = connection.Query("SELECT * FROM Tokens");

                foreach (var row in authenticatedDevices)
                {
                    var dateCreatedStr = row[9].ToString();
                    _ = DateTime.TryParse(dateCreatedStr, out var dateCreated);
                    var dateLastActivityStr = row[10].ToString();
                    _ = DateTime.TryParse(dateLastActivityStr, out var dateLastActivity);

                    if (row[6].IsDbNull())
                    {
                        dbContext.ApiKeys.Add(new ApiKey(row[3].ToString())
                        {
                            AccessToken = row[1].ToString(),
                            DateCreated = dateCreated,
                            DateLastActivity = dateLastActivity
                        });
                    }
                    else
                    {
                        var userId = new Guid(row[6].ToString());
                        var user = _userManager.GetUserById(userId);
                        if (user is null)
                        {
                            // User doesn't exist, don't bring over the device.
                            continue;
                        }

                        dbContext.Devices.Add(new Device(
                            new Guid(row[6].ToString()),
                            row[3].ToString(),
                            row[4].ToString(),
                            row[5].ToString(),
                            row[2].ToString())
                        {
                            AccessToken = row[1].ToString(),
                            IsActive = row[8].ToBool(),
                            DateCreated = dateCreated,
                            DateLastActivity = dateLastActivity
                        });
                    }
                }

                var deviceOptions = connection.Query("SELECT * FROM Devices");
                var deviceIds = new HashSet<string>();
                foreach (var row in deviceOptions)
                {
                    if (row[2].IsDbNull())
                    {
                        continue;
                    }

                    var deviceId = row[2].ToString();
                    if (deviceIds.Contains(deviceId))
                    {
                        continue;
                    }

                    deviceIds.Add(deviceId);

                    dbContext.DeviceOptions.Add(new DeviceOptions(deviceId)
                    {
                        CustomName = row[1].IsDbNull() ? null : row[1].ToString()
                    });
                }

                dbContext.SaveChanges();
            }

            try
            {
                File.Move(Path.Combine(dataPath, DbFilename), Path.Combine(dataPath, DbFilename + ".old"));

                var journalPath = Path.Combine(dataPath, DbFilename + "-journal");
                if (File.Exists(journalPath))
                {
                    File.Move(journalPath, Path.Combine(dataPath, DbFilename + ".old-journal"));
                }
            }
            catch (IOException e)
            {
                _logger.LogError(e, "Error renaming legacy activity log database to 'authentication.db.old'");
            }
        }
    }
}
