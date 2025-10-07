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
using Jellyfin.Extensions;
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
    private readonly IApplicationPaths _applicationPaths;
    private readonly ILocalizationManager _localization;
    private readonly ILogger<AudioNormalizationTask> _logger;

    private static readonly TimeSpan _dbSaveInterval = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioNormalizationTask"/> class.
    /// </summary>
    /// <param name="itemRepository">Instance of the <see cref="IItemRepository"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="localizationManager">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{AudioNormalizationTask}"/> interface.</param>
    public AudioNormalizationTask(
        IItemRepository itemRepository,
        ILibraryManager libraryManager,
        IMediaEncoder mediaEncoder,
        IApplicationPaths applicationPaths,
        ILocalizationManager localizationManager,
        ILogger<AudioNormalizationTask> logger)
    {
        _itemRepository = itemRepository;
        _libraryManager = libraryManager;
        _mediaEncoder = mediaEncoder;
        _applicationPaths = applicationPaths;
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

    [GeneratedRegex(@"^\s+I:\s+(.*?)\s+LUFS")]
    private static partial Regex LUFSRegex();

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var numComplete = 0;
        var libraries = _libraryManager.RootFolder.Children.Where(library => _libraryManager.GetLibraryOptions(library).EnableLUFSScan).ToArray();
        double percent = 0;

        foreach (var library in libraries)
        {
            var startDbSaveInterval = Stopwatch.GetTimestamp();
            var albums = _libraryManager.GetItemList(new InternalItemsQuery { IncludeItemTypes = [BaseItemKind.MusicAlbum], Parent = library, Recursive = true });
            var toSaveDbItems = new List<BaseItem>();

            double nextPercent = numComplete + 1;
            nextPercent /= libraries.Length;
            nextPercent -= percent;
            // Split the progress for this single library into two halves: album gain and track gain.
            // The first half will be for album gain, the second half for track gain.
            nextPercent /= 2;
            var albumComplete = 0;

            foreach (var a in albums)
            {
                if (!a.NormalizationGain.HasValue && !a.LUFS.HasValue)
                {
                    // Album gain
                    var albumTracks = ((MusicAlbum)a).Tracks.Where(x => x.IsFileProtocol).ToList();

                    // Skip albums that don't have multiple tracks, album gain is useless here
                    if (albumTracks.Count > 1)
                    {
                        _logger.LogInformation("Calculating LUFS for album: {Album} with id: {Id}", a.Name, a.Id);
                        var tempDir = _applicationPaths.TempDirectory;
                        Directory.CreateDirectory(tempDir);
                        var tempFile = Path.Join(tempDir, a.Id + ".concat");
                        var inputLines = albumTracks.Select(x => string.Format(CultureInfo.InvariantCulture, "file '{0}'", x.Path.Replace("'", @"'\''", StringComparison.Ordinal)));
                        await File.WriteAllLinesAsync(tempFile, inputLines, cancellationToken).ConfigureAwait(false);
                        try
                        {
                            a.LUFS = await CalculateLUFSAsync(
                                string.Format(CultureInfo.InvariantCulture, "-f concat -safe 0 -i \"{0}\"", tempFile),
                                OperatingSystem.IsWindows(), // Wait for process to exit on Windows before we try deleting the concat file
                                cancellationToken).ConfigureAwait(false);
                            toSaveDbItems.Add(a);
                        }
                        finally
                        {
                            try
                            {
                                File.Delete(tempFile);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to delete concat file: {FileName}.", tempFile);
                            }
                        }
                    }
                }

                if (Stopwatch.GetElapsedTime(startDbSaveInterval) > _dbSaveInterval)
                {
                    if (toSaveDbItems.Count > 1)
                    {
                        _itemRepository.SaveItems(toSaveDbItems, cancellationToken);
                        toSaveDbItems.Clear();
                    }

                    startDbSaveInterval = Stopwatch.GetTimestamp();
                }

                // Update sub-progress for album gain
                albumComplete++;
                double albumPercent = albumComplete;
                albumPercent /= albums.Count;

                progress.Report(100 * (percent + (albumPercent * nextPercent)));
            }

            // Update progress to start at the track gain percent calculation
            percent += nextPercent;

            if (toSaveDbItems.Count > 1)
            {
                _itemRepository.SaveItems(toSaveDbItems, cancellationToken);
                toSaveDbItems.Clear();
            }

            startDbSaveInterval = Stopwatch.GetTimestamp();

            // Track gain
            var tracks = _libraryManager.GetItemList(new InternalItemsQuery { MediaTypes = [MediaType.Audio], IncludeItemTypes = [BaseItemKind.Audio], Parent = library, Recursive = true });

            var tracksComplete = 0;
            foreach (var t in tracks)
            {
                if (!t.NormalizationGain.HasValue && !t.LUFS.HasValue && t.IsFileProtocol)
                {
                    t.LUFS = await CalculateLUFSAsync(
                        string.Format(CultureInfo.InvariantCulture, "-i \"{0}\"", t.Path.Replace("\"", "\\\"", StringComparison.Ordinal)),
                        false,
                        cancellationToken).ConfigureAwait(false);
                    toSaveDbItems.Add(t);
                }

                if (Stopwatch.GetElapsedTime(startDbSaveInterval) > _dbSaveInterval)
                {
                    if (toSaveDbItems.Count > 1)
                    {
                        _itemRepository.SaveItems(toSaveDbItems, cancellationToken);
                        toSaveDbItems.Clear();
                    }

                    startDbSaveInterval = Stopwatch.GetTimestamp();
                }

                // Update sub-progress for track gain
                tracksComplete++;
                double trackPercent = tracksComplete;
                trackPercent /= tracks.Count;

                progress.Report(100 * (percent + (trackPercent * nextPercent)));
            }

            if (toSaveDbItems.Count > 1)
            {
                _itemRepository.SaveItems(toSaveDbItems, cancellationToken);
            }

            // Update progress
            numComplete++;
            percent = numComplete;
            percent /= libraries.Length;

            progress.Report(100 * percent);
        }

        progress.Report(100.0);
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.IntervalTrigger,
            IntervalTicks = TimeSpan.FromHours(24).Ticks
        };
    }

    private async Task<float?> CalculateLUFSAsync(string inputArgs, bool waitForExit, CancellationToken cancellationToken)
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
            _logger.LogDebug("Starting ffmpeg with arguments: {Arguments}", args);
            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting ffmpeg with arguments: {Arguments}", args);
                return null;
            }

            try
            {
                process.PriorityClass = ProcessPriorityClass.BelowNormal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error setting ffmpeg process priority");
            }

            using var reader = process.StandardError;
            float? lufs = null;
            var foundLufs = false;
            await foreach (var line in reader.ReadAllLinesAsync(cancellationToken).ConfigureAwait(false))
            {
                if (foundLufs)
                {
                    continue;
                }

                Match match = LUFSRegex().Match(line);
                if (!match.Success)
                {
                    continue;
                }

                lufs = float.Parse(match.Groups[1].ValueSpan, CultureInfo.InvariantCulture.NumberFormat);
                foundLufs = true;
            }

            if (lufs is null)
            {
                _logger.LogError("Failed to find LUFS value in output");
            }

            if (waitForExit)
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }

            return lufs;
        }
    }
}
