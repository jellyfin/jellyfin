using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
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

        private readonly LiveTvDtoService _tvDtoService;

        private readonly List<ILiveTvService> _services = new List<ILiveTvService>();

        private Dictionary<Guid, LiveTvChannel> _channels = new Dictionary<Guid, LiveTvChannel>();
        private Dictionary<Guid, LiveTvProgram> _programs = new Dictionary<Guid, LiveTvProgram>();

        public LiveTvManager(IServerApplicationPaths appPaths, IFileSystem fileSystem, ILogger logger, IItemRepository itemRepo, IImageProcessor imageProcessor, IUserDataManager userDataManager, IDtoService dtoService, IUserManager userManager)
        {
            _appPaths = appPaths;
            _fileSystem = fileSystem;
            _logger = logger;
            _itemRepo = itemRepo;
            _userManager = userManager;

            _tvDtoService = new LiveTvDtoService(dtoService, userDataManager, imageProcessor, logger, _itemRepo);
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

            IEnumerable<LiveTvChannel> channels = _channels.Values;

            if (user != null)
            {
                // Avoid implicitly captured closure
                var currentUser = user;

                channels = channels
                    .Where(i => i.IsParentalAllowed(currentUser))
                    .OrderBy(i =>
                    {
                        double number = 0;

                        if (!string.IsNullOrEmpty(i.ChannelInfo.Number))
                        {
                            double.TryParse(i.ChannelInfo.Number, out number);
                        }

                        return number;

                    });
            }

            var returnChannels = channels.OrderBy(i =>
            {
                double number = 0;

                if (!string.IsNullOrEmpty(i.ChannelInfo.Number))
                {
                    double.TryParse(i.ChannelInfo.Number, out number);
                }

                return number;

            }).ThenBy(i => i.Name)
            .Select(i => _tvDtoService.GetChannelInfoDto(i, GetCurrentProgram(i.ChannelInfo.Id), user))
            .ToArray();

            var result = new QueryResult<ChannelInfoDto>
            {
                Items = returnChannels,
                TotalRecordCount = returnChannels.Length
            };

            return Task.FromResult(result);
        }

        public LiveTvChannel GetInternalChannel(string id)
        {
            return GetInternalChannel(new Guid(id));
        }

        private LiveTvChannel GetInternalChannel(Guid id)
        {
            LiveTvChannel channel = null;

            _channels.TryGetValue(id, out channel);
            return channel;
        }

        public LiveTvProgram GetInternalProgram(string id)
        {
            var guid = new Guid(id);

            LiveTvProgram obj = null;

            _programs.TryGetValue(guid, out obj);
            return obj;
        }

        public async Task<ILiveTvRecording> GetInternalRecording(string id, CancellationToken cancellationToken)
        {
            var service = ActiveService;

            var recordings = await service.GetRecordingsAsync(cancellationToken).ConfigureAwait(false);

            var recording = recordings.FirstOrDefault(i => _tvDtoService.GetInternalRecordingId(service.Name, i.Id) == new Guid(id));

            return await GetRecording(recording, service.Name, cancellationToken).ConfigureAwait(false);
        }

        public async Task<LiveStreamInfo> GetRecordingStream(string id, CancellationToken cancellationToken)
        {
            var service = ActiveService;

            var recordings = await service.GetRecordingsAsync(cancellationToken).ConfigureAwait(false);

            var recording = recordings.First(i => _tvDtoService.GetInternalRecordingId(service.Name, i.Id) == new Guid(id));

            return await service.GetRecordingStream(recording.Id, cancellationToken).ConfigureAwait(false);
        }

        public async Task<LiveStreamInfo> GetChannelStream(string id, CancellationToken cancellationToken)
        {
            var service = ActiveService;

            var channel = GetInternalChannel(id);

            return await service.GetChannelStream(channel.ChannelInfo.Id, cancellationToken).ConfigureAwait(false);
        }

        private async Task<LiveTvChannel> GetChannel(ChannelInfo channelInfo, string serviceName, CancellationToken cancellationToken)
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

            var id = _tvDtoService.GetInternalChannelId(serviceName, channelInfo.Id);

            var item = _itemRepo.RetrieveItem(id) as LiveTvChannel;

            if (item == null)
            {
                item = new LiveTvChannel
                {
                    Name = channelInfo.Name,
                    Id = id,
                    DateCreated = _fileSystem.GetCreationTimeUtc(fileInfo),
                    DateModified = _fileSystem.GetLastWriteTimeUtc(fileInfo),
                    Path = path
                };

                isNew = true;
            }

            item.ChannelInfo = channelInfo;
            item.ServiceName = serviceName;

            // Set this now so we don't cause additional file system access during provider executions
            item.ResetResolveArgs(fileInfo);

            await item.RefreshMetadata(cancellationToken, forceSave: isNew, resetResolveArgs: false);

            return item;
        }

        private async Task<LiveTvProgram> GetProgram(ProgramInfo info, ChannelType channelType, string serviceName, CancellationToken cancellationToken)
        {
            var isNew = false;

            var id = _tvDtoService.GetInternalProgramId(serviceName, info.Id);

            var item = _itemRepo.RetrieveItem(id) as LiveTvProgram;

            if (item == null)
            {
                item = new LiveTvProgram
                {
                    Name = info.Name,
                    Id = id,
                    DateCreated = DateTime.UtcNow,
                    DateModified = DateTime.UtcNow
                };

                isNew = true;
            }

            item.ChannelType = channelType;
            item.ProgramInfo = info;
            item.ServiceName = serviceName;

            await item.RefreshMetadata(cancellationToken, forceSave: isNew, resetResolveArgs: false);

            return item;
        }

        private async Task<ILiveTvRecording> GetRecording(RecordingInfo info, string serviceName, CancellationToken cancellationToken)
        {
            var isNew = false;

            var id = _tvDtoService.GetInternalRecordingId(serviceName, info.Id);

            var item = _itemRepo.RetrieveItem(id) as ILiveTvRecording;

            if (item == null)
            {
                if (info.ChannelType == ChannelType.TV)
                {
                    item = new LiveTvVideoRecording
                    {
                        Name = info.Name,
                        Id = id,
                        DateCreated = DateTime.UtcNow,
                        DateModified = DateTime.UtcNow,
                        VideoType = VideoType.VideoFile
                    };
                }
                else
                {
                    item = new LiveTvAudioRecording
                    {
                        Name = info.Name,
                        Id = id,
                        DateCreated = DateTime.UtcNow,
                        DateModified = DateTime.UtcNow
                    };
                }

                if (!string.IsNullOrEmpty(info.Path))
                {
                    item.Path = info.Path;
                }
                else if (!string.IsNullOrEmpty(info.Url))
                {
                    item.Path = info.Url;
                }

                isNew = true;
            }

            item.RecordingInfo = info;
            item.ServiceName = serviceName;

            await item.RefreshMetadata(cancellationToken, forceSave: isNew, resetResolveArgs: false);

            return item;
        }

        private LiveTvChannel GetChannel(LiveTvProgram program)
        {
            var programChannelId = program.ProgramInfo.ChannelId;

            var internalProgramChannelId = _tvDtoService.GetInternalChannelId(program.ServiceName, programChannelId);

            return GetInternalChannel(internalProgramChannelId);
        }

        public async Task<ProgramInfoDto> GetProgram(string id, CancellationToken cancellationToken, User user = null)
        {
            var program = GetInternalProgram(id);

            var channel = GetChannel(program);

            var channelName = channel == null ? null : channel.ChannelInfo.Name;

            var dto = _tvDtoService.GetProgramInfoDto(program, channelName, user);

            await AddRecordingInfo(new[] { dto }, cancellationToken).ConfigureAwait(false);

            return dto;
        }

        public async Task<QueryResult<ProgramInfoDto>> GetPrograms(ProgramQuery query, CancellationToken cancellationToken)
        {
            IEnumerable<LiveTvProgram> programs = _programs.Values;

            if (query.MinEndDate.HasValue)
            {
                var val = query.MinEndDate.Value;

                programs = programs.Where(i => i.ProgramInfo.EndDate >= val);
            }

            if (query.MinStartDate.HasValue)
            {
                var val = query.MinStartDate.Value;

                programs = programs.Where(i => i.ProgramInfo.StartDate >= val);
            }

            if (query.MaxEndDate.HasValue)
            {
                var val = query.MaxEndDate.Value;

                programs = programs.Where(i => i.ProgramInfo.EndDate <= val);
            }

            if (query.MaxStartDate.HasValue)
            {
                var val = query.MaxStartDate.Value;

                programs = programs.Where(i => i.ProgramInfo.StartDate <= val);
            }

            if (query.ChannelIdList.Length > 0)
            {
                var guids = query.ChannelIdList.Select(i => new Guid(i)).ToList();
                var serviceName = ActiveService.Name;

                programs = programs.Where(i =>
                {
                    var programChannelId = i.ProgramInfo.ChannelId;

                    var internalProgramChannelId = _tvDtoService.GetInternalChannelId(serviceName, programChannelId);

                    return guids.Contains(internalProgramChannelId);
                });
            }

            var user = string.IsNullOrEmpty(query.UserId) ? null : _userManager.GetUserById(new Guid(query.UserId));

            if (user != null)
            {
                // Avoid implicitly captured closure
                var currentUser = user;
                programs = programs.Where(i => i.IsParentalAllowed(currentUser));
            }

            var returnArray = programs
                .OrderBy(i => i.ProgramInfo.StartDate)
                .Select(i =>
                {
                    var channel = GetChannel(i);

                    var channelName = channel == null ? null : channel.ChannelInfo.Name;

                    return _tvDtoService.GetProgramInfoDto(i, channelName, user);
                })
                .ToArray();

            await AddRecordingInfo(returnArray, cancellationToken).ConfigureAwait(false);

            var result = new QueryResult<ProgramInfoDto>
            {
                Items = returnArray,
                TotalRecordCount = returnArray.Length
            };

            return result;
        }

        private async Task AddRecordingInfo(IEnumerable<ProgramInfoDto> programs, CancellationToken cancellationToken)
        {
            var timers = await ActiveService.GetTimersAsync(cancellationToken).ConfigureAwait(false);
            var timerList = timers.ToList();

            foreach (var program in programs)
            {
                var timer = timerList.FirstOrDefault(i => string.Equals(i.ProgramId, program.ExternalId, StringComparison.OrdinalIgnoreCase));

                if (timer != null)
                {
                    program.TimerId = _tvDtoService.GetInternalTimerId(program.ServiceName, timer.Id)
                        .ToString("N");

                    if (!string.IsNullOrEmpty(timer.SeriesTimerId))
                    {
                        program.SeriesTimerId = _tvDtoService.GetInternalSeriesTimerId(program.ServiceName, timer.SeriesTimerId)
                            .ToString("N");
                    }
                }
            }

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

            var list = new List<LiveTvChannel>();

            var numComplete = 0;

            foreach (var channelInfo in allChannelsList)
            {
                try
                {
                    var item = await GetChannel(channelInfo.Item2, channelInfo.Item1, cancellationToken).ConfigureAwait(false);

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

                progress.Report(5 * percent + 10);
            }
            _channels = list.ToDictionary(i => i.Id);
            progress.Report(15);

             numComplete = 0;
             var programs = new List<LiveTvProgram>();

            foreach (var item in list)
            {
                // Avoid implicitly captured closure
                var currentChannel = item;

                try
                {
                    var channelPrograms = await service.GetProgramsAsync(currentChannel.ChannelInfo.Id, cancellationToken).ConfigureAwait(false);

                    var programTasks = channelPrograms.Select(program => GetProgram(program, currentChannel.ChannelInfo.ChannelType, service.Name, cancellationToken));
                    var programEntities = await Task.WhenAll(programTasks).ConfigureAwait(false);

                    programs.AddRange(programEntities);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error getting programs for channel {0}", ex, currentChannel.Name);
                }

                numComplete++;
                double percent = numComplete;
                percent /= allChannelsList.Count;

                progress.Report(90 * percent + 10);
            }

            _programs = programs.ToDictionary(i => i.Id);
        }

        private async Task<IEnumerable<Tuple<string, ChannelInfo>>> GetChannels(ILiveTvService service, CancellationToken cancellationToken)
        {
            var channels = await service.GetChannelsAsync(cancellationToken).ConfigureAwait(false);

            return channels.Select(i => new Tuple<string, ChannelInfo>(service.Name, i));
        }

        public async Task<QueryResult<RecordingInfoDto>> GetRecordings(RecordingQuery query, CancellationToken cancellationToken)
        {
            var service = ActiveService;

            var user = string.IsNullOrEmpty(query.UserId) ? null : _userManager.GetUserById(new Guid(query.UserId));

            var recordings = await service.GetRecordingsAsync(cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(query.ChannelId))
            {
                var guid = new Guid(query.ChannelId);

                var currentServiceName = service.Name;

                recordings = recordings
                    .Where(i => _tvDtoService.GetInternalChannelId(currentServiceName, i.ChannelId) == guid);
            }

            if (!string.IsNullOrEmpty(query.Id))
            {
                var guid = new Guid(query.Id);

                var currentServiceName = service.Name;

                recordings = recordings
                    .Where(i => _tvDtoService.GetInternalRecordingId(currentServiceName, i.Id) == guid);
            }

            if (!string.IsNullOrEmpty(query.GroupId))
            {
                var guid = new Guid(query.GroupId);

                recordings = recordings.Where(i => GetRecordingGroupIds(i).Contains(guid));
            }

            if (query.IsRecording.HasValue)
            {
                var val = query.IsRecording.Value;
                recordings = recordings.Where(i => (i.Status == RecordingStatus.InProgress) == val);
            }

            IEnumerable<ILiveTvRecording> entities = await GetEntities(recordings, service.Name, cancellationToken).ConfigureAwait(false);

            entities = entities.OrderByDescending(i => i.RecordingInfo.StartDate);

            if (user != null)
            {
                var currentUser = user;
                entities = entities.Where(i => i.IsParentalAllowed(currentUser));
            }

            if (query.StartIndex.HasValue)
            {
                entities = entities.Skip(query.StartIndex.Value);
            }

            if (query.Limit.HasValue)
            {
                entities = entities.Take(query.Limit.Value);
            }

            var returnArray = entities
                .Select(i =>
                {
                    var channel = string.IsNullOrEmpty(i.RecordingInfo.ChannelId) ? null : GetInternalChannel(_tvDtoService.GetInternalChannelId(service.Name, i.RecordingInfo.ChannelId));
                    return _tvDtoService.GetRecordingInfoDto(i, channel, service, user);
                })
                .ToArray();

            return new QueryResult<RecordingInfoDto>
            {
                Items = returnArray,
                TotalRecordCount = returnArray.Length
            };
        }

        private Task<ILiveTvRecording[]> GetEntities(IEnumerable<RecordingInfo> recordings, string serviceName, CancellationToken cancellationToken)
        {
            var tasks = recordings.Select(i => GetRecording(i, serviceName, cancellationToken));

            return Task.WhenAll(tasks);
        }

        private IEnumerable<ILiveTvService> GetServices(string serviceName, string channelId)
        {
            IEnumerable<ILiveTvService> services = _services;

            if (string.IsNullOrEmpty(serviceName) && !string.IsNullOrEmpty(channelId))
            {
                var channel = GetInternalChannel(channelId);

                if (channel != null)
                {
                    serviceName = channel.ServiceName;
                }
            }

            if (!string.IsNullOrEmpty(serviceName))
            {
                services = services.Where(i => string.Equals(i.Name, serviceName, StringComparison.OrdinalIgnoreCase));
            }

            return services;
        }

        public async Task<QueryResult<TimerInfoDto>> GetTimers(TimerQuery query, CancellationToken cancellationToken)
        {
            var service = ActiveService;
            var timers = await service.GetTimersAsync(cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(query.ChannelId))
            {
                var guid = new Guid(query.ChannelId);
                timers = timers.Where(i => guid == _tvDtoService.GetInternalChannelId(service.Name, i.ChannelId));
            }

            var returnArray = timers
                .Select(i =>
                {
                    var program = string.IsNullOrEmpty(i.ProgramId) ? null : GetInternalProgram(_tvDtoService.GetInternalProgramId(service.Name, i.ProgramId).ToString("N"));
                    var channel = string.IsNullOrEmpty(i.ChannelId) ? null : GetInternalChannel(_tvDtoService.GetInternalChannelId(service.Name, i.ChannelId));

                    return _tvDtoService.GetTimerInfoDto(i, service, program, channel);
                })
                .OrderBy(i => i.StartDate)
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
                UserId = user == null ? null : user.Id.ToString("N"),
                Id = id

            }, cancellationToken).ConfigureAwait(false);

            return results.Items.FirstOrDefault();
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

        public async Task<QueryResult<SeriesTimerInfoDto>> GetSeriesTimers(SeriesTimerQuery query, CancellationToken cancellationToken)
        {
            var service = ActiveService;

            var timers = await service.GetSeriesTimersAsync(cancellationToken).ConfigureAwait(false);

            var returnArray = timers
                .Select(i =>
                {
                    string channelName = null;

                    if (!string.IsNullOrEmpty(i.ChannelId))
                    {
                        var internalChannelId = _tvDtoService.GetInternalChannelId(service.Name, i.ChannelId);
                        var channel = GetInternalChannel(internalChannelId);
                        channelName = channel == null ? null : channel.ChannelInfo.Name;
                    }

                    return _tvDtoService.GetSeriesTimerInfoDto(i, service, channelName);

                })
                .OrderByDescending(i => i.StartDate)
                .ToArray();

            return new QueryResult<SeriesTimerInfoDto>
            {
                Items = returnArray,
                TotalRecordCount = returnArray.Length
            };
        }

        public Task<ChannelInfoDto> GetChannel(string id, CancellationToken cancellationToken, User user = null)
        {
            var channel = GetInternalChannel(id);

            var dto = _tvDtoService.GetChannelInfoDto(channel, GetCurrentProgram(channel.ChannelInfo.Id), user);

            return Task.FromResult(dto);
        }

        private LiveTvProgram GetCurrentProgram(string externalChannelId)
        {
            var now = DateTime.UtcNow;

            return _programs.Values
                .Where(i => string.Equals(externalChannelId, i.ProgramInfo.ChannelId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(i => i.ProgramInfo.StartDate)
                .SkipWhile(i => now >= i.ProgramInfo.EndDate)
                .FirstOrDefault();
        }

        public async Task<SeriesTimerInfoDto> GetNewTimerDefaults(CancellationToken cancellationToken)
        {
            var service = ActiveService;

            var info = await service.GetNewTimerDefaultsAsync(cancellationToken).ConfigureAwait(false);

            var obj = _tvDtoService.GetSeriesTimerInfoDto(info, service, null);

            obj.Id = obj.ExternalId = string.Empty;

            return obj;
        }

        public async Task<SeriesTimerInfoDto> GetNewTimerDefaults(string programId, CancellationToken cancellationToken)
        {
            var info = await GetNewTimerDefaults(cancellationToken).ConfigureAwait(false);

            var program = await GetProgram(programId, cancellationToken).ConfigureAwait(false);

            info.Days = new List<DayOfWeek>
            {
                program.StartDate.ToLocalTime().DayOfWeek
            };

            info.DayPattern = _tvDtoService.GetDayPattern(info.Days);

            info.Name = program.Name;
            info.ChannelId = program.ChannelId;
            info.ChannelName = program.ChannelName;
            info.EndDate = program.EndDate;
            info.StartDate = program.StartDate;
            info.Name = program.Name;
            info.Overview = program.Overview;
            info.ProgramId = program.Id;
            info.ExternalProgramId = program.ExternalId;

            return info;
        }

        public async Task CreateTimer(TimerInfoDto timer, CancellationToken cancellationToken)
        {
            var service = string.IsNullOrEmpty(timer.ServiceName) ? ActiveService : GetServices(timer.ServiceName, null).First();

            var info = await _tvDtoService.GetTimerInfo(timer, true, this, cancellationToken).ConfigureAwait(false);

            // Set priority from default values
            var defaultValues = await service.GetNewTimerDefaultsAsync(cancellationToken).ConfigureAwait(false);
            info.Priority = defaultValues.Priority;

            await service.CreateTimerAsync(info, cancellationToken).ConfigureAwait(false);
        }

        public async Task CreateSeriesTimer(SeriesTimerInfoDto timer, CancellationToken cancellationToken)
        {
            var service = string.IsNullOrEmpty(timer.ServiceName) ? ActiveService : GetServices(timer.ServiceName, null).First();

            var info = await _tvDtoService.GetSeriesTimerInfo(timer, true, this, cancellationToken).ConfigureAwait(false);

            // Set priority from default values
            var defaultValues = await service.GetNewTimerDefaultsAsync(cancellationToken).ConfigureAwait(false);
            info.Priority = defaultValues.Priority;

            await service.CreateSeriesTimerAsync(info, cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateTimer(TimerInfoDto timer, CancellationToken cancellationToken)
        {
            var info = await _tvDtoService.GetTimerInfo(timer, false, this, cancellationToken).ConfigureAwait(false);

            var service = string.IsNullOrEmpty(timer.ServiceName) ? ActiveService : GetServices(timer.ServiceName, null).First();

            await service.UpdateTimerAsync(info, cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateSeriesTimer(SeriesTimerInfoDto timer, CancellationToken cancellationToken)
        {
            var info = await _tvDtoService.GetSeriesTimerInfo(timer, false, this, cancellationToken).ConfigureAwait(false);

            var service = string.IsNullOrEmpty(timer.ServiceName) ? ActiveService : GetServices(timer.ServiceName, null).First();

            await service.UpdateSeriesTimerAsync(info, cancellationToken).ConfigureAwait(false);
        }

        private List<string> GetRecordingGroupNames(RecordingInfo recording)
        {
            var list = new List<string>();

            if (recording.IsSeries)
            {
                list.Add(recording.Name);
            }

            if (recording.IsKids)
            {
                list.Add("Kids");
            }

            if (recording.IsMovie)
            {
                list.Add("Movies");
            }

            if (recording.IsNews)
            {
                list.Add("News");
            }

            if (recording.IsSports)
            {
                list.Add("Sports");
            }

            if (!recording.IsSports && !recording.IsNews && !recording.IsMovie && !recording.IsKids && !recording.IsSeries)
            {
                list.Add("Others");
            }

            return list;
        }

        private List<Guid> GetRecordingGroupIds(RecordingInfo recording)
        {
            return GetRecordingGroupNames(recording).Select(i => i.ToLower()
                .GetMD5())
                .ToList();
        }

        public async Task<QueryResult<RecordingGroupDto>> GetRecordingGroups(RecordingGroupQuery query, CancellationToken cancellationToken)
        {
            var recordingResult = await GetRecordings(new RecordingQuery
            {
                UserId = query.UserId

            }, cancellationToken).ConfigureAwait(false);

            var recordings = recordingResult.Items;

            var groups = new List<RecordingGroupDto>();

            var series = recordings
                .Where(i => i.IsSeries)
                .ToLookup(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            groups.AddRange(series.OrderBy(i => i.Key).Select(i => new RecordingGroupDto
            {
                Name = i.Key,
                RecordingCount = i.Count()
            }));

            groups.Add(new RecordingGroupDto
            {
                Name = "Kids",
                RecordingCount = recordings.Count(i => i.IsKids)
            });

            groups.Add(new RecordingGroupDto
            {
                Name = "Movies",
                RecordingCount = recordings.Count(i => i.IsMovie)
            });

            groups.Add(new RecordingGroupDto
            {
                Name = "News",
                RecordingCount = recordings.Count(i => i.IsNews)
            });

            groups.Add(new RecordingGroupDto
            {
                Name = "Sports",
                RecordingCount = recordings.Count(i => i.IsSports)
            });

            groups.Add(new RecordingGroupDto
            {
                Name = "Others",
                RecordingCount = recordings.Count(i => !i.IsSports && !i.IsNews && !i.IsMovie && !i.IsKids && !i.IsSeries)
            });

            groups = groups
                .Where(i => i.RecordingCount > 0)
                .ToList();

            foreach (var group in groups)
            {
                group.Id = group.Name.ToLower().GetMD5().ToString("N");
            }

            return new QueryResult<RecordingGroupDto>
            {
                Items = groups.ToArray(),
                TotalRecordCount = groups.Count
            };
        }

        public Task CloseLiveStream(string id, CancellationToken cancellationToken)
        {
            return ActiveService.CloseLiveStream(id, cancellationToken);
        }

        public GuideInfo GetGuideInfo()
        {
            var programs = _programs.ToList();

            var startDate = programs.Select(i => i.Value.ProgramInfo.StartDate).Min();
            var endDate = programs.Select(i => i.Value.ProgramInfo.StartDate).Max();

            return new GuideInfo
            {
                StartDate = startDate,
                EndDate = endDate
            };
        }
    }
}
