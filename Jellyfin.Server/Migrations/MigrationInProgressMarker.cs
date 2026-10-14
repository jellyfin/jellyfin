using System;
using Jellyfin.Server.Implementations.FullSystemBackup;

namespace Jellyfin.Server.Migrations;

/// <summary>
/// The content of the file that records a schema migration which has begun and has not been confirmed as applied.
/// </summary>
/// <param name="MigrationId">The identifier of the schema migration.</param>
/// <param name="ProviderKey">The key of the database provider that ran the migration.</param>
/// <param name="StartedAtUtc">The time at which the migration began.</param>
internal sealed record MigrationInProgressMarker(string MigrationId, string ProviderKey, DateTime StartedAtUtc)
{
    /// <summary>
    /// The name of the marker file in the data directory.
    /// </summary>
    public const string FileName = BackupService.MigrationInProgressMarkerFileName;
}
