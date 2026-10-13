using Jellyfin.Server.Migrations;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

public class MigrationRoutineCatalogTests
{
    [Theory]
    [InlineData("20260915120000_MigrateRatingLevels")]
    [InlineData("20260825200000_ConsolidateLocalizedUserViews")]
    public void RoutineIds_NameTheRoutinesAsTheyAreRecordedInTheHistory(string routineId)
    {
        Assert.Contains(routineId, new MigrationRoutineCatalog().RoutineIds);
    }
}
