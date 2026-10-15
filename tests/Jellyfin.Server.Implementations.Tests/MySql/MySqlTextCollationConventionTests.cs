using Jellyfin.Database.Providers.MySql;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

public class MySqlTextCollationConventionTests
{
    [Theory]
    [InlineData("longtext", true)]
    [InlineData("varchar(256)", true)]
    [InlineData("char(36)", true)]
    [InlineData("text", true)]
    [InlineData("mediumtext", true)]
    [InlineData("tinytext", true)]
    [InlineData("LONGTEXT", true)]
    [InlineData("varchar(64) CHARACTER SET utf8mb4", true)]
    [InlineData("json", false)]
    [InlineData("binary(16)", false)]
    [InlineData("longblob", false)]
    [InlineData("int", false)]
    [InlineData("datetime(6)", false)]
    [InlineData("nvarchar(10)", false)]
    [InlineData("textual", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsText_KnowsTheTextTypes(string? storeType, bool expected)
    {
        Assert.Equal(expected, TextCollationConvention.IsText(storeType));
    }
}
