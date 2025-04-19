namespace Jellyfin.Server.Migrations
{
    /// <summary>
    /// Interface that describes a migration routine.
    /// </summary>
    internal interface IMigrationRoutine
    {
        /// <summary>
        /// Execute the migration routine.
        /// </summary>
        public void Perform();
    }
}
