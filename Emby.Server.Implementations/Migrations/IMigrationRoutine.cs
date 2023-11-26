using System;

namespace Emby.Server.Implementations.Migrations
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
        /// Gets the timestamp from when this migration was written.
        /// </summary>
        public string Timestamp { get; }

        /// <summary>
        /// Gets a value indicating whether to perform migration on a new install.
        /// </summary>
        public bool PerformOnNewInstall { get; }

        /// <summary>
        /// Execute the migration routine.
        /// </summary>
        public void Perform();
    }
}
