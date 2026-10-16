using System;
using Xunit;

namespace Jellyfin.Database.Testing;

/// <summary>
/// Marks a theory that needs the migrations of the provider of the selected engine, so that it is skipped on an engine
/// whose provider has none yet (<see cref="EngineTraits.HasSchemaMigrations"/>). The skip lifts when the trait changes.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SchemaMigrationsTheoryAttribute : TheoryAttribute
{
    public SchemaMigrationsTheoryAttribute()
    {
        Skip = ProviderRequirement.SchemaMigrationsSkipReason();
    }
}
