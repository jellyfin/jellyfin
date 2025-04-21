using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to move extracted files to the new directories.
/// </summary>
public class MigrateKeyframeData : IDatabaseMigrationRoutine
{
    private readonly ILogger<MigrateKeyframeData> _logger;
    private readonly IApplicationPaths _appPaths;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private static readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrateKeyframeData"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="appPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="dbProvider">The EFCore db factory.</param>
    public MigrateKeyframeData(
        ILogger<MigrateKeyframeData> logger,
        IApplicationPaths appPaths,
        IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _logger = logger;
        _appPaths = appPaths;
        _dbProvider = dbProvider;
    }

    private string KeyframeCachePath => Path.Combine(_appPaths.DataPath, "keyframes");

    /// <inheritdoc />
    public Guid Id => new("EA4bCAE1-09A4-428E-9B90-4B4FD2EA1B24");

    /// <inheritdoc />
    public string Name => "MigrateKeyframeData";

    /// <inheritdoc />
    public bool PerformOnNewInstall => false;

    /// <inheritdoc />
    public void Perform()
    {
        const int Limit = 5000;
        int itemCount = 0, offset = 0;

        var sw = Stopwatch.StartNew();

        using var context = _dbProvider.CreateDbContext();
        var baseQuery = context.BaseItems.Where(b => b.MediaType == MediaType.Video.ToString() && !b.IsVirtualItem && !b.IsFolder).OrderBy(e => e.Id);
        var records = baseQuery.Count();
        _logger.LogInformation("Checking {Count} items for importable keyframe data.", records);

        context.KeyframeData.ExecuteDelete();
        using var transaction = context.Database.BeginTransaction();
        do
        {
            var results = baseQuery.Skip(offset).Take(Limit).Select(b => new Tuple<Guid, string?>(b.Id, b.Path)).ToList();
            foreach (var result in results)
            {
                if (TryGetKeyframeData(result.Item1, result.Item2, out var data))
                {
                    itemCount++;
                    context.KeyframeData.Add(data);
                }
            }

            offset += Limit;
            _logger.LogInformation("Checked: {Count} - Imported: {Items} - Time: {Time}", offset, itemCount, sw.Elapsed);
        } while (offset < records);

        context.SaveChanges();
        transaction.Commit();

        _logger.LogInformation("Imported keyframes for {Count} items in {Time}", itemCount, sw.Elapsed);

        if (Directory.Exists(KeyframeCachePath))
        {
            Directory.Delete(KeyframeCachePath, true);
        }
    }

    private bool TryGetKeyframeData(Guid id, string? path, [NotNullWhen(true)] out KeyframeData? data)
    {
        data = null;
        if (!string.IsNullOrEmpty(path))
        {
            var cachePath = GetCachePath(KeyframeCachePath, path);
            if (TryReadFromCache(cachePath, out var keyframeData))
            {
                data = new()
                {
                    ItemId = id,
                    KeyframeTicks = keyframeData.KeyframeTicks.ToList(),
                    TotalDuration = keyframeData.TotalDuration
                };

                return true;
            }
        }

        return false;
    }

    private string? GetCachePath(string keyframeCachePath, string filePath)
    {
        DateTime? lastWriteTimeUtc;
        try
        {
            lastWriteTimeUtc = File.GetLastWriteTimeUtc(filePath);
        }
        catch (IOException e)
        {
            _logger.LogDebug("Skipping {Path}: {Exception}", filePath, e.Message);

            return null;
        }

        ReadOnlySpan<char> filename = (filePath + "_" + lastWriteTimeUtc.Value.Ticks.ToString(CultureInfo.InvariantCulture)).GetMD5() + ".json";
        var prefix = filename[..1];

        return Path.Join(keyframeCachePath, prefix, filename);
    }

    private static bool TryReadFromCache(string? cachePath, [NotNullWhen(true)] out MediaEncoding.Keyframes.KeyframeData? cachedResult)
    {
        if (File.Exists(cachePath))
        {
            var bytes = File.ReadAllBytes(cachePath);
            cachedResult = JsonSerializer.Deserialize<MediaEncoding.Keyframes.KeyframeData>(bytes, _jsonOptions);

            return cachedResult is not null;
        }

        cachedResult = null;

        return false;
    }
}
