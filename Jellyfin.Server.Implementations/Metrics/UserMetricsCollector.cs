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
            (PermissionKind.EnableSharedDeviceControl, "enable_shared_device_control"),
            (PermissionKind.EnableRemoteAccess, "enable_remote_access"),
            (PermissionKind.EnableLiveTvManagement, "enable_live_tv_management"),
            (PermissionKind.EnableLiveTvAccess, "enable_live_tv_access"),
            (PermissionKind.EnableMediaPlayback, "enable_media_playback"),
            (PermissionKind.EnableAudioPlaybackTranscoding, "enable_audio_playback_transcoding"),
            (PermissionKind.EnableVideoPlaybackTranscoding, "enable_video_playback_transcoding"),
            (PermissionKind.EnableContentDeletion, "enable_content_deletion"),
            (PermissionKind.EnableContentDownloading, "enable_content_downloading"),
            (PermissionKind.EnableSyncTranscoding, "enable_sync_transcoding"),
            (PermissionKind.EnableMediaConversion, "enable_media_conversion"),
            (PermissionKind.EnableAllDevices, "enable_all_devices"),
            (PermissionKind.EnableAllChannels, "enable_all_channels"),
            (PermissionKind.EnableAllFolders, "enable_all_folders"),
            (PermissionKind.EnablePublicSharing, "enable_public_sharing"),
            (PermissionKind.EnableRemoteControlOfOtherUsers, "enable_remote_control_of_other_users"),
            (PermissionKind.EnablePlaybackRemuxing, "enable_playback_remuxing"),
            (PermissionKind.ForceRemoteSourceTranscoding, "force_remote_source_transcoding"),
            (PermissionKind.EnableCollectionManagement, "enable_collection_management"),
            (PermissionKind.EnableSubtitleManagement, "enable_subtitle_management"),
            (PermissionKind.EnableLyricManagement, "enable_lyric_management"),
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
