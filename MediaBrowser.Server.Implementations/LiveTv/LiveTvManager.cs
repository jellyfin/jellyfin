using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.LiveTv
{
    /// <summary>
    /// Class LiveTvManager
    /// </summary>
    public class LiveTvManager : ILiveTvManager
    {
        private readonly IServerApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly IItemRepository _itemRepo;
        private readonly IUserManager _userManager;

        private readonly ILocalizationManager _localization;
        private readonly LiveTvDtoService _tvDtoService;

        private readonly List<ILiveTvService> _services = new List<ILiveTvService>();

        private List<Channel> _channels = new List<Channel>();
        private List<ProgramInfoDto> _programs = new List<ProgramInfoDto>();

        public LiveTvManager(IServerApplicationPaths appPaths, IFileSystem fileSystem, ILogger logger, IItemRepository itemRepo, IImageProcessor imageProcessor, ILocalizationManager localization, IUserDataManager userDataManager, IDtoService dtoService, IUserManager userManager)
        {
            _appPaths = appPaths;
            _fileSystem = fileSystem;
            _logger = logger;
            _itemRepo = itemRepo;
            _localization = localization;
            _userManager = userManager;

            _tvDtoService = new LiveTvDtoService(dtoService, userDataManager, imageProcessor, logger);
        }

        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>The services.</value>
        public IReadOnlyList<ILiveTvService> Services
        {
            get { return _services; }
        }

        public ILiveTvService ActiveService { get; private set; }

        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="services">The services.</param>
        public void AddParts(IEnumerable<ILiveTvService> services)
        {
            _services.AddRange(services);

            ActiveService = _services.FirstOrDefault();
        }

        public Task<QueryResult<ChannelInfoDto>> GetChannels(ChannelQuery query, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrEmpty(query.UserId) ? null : _userManager.GetUserById(new Guid(query.UserId));

            IEnumerable<Channel> channels = _channels;

            if (user != null)
            {
                channels = channels.Where(i => i.IsParentalAllowed(user, _localization))
                    .OrderBy(i =>
                    {
                        double number = 0;

                        if (!string.IsNullOrEmpty(i.ChannelNumber))
                        {
                            double.TryParse(i.ChannelNumber, out number);
                        }

                        return number;

                    });
            }

            var returnChannels = channels.OrderBy(i =>
            {
                double number = 0;

                if (!string.IsNullOrEmpty(i.ChannelNumber))
                {
                    double.TryParse(i.ChannelNumber, out number);
                }

                return number;

            }).ThenBy(i => i.Name)
            .Select(i => _tvDtoService.GetChannelInfoDto(i, user))
            .ToArray();

            var result = new QueryResult<ChannelInfoDto>
            {
                Items = returnChannels,
                TotalRecordCount = returnChannels.Length
            };

            return Task.FromResult(result);
        }

        public Channel GetChannel(string id)
        {
            var guid = new Guid(id);

            return _channels.FirstOrDefault(i => i.Id == guid);
        }

        private async Task<Channel> GetChannel(ChannelInfo channelInfo, string serviceName, CancellationToken cancellationToken)
        {
            var path = Path.Combine(_appPaths.ItemsByNamePath, "channels", _fileSystem.GetValidFilename(serviceName), _fileSystem.GetValidFilename(channelInfo.Name));

            var fileInfo = new DirectoryInfo(path);

            var isNew = false;

            if (!fileInfo.Exists)
            {
                Directory.CreateDirectory(path);
                fileInfo = new DirectoryInfo(path);

                if (!fileInfo.Exists)
                {
                    throw new IOException("Path not created: " + path);
                }

                isNew = true;
            }

            var id = _tvDtoService.GetInternalChannelId(serviceName, channelInfo.Id, channelInfo.Name);

            var item = _itemRepo.RetrieveItem(id) as Channel;

            if (item == null)
            {
                item = new Channel
                {
                    Name = channelInfo.Name,
                    Id = id,
                    DateCreated = _fileSystem.GetCreationTimeUtc(fileInfo),
                    DateModified = _fileSystem.GetLastWriteTimeUtc(fileInfo),
                    Path = path,
                    ChannelId = channelInfo.Id,
                    ChannelNumber = channelInfo.Number,
                    ServiceName = serviceName,
                    HasProviderImage = channelInfo.HasImage
                };

                isNew = true;
            }

            // Set this now so we don't cause additional file system access during provider executions
            item.ResetResolveArgs(fileInfo);

            await item.RefreshMetadata(cancellationToken, forceSave: isNew, resetResolveArgs: false);

            return item;
        }

        public async Task<QueryResult<ProgramInfoDto>> GetPrograms(ProgramQuery query, CancellationToken cancellationToken)
        {
            IEnumerable<ProgramInfoDto> programs = _programs
                .OrderBy(i => i.StartDate)
                .ThenBy(i => i.EndDate);

            if (query.ChannelIdList.Length > 0)
            {
                var guids = query.ChannelIdList.Select(i => new Guid(i)).ToList();

                programs = programs.Where(i => guids.Contains(new Guid(i.ChannelId)));
            }

            var returnArray = programs.ToArray();

            return new QueryResult<ProgramInfoDto>
            {
                Items = returnArray,
                TotalRecordCount = returnArray.Length
            };
        }

        internal async Task RefreshChannels(IProgress<double> progress, CancellationToken cancellationToken)
        {
            // Avoid implicitly captured closure
            var service = ActiveService;

            if (service == null)
            {
                progress.Report(100);
                return;
            }

            progress.Report(10);

            var allChannels = await GetChannels(service, cancellationToken).ConfigureAwait(false);
            var allChannelsList = allChannels.ToList();

            var list = new List<Channel>();
            var programs = new List<ProgramInfoDto>();

            var numComplete = 0;

            foreach (var channelInfo in allChannelsList)
            {
                try
                {
                    var item = await GetChannel(channelInfo.Item2, channelInfo.Item1, cancellationToken).ConfigureAwait(false);

                    var channelPrograms = await service.GetProgramsAsync(channelInfo.Item2.Id, cancellationToken).ConfigureAwait(false);

                    programs.AddRange(channelPrograms.Select(program => _tvDtoService.GetProgramInfoDto(program, item)));

                    list.Add(item);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error getting channel information for {0}", ex, channelInfo.Item2.Name);
                }

                numComplete++;
                double percent = numComplete;
                percent /= allChannelsList.Count;

                progress.Report(90 * percent + 10);
            }

            _programs = programs;
            _channels = list;
        }

        private async Task<IEnumerable<Tuple<string, ChannelInfo>>> GetChannels(ILiveTvService service, CancellationToken cancellationToken)
        {
            var channels = await service.GetChannelsAsync(cancellationToken).ConfigureAwait(false);

            return channels.Select(i => new Tuple<string, ChannelInfo>(service.Name, i));
        }

        public async Task<QueryResult<RecordingInfoDto>> GetRecordings(RecordingQuery query, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrEmpty(query.UserId) ? null : _userManager.GetUserById(new Guid(query.UserId));

            var list = new List<RecordingInfoDto>();

            if (ActiveService != null)
            {
                var recordings = await ActiveService.GetRecordingsAsync(cancellationToken).ConfigureAwait(false);

                var dtos = recordings.Select(i => _tvDtoService.GetRecordingInfoDto(i, ActiveService, user));

                list.AddRange(dtos);
            }

            if (!string.IsNullOrEmpty(query.ChannelId))
            {
                list = list.Where(i => string.Equals(i.ChannelId, query.ChannelId))
                    .ToList();
            }

            var returnArray = list.OrderByDescending(i => i.StartDate)
                .ToArray();

            return new QueryResult<RecordingInfoDto>
            {
                Items = returnArray,
                TotalRecordCount = returnArray.Length
            };
        }

        private IEnumerable<ILiveTvService> GetServices(string serviceName, string channelId)
        {
            IEnumerable<ILiveTvService> services = _services;

            if (string.IsNullOrEmpty(serviceName) && !string.IsNullOrEmpty(channelId))
            {
                var channelIdGuid = new Guid(channelId);

                serviceName = _channels.Where(i => i.Id == channelIdGuid)
                    .Select(i => i.ServiceName)
                    .FirstOrDefault();
            }

            if (!string.IsNullOrEmpty(serviceName))
            {
                services = services.Where(i => string.Equals(i.Name, serviceName, StringComparison.OrdinalIgnoreCase));
            }

            return services;
        }

        public Task ScheduleRecording(string programId)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryResult<TimerInfoDto>> GetTimers(TimerQuery query, CancellationToken cancellationToken)
        {
            var list = new List<TimerInfoDto>();

            if (ActiveService != null)
            {
                var timers = await ActiveService.GetTimersAsync(cancellationToken).ConfigureAwait(false);

                var dtos = timers.Select(i => _tvDtoService.GetTimerInfoDto(i, ActiveService));

                list.AddRange(dtos);
            }

            if (!string.IsNullOrEmpty(query.ChannelId))
            {
                list = list.Where(i => string.Equals(i.ChannelId, query.ChannelId))
                    .ToList();
            }

            var returnArray = list.OrderBy(i => i.StartDate)
                .ToArray();

            return new QueryResult<TimerInfoDto>
            {
                Items = returnArray,
                TotalRecordCount = returnArray.Length
            };
        }

        public async Task DeleteRecording(string recordingId)
        {
            var recording = await GetRecording(recordingId, CancellationToken.None).ConfigureAwait(false);

            if (recording == null)
            {
                throw new ResourceNotFoundException(string.Format("Recording with Id {0} not found", recordingId));
            }

            var service = GetServices(recording.ServiceName, null)
                .First();

            await service.DeleteRecordingAsync(recording.ExternalId, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task CancelTimer(string id)
        {
            var timer = await GetTimer(id, CancellationToken.None).ConfigureAwait(false);

            if (timer == null)
            {
                throw new ResourceNotFoundException(string.Format("Timer with Id {0} not found", id));
            }

            var service = GetServices(timer.ServiceName, null)
                .First();

            await service.CancelTimerAsync(timer.ExternalId, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task CancelSeriesTimer(string id)
        {
            var timer = await GetSeriesTimer(id, CancellationToken.None).ConfigureAwait(false);

            if (timer == null)
            {
                throw new ResourceNotFoundException(string.Format("Timer with Id {0} not found", id));
            }

            var service = GetServices(timer.ServiceName, null)
                .First();

            await service.CancelSeriesTimerAsync(timer.ExternalId, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<RecordingInfoDto> GetRecording(string id, CancellationToken cancellationToken, User user = null)
        {
            var results = await GetRecordings(new RecordingQuery
            {
                UserId = user == null ? null : user.Id.ToString("N")

            }, cancellationToken).ConfigureAwait(false);

            return results.Items.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.CurrentCulture));
        }

        public async Task<TimerInfoDto> GetTimer(string id, CancellationToken cancellationToken)
        {
            var results = await GetTimers(new TimerQuery(), cancellationToken).ConfigureAwait(false);

            return results.Items.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.CurrentCulture));
        }

        public async Task<SeriesTimerInfoDto> GetSeriesTimer(string id, CancellationToken cancellationToken)
        {
            var results = await GetSeriesTimers(new SeriesTimerQuery(), cancellationToken).ConfigureAwait(false);

            return results.Items.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.CurrentCulture));
        }

        public Task UpdateTimer(TimerInfoDto timer, CancellationToken cancellationToken)
        {
            var info = _tvDtoService.GetTimerInfo(timer);

            var service = GetServices(timer.ServiceName, null)
              .First();

            return service.UpdateTimerAsync(info, cancellationToken);
        }

        public Task UpdateSeriesTimer(SeriesTimerInfoDto timer, CancellationToken cancellationToken)
        {
            var info = _tvDtoService.GetSeriesTimerInfo(timer);

            var service = GetServices(timer.ServiceName, null)
                .First();

            return service.UpdateSeriesTimerAsync(info, cancellationToken);
        }

        public async Task<QueryResult<SeriesTimerInfoDto>> GetSeriesTimers(SeriesTimerQuery query, CancellationToken cancellationToken)
        {
            var list = new List<SeriesTimerInfoDto>();

            if (ActiveService != null)
            {
                var timers = await ActiveService.GetSeriesTimersAsync(cancellationToken).ConfigureAwait(false);

                var dtos = timers.Select(i => _tvDtoService.GetSeriesTimerInfoDto(i, ActiveService));

                list.AddRange(dtos);
            }

            var returnArray = list.OrderByDescending(i => i.StartDate)
                .ToArray();

            return new QueryResult<SeriesTimerInfoDto>
            {
                Items = returnArray,
                TotalRecordCount = returnArray.Length
            };
        }

        public async Task<ChannelInfoDto> GetChannel(string id, CancellationToken cancellationToken, User user = null)
        {
            var results = await GetChannels(new ChannelQuery
            {
                UserId = user == null ? null : user.Id.ToString("N")

            }, cancellationToken).ConfigureAwait(false);

            return results.Items.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.CurrentCulture));
        }
    }
}
