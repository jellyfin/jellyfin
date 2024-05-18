using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks;

/// <summary>
/// The audio normalization task.
/// </summary>
public partial class AudioNormalizationTask : IScheduledTask
{
    private readonly IItemRepository _itemRepository;
    private readonly ILibraryManager _libraryManager;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly IConfigurationManager _configurationManager;
    private readonly ILocalizationManager _localization;
    private readonly ILogger<AudioNormalizationTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioNormalizationTask"/> class.
    /// </summary>
    /// <param name="itemRepository">Instance of the <see cref="IItemRepository"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="configurationManager">Instance of the <see cref="IConfigurationManager"/> interface.</param>
    /// <param name="localizationManager">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{AudioNormalizationTask}"/> interface.</param>
    public AudioNormalizationTask(
        IItemRepository itemRepository,
        ILibraryManager libraryManager,
        IMediaEncoder mediaEncoder,
        IConfigurationManager configurationManager,
        ILocalizationManager localizationManager,
        ILogger<AudioNormalizationTask> logger)
    {
        _itemRepository = itemRepository;
        _libraryManager = libraryManager;
        _mediaEncoder = mediaEncoder;
        _configurationManager = configurationManager;
        _localization = localizationManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => _localization.GetLocalizedString("TaskAudioNormalization");

    /// <inheritdoc />
    public string Description => _localization.GetLocalizedString("TaskAudioNormalizationDescription");

    /// <inheritdoc />
    public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

    /// <inheritdoc />
    public string Key => "AudioNormalization";

    [GeneratedRegex(@"I:\s+(.*?)\s+LUFS")]
    private static partial Regex LUFSRegex();

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        foreach (var library in _libraryManager.RootFolder.Children)
        {
            var libraryOptions = _libraryManager.GetLibraryOptions(library);
            if (!libraryOptions.EnableLUFSScan)
            {
                continue;
            }

            // Album gain
            var albums = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.MusicAlbum],
                Parent = library,
                Recursive = true
            });

            foreach (var a in albums)
            {
                if (a.NormalizationGain.HasValue || a.LUFS.HasValue)
                {
                    continue;
                }

                // Skip albums that don't have multiple tracks, album gain is useless here
                var albumTracks = ((MusicAlbum)a).Tracks.Where(x => x.IsFileProtocol).ToList();
                if (albumTracks.Count <= 1)
                {
                    continue;
                }

                var tempFile = Path.Join(_configurationManager.GetTranscodePath(), Guid.NewGuid() + ".concat");
                var inputLines = albumTracks.Select(x => string.Format(CultureInfo.InvariantCulture, "file '{0}'", x.Path.Replace("'", @"'\''", StringComparison.Ordinal)));
                await File.WriteAllLinesAsync(tempFile, inputLines, cancellationToken).ConfigureAwait(false);
                a.LUFS = await CalculateLUFSAsync(
                    string.Format(CultureInfo.InvariantCulture, "-f concat -safe 0 -i \"{0}\"", tempFile),
                    cancellationToken).ConfigureAwait(false);
                File.Delete(tempFile);
            }

            _itemRepository.SaveItems(albums, cancellationToken);

            // Track gain
            var tracks = _libraryManager.GetItemList(new InternalItemsQuery
            {
                MediaTypes = [MediaType.Audio],
                IncludeItemTypes = [BaseItemKind.Audio],
                Parent = library,
                Recursive = true
            });

            foreach (var t in tracks)
            {
                if (t.NormalizationGain.HasValue || t.LUFS.HasValue || !t.IsFileProtocol)
                {
                    continue;
                }

                t.LUFS = await CalculateLUFSAsync(string.Format(CultureInfo.InvariantCulture, "-i \"{0}\"", t.Path.Replace("\"", "\\\"", StringComparison.Ordinal)), cancellationToken);
            }

            _itemRepository.SaveItems(tracks, cancellationToken);
        }
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return
        [
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerInterval,
                IntervalTicks = TimeSpan.FromHours(24).Ticks
            }
        ];
    }

    private async Task<float?> CalculateLUFSAsync(string inputArgs, CancellationToken cancellationToken)
    {
        var args = $"-hide_banner {inputArgs} -af ebur128=framelog=verbose -f null -";

        using (var process = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _mediaEncoder.EncoderPath,
                Arguments = args,
                RedirectStandardOutput = false,
                RedirectStandardError = true
            },
        })
        {
            try
            {
                _logger.LogDebug("Starting ffmpeg with arguments: {Arguments}", args);
                process.Start();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting ffmpeg with arguments: {Arguments}", args);
                return null;
            }

            using var reader = process.StandardError;
            var output = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            MatchCollection split = LUFSRegex().Matches(output);

            if (split.Count != 0)
            {
                return float.Parse(split[0].Groups[1].ValueSpan, CultureInfo.InvariantCulture.NumberFormat);
            }

            _logger.LogError("Failed to find LUFS value in output:\n{Output}", output);
            return null;
        }
    }
}
