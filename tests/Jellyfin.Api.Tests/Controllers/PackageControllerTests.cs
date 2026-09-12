using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Controllers;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Updates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Moq.Protected;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class PackageControllerTests
{
    [Theory]
    [InlineData(HttpStatusCode.NotFound, "Not found", HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.OK, "<html>Error</html>", HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.OK, "[]", HttpStatusCode.NoContent)]
    public async Task ValidateRepository_HttpRequest_DoesNotSaveConfiguration(
        HttpStatusCode manifestStatus,
        string manifest,
        HttpStatusCode expectedStatus)
    {
        var configuration = new Mock<IServerConfigurationManager>(MockBehavior.Strict);
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(manifestStatus) { Content = new StringContent(manifest) });
        using var manifestClient = new HttpClient(handler.Object);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(manifestClient);
        using var host = await new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddSingleton(configuration.Object);
                services.AddSingleton(Mock.Of<IInstallationManager>());
                services.AddSingleton(Mock.Of<IPluginManager>());
                services.AddSingleton(factory.Object);
                services.AddControllers().AddApplicationPart(typeof(PackageController).Assembly);
                services.AddAuthentication();
                // Authentication has separate tests; exercise routing and model binding here.
                services.AddAuthorization(options => options.AddPolicy(Policies.RequiresElevation, policy => policy.RequireAssertion(_ => true)));
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            })).StartAsync(TestContext.Current.CancellationToken);
        using var client = host.GetTestClient();

        using var response = await client.PostAsJsonAsync(
            "/Repositories/Validate",
            new RepositoryInfo { Name = "Test", Url = "https://example.com/manifest.json" },
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedStatus, response.StatusCode);
        configuration.VerifyNoOtherCalls();
    }

    [Fact]
    public void SetRepositories_PreservesLegacySaveWithoutValidation()
    {
        var configuration = new ServerConfiguration();
        var manager = new Mock<IServerConfigurationManager>();
        manager.SetupGet(x => x.Configuration).Returns(configuration);
        var controller = new PackageController(Mock.Of<IInstallationManager>(), manager.Object, Mock.Of<IPluginManager>());
        RepositoryInfo[] repositories = [new() { Name = "Missing", Url = "https://example.com/missing.json" }];

        var result = controller.SetRepositories(repositories);

        Assert.IsType<NoContentResult>(result);
        Assert.Same(repositories, configuration.PluginRepositories);
        manager.Verify(x => x.SaveConfiguration(), Times.Once);
    }
}
