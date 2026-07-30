using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.CustomNetflix;
using MediaBrowser.Controller.CustomNetflix;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddCustomNetflixServices_InvalidPostgreSqlConnectionStringDoesNotFailStartup()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CustomNetflix:PostgreSqlConnectionString"] = "invalid"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddCustomNetflixServices(configuration);

        var descriptor = Assert.Single(services, service => service.ServiceType == typeof(ICustomNetflixRepository));
        var repository = Assert.IsType<DisabledCustomNetflixRepository>(descriptor.ImplementationInstance);
        Assert.True(repository.IsEnabled);
        await Assert.ThrowsAsync<CustomNetflixUnavailableException>(
            () => repository.EnsureSchemaAsync(CancellationToken.None));
    }

    [Fact]
    public void AddCustomNetflixServices_BindsConfiguredProfileLimit()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CustomNetflix:MaxProfilesPerAccount"] = "8"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddCustomNetflixServices(configuration);

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(IOptions<CustomNetflixOptions>));
        var options = Assert.IsAssignableFrom<IOptions<CustomNetflixOptions>>(descriptor.ImplementationInstance);
        Assert.Equal(8, options.Value.MaxProfilesPerAccount);
    }
}
