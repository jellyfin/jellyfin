using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.ServerApplication.Implementations
{
    public class FFMpegDownloader
    {
        private readonly IZipClient _zipClient;
        private readonly IHttpClient _httpClient;
        private readonly IApplicationPaths _appPaths;
        private readonly ILogger _logger;

        public FFMpegDownloader(ILogger logger, IApplicationPaths appPaths, IHttpClient httpClient, IZipClient zipClient)
        {
            _logger = logger;
            _appPaths = appPaths;
            _httpClient = httpClient;
            _zipClient = zipClient;
        }

        public async Task<FFMpegInfo> GetFFMpegInfo()
        {
            var assembly = GetType().Assembly;

            var prefix = GetType().Namespace + ".";

            var srch = prefix + "ffmpeg";

            var resource = assembly.GetManifestResourceNames().First(r => r.StartsWith(srch));

            var filename =
                resource.Substring(resource.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) + prefix.Length);

            var versionedDirectoryPath = Path.Combine(GetMediaToolsPath(true),
                                                      Path.GetFileNameWithoutExtension(filename));

            if (!Directory.Exists(versionedDirectoryPath))
            {
                Directory.CreateDirectory(versionedDirectoryPath);
            }

            await ExtractTools(assembly, resource, versionedDirectoryPath).ConfigureAwait(false);

            return new FFMpegInfo
            {
                ProbePath = Path.Combine(versionedDirectoryPath, "ffprobe.exe"),
                Path = Path.Combine(versionedDirectoryPath, "ffmpeg.exe"),
                Version = Path.GetFileNameWithoutExtension(versionedDirectoryPath)
            };
        }

        /// <summary>
        /// Extracts the tools.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="zipFileResourcePath">The zip file resource path.</param>
        /// <param name="targetPath">The target path.</param>
        private async Task ExtractTools(Assembly assembly, string zipFileResourcePath, string targetPath)
        {
            using (var resourceStream = assembly.GetManifestResourceStream(zipFileResourcePath))
            {
                _zipClient.ExtractAll(resourceStream, targetPath, false);
            }

            try
            {
                await DownloadFonts(targetPath).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error getting ffmpeg font files", ex);
            }
        }

        private const string FontUrl = "https://www.dropbox.com/s/9nb76tybcsw5xrk/ARIALUNI.zip?dl=1";

        /// <summary>
        /// Extracts the fonts.
        /// </summary>
        /// <param name="targetPath">The target path.</param>
        private async Task DownloadFonts(string targetPath)
        {
            var fontsDirectory = Path.Combine(targetPath, "fonts");

            if (!Directory.Exists(fontsDirectory))
            {
                Directory.CreateDirectory(fontsDirectory);
            }

            const string fontFilename = "ARIALUNI.TTF";

            var fontFile = Path.Combine(fontsDirectory, fontFilename);

            if (!File.Exists(fontFile))
            {
                await DownloadFontFile(fontsDirectory, fontFilename).ConfigureAwait(false);
            }

            await WriteFontConfigFile(fontsDirectory).ConfigureAwait(false);
        }

        /// <summary>
        /// Downloads the font file.
        /// </summary>
        /// <param name="fontsDirectory">The fonts directory.</param>
        /// <param name="fontFilename">The font filename.</param>
        /// <returns>Task.</returns>
        private async Task DownloadFontFile(string fontsDirectory, string fontFilename)
        {
            var existingFile = Directory
                .EnumerateFiles(_appPaths.ProgramDataPath, fontFilename, SearchOption.AllDirectories)
                .FirstOrDefault();

            if (existingFile != null)
            {
                try
                {
                    File.Copy(existingFile, Path.Combine(fontsDirectory, fontFilename), true);
                    return;
                }
                catch (IOException ex)
                {
                    // Log this, but don't let it fail the operation
                    _logger.ErrorException("Error copying file", ex);
                }
            }

            var tempFile = await _httpClient.GetTempFile(new HttpRequestOptions
            {
                Url = FontUrl,
                Progress = new Progress<double>()
            });

            _zipClient.ExtractAll(tempFile, fontsDirectory, true);

            try
            {
                File.Delete(tempFile);
            }
            catch (IOException ex)
            {
                // Log this, but don't let it fail the operation
                _logger.ErrorException("Error deleting temp file {0}", ex, tempFile);
            }
        }

        /// <summary>
        /// Writes the font config file.
        /// </summary>
        /// <param name="fontsDirectory">The fonts directory.</param>
        /// <returns>Task.</returns>
        private async Task WriteFontConfigFile(string fontsDirectory)
        {
            const string fontConfigFilename = "fonts.conf";
            var fontConfigFile = Path.Combine(fontsDirectory, fontConfigFilename);

            if (!File.Exists(fontConfigFile))
            {
                var contents = string.Format("<?xml version=\"1.0\"?><fontconfig><dir>{0}</dir><alias><family>Arial</family><prefer>Arial Unicode MS</prefer></alias></fontconfig>", fontsDirectory);

                var bytes = Encoding.UTF8.GetBytes(contents);

                using (var fileStream = new FileStream(fontConfigFile, FileMode.Create, FileAccess.Write,
                                                    FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize,
                                                    FileOptions.Asynchronous))
                {
                    await fileStream.WriteAsync(bytes, 0, bytes.Length);
                }
            }
        }

        /// <summary>
        /// Gets the media tools path.
        /// </summary>
        /// <param name="create">if set to <c>true</c> [create].</param>
        /// <returns>System.String.</returns>
        private string GetMediaToolsPath(bool create)
        {
            var path = Path.Combine(_appPaths.ProgramDataPath, "ffmpeg");

            if (create && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }
    }

    public class FFMpegInfo
    {
        public string Path { get; set; }
        public string ProbePath { get; set; }
        public string Version { get; set; }
    }
}
