using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Database.Testing;

public sealed class TestDbContextFactory : IDbContextFactory<JellyfinDbContext>
{
    private readonly Func<JellyfinDbContext> _createDbContext;

    public TestDbContextFactory(Func<JellyfinDbContext> createDbContext)
    {
        _createDbContext = createDbContext;
    }

    public JellyfinDbContext CreateDbContext() => _createDbContext();

    public Task<JellyfinDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_createDbContext());
}
