using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Api.Middleware.Tests
{
    public class ExceptionMiddlewareTests
    {
        [Fact]
        public async Task Invoke_BadHttpRequest_ReturnsBadRequestAndLogsWarning()
        {
            var logger = new Mock<ILogger<ExceptionMiddleware>>();
            var middleware = new ExceptionMiddleware(
                _ => throw new BadHttpRequestException("Unexpected end of request content."),
                logger.Object,
                Mock.Of<IServerConfigurationManager>(),
                Mock.Of<IWebHostEnvironment>());
            var context = new DefaultHttpContext();
            context.Response.Body = new System.IO.MemoryStream();

            await middleware.Invoke(context);

            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
            logger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<System.Func<It.IsAnyType, System.Exception?, string>>()),
                Times.Once);
            logger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<System.Exception?>(),
                    It.IsAny<System.Func<It.IsAnyType, System.Exception?, string>>()),
                Times.Never);
        }
    }
}
