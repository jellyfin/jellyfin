namespace Jellyfin.Server.Implementations.Tests.EfMigrations;

/// <summary>
/// An operation that some of the providers with a migration have more often than the others.
/// </summary>
/// <param name="MigrationId">The id of the migration.</param>
/// <param name="Direction"><see cref="MigrationContents.Up"/> or <see cref="MigrationContents.Down"/>.</param>
/// <param name="Provider">The provider that has the operation, or the providers separated by a comma, in the order of their table.</param>
/// <param name="Operation">The operation, as described by <see cref="MigrationContents.Describe"/>, with the number of extra times when it is more than one.</param>
internal sealed record MigrationDifference(string MigrationId, string Direction, string Provider, string Operation)
{
    /// <summary>
    /// Gets the key that an allowlist entry of the difference has.
    /// </summary>
    public (string Migration, string Direction, string Provider, string Operation) Key => (MigrationId, Direction, Provider, Operation);
}
