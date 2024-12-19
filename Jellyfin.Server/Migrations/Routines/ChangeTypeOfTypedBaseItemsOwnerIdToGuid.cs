using System;
using System.Globalization;
using System.IO;
using Emby.Server.Implementations.Data;
using MediaBrowser.Controller;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// Change type of TypedBaseItems.OwnerId to GUID so database can use index when compared to guid column.
    /// </summary>
    internal class ChangeTypeOfTypedBaseItemsOwnerIdToGuid : IMigrationRoutine
    {
        private const string DbFilename = "library.db";
        private readonly ILogger<ChangeTypeOfTypedBaseItemsOwnerIdToGuid> _logger;
        private readonly IServerApplicationPaths _paths;

        public ChangeTypeOfTypedBaseItemsOwnerIdToGuid(ILogger<ChangeTypeOfTypedBaseItemsOwnerIdToGuid> logger, IServerApplicationPaths paths)
        {
            _logger = logger;
            _paths = paths;
        }

        /// <inheritdoc/>
        public Guid Id => Guid.Parse("{9ACD7444-568D-4E20-89A1-B0E0D94023AC}");

        /// <inheritdoc/>
        public string Name => "ChangeTypeOfTypedBaseItemsOwnerIdToGuid";

        /// <inheritdoc/>
        public bool PerformOnNewInstall => false;

        /// <inheritdoc/>
        public void Perform()
        {
            var dataPath = _paths.DataPath;
            var dbPath = Path.Combine(dataPath, DbFilename);
            using (var connection = new SqliteConnection($"Filename={Path.Combine(dataPath, DbFilename)}"))
            {
                // Back up the database before column is changed
                for (int i = 1; ; i++)
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

                // Determine the type of TypedBaseItems.OwnerId
                _logger.LogInformation("Determine type of column TypedBaseItems.OwnerId.");
                var result = connection.Query("SELECT type FROM pragma_table_info('TypedBaseItems') WHERE name = 'OwnerId'").GetEnumerator();
                if (result.MoveNext())
                {
                    var row = result.Current;

                    // If the type is TEXT change it to GUID so the database can use indexes when OwnerId is compared to guid column
                    if (row.TryGetString(0, out var columnType) && columnType != "GUID")
                    {
                        _logger.LogInformation("Type of column TypedBaseItems.OwnerId is {ColumnType} -> changing to GUID.", columnType);

                        // use separate connections for alter table commands to prevent sqlite "database table is locked" exception
                        using (var alterTableConnection = new SqliteConnection($"Filename={Path.Combine(dataPath, DbFilename)}"))
                        using (var transaction = alterTableConnection.BeginTransaction())
                        {
                            alterTableConnection.Execute("DROP INDEX IF EXISTS idx_TypedBaseItemsOwnerId");
                            alterTableConnection.Execute("ALTER TABLE TypedBaseItems RENAME COLUMN OwnerId TO OwnerId_OLD");
                            alterTableConnection.Execute("ALTER TABLE TypedBaseItems ADD COLUMN OwnerId GUID NULL");
                            alterTableConnection.Execute("UPDATE TypedBaseItems SET OwnerId = OwnerId_OLD WHERE OwnerId_OLD IS NOT NULL");
                            alterTableConnection.Execute("ALTER TABLE TypedBaseItems DROP COLUMN OwnerId_OLD");
                            alterTableConnection.Execute("CREATE INDEX idx_TypedBaseItemsOwnerId ON TypedBaseItems(OwnerId)");
                            transaction.Commit();
                        }
                    }
                    else
                    {
                        // Do nothing if the column is already of type GUID
                        _logger.LogInformation("Type of column TypedBaseItems.OwnerId is GUID -> no migration necessary");
                    }
                }
            }
        }
    }
}
