using System;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Api.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Middleware;

public class ExceptionMiddlewareTests
{
    [Theory]
    [InlineData(StatusCodes.Status400BadRequest)]
    [InlineData(StatusCodes.Status413PayloadTooLarge)]
    public async Task Invoke_BadHttpRequestException_PreservesStatusWithoutErrorLog(int statusCode)
    {
        var logger = new Mock<ILogger<ExceptionMiddleware>>();
        var environment = Mock.Of<IWebHostEnvironment>(e => e.EnvironmentName == Environments.Production);
        var middleware = new ExceptionMiddleware(
            _ => throw new BadHttpRequestException("Invalid request", statusCode),
            logger.Object,
            null!,
            environment);
        var context = new DefaultHttpContext();
        using var body = new MemoryStream();
        context.Response.Body = body;

        await middleware.Invoke(context);

        Assert.Equal(statusCode, context.Response.StatusCode);
        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((_, _) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task Invoke_IOException_RemainsServerError()
    {
        var logger = new Mock<ILogger<ExceptionMiddleware>>();
        var environment = Mock.Of<IWebHostEnvironment>(e => e.EnvironmentName == Environments.Production);
        var middleware = new ExceptionMiddleware(
            _ => throw new IOException("Unexpected I/O failure"),
            logger.Object,
            null!,
            environment);
        var context = new DefaultHttpContext();
        using var body = new MemoryStream();
        context.Response.Body = body;

        await middleware.Invoke(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((_, _) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
