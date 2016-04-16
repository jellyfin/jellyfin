using MediaBrowser.Common;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.LiveTv
{
    public class LiveTvDtoService
    {
        private readonly ILogger _logger;
        private readonly IImageProcessor _imageProcessor;

        private readonly IUserDataManager _userDataManager;
        private readonly IDtoService _dtoService;
        private readonly IApplicationHost _appHost;
        private readonly ILibraryManager _libraryManager;

        public LiveTvDtoService(IDtoService dtoService, IUserDataManager userDataManager, IImageProcessor imageProcessor, ILogger logger, IApplicationHost appHost, ILibraryManager libraryManager)
        {
            _dtoService = dtoService;
            _userDataManager = userDataManager;
            _imageProcessor = imageProcessor;
            _logger = logger;
            _appHost = appHost;
            _libraryManager = libraryManager;
        }

        public TimerInfoDto GetTimerInfoDto(TimerInfo info, ILiveTvService service, LiveTvProgram program, LiveTvChannel channel)
        {
            var dto = new TimerInfoDto
            {
                Id = GetInternalTimerId(service.Name, info.Id).ToString("N"),
                Overview = info.Overview,
                EndDate = info.EndDate,
                Name = info.Name,
                StartDate = info.StartDate,
                ExternalId = info.Id,
                ChannelId = GetInternalChannelId(service.Name, info.ChannelId).ToString("N"),
                Status = info.Status,
                SeriesTimerId = string.IsNullOrEmpty(info.SeriesTimerId) ? null : GetInternalSeriesTimerId(service.Name, info.SeriesTimerId).ToString("N"),
                PrePaddingSeconds = info.PrePaddingSeconds,
                PostPaddingSeconds = info.PostPaddingSeconds,
                IsPostPaddingRequired = info.IsPostPaddingRequired,
                IsPrePaddingRequired = info.IsPrePaddingRequired,
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
                dto.ProgramId = GetInternalProgramId(service.Name, info.ProgramId).ToString("N");
            }

            if (program != null)
            {
                dto.ProgramInfo = _dtoService.GetBaseItemDto(program, new DtoOptions());

                dto.ProgramInfo.TimerId = dto.Id;
                dto.ProgramInfo.SeriesTimerId = dto.SeriesTimerId;
            }

            if (channel != null)
            {
                dto.ChannelName = channel.Name;
            }

            return dto;
        }

        public SeriesTimerInfoDto GetSeriesTimerInfoDto(SeriesTimerInfo info, ILiveTvService service, string channelName)
        {
            var dto = new SeriesTimerInfoDto
            {
                Id = GetInternalSeriesTimerId(service.Name, info.Id).ToString("N"),
                Overview = info.Overview,
                EndDate = info.EndDate,
                Name = info.Name,
                StartDate = info.StartDate,
                ExternalId = info.Id,
                PrePaddingSeconds = info.PrePaddingSeconds,
                PostPaddingSeconds = info.PostPaddingSeconds,
                IsPostPaddingRequired = info.IsPostPaddingRequired,
                IsPrePaddingRequired = info.IsPrePaddingRequired,
                Days = info.Days,
                Priority = info.Priority,
                RecordAnyChannel = info.RecordAnyChannel,
                RecordAnyTime = info.RecordAnyTime,
                RecordNewOnly = info.RecordNewOnly,
                ExternalChannelId = info.ChannelId,
                ExternalProgramId = info.ProgramId,
                ServiceName = service.Name,
                ChannelName = channelName,
                ServerId = _appHost.SystemId
            };

            if (!string.IsNullOrEmpty(info.ChannelId))
            {
                dto.ChannelId = GetInternalChannelId(service.Name, info.ChannelId).ToString("N");
            }

            if (!string.IsNullOrEmpty(info.ProgramId))
            {
                dto.ProgramId = GetInternalProgramId(service.Name, info.ProgramId).ToString("N");
            }

            dto.DayPattern = info.Days == null ? null : GetDayPattern(info.Days);

            return dto;
        }

        public DayPattern? GetDayPattern(List<DayOfWeek> days)
        {
            DayPattern? pattern = null;

            if (days.Count > 0)
            {
                if (days.Count == 7)
                {
                    pattern = DayPattern.Daily;
                }
                else if (days.Count == 2)
                {
                    if (days.Contains(DayOfWeek.Saturday) && days.Contains(DayOfWeek.Sunday))
                    {
                        pattern = DayPattern.Weekends;
                    }
                }
                else if (days.Count == 5)
                {
                    if (days.Contains(DayOfWeek.Monday) && days.Contains(DayOfWeek.Tuesday) && days.Contains(DayOfWeek.Wednesday) && days.Contains(DayOfWeek.Thursday) && days.Contains(DayOfWeek.Friday))
                    {
                        pattern = DayPattern.Weekdays;
                    }
                }
            }

            return pattern;
        }

        public LiveTvTunerInfoDto GetTunerInfoDto(string serviceName, LiveTvTunerInfo info, string channelName)
        {
            var dto = new LiveTvTunerInfoDto
            {
                Name = info.Name,
                Id = info.Id,
                Clients = info.Clients,
                ProgramName = info.ProgramName,
                SourceType = info.SourceType,
                Status = info.Status,
                ChannelName = channelName,
                Url = info.Url,
                CanReset = info.CanReset
            };

            if (!string.IsNullOrEmpty(info.ChannelId))
            {
                dto.ChannelId = GetInternalChannelId(serviceName, info.ChannelId).ToString("N");
            }

            if (!string.IsNullOrEmpty(info.RecordingId))
            {
                dto.RecordingId = GetInternalRecordingId(serviceName, info.RecordingId).ToString("N");
            }

            return dto;
        }

        internal string GetImageTag(IHasImages info)
        {
            try
            {
                return _imageProcessor.GetImageCacheTag(info, ImageType.Primary);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error getting image info for {0}", ex, info.Name);
            }

            return null;
        }

        private const string InternalVersionNumber = "4";

        public Guid GetInternalChannelId(string serviceName, string externalId)
        {
            var name = serviceName + externalId + InternalVersionNumber;

            return _libraryManager.GetNewItemId(name.ToLower(), typeof(LiveTvChannel));
        }

        public Guid GetInternalTimerId(string serviceName, string externalId)
        {
            var name = serviceName + externalId + InternalVersionNumber;

            return name.ToLower().GetMD5();
        }

        public Guid GetInternalSeriesTimerId(string serviceName, string externalId)
        {
            var name = serviceName + externalId + InternalVersionNumber;

            return name.ToLower().GetMD5();
        }

        public Guid GetInternalProgramId(string serviceName, string externalId)
        {
            var name = serviceName + externalId + InternalVersionNumber;

            return _libraryManager.GetNewItemId(name.ToLower(), typeof(LiveTvProgram));
        }

        public Guid GetInternalRecordingId(string serviceName, string externalId)
        {
            var name = serviceName + externalId + InternalVersionNumber + "0";

            return _libraryManager.GetNewItemId(name.ToLower(), typeof(ILiveTvRecording));
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

            if (!string.IsNullOrEmpty(dto.ChannelId) && string.IsNullOrEmpty(info.ChannelId))
            {
                var channel = liveTv.GetInternalChannel(dto.ChannelId);

                if (channel != null)
                {
                    info.ChannelId = channel.ExternalId;
                }
            }

            if (!string.IsNullOrEmpty(dto.ProgramId) && string.IsNullOrEmpty(info.ProgramId))
            {
                var program = liveTv.GetInternalProgram(dto.ProgramId);

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
                Days = dto.Days,
                Priority = dto.Priority,
                RecordAnyChannel = dto.RecordAnyChannel,
                RecordAnyTime = dto.RecordAnyTime,
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

            if (!string.IsNullOrEmpty(dto.ChannelId) && string.IsNullOrEmpty(info.ChannelId))
            {
                var channel = liveTv.GetInternalChannel(dto.ChannelId);

                if (channel != null)
                {
                    info.ChannelId = channel.ExternalId;
                }
            }

            if (!string.IsNullOrEmpty(dto.ProgramId) && string.IsNullOrEmpty(info.ProgramId))
            {
                var program = liveTv.GetInternalProgram(dto.ProgramId);

                if (program != null)
                {
                    info.ProgramId = program.ExternalId;
                }
            }

            return info;
        }
    }
}
