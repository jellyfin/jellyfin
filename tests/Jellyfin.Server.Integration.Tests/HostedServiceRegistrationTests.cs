using Jellyfin.Server.HostedServices;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Controller.Session;
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

    [Fact]
    public void VideoPlaybackCoordinator_IsSharedWithHostedCleanup()
    {
        _ = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();

        Assert.Same(
            _factory.Services.GetRequiredService<IVideoPlaybackCoordinator>(),
            scope.ServiceProvider.GetRequiredService<IVideoPlaybackCoordinator>());
        Assert.Contains(_factory.Services.GetServices<IHostedService>(), service => service is VideoPlaybackCleanupService);
    }
}
