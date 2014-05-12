using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Progress;
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
#if __MonoCS__
using Mono.Unix.Native;
#endif

namespace MediaBrowser.ServerApplication.FFMpeg
{
    public class FFMpegDownloader
    {
        private readonly IHttpClient _httpClient;
        private readonly IApplicationPaths _appPaths;
        private readonly ILogger _logger;
        private readonly IZipClient _zipClient;
        private readonly IFileSystem _fileSystem;

        private readonly string[] _fontUrls = new[]
            {
                "https://www.dropbox.com/s/pj847twf7riq0j7/ARIALUNI.7z?dl=1"
            };

        public FFMpegDownloader(ILogger logger, IApplicationPaths appPaths, IHttpClient httpClient, IZipClient zipClient, IFileSystem fileSystem)
        {
            _logger = logger;
            _appPaths = appPaths;
            _httpClient = httpClient;
            _zipClient = zipClient;
            _fileSystem = fileSystem;
        }

        public async Task<FFMpegInfo> GetFFMpegInfo(IProgress<double> progress)
        {
            var rootEncoderPath = Path.Combine(_appPaths.ProgramDataPath, "ffmpeg");
            var versionedDirectoryPath = Path.Combine(rootEncoderPath, FFMpegDownloadInfo.Version);

            var info = new FFMpegInfo
            {
                ProbePath = Path.Combine(versionedDirectoryPath, FFMpegDownloadInfo.FFProbeFilename),
                EncoderPath = Path.Combine(versionedDirectoryPath, FFMpegDownloadInfo.FFMpegFilename),
                Version = FFMpegDownloadInfo.Version
            };

            Directory.CreateDirectory(versionedDirectoryPath);

            if (!File.Exists(info.ProbePath) || !File.Exists(info.EncoderPath))
            {
                // ffmpeg not present. See if there's an older version we can start with
                var existingVersion = GetExistingVersion(info, rootEncoderPath);

                // No older version. Need to download and block until complete
                if (existingVersion == null)
                {
                    await DownloadFFMpeg(versionedDirectoryPath, progress).ConfigureAwait(false);
                }
                else
                {
                    // Older version found. 
                    // Start with that. Download new version in the background.
                    var newPath = versionedDirectoryPath;
                    Task.Run(() => DownloadFFMpegInBackground(newPath));

                    info = existingVersion;
                    versionedDirectoryPath = Path.GetDirectoryName(info.EncoderPath);
                }
            }

            await DownloadFonts(versionedDirectoryPath).ConfigureAwait(false);

            return info;
        }

        private FFMpegInfo GetExistingVersion(FFMpegInfo info, string rootEncoderPath)
        {
            var encoderFilename = Path.GetFileName(info.EncoderPath);
            var probeFilename = Path.GetFileName(info.ProbePath);

            foreach (var directory in Directory.EnumerateDirectories(rootEncoderPath, "*", SearchOption.TopDirectoryOnly)
                .ToList())
            {
                var allFiles = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).ToList();

                var encoder = allFiles.FirstOrDefault(i => string.Equals(Path.GetFileName(i), encoderFilename, StringComparison.OrdinalIgnoreCase));
                var probe = allFiles.FirstOrDefault(i => string.Equals(Path.GetFileName(i), probeFilename, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(encoder) &&
                    !string.IsNullOrWhiteSpace(probe))
                {
                    return new FFMpegInfo
                    {
                         EncoderPath = encoder,
                         ProbePath = probe,
                         Version = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(probe))
                    };
                }
            }

            return null;
        }

        private async void DownloadFFMpegInBackground(string directory)
        {
            try
            {
                await DownloadFFMpeg(directory, new Progress<double>()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error downloading ffmpeg", ex);
            }
        }

        private async Task DownloadFFMpeg(string directory, IProgress<double> progress)
        {
            foreach (var url in FFMpegDownloadInfo.FfMpegUrls)
            {
                progress.Report(0);

                try
                {
                    var tempFile = await _httpClient.GetTempFile(new HttpRequestOptions
                    {
                        Url = url,
                        CancellationToken = CancellationToken.None,
                        Progress = progress

                    }).ConfigureAwait(false);

                    ExtractFFMpeg(tempFile, directory);
                    return;
                }
                catch (HttpException ex)
                {
                    _logger.ErrorException("Error downloading {0}", ex, url);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error unpacking {0}", ex, url);
                }
            }

            throw new ApplicationException("Unable to download required components. Please try again later.");
        }

        private void ExtractFFMpeg(string tempFile, string targetFolder)
        {
            _logger.Info("Extracting ffmpeg from {0}", tempFile);

            var tempFolder = Path.Combine(_appPaths.TempDirectory, Guid.NewGuid().ToString());

            Directory.CreateDirectory(tempFolder);

            try
            {
                ExtractArchive(tempFile, tempFolder);

                var files = Directory.EnumerateFiles(tempFolder, "*", SearchOption.AllDirectories).ToList();

                foreach (var file in files.Where(i =>
                    {
                        var filename = Path.GetFileName(i);

                        return
                            string.Equals(filename, FFMpegDownloadInfo.FFProbeFilename, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(filename, FFMpegDownloadInfo.FFMpegFilename, StringComparison.OrdinalIgnoreCase);
                    }))
                {
                    File.Copy(file, Path.Combine(targetFolder, Path.GetFileName(file)), true);
                    #if __MonoCS__
                    //Linux: File permission to 666, and user's execute bit
                    if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
                    {
                        Syscall.chmod(Path.Combine(targetFolder, Path.GetFileName(file)), FilePermissions.DEFFILEMODE | FilePermissions.S_IXUSR);
                    }
                    #endif
                }
            }
            finally
            {
                DeleteFile(tempFile);
            }
        }

        private void ExtractArchive(string archivePath, string targetPath)
        {
            _logger.Info("Extracting {0} to {1}", archivePath, targetPath);
            
            if (string.Equals(FFMpegDownloadInfo.ArchiveType, "7z", StringComparison.OrdinalIgnoreCase))
            {
                _zipClient.ExtractAllFrom7z(archivePath, targetPath, true);
            }
            else if (string.Equals(FFMpegDownloadInfo.ArchiveType, "gz", StringComparison.OrdinalIgnoreCase))
            {
                _zipClient.ExtractAllFromTar(archivePath, targetPath, true);
            }
        }
        private void Extract7zArchive(string archivePath, string targetPath)
        {
            _logger.Info("Extracting {0} to {1}", archivePath, targetPath);

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
        /// <returns>Task.</returns>
        private async Task DownloadFonts(string targetPath)
        {
            try
            {
                var fontsDirectory = Path.Combine(targetPath, "fonts");

                Directory.CreateDirectory(fontsDirectory);

                const string fontFilename = "ARIALUNI.TTF";

                var fontFile = Path.Combine(fontsDirectory, fontFilename);

                if (File.Exists(fontFile))
                {
                    await WriteFontConfigFile(fontsDirectory).ConfigureAwait(false);
                }
                else
                {
                    // Kick this off, but no need to wait on it
                    Task.Run(async () =>
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

                using (var fileStream = _fileSystem.GetFileStream(fontConfigFile, FileMode.Create, FileAccess.Write,
                                                    FileShare.Read, true))
                {
                    await fileStream.WriteAsync(bytes, 0, bytes.Length);
                }
            }
        }
    }
}
