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
        private readonly IImageProcessor _imageProcessor;

        private readonly IUserManager _userManager;
        private readonly ILocalizationManager _localization;
        private readonly IUserDataManager _userDataManager;
        private readonly IDtoService _dtoService;

        private readonly List<ILiveTvService> _services = new List<ILiveTvService>();

        private List<Channel> _channels = new List<Channel>();
        private List<ProgramInfoDto> _programs = new List<ProgramInfoDto>();

        public LiveTvManager(IServerApplicationPaths appPaths, IFileSystem fileSystem, ILogger logger, IItemRepository itemRepo, IImageProcessor imageProcessor, IUserManager userManager, ILocalizationManager localization, IUserDataManager userDataManager, IDtoService dtoService)
        {
            _appPaths = appPaths;
            _fileSystem = fileSystem;
            _logger = logger;
            _itemRepo = itemRepo;
            _imageProcessor = imageProcessor;
            _userManager = userManager;
            _localization = localization;
            _userDataManager = userDataManager;
            _dtoService = dtoService;
        }

        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>The services.</value>
        public IReadOnlyList<ILiveTvService> Services
        {
            get { return _services; }
        }

        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="services">The services.</param>
        public void AddParts(IEnumerable<ILiveTvService> services)
        {
            _services.AddRange(services);
        }

        /// <summary>
        /// Gets the channel info dto.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="user">The user.</param>
        /// <returns>ChannelInfoDto.</returns>
        public ChannelInfoDto GetChannelInfoDto(Channel info, User user)
        {
            var dto = new ChannelInfoDto
            {
                Name = info.Name,
                ServiceName = info.ServiceName,
                ChannelType = info.ChannelType,
                Number = info.ChannelNumber,
                PrimaryImageTag = GetLogoImageTag(info),
                Type = info.GetType().Name,
                Id = info.Id.ToString("N"),
                MediaType = info.MediaType
            };

            if (user != null)
            {
                dto.UserData = _dtoService.GetUserItemDataDto(_userDataManager.GetUserData(user.Id, info.GetUserDataKey()));
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

        public QueryResult<ChannelInfoDto> GetChannels(ChannelQuery query)
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
            .Select(i => GetChannelInfoDto(i, user))
            .ToArray();

            return new QueryResult<ChannelInfoDto>
            {
                Items = returnChannels,
                TotalRecordCount = returnChannels.Length
            };
        }

        public Channel GetChannel(string id)
        {
            var guid = new Guid(id);

            return _channels.FirstOrDefault(i => i.Id == guid);
        }

        public ChannelInfoDto GetChannelInfoDto(string id, string userId)
        {
            var channel = GetChannel(id);

            var user = string.IsNullOrEmpty(userId) ? null : _userManager.GetUserById(new Guid(userId));

            return channel == null ? null : GetChannelInfoDto(channel, user);
        }

        private ProgramInfoDto GetProgramInfoDto(ProgramInfo program, Channel channel)
        {
            var id = GetInternalProgramIdId(channel.ServiceName, program.Id).ToString("N");

            return new ProgramInfoDto
            {
                ChannelId = channel.Id.ToString("N"),
                Description = program.Description,
                EndDate = program.EndDate,
                Genres = program.Genres,
                ExternalId = program.Id,
                Id = id,
                Name = program.Name,
                ServiceName = channel.ServiceName,
                StartDate = program.StartDate,
                OfficialRating = program.OfficialRating,
                Quality = program.Quality,
                OriginalAirDate = program.OriginalAirDate,
                Audio = program.Audio,
                CommunityRating = program.CommunityRating,
                AspectRatio = program.AspectRatio
            };
        }

        private Guid GetInternalChannelId(string serviceName, string externalChannelId)
        {
            var name = serviceName + externalChannelId;

            return name.ToLower().GetMBId(typeof(Channel));
        }

        private Guid GetInternalProgramIdId(string serviceName, string externalProgramId)
        {
            var name = serviceName + externalProgramId;

            return name.ToLower().GetMD5();
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

            var id = GetInternalChannelId(serviceName, channelInfo.Id);

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
                    ServiceName = serviceName
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

            if (!string.IsNullOrEmpty(query.ServiceName))
            {
                programs = programs.Where(i => string.Equals(i.ServiceName, query.ServiceName, StringComparison.OrdinalIgnoreCase));
            }

            if (query.ChannelIdList.Length > 0)
            {
                var guids = query.ChannelIdList.Select(i => new Guid(i)).ToList();

                programs = programs.Where(i => guids.Contains(new Guid(i.ChannelId)));
            }

            var returnArray = programs.ToArray();

            var recordings = await GetRecordings(new RecordingQuery
            {


            }, cancellationToken).ConfigureAwait(false);

            foreach (var program in returnArray)
            {
                var recording = recordings.Items
                    .FirstOrDefault(i => string.Equals(i.ProgramId, program.Id));

                program.RecordingId = recording == null ? null : recording.Id;
                program.RecordingStatus = recording == null ? (RecordingStatus?)null : recording.Status;
            }

            return new QueryResult<ProgramInfoDto>
            {
                Items = returnArray,
                TotalRecordCount = returnArray.Length
            };
        }

        internal async Task RefreshChannels(IProgress<double> progress, CancellationToken cancellationToken)
        {
            // Avoid implicitly captured closure
            var currentCancellationToken = cancellationToken;

            var channelTasks = _services.Select(i => GetChannels(i, currentCancellationToken));

            progress.Report(10);

            var results = await Task.WhenAll(channelTasks).ConfigureAwait(false);

            var allChannels = results.SelectMany(i => i).ToList();

            var list = new List<Channel>();
            var programs = new List<ProgramInfoDto>();

            var numComplete = 0;

            foreach (var channelInfo in allChannels)
            {
                try
                {
                    var item = await GetChannel(channelInfo.Item2, channelInfo.Item1, cancellationToken).ConfigureAwait(false);

                    var service = _services.First(i => string.Equals(channelInfo.Item1, i.Name, StringComparison.OrdinalIgnoreCase));

                    var channelPrograms = await service.GetProgramsAsync(channelInfo.Item2.Id, cancellationToken).ConfigureAwait(false);

                    programs.AddRange(channelPrograms.Select(program => GetProgramInfoDto(program, item)));

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
                percent /= allChannels.Count;

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

        private async Task<IEnumerable<RecordingInfoDto>> GetRecordings(ILiveTvService service, CancellationToken cancellationToken)
        {
            var recordings = await service.GetRecordingsAsync(cancellationToken).ConfigureAwait(false);

            return recordings.Select(i => GetRecordingInfoDto(i, service));
        }

        private RecordingInfoDto GetRecordingInfoDto(RecordingInfo info, ILiveTvService service)
        {
            var id = service.Name + info.ChannelId + info.Id;
            id = id.GetMD5().ToString("N");

            var dto = new RecordingInfoDto
            {
                ChannelName = info.ChannelName,
                Description = info.Description,
                EndDate = info.EndDate,
                Name = info.Name,
                StartDate = info.StartDate,
                Id = id,
                ExternalId = info.Id,
                ChannelId = GetInternalChannelId(service.Name, info.ChannelId).ToString("N"),
                Status = info.Status
            };

            return dto;
        }

        public async Task<QueryResult<RecordingInfoDto>> GetRecordings(RecordingQuery query, CancellationToken cancellationToken)
        {
            var list = new List<RecordingInfoDto>();

            foreach (var service in GetServices(query.ServiceName, query.ChannelId))
            {
                var recordings = await GetRecordings(service, cancellationToken).ConfigureAwait(false);

                list.AddRange(recordings);
            }

            if (!string.IsNullOrEmpty(query.ChannelId))
            {
                list = list.Where(i => string.Equals(i.ChannelId, query.ChannelId))
                    .ToList();
            }

            var returnArray = list.ToArray();

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
    }
}
