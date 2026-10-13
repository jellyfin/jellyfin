using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Rewrites the user data keys and device ids that are longer than their column into the normalized form the
/// application now looks them up by, including the device ids in the enabled devices of a user. Rows stored before
/// the length was limited are otherwise never found again, and cannot be copied to a database that enforces the
/// length.
/// </summary>
[JellyfinMigration("2026-10-13T09:00:00", nameof(NormalizeOverlongKeys))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class NormalizeOverlongKeys : IAsyncMigrationRoutine
{
    private readonly IStartupLogger<NormalizeOverlongKeys> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="NormalizeOverlongKeys"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    public NormalizeOverlongKeys(
        IStartupLogger<NormalizeOverlongKeys> logger,
        IDbContextFactory<JellyfinDbContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            // The length in the query is counted in code points and the bound in UTF-16 units, so the query only
            // narrows the rows down: a value of more than the bound units has more than half the bound in points.
            const int UserDataQueryLength = KeyNormalizer.UserDataKeyMaxLength / 2;
            const int DeviceIdQueryLength = KeyNormalizer.DeviceIdMaxLength / 2;

            var userData = new List<UserDataRow>();
            await foreach (var row in context.UserData
                .AsNoTracking()
                .Where(e => e.CustomDataKey.Length > UserDataQueryLength)
                .Select(e => new UserDataRow(e.ItemId, e.UserId, e.CustomDataKey, e.LastPlayedDate))
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                if (row.Key.Length > KeyNormalizer.UserDataKeyMaxLength)
                {
                    userData.Add(row);
                }
            }

            var deviceIds = new List<string>();
            await foreach (var deviceId in context.Devices
                .AsNoTracking()
                .Where(e => e.DeviceId.Length > DeviceIdQueryLength)
                .Select(e => e.DeviceId)
                .Distinct()
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                if (deviceId.Length > KeyNormalizer.DeviceIdMaxLength)
                {
                    deviceIds.Add(deviceId);
                }
            }

            var deviceOptionIds = new List<string>();
            await foreach (var deviceId in context.DeviceOptions
                .AsNoTracking()
                .Where(e => e.DeviceId.Length > DeviceIdQueryLength)
                .Select(e => e.DeviceId)
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                if (deviceId.Length > KeyNormalizer.DeviceIdMaxLength)
                {
                    deviceOptionIds.Add(deviceId);
                }
            }

            var enabledDevicesUserIds = new List<Guid>();
            await foreach (var user in context.Users
                .AsNoTracking()
                .Include(e => e.Preferences)
                .Where(e => e.Preferences.Any(p => p.Kind == PreferenceKind.EnabledDevices && p.Value.Length > DeviceIdQueryLength))
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                if (user.GetPreference(PreferenceKind.EnabledDevices).Any(e => e.Length > KeyNormalizer.DeviceIdMaxLength))
                {
                    enabledDevicesUserIds.Add(user.Id);
                }
            }

            if (userData.Count == 0 && deviceIds.Count == 0 && deviceOptionIds.Count == 0 && enabledDevicesUserIds.Count == 0)
            {
                return;
            }

            _logger.LogInformation(
                "Shortening {UserDataCount} user data keys, {DeviceCount} device ids, {DeviceOptionsCount} device option ids and the enabled devices of {EnabledDevicesCount} users that are longer than their columns",
                userData.Count,
                deviceIds.Count,
                deviceOptionIds.Count,
                enabledDevicesUserIds.Count);

            var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false))
            {
                await NormalizeUserDataKeysAsync(context, userData, cancellationToken).ConfigureAwait(false);

                foreach (var deviceId in deviceIds)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var normalized = KeyNormalizer.NormalizeDeviceId(deviceId);
                    await context.Devices
                        .Where(e => e.DeviceId == deviceId)
                        .ExecuteUpdateAsync(s => s.SetProperty(e => e.DeviceId, normalized), cancellationToken)
                        .ConfigureAwait(false);
                }

                await NormalizeDeviceOptionsAsync(context, deviceOptionIds, cancellationToken).ConfigureAwait(false);
                await NormalizeEnabledDevicesAsync(context, enabledDevicesUserIds, cancellationToken).ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task NormalizeUserDataKeysAsync(JellyfinDbContext context, List<UserDataRow> userData, CancellationToken cancellationToken)
    {
        var dropped = 0;
        foreach (var row in userData)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (itemId, userId, key, lastPlayed) = row;
            var normalized = KeyNormalizer.NormalizeUserDataKey(key);
            var rows = context.UserData.Where(e => e.ItemId.Equals(itemId) && e.UserId.Equals(userId));

            var existing = await rows
                .AsNoTracking()
                .Where(e => e.CustomDataKey == normalized)
                .Select(e => new { e.LastPlayedDate })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (existing is not null)
            {
                // The most recently played row wins and the row that is already stored under the new key wins a tie.
                // The other row is dropped whole: combining fields would count plays twice.
                dropped++;
                var overlongWins = (lastPlayed ?? DateTime.MinValue) > (existing.LastPlayedDate ?? DateTime.MinValue);
                _logger.LogInformation(
                    "User data of user {UserId} on item {ItemId} was stored under a key that is too long and under its shortened form, keeping the {KeptRow} row",
                    userId,
                    itemId,
                    overlongWins ? "more recently played, previously too long" : "existing");

                if (overlongWins)
                {
                    await rows.Where(e => e.CustomDataKey == normalized).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await rows.Where(e => e.CustomDataKey == key).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
                    continue;
                }
            }

            await rows
                .Where(e => e.CustomDataKey == key)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.CustomDataKey, normalized), cancellationToken)
                .ConfigureAwait(false);
        }

        if (dropped > 0)
        {
            _logger.LogInformation("Dropped {DroppedCount} user data rows whose keys became equal once shortened", dropped);
        }
    }

    private async Task NormalizeDeviceOptionsAsync(JellyfinDbContext context, List<string> deviceIds, CancellationToken cancellationToken)
    {
        var dropped = 0;
        foreach (var deviceId in deviceIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalized = KeyNormalizer.NormalizeDeviceId(deviceId);
            var exists = await context.DeviceOptions
                .AsNoTracking()
                .AnyAsync(e => e.DeviceId == normalized, cancellationToken)
                .ConfigureAwait(false);
            if (exists)
            {
                // Device ids are unique in the options, and the row that is already stored under the new id is kept.
                dropped++;
                await context.DeviceOptions
                    .Where(e => e.DeviceId == deviceId)
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);
                continue;
            }

            await context.DeviceOptions
                .Where(e => e.DeviceId == deviceId)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.DeviceId, normalized), cancellationToken)
                .ConfigureAwait(false);
        }

        if (dropped > 0)
        {
            _logger.LogInformation("Dropped {DroppedCount} device options whose device ids became equal once shortened", dropped);
        }
    }

    private static async Task NormalizeEnabledDevicesAsync(JellyfinDbContext context, List<Guid> userIds, CancellationToken cancellationToken)
    {
        foreach (var userId in userIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await context.Users
                .Include(e => e.Preferences)
                .FirstAsync(e => e.Id.Equals(userId), cancellationToken)
                .ConfigureAwait(false);
            user.SetPreference(PreferenceKind.EnabledDevices, KeyNormalizer.NormalizeDeviceIds(user.GetPreference(PreferenceKind.EnabledDevices)));
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            context.ChangeTracker.Clear();
        }
    }

    private sealed record UserDataRow(Guid ItemId, Guid UserId, string Key, DateTime? LastPlayedDate);
}
