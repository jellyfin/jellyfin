using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace MediaBrowser.ServerApplication.Native
{
    /// <summary>
    /// Class Authorization
    /// </summary>
    public static class ServerAuthorization
    {
        /// <summary>
        /// Authorizes the server.
        /// </summary>
        /// <param name="udpPort">The UDP port.</param>
        /// <param name="httpServerPort">The HTTP server port.</param>
        /// <param name="httpsServerPort">The HTTPS server port.</param>
        /// <param name="tempDirectory">The temp directory.</param>
        public static void AuthorizeServer(int udpPort, int httpServerPort, int httpsServerPort, string applicationPath, string tempDirectory)
        {
            Directory.CreateDirectory(tempDirectory);

            // Create a temp file path to extract the bat file to
            var tmpFile = Path.Combine(tempDirectory, Guid.NewGuid() + ".bat");

            // Extract the bat file
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(ServerAuthorization).Namespace + ".RegisterServer.bat"))
            {
                using (var fileStream = File.Create(tmpFile))
                {
                    stream.CopyTo(fileStream);
                }
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = tmpFile,

                Arguments = string.Format("{0} {1} {2} \"{3}\"", udpPort, httpServerPort, httpsServerPort, applicationPath),

                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Verb = "runas",
                ErrorDialog = false
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
            }
        }
    }
}
