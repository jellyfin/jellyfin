using System;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations
{
    /// <summary>
    /// Interface that describes a migration routine.
    /// </summary>
    internal interface IMigrationRoutine
    {
        /// <summary>
        /// Gets the unique id for this migration. This should never be modified after the migration has been created.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the display name of the migration.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Execute the migration routine.
        /// </summary>
        /// <param name="host">Host that hosts current version.</param>
        /// <param name="logger">Host logger.</param>
        public void Perform(CoreAppHost host, ILogger logger);
    }
}
