using Jellyfin.Server.Helpers;
using Xunit;

namespace Jellyfin.Server.Tests;

public class StartupHelpersTests
{
    [Theory]
    [InlineData("JELLYFIN_CustomNetflix__PostgreSqlConnectionString")]
    [InlineData("JELLYFIN_ADMIN_PASSWORD")]
    [InlineData("ASPNETCORE_API_TOKEN")]
    [InlineData("DOTNET_CLIENT_SECRET")]
    [InlineData("JELLYFIN_API_KEY")]
    public void IsSensitiveEnvironmentVariable_RedactsSecrets(string key)
    {
        Assert.True(StartupHelpers.IsSensitiveEnvironmentVariable(key));
    }

    [Theory]
    [InlineData("JELLYFIN_LOG_DIR")]
    [InlineData("DOTNET_ENVIRONMENT")]
    [InlineData("ASPNETCORE_URLS")]
    public void IsSensitiveEnvironmentVariable_KeepsOperationalValues(string key)
    {
        Assert.False(StartupHelpers.IsSensitiveEnvironmentVariable(key));
    }
}
