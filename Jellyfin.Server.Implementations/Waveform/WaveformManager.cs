using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions.Json;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Waveform;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Waveform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Waveform;

/// <summary>
/// IWaveformManager implementation.
/// </summary>
public class WaveformManager : IWaveformManager
{
    private const int SamplesPerSecond = 2;
    private static readonly TimeSpan _processTimeout = TimeSpan.FromMinutes(5);

    private readonly ILogger<WaveformManager> _logger;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly ILibraryManager _libraryManager;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IPathManager _pathManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="WaveformManager"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="mediaEncoder">The media encoder.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="dbProvider">The database provider.</param>
    /// <param name="pathManager">The path manager.</param>
    public WaveformManager(
        ILogger<WaveformManager> logger,
        IMediaEncoder mediaEncoder,
        ILibraryManager libraryManager,
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IPathManager pathManager)
    {
        _logger = logger;
        _mediaEncoder = mediaEncoder;
        _libraryManager = libraryManager;
        _dbProvider = dbProvider;
        _pathManager = pathManager;
    }

    /// <inheritdoc />
    public async Task<WaveformDto?> GetWaveformAsync(Guid itemId, CancellationToken cancellationToken)
    {
        var item = _libraryManager.GetItemById<BaseItem>(itemId);
        if (item is null)
        {
            return null;
        }

        var cachePath = _pathManager.GetWaveformPath(itemId);

        // Check for cached waveform data
        if (File.Exists(cachePath))
        {
            try
            {
                var fileStream = AsyncFile.OpenRead(cachePath);
                await using (fileStream.ConfigureAwait(false))
                {
                    var cached = await JsonSerializer.DeserializeAsync<WaveformDto>(fileStream, JsonDefaults.Options, cancellationToken).ConfigureAwait(false);
                    if (cached is not null)
                    {
                        return cached;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read cached waveform data for item {ItemId}, regenerating", itemId);
            }
        }

        // Generate waveform data
        var dto = await GenerateWaveformAsync(item, cancellationToken).ConfigureAwait(false);
        if (dto is null)
        {
            return null;
        }

        // Cache to disk
        try
        {
            var directory = Path.GetDirectoryName(cachePath);
            if (directory is not null)
            {
                Directory.CreateDirectory(directory);
            }

            var fileStream = File.Create(cachePath);
            await using (fileStream.ConfigureAwait(false))
            {
                await JsonSerializer.SerializeAsync(fileStream, dto, JsonDefaults.Options, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cache waveform data for item {ItemId}", itemId);
        }

        // Save metadata to DB
        try
        {
            var info = new WaveformInfo
            {
                ItemId = itemId,
                SamplesPerSecond = SamplesPerSecond
            };

            await SaveWaveformInfoAsync(info, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save waveform info for item {ItemId}", itemId);
        }

        return dto;
    }

    /// <inheritdoc />
    public async Task SaveWaveformInfoAsync(WaveformInfo info, CancellationToken cancellationToken)
    {
        var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var oldInfo = await dbContext.WaveformInfos.FindAsync([info.ItemId], cancellationToken).ConfigureAwait(false);
            if (oldInfo is not null)
            {
                dbContext.WaveformInfos.Remove(oldInfo);
            }

            dbContext.Add(info);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task DeleteWaveformDataAsync(Guid itemId, CancellationToken cancellationToken)
    {
        var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            await dbContext.WaveformInfos
                .Where(i => i.ItemId.Equals(itemId))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        var cachePath = _pathManager.GetWaveformPath(itemId);
        if (File.Exists(cachePath))
        {
            try
            {
                File.Delete(cachePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete cached waveform file for item {ItemId}", itemId);
            }
        }
    }

    private async Task<WaveformDto?> GenerateWaveformAsync(BaseItem item, CancellationToken cancellationToken)
    {
        var mediaStreams = item.GetMediaStreams();
        var audioStream = mediaStreams.FirstOrDefault(s => s.Type == MediaStreamType.Audio);

        var fileSampleRate = audioStream?.SampleRate ?? 44100;
        var sampleRate = Math.Max(fileSampleRate / SamplesPerSecond, 1);

        var escapedPath = EscapeLavfiPath(item.Path);
        var args = string.Format(
            CultureInfo.InvariantCulture,
            "-v error -f lavfi -i \"amovie={0},asetnsamples={1},astats=metadata=1:reset=1\" -show_entries frame_tags=lavfi.astats.Overall.RMS_peak -of csv=p=0",
            escapedPath,
            sampleRate);

        _logger.LogDebug("Starting ffprobe for waveform generation with arguments: {Arguments}", args);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _mediaEncoder.ProbePath,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting ffprobe for waveform generation");
            return null;
        }

        try
        {
            process.PriorityClass = ProcessPriorityClass.BelowNormal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error setting ffprobe process priority");
        }

        var samples = new List<double>();

        try
        {
            using var reader = process.StandardOutput;
            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (string.Equals(line.Trim(), "-inf", StringComparison.OrdinalIgnoreCase))
                {
                    samples.Add(0.0);
                }
                else if (double.TryParse(line.Trim(), CultureInfo.InvariantCulture, out var dbValue))
                {
                    samples.Add(Math.Pow(2, dbValue / 6));
                }
                else
                {
                    samples.Add(0.0);
                }
            }

            await process.WaitForExitAsync(cancellationToken).WaitAsync(_processTimeout, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            _logger.LogError("Ffprobe waveform process timed out for item {ItemId}, killing process", item.Id);
            try
            {
                process.Kill(true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error killing timed out ffprobe process");
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error killing cancelled ffprobe process");
            }

            throw;
        }

        if (process.ExitCode != 0)
        {
            _logger.LogError("Ffprobe exited with code {ExitCode} for item {ItemId}", process.ExitCode, item.Id);
            return null;
        }

        if (samples.Count == 0)
        {
            _logger.LogWarning("No waveform samples generated for item {ItemId}", item.Id);
            return null;
        }

        return new WaveformDto
        {
            SampleDuration = 1.0 / SamplesPerSecond,
            Samples = samples
        };
    }

    private static string EscapeLavfiPath(string path)
    {
        // Escape characters that have special meaning in the lavfi amovie filter
        return path
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(":", "\\:", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal)
            .Replace("]", "\\]", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal);
    }
}
