using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Jellyfin.Server.Implementations.Tests.EfMigrations;

/// <summary>
/// The operations of one migration of one provider.
/// </summary>
/// <param name="Provider">The name of the provider.</param>
/// <param name="MigrationId">The id of the migration.</param>
/// <param name="Up">The operations that apply the migration.</param>
/// <param name="Down">The operations that revert the migration.</param>
internal sealed record MigrationOperations(
    string Provider,
    string MigrationId,
    IReadOnlyList<MigrationOperation> Up,
    IReadOnlyList<MigrationOperation> Down);
