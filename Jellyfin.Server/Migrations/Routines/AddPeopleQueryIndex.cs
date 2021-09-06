using System;
using System.IO;
using MediaBrowser.Controller;
using Microsoft.Extensions.Logging;
using SQLitePCL.pretty;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// Migration to add table indexes to optimize the Persons query.
    /// </summary>
    public class AddPeopleQueryIndex : IMigrationRoutine
    {
        private const string DbFilename = "library.db";
        private readonly ILogger<AddPeopleQueryIndex> _logger;
        private readonly IServerApplicationPaths _serverApplicationPaths;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddPeopleQueryIndex"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{AddPeopleQueryIndex}"/> interface.</param>
        /// <param name="serverApplicationPaths">Instance of the <see cref="IServerApplicationPaths"/> interface.</param>
        public AddPeopleQueryIndex(ILogger<AddPeopleQueryIndex> logger, IServerApplicationPaths serverApplicationPaths)
        {
            _logger = logger;
            _serverApplicationPaths = serverApplicationPaths;
        }

        /// <inheritdoc />
        public Guid Id => new Guid("DE009B59-BAAE-428D-A810-F67762DC05B8");

        /// <inheritdoc />
        public string Name => "AddPeopleQueryIndex";

        /// <inheritdoc />
        public bool PerformOnNewInstall => true;

        /// <inheritdoc />
        public void Perform()
        {
            var databasePath = Path.Join(_serverApplicationPaths.DataPath, DbFilename);
            using var connection = SQLite3.Open(databasePath, ConnectionFlags.ReadWrite, null);
            _logger.LogInformation("Creating index idx_TypedBaseItemsUserDataKeyType");
            connection.Execute("CREATE INDEX IF NOT EXISTS idx_TypedBaseItemsUserDataKeyType ON TypedBaseItems(UserDataKey, Type);");
            _logger.LogInformation("Creating index idx_PeopleNameListOrder");
            connection.Execute("CREATE INDEX IF NOT EXISTS idx_PeopleNameListOrder ON People(Name, ListOrder);");
        }
    }
}
