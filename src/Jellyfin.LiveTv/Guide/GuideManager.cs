using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using Jellyfin.LiveTv.Configuration;
using Jellyfin.LiveTv.Listings;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LiveTv.Guide;

/// <inheritdoc />
public class GuideManager : IGuideManager
{
    private const int MaxGuideDays = 14;
    private const string EtagKey = "ProgramEtag";
    private const string ExternalServiceTag = "ExternalServiceId";
    private const int ChannelSaveBatchSize = 100;

    private static readonly ParallelOptions _cacheParallelOptions = new() { MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 10) };

    private readonly ILogger<GuideManager> _logger;
    private readonly IConfigurationManager _config;
    private readonly IFileSystem _fileSystem;
    private readonly IItemRepository _itemRepo;
    private readonly ILibraryManager _libraryManager;
    private readonly ILiveTvManager _liveTvManager;
    private readonly ITunerHostManager _tunerHostManager;
    private readonly IRecordingsManager _recordingsManager;
    private readonly ISchedulesDirectService _schedulesDirectService;
    private readonly LiveTvDtoService _tvDtoService;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Amount of days images are pre-cached from external sources.
    /// </summary>
    public const int MaxCacheDays = 2;

    /// <summary>
    /// Initializes a new instance of the <see cref="GuideManager"/> class.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger{TCategoryName}"/>.</param>
    /// <param name="config">The <see cref="IConfigurationManager"/>.</param>
    /// <param name="fileSystem">The <see cref="IFileSystem"/>.</param>
    /// <param name="itemRepo">The <see cref="IItemRepository"/>.</param>
    /// <param name="libraryManager">The <see cref="ILibraryManager"/>.</param>
    /// <param name="liveTvManager">The <see cref="ILiveTvManager"/>.</param>
    /// <param name="tunerHostManager">The <see cref="ITunerHostManager"/>.</param>
    /// <param name="recordingsManager">The <see cref="IRecordingsManager"/>.</param>
    /// <param name="schedulesDirectService">The <see cref="ISchedulesDirectService"/>.</param>
    /// <param name="tvDtoService">The <see cref="LiveTvDtoService"/>.</param>
    /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
    public GuideManager(
        ILogger<GuideManager> logger,
        IConfigurationManager config,
        IFileSystem fileSystem,
        IItemRepository itemRepo,
        ILibraryManager libraryManager,
        ILiveTvManager liveTvManager,
        ITunerHostManager tunerHostManager,
        IRecordingsManager recordingsManager,
        ISchedulesDirectService schedulesDirectService,
        LiveTvDtoService tvDtoService,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _config = config;
        _fileSystem = fileSystem;
        _itemRepo = itemRepo;
        _libraryManager = libraryManager;
        _liveTvManager = liveTvManager;
        _tunerHostManager = tunerHostManager;
        _recordingsManager = recordingsManager;
        _schedulesDirectService = schedulesDirectService;
        _tvDtoService = tvDtoService;
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public GuideInfo GetGuideInfo()
    {
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddDays(GetGuideDays());

        return new GuideInfo
        {
            StartDate = startDate,
            EndDate = endDate
        };
    }

    /// <inheritdoc />
    public async Task RefreshGuide(IProgress<double> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);

        await _recordingsManager.CreateRecordingFolders().ConfigureAwait(false);

        await _tunerHostManager.ScanForTunerDeviceChanges(cancellationToken).ConfigureAwait(false);

        var numComplete = 0;
        double progressPerService = _liveTvManager.Services.Count == 0
            ? 0
            : 1.0 / _liveTvManager.Services.Count;

        var newChannelIdList = new List<Guid>();
        var newProgramIdList = new List<Guid>();

        var cleanDatabase = true;

        foreach (var service in _liveTvManager.Services)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogDebug("Refreshing guide from {Name}", service.Name);

            try
            {
                var innerProgress = new Progress<double>(p => progress.Report(p * progressPerService));

                var idList = await RefreshChannelsInternal(service, innerProgress, cancellationToken).ConfigureAwait(false);

                newChannelIdList.AddRange(idList.Item1);
                newProgramIdList.AddRange(idList.Item2);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                cleanDatabase = false;
                _logger.LogError(ex, "Error refreshing channels for service");
            }

            numComplete++;
            double percent = numComplete;
            percent /= _liveTvManager.Services.Count;

            progress.Report(100 * percent);
        }

        if (cleanDatabase)
        {
            CleanDatabase(newChannelIdList, [BaseItemKind.LiveTvChannel], progress, cancellationToken);
            CleanDatabase(newProgramIdList, [BaseItemKind.LiveTvProgram], progress, cancellationToken);
        }

        var coreService = _liveTvManager.Services.OfType<DefaultLiveTvService>().FirstOrDefault();
        if (coreService is not null)
        {
            await coreService.RefreshSeriesTimers(cancellationToken).ConfigureAwait(false);
            await coreService.RefreshTimers(cancellationToken).ConfigureAwait(false);
        }

        progress.Report(100);
    }

    private double GetGuideDays()
    {
        var config = _config.GetLiveTvConfiguration();

        return config.GuideDays.HasValue
            ? Math.Clamp(config.GuideDays.Value, 1, MaxGuideDays)
            : 7;
    }

    private async Task<Tuple<List<Guid>, List<Guid>>> RefreshChannelsInternal(ILiveTvService service, IProgress<double> progress, CancellationToken cancellationToken)
    {
        progress.Report(10);

        var allChannelsList = (await service.GetChannelsAsync(cancellationToken).ConfigureAwait(false))
            .Select(i => new Tuple<string, ChannelInfo>(service.Name, i))
            .ToList();

        var list = new List<LiveTvChannel>();
        // Keyed by item id: a duplicate channel id in the provider's list maps to the same cached
        // BaseItem instance, which the parallel icon pass below would then mutate from two tasks.
        var channelSources = new Dictionary<Guid, (LiveTvChannel Channel, ChannelInfo Info)>();
        var changedChannels = new HashSet<Guid>();

        var numComplete = 0;
        var parentFolder = _liveTvManager.GetInternalLiveTvFolder(cancellationToken);

        foreach (var channelInfo in allChannelsList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var (item, changed) = await GetChannel(channelInfo.Item2, channelInfo.Item1, parentFolder, cancellationToken).ConfigureAwait(false);

                list.Add(item);
                channelSources[item.Id] = (item, channelInfo.Item2);
                if (changed)
                {
                    changedChannels.Add(item.Id);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting channel information for {Name}", channelInfo.Item2.Name);
            }

            numComplete++;
            double percent = numComplete;
            percent /= allChannelsList.Count;

            progress.Report((3 * percent) + 10);
        }

        var (iconChanged, validatorsChanged) = await UpdateChannelImagesAsync(
            channelSources.Values,
            new Progress<double>(percent => progress.Report((2 * percent) + 13)),
            cancellationToken).ConfigureAwait(false);

        // Persist the icon results now instead of leaving it to the program loop below: the pass
        // mutated the shared cached items, so an unsaved change makes the next refresh believe the icon
        // is current and the stale image would stick. ImageUpdate is what makes UpdateItemsAsync
        // download and localise the new remote icon, along with its dimensions and blurhash.
        await SaveChannelsInBatchesAsync(iconChanged, parentFolder, ItemUpdateType.ImageUpdate, cancellationToken).ConfigureAwait(false);
        await SaveChannelsInBatchesAsync(validatorsChanged, parentFolder, ItemUpdateType.MetadataImport, cancellationToken).ConfigureAwait(false);

        progress.Report(15);

        numComplete = 0;
        var programIds = new List<Guid>();
        var channels = new List<Guid>();
        var channelsToSave = new List<BaseItem>();

        var guideDays = GetGuideDays();

        _logger.LogInformation("Refreshing guide with {Days} days of guide data", guideDays);

        var maxCacheDate = DateTime.UtcNow.AddDays(MaxCacheDays);
        foreach (var currentChannel in list)
        {
            cancellationToken.ThrowIfCancellationRequested();
            channels.Add(currentChannel.Id);

            try
            {
                var start = DateTime.UtcNow.AddHours(-1);
                var end = start.AddDays(guideDays);

                var isMovie = false;
                var isSports = false;
                var isNews = false;
                var isKids = false;
                var isSeries = false;

                var channelPrograms = (await service.GetProgramsAsync(currentChannel.ExternalId, start, end, cancellationToken).ConfigureAwait(false)).ToList();

                var existingPrograms = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = [BaseItemKind.LiveTvProgram],
                    ChannelIds = [currentChannel.Id],
                    DtoOptions = new DtoOptions(true)
                }).Cast<LiveTvProgram>().ToDictionary(i => i.Id);

                var newPrograms = new List<LiveTvProgram>();
                var updatedPrograms = new List<LiveTvProgram>();
                var programsToCache = new List<LiveTvProgram>();

                foreach (var program in channelPrograms)
                {
                    var (programItem, isNew, isUpdated) = GetProgram(program, existingPrograms, currentChannel);
                    var id = programItem.Id;
                    if (isNew)
                    {
                        newPrograms.Add(programItem);
                    }
                    else if (isUpdated)
                    {
                        updatedPrograms.Add(programItem);
                    }
                    else if (programItem.ImageInfos.Any(i => !i.IsLocalFile))
                    {
                        // An unchanged program still needs its images cached once it moves into the
                        // cache window, which it does not do on the refresh that first stored it.
                        // PreCacheImages applies the date and daily-limit rules.
                        programsToCache.Add(programItem);
                    }

                    programIds.Add(programItem.Id);

                    isMovie |= program.IsMovie;
                    isSeries |= program.IsSeries;
                    isSports |= program.IsSports;
                    isNews |= program.IsNews;
                    isKids |= program.IsKids;
                }

                _logger.LogDebug(
                    "Channel {Name} has {NewCount} new programs and {UpdatedCount} updated programs",
                    currentChannel.Name,
                    newPrograms.Count,
                    updatedPrograms.Count);

                if (newPrograms.Count > 0)
                {
                    _libraryManager.CreateItems(newPrograms, currentChannel, cancellationToken);

                    await PreCacheImages(newPrograms, maxCacheDate).ConfigureAwait(false);
                }

                if (updatedPrograms.Count > 0)
                {
                    await _libraryManager.UpdateItemsAsync(
                        updatedPrograms,
                        currentChannel,
                        ItemUpdateType.MetadataImport,
                        cancellationToken).ConfigureAwait(false);

                    await PreCacheImages(updatedPrograms, maxCacheDate).ConfigureAwait(false);
                }

                if (programsToCache.Count > 0)
                {
                    // ConvertImageToLocal persists each converted image itself, so these programs do
                    // not need a save of their own.
                    await PreCacheImages(programsToCache, maxCacheDate).ConfigureAwait(false);
                }

                var flagsChanged = currentChannel.IsMovie != isMovie
                    || currentChannel.IsNews != isNews
                    || currentChannel.IsSports != isSports
                    || currentChannel.IsSeries != isSeries
                    || (isKids && !currentChannel.Tags.Contains("Kids", StringComparer.OrdinalIgnoreCase));

                currentChannel.IsMovie = isMovie;
                currentChannel.IsNews = isNews;
                currentChannel.IsSports = isSports;
                currentChannel.IsSeries = isSeries;

                if (isKids)
                {
                    currentChannel.AddTag("Kids");
                }

                // Persisting the channel and re-running metadata providers is expensive, so only do it when needed.
                // Icon changes are already saved above and deliberately not handled here.
                if (changedChannels.Contains(currentChannel.Id))
                {
                    // The channel metadata changed: run a full refresh.
                    await currentChannel.RefreshMetadata(
                        new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                        {
                            ForceSave = true
                        },
                        cancellationToken).ConfigureAwait(false);
                }
                else if (flagsChanged)
                {
                    // Only the derived category flags changed.
                    channelsToSave.Add(currentChannel);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting programs for channel {Name}", currentChannel.Name);
            }

            if (channelsToSave.Count >= ChannelSaveBatchSize)
            {
                await FlushChannelUpdatesAsync(channelsToSave, parentFolder, ItemUpdateType.MetadataImport, cancellationToken).ConfigureAwait(false);
                channelsToSave.Clear();
            }

            numComplete++;
            double percent = numComplete / (double)allChannelsList.Count;

            progress.Report((85 * percent) + 15);
        }

        // Flush any channels left over from the last, partial batch.
        await FlushChannelUpdatesAsync(channelsToSave, parentFolder, ItemUpdateType.MetadataImport, cancellationToken).ConfigureAwait(false);
        channelsToSave.Clear();

        progress.Report(100);
        return new Tuple<List<Guid>, List<Guid>>(channels, programIds);
    }

    private async Task SaveChannelsInBatchesAsync(
        IReadOnlyList<BaseItem> channels,
        BaseItem parentFolder,
        ItemUpdateType reason,
        CancellationToken cancellationToken)
    {
        foreach (var batch in channels.Chunk(ChannelSaveBatchSize))
        {
            await FlushChannelUpdatesAsync(batch, parentFolder, reason, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task FlushChannelUpdatesAsync(
        IReadOnlyList<BaseItem> channelsToSave,
        BaseItem parentFolder,
        ItemUpdateType reason,
        CancellationToken cancellationToken)
    {
        if (channelsToSave.Count == 0)
        {
            return;
        }

        try
        {
            await _libraryManager.UpdateItemsAsync(channelsToSave, parentFolder, reason, cancellationToken).ConfigureAwait(false);
            return;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving {Count} updated Live TV channels, retrying individually", channelsToSave.Count);
        }

        // UpdateItemsAsync persists the whole set, so a single bad channel would discard the batch.
        foreach (var channel in channelsToSave)
        {
            try
            {
                await _libraryManager.UpdateItemAsync(channel, parentFolder, reason, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving updated Live TV channel {Name}", channel.Name);
            }
        }
    }

    /// <summary>
    /// Detects and applies channel icon changes for a whole channel list, in parallel.
    /// </summary>
    /// <param name="channels">The channels and their guide or tuner metadata. Must not contain duplicate items.</param>
    /// <param name="progress">Reports the fraction of channels processed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The channels whose icon changed, and those where only the cache validators changed.</returns>
    private async Task<(List<BaseItem> IconChanged, List<BaseItem> ValidatorsChanged)> UpdateChannelImagesAsync(
        IReadOnlyCollection<(LiveTvChannel Channel, ChannelInfo Info)> channels,
        IProgress<double> progress,
        CancellationToken cancellationToken)
    {
        if (channels.Count == 0)
        {
            return ([], []);
        }

        // Both lists only hold in-memory mutations: the caller has to persist them. IconChanged needs
        // ItemUpdateType.ImageUpdate so the new remote icon is downloaded, the rest a plain save.
        var iconChanged = new ConcurrentBag<BaseItem>();
        var validatorsChanged = new ConcurrentBag<BaseItem>();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 6),
            CancellationToken = cancellationToken
        };

        var numComplete = 0;

        await Parallel.ForEachAsync(
            channels,
            options,
            async (source, ct) =>
            {
                try
                {
                    var result = await UpdateChannelImageIfNeededAsync(
                        source.Channel,
                        source.Info.ImagePath,
                        source.Info.ImageUrl,
                        ct).ConfigureAwait(false);

                    switch (result)
                    {
                        case ChannelImageUpdate.ImageChanged:
                            iconChanged.Add(source.Channel);
                            break;
                        case ChannelImageUpdate.ValidatorsOnly:
                            validatorsChanged.Add(source.Channel);
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating icon for channel {Name}", source.Channel.Name);
                }

                // This phase is network bound and can take minutes on large channel lists.
                progress.Report(Interlocked.Increment(ref numComplete) / (double)channels.Count);
            }).ConfigureAwait(false);

        return ([.. iconChanged], [.. validatorsChanged]);
    }

    /// <summary>
    /// Applies the channel icon from guide or tuner metadata when it actually changed.
    /// </summary>
    /// <param name="item">The channel item.</param>
    /// <param name="imagePath">The local image path from the tuner, if any.</param>
    /// <param name="imageUrl">The remote image URL from the guide provider, if any.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>What the caller has to persist, if anything.</returns>
    // Any result other than None must be persisted by the caller: item is the shared cached instance,
    // so an unsaved mutation makes the next refresh believe nothing changed and leaves the icon stale.
    private async Task<ChannelImageUpdate> UpdateChannelImageIfNeededAsync(
        BaseItem item,
        string? imagePath,
        string? imageUrl,
        CancellationToken cancellationToken)
    {
        var newImageSource = !string.IsNullOrWhiteSpace(imagePath)
            ? imagePath
            : imageUrl;

        if (string.IsNullOrWhiteSpace(newImageSource))
        {
            return ChannelImageUpdate.None;
        }

        // Only remote http(s) sources can be probed for changes; a local tuner path is treated as-is.
        var isRemote = string.IsNullOrWhiteSpace(imagePath)
            && (newImageSource.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || newImageSource.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

        var primary = item.GetImageInfo(ImageType.Primary, 0);

        // Apply unconditionally when the channel has no primary image yet or the source path/URL changed.
        if (primary is null || !string.Equals(primary.Source, newImageSource, StringComparison.Ordinal))
        {
            ApplySource(item, newImageSource, etag: null, lastModified: null);
            return ChannelImageUpdate.ImageChanged;
        }

        // Same local (tuner) path or a non-http source: keep the cached image, nothing to detect.
        if (!isRemote)
        {
            return ChannelImageUpdate.None;
        }

        // Same remote URL: only re-apply (and re-download) when the picon content actually changed.
        var probe = await ProbeRemoteAsync(newImageSource, primary.ETag, primary.SourceLastModified, cancellationToken).ConfigureAwait(false);

        if (probe.Changed)
        {
            ApplySource(item, newImageSource, probe.ETag, probe.LastModified);
            return ChannelImageUpdate.ImageChanged;
        }

        // Newly learned validators have to be saved, otherwise every later refresh probes without them
        // and could never detect a change.
        if (probe.LearnedValidators)
        {
            primary.ETag = probe.ETag;
            primary.SourceLastModified = probe.LastModified;
            return ChannelImageUpdate.ValidatorsOnly;
        }

        return ChannelImageUpdate.None;
    }

    private static void ApplySource(BaseItem item, string source, string? etag, DateTime? lastModified)
    {
        item.SetImagePath(ImageType.Primary, source);

        // SetImagePath preserves fields it does not know about, so update the source/validators explicitly.
        var image = item.GetImageInfo(ImageType.Primary, 0);
        if (image is not null)
        {
            image.Source = source;
            image.ETag = etag;
            image.SourceLastModified = lastModified;
        }
    }

    private async Task<RemoteProbeResult> ProbeRemoteAsync(
        string url,
        string? storedETag,
        DateTime? storedLastModified,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(storedETag))
            {
                request.Headers.TryAddWithoutValidation("If-None-Match", storedETag);
            }

            if (storedLastModified.HasValue)
            {
                request.Headers.TryAddWithoutValidation(
                    "If-Modified-Since",
                    storedLastModified.Value.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture));
            }

            var client = _httpClientFactory.CreateClient(NamedClient.Default);
            // ResponseHeadersRead avoids buffering the body: on a 200 we inspect the validators and
            // dispose without downloading the payload (the actual download happens later, only if needed).
            using var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                return new RemoteProbeResult(false, false, storedETag, storedLastModified);
            }

            if (!response.IsSuccessStatusCode)
            {
                // Can't determine the state; keep the cached icon rather than re-downloading every refresh.
                _logger.LogDebug("Channel icon {Url} returned {StatusCode}; keeping cached image", url, response.StatusCode);
                return new RemoteProbeResult(false, false, storedETag, storedLastModified);
            }

            var newETag = response.Headers.ETag?.ToString();
            var newLastModified = response.Content.Headers.LastModified?.UtcDateTime;

            var hadValidators = !string.IsNullOrEmpty(storedETag) || storedLastModified.HasValue;
            var hasValidators = !string.IsNullOrEmpty(newETag) || newLastModified.HasValue;

            if (!hasValidators)
            {
                // The server exposes no cache validators, so we can't tell whether it changed: re-download.
                return new RemoteProbeResult(true, false, null, null);
            }

            if (!hadValidators)
            {
                // First time we record validators for an already-cached icon; assume the cache is current.
                return new RemoteProbeResult(false, true, newETag, newLastModified);
            }

            // Prefer the ETag when both sides have one: it is authoritative, while Last-Modified is often
            // inconsistent across load-balanced origins and would otherwise cause spurious re-downloads.
            bool unchanged;
            if (!string.IsNullOrEmpty(newETag) && !string.IsNullOrEmpty(storedETag))
            {
                unchanged = string.Equals(newETag, storedETag, StringComparison.Ordinal);
            }
            else
            {
                unchanged = newLastModified.HasValue && newLastModified == storedLastModified;
            }

            // Only worth a save when a validator we did not have before appeared. Drift in one that is
            // already stored (typically Last-Modified varying across load-balanced origins while the
            // authoritative ETag is stable) must not trigger one, or every refresh re-saves every channel.
            var learned = (string.IsNullOrEmpty(storedETag) && !string.IsNullOrEmpty(newETag))
                || (!storedLastModified.HasValue && newLastModified.HasValue);

            return new RemoteProbeResult(!unchanged, learned, newETag, newLastModified);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Network error, timeout or unsupported method: keep the cached icon.
            _logger.LogDebug(ex, "Unable to check channel icon {Url} for changes; keeping cached image", url);
            return new RemoteProbeResult(false, false, storedETag, storedLastModified);
        }
    }

    private void CleanDatabase(IReadOnlyCollection<Guid> currentIdList, BaseItemKind[] validTypes, IProgress<double> progress, CancellationToken cancellationToken)
    {
        var list = _itemRepo.GetItemIdsList(new InternalItemsQuery
        {
            IncludeItemTypes = validTypes,
            DtoOptions = new DtoOptions(false)
        });

        // Both collections scale with the number of programs, so a linear lookup per row would make
        // this quadratic in the size of the guide.
        var currentIds = currentIdList as HashSet<Guid> ?? [.. currentIdList];

        var numComplete = 0;

        foreach (var itemId in list)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (itemId.IsEmpty())
            {
                // Somehow some invalid data got into the db. It probably predates the boundary checking
                continue;
            }

            if (!currentIds.Contains(itemId))
            {
                var item = _libraryManager.GetItemById(itemId);

                if (item is not null)
                {
                    _libraryManager.DeleteItem(
                        item,
                        new DeleteOptions
                        {
                            DeleteFileLocation = false,
                            DeleteFromExternalProvider = false
                        },
                        false);
                }
            }

            numComplete++;
            double percent = numComplete / (double)list.Count;

            progress.Report(100 * percent);
        }
    }

    private async Task<(LiveTvChannel Item, bool Changed)> GetChannel(
        ChannelInfo channelInfo,
        string serviceName,
        BaseItem parentFolder,
        CancellationToken cancellationToken)
    {
        var parentFolderId = parentFolder.Id;
        var isNew = false;
        var forceUpdate = false;

        var id = _tvDtoService.GetInternalChannelId(serviceName, channelInfo.Id);

        if (_libraryManager.GetItemById(id) is not LiveTvChannel item)
        {
            item = new LiveTvChannel
            {
                Name = channelInfo.Name,
                Id = id,
                DateCreated = DateTime.UtcNow
            };

            isNew = true;
        }

        if (channelInfo.Tags is not null)
        {
            if (!channelInfo.Tags.SequenceEqual(item.Tags, StringComparer.OrdinalIgnoreCase))
            {
                isNew = true;
            }

            item.Tags = channelInfo.Tags;
        }

        if (!item.ParentId.Equals(parentFolderId))
        {
            isNew = true;
        }

        item.ParentId = parentFolderId;

        item.ChannelType = channelInfo.ChannelType;
        item.ServiceName = serviceName;

        if (!string.Equals(item.GetProviderId(ExternalServiceTag), serviceName, StringComparison.OrdinalIgnoreCase))
        {
            forceUpdate = true;
        }

        item.SetProviderId(ExternalServiceTag, serviceName);

        if (!string.Equals(channelInfo.Id, item.ExternalId, StringComparison.Ordinal))
        {
            forceUpdate = true;
        }

        item.ExternalId = channelInfo.Id;

        if (!string.Equals(channelInfo.Number, item.Number, StringComparison.Ordinal))
        {
            forceUpdate = true;
        }

        item.Number = channelInfo.Number;

        if (!string.Equals(channelInfo.Name, item.Name, StringComparison.Ordinal))
        {
            forceUpdate = true;
        }

        item.Name = channelInfo.Name;

        // The icon is deliberately not handled here: UpdateChannelImagesAsync probes every channel's
        // source in parallel afterwards, so an unchanged picon is not re-downloaded on every refresh.

        if (isNew)
        {
            _libraryManager.CreateItem(item, parentFolder);
        }
        else if (forceUpdate)
        {
            await _libraryManager.UpdateItemAsync(item, parentFolder, ItemUpdateType.MetadataImport, cancellationToken).ConfigureAwait(false);
        }

        return (item, isNew || forceUpdate);
    }

    private (LiveTvProgram Item, bool IsNew, bool IsUpdated) GetProgram(
        ProgramInfo info,
        Dictionary<Guid, LiveTvProgram> allExistingPrograms,
        LiveTvChannel channel)
    {
        var id = _tvDtoService.GetInternalProgramId(info.Id);

        var isNew = false;
        var forceUpdate = false;

        // Providers that do not supply a usable etag get one computed from the same ProgramInfo
        // fields, so they get the unchanged-program fast path too instead of having every program
        // rewritten on every refresh. Computed before anything below mutates info, so the hash is
        // reproducible across refreshes.
        var incomingEtag = info.Etag;
        if (!ProgramEtag.IsProgramEtag(incomingEtag)
            && ProgramEtag.TryCreate(info, out var computedEtag, out _))
        {
            incomingEtag = computedEtag;
        }

        if (!allExistingPrograms.TryGetValue(id, out var item))
        {
            isNew = true;
            item = new LiveTvProgram
            {
                Name = info.Name,
                Id = id,
                DateCreated = DateTime.UtcNow,
                DateModified = DateTime.UtcNow
            };
        }
        else if (ProgramEtag.MatchesStored(incomingEtag, item.GetProviderId(EtagKey)))
        {
            // The etag is generated from the final ProgramInfo fields Jellyfin consumes, so an exact
            // match means nothing relevant changed and the item does not have to be touched.
            return (item, false, false);
        }

        if (!string.Equals(info.ShowId, item.ShowId, StringComparison.OrdinalIgnoreCase))
        {
            item.ShowId = info.ShowId;
            forceUpdate = true;
        }

        var channelId = channel.Id;
        if (!item.ParentId.Equals(channelId))
        {
            item.ParentId = channel.Id;
            forceUpdate = true;
        }

        item.Audio = info.Audio;
        item.ChannelId = channelId;
        item.CommunityRating = info.CommunityRating;
        item.EpisodeTitle = info.EpisodeTitle;
        item.ExternalId = info.Id;

        var seriesId = info.SeriesId;
        if (!string.IsNullOrWhiteSpace(seriesId) && !string.Equals(item.ExternalSeriesId, seriesId, StringComparison.OrdinalIgnoreCase))
        {
            item.ExternalSeriesId = seriesId;
            forceUpdate = true;
        }

        var isSeries = info.IsSeries || !string.IsNullOrEmpty(info.EpisodeTitle);
        if (isSeries || !string.IsNullOrEmpty(info.EpisodeTitle))
        {
            item.SeriesName = info.Name;
        }

        var tags = new List<string>();
        if (info.IsLive)
        {
            tags.Add("Live");
        }

        if (info.IsPremiere)
        {
            tags.Add("Premiere");
        }

        if (info.IsNews)
        {
            tags.Add("News");
        }

        if (info.IsSports)
        {
            tags.Add("Sports");
        }

        if (info.IsKids)
        {
            tags.Add("Kids");
        }

        if (info.IsRepeat)
        {
            tags.Add("Repeat");
        }

        if (info.IsMovie)
        {
            tags.Add("Movie");
        }

        if (isSeries)
        {
            tags.Add("Series");
        }

        item.Tags = tags.ToArray();
        item.Genres = info.Genres.ToArray();

        if (info.IsHD ?? false)
        {
            item.Width = 1280;
            item.Height = 720;
        }

        item.IsMovie = info.IsMovie;
        item.IsRepeat = info.IsRepeat;
        if (item.IsSeries != isSeries)
        {
            item.IsSeries = isSeries;
            forceUpdate = true;
        }

        item.Name = info.Name;
        item.OfficialRating = info.OfficialRating;
        item.Overview = info.Overview;
        item.RunTimeTicks = (info.EndDate - info.StartDate).Ticks;
        foreach (var providerId in info.SeriesProviderIds)
        {
            info.ProviderIds["Series" + providerId.Key] = providerId.Value;
        }

        item.ProviderIds = info.ProviderIds;
        if (item.StartDate != info.StartDate)
        {
            item.StartDate = info.StartDate;
            forceUpdate = true;
        }

        if (item.EndDate != info.EndDate)
        {
            item.EndDate = info.EndDate;
            forceUpdate = true;
        }

        item.ProductionYear = info.ProductionYear;
        if (!isSeries || info.IsRepeat)
        {
            item.PremiereDate = info.OriginalAirDate;
        }

        item.IndexNumber = info.EpisodeNumber;
        item.ParentIndexNumber = info.SeasonNumber;

        forceUpdate |= UpdateImages(item, info);

        // Restore the etag wiped by `item.ProviderIds = info.ProviderIds` above and
        // persist it on new items so they join the fast path on the next refresh
        // instead of taking an extra full processing cycle.
        var isUpdated = forceUpdate;
        if (string.IsNullOrWhiteSpace(incomingEtag))
        {
            isUpdated = true;
        }
        else if (!string.Equals(incomingEtag, item.GetProviderId(EtagKey), StringComparison.OrdinalIgnoreCase))
        {
            item.SetProviderId(EtagKey, incomingEtag);
            isUpdated = true;
        }

        if (isNew)
        {
            item.OnMetadataChanged();

            return (item, true, false);
        }

        if (isUpdated)
        {
            item.OnMetadataChanged();

            return (item, false, true);
        }

        return (item, false, false);
    }

    /// <summary>
    /// Applies the program images from guide metadata.
    /// </summary>
    /// <param name="item">The program item to update.</param>
    /// <param name="info">The guide metadata.</param>
    /// <returns><c>true</c> when the item was changed and has to be saved.</returns>
    internal static bool UpdateImages(BaseItem item, ProgramInfo info)
    {
        var updated = false;

        // Primary
        updated |= UpdateImage(ImageType.Primary, item, info);

        // Thumbnail
        updated |= UpdateImage(ImageType.Thumb, item, info);

        // Logo
        updated |= UpdateImage(ImageType.Logo, item, info);

        // Backdrop
        updated |= UpdateImage(ImageType.Backdrop, item, info);

        return updated;
    }

    private static bool UpdateImage(ImageType imageType, BaseItem item, ProgramInfo info)
    {
        var image = item.GetImages(imageType).FirstOrDefault();
        var newImagePath = imageType switch
        {
            ImageType.Primary => info.ImagePath,
            _ => null
        };
        var newImageUrl = imageType switch
        {
            ImageType.Backdrop => info.BackdropImageUrl,
            ImageType.Logo => info.LogoImageUrl,
            ImageType.Primary => info.ImageUrl,
            ImageType.Thumb => info.ThumbImageUrl,
            _ => null
        };

        // Compare against where the image came from, not where it currently lives: PreCacheImages
        // rewrites Path to the local cache file, so comparing that to the guide URL would always look
        // like a change and re-apply (and therefore re-download) the image on every refresh.
        // Source is empty for images stored before it was tracked, hence the Path fallback.
        var currentSource = string.IsNullOrEmpty(image?.Source) ? image?.Path : image.Source;

        var sameImage = (currentSource?.Equals(newImageUrl, StringComparison.OrdinalIgnoreCase) ?? false)
                                || (currentSource?.Equals(newImagePath, StringComparison.OrdinalIgnoreCase) ?? false);
        if (sameImage)
        {
            return false;
        }

        var newSource = !string.IsNullOrWhiteSpace(newImagePath) ? newImagePath : newImageUrl;
        if (!string.IsNullOrWhiteSpace(newSource))
        {
            item.SetImage(
                new ItemImageInfo
                {
                    Path = newSource,
                    Type = imageType,
                    Source = newSource
                },
                0);

            return true;
        }

        if (image is null)
        {
            return false;
        }

        // Report the removal, or it would only be applied in memory and never persisted.
        item.RemoveImage(image);

        return true;
    }

    private async Task PreCacheImages(IReadOnlyList<BaseItem> programs, DateTime maxCacheDate)
    {
        var sdLimitActive = _schedulesDirectService.IsImageDailyLimitActive();

        await Parallel.ForEachAsync(
            programs
                .Where(p => p.EndDate.HasValue && p.EndDate.Value < maxCacheDate)
                .Where(p => !sdLimitActive || !p.ImageInfos.All(
                    img => img.IsLocalFile || img.Path.Contains("schedulesdirect", StringComparison.OrdinalIgnoreCase)))
                .DistinctBy(p => p.Id),
            _cacheParallelOptions,
            async (program, cancellationToken) =>
            {
                // Re-check: limit may have been set by a parallel task since the LINQ filter ran.
                if (_schedulesDirectService.IsImageDailyLimitActive()
                    && program.ImageInfos.All(
                        img => img.IsLocalFile || img.Path.Contains("schedulesdirect", StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                for (var i = 0; i < program.ImageInfos.Length; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var imageInfo = program.ImageInfos[i];
                    if (imageInfo.IsLocalFile)
                    {
                        continue;
                    }

                    // Skip SD downloads once the daily limit has been hit.
                    if (imageInfo.Path.Contains("schedulesdirect", StringComparison.OrdinalIgnoreCase)
                        && _schedulesDirectService.IsImageDailyLimitActive())
                    {
                        continue;
                    }

                    _logger.LogDebug("Caching image locally: {Url}", imageInfo.Path);
                    try
                    {
                        program.ImageInfos[i] = await _libraryManager.ConvertImageToLocal(
                                program,
                                imageInfo,
                                imageIndex: 0,
                                removeOnFailure: false)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Unable to pre-cache {Url}", imageInfo.Path);
                    }
                }
            }).ConfigureAwait(false);
    }

    // LearnedValidators: a cache validator that was not stored before is now available.
    private readonly record struct RemoteProbeResult(bool Changed, bool LearnedValidators, string? ETag, DateTime? LastModified);
}
