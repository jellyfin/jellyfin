using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class FontConfigLoader
    {
        private readonly IHttpClient _httpClient;
        private readonly IApplicationPaths _appPaths;
        private readonly ILogger _logger;
        private readonly IZipClient _zipClient;
        private readonly IFileSystem _fileSystem;

        private readonly string[] _fontUrls =
        {
            "https://github.com/MediaBrowser/MediaBrowser.Resources/raw/master/ffmpeg/ARIALUNI.7z"
        };

        public FontConfigLoader(IHttpClient httpClient, IApplicationPaths appPaths, ILogger logger, IZipClient zipClient, IFileSystem fileSystem)
        {
            _httpClient = httpClient;
            _appPaths = appPaths;
            _logger = logger;
            _zipClient = zipClient;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Extracts the fonts.
        /// </summary>
        /// <param name="targetPath">The target path.</param>
        /// <returns>Task.</returns>
        public async Task DownloadFonts(string targetPath)
        {
            try
            {
                var fontsDirectory = Path.Combine(targetPath, "fonts");

                _fileSystem.CreateDirectory(fontsDirectory);

                const string fontFilename = "ARIALUNI.TTF";

                var fontFile = Path.Combine(fontsDirectory, fontFilename);

                if (_fileSystem.FileExists(fontFile))
                {
                    await WriteFontConfigFile(fontsDirectory).ConfigureAwait(false);
                }
                else
                {
                    // Kick this off, but no need to wait on it
                    var task = Task.Run(async () =>
                    {
                        await DownloadFontFile(fontsDirectory, fontFilename, new Progress<double>()).ConfigureAwait(false);

                        await WriteFontConfigFile(fontsDirectory).ConfigureAwait(false);
                    });
                }
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
        private async Task DownloadFontFile(string fontsDirectory, string fontFilename, IProgress<double> progress)
        {
            var existingFile = Directory
                .EnumerateFiles(_appPaths.ProgramDataPath, fontFilename, SearchOption.AllDirectories)
                .FirstOrDefault();

            if (existingFile != null)
            {
                try
                {
                    _fileSystem.CopyFile(existingFile, Path.Combine(fontsDirectory, fontFilename), true);
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
                progress.Report(0);

                try
                {
                    tempFile = await _httpClient.GetTempFile(new HttpRequestOptions
                    {
                        Url = url,
                        Progress = progress

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
                _fileSystem.DeleteFile(tempFile);
            }
            catch (IOException ex)
            {
                // Log this, but don't let it fail the operation
                _logger.ErrorException("Error deleting temp file {0}", ex, tempFile);
            }
        }
        private void Extract7zArchive(string archivePath, string targetPath)
        {
            _logger.Info("Extracting {0} to {1}", archivePath, targetPath);

            _zipClient.ExtractAllFrom7z(archivePath, targetPath, true);
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

            if (!_fileSystem.FileExists(fontConfigFile))
            {
                var contents = string.Format("<?xml version=\"1.0\"?><fontconfig><dir>{0}</dir><alias><family>Arial</family><prefer>Arial Unicode MS</prefer></alias></fontconfig>", fontsDirectory);

                var bytes = Encoding.UTF8.GetBytes(contents);

                using (var fileStream = _fileSystem.GetFileStream(fontConfigFile, FileMode.Create, FileAccess.Write,
                                                    FileShare.Read, true))
                {
                    await fileStream.WriteAsync(bytes, 0, bytes.Length);
                }
            }
        }
    }
}
