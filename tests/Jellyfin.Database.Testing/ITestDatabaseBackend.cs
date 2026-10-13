using System;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Jellyfin.Database.Testing;

public interface ITestDatabaseBackend
{
    TestDbProvider Provider { get; }

    Version ServerVersion { get; }

    TestDatabase Create(TestSchemaSource source, IInterceptor[] interceptors);

    /// <summary>
    /// Creates a database without any schema.
    /// </summary>
    /// <returns>The database.</returns>
    TestDatabase CreateEmpty();
}
