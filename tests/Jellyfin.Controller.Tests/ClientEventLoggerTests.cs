using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.ClientEvent;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests
{
    public class ClientEventLoggerTests
    {
        [Theory]
        [InlineData("../../../../etc/passwd", "1.0")]
        [InlineData("..\\..\\windows\\system32", "1.0")]
        [InlineData("normal-client", "../../../etc/passwd")]
        [InlineData("/absolute/path", "1.0")]
        public async Task WriteDocumentAsync_TraversalInput_StaysInsideLogDirectory(string clientName, string clientVersion)
        {
            var logDir = Path.Combine(Path.GetTempPath(), "jellyfin-clientlog-test-" + Path.GetRandomFileName());
            Directory.CreateDirectory(logDir);
            try
            {
                var paths = new Mock<IServerApplicationPaths>();
                paths.Setup(p => p.LogDirectoryPath).Returns(logDir);

                var logger = new ClientEventLogger(paths.Object);
                using var contents = new MemoryStream(Encoding.UTF8.GetBytes("payload"));

                var fileName = await logger.WriteDocumentAsync(clientName, clientVersion, contents);

                var resolved = Path.GetFullPath(Path.Combine(logDir, fileName));
                var rootWithSep = Path.GetFullPath(logDir) + Path.DirectorySeparatorChar;
                Assert.StartsWith(rootWithSep, resolved, StringComparison.Ordinal);
                Assert.True(File.Exists(resolved));
            }
            finally
            {
                Directory.Delete(logDir, recursive: true);
            }
        }
    }
}
