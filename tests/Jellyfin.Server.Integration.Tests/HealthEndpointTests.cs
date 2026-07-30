using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Emby.Server.Implementations;
using MediaBrowser.Controller;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Jellyfin.Server.Integration.Tests;

public sealed class HealthEndpointTests : IClassFixture<JellyfinApplicationFactory>
{
    private readonly JellyfinApplicationFactory _factory;

    public HealthEndpointTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Liveness_DoesNotDependOnExternalServices()
    {
        var client = _factory.CreateClient();

        using var response = await client.GetAsync("live", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Liveness_DoesNotDependOnCoreStartupCompletion()
    {
        using var factory = new JellyfinApplicationFactory();
        var client = factory.CreateClient();
        var applicationHost = factory.Services.GetRequiredService<IServerApplicationHost>();
        var startupProperty = typeof(ApplicationHost)
            .GetProperty(nameof(IServerApplicationHost.CoreStartupHasCompleted))
            ?? throw new InvalidOperationException("Core startup state property was not found.");
        var startupSetter = startupProperty.GetSetMethod(nonPublic: true)
            ?? throw new InvalidOperationException("Core startup state setter was not found.");
        startupSetter.Invoke(applicationHost, [false]);

        using var response = await client.GetAsync("live", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_RequiresCustomNetflixPostgreSql()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CustomNetflix:PostgreSqlConnectionString"] = string.Empty
                })));
        var client = factory.CreateClient();

        using var response = await client.GetAsync("ready", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
