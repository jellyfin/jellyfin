using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.AudioBook;
using Emby.Naming.Common;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.MediaInfo
{
    /// <summary>
    /// Provider to calculate total duration for multi-file audiobooks.
    /// This runs after ProbeProvider to sum durations of all parts.
    /// </summary>
    public class AudioBookDurationProvider :
        ICustomMetadataProvider<AudioBook>,
        IHasOrder
    {
        private readonly ILogger<AudioBookDurationProvider> _logger;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IFileSystem _fileSystem;
        private readonly NamingOptions _namingOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioBookDurationProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="mediaEncoder">The media encoder.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="namingOptions">The naming options.</param>
        public AudioBookDurationProvider(
            ILogger<AudioBookDurationProvider> logger,
            IMediaEncoder mediaEncoder,
            IFileSystem fileSystem,
            NamingOptions namingOptions)
        {
            _logger = logger;
            _mediaEncoder = mediaEncoder;
            _fileSystem = fileSystem;
            _namingOptions = namingOptions;
        }

        /// <inheritdoc />
        public string Name => "AudioBook Duration Calculator";

        /// <summary>
        /// Gets the order. Should run after ProbeProvider (which has Order 100).
        /// </summary>
        public int Order => 200;

        /// <inheritdoc />
        public Task<ItemUpdateType> FetchAsync(AudioBook item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            // Always process audiobooks - we need to calculate total duration
            return CalculateTotalDurationAsync(item, cancellationToken);
        }

        private async Task<ItemUpdateType> CalculateTotalDurationAsync(AudioBook item, CancellationToken cancellationToken)
        {
            // Get the directory containing the audiobook
            if (string.IsNullOrEmpty(item.Path))
            {
                return ItemUpdateType.None;
            }

            var directory = Path.GetDirectoryName(item.Path);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                return ItemUpdateType.None;
            }

            // Get all audio files in the directory
            var files = _fileSystem.GetFiles(directory, true)
                .Where(f => _namingOptions.AudioFileExtensions.Contains(Path.GetExtension(f.FullName), StringComparer.OrdinalIgnoreCase))
                .OrderBy(f => f.FullName)
                .ToList();

            _logger.LogInformation(
                "Calculating total duration for audiobook: {Name} with {Count} audio files in directory",
                item.Name,
                files.Count);

            if (files.Count <= 1)
            {
                // Single file audiobook, ProbeProvider already handled it
                return ItemUpdateType.None;
            }

            // Probe each file and sum durations
            long totalTicks = 0;
            int successCount = 0;

            foreach (var file in files)
            {
                try
                {
                    var partResult = await _mediaEncoder.GetMediaInfo(
                        new MediaInfoRequest
                        {
                            MediaType = DlnaProfileType.Audio,
                            MediaSource = new MediaSourceInfo
                            {
                                Path = file.FullName,
                                Protocol = MediaProtocol.File
                            }
                        },
                        cancellationToken).ConfigureAwait(false);

                    if (partResult.RunTimeTicks.HasValue)
                    {
                        totalTicks += partResult.RunTimeTicks.Value;
                        successCount++;
                        _logger.LogDebug(
                            "Added {Minutes:F2} minutes from file {Index}/{Total}: {Path}",
                            TimeSpan.FromTicks(partResult.RunTimeTicks.Value).TotalMinutes,
                            successCount,
                            files.Count,
                            file.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to probe duration for file: {Path}",
                        file.FullName);
                }
            }

            if (successCount > 0)
            {
                // Update the item's runtime
                item.RunTimeTicks = totalTicks;

                var totalHours = TimeSpan.FromTicks(totalTicks).TotalHours;
                _logger.LogInformation(
                    "Total runtime calculated for {Name}: {Hours:F2} hours ({SuccessCount}/{TotalCount} files)",
                    item.Name,
                    totalHours,
                    successCount,
                    files.Count);

                return ItemUpdateType.MetadataImport;
            }

            return ItemUpdateType.None;
        }
    }
}
