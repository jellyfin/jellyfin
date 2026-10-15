namespace Jellyfin.Server.Implementations.Tests.EfMigrations;

/// <summary>
/// One accepted difference between migrations that share an id, with its reason.
/// </summary>
/// <param name="Migration">The id of the migration.</param>
/// <param name="Direction"><see cref="MigrationContents.Up"/> or <see cref="MigrationContents.Down"/>.</param>
/// <param name="Provider">The provider that has the operation, or the providers separated by a comma, in the order of their table.</param>
/// <param name="Operation">The operation exactly as <see cref="MigrationContents.Describe"/> describes it.</param>
/// <param name="Category">Why such a difference may exist at all.</param>
/// <param name="Reason">Why this difference is correct.</param>
internal sealed record MigrationContentsAllowlistEntry(
    string Migration,
    string Direction,
    string Provider,
    string Operation,
    MigrationContentsCategory Category,
    string Reason)
{
    /// <summary>
    /// Gets the difference that the entry accepts.
    /// </summary>
    public (string Migration, string Direction, string Provider, string Operation) Key => (Migration, Direction, Provider, Operation);

    /// <summary>
    /// Describes the entry in one line.
    /// </summary>
    /// <returns>The description.</returns>
    public string Describe() => $"{Migration} {Direction} {Provider}: {Operation}";
}
