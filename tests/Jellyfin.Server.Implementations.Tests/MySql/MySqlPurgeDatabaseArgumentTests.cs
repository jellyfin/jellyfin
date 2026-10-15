using System;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Providers.MySql;
using Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

public class MySqlPurgeDatabaseArgumentTests
{
    private readonly IJellyfinDatabaseProvider _provider = new MySqlDatabaseProvider(new CollectingLogger<MySqlDatabaseProvider>());

    [Fact]
    public async Task PurgeDatabase_WithoutTableNames_Throws()
    {
        await using var context = OfflineContexts.MySql();

        await Assert.ThrowsAsync<ArgumentNullException>(() => _provider.PurgeDatabase(context, null));
    }

    [Fact]
    public async Task PurgeDatabase_WithoutContext_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _provider.PurgeDatabase(null!, ["BaseItems"]));
    }

    [Fact]
    public async Task PurgeDatabase_WithoutTables_DoesNothingAndDoesNotTouchTheServer()
    {
        await using var context = OfflineContexts.MySql();

        await _provider.PurgeDatabase(context, []);
    }
}
