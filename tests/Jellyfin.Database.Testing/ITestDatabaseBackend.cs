using System;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Jellyfin.Database.Testing;

public interface ITestDatabaseBackend
{
    TestDbProvider Provider { get; }

    Version ServerVersion { get; }

    /// <summary>
    /// Creates a database with the schema of <paramref name="source"/>. Nobody else uses it until it is disposed.
    /// </summary>
    /// <param name="source">Where the schema comes from.</param>
    /// <param name="interceptors">The interceptors of the contexts that are created for the database.</param>
    /// <param name="fresh">
    /// Whether the database has to be one that was never used before and is dropped when it is disposed, which a test
    /// that changes the schema needs. A backend that reuses databases hands out a reset one otherwise.
    /// </param>
    /// <returns>The database.</returns>
    TestDatabase Create(TestSchemaSource source, IInterceptor[] interceptors, bool fresh = false);

    /// <summary>
    /// Creates a database without any schema.
    /// </summary>
    /// <returns>The database.</returns>
    TestDatabase CreateEmpty();
}
