using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using Mono.Unix.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Server.Startup.Common.FFMpeg
{
    public class FFMpegLoader
    {
        private readonly IHttpClient _httpClient;
        private readonly IApplicationPaths _appPaths;
        private readonly ILogger _logger;
        private readonly IZipClient _zipClient;
        private readonly IFileSystem _fileSystem;
        private readonly NativeEnvironment _environment;
        private readonly Assembly _ownerAssembly;
        private readonly FFMpegInstallInfo _ffmpegInstallInfo;

        private readonly string[] _fontUrls =
        {
            "https://github.com/MediaBrowser/MediaBrowser.Resources/raw/master/ffmpeg/ARIALUNI.7z"
        };

        public FFMpegLoader(ILogger logger, IApplicationPaths appPaths, IHttpClient httpClient, IZipClient zipClient, IFileSystem fileSystem, NativeEnvironment environment, Assembly ownerAssembly, FFMpegInstallInfo ffmpegInstallInfo)
        {
            _logger = logger;
            _appPaths = appPaths;
            _httpClient = httpClient;
            _zipClient = zipClient;
            _fileSystem = fileSystem;
            _environment = environment;
            _ownerAssembly = ownerAssembly;
            _ffmpegInstallInfo = ffmpegInstallInfo;
        }

        public async Task<FFMpegInfo> GetFFMpegInfo(NativeEnvironment environment, StartupOptions options, IProgress<double> progress)
        {
            var customffMpegPath = options.GetOption("-ffmpeg");
            var customffProbePath = options.GetOption("-ffprobe");

            if (!string.IsNullOrWhiteSpace(customffMpegPath) && !string.IsNullOrWhiteSpace(customffProbePath))
            {
                return new FFMpegInfo
                {
                    ProbePath = customffProbePath,
                    EncoderPath = customffMpegPath,
                    Version = "custom"
                };
            }

            var downloadInfo = _ffmpegInstallInfo;

            var version = downloadInfo.Version;

            if (string.Equals(version, "path", StringComparison.OrdinalIgnoreCase))
            {
                return new FFMpegInfo
                {
                    ProbePath = downloadInfo.FFProbeFilename,
                    EncoderPath = downloadInfo.FFMpegFilename,
                    Version = version
                };
            }

            var rootEncoderPath = Path.Combine(_appPaths.ProgramDataPath, "ffmpeg");
            var versionedDirectoryPath = Path.Combine(rootEncoderPath, version);

            var info = new FFMpegInfo
            {
                ProbePath = Path.Combine(versionedDirectoryPath, downloadInfo.FFProbeFilename),
                EncoderPath = Path.Combine(versionedDirectoryPath, downloadInfo.FFMpegFilename),
                Version = version
            };

            _fileSystem.CreateDirectory(versionedDirectoryPath);

            var excludeFromDeletions = new List<string> { versionedDirectoryPath };

            if (!_fileSystem.FileExists(info.ProbePath) || !_fileSystem.FileExists(info.EncoderPath))
            {
                // ffmpeg not present. See if there's an older version we can start with
                var existingVersion = GetExistingVersion(info, rootEncoderPath);

                // No older version. Need to download and block until complete
                if (existingVersion == null)
                {
                    await DownloadFFMpeg(downloadInfo, versionedDirectoryPath, progress).ConfigureAwait(false);
                }
                else
                {
                    // Older version found. 
                    // Start with that. Download new version in the background.
                    var newPath = versionedDirectoryPath;
                    Task.Run(() => DownloadFFMpegInBackground(downloadInfo, newPath));

                    info = existingVersion;
                    versionedDirectoryPath = Path.GetDirectoryName(info.EncoderPath);

                    excludeFromDeletions.Add(versionedDirectoryPath);
                }
            }

            if (_environment.OperatingSystem == OperatingSystem.Windows)
            {
                await DownloadFonts(versionedDirectoryPath).ConfigureAwait(false);
            }

            DeleteOlderFolders(Path.GetDirectoryName(versionedDirectoryPath), excludeFromDeletions);

            // Allow just one of these to be overridden, if desired.
            if (!string.IsNullOrWhiteSpace(customffMpegPath))
            {
                info.EncoderPath = customffMpegPath;
            }
            if (!string.IsNullOrWhiteSpace(customffProbePath))
            {
                info.EncoderPath = customffProbePath;
            }

            return info;
        }

        private void DeleteOlderFolders(string path, IEnumerable<string> excludeFolders)
        {
            var folders = Directory.GetDirectories(path)
                .Where(i => !excludeFolders.Contains(i, StringComparer.OrdinalIgnoreCase))
                .ToList();

            foreach (var folder in folders)
            {
                DeleteFolder(folder);
            }
        }

        private void DeleteFolder(string path)
        {
            try
            {
                _fileSystem.DeleteDirectory(path, true);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error deleting {0}", ex, path);
            }
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
                        Version = Path.GetFileName(Path.GetDirectoryName(probe))
                    };
                }
            }

            return null;
        }

        private async void DownloadFFMpegInBackground(FFMpegInstallInfo downloadinfo, string directory)
        {
            try
            {
                await DownloadFFMpeg(downloadinfo, directory, new Progress<double>()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error downloading ffmpeg", ex);
            }
        }

        private async Task DownloadFFMpeg(FFMpegInstallInfo downloadinfo, string directory, IProgress<double> progress)
        {
            if (downloadinfo.IsEmbedded)
            {
                var tempFile = Path.Combine(_appPaths.TempDirectory, Guid.NewGuid().ToString());
                _fileSystem.CreateDirectory(Path.GetDirectoryName(tempFile));

                using (var stream = _ownerAssembly.GetManifestResourceStream(downloadinfo.DownloadUrls[0]))
                {
                    using (var fs = _fileSystem.GetFileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                    {
                        await stream.CopyToAsync(fs).ConfigureAwait(false);
                    }
                }
                ExtractFFMpeg(downloadinfo, tempFile, directory);
                return;
            }

            foreach (var url in downloadinfo.DownloadUrls)
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

                    ExtractFFMpeg(downloadinfo, tempFile, directory);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error downloading {0}", ex, url);
                }
            }

            if (downloadinfo.DownloadUrls.Length == 0)
            {
                throw new ApplicationException("ffmpeg unvailable. Please install it and start the server with two command line arguments: -ffmpeg \"{PATH}\" and -ffprobe \"{PATH}\"");
            }

            throw new ApplicationException("Unable to download required components. Please try again later.");
        }

        private void ExtractFFMpeg(FFMpegInstallInfo downloadinfo, string tempFile, string targetFolder)
        {
            _logger.Info("Extracting ffmpeg from {0}", tempFile);

            var tempFolder = Path.Combine(_appPaths.TempDirectory, Guid.NewGuid().ToString());

            _fileSystem.CreateDirectory(tempFolder);

            try
            {
                ExtractArchive(downloadinfo, tempFile, tempFolder);

                var files = Directory.EnumerateFiles(tempFolder, "*", SearchOption.AllDirectories)
                    .ToList();

                foreach (var file in files.Where(i =>
                    {
                        var filename = Path.GetFileName(i);

                        return
                            string.Equals(filename, downloadinfo.FFProbeFilename, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(filename, downloadinfo.FFMpegFilename, StringComparison.OrdinalIgnoreCase);
                    }))
                {
                    var targetFile = Path.Combine(targetFolder, Path.GetFileName(file));
                    _fileSystem.CopyFile(file, targetFile, true);
                    SetFilePermissions(targetFile);
                }
            }
            finally
            {
                DeleteFile(tempFile);
            }
        }

        private void SetFilePermissions(string path)
        {
            // Linux: File permission to 666, and user's execute bit
            if (_environment.OperatingSystem == OperatingSystem.Bsd || _environment.OperatingSystem == OperatingSystem.Linux || _environment.OperatingSystem == OperatingSystem.Osx)
            {
                _logger.Info("Syscall.chmod {0} FilePermissions.DEFFILEMODE | FilePermissions.S_IRWXU | FilePermissions.S_IXGRP | FilePermissions.S_IXOTH", path);

                Syscall.chmod(path, FilePermissions.DEFFILEMODE | FilePermissions.S_IRWXU | FilePermissions.S_IXGRP | FilePermissions.S_IXOTH);
            }
        }

        private void ExtractArchive(FFMpegInstallInfo downloadinfo, string archivePath, string targetPath)
        {
            _logger.Info("Extracting {0} to {1}", archivePath, targetPath);

            if (string.Equals(downloadinfo.ArchiveType, "7z", StringComparison.OrdinalIgnoreCase))
            {
                _zipClient.ExtractAllFrom7z(archivePath, targetPath, true);
            }
            else if (string.Equals(downloadinfo.ArchiveType, "gz", StringComparison.OrdinalIgnoreCase))
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
                _fileSystem.DeleteFile(path);
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
