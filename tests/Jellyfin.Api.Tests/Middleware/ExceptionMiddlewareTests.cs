using System;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.CustomNetflix;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Api.Middleware.Tests;

public sealed class ExceptionMiddlewareTests
{
    [Fact]
    public async Task Invoke_CustomNetflixDependencyUnavailable_Returns503()
    {
        Assert.Equal(
            StatusCodes.Status503ServiceUnavailable,
            await InvokeAsync("/CustomNetflix/v1/home", new CustomNetflixUnavailableException("Unavailable")));
        Assert.Equal(
            StatusCodes.Status503ServiceUnavailable,
            await InvokeAsync("/CustomNetflix/v1/home", new TestDbException()));
        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            await InvokeAsync("/System/Info", new TestDbException()));
    }

    [Fact]
    public async Task Invoke_RateLimitExceeded_ReturnsExplicit429Response()
    {
        var context = await InvokeContextAsync(
            "/Videos/item/master.m3u8",
            new RateLimitExceededException("The FFmpeg concurrency limit has been reached."));
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
        Assert.Equal("5", context.Response.Headers.RetryAfter);
        Assert.Equal("The FFmpeg concurrency limit has been reached.", body);
    }

    private static async Task<int> InvokeAsync(string path, Exception exception)
        => (await InvokeContextAsync(path, exception)).Response.StatusCode;

    private static async Task<DefaultHttpContext> InvokeContextAsync(string path, Exception exception)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns(Environments.Production);
        var middleware = new ExceptionMiddleware(
            _ => throw exception,
            NullLogger<ExceptionMiddleware>.Instance,
            Mock.Of<IServerConfigurationManager>(),
            environment.Object);
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        await middleware.Invoke(context);
        return context;
    }

    private sealed class TestDbException : DbException
    {
    }
}
