using System;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Diagnostics;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using UtfUnknown;

namespace MediaBrowser.MediaEncoding.Attachments
{
    public class AttachmentExtractor : IAttachmentExtractor
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IMediaSourceManager _mediaSourceManager;

        public AttachmentExtractor(
            ILibraryManager libraryManager,
            ILogger<AttachmentExtractor> logger,
            IApplicationPaths appPaths,
            IFileSystem fileSystem,
            IMediaEncoder mediaEncoder,
            IMediaSourceManager mediaSourceManager)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _appPaths = appPaths;
            _fileSystem = fileSystem;
            _mediaEncoder = mediaEncoder;
            _mediaSourceManager = mediaSourceManager;
        }

        private string AttachmentCachePath => Path.Combine(_appPaths.DataPath, "attachments");

        public async Task<(MediaAttachment attachment, Stream stream)> GetAttachment(BaseItem item, string mediaSourceId, int attachmentStreamIndex, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (string.IsNullOrWhiteSpace(mediaSourceId))
            {
                throw new ArgumentNullException(nameof(mediaSourceId));
            }

            var mediaSources = await _mediaSourceManager.GetPlayackMediaSources(item, null, true, false, cancellationToken).ConfigureAwait(false);
            var mediaSource = mediaSources
                .FirstOrDefault(i => string.Equals(i.Id, mediaSourceId, StringComparison.OrdinalIgnoreCase));
            if (mediaSource == null)
            {
                throw new ResourceNotFoundException($"MediaSource {mediaSourceId} not found");
            }
            var mediaAttachment = mediaSource.MediaAttachments
                .FirstOrDefault(i => i.Index == attachmentStreamIndex);
            if (mediaAttachment == null)
            {
                throw new ResourceNotFoundException($"MediaSource {mediaSourceId} has no attachment with stream index {attachmentStreamIndex}");
            }
            var attachmentStream = await GetAttachmentStream(mediaSource, mediaAttachment, cancellationToken)
                    .ConfigureAwait(false);

            return (mediaAttachment, attachmentStream);
        }

        private async Task<Stream> GetAttachmentStream(
            MediaSourceInfo mediaSource,
            MediaAttachment mediaAttachment,
            CancellationToken cancellationToken)
        {
            var inputFiles = new[] { mediaSource.Path };
            var attachmentPath = await GetReadableFile(mediaSource.Path, inputFiles, mediaSource.Protocol, mediaAttachment, cancellationToken).ConfigureAwait(false);
            var stream = await GetAttachmentStream(attachmentPath, cancellationToken).ConfigureAwait(false);
            return stream;
        }

        private async Task<Stream> GetAttachmentStream(
            string path,
            CancellationToken cancellationToken)
        {
            return File.OpenRead(path);
        }

        private async Task<String> GetReadableFile(
            string mediaPath,
            string[] inputFiles,
            MediaProtocol protocol,
            MediaAttachment mediaAttachment,
            CancellationToken cancellationToken)
        {
            var outputPath = GetAttachmentCachePath(mediaPath, protocol, mediaAttachment.Index);
            await ExtractAttachment(inputFiles, protocol, mediaAttachment.Index, outputPath, cancellationToken)
                .ConfigureAwait(false);

            return outputPath;
        }

        private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphoreLocks =
            new ConcurrentDictionary<string, SemaphoreSlim>();

        private SemaphoreSlim GetLock(string filename)
        {
            return _semaphoreLocks.GetOrAdd(filename, key => new SemaphoreSlim(1, 1));
        }

        private async Task ExtractAttachment(
            string[] inputFiles,
            MediaProtocol protocol,
            int attachmentStreamIndex,
            string outputPath,
            CancellationToken cancellationToken)
        {
            var semaphore = GetLock(outputPath);

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!File.Exists(outputPath))
                {
                    await ExtractAttachmentInternal(_mediaEncoder.GetInputArgument(inputFiles, protocol), attachmentStreamIndex, outputPath, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task ExtractAttachmentInternal(
            string inputPath,
            int attachmentStreamIndex,
            string outputPath,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(inputPath))
            {
                throw new ArgumentNullException(nameof(inputPath));
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentNullException(nameof(outputPath));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var processArgs = string.Format("-dump_attachment:{1} {2} -i {0} -t 0 -f null null", inputPath, attachmentStreamIndex, outputPath);
            var startInfo = new ProcessStartInfo
            {
                Arguments = processArgs,
                FileName = _mediaEncoder.EncoderPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                ErrorDialog = false
            };
            var process = new Process
            {
                StartInfo = startInfo
            };

            _logger.LogInformation("{File} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting ffmpeg");

                throw;
            }

            var processTcs = new TaskCompletionSource<bool>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => processTcs.TrySetResult(true);
            var unregister = cancellationToken.Register(() => processTcs.TrySetResult(process.HasExited));
            var ranToCompletion = await processTcs.Task.ConfigureAwait(false);
            unregister.Dispose();

            if (!ranToCompletion)
            {
                try
                {
                    _logger.LogWarning("Killing ffmpeg attachment extraction process");
                    process.Kill();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error killing attachment extraction process");
                }
            }
            var exitCode = ranToCompletion ? process.ExitCode : -1;

            process.Dispose();
            
            var failed = false;

            if (exitCode != 0)
            {
                failed = true;

                _logger.LogWarning("Deleting extracted attachment {Path} due to failure: {ExitCode}", outputPath, exitCode);
                try
                {
                    if (File.Exists(outputPath))
                    {
                        _fileSystem.DeleteFile(outputPath);
                    }
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "Error deleting extracted attachment {Path}", outputPath);
                }

            }
            else if (!File.Exists(outputPath))
            {
                failed = true;
            }

            if (failed)
            {
                var msg = $"ffmpeg attachment extraction failed for {inputPath} to {outputPath}";

                _logger.LogError(msg);

                throw new Exception(msg);
            }
            else
            {
                _logger.LogInformation("ffmpeg attachment extraction completed for {Path} to {Path}", inputPath, outputPath);
            }
        }

        private string GetAttachmentCachePath(string mediaPath, MediaProtocol protocol, int attachmentStreamIndex)
        {
            string filename;
            if (protocol == MediaProtocol.File)
            {
                var date = _fileSystem.GetLastWriteTimeUtc(mediaPath);
                filename = (mediaPath + attachmentStreamIndex.ToString(CultureInfo.InvariantCulture) + "_" + date.Ticks.ToString(CultureInfo.InvariantCulture)).GetMD5().ToString("D");
            }
            else
            {
                filename = (mediaPath + attachmentStreamIndex.ToString(CultureInfo.InvariantCulture)).GetMD5().ToString("D");
            }
            var prefix = filename.Substring(0, 1);
            return Path.Combine(AttachmentCachePath, prefix, filename);
        }

    }
}
