using System;
using System.Collections.Generic;
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
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to move extracted files to the new directories.
/// </summary>
[JellyfinMigration("2025-04-21T00:00:00", nameof(MigrateKeyframeData), "EA4bCAE1-09A4-428E-9B90-4B4FD2EA1B24")]
public class MigrateKeyframeData : IDatabaseMigrationRoutine
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<MoveTrickplayFiles> _logger;
    private readonly IApplicationPaths _appPaths;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private static readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrateKeyframeData"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="appPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="dbProvider">The EFCore db factory.</param>
    public MigrateKeyframeData(
        ILibraryManager libraryManager,
        ILogger<MoveTrickplayFiles> logger,
        IApplicationPaths appPaths,
        IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _libraryManager = libraryManager;
        _logger = logger;
        _appPaths = appPaths;
        _dbProvider = dbProvider;
    }

    private string KeyframeCachePath => Path.Combine(_appPaths.DataPath, "keyframes");

    /// <inheritdoc />
    public void Perform()
    {
        const int Limit = 100;
        int itemCount = 0, offset = 0, previousCount;

        var sw = Stopwatch.StartNew();
        var itemsQuery = new InternalItemsQuery
        {
            MediaTypes = [MediaType.Video],
            SourceTypes = [SourceType.Library],
            IsVirtualItem = false,
            IsFolder = false
        };

        using var context = _dbProvider.CreateDbContext();
        context.KeyframeData.ExecuteDelete();
        using var transaction = context.Database.BeginTransaction();
        List<KeyframeData> keyframes = [];

        do
        {
            var result = _libraryManager.GetItemsResult(itemsQuery);
            _logger.LogInformation("Importing keyframes for {Count} items", result.TotalRecordCount);

            var items = result.Items;
            previousCount = items.Count;
            offset += Limit;
            foreach (var item in items)
            {
                if (TryGetKeyframeData(item, out var data))
                {
                    keyframes.Add(data);
                }

                if (++itemCount % 10_000 == 0)
                {
                    context.KeyframeData.AddRange(keyframes);
                    keyframes.Clear();
                    _logger.LogInformation("Imported keyframes for {Count} items in {Time}", itemCount, sw.Elapsed);
                }
            }
        } while (previousCount == Limit);

        context.KeyframeData.AddRange(keyframes);
        context.SaveChanges();
        transaction.Commit();

        _logger.LogInformation("Imported keyframes for {Count} items in {Time}", itemCount, sw.Elapsed);

        if (Directory.Exists(KeyframeCachePath))
        {
            Directory.Delete(KeyframeCachePath, true);
        }
    }

    private bool TryGetKeyframeData(BaseItem item, [NotNullWhen(true)] out KeyframeData? data)
    {
        data = null;
        var path = item.Path;
        if (!string.IsNullOrEmpty(path))
        {
            var cachePath = GetCachePath(KeyframeCachePath, path);
            if (TryReadFromCache(cachePath, out var keyframeData))
            {
                data = new()
                {
                    ItemId = item.Id,
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
