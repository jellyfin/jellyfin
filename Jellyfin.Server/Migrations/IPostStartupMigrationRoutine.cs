namespace Jellyfin.Server.Migrations
{
    /// <summary>
    /// Interface that describes a post startup migration routine.
    /// </summary>
    internal interface IPostStartupMigrationRoutine : IMigrationRoutine
    {
    }
}
