using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Emby.Server.Implementations;
using MediaBrowser.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Transformation
{
    public class TransformationTests : IClassFixture<JellyfinApplicationFactory>
    {
        private readonly JellyfinApplicationFactory _factory;

        public TransformationTests(JellyfinApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetIndexHtmlTransformed()
        {
            // prepare test data
            var testIndexHtml = @"<html>
</html>";
            var client = _factory.CreateClient();

            Assert.NotNull(_factory.AppPaths);
            var testIndexPath = Path.Combine(_factory.AppPaths.WebPath, "index.html");
            await File.WriteAllTextAsync(testIndexPath, testIndexHtml);

            var response = await client.GetAsync("/web/index.html").ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Assert.Contains("<<<html>>>", content, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
