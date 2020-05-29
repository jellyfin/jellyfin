#pragma warning disable CS1591

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.LiveTv
{
    public class LiveTvDtoService
    {
        private const string InternalVersionNumber = "4";

        private const string ServiceName = "Emby";

        private readonly ILogger _logger;
        private readonly IImageProcessor _imageProcessor;
        private readonly IDtoService _dtoService;
        private readonly IApplicationHost _appHost;
        private readonly ILibraryManager _libraryManager;

        public LiveTvDtoService(
            IDtoService dtoService,
            IImageProcessor imageProcessor,
            ILogger<LiveTvDtoService> logger,
            IApplicationHost appHost,
            ILibraryManager libraryManager)
        {
            _dtoService = dtoService;
            _imageProcessor = imageProcessor;
            _logger = logger;
            _appHost = appHost;
            _libraryManager = libraryManager;
        }

        public TimerInfoDto GetTimerInfoDto(TimerInfo info, ILiveTvService service, LiveTvProgram program, BaseItem channel)
        {
            var dto = new TimerInfoDto
            {
                Id = GetInternalTimerId(info.Id),
                Overview = info.Overview,
                EndDate = info.EndDate,
                Name = info.Name,
                StartDate = info.StartDate,
                ExternalId = info.Id,
                ChannelId = GetInternalChannelId(service.Name, info.ChannelId),
                Status = info.Status,
                SeriesTimerId = string.IsNullOrEmpty(info.SeriesTimerId) ? null : GetInternalSeriesTimerId(info.SeriesTimerId).ToString("N", CultureInfo.InvariantCulture),
                PrePaddingSeconds = info.PrePaddingSeconds,
                PostPaddingSeconds = info.PostPaddingSeconds,
                IsPostPaddingRequired = info.IsPostPaddingRequired,
                IsPrePaddingRequired = info.IsPrePaddingRequired,
                KeepUntil = info.KeepUntil,
                ExternalChannelId = info.ChannelId,
                ExternalSeriesTimerId = info.SeriesTimerId,
                ServiceName = service.Name,
                ExternalProgramId = info.ProgramId,
                Priority = info.Priority,
                RunTimeTicks = (info.EndDate - info.StartDate).Ticks,
                ServerId = _appHost.SystemId
            };

            if (!string.IsNullOrEmpty(info.ProgramId))
            {
                dto.ProgramId = GetInternalProgramId(info.ProgramId).ToString("N", CultureInfo.InvariantCulture);
            }

            if (program != null)
            {
                dto.ProgramInfo = _dtoService.GetBaseItemDto(program, new DtoOptions());

                if (info.Status != RecordingStatus.Cancelled && info.Status != RecordingStatus.Error)
                {
                    dto.ProgramInfo.TimerId = dto.Id;
                    dto.ProgramInfo.Status = info.Status.ToString();
                }

                dto.ProgramInfo.SeriesTimerId = dto.SeriesTimerId;

                if (!string.IsNullOrEmpty(info.SeriesTimerId))
                {
                    FillImages(dto.ProgramInfo, info.Name, info.SeriesId);
                }
            }

            if (channel != null)
            {
                dto.ChannelName = channel.Name;

                if (channel.HasImage(ImageType.Primary))
                {
                    dto.ChannelPrimaryImageTag = GetImageTag(channel);
                }
            }

            return dto;
        }

        public SeriesTimerInfoDto GetSeriesTimerInfoDto(SeriesTimerInfo info, ILiveTvService service, string channelName)
        {
            var dto = new SeriesTimerInfoDto
            {
                Id = GetInternalSeriesTimerId(info.Id).ToString("N", CultureInfo.InvariantCulture),
                Overview = info.Overview,
                EndDate = info.EndDate,
                Name = info.Name,
                StartDate = info.StartDate,
                ExternalId = info.Id,
                PrePaddingSeconds = info.PrePaddingSeconds,
                PostPaddingSeconds = info.PostPaddingSeconds,
                IsPostPaddingRequired = info.IsPostPaddingRequired,
                IsPrePaddingRequired = info.IsPrePaddingRequired,
                Days = info.Days.ToArray(),
                Priority = info.Priority,
                RecordAnyChannel = info.RecordAnyChannel,
                RecordAnyTime = info.RecordAnyTime,
                SkipEpisodesInLibrary = info.SkipEpisodesInLibrary,
                KeepUpTo = info.KeepUpTo,
                KeepUntil = info.KeepUntil,
                RecordNewOnly = info.RecordNewOnly,
                ExternalChannelId = info.ChannelId,
                ExternalProgramId = info.ProgramId,
                ServiceName = service.Name,
                ChannelName = channelName,
                ServerId = _appHost.SystemId
            };

            if (!string.IsNullOrEmpty(info.ChannelId))
            {
                dto.ChannelId = GetInternalChannelId(service.Name, info.ChannelId);
            }

            if (!string.IsNullOrEmpty(info.ProgramId))
            {
                dto.ProgramId = GetInternalProgramId(info.ProgramId).ToString("N", CultureInfo.InvariantCulture);
            }

            dto.DayPattern = info.Days == null ? null : GetDayPattern(info.Days.ToArray());

            FillImages(dto, info.Name, info.SeriesId);

            return dto;
        }

        private void FillImages(BaseItemDto dto, string seriesName, string programSeriesId)
        {
            var librarySeries = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new string[] { typeof(Series).Name },
                Name = seriesName,
                Limit = 1,
                ImageTypes = new ImageType[] { ImageType.Thumb },
                DtoOptions = new DtoOptions(false)
            }).FirstOrDefault();

            if (librarySeries != null)
            {
                var image = librarySeries.GetImageInfo(ImageType.Thumb, 0);
                if (image != null)
                {
                    try
                    {
                        dto.ParentThumbImageTag = _imageProcessor.GetImageCacheTag(librarySeries, image);
                        dto.ParentThumbItemId = librarySeries.Id.ToString("N", CultureInfo.InvariantCulture);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error");
                    }
                }

                image = librarySeries.GetImageInfo(ImageType.Backdrop, 0);
                if (image != null)
                {
                    try
                    {
                        dto.ParentBackdropImageTags = new string[]
                            {
                                _imageProcessor.GetImageCacheTag(librarySeries, image)
                            };
                        dto.ParentBackdropItemId = librarySeries.Id.ToString("N", CultureInfo.InvariantCulture);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error");
                    }
                }
            }

            var program = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new string[] { nameof(LiveTvProgram) },
                ExternalSeriesId = programSeriesId,
                Limit = 1,
                ImageTypes = new ImageType[] { ImageType.Primary },
                DtoOptions = new DtoOptions(false),
                Name = string.IsNullOrEmpty(programSeriesId) ? seriesName : null
            }).FirstOrDefault();

            if (program != null)
            {
                var image = program.GetImageInfo(ImageType.Primary, 0);
                if (image != null)
                {
                    try
                    {
                        dto.ParentPrimaryImageTag = _imageProcessor.GetImageCacheTag(program, image);
                        dto.ParentPrimaryImageItemId = program.Id.ToString("N", CultureInfo.InvariantCulture);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error");
                    }
                }

                if (dto.ParentBackdropImageTags == null || dto.ParentBackdropImageTags.Length == 0)
                {
                    image = program.GetImageInfo(ImageType.Backdrop, 0);
                    if (image != null)
                    {
                        try
                        {
                            dto.ParentBackdropImageTags = new string[]
                            {
                                _imageProcessor.GetImageCacheTag(program, image)
                            };

                            dto.ParentBackdropItemId = program.Id.ToString("N", CultureInfo.InvariantCulture);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error");
                        }
                    }
                }
            }
        }

        private void FillImages(SeriesTimerInfoDto dto, string seriesName, string programSeriesId)
        {
            var librarySeries = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new string[] { typeof(Series).Name },
                Name = seriesName,
                Limit = 1,
                ImageTypes = new ImageType[] { ImageType.Thumb },
                DtoOptions = new DtoOptions(false)
            }).FirstOrDefault();

            if (librarySeries != null)
            {
                var image = librarySeries.GetImageInfo(ImageType.Thumb, 0);
                if (image != null)
                {
                    try
                    {
                        dto.ParentThumbImageTag = _imageProcessor.GetImageCacheTag(librarySeries, image);
                        dto.ParentThumbItemId = librarySeries.Id.ToString("N", CultureInfo.InvariantCulture);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error");
                    }
                }

                image = librarySeries.GetImageInfo(ImageType.Backdrop, 0);
                if (image != null)
                {
                    try
                    {
                        dto.ParentBackdropImageTags = new string[]
                            {
                                _imageProcessor.GetImageCacheTag(librarySeries, image)
                            };
                        dto.ParentBackdropItemId = librarySeries.Id.ToString("N", CultureInfo.InvariantCulture);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error");
                    }
                }
            }

            var program = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new string[] { typeof(Series).Name },
                Name = seriesName,
                Limit = 1,
                ImageTypes = new ImageType[] { ImageType.Primary },
                DtoOptions = new DtoOptions(false)
            }).FirstOrDefault();

            if (program == null)
            {
                program = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new string[] { typeof(LiveTvProgram).Name },
                    ExternalSeriesId = programSeriesId,
                    Limit = 1,
                    ImageTypes = new ImageType[] { ImageType.Primary },
                    DtoOptions = new DtoOptions(false),
                    Name = string.IsNullOrEmpty(programSeriesId) ? seriesName : null
                }).FirstOrDefault();
            }

            if (program != null)
            {
                var image = program.GetImageInfo(ImageType.Primary, 0);
                if (image != null)
                {
                    try
                    {
                        dto.ParentPrimaryImageTag = _imageProcessor.GetImageCacheTag(program, image);
                        dto.ParentPrimaryImageItemId = program.Id.ToString("N", CultureInfo.InvariantCulture);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "GetImageCacheTag raised an exception in LiveTvDtoService.FillImages.");
                    }
                }

                if (dto.ParentBackdropImageTags == null || dto.ParentBackdropImageTags.Length == 0)
                {
                    image = program.GetImageInfo(ImageType.Backdrop, 0);
                    if (image != null)
                    {
                        try
                        {
                            dto.ParentBackdropImageTags = new[]
                            {
                                    _imageProcessor.GetImageCacheTag(program, image)
                            };
                            dto.ParentBackdropItemId = program.Id.ToString("N", CultureInfo.InvariantCulture);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error");
                        }
                    }
                }
            }
        }

        public DayPattern? GetDayPattern(DayOfWeek[] days)
        {
            DayPattern? pattern = null;

            if (days.Length > 0)
            {
                if (days.Length == 7)
                {
                    pattern = DayPattern.Daily;
                }
                else if (days.Length == 2)
                {
                    if (days.Contains(DayOfWeek.Saturday) && days.Contains(DayOfWeek.Sunday))
                    {
                        pattern = DayPattern.Weekends;
                    }
                }
                else if (days.Length == 5)
                {
                    if (days.Contains(DayOfWeek.Monday) && days.Contains(DayOfWeek.Tuesday) && days.Contains(DayOfWeek.Wednesday) && days.Contains(DayOfWeek.Thursday) && days.Contains(DayOfWeek.Friday))
                    {
                        pattern = DayPattern.Weekdays;
                    }
                }
            }

            return pattern;
        }

        internal string GetImageTag(BaseItem info)
        {
            try
            {
                return _imageProcessor.GetImageCacheTag(info, ImageType.Primary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting image info for {name}", info.Name);
            }

            return null;
        }

        public Guid GetInternalChannelId(string serviceName, string externalId)
        {
            var name = serviceName + externalId + InternalVersionNumber;

            return _libraryManager.GetNewItemId(name.ToLowerInvariant(), typeof(LiveTvChannel));
        }

        public string GetInternalTimerId(string externalId)
        {
            var name = ServiceName + externalId + InternalVersionNumber;

            return name.ToLowerInvariant().GetMD5().ToString("N", CultureInfo.InvariantCulture);
        }

        public Guid GetInternalSeriesTimerId(string externalId)
        {
            var name = ServiceName + externalId + InternalVersionNumber;

            return name.ToLowerInvariant().GetMD5();
        }

        public Guid GetInternalProgramId(string externalId)
        {
            var name = ServiceName + externalId + InternalVersionNumber;

            return _libraryManager.GetNewItemId(name.ToLowerInvariant(), typeof(LiveTvProgram));
        }

        public async Task<TimerInfo> GetTimerInfo(TimerInfoDto dto, bool isNew, LiveTvManager liveTv, CancellationToken cancellationToken)
        {
            var info = new TimerInfo
            {
                Overview = dto.Overview,
                EndDate = dto.EndDate,
                Name = dto.Name,
                StartDate = dto.StartDate,
                Status = dto.Status,
                PrePaddingSeconds = dto.PrePaddingSeconds,
                PostPaddingSeconds = dto.PostPaddingSeconds,
                IsPostPaddingRequired = dto.IsPostPaddingRequired,
                IsPrePaddingRequired = dto.IsPrePaddingRequired,
                KeepUntil = dto.KeepUntil,
                Priority = dto.Priority,
                SeriesTimerId = dto.ExternalSeriesTimerId,
                ProgramId = dto.ExternalProgramId,
                ChannelId = dto.ExternalChannelId,
                Id = dto.ExternalId
            };

            // Convert internal server id's to external tv provider id's
            if (!isNew && !string.IsNullOrEmpty(dto.Id) && string.IsNullOrEmpty(info.Id))
            {
                var timer = await liveTv.GetSeriesTimer(dto.Id, cancellationToken).ConfigureAwait(false);

                info.Id = timer.ExternalId;
            }

            if (!dto.ChannelId.Equals(Guid.Empty) && string.IsNullOrEmpty(info.ChannelId))
            {
                var channel = _libraryManager.GetItemById(dto.ChannelId);

                if (channel != null)
                {
                    info.ChannelId = channel.ExternalId;
                }
            }

            if (!string.IsNullOrEmpty(dto.ProgramId) && string.IsNullOrEmpty(info.ProgramId))
            {
                var program = _libraryManager.GetItemById(dto.ProgramId);

                if (program != null)
                {
                    info.ProgramId = program.ExternalId;
                }
            }

            if (!string.IsNullOrEmpty(dto.SeriesTimerId) && string.IsNullOrEmpty(info.SeriesTimerId))
            {
                var timer = await liveTv.GetSeriesTimer(dto.SeriesTimerId, cancellationToken).ConfigureAwait(false);

                if (timer != null)
                {
                    info.SeriesTimerId = timer.ExternalId;
                }
            }

            return info;
        }

        public async Task<SeriesTimerInfo> GetSeriesTimerInfo(SeriesTimerInfoDto dto, bool isNew, LiveTvManager liveTv, CancellationToken cancellationToken)
        {
            var info = new SeriesTimerInfo
            {
                Overview = dto.Overview,
                EndDate = dto.EndDate,
                Name = dto.Name,
                StartDate = dto.StartDate,
                PrePaddingSeconds = dto.PrePaddingSeconds,
                PostPaddingSeconds = dto.PostPaddingSeconds,
                IsPostPaddingRequired = dto.IsPostPaddingRequired,
                IsPrePaddingRequired = dto.IsPrePaddingRequired,
                Days = dto.Days.ToList(),
                Priority = dto.Priority,
                RecordAnyChannel = dto.RecordAnyChannel,
                RecordAnyTime = dto.RecordAnyTime,
                SkipEpisodesInLibrary = dto.SkipEpisodesInLibrary,
                KeepUpTo = dto.KeepUpTo,
                KeepUntil = dto.KeepUntil,
                RecordNewOnly = dto.RecordNewOnly,
                ProgramId = dto.ExternalProgramId,
                ChannelId = dto.ExternalChannelId,
                Id = dto.ExternalId
            };

            // Convert internal server id's to external tv provider id's
            if (!isNew && !string.IsNullOrEmpty(dto.Id) && string.IsNullOrEmpty(info.Id))
            {
                var timer = await liveTv.GetSeriesTimer(dto.Id, cancellationToken).ConfigureAwait(false);

                info.Id = timer.ExternalId;
            }

            if (!dto.ChannelId.Equals(Guid.Empty) && string.IsNullOrEmpty(info.ChannelId))
            {
                var channel = _libraryManager.GetItemById(dto.ChannelId);

                if (channel != null)
                {
                    info.ChannelId = channel.ExternalId;
                }
            }

            if (!string.IsNullOrEmpty(dto.ProgramId) && string.IsNullOrEmpty(info.ProgramId))
            {
                var program = _libraryManager.GetItemById(dto.ProgramId);

                if (program != null)
                {
                    info.ProgramId = program.ExternalId;
                }
            }

            return info;
        }
    }
}
