using System;
using System.Globalization;
using System.IO;

using MediaBrowser.Controller;
using Microsoft.Extensions.Logging;
using SQLitePCL.pretty;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// Remove duplicate entries which were caused by a bug where a file was considered to be an "Extra" to itself.
    /// </summary>
    internal class RemoveDuplicateExtras : IMigrationRoutine
    {
        private const string DbFilename = "library.db";
        private readonly ILogger _logger;
        private readonly IServerApplicationPaths _paths;

        public RemoveDuplicateExtras(ILogger<RemoveDuplicateExtras> logger, IServerApplicationPaths paths)
        {
            _logger = logger;
            _paths = paths;
        }

        /// <inheritdoc/>
        public Guid Id => Guid.Parse("{ACBE17B7-8435-4A83-8B64-6FCF162CB9BD}");

        /// <inheritdoc/>
        public string Name => "RemoveDuplicateExtras";

        /// <inheritdoc/>
        public void Perform()
        {
            var dataPath = _paths.DataPath;
            var dbPath = Path.Combine(dataPath, DbFilename);
            using (var connection = SQLite3.Open(
                dbPath,
                ConnectionFlags.ReadWrite,
                null))
            {
                var queryResult = connection.Query("SELECT t1.Path FROM TypedBaseItems AS t1, TypedBaseItems AS t2 WHERE t1.Path=t2.Path AND t1.Type!=t2.Type AND t1.Type='MediaBrowser.Controller.Entities.Video'");
                var bads = string.Join(", ", queryResult.SelectScalarString());
                if (bads.Length != 0)
                {
                    _logger.LogInformation("Found duplicate extras, making {Library} backup", DbFilename);
                    for (int i = 1; ; i++)
                    {
                        var bakPath = string.Format(CultureInfo.InvariantCulture, "{0}.bak{1}", dbPath, i);
                        if (!File.Exists(bakPath))
                        {
                            try
                            {
                                File.Copy(dbPath, bakPath);
                                break;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Cannot make a backup of {Library}", DbFilename);
                                throw;
                            }
                        }
                    }

                    _logger.LogInformation("Removing found duplicated extras for the following items: {DuplicateExtras}", bads);
                    connection.Execute("DELETE FROM TypedBaseItems WHERE rowid IN (SELECT t1.rowid FROM TypedBaseItems AS t1, TypedBaseItems AS t2 WHERE t1.Path=t2.Path AND t1.Type!=t2.Type AND t1.Type='MediaBrowser.Controller.Entities.Video')");
                }
            }
        }
    }
}
