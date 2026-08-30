using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Middleware;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Middleware
{
    /// <summary>
    /// The login path refuses out-of-schedule users by throwing, so the error code has
    /// to be attached where the exception is turned into a response. Without it a client
    /// cannot tell "outside your allowed hours" from a wrong password.
    /// </summary>
    public class ExceptionMiddlewareTests
    {
        [Fact]
        public async Task ParentalControlExceptionSetsErrorCodeHeader()
        {
            var context = await InvokeWith(new ParentalControlException("User is not allowed access at this time."));

            Assert.Equal((int)HttpStatusCode.Forbidden, context.Response.StatusCode);
            Assert.Equal(
                ApplicationErrorCodes.ParentalControl,
                context.Response.Headers[ApplicationErrorCodes.HeaderName].ToString());
        }

        [Fact]
        public async Task WrappedParentalControlExceptionSetsErrorCodeHeader()
        {
            // UserController catches SecurityException and rethrows a plain one to prefix
            // the remote IP to the message, which erases the original type. The error code
            // must survive that.
            var inner = new ParentalControlException("User is not allowed access at this time.");
            var context = await InvokeWith(new SecurityException("[127.0.0.1] User is not allowed access at this time.", inner));

            Assert.Equal((int)HttpStatusCode.Forbidden, context.Response.StatusCode);
            Assert.Equal(
                ApplicationErrorCodes.ParentalControl,
                context.Response.Headers[ApplicationErrorCodes.HeaderName].ToString());
        }

        [Fact]
        public async Task PlainSecurityExceptionDoesNotSetErrorCodeHeader()
        {
            // Still a 403, but not a parental-control one -- it must not be mislabelled.
            var context = await InvokeWith(new SecurityException("Forbidden."));

            Assert.Equal((int)HttpStatusCode.Forbidden, context.Response.StatusCode);
            Assert.False(context.Response.Headers.ContainsKey(ApplicationErrorCodes.HeaderName));
        }

        [Fact]
        public async Task UnrelatedExceptionDoesNotSetErrorCodeHeader()
        {
            var context = await InvokeWith(new ArgumentException("bad argument"));

            Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
            Assert.False(context.Response.Headers.ContainsKey(ApplicationErrorCodes.HeaderName));
        }

        private static async Task<DefaultHttpContext> InvokeWith(Exception exception)
        {
            var configurationManager = new Mock<IServerConfigurationManager>();
            configurationManager
                .Setup(c => c.ApplicationPaths)
                .Returns(Mock.Of<IServerApplicationPaths>());

            var hostEnvironment = new Mock<IWebHostEnvironment>();
            hostEnvironment.Setup(h => h.EnvironmentName).Returns("Production");

            var sut = new ExceptionMiddleware(
                _ => throw exception,
                NullLogger<ExceptionMiddleware>.Instance,
                configurationManager.Object,
                hostEnvironment.Object);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            await sut.Invoke(context);

            return context;
        }
    }
}
