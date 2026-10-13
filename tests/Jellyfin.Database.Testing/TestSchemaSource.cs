namespace Jellyfin.Database.Testing;

/// <summary>
/// Where the schema of a test database comes from.
/// </summary>
public enum TestSchemaSource
{
    /// <summary>
    /// The schema EF Core derives from the model, and nothing else. What only the migrations create is missing, among
    /// other things the column defaults the migrations set on SQLite, and on PostgreSQL the min aggregate over uuid,
    /// without which queries that group by an item id fail. It has no migration history either.
    /// </summary>
    Model,

    /// <summary>
    /// The schema of a server, built by applying the migrations of the provider. This is what
    /// <see cref="DatabaseTestFixture"/> uses unless a test asks for the model.
    /// </summary>
    Migrations
}
