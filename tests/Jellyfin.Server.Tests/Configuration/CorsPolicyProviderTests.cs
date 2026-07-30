using System.Threading.Tasks;
using Jellyfin.Server.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Configuration;

public sealed class CorsPolicyProviderTests
{
    [Fact]
    public async Task EmptyOrigins_DoesNotEnableCrossOriginRequests()
    {
        var policy = await CreateProvider([]).GetPolicyAsync(new DefaultHttpContext(), null);

        Assert.NotNull(policy);
        Assert.False(policy.AllowAnyOrigin);
        Assert.Empty(policy.Origins);
    }

    [Fact]
    public async Task WildcardOrigin_MustBeExplicit()
    {
        var policy = await CreateProvider(["*"]).GetPolicyAsync(new DefaultHttpContext(), null);

        Assert.NotNull(policy);
        Assert.True(policy.AllowAnyOrigin);
        Assert.False(policy.SupportsCredentials);
    }

    [Fact]
    public async Task NamedOrigins_AreRestrictedAndSupportCredentials()
    {
        var policy = await CreateProvider(["https://media.example"]).GetPolicyAsync(new DefaultHttpContext(), null);

        Assert.NotNull(policy);
        Assert.False(policy.AllowAnyOrigin);
        Assert.Contains("https://media.example", policy.Origins);
        Assert.True(policy.SupportsCredentials);
    }

    private static CorsPolicyProvider CreateProvider(string[] origins)
    {
        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager
            .SetupGet(manager => manager.Configuration)
            .Returns(new ServerConfiguration { CorsHosts = origins });
        return new CorsPolicyProvider(configurationManager.Object);
    }
}
