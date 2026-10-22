using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

public sealed class MySqlDatabaseProviderTests : DatabaseTestFixture
{
    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public async Task PurgeDatabase_IdentifierWithQuote_IsEscaped()
    {
        await using var context = CreateDbContext();
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE `we``ird` (`Id` int AUTO_INCREMENT PRIMARY KEY)");
        await context.Database.ExecuteSqlRawAsync("INSERT INTO `we``ird` VALUES ()");

        await DatabaseProvider.PurgeDatabase(context, ["we`ird"]);

        Assert.Equal(0, await context.Database.SqlQueryRaw<long>("SELECT count(*) AS `Value` FROM `we``ird`").SingleAsync());
        await context.Database.ExecuteSqlRawAsync("INSERT INTO `we``ird` VALUES ()");
        Assert.Equal(1, await context.Database.SqlQueryRaw<int>("SELECT `Id` AS `Value` FROM `we``ird`").SingleAsync());
    }
}
