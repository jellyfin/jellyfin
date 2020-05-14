using System;
using System.IO;

using MediaBrowser.Controller;
using Microsoft.Extensions.Logging;
using SQLitePCL.pretty;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// Remove duplicate entries which were caused by a bug where a file was considered to be an "Extra" to itself.
    /// </summary>
    internal class RemoveBuggedExtras : IMigrationRoutine
    {
        private const string DbFilename = "library.db";
        private readonly ILogger _logger;
        private readonly IServerApplicationPaths _paths;

        public RemoveBuggedExtras(ILogger<RemoveBuggedExtras> logger, IServerApplicationPaths paths)
        {
            _logger = logger;
            _paths = paths;
        }

        /// <inheritdoc/>
        public Guid Id => Guid.Parse("{ACBE17B7-8435-4A83-8B64-6FCF162CB9BD}");

        /// <inheritdoc/>
        public string Name => "RemoveBuggedExtras";

        /// <inheritdoc/>
        public void Perform()
        {
            var dataPath = _paths.DataPath;
            using (var connection = SQLite3.Open(
                Path.Combine(dataPath, DbFilename),
                ConnectionFlags.ReadWrite,
                null))
            {
                var queryResult = connection.Query("SELECT t1.Path FROM TypedBaseItems AS t1, TypedBaseItems AS t2 WHERE t1.Path=t2.Path AND t1.Type!=t2.Type AND t1.Type='MediaBrowser.Controller.Entities.Video'");
                var bads = string.Join(", ", queryResult.SelectScalarString());
                if (bads.Length != 0)
                {
                    _logger.LogInformation("Removing found duplicated extras for the following items: {0}", bads);
                    connection.Execute("DELETE FROM TypedBaseItems WHERE rowid IN (SELECT t1.rowid FROM TypedBaseItems AS t1, TypedBaseItems AS t2 WHERE t1.Path=t2.Path AND t1.Type!=t2.Type AND t1.Type='MediaBrowser.Controller.Entities.Video')");
                }
            }
        }
    }
}
