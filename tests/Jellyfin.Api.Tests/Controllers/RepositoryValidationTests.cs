using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Controllers;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Updates;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.Protected;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class RepositoryValidationTests
{
    private readonly Mock<IServerConfigurationManager> _configuration = new(MockBehavior.Strict);
    private readonly Mock<HttpMessageHandler> _handler = new();

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "Not found")]
    [InlineData(HttpStatusCode.OK, "<html>Not a manifest</html>")]
    [InlineData(HttpStatusCode.OK, "null")]
    [InlineData(HttpStatusCode.OK, "{}")]
    [InlineData(HttpStatusCode.OK, "[null]")]
    [InlineData(HttpStatusCode.OK, "[{\"versions\":null}]")]
    [InlineData(HttpStatusCode.OK, "[{\"versions\":[null]}]")]
    [InlineData(HttpStatusCode.OK, "[{\"versions\":[{\"version\":\"1.bad.0\"}]}]")]
    [InlineData(HttpStatusCode.OK, "[{\"versions\":[{\"version\":\"1\"}]}]")]
    [InlineData(HttpStatusCode.OK, "[{\"versions\":[{\"version\":\"999999999999.0\"}]}]")]
    [InlineData(HttpStatusCode.OK, "[{\"versions\":[{\"version\":\"1.0\",\"targetAbi\":\"invalid\"}]}]")]
    public async Task ValidateRepository_InvalidManifest_ReturnsBadRequest(HttpStatusCode status, string content)
    {
        SetupResponse(status, content);
        Assert.IsType<BadRequestObjectResult>(await Validate("https://example.com/manifest.json", TestContext.Current.CancellationToken));
        _configuration.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("[{\"name\":\"Test\",\"versions\":[]}]")]
    [InlineData("[{\"name\":\"Future\",\"versions\":[{\"version\":\"1.0.0\",\"targetAbi\":\"999.0.0\"}]}]")]
    public async Task ValidateRepository_ValidManifest_ReturnsNoContent(string content)
    {
        SetupResponse(HttpStatusCode.OK, content);
        Assert.IsType<NoContentResult>(await Validate("https://example.com/manifest.json", TestContext.Current.CancellationToken));
        _configuration.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("relative/path")]
    [InlineData("file:///tmp/manifest.json")]
    [InlineData("ftp://example.com/manifest.json")]
    public async Task ValidateRepository_InvalidUrl_DoesNotMakeRequest(string? url)
    {
        Assert.IsType<BadRequestObjectResult>(await Validate(url, TestContext.Current.CancellationToken));
        _handler.Protected().Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ValidateRepository_NetworkFailure_ReturnsBadRequest()
    {
        SetupException(new HttpRequestException("Connection failed"));
        Assert.IsType<BadRequestObjectResult>(await Validate("https://example.com/manifest.json", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ValidateRepository_Timeout_ReturnsBadRequest()
    {
        SetupException(new TaskCanceledException("Timed out"));
        Assert.IsType<BadRequestObjectResult>(await Validate("https://example.com/manifest.json", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ValidateRepository_RequestCancelled_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        SetupException(new OperationCanceledException(cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Validate("https://example.com/manifest.json", cancellation.Token));
    }

    private void SetupResponse(HttpStatusCode status, string content) =>
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(status) { Content = new StringContent(content) });

    private void SetupException(Exception exception) =>
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);

    private async Task<ActionResult> Validate(string? url, CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient(_handler.Object);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
        var controller = new PackageController(Mock.Of<IInstallationManager>(), _configuration.Object, Mock.Of<IPluginManager>());
        return await controller.ValidateRepository(new RepositoryInfo { Url = url }, factory.Object, cancellationToken);
    }
}
