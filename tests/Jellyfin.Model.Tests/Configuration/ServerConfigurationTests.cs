using MediaBrowser.Model.Configuration;
using Xunit;

namespace Jellyfin.Model.Tests.Configuration;

public sealed class ServerConfigurationTests
{
    [Fact]
    public void Constructor_UsesSafePublicServerDefaults()
    {
        var configuration = new ServerConfiguration();

        Assert.False(configuration.EnablePublicUserRegistration);
        Assert.False(configuration.AllowClientLogUpload);
        Assert.False(configuration.EnableLegacyAuthorization);
        Assert.Empty(configuration.CorsHosts);
        Assert.Equal(2, configuration.PublicUserRegistrationMaxActiveSessions);
        Assert.Equal(8_000_000, configuration.PublicUserRegistrationRemoteClientBitrateLimit);
        Assert.Equal(2, configuration.MaxConcurrentTranscodingJobs);
    }
}
