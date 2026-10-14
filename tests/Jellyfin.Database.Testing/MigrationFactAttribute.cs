using System;
using Xunit;

namespace Jellyfin.Database.Testing;

/// <summary>
/// Marks a test of a single migration, so that it is skipped on a provider whose baseline migration already covers the
/// change. A provider that lacks the migration for any other reason runs the test, which fails.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class MigrationFactAttribute : FactAttribute
{
    public MigrationFactAttribute(string migrationId)
    {
        MigrationId = migrationId;
        Skip = ProviderRequirement.MigrationSkipReason(migrationId);
    }

    public string MigrationId { get; }
}
