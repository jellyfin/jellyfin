#pragma warning disable CA1307

using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Jellyfin.Server.Implementations.Metrics
{
    /// <summary>
    /// Provides Prometheus metrics related to Jellyfin users.
    /// </summary>
    public class UserMetrics
    {
        private readonly ILogger<UserMetrics> _logger;
        private readonly IEnumerable<IAuthenticationProvider> _authenticationProviders;

        // Prometheus Gauges for user metrics
        private static readonly Gauge _totalUserAccounts = Prometheus.Metrics
            .CreateGauge("jellyfin_user_accounts_total", "Total number of user accounts in Jellyfin");

        private static readonly Gauge _activeUserAccounts = Prometheus.Metrics
            .CreateGauge("jellyfin_user_accounts_active", "Number of active (non-disabled) user accounts");

        private static readonly Gauge _adminUserAccounts = Prometheus.Metrics
            .CreateGauge("jellyfin_user_accounts_admin", "Number of administrator user accounts");

        private static readonly Gauge _usersByAuthProvider = Prometheus.Metrics
            .CreateGauge("jellyfin_users_by_auth_provider", "Number of users by authentication provider", new[] { "provider" });

        private static readonly Gauge _usersByPermission = Prometheus.Metrics
            .CreateGauge("jellyfin_users_by_permission", "Number of users with specific permissions", new[] { "permission" });

        private static readonly Gauge _usersWithExternalAuth = Prometheus.Metrics
            .CreateGauge("jellyfin_users_external_auth_total", "Total number of users using external authentication");

        private static readonly Gauge _recentlyActiveUsers = Prometheus.Metrics
            .CreateGauge("jellyfin_users_recently_active", "Number of users active within the last 30 days");

        private static readonly Gauge _usersWithFailedLogins = Prometheus.Metrics
            .CreateGauge("jellyfin_users_failed_logins", "Number of users with failed login attempts", new[] { "attempt_range" });

        /// <summary>
        /// Initializes a new instance of the <see cref="UserMetrics"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="authenticationProviders">The authentication providers.</param>
        public UserMetrics(ILogger<UserMetrics> logger, IEnumerable<IAuthenticationProvider> authenticationProviders)
        {
            _authenticationProviders = authenticationProviders;
            _logger = logger;
        }

        /// <summary>
        /// Updates All User Metrics.
        /// </summary>
        /// <param name="users">List of users to update in metrics.</param>
        public void UpdateUserMetrics(IEnumerable<User> users)
        {
            try
            {
                var userList = users.ToList();

                // Basic user count metrics
                UpdateBasicUserCounts(userList);

                // Authentication provider metrics
                UpdateAuthProviderMetrics(userList);

                // Permission-based metrics
                UpdatePermissionMetrics(userList);

                // Activity-based metrics
                UpdateActivityMetrics(userList);

                // Security-related metrics
                UpdateSecurityMetrics(userList);

                _logger.LogDebug("Updated user metrics for {UserCount} users", userList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user metrics");
            }
        }

        /// <summary>
        /// Updates Basic User Counts.
        /// </summary>
        /// <param name="users">List of users to update in metrics.</param>
        private void UpdateBasicUserCounts(IEnumerable<User> users)
        {
            var userList = users.ToList();
            // Total user accounts
            _totalUserAccounts.Set(userList.Count);
            // Active user accounts (not disabled)
            var activeUsers = userList.Count(u => !u.HasPermission(PermissionKind.IsDisabled));
            _activeUserAccounts.Set(activeUsers);
            // Administrator accounts
            var adminUsers = userList.Count(u => u.HasPermission(PermissionKind.IsAdministrator));
            _adminUserAccounts.Set(adminUsers);
        }

        /// <summary>
        /// Update user permissions related metrics.
        /// </summary>
        /// <param name="users">List of users to update in metrics.</param>
        private void UpdatePermissionMetrics(IEnumerable<User> users)
        {
            var userList = users.ToList();

            // Define key permissions to track
            var permissionsToTrack = new[]
            {
                PermissionKind.IsAdministrator,
                PermissionKind.IsHidden,
                PermissionKind.IsDisabled,
                PermissionKind.EnableRemoteAccess,
                PermissionKind.EnableLiveTvAccess,
                PermissionKind.EnableLiveTvManagement,
                PermissionKind.EnableMediaPlayback,
                PermissionKind.EnableAudioPlaybackTranscoding,
                PermissionKind.EnableVideoPlaybackTranscoding,
                PermissionKind.EnableContentDeletion,
                PermissionKind.EnableContentDownloading,
                PermissionKind.EnableAllFolders,
                PermissionKind.EnableAllDevices,
                PermissionKind.EnableAllChannels,
                PermissionKind.EnablePublicSharing,
                PermissionKind.EnableRemoteControlOfOtherUsers,
                PermissionKind.EnableSharedDeviceControl
            };

            foreach (var permission in permissionsToTrack)
            {
                var userCount = userList.Count(u => u.HasPermission(permission));
                _usersByPermission.WithLabels(permission.ToString()).Set(userCount);
            }
        }

        /// <summary>
        /// Update provider related metrics.
        /// </summary>
        /// <param name="users">List of users to update in metrics.</param>
        private void UpdateAuthProviderMetrics(IEnumerable<User> users)
        {
            var userList = users.ToList();

            // Reset all auth provider metrics
            foreach (var provider in _authenticationProviders)
            {
                var providerName = GetProviderDisplayName(provider);
                _usersByAuthProvider.WithLabels(providerName).Set(0);
            }

            // Count users by authentication provider
            var authProviderGroups = userList.GroupBy(u => u.AuthenticationProviderId).ToList();
            foreach (var group in authProviderGroups)
            {
                var providerName = GetProviderNameById(group.Key);
                _usersByAuthProvider.WithLabels(providerName).Set(group.Count());
            }

            // Count users with external authentication (non-default providers)
            var externalAuthUsers = userList.Count(u => !string.IsNullOrEmpty(u.AuthenticationProviderId) && !u.AuthenticationProviderId.Contains("DefaultAuthenticationProvider"));
            _usersWithExternalAuth.Set(externalAuthUsers);
        }

        /// <summary>
        /// Updates user activity metrics.
        /// </summary>
        /// <param name="users">List of users to update in metrics.</param>
        private void UpdateActivityMetrics(IEnumerable<User> users)
        {
            var userList = users.ToList();
            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);

            // Users active within last 30 days
            var recentlyActiveCount = userList.Count(u => u.LastActivityDate.HasValue && u.LastActivityDate.Value >= thirtyDaysAgo);
            _recentlyActiveUsers.Set(recentlyActiveCount);
        }

        /// <summary>
        /// Updates security related metrics.
        /// </summary>
        /// <param name="users">List of users to update in metrics.</param>
        private void UpdateSecurityMetrics(IEnumerable<User> users)
        {
            var userList = users.ToList();

            // Users with failed login attempts
            var usersWithSomeFailedLogins = userList.Count(u => u.InvalidLoginAttemptCount is > 0 and < 3);
            var usersWithManyFailedLogins = userList.Count(u => u.InvalidLoginAttemptCount >= 3);

            _usersWithFailedLogins.WithLabels("1-2").Set(usersWithSomeFailedLogins);
            _usersWithFailedLogins.WithLabels("3+").Set(usersWithManyFailedLogins);
        }

        /// <summary>
        /// Gets Provider display name.
        /// </summary>
        /// <param name="provider">Authentication provider.</param>
        /// <returns>The display name of provider.</returns>
        private string GetProviderDisplayName(IAuthenticationProvider provider)
        {
            return provider.Name;
        }

        /// <summary>
        /// Gets provider name with providerId.
        /// </summary>
        /// <param name="providerId">Provider Id.</param>
        /// <returns>The name of the provider.</returns>
        private string GetProviderNameById(string providerId)
        {
            if (string.IsNullOrEmpty(providerId) || providerId == "Default")
            {
                return "Default";
            }

            var provider = _authenticationProviders.FirstOrDefault(p => p.GetType().FullName == providerId);
            return provider?.Name ?? providerId.Split('.').LastOrDefault() ?? providerId;
        }

        /// <summary>
        /// Gets current metric values for debugging/logging purposes.
        /// </summary>
        /// <returns>All user related metrics.</returns>
        public Dictionary<string, double> GetCurrentMetrics()
        {
            return new Dictionary<string, double>
            {
                ["total_users"] = _totalUserAccounts.Value,
                ["active_users"] = _activeUserAccounts.Value,
                ["admin_users"] = _adminUserAccounts.Value,
                ["external_auth_users"] = _usersWithExternalAuth.Value,
                ["recently_active_users"] = _recentlyActiveUsers.Value
            };
        }
    }
}
