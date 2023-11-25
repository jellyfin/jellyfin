namespace Jellyfin.Server.Migrations
{
    /// <summary>
    /// Interface that describes a pre startup migration routine.
    /// </summary>
    internal interface IPreStartupMigrationRoutine : IMigrationRoutine
    {
    }
}
