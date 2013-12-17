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

namespace MediaBrowser.Server.Implementations.LiveTv
{
    public class LiveTvDtoService
    {
        private readonly ILogger _logger;
        private readonly IImageProcessor _imageProcessor;

        private readonly IUserDataManager _userDataManager;
        private readonly IDtoService _dtoService;

        public LiveTvDtoService(IDtoService dtoService, IUserDataManager userDataManager, IImageProcessor imageProcessor, ILogger logger)
        {
            _dtoService = dtoService;
            _userDataManager = userDataManager;
            _imageProcessor = imageProcessor;
            _logger = logger;
        }

        public TimerInfoDto GetTimerInfoDto(TimerInfo info, ILiveTvService service)
        {
            var dto = new TimerInfoDto
            {
                Id = GetInternalTimerId(service.Name, info.Id).ToString("N"),
                ChannelName = info.ChannelName,
                Overview = info.Overview,
                EndDate = info.EndDate,
                Name = info.Name,
                StartDate = info.StartDate,
                ExternalId = info.Id,
                ChannelId = GetInternalChannelId(service.Name, info.ChannelId, info.ChannelName).ToString("N"),
                Status = info.Status,
                SeriesTimerId = string.IsNullOrEmpty(info.SeriesTimerId) ? null : GetInternalSeriesTimerId(service.Name, info.SeriesTimerId).ToString("N"),
                RequestedPostPaddingSeconds = info.RequestedPostPaddingSeconds,
                RequestedPrePaddingSeconds = info.RequestedPrePaddingSeconds,
                RequiredPostPaddingSeconds = info.RequiredPostPaddingSeconds,
                RequiredPrePaddingSeconds = info.RequiredPrePaddingSeconds,
                ExternalChannelId = info.ChannelId,
                ExternalSeriesTimerId = info.SeriesTimerId,
                ServiceName = service.Name,
                ExternalProgramId = info.ProgramId,
                Priority = info.Priority
            };

            var duration = info.EndDate - info.StartDate;
            dto.DurationMs = Convert.ToInt32(duration.TotalMilliseconds);

            if (!string.IsNullOrEmpty(info.ProgramId))
            {
                dto.ProgramId = GetInternalProgramId(service.Name, info.ProgramId).ToString("N");
            }

            return dto;
        }

        public SeriesTimerInfoDto GetSeriesTimerInfoDto(SeriesTimerInfo info, ILiveTvService service)
        {
            var dto = new SeriesTimerInfoDto
            {
                Id = GetInternalSeriesTimerId(service.Name, info.Id).ToString("N"),
                ChannelName = info.ChannelName,
                Overview = info.Overview,
                EndDate = info.EndDate,
                Name = info.Name,
                StartDate = info.StartDate,
                ExternalId = info.Id,
                RequestedPostPaddingSeconds = info.RequestedPostPaddingSeconds,
                RequestedPrePaddingSeconds = info.RequestedPrePaddingSeconds,
                RequiredPostPaddingSeconds = info.RequiredPostPaddingSeconds,
                RequiredPrePaddingSeconds = info.RequiredPrePaddingSeconds,
                Days = info.Days,
                Priority = info.Priority,
                RecordAnyChannel = info.RecordAnyChannel,
                RecordAnyTime = info.RecordAnyTime,
                RecordNewOnly = info.RecordNewOnly,
                ExternalChannelId = info.ChannelId,
                ExternalProgramId = info.ProgramId,
                ServiceName = service.Name
            };

            if (!string.IsNullOrEmpty(info.ChannelId))
            {
                dto.ChannelId = GetInternalChannelId(service.Name, info.ChannelId, info.ChannelName).ToString("N");
            }

            if (!string.IsNullOrEmpty(info.ProgramId))
            {
                dto.ProgramId = GetInternalProgramId(service.Name, info.ProgramId).ToString("N");
            }

            DayPattern? pattern = null;

            if (info.Days != null && info.Days.Count > 0)
            {
                if (info.Days.Count == 7)
                {
                    pattern = DayPattern.Daily;
                }
                else if (info.Days.Count == 2)
                {
                    if (info.Days.Contains(DayOfWeek.Saturday) && info.Days.Contains(DayOfWeek.Sunday))
                    {
                        pattern = DayPattern.Weekends;
                    }
                }
                else if (info.Days.Count == 5)
                {
                    if (info.Days.Contains(DayOfWeek.Monday) && info.Days.Contains(DayOfWeek.Tuesday) && info.Days.Contains(DayOfWeek.Wednesday) && info.Days.Contains(DayOfWeek.Thursday) && info.Days.Contains(DayOfWeek.Friday))
                    {
                        pattern = DayPattern.Weekdays;
                    }
                }
            }

            dto.DayPattern = pattern;

            return dto;
        }

