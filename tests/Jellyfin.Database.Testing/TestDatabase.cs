using System;
using Jellyfin.Database.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Database.Testing;

public sealed class TestDatabase : IDisposable
{
    private readonly Func<IJellyfinDatabaseProvider> _providerFactory;
    private readonly Action _onDispose;

    public TestDatabase(
        DbContextOptions<JellyfinDbContext> options,
        Func<IJellyfinDatabaseProvider> providerFactory,
        Action onDispose)
    {
        Options = options;
        _providerFactory = providerFactory;
        _onDispose = onDispose;
    }

    public DbContextOptions<JellyfinDbContext> Options { get; }

    public IJellyfinDatabaseProvider CreateProvider() => _providerFactory();

    public void Dispose() => _onDispose();
}
