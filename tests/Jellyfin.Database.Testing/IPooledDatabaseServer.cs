using System;
using Jellyfin.Database.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Jellyfin.Database.Testing;

/// <summary>
/// The part of a <see cref="LeasedDatabasePool"/> that depends on the engine: where the databases live, and how they
/// are created, opened and dropped. Disposing the server releases whatever it started, once every database is dropped.
/// </summary>
internal interface IPooledDatabaseServer : IDisposable
{
    /// <summary>
    /// Creates a database without any schema.
    /// </summary>
    /// <returns>The unique name of the database.</returns>
    string CreateDatabase();

    DbContextOptions<JellyfinDbContext> CreateOptions(string database, IInterceptor[] interceptors);

    IJellyfinDatabaseProvider CreateProvider();

    /// <summary>
    /// Describes the structure of a database, without its contents, so that a change to it can be detected. The
    /// contents of the migration history are not part of the structure.
    /// </summary>
    /// <param name="database">The name of the database.</param>
    /// <returns>A value that is equal for databases of the same structure.</returns>
    string GetSchemaFingerprint(string database);

    /// <summary>
    /// Drops a database, which must not fail when the database does not exist.
    /// </summary>
    /// <param name="database">The name of the database.</param>
    void DropDatabase(string database);
}
