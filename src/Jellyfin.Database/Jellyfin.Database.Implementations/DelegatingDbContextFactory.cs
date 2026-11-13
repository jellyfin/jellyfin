using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Database.Implementations;

/// <summary>
/// Adapts a provider-specific <see cref="IDbContextFactory{TContext}"/> so it can also satisfy
/// <see cref="IDbContextFactory{TContext}"/> of <see cref="JellyfinDbContext"/>. This lets a database
/// provider that needs its own <see cref="JellyfinDbContext"/> subclass back the single, application-wide
/// pool instead of registering a second, parallel pool against the same database.
/// </summary>
/// <typeparam name="TContext">The provider-specific <see cref="JellyfinDbContext"/> subclass.</typeparam>
/// <param name="innerFactory">The provider-specific factory to delegate to.</param>
public sealed class DelegatingDbContextFactory<TContext>(IDbContextFactory<TContext> innerFactory) : IDbContextFactory<JellyfinDbContext>
    where TContext : JellyfinDbContext
{
    /// <inheritdoc/>
    public JellyfinDbContext CreateDbContext() => innerFactory.CreateDbContext();
}