        public RecordingInfoDto GetRecordingInfoDto(RecordingInfo info, ILiveTvService service, User user = null)
        {
            var dto = new RecordingInfoDto
            {
                Id = GetInternalRecordingId(service.Name, info.Id).ToString("N"),
                ChannelName = info.ChannelName,
                Overview = info.Overview,
                EndDate = info.EndDate,
                Name = info.Name,
                StartDate = info.StartDate,
                ExternalId = info.Id,
                ChannelId = GetInternalChannelId(service.Name, info.ChannelId, info.ChannelName).ToString("N"),
                Status = info.Status,
                Path = info.Path,
                Genres = info.Genres,
                IsRepeat = info.IsRepeat,
                EpisodeTitle = info.EpisodeTitle,
                ChannelType = info.ChannelType,
                MediaType = info.ChannelType == ChannelType.Radio ? MediaType.Audio : MediaType.Video,
                CommunityRating = info.CommunityRating,
                OfficialRating = info.OfficialRating,
                Audio = info.Audio,
                IsHD = info.IsHD,
                ServiceName = service.Name,
                Url = info.Url
            };

            if (user != null)
            {
                //dto.UserData = _dtoService.GetUserItemDataDto(_userDataManager.GetUserData(user.Id, info.GetUserDataKey()));
            }

            var duration = info.EndDate - info.StartDate;
            dto.DurationMs = Convert.ToInt32(duration.TotalMilliseconds);

            if (!string.IsNullOrEmpty(info.ProgramId))
            {
                dto.ProgramId = GetInternalProgramId(service.Name, info.ProgramId).ToString("N");
            }

            return dto;
        }

        /// <summary>
        /// Gets the channel info dto.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="user">The user.</param>
        /// <returns>ChannelInfoDto.</returns>
        public ChannelInfoDto GetChannelInfoDto(Channel info, User user = null)
        {
            var dto = new ChannelInfoDto
            {
                Name = info.Name,
                ServiceName = info.ServiceName,
                ChannelType = info.ChannelType,
                Number = info.ChannelNumber,
                Type = info.GetType().Name,
                Id = info.Id.ToString("N"),
                MediaType = info.MediaType
            };

            if (user != null)
            {
                dto.UserData = _dtoService.GetUserItemDataDto(_userDataManager.GetUserData(user.Id, info.GetUserDataKey()));
            }

            var imageTag = GetLogoImageTag(info);

            if (imageTag.HasValue)
            {
                dto.ImageTags[ImageType.Primary] = imageTag.Value;
            }

            return dto;
        }

