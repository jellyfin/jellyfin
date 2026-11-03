using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.Metrics;
using MediaBrowser.Controller.Library;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Metrics;

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

    private static User CreateUser(string username)
        => new(username, "default-provider", "default-password-reset");
}
