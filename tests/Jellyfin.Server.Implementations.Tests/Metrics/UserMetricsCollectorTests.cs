using System;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.Metrics;
using MediaBrowser.Controller.Library;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Metrics;

[Collection(MetricsTestCollection.Name)]
public class UserMetricsCollectorTests
{
    [Fact]
    public void RefreshGauges_TracksUserCountsAndPermissions()
    {
        var userManager = new Mock<IUserManager>();
        var users = new[]
        {
            CreateUser("admin"),
            CreateUser("member"),
            CreateUser("hidden")
        };

        users[0].Permissions.Add(new Permission(PermissionKind.IsAdministrator, true));
        users[1].Permissions.Add(new Permission(PermissionKind.EnableMediaPlayback, true));
        users[2].Permissions.Add(new Permission(PermissionKind.IsHidden, true));

        userManager.Setup(x => x.GetUsers()).Returns(users);

        var collector = new UserMetricsCollector(userManager.Object);
        collector.RefreshGauges();

        Assert.Equal(3d, JellyfinMetrics.UserAccounts.Value);
        Assert.Equal(1d, JellyfinMetrics.UsersWithPermission.WithLabels("is_administrator").Value);
        Assert.Equal(1d, JellyfinMetrics.UsersWithPermission.WithLabels("is_hidden").Value);
        Assert.Equal(1d, JellyfinMetrics.UsersWithPermission.WithLabels("enable_media_playback").Value);
    }

    [Fact]
    public void RefreshGauges_TracksEveryPermissionKind()
    {
        var user = CreateUser("all-permissions");
        foreach (var kind in Enum.GetValues<PermissionKind>())
        {
            user.Permissions.Add(new Permission(kind, true));
        }

        var userManager = new Mock<IUserManager>();
        userManager.Setup(x => x.GetUsers()).Returns([user]);

        var collector = new UserMetricsCollector(userManager.Object);
        collector.RefreshGauges();

        var permissionLabels = new[]
        {
            "is_administrator",
            "is_hidden",
            "is_disabled",
            "enable_shared_device_control",
            "enable_remote_access",
            "enable_live_tv_management",
            "enable_live_tv_access",
            "enable_media_playback",
            "enable_audio_playback_transcoding",
            "enable_video_playback_transcoding",
            "enable_content_deletion",
            "enable_content_downloading",
            "enable_sync_transcoding",
            "enable_media_conversion",
            "enable_all_devices",
            "enable_all_channels",
            "enable_all_folders",
            "enable_public_sharing",
            "enable_remote_control_of_other_users",
            "enable_playback_remuxing",
            "force_remote_source_transcoding",
            "enable_collection_management",
            "enable_subtitle_management",
            "enable_lyric_management"
        };

        Assert.Equal(Enum.GetValues<PermissionKind>().Length, permissionLabels.Length);
        foreach (var label in permissionLabels)
        {
            Assert.Equal(1d, JellyfinMetrics.UsersWithPermission.WithLabels(label).Value);
        }
    }

    private static User CreateUser(string username)
        => new(username, "default-provider", "default-password-reset");
}
