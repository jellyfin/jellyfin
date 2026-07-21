using System;
using Emby.Server.Implementations.Library;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Configuration;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class UserDataManagerTests
{
    [Fact]
    public void GetUserData_NullUser_ThrowsArgumentNullException()
    {
        var manager = CreateUserDataManager();
        BaseItem item = new Audio { Name = "Test", Id = Guid.NewGuid() };

        Assert.Throws<ArgumentNullException>(() => manager.GetUserData(null!, item));
    }

    private static UserDataManager CreateUserDataManager()
    {
        var mockConfig = new Mock<IServerConfigurationManager>();
        mockConfig.SetupGet(x => x.Configuration).Returns(new ServerConfiguration());
        var mockDb = new Mock<IDbContextFactory<JellyfinDbContext>>();

        return new UserDataManager(mockConfig.Object, mockDb.Object);
    }
}
