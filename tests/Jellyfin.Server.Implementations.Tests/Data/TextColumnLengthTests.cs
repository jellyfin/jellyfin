using System;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Server.Implementations.Tests.Item;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

/// <summary>
/// The <c>[MaxLength]</c> attributes on text columns are model metadata only: a value longer than the
/// declared length is stored and read back whole, and is never truncated or rejected.
/// </summary>
public sealed class TextColumnLengthTests : JellyfinDbTestFixture
{
    [Fact]
    public void OverLength_DisplayText_IsStoredWhole()
    {
        var userId = Guid.NewGuid();
        var overview = new string('o', 5000);
        var appVersion = new string('1', 100);

        using (var context = CreateDbContext())
        {
            context.Users.Add(new User("user", "auth", "reset") { Id = userId });
            context.ActivityLogs.Add(new ActivityLog("Over-long overview", "Test", userId) { Overview = overview });
            context.Devices.Add(new Device(userId, "app", appVersion, "device", "device-id"));
            context.SaveChanges();
        }

        using (var context = CreateDbContext())
        {
            Assert.Equal(overview, context.ActivityLogs.Single().Overview);
            Assert.Equal(appVersion, context.Devices.Single().AppVersion);
        }
    }

    [Fact]
    public void UserProfileImage_LongPath_RoundTrips()
    {
        var userId = Guid.NewGuid();
        var path = "/images/" + new string('p', 992);
        Assert.Equal(1000, path.Length);

        using (var context = CreateDbContext())
        {
            context.Users.Add(new User("user", "auth", "reset")
            {
                Id = userId,
                ProfileImage = new ImageInfo(path)
            });
            context.SaveChanges();
        }

        using (var context = CreateDbContext())
        {
            var user = context.Users.Include(u => u.ProfileImage).Single();
            Assert.Equal(path, user.ProfileImage!.Path);
        }
    }
}
