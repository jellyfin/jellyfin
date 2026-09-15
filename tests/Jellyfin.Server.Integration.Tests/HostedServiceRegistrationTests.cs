using Jellyfin.Server.Implementations.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Jellyfin.Server.Integration.Tests;

public sealed class HostedServiceRegistrationTests : IClassFixture<JellyfinApplicationFactory>
{
    private readonly JellyfinApplicationFactory _factory;

    public HostedServiceRegistrationTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void DeviceAccessHost_IsRegisteredAsHostedService()
    {
        _ = _factory.CreateClient();

        var hostedServices = _factory.Services.GetServices<IHostedService>();

        Assert.Contains(hostedServices, service => service is DeviceAccessHost);
    }
}
