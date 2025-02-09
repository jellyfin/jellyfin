using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.AudioWaveform;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.AudioWaveform;

/// <summary>
/// IAudioWaveformManager implementation.
/// </summary>
public class AudioWaveformManager : IAudioWaveformManager
{
    private readonly ILogger<AudioWaveformManager> _logger;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly IFileSystem _fileSystem;
    private readonly EncodingHelper _encodingHelper;
    private readonly ILibraryManager _libraryManager;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IApplicationPaths _appPaths;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioWaveformManager"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="mediaEncoder">The media encoder.</param>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="encodingHelper">The encoding helper.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="config">The server configuration manager.</param>
    /// <param name="imageEncoder">The image encoder.</param>
    /// <param name="dbProvider">The database provider.</param>
    /// <param name="appPaths">The application paths.</param>
    public AudioWaveformManager(
        ILogger<AudioWaveformManager> logger,
        IMediaEncoder mediaEncoder,
        IFileSystem fileSystem,
        EncodingHelper encodingHelper,
        ILibraryManager libraryManager,
        IServerConfigurationManager config,
        IImageEncoder imageEncoder,
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IApplicationPaths appPaths)
    {
        _logger = logger;
        _mediaEncoder = mediaEncoder;
        _fileSystem = fileSystem;
        _encodingHelper = encodingHelper;
        _libraryManager = libraryManager;
        _dbProvider = dbProvider;
        _appPaths = appPaths;
    }

    /// <inheritdoc />
    public async Task<FileStream> GetAudioWaveformAnsyc(Guid itemId)
    {
        var item = _libraryManager.GetItemById<BaseItem>(itemId)
            ?? throw new ResourceNotFoundException();

        var saveInMediaFolder = BaseItem.LibraryManager.GetLibraryOptions(item).SaveLocalMetadata;
        var saveFileName = Path.ChangeExtension(item.Path, "json");
        string outputPathJson = saveInMediaFolder
            ? Path.Combine(item.ContainingFolderPath, saveFileName)
            : Path.Combine(item.GetInternalMetadataPath(), saveFileName);

        if (!File.Exists(outputPathJson))
        {
            var tempDir = Path.Combine(_appPaths.TempDirectory, "audiowaveform_" + itemId);
            Directory.CreateDirectory(tempDir);
            string outputPathCsv = Path.Combine(tempDir, Path.ChangeExtension(saveFileName, "csv"));
            int sampleRate = 0;
            var mediaStreams = item.GetMediaStreams();
            int fileSampleRate = (mediaStreams.Count > 0 && mediaStreams[0].SampleRate.HasValue) ? mediaStreams[0].SampleRate.Value : 44100;
            // This value sets the length of a frame, the amount of measurements per second.
            int samplesPerSecond = 2;
            sampleRate = Math.Max(fileSampleRate / samplesPerSecond, 1);

            // escape double quotes
            string itemPathEscaped = item.Path.Replace("\"", "\\\"", StringComparison.Ordinal);
            string outputPathCsvEscaped = outputPathCsv.Replace("\"", "\\\"", StringComparison.Ordinal);

            var process = Process.Start("ffprobe", $"-v error -f lavfi -i \"amovie={itemPathEscaped},asetnsamples={sampleRate},astats=metadata=1:reset=1\" -show_entries frame_tags=lavfi.astats.Overall.RMS_peak -of csv=p=0 -o \"{outputPathCsvEscaped}\"");

            int timeout = 10; // seconds, this is quite an arbitrary value
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(timeout)).ConfigureAwait(false);

            var csvLines = await File.ReadAllLinesAsync(outputPathCsv).ConfigureAwait(false);
            var samples = csvLines
                    .Select(v =>
                {
                    if (string.Equals(v.Trim(), "-inf", StringComparison.Ordinal))
                    {
                        return 0.0;  // sometimes ffprobe returns "-inf" instead of a number
                    }
                    else
                    {
                        return double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedValue)
                            ? Math.Pow(2, parsedValue / 6)
                            : 0.0;
                    }
                })
                .ToArray();

            var data = new Dictionary<string, object>
            {
                { "samples", samples },
                { "sampleRate", sampleRate }
            };

            var json = JsonSerializer.Serialize(data);
            await File.WriteAllTextAsync(outputPathJson, json).ConfigureAwait(false);
            File.Delete(outputPathCsv);
        }

        var audioWaveformInfo = new AudioWaveformInfo
        {
            ItemId = itemId,
            SamplesPerSecond = 0
        };

        await SaveAudioWaveformInfo(audioWaveformInfo).ConfigureAwait(false);

        var fileStream = new FileStream(outputPathJson, FileMode.Open, FileAccess.Read, FileShare.Read);
        return fileStream;
    }

    /// <inheritdoc />
    public async Task SaveAudioWaveformInfo(AudioWaveformInfo info)
    {
        var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var oldInfo = await dbContext.AudioWaveformInfos.FindAsync(info.ItemId).ConfigureAwait(false);
            if (oldInfo is not null)
            {
                dbContext.AudioWaveformInfos.Remove(oldInfo);
            }

            dbContext.Add(info);

            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
