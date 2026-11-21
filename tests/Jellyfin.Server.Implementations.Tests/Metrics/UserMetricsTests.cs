using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.Metrics;
using MediaBrowser.Controller.Library;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Metrics;

public class UserMetricsTests
{
    [Fact]
    public async Task CollectAsync_WithMixedUsers_CompletesWithoutError()
    {
        var users = new[]
        {
            CreateUser("admin", isAdmin: true, isDisabled: false, lastActivityDaysAgo: 1, failedLogins: 0),
            CreateUser("regular", isAdmin: false, isDisabled: false, lastActivityDaysAgo: 5, failedLogins: 2),
            CreateUser("disabled", isAdmin: false, isDisabled: true, lastActivityDaysAgo: 90, failedLogins: 0),
        };
        var userManager = new Mock<IUserManager>();
        userManager.Setup(m => m.GetUsers()).Returns(users);

        var collector = new UserMetrics(userManager.Object);
        await collector.CollectAsync(CancellationToken.None);

        userManager.Verify(m => m.GetUsers(), Times.Once);
    }

    [Fact]
    public async Task CollectAsync_WithNoUsers_DoesNotThrow()
    {
        var userManager = new Mock<IUserManager>();
        userManager.Setup(m => m.GetUsers()).Returns(Array.Empty<User>());

        var collector = new UserMetrics(userManager.Object);
        await collector.CollectAsync(CancellationToken.None);

        userManager.Verify(m => m.GetUsers(), Times.Once);
    }

    [Fact]
    public void Name_ReturnsExpectedValue()
    {
        var collector = new UserMetrics(Mock.Of<IUserManager>());

        Assert.Equal(nameof(UserMetrics), collector.Name);
    }

    private static User CreateUser(string name, bool isAdmin, bool isDisabled, int lastActivityDaysAgo, int failedLogins)
    {
        var user = new User(name, "DefaultAuthenticationProvider", "DefaultPasswordResetProvider")
        {
            LastActivityDate = DateTime.UtcNow.AddDays(-lastActivityDaysAgo),
            InvalidLoginAttemptCount = failedLogins,
        };
        user.SetPermission(PermissionKind.IsAdministrator, isAdmin);
        user.SetPermission(PermissionKind.IsDisabled, isDisabled);
        return user;
    }
}
