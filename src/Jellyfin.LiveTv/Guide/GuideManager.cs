using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using Jellyfin.LiveTv.Configuration;
using MediaBrowser.Common.Configuration;
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

    private static readonly ParallelOptions _cacheParallelOptions = new() { MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 10) };

    private readonly ILogger<GuideManager> _logger;
    private readonly IConfigurationManager _config;
    private readonly IFileSystem _fileSystem;
    private readonly IItemRepository _itemRepo;
    private readonly ILibraryManager _libraryManager;
    private readonly ILiveTvManager _liveTvManager;
    private readonly ITunerHostManager _tunerHostManager;
    private readonly IRecordingsManager _recordingsManager;
    private readonly LiveTvDtoService _tvDtoService;

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
    /// <param name="tvDtoService">The <see cref="LiveTvDtoService"/>.</param>
    public GuideManager(
        ILogger<GuideManager> logger,
        IConfigurationManager config,
        IFileSystem fileSystem,
        IItemRepository itemRepo,
        ILibraryManager libraryManager,
        ILiveTvManager liveTvManager,
        ITunerHostManager tunerHostManager,
        IRecordingsManager recordingsManager,
        LiveTvDtoService tvDtoService)
    {
        _logger = logger;
        _config = config;
        _fileSystem = fileSystem;
        _itemRepo = itemRepo;
        _libraryManager = libraryManager;
        _liveTvManager = liveTvManager;
        _tunerHostManager = tunerHostManager;
        _recordingsManager = recordingsManager;
        _tvDtoService = tvDtoService;
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
            CleanDatabase(newChannelIdList.ToArray(), [BaseItemKind.LiveTvChannel], progress, cancellationToken);
            CleanDatabase(newProgramIdList.ToArray(), [BaseItemKind.LiveTvProgram], progress, cancellationToken);
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

        var numComplete = 0;
        var parentFolder = _liveTvManager.GetInternalLiveTvFolder(cancellationToken);

        foreach (var channelInfo in allChannelsList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var item = await GetChannel(channelInfo.Item2, channelInfo.Item1, parentFolder, cancellationToken).ConfigureAwait(false);

                list.Add(item);
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

            progress.Report((5 * percent) + 10);
        }

        progress.Report(15);

        numComplete = 0;
        var programs = new List<Guid>();
        var channels = new List<Guid>();

        var guideDays = GetGuideDays();

        _logger.LogInformation("Refreshing guide with {0} days of guide data", guideDays);

        var maxCacheDate = DateTime.UtcNow.AddDays(2);
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
                var updatedPrograms = new List<BaseItem>();

                foreach (var program in channelPrograms)
                {
                    var (programItem, isNew, isUpdated) = GetProgram(program, existingPrograms, currentChannel);
                    if (isNew)
                    {
                        newPrograms.Add(programItem);
                    }
                    else if (isUpdated)
                    {
                        updatedPrograms.Add(programItem);
                    }

                    programs.Add(programItem.Id);

                    isMovie |= program.IsMovie;
                    isSeries |= program.IsSeries;
                    isSports |= program.IsSports;
                    isNews |= program.IsNews;
                    isKids |= program.IsKids;
                }

                _logger.LogDebug("Channel {0} has {1} new programs and {2} updated programs", currentChannel.Name, newPrograms.Count, updatedPrograms.Count);

                if (newPrograms.Count > 0)
                {
                    _libraryManager.CreateOrUpdateItems(newPrograms, null, cancellationToken);
                    await PrecacheImages(newPrograms, maxCacheDate).ConfigureAwait(false);
                }

                if (updatedPrograms.Count > 0)
                {
                    await _libraryManager.UpdateItemsAsync(
                        updatedPrograms,
                        currentChannel,
                        ItemUpdateType.MetadataImport,
                        cancellationToken).ConfigureAwait(false);
                    await PrecacheImages(updatedPrograms, maxCacheDate).ConfigureAwait(false);
                }

                currentChannel.IsMovie = isMovie;
                currentChannel.IsNews = isNews;
                currentChannel.IsSports = isSports;
                currentChannel.IsSeries = isSeries;

                if (isKids)
                {
                    currentChannel.AddTag("Kids");
                }

                await currentChannel.UpdateToRepositoryAsync(ItemUpdateType.MetadataImport, cancellationToken).ConfigureAwait(false);
                await currentChannel.RefreshMetadata(
                    new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                    {
                        ForceSave = true
                    },
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting programs for channel {Name}", currentChannel.Name);
            }

            numComplete++;
            double percent = numComplete / (double)allChannelsList.Count;

            progress.Report((85 * percent) + 15);
        }

        progress.Report(100);
        return new Tuple<List<Guid>, List<Guid>>(channels, programs);
    }

    private void CleanDatabase(Guid[] currentIdList, BaseItemKind[] validTypes, IProgress<double> progress, CancellationToken cancellationToken)
    {
        var list = _itemRepo.GetItemIdsList(new InternalItemsQuery
        {
            IncludeItemTypes = validTypes,
            DtoOptions = new DtoOptions(false)
        });

        var numComplete = 0;

        foreach (var itemId in list)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (itemId.IsEmpty())
            {
                // Somehow some invalid data got into the db. It probably predates the boundary checking
                continue;
            }

            if (!currentIdList.Contains(itemId))
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

    private async Task<LiveTvChannel> GetChannel(
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

        if (!item.HasImage(ImageType.Primary))
        {
            if (!string.IsNullOrWhiteSpace(channelInfo.ImagePath))
            {
                item.SetImagePath(ImageType.Primary, channelInfo.ImagePath);
                forceUpdate = true;
            }
            else if (!string.IsNullOrWhiteSpace(channelInfo.ImageUrl))
            {
                item.SetImagePath(ImageType.Primary, channelInfo.ImageUrl);
                forceUpdate = true;
            }
        }

        if (isNew)
        {
            _libraryManager.CreateItem(item, parentFolder);
        }
        else if (forceUpdate)
        {
            await _libraryManager.UpdateItemAsync(item, parentFolder, ItemUpdateType.MetadataImport, cancellationToken).ConfigureAwait(false);
        }

        return item;
    }

    private (LiveTvProgram Item, bool IsNew, bool IsUpdated) GetProgram(
        ProgramInfo info,
        Dictionary<Guid, LiveTvProgram> allExistingPrograms,
        LiveTvChannel channel)
    {
        var id = _tvDtoService.GetInternalProgramId(info.Id);

        var isNew = false;
        var forceUpdate = false;

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

            item.TrySetProviderId(EtagKey, info.Etag);
        }

        if (!string.Equals(info.ShowId, item.ShowId, StringComparison.OrdinalIgnoreCase))
        {
            item.ShowId = info.ShowId;
            forceUpdate = true;
        }

        var seriesId = info.SeriesId;

        if (!item.ParentId.Equals(channel.Id))
        {
            forceUpdate = true;
        }

        item.ParentId = channel.Id;

        item.Audio = info.Audio;
        item.ChannelId = channel.Id;
        item.CommunityRating ??= info.CommunityRating;
        if ((item.CommunityRating ?? 0).Equals(0))
        {
            item.CommunityRating = null;
        }

        item.EpisodeTitle = info.EpisodeTitle;
        item.ExternalId = info.Id;

        if (!string.IsNullOrWhiteSpace(seriesId) && !string.Equals(item.ExternalSeriesId, seriesId, StringComparison.Ordinal))
        {
            forceUpdate = true;
        }

        item.ExternalSeriesId = seriesId;

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
            forceUpdate = true;
        }

        item.IsSeries = isSeries;

        item.Name = info.Name;
        item.OfficialRating ??= info.OfficialRating;
        item.Overview ??= info.Overview;
        item.RunTimeTicks = (info.EndDate - info.StartDate).Ticks;
        item.ProviderIds = info.ProviderIds;

        foreach (var providerId in info.SeriesProviderIds)
        {
            info.ProviderIds["Series" + providerId.Key] = providerId.Value;
        }

        if (item.StartDate != info.StartDate)
        {
            forceUpdate = true;
        }

        item.StartDate = info.StartDate;

        if (item.EndDate != info.EndDate)
        {
            forceUpdate = true;
        }

        item.EndDate = info.EndDate;

        item.ProductionYear = info.ProductionYear;

        if (!isSeries || info.IsRepeat)
        {
            item.PremiereDate = info.OriginalAirDate;
        }

        item.IndexNumber = info.EpisodeNumber;
        item.ParentIndexNumber = info.SeasonNumber;

        if (!item.HasImage(ImageType.Primary))
        {
            if (!string.IsNullOrWhiteSpace(info.ImagePath))
            {
                item.SetImage(
                    new ItemImageInfo
                    {
                        Path = info.ImagePath,
                        Type = ImageType.Primary
                    },
                    0);
            }
            else if (!string.IsNullOrWhiteSpace(info.ImageUrl))
            {
                item.SetImage(
                    new ItemImageInfo
                    {
                        Path = info.ImageUrl,
                        Type = ImageType.Primary
                    },
                    0);
            }
        }

        if (!item.HasImage(ImageType.Thumb))
        {
            if (!string.IsNullOrWhiteSpace(info.ThumbImageUrl))
            {
                item.SetImage(
                    new ItemImageInfo
                    {
                        Path = info.ThumbImageUrl,
                        Type = ImageType.Thumb
                    },
                    0);
            }
        }

        if (!item.HasImage(ImageType.Logo))
        {
            if (!string.IsNullOrWhiteSpace(info.LogoImageUrl))
            {
                item.SetImage(
                    new ItemImageInfo
                    {
                        Path = info.LogoImageUrl,
                        Type = ImageType.Logo
                    },
                    0);
            }
        }

        if (!item.HasImage(ImageType.Backdrop))
        {
            if (!string.IsNullOrWhiteSpace(info.BackdropImageUrl))
            {
                item.SetImage(
                    new ItemImageInfo
                    {
                        Path = info.BackdropImageUrl,
                        Type = ImageType.Backdrop
                    },
                    0);
            }
        }

        var isUpdated = false;
        if (isNew)
        {
        }
        else if (forceUpdate || string.IsNullOrWhiteSpace(info.Etag))
        {
            isUpdated = true;
        }
        else
        {
            var etag = info.Etag;

            if (!string.Equals(etag, item.GetProviderId(EtagKey), StringComparison.OrdinalIgnoreCase))
            {
                item.SetProviderId(EtagKey, etag);
                isUpdated = true;
            }
        }

        if (isNew || isUpdated)
        {
            item.OnMetadataChanged();
        }

        return (item, isNew, isUpdated);
    }

    private async Task PrecacheImages(IReadOnlyList<BaseItem> programs, DateTime maxCacheDate)
    {
        await Parallel.ForEachAsync(
            programs
                .Where(p => p.EndDate.HasValue && p.EndDate.Value < maxCacheDate)
                .DistinctBy(p => p.Id),
            _cacheParallelOptions,
            async (program, cancellationToken) =>
            {
                for (var i = 0; i < program.ImageInfos.Length; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var imageInfo = program.ImageInfos[i];
                    if (!imageInfo.IsLocalFile)
                    {
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
                            _logger.LogWarning(ex, "Unable to precache {Url}", imageInfo.Path);
                        }
                    }
                }
            }).ConfigureAwait(false);
    }
}
