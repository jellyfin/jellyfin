using System;
using Jellyfin.Database.Implementations;
using MediaBrowser.Common.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// A database a <see cref="DbTestFixture"/> runs its tests against. It owns the database, which lives as long as
/// this object does. The fixture creates the schema unless the database already has one.
/// </summary>
public abstract class TestDatabase : IDisposable
{
    /// <summary>
    /// Points the options at this database.
    /// </summary>
    /// <param name="builder">The options to configure.</param>
    /// <returns>The same builder.</returns>
    public abstract DbContextOptionsBuilder<JellyfinDbContext> Configure(DbContextOptionsBuilder<JellyfinDbContext> builder);

    /// <summary>
    /// Creates the Jellyfin provider matching this database, which the context uses to build its model.
    /// </summary>
    /// <param name="applicationPaths">The application paths.</param>
    /// <returns>The provider.</returns>
    public abstract IJellyfinDatabaseProvider CreateProvider(IApplicationPaths applicationPaths);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected abstract void Dispose(bool disposing);
}
