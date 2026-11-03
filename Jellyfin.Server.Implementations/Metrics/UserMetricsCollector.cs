using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Server.Implementations.Metrics
{
    /// <summary>
    /// Collects user and permission related Prometheus metrics.
    /// </summary>
    public sealed class UserMetricsCollector
    {
        private static readonly (PermissionKind Kind, string Label)[] _trackedPermissions = new[]
        {
            (PermissionKind.IsAdministrator, "is_administrator"),
            (PermissionKind.IsHidden, "is_hidden"),
            (PermissionKind.IsDisabled, "is_disabled"),
            (PermissionKind.EnableRemoteAccess, "enable_remote_access"),
            (PermissionKind.EnableLiveTvAccess, "enable_live_tv_access"),
            (PermissionKind.EnableMediaPlayback, "enable_media_playback"),
            (PermissionKind.EnableContentDeletion, "enable_content_deletion"),
            (PermissionKind.EnableContentDownloading, "enable_content_downloading"),
            (PermissionKind.EnablePublicSharing, "enable_public_sharing"),
            (PermissionKind.EnableRemoteControlOfOtherUsers, "enable_remote_control_of_other_users"),
        };

        private readonly IUserManager _userManager;
        private readonly HashSet<string> _permissionKeys = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="UserMetricsCollector"/> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        public UserMetricsCollector(IUserManager userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Refreshes user gauges from the current user list.
        /// </summary>
        public void RefreshGauges()
        {
            var users = _userManager.GetUsers().ToArray();

            JellyfinMetrics.UserAccounts.Set(users.Length);

            var snapshot = new Dictionary<string, double>();
            foreach (var (kind, label) in _trackedPermissions)
            {
                snapshot[label] = users.Count(u => u.Permissions.Any(p => p.Kind == kind && p.Value));
            }

            MetricsSnapshot.SetSnapshot(JellyfinMetrics.UsersWithPermission, snapshot, _permissionKeys);
        }
    }
}
