using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Scanning
{
    internal sealed class Phase1TreeScanner
    {
        private const int FolderWorkerConcurrency = 4;
        private const int ResolveParallelismPerFolder = 4;
        private const double MtimeToleranceSeconds = 1.0;
        private const int SlowFolderLogThresholdMs = 500;

        private static readonly string[] _standaloneImageNames =
        [
            "poster",
            "folder",
            "cover",
            "default",
            "movie",
            "show",
            "jacket",
            "backdrop",
            "fanart",
            "background",
            "art",
            "logo",
            "clearlogo",
            "banner",
            "thumb",
            "landscape",
            "clearart",
            "disc",
            "cdart",
            "discart"
        ];

        private static readonly string[] _imageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp"];

        private static readonly MethodInfo? _updateFromResolvedItemMethod = typeof(BaseItem).GetMethod("UpdateFromResolvedItem", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<Phase1TreeScanner> _logger;

        public Phase1TreeScanner(
            ILibraryManager libraryManager,
            ILogger<Phase1TreeScanner> logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
        }

        public async Task<Phase1TreeScanResult> ScanAsync(
            Folder folder,
            CollectionType collectionType,
            IDirectoryService directoryService,
            CancellationToken cancellationToken)
        {
            var result = new ConcurrentScanResult();
            var stats = new ScanStats();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            await ScanTreeAsync(folder, collectionType, directoryService, result, stats, cancellationToken).ConfigureAwait(false);

            var finalResult = result.ToPhase1Result();
            NormalizeTvRelationshipsFast(finalResult, collectionType);

            _logger.LogInformation(
                "PHASE1_SCAN: {Path} reused={Reused} resolved={Resolved} new={New} updated={Updated} removed={Removed} folders={FoldersProcessed} in {ElapsedMs}ms",
                folder.Path,
                stats.Reused,
                stats.Resolved,
                finalResult.NewItems.Count,
                finalResult.UpdatedItems.Count,
                finalResult.RemovedItems.Count,
                stats.FoldersProcessed,
                sw.ElapsedMilliseconds);

            return finalResult;
        }

        private async Task ScanTreeAsync(
            Folder rootFolder,
            CollectionType collectionType,
            IDirectoryService directoryService,
            ConcurrentScanResult result,
            ScanStats stats,
            CancellationToken cancellationToken)
        {
            var queue = new ConcurrentQueue<FolderWorkItem>();
            queue.Enqueue(new FolderWorkItem(rootFolder, IsKnownNewSubtree: false));

            using var pending = new SemaphoreSlim(0, int.MaxValue);
            pending.Release();

            using var concurrency = new SemaphoreSlim(FolderWorkerConcurrency, FolderWorkerConcurrency);
            var inFlight = 0;
            var inFlightLock = new object();

            void AddWork(FolderWorkItem work)
            {
                queue.Enqueue(work);
                pending.Release();
            }

            void IncrementInFlight()
            {
                lock (inFlightLock)
                {
                    inFlight++;
                }
            }

            bool DecrementAndCheckDone()
            {
                lock (inFlightLock)
                {
                    inFlight--;
                    return inFlight == 0 && queue.IsEmpty;
                }
            }

            var tasks = new List<Task>(FolderWorkerConcurrency);
            var done = false;

            async Task WorkerLoop()
            {
                while (!done)
                {
                    await pending.WaitAsync(cancellationToken).ConfigureAwait(false);
                    if (done)
                    {
                        pending.Release();
                        return;
                    }

                    if (!queue.TryDequeue(out var work))
                    {
                        continue;
                    }

                    IncrementInFlight();

                    try
                    {
                        await ProcessFolderAsync(work, collectionType, directoryService, result, stats, AddWork, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error scanning folder {Path}", work.Folder.Path);
                    }

                    if (DecrementAndCheckDone())
                    {
                        done = true;
                        for (var i = 0; i < FolderWorkerConcurrency; i++)
                        {
                            pending.Release();
                        }
                    }
                }
            }

            for (var w = 0; w < FolderWorkerConcurrency; w++)
            {
                tasks.Add(Task.Run(WorkerLoop, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task ProcessFolderAsync(
            FolderWorkItem work,
            CollectionType collectionType,
            IDirectoryService directoryService,
            ConcurrentScanResult result,
            ScanStats stats,
            Action<FolderWorkItem> addWork,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var folder = work.Folder;
            if (string.IsNullOrEmpty(folder.Path))
            {
                return;
            }

            stats.IncFolders();
            var folderSw = System.Diagnostics.Stopwatch.StartNew();

            IReadOnlyList<BaseItem> currentChildren;
            Dictionary<string, BaseItem> currentByPath;

            if (work.IsKnownNewSubtree)
            {
                currentChildren = Array.Empty<BaseItem>();
                currentByPath = new Dictionary<string, BaseItem>(0, StringComparer.Ordinal);
            }
            else
            {
                currentChildren = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    ParentId = folder.Id,
                    Recursive = false,
                    DtoOptions = new DtoOptions(false) { EnableImages = false }
                });

                currentByPath = BuildPathMap(currentChildren);
            }

            var libraryOptions = _libraryManager.GetLibraryOptions(folder);
            var fileSystemChildren = directoryService.GetFileSystemEntries(folder.Path);

            var reusable = new List<(FileSystemMetadata Fs, BaseItem Existing)>();
            var toResolve = new List<FileSystemMetadata>();

            foreach (var fs in fileSystemChildren)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var normalizedPath = NormalizePath(fs.FullName);
                if (currentByPath.TryGetValue(normalizedPath, out var existing)
                    && IsUnchanged(existing, fs))
                {
                    reusable.Add((fs, existing));
                }
                else
                {
                    toResolve.Add(fs);
                }
            }

            var consumedExistingIds = new HashSet<Guid>();

            foreach (var (fs, existing) in reusable)
            {
                if (SyncFilesystemFields(existing, fs))
                {
                    result.UpdatedItems.Add(existing);
                }

                result.AllItems.Add(existing);
                consumedExistingIds.Add(existing.Id);
                _libraryManager.RegisterItem(existing);
                stats.IncReused();

                if (existing is Folder existingFolder)
                {
                    addWork(new FolderWorkItem(existingFolder, IsKnownNewSubtree: false));
                }
            }

            var sidecarIndex = BuildSidecarIndex(fileSystemChildren);

            if (toResolve.Count > 0)
            {
                var resolvedList = _libraryManager.ResolvePaths(
                    toResolve,
                    directoryService,
                    folder,
                    libraryOptions,
                    collectionType,
                    maxParallelism: ResolveParallelismPerFolder).ToList();

                var currentById = work.IsKnownNewSubtree
                    ? new Dictionary<Guid, BaseItem>(0)
                    : currentChildren.ToDictionary(c => c.Id);

                foreach (var child in resolvedList)
                {
                    if (child is null)
                    {
                        continue;
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    stats.IncResolved();

                    if (currentById.TryGetValue(child.Id, out var existing))
                    {
                        var updateType = ItemUpdateType.None;
                        if (_updateFromResolvedItemMethod is not null)
                        {
                            updateType = (ItemUpdateType?)_updateFromResolvedItemMethod.Invoke(existing, new object[] { child }) ?? ItemUpdateType.None;
                        }

                        var matchingFs = LookupFsEntry(child.Path, directoryService);
                        if (matchingFs is not null && SyncFilesystemFields(existing, matchingFs))
                        {
                            updateType |= ItemUpdateType.MetadataImport;
                        }

                        if (AttachFileBasedSidecars(existing, sidecarIndex))
                        {
                            updateType |= ItemUpdateType.MetadataImport;
                        }

                        if (existing is Episode existingEpisode
                            && (!existingEpisode.IndexNumber.HasValue || !existingEpisode.ParentIndexNumber.HasValue)
                            && _libraryManager.FillMissingEpisodeNumbersFromPath(existingEpisode, false))
                        {
                            updateType |= ItemUpdateType.MetadataImport;
                        }

                        if (updateType > ItemUpdateType.None)
                        {
                            result.UpdatedItems.Add(existing);
                        }

                        result.AllItems.Add(existing);
                        consumedExistingIds.Add(existing.Id);
                        _libraryManager.RegisterItem(existing);

                        if (existing is Folder existingFolder)
                        {
                            addWork(new FolderWorkItem(existingFolder, IsKnownNewSubtree: false));
                        }
                    }
                    else
                    {
                        AttachFileBasedSidecars(child, sidecarIndex);
                        if (child is Episode episode)
                        {
                            _libraryManager.FillMissingEpisodeNumbersFromPath(episode, false);
                        }

                        try
                        {
                            await _libraryManager.ApplyLocalNfoFieldsAsync(child, directoryService, cancellationToken).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Error reading NFO during Phase 1 for {Path}", child.Path ?? child.Name);
                        }

                        result.NewItems.Add(child);
                        result.AllItems.Add(child);
                        _libraryManager.RegisterItem(child);

                        if (child is Folder childFolder)
                        {
                            addWork(new FolderWorkItem(childFolder, IsKnownNewSubtree: true));
                        }
                    }
                }
            }

            AttachFolderOwnSidecars(folder, fileSystemChildren, result);

            AttachStandaloneImagesToChildItems(folder.Path, fileSystemChildren, result);
            AttachStandaloneImagesFromItemParentDirs(folder.Path, directoryService, result);

            if (folderSw.ElapsedMilliseconds > SlowFolderLogThresholdMs)
            {
                _logger.LogInformation(
                    "SLOW_FOLDER: {Path} took {Ms}ms fsEntries={N} toResolve={R}",
                    folder.Path,
                    folderSw.ElapsedMilliseconds,
                    fileSystemChildren.Length,
                    toResolve.Count);
            }

            if (!work.IsKnownNewSubtree)
            {
                foreach (var existing in currentChildren.Where(e => !consumedExistingIds.Contains(e.Id)))
                {
                    result.RemovedItems.Add(existing);
                }
            }
        }

        private static FileSystemMetadata? LookupFsEntry(string? path, IDirectoryService directoryService)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return directoryService.GetFileSystemEntry(path);
        }

        private static Dictionary<string, BaseItem> BuildPathMap(IReadOnlyList<BaseItem> items)
        {
            var map = new Dictionary<string, BaseItem>(items.Count, StringComparer.Ordinal);
            foreach (var item in items.Where(i => !string.IsNullOrEmpty(i.Path)))
            {
                map[NormalizePath(item.Path)] = item;
            }

            return map;
        }

        private static string NormalizePath(string path)
        {
            return Path.TrimEndingDirectorySeparator(path);
        }

        private static bool IsUnchanged(BaseItem existing, FileSystemMetadata fs)
        {
            if (existing.DateModified == DateTime.MinValue)
            {
                return true;
            }

            if (Math.Abs((existing.DateModified - fs.LastWriteTimeUtc).TotalSeconds) > MtimeToleranceSeconds)
            {
                return false;
            }

            if (!fs.IsDirectory && existing.Size.HasValue && existing.Size.Value != fs.Length)
            {
                return false;
            }

            return true;
        }

        private static bool SyncFilesystemFields(BaseItem existing, FileSystemMetadata fs)
        {
            var changed = false;

            if (existing.DateModified == DateTime.MinValue
                || Math.Abs((existing.DateModified - fs.LastWriteTimeUtc).TotalSeconds) > MtimeToleranceSeconds)
            {
                existing.DateModified = fs.LastWriteTimeUtc;
                changed = true;
            }

            if (!fs.IsDirectory && (!existing.Size.HasValue || existing.Size.Value != fs.Length))
            {
                existing.Size = fs.Length;
                changed = true;
            }

            return changed;
        }

        private static SidecarIndex BuildSidecarIndex(FileSystemMetadata[] entries)
        {
            var imagesByStem = new Dictionary<string, List<FileSystemMetadata>>(StringComparer.OrdinalIgnoreCase);
            var nfoByStem = new Dictionary<string, FileSystemMetadata>(StringComparer.OrdinalIgnoreCase);
            var subsByStem = new Dictionary<string, List<FileSystemMetadata>>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in entries)
            {
                if (entry.IsDirectory)
                {
                    continue;
                }

                var name = entry.Name;
                var ext = Path.GetExtension(name.AsSpan());
                if (ext.IsEmpty)
                {
                    continue;
                }

                if (IsImageExtension(ext))
                {
                    var stem = Path.GetFileNameWithoutExtension(name) ?? string.Empty;
                    if (!imagesByStem.TryGetValue(stem, out var list))
                    {
                        list = new List<FileSystemMetadata>();
                        imagesByStem[stem] = list;
                    }

                    list.Add(entry);
                    continue;
                }

                if (ext.Equals(".nfo", StringComparison.OrdinalIgnoreCase)
                    || ext.Equals(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    var stem = Path.GetFileNameWithoutExtension(name) ?? string.Empty;
                    nfoByStem.TryAdd(stem, entry);
                    continue;
                }

                if (IsSubtitleExtension(ext))
                {
                    var stem = Path.GetFileNameWithoutExtension(name) ?? string.Empty;
                    var baseStem = StripSubtitleFlags(stem);
                    if (!subsByStem.TryGetValue(baseStem, out var list))
                    {
                        list = new List<FileSystemMetadata>();
                        subsByStem[baseStem] = list;
                    }

                    list.Add(entry);
                }
            }

            return new SidecarIndex(imagesByStem, nfoByStem, subsByStem);
        }

        private static bool AttachFileBasedSidecars(BaseItem item, SidecarIndex sidecars)
        {
            if (string.IsNullOrEmpty(item.Path) || item is Folder)
            {
                return false;
            }

            var stem = Path.GetFileNameWithoutExtension(item.Path) ?? string.Empty;
            return AttachImagesForStemPrefixes(item, sidecars, new[] { stem });
        }

        private void AttachStandaloneImagesToChildItems(string folderPath, FileSystemMetadata[] entries, ConcurrentScanResult result)
        {
            var standaloneImages = new List<(FileSystemMetadata Entry, ImageType Type)>();
            foreach (var entry in entries)
            {
                if (entry.IsDirectory)
                {
                    continue;
                }

                var ext = Path.GetExtension(entry.Name.AsSpan());
                if (!IsImageExtension(ext))
                {
                    continue;
                }

                var stem = Path.GetFileNameWithoutExtension(entry.Name) ?? string.Empty;
                var type = ClassifyFolderImageType(stem);
                if (type is not null)
                {
                    standaloneImages.Add((entry, type.Value));
                }
            }

            if (standaloneImages.Count == 0)
            {
                return;
            }

            foreach (var item in result.NewItems)
            {
                if (item is Folder || string.IsNullOrEmpty(item.Path))
                {
                    continue;
                }

                var itemDir = Path.GetDirectoryName(item.Path);
                if (!string.Equals(itemDir, folderPath, StringComparison.Ordinal))
                {
                    continue;
                }

                var existing = new HashSet<string>(item.ImageInfos.Select(i => i.Path), StringComparer.OrdinalIgnoreCase);
                foreach (var (entry, type) in standaloneImages)
                {
                    if (type == ImageType.Primary && item.ImageInfos.Any(i => i.Type == ImageType.Primary))
                    {
                        continue;
                    }

                    if (existing.Contains(entry.FullName))
                    {
                        continue;
                    }

                    item.AddImage(new ItemImageInfo
                    {
                        Path = entry.FullName,
                        Type = type,
                        DateModified = entry.LastWriteTimeUtc
                    });
                    existing.Add(entry.FullName);
                }
            }
        }

        private void AttachStandaloneImagesFromItemParentDirs(string folderPath, IDirectoryService directoryService, ConcurrentScanResult result)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in result.NewItems)
            {
                if (item is Folder || string.IsNullOrEmpty(item.Path))
                {
                    continue;
                }

                var parent = Path.GetDirectoryName(item.Path);
                if (string.IsNullOrEmpty(parent) || string.Equals(parent, folderPath, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!visited.Add(parent))
                {
                    continue;
                }

                FileSystemMetadata[] entries;
                try
                {
                    entries = directoryService.GetFileSystemEntries(parent);
                }
                catch
                {
                    continue;
                }

                AttachStandaloneImagesToChildItems(parent, entries, result);
            }
        }

        private void AttachFolderOwnSidecars(Folder folder, FileSystemMetadata[] entries, ConcurrentScanResult result)
        {
            var existingPaths = new HashSet<string>(folder.ImageInfos.Select(i => i.Path), StringComparer.OrdinalIgnoreCase);
            var folderImagesAdded = false;

            foreach (var entry in entries)
            {
                if (entry.IsDirectory)
                {
                    continue;
                }

                var ext = Path.GetExtension(entry.Name.AsSpan());
                if (!IsImageExtension(ext))
                {
                    continue;
                }

                var stem = Path.GetFileNameWithoutExtension(entry.Name) ?? string.Empty;
                var type = ClassifyFolderImageType(stem);
                if (type is null || existingPaths.Contains(entry.FullName))
                {
                    continue;
                }

                folder.AddImage(new ItemImageInfo
                {
                    Path = entry.FullName,
                    Type = type.Value,
                    DateModified = entry.LastWriteTimeUtc
                });

                existingPaths.Add(entry.FullName);
                folderImagesAdded = true;
            }

            if (folderImagesAdded && !result.NewItems.Contains(folder) && !result.UpdatedItems.Contains(folder))
            {
                result.UpdatedItems.Add(folder);
            }
        }

        private static bool AttachImagesForStemPrefixes(BaseItem item, SidecarIndex sidecars, string[] stems)
        {
            var changed = false;
            var existing = new HashSet<string>(item.ImageInfos.Select(i => i.Path), StringComparer.OrdinalIgnoreCase);

            foreach (var stem in stems)
            {
                if (sidecars.Images.TryGetValue(stem, out var exactMatches))
                {
                    foreach (var img in exactMatches)
                    {
                        if (existing.Contains(img.FullName))
                        {
                            continue;
                        }

                        item.AddImage(new ItemImageInfo
                        {
                            Path = img.FullName,
                            Type = ImageType.Primary,
                            DateModified = img.LastWriteTimeUtc
                        });

                        existing.Add(img.FullName);
                        changed = true;
                    }
                }

                foreach (var pair in sidecars.Images)
                {
                    var imgStem = pair.Key;
                    if (imgStem.Length <= stem.Length || !imgStem.StartsWith(stem + "-", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var suffix = imgStem.Substring(stem.Length + 1);
                    var type = ClassifyFileImageSuffix(suffix);
                    if (type is null)
                    {
                        continue;
                    }

                    foreach (var img in pair.Value)
                    {
                        if (existing.Contains(img.FullName))
                        {
                            continue;
                        }

                        item.AddImage(new ItemImageInfo
                        {
                            Path = img.FullName,
                            Type = type.Value,
                            DateModified = img.LastWriteTimeUtc
                        });

                        existing.Add(img.FullName);
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private static ImageType? ClassifyFolderImageType(string stem)
        {
            return ClassifyImageByName(stem);
        }

        private static ImageType? ClassifyFileImageSuffix(string suffix)
        {
            return ClassifyImageByName(suffix);
        }

        private static ImageType? ClassifyImageByName(string name)
        {
            if (name.Equals("poster", StringComparison.OrdinalIgnoreCase)
                || name.Equals("folder", StringComparison.OrdinalIgnoreCase)
                || name.Equals("cover", StringComparison.OrdinalIgnoreCase)
                || name.Equals("default", StringComparison.OrdinalIgnoreCase)
                || name.Equals("movie", StringComparison.OrdinalIgnoreCase)
                || name.Equals("show", StringComparison.OrdinalIgnoreCase)
                || name.Equals("jacket", StringComparison.OrdinalIgnoreCase))
            {
                return ImageType.Primary;
            }

            if (name.Equals("backdrop", StringComparison.OrdinalIgnoreCase)
                || name.Equals("fanart", StringComparison.OrdinalIgnoreCase)
                || name.Equals("background", StringComparison.OrdinalIgnoreCase)
                || name.Equals("art", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("backdrop", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("fanart", StringComparison.OrdinalIgnoreCase))
            {
                return ImageType.Backdrop;
            }

            if (name.Equals("logo", StringComparison.OrdinalIgnoreCase)
                || name.Equals("clearlogo", StringComparison.OrdinalIgnoreCase))
            {
                return ImageType.Logo;
            }

            if (name.Equals("banner", StringComparison.OrdinalIgnoreCase))
            {
                return ImageType.Banner;
            }

            if (name.Equals("thumb", StringComparison.OrdinalIgnoreCase)
                || name.Equals("landscape", StringComparison.OrdinalIgnoreCase))
            {
                return ImageType.Thumb;
            }

            if (name.Equals("clearart", StringComparison.OrdinalIgnoreCase))
            {
                return ImageType.Art;
            }

            if (name.Equals("disc", StringComparison.OrdinalIgnoreCase)
                || name.Equals("discart", StringComparison.OrdinalIgnoreCase)
                || name.Equals("cdart", StringComparison.OrdinalIgnoreCase))
            {
                return ImageType.Disc;
            }

            return null;
        }

        private static bool IsImageExtension(ReadOnlySpan<char> ext)
        {
            foreach (var imageExtension in _imageExtensions)
            {
                if (ext.Equals(imageExtension, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSubtitleExtension(ReadOnlySpan<char> ext)
        {
            return ext.Equals(".srt", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".ass", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".ssa", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".sub", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".vtt", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".idx", StringComparison.OrdinalIgnoreCase);
        }

        private static string StripSubtitleFlags(string stem)
        {
            var idx = stem.IndexOf('.', StringComparison.Ordinal);
            return idx > 0 ? stem.Substring(0, idx) : stem;
        }

        private void NormalizeTvRelationshipsFast(Phase1TreeScanResult result, CollectionType collectionType)
        {
            if (collectionType != CollectionType.tvshows)
            {
                return;
            }

            var byParent = new Dictionary<Guid, List<BaseItem>>();
            foreach (var item in result.AllItems)
            {
                if (!byParent.TryGetValue(item.ParentId, out var list))
                {
                    list = new List<BaseItem>();
                    byParent[item.ParentId] = list;
                }

                list.Add(item);
            }

            var seriesList = result.AllItems.OfType<Series>().ToList();
            foreach (var series in seriesList)
            {
                if (!byParent.TryGetValue(series.Id, out var directChildren))
                {
                    continue;
                }

                var seasonsByIndex = new Dictionary<int, Season>();
                var unassignedEpisodes = new List<Episode>();

                foreach (var child in directChildren)
                {
                    if (child is Season season && season.IndexNumber.HasValue)
                    {
                        seasonsByIndex[season.IndexNumber.Value] = season;
                    }
                    else if (child is Episode episode)
                    {
                        unassignedEpisodes.Add(episode);
                    }
                }

                foreach (var episode in unassignedEpisodes)
                {
                    var seasonNumber = episode.ParentIndexNumber ?? 1;
                    if (!seasonsByIndex.TryGetValue(seasonNumber, out var season))
                    {
                        var seasonPath = Path.Combine(series.Path, $"Season {seasonNumber:D2}");
                        season = new Season
                        {
                            Path = seasonPath,
                            Name = $"Season {seasonNumber}",
                            ParentId = series.Id,
                            IndexNumber = seasonNumber,
                        };

                        season.Id = _libraryManager.GetNewItemId(season.Path, season.GetType());
                        season.SetParent(series);
                        result.NewItems.Add(season);
                        result.AllItems.Add(season);
                        seasonsByIndex[seasonNumber] = season;
                    }

                    episode.SeriesId = series.Id;
                    episode.SeriesName = series.Name;
                    episode.SeasonId = season.Id;
                    episode.SeasonName = season.Name;
                    episode.ParentId = season.Id;
                    episode.SetParent(season);
                }

                foreach (var season in seasonsByIndex.Values)
                {
                    if (!byParent.TryGetValue(season.Id, out var seasonChildren))
                    {
                        continue;
                    }

                    foreach (var item in seasonChildren)
                    {
                        if (item is not Episode episode)
                        {
                            continue;
                        }

                        if (episode.SeriesId.IsEmpty())
                        {
                            episode.SeriesId = series.Id;
                            episode.SeriesName = series.Name;
                        }

                        if (episode.SeasonId.IsEmpty())
                        {
                            episode.SeasonId = season.Id;
                            episode.SeasonName = season.Name;
                        }
                    }
                }
            }
        }

        private readonly record struct FolderWorkItem(Folder Folder, bool IsKnownNewSubtree);

        private sealed record SidecarIndex(
            Dictionary<string, List<FileSystemMetadata>> Images,
            Dictionary<string, FileSystemMetadata> Nfo,
            Dictionary<string, List<FileSystemMetadata>> Subtitles);

        private sealed class ScanStats
        {
            private int _reused;
            private int _resolved;
            private int _foldersProcessed;

            public int Reused => _reused;

            public int Resolved => _resolved;

            public int FoldersProcessed => _foldersProcessed;

            public void IncReused() => Interlocked.Increment(ref _reused);

            public void IncResolved() => Interlocked.Increment(ref _resolved);

            public void IncFolders() => Interlocked.Increment(ref _foldersProcessed);
        }

        private sealed class ConcurrentScanResult
        {
            public ConcurrentBag<BaseItem> AllItems { get; } = new();

            public ConcurrentBag<BaseItem> NewItems { get; } = new();

            public ConcurrentBag<BaseItem> UpdatedItems { get; } = new();

            public ConcurrentBag<BaseItem> RemovedItems { get; } = new();

            public Phase1TreeScanResult ToPhase1Result()
            {
                var r = new Phase1TreeScanResult { Scanned = true };
                r.AllItems.AddRange(AllItems);
                r.NewItems.AddRange(NewItems);
                r.UpdatedItems.AddRange(UpdatedItems);
                r.RemovedItems.AddRange(RemovedItems);
                return r;
            }
        }
    }
}
