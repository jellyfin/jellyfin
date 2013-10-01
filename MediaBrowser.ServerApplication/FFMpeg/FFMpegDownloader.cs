using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.ServerApplication.FFMpeg
{
    public class FFMpegDownloader
    {
        private readonly IHttpClient _httpClient;
        private readonly IApplicationPaths _appPaths;
        private readonly ILogger _logger;
        private readonly IZipClient _zipClient;

        private const string Version = "ffmpeg20130904";

        private readonly string[] _fontUrls = new[]
            {
                "https://www.dropbox.com/s/pj847twf7riq0j7/ARIALUNI.7z?dl=1"
            };

        private readonly string[] _ffMpegUrls = new[]
                {
                    "https://github.com/MediaBrowser/MediaBrowser.Resources/raw/master/ffmpeg/ffmpeg-20130904-git-f974289-win32-static.7z",

                    "http://ffmpeg.zeranoe.com/builds/win32/static/ffmpeg-20130904-git-f974289-win32-static.7z",
                    "https://www.dropbox.com/s/a81cb2ob23fwcfs/ffmpeg-20130904-git-f974289-win32-static.7z?dl=1"
                };

        public FFMpegDownloader(ILogger logger, IApplicationPaths appPaths, IHttpClient httpClient, IZipClient zipClient)
        {
            _logger = logger;
            _appPaths = appPaths;
            _httpClient = httpClient;
            _zipClient = zipClient;
        }

        public async Task<FFMpegInfo> GetFFMpegInfo()
        {
            var versionedDirectoryPath = Path.Combine(GetMediaToolsPath(true), Version);

            var info = new FFMpegInfo
            {
                ProbePath = Path.Combine(versionedDirectoryPath, "ffprobe.exe"),
                Path = Path.Combine(versionedDirectoryPath, "ffmpeg.exe"),
                Version = Version
            };

            Directory.CreateDirectory(versionedDirectoryPath);

            var tasks = new List<Task>();

            if (!File.Exists(info.ProbePath) || !File.Exists(info.Path))
            {
                tasks.Add(DownloadFFMpeg(info));
            }

            tasks.Add(DownloadFonts(versionedDirectoryPath));

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return info;
        }

        private async Task DownloadFFMpeg(FFMpegInfo info)
        {
            foreach (var url in _ffMpegUrls)
            {
                try
                {
                    var tempFile = await DownloadFFMpeg(info, url).ConfigureAwait(false);

                    ExtractFFMpeg(tempFile, Path.GetDirectoryName(info.Path));
                    return;
                }
                catch (HttpException ex)
                {

                }
            }

            throw new ApplicationException("Unable to download required components. Please try again later.");
        }

        private Task<string> DownloadFFMpeg(FFMpegInfo info, string url)
        {
            return _httpClient.GetTempFile(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = CancellationToken.None,
                Progress = new Progress<double>(),

                // Make it look like a browser
                // Try to hide that we're direct linking
                UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/30.0.1599.47 Safari/537.36"
            });
        }

        private void ExtractFFMpeg(string tempFile, string targetFolder)
        {
            _logger.Debug("Extracting ffmpeg from {0}", tempFile);

            var tempFolder = Path.Combine(_appPaths.TempDirectory, Guid.NewGuid().ToString());

            Directory.CreateDirectory(tempFolder);

            try
            {
                Extract7zArchive(tempFile, tempFolder);

                var files = Directory.EnumerateFiles(tempFolder, "*.exe", SearchOption.AllDirectories).ToList();

                foreach (var file in files)
                {
                    File.Copy(file, Path.Combine(targetFolder, Path.GetFileName(file)));
                }
            }
            finally
            {
                DeleteFile(tempFile);
            }
        }

        private void Extract7zArchive(string archivePath, string targetPath)
        {
            _zipClient.ExtractAllFrom7z(archivePath, targetPath, true);
        }

        private void DeleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException ex)
            {
                _logger.ErrorException("Error deleting temp file {0}", ex, path);
            }
        }

        /// <summary>
        /// Extracts the fonts.
        /// </summary>
        /// <param name="targetPath">The target path.</param>
        private async Task DownloadFonts(string targetPath)
        {
            try
            {
                var fontsDirectory = Path.Combine(targetPath, "fonts");

                Directory.CreateDirectory(fontsDirectory);

                const string fontFilename = "ARIALUNI.TTF";

                var fontFile = Path.Combine(fontsDirectory, fontFilename);

                if (!File.Exists(fontFile))
                {
                    await DownloadFontFile(fontsDirectory, fontFilename).ConfigureAwait(false);
                }

                await WriteFontConfigFile(fontsDirectory).ConfigureAwait(false);
            }
            catch (HttpException ex)
            {
                // Don't let the server crash because of this
                _logger.ErrorException("Error downloading ffmpeg font files", ex);
            }
            catch (Exception ex)
            {
                // Don't let the server crash because of this
                _logger.ErrorException("Error writing ffmpeg font files", ex);
            }
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

            string tempFile = null;

            foreach (var url in _fontUrls)
            {
                try
                {
                    tempFile = await _httpClient.GetTempFile(new HttpRequestOptions
                    {
                        Url = url,
                        Progress = new Progress<double>()

                    }).ConfigureAwait(false);

                    break;
                }
                catch (Exception ex)
                {
                    // The core can function without the font file, so handle this
                    _logger.ErrorException("Failed to download ffmpeg font file from {0}", ex, url);
                }
            }

            if (string.IsNullOrEmpty(tempFile))
            {
                return;
            }

            Extract7zArchive(tempFile, fontsDirectory);

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

            Directory.CreateDirectory(path);

            return path;
        }
    }
}
