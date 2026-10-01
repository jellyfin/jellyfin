using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Library;
using Prometheus;

namespace Jellyfin.Server.Implementations.Metrics;

/// <summary>
/// Exposes Prometheus metrics describing the configured Jellyfin user accounts.
/// </summary>
public sealed class UserMetrics : IMetricsCollector
{
    private static readonly Gauge _users = Prometheus.Metrics
        .CreateGauge("jellyfin_users", "Number of Jellyfin user accounts grouped by state.", "state");

    private static readonly Gauge _usersByAuthProvider = Prometheus.Metrics
        .CreateGauge("jellyfin_users_authentication_provider", "Number of users grouped by authentication provider id.", "provider");

    private static readonly Gauge _recentlyActiveUsers = Prometheus.Metrics
        .CreateGauge("jellyfin_users_active_recent", "Number of users that have been active within the last 30 days.");

    private static readonly Gauge _usersWithFailedLogins = Prometheus.Metrics
        .CreateGauge("jellyfin_users_failed_logins", "Number of users that currently have one or more failed login attempts on record.");

    private readonly IUserManager _userManager;

    private readonly HashSet<string> _seenAuthProviders = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="UserMetrics"/> class.
    /// </summary>
    /// <param name="userManager">The user manager.</param>
    public UserMetrics(IUserManager userManager)
    {
        _userManager = userManager;
    }

    /// <inheritdoc />
    public string Name => nameof(UserMetrics);

    /// <inheritdoc />
    public Task CollectAsync(CancellationToken cancellationToken)
    {
        var users = _userManager.GetUsers().ToList();

        var disabled = users.Count(u => u.HasPermission(PermissionKind.IsDisabled));
        var admins = users.Count(u => u.HasPermission(PermissionKind.IsAdministrator));
        _users.WithLabels("enabled").Set(users.Count - disabled);
        _users.WithLabels("disabled").Set(disabled);
        _users.WithLabels("admin").Set(admins);

        var providerCounts = users
            .GroupBy(u => string.IsNullOrEmpty(u.AuthenticationProviderId) ? "Unknown" : u.AuthenticationProviderId)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        // Reset providers seen in earlier ticks but absent from this snapshot, so the gauge reflects reality.
        foreach (var oldProvider in _seenAuthProviders)
        {
            if (!providerCounts.ContainsKey(oldProvider))
            {
                _usersByAuthProvider.WithLabels(oldProvider).Set(0);
            }
        }

        foreach (var (provider, count) in providerCounts)
        {
            _usersByAuthProvider.WithLabels(provider).Set(count);
            _seenAuthProviders.Add(provider);
        }

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        _recentlyActiveUsers.Set(users.Count(u => u.LastActivityDate >= thirtyDaysAgo));

        _usersWithFailedLogins.Set(users.Count(u => u.InvalidLoginAttemptCount > 0));

        return Task.CompletedTask;
    }
}
