using Jellyfin.Api.Auth.DefaultAuthorizationPolicy;
using Jellyfin.Server.Extensions;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jellyfin.Server.Tests.Extensions;

public class ApiServiceCollectionExtensionsTests
{
    [Fact]
    public void RequiresElevationPolicy_IncludesDefaultAuthorizationRequirement()
    {
        var services = new ServiceCollection();
        services.AddJellyfinApiAuthorization();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        var policy = options.GetPolicy(Policies.RequiresElevation);

        Assert.NotNull(policy);
        Assert.Contains(
            policy.Requirements,
            requirement => requirement is DefaultAuthorizationRequirement);
    }
}
