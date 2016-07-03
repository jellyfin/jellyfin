using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using Mono.Unix.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly FFMpegInstallInfo _ffmpegInstallInfo;

        public FFMpegLoader(ILogger logger, IApplicationPaths appPaths, IHttpClient httpClient, IZipClient zipClient, IFileSystem fileSystem, NativeEnvironment environment, FFMpegInstallInfo ffmpegInstallInfo)
        {
            _logger = logger;
            _appPaths = appPaths;
            _httpClient = httpClient;
            _zipClient = zipClient;
            _fileSystem = fileSystem;
            _environment = environment;
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
                    Version = "external"
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

            if (string.Equals(version, "0", StringComparison.OrdinalIgnoreCase))
            {
                return new FFMpegInfo();
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
                    var success = await DownloadFFMpeg(downloadInfo, versionedDirectoryPath, progress).ConfigureAwait(false);
                    if (!success)
                    {
                        return new FFMpegInfo();
                    }
                }
                else
                {
                    info = existingVersion;
                    versionedDirectoryPath = Path.GetDirectoryName(info.EncoderPath);
                    excludeFromDeletions.Add(versionedDirectoryPath);
                }
            }

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

        private async Task<bool> DownloadFFMpeg(FFMpegInstallInfo downloadinfo, string directory, IProgress<double> progress)
        {
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
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error downloading {0}", ex, url);
                }
            }
            return false;
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

    }
}