        public ProgramInfoDto GetProgramInfoDto(ProgramInfo program, Channel channel, User user = null)
        {
            var dto = new ProgramInfoDto
            {
                Id = GetInternalProgramId(channel.ServiceName, program.Id).ToString("N"),
                ChannelId = channel.Id.ToString("N"),
                Overview = program.Overview,
                EndDate = program.EndDate,
                Genres = program.Genres,
                ExternalId = program.Id,
                Name = program.Name,
                ServiceName = channel.ServiceName,
                StartDate = program.StartDate,
                OfficialRating = program.OfficialRating,
                IsHD = program.IsHD,
                OriginalAirDate = program.OriginalAirDate,
                Audio = program.Audio,
                CommunityRating = program.CommunityRating,
                AspectRatio = program.AspectRatio,
                IsRepeat = program.IsRepeat,
                EpisodeTitle = program.EpisodeTitle,
                ChannelName = program.ChannelName
            };

            if (user != null)
            {
                //dto.UserData = _dtoService.GetUserItemDataDto(_userDataManager.GetUserData(user.Id, info.GetUserDataKey()));
            }

            return dto;
        }

        private Guid? GetLogoImageTag(Channel info)
        {
            var path = info.PrimaryImagePath;

            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            try
            {
                return _imageProcessor.GetImageCacheTag(info, ImageType.Primary, path);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error getting channel image info for {0}", ex, info.Name);
            }

            return null;
        }

        public Guid GetInternalChannelId(string serviceName, string externalId, string channelName)
        {
            var name = serviceName + externalId + channelName;

            return name.ToLower().GetMBId(typeof(Channel));
        }

        public Guid GetInternalTimerId(string serviceName, string externalId)
        {
            var name = serviceName + externalId;

            return name.ToLower().GetMD5();
        }

        public Guid GetInternalSeriesTimerId(string serviceName, string externalId)
        {
            var name = serviceName + externalId;

            return name.ToLower().GetMD5();
        }

        public Guid GetInternalProgramId(string serviceName, string externalId)
        {
            var name = serviceName + externalId;

            return name.ToLower().GetMD5();
        }

        public Guid GetInternalRecordingId(string serviceName, string externalId)
        {
            var name = serviceName + externalId;

            return name.ToLower().GetMD5();
        }

        public TimerInfo GetTimerInfo(TimerInfoDto dto)
        {
            return new TimerInfo
            {
                Id = dto.ExternalId,
                ChannelName = dto.ChannelName,
                Overview = dto.Overview,
                EndDate = dto.EndDate,
                Name = dto.Name,
                StartDate = dto.StartDate,
                ChannelId = dto.ExternalChannelId,
                Status = dto.Status,
                SeriesTimerId = dto.ExternalSeriesTimerId,
                RequestedPostPaddingSeconds = dto.RequestedPostPaddingSeconds,
                RequestedPrePaddingSeconds = dto.RequestedPrePaddingSeconds,
                RequiredPostPaddingSeconds = dto.RequiredPostPaddingSeconds,
                RequiredPrePaddingSeconds = dto.RequiredPrePaddingSeconds,
                ProgramId = dto.ExternalProgramId,
                Priority = dto.Priority
            };
        }

        public SeriesTimerInfo GetSeriesTimerInfo(SeriesTimerInfoDto dto)
        {
            return new SeriesTimerInfo
            {
                Id = dto.ExternalId,
                ChannelName = dto.ChannelName,
                Overview = dto.Overview,
                EndDate = dto.EndDate,
                Name = dto.Name,
                StartDate = dto.StartDate,
                ChannelId = dto.ExternalChannelId,
                RequestedPostPaddingSeconds = dto.RequestedPostPaddingSeconds,
                RequestedPrePaddingSeconds = dto.RequestedPrePaddingSeconds,
                RequiredPostPaddingSeconds = dto.RequiredPostPaddingSeconds,
                RequiredPrePaddingSeconds = dto.RequiredPrePaddingSeconds,
                Days = dto.Days,
                Priority = dto.Priority,
                ProgramId = dto.ExternalProgramId,
                RecordAnyChannel = dto.RecordAnyChannel,
                RecordAnyTime = dto.RecordAnyTime,
                RecordNewOnly = dto.RecordNewOnly
            };
        }
    }
}
