using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Api.Middleware;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dlna;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Middleware;

public class ExceptionMiddlewareTests
{
    [Fact]
    public async Task VideoPlaybackLimit_ProductionResponseHasStableCodeAndForbiddenStatus()
    {
        var context = new DefaultHttpContext();
        await using var body = new MemoryStream();
        context.Response.Body = body;
        var middleware = CreateMiddleware(_ => throw new VideoPlaybackLimitException());

        await middleware.Invoke(context);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal(nameof(PlaybackErrorCode.MaxActiveVideoStreamsReached), context.Response.Headers["X-Jellyfin-Error-Code"].ToString());
        Assert.StartsWith("application/json", context.Response.ContentType, StringComparison.Ordinal);
        body.Position = 0;
        using var document = await JsonDocument.ParseAsync(body, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(nameof(PlaybackErrorCode.MaxActiveVideoStreamsReached), document.RootElement.GetProperty("ErrorCode").GetString());
        Assert.False(document.RootElement.TryGetProperty("Message", out _));
    }

    [Fact]
    public async Task OtherSecurityException_PreservesGenericProductionResponse()
    {
        var context = new DefaultHttpContext();
        await using var body = new MemoryStream();
        context.Response.Body = body;
        var middleware = CreateMiddleware(_ => throw new SecurityException("Private user or server details"));

        await middleware.Invoke(context);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal("text/plain", context.Response.ContentType);
        Assert.False(context.Response.Headers.ContainsKey("X-Jellyfin-Error-Code"));
        body.Position = 0;
        using var reader = new StreamReader(body);
        Assert.Equal("Error processing request.", await reader.ReadToEndAsync(TestContext.Current.CancellationToken));
    }

    private static ExceptionMiddleware CreateMiddleware(RequestDelegate next)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.EnvironmentName).Returns(Environments.Production);
        return new ExceptionMiddleware(
            next,
            NullLogger<ExceptionMiddleware>.Instance,
            Mock.Of<IServerConfigurationManager>(),
            environment.Object);
    }
}
