using System;
using Xunit;

namespace Jellyfin.Database.Testing;

/// <summary>
/// Marks a theory that builds its schema from the migrations of the provider, so that it is skipped on a provider that
/// has none.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class MigrationsTheoryAttribute : TheoryAttribute
{
    public MigrationsTheoryAttribute()
    {
        Skip = ProviderRequirement.MigrationsSkipReason();
    }
}
