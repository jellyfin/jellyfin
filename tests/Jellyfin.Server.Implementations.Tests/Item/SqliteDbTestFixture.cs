using Emby.Server.Implementations.Data;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Base fixture for the item tests: one database per test instance on the selected provider, plus the wiring the
/// repositories under test need. Derived classes seed in their own constructor.
/// </summary>
public abstract class SqliteDbTestFixture : DatabaseTestFixture
{
    protected SqliteDbTestFixture(params IInterceptor[] interceptors)
        : base(interceptors)
    {
    }

    protected BaseItemRepository CreateBaseItemRepository(ItemTypeLookup itemTypeLookup)
    {
        var serverConfigurationManager = new Mock<IServerConfigurationManager>();
        serverConfigurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());

        return new BaseItemRepository(
            CreateDbContextFactory(),
            new Mock<IServerApplicationHost>().Object,
            itemTypeLookup,
            serverConfigurationManager.Object,
            NullLogger<BaseItemRepository>.Instance);
    }
}
