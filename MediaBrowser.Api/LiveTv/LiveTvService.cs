using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Api.UserLibrary;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace MediaBrowser.Api.LiveTv
{
    [Route("/LiveTv/LiveStreamFiles/{Id}/stream.{Container}", "GET", Summary = "Gets a live tv channel")]
    public class GetLiveStreamFile
    {
        public string Id { get; set; }
        public string Container { get; set; }
    }

    [Route("/LiveTv/LiveRecordings/{Id}/stream", "GET", Summary = "Gets a live tv channel")]
    public class GetLiveRecordingFile
    {
        public string Id { get; set; }
    }

    public class LiveTvService : BaseApiService
    {
        private readonly ILiveTvManager _liveTvManager;
        private readonly IUserManager _userManager;
        private readonly IHttpClient _httpClient;
        private readonly ILibraryManager _libraryManager;
        private readonly IDtoService _dtoService;
        private readonly IAuthorizationContext _authContext;
        private readonly ISessionContext _sessionContext;
        private readonly IStreamHelper _streamHelper;
        private readonly IMediaSourceManager _mediaSourceManager;

        public LiveTvService(
            ILogger<LiveTvService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IMediaSourceManager mediaSourceManager,
            IStreamHelper streamHelper,
            ILiveTvManager liveTvManager,
            IUserManager userManager,
            IHttpClient httpClient,
            ILibraryManager libraryManager,
            IDtoService dtoService,
            IAuthorizationContext authContext,
            ISessionContext sessionContext)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _mediaSourceManager = mediaSourceManager;
            _streamHelper = streamHelper;
            _liveTvManager = liveTvManager;
            _userManager = userManager;
            _httpClient = httpClient;
            _libraryManager = libraryManager;
            _dtoService = dtoService;
            _authContext = authContext;
            _sessionContext = sessionContext;
        }

        public object Get(GetTunerHostTypes request)
        {
            var list = _liveTvManager.GetTunerHostTypes();
            return ToOptimizedResult(list);
        }

        public object Get(GetLiveRecordingFile request)
        {
            var path = _liveTvManager.GetEmbyTvActiveRecordingPath(request.Id);

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new FileNotFoundException();
            }

            var outputHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [HeaderNames.ContentType] = Model.Net.MimeTypes.GetMimeType(path)
            };

            return new ProgressiveFileCopier(_streamHelper, path, outputHeaders, Logger)
            {
                AllowEndOfFile = false
            };
        }

        public async Task<object> Get(DiscoverTuners request)
        {
            var result = await _liveTvManager.DiscoverTuners(request.NewDevicesOnly, CancellationToken.None).ConfigureAwait(false);
            return ToOptimizedResult(result);
        }

        public async Task<object> Get(GetLiveStreamFile request)
        {
            var liveStreamInfo = await _mediaSourceManager.GetDirectStreamProviderByUniqueId(request.Id, CancellationToken.None).ConfigureAwait(false);

            var directStreamProvider = liveStreamInfo;

            var outputHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [HeaderNames.ContentType] = Model.Net.MimeTypes.GetMimeType("file." + request.Container)
            };

            return new ProgressiveFileCopier(directStreamProvider, _streamHelper, outputHeaders, Logger)
            {
                AllowEndOfFile = false
            };
        }

        public object Get(GetDefaultListingProvider request)
        {
            return ToOptimizedResult(new ListingsProviderInfo());
        }

        public async Task<object> Post(SetChannelMapping request)
        {
            return await _liveTvManager.SetChannelMapping(request.ProviderId, request.TunerChannelId, request.ProviderChannelId).ConfigureAwait(false);
        }

        public async Task<object> Get(GetChannelMappingOptions request)
        {
            var config = GetConfiguration();

            var listingsProviderInfo = config.ListingProviders.First(i => string.Equals(request.ProviderId, i.Id, StringComparison.OrdinalIgnoreCase));

            var listingsProviderName = _liveTvManager.ListingProviders.First(i => string.Equals(i.Type, listingsProviderInfo.Type, StringComparison.OrdinalIgnoreCase)).Name;

            var tunerChannels = await _liveTvManager.GetChannelsForListingsProvider(request.ProviderId, CancellationToken.None)
                        .ConfigureAwait(false);

            var providerChannels = await _liveTvManager.GetChannelsFromListingsProviderData(request.ProviderId, CancellationToken.None)
                     .ConfigureAwait(false);

            var mappings = listingsProviderInfo.ChannelMappings;

            var result = new ChannelMappingOptions
            {
                TunerChannels = tunerChannels.Select(i => _liveTvManager.GetTunerChannelMapping(i, mappings, providerChannels)).ToList(),

                ProviderChannels = providerChannels.Select(i => new NameIdPair
                {
                    Name = i.Name,
                    Id = i.Id

                }).ToList(),

                Mappings = mappings,

                ProviderName = listingsProviderName
            };

            return ToOptimizedResult(result);
        }

        public async Task<object> Get(GetSchedulesDirectCountries request)
        {
            // https://json.schedulesdirect.org/20141201/available/countries

            var response = await _httpClient.Get(new HttpRequestOptions
            {
                Url = "https://json.schedulesdirect.org/20141201/available/countries",
                BufferContent = false

            }).ConfigureAwait(false);

            return ResultFactory.GetResult(Request, response, "application/json");
        }

        private void AssertUserCanManageLiveTv()
        {
            var user = _sessionContext.GetUser(Request);

            if (user == null)
            {
                throw new SecurityException("Anonymous live tv management is not allowed.");
            }

            if (!user.HasPermission(PermissionKind.EnableLiveTvManagement))
            {
                throw new SecurityException("The current user does not have permission to manage live tv.");
            }
        }

        public async Task<object> Post(AddListingProvider request)
        {
            if (request.Pw != null)
            {
                request.Password = GetHashedString(request.Pw);
            }

            request.Pw = null;

            var result = await _liveTvManager.SaveListingProvider(request, request.ValidateLogin, request.ValidateListings).ConfigureAwait(false);
            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the hashed string.
        /// </summary>
        private string GetHashedString(string str)
        {
            // SchedulesDirect requires a SHA1 hash of the user's password
            // https://github.com/SchedulesDirect/JSON-Service/wiki/API-20141201#obtain-a-token
            using SHA1 sha = SHA1.Create();

            return Hex.Encode(
                sha.ComputeHash(Encoding.UTF8.GetBytes(str)));
        }

        public void Delete(DeleteListingProvider request)
        {
            _liveTvManager.DeleteListingsProvider(request.Id);
        }

        public async Task<object> Post(AddTunerHost request)
        {
            var result = await _liveTvManager.SaveTunerHost(request).ConfigureAwait(false);
            return ToOptimizedResult(result);
        }

        public void Delete(DeleteTunerHost request)
        {
            var config = GetConfiguration();

            config.TunerHosts = config.TunerHosts.Where(i => !string.Equals(request.Id, i.Id, StringComparison.OrdinalIgnoreCase)).ToArray();

            ServerConfigurationManager.SaveConfiguration("livetv", config);
        }

        private LiveTvOptions GetConfiguration()
        {
            return ServerConfigurationManager.GetConfiguration<LiveTvOptions>("livetv");
        }

        private void UpdateConfiguration(LiveTvOptions options)
        {
            ServerConfigurationManager.SaveConfiguration("livetv", options);
        }

        public async Task<object> Get(GetLineups request)
        {
            var info = await _liveTvManager.GetLineups(request.Type, request.Id, request.Country, request.Location).ConfigureAwait(false);

            return ToOptimizedResult(info);
        }

        private void RemoveFields(DtoOptions options)
        {
            var fields = options.Fields.ToList();

            fields.Remove(ItemFields.CanDelete);
            fields.Remove(ItemFields.CanDownload);
            fields.Remove(ItemFields.DisplayPreferencesId);
            fields.Remove(ItemFields.Etag);
            options.Fields = fields.ToArray();
        }

        public async Task<object> Get(GetPrograms request)
        {
            var user = request.UserId.Equals(Guid.Empty) ? null : _userManager.GetUserById(request.UserId);

            var query = new InternalItemsQuery(user)
            {
                ChannelIds = ApiEntryPoint.Split(request.ChannelIds, ',', true).Select(i => new Guid(i)).ToArray(),
                HasAired = request.HasAired,
                IsAiring = request.IsAiring,
                EnableTotalRecordCount = request.EnableTotalRecordCount
            };

            if (!string.IsNullOrEmpty(request.MinStartDate))
            {
                query.MinStartDate = DateTime.Parse(request.MinStartDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();
            }

            if (!string.IsNullOrEmpty(request.MinEndDate))
            {
                query.MinEndDate = DateTime.Parse(request.MinEndDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();
            }

            if (!string.IsNullOrEmpty(request.MaxStartDate))
            {
                query.MaxStartDate = DateTime.Parse(request.MaxStartDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();
            }

            if (!string.IsNullOrEmpty(request.MaxEndDate))
            {
                query.MaxEndDate = DateTime.Parse(request.MaxEndDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();
            }

            query.StartIndex = request.StartIndex;
            query.Limit = request.Limit;
            query.OrderBy = BaseItemsRequest.GetOrderBy(request.SortBy, request.SortOrder);
            query.IsNews = request.IsNews;
            query.IsMovie = request.IsMovie;
            query.IsSeries = request.IsSeries;
            query.IsKids = request.IsKids;
            query.IsSports = request.IsSports;
            query.SeriesTimerId = request.SeriesTimerId;
            query.Genres = (request.Genres ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            query.GenreIds = GetGuids(request.GenreIds);

            if (!request.LibrarySeriesId.Equals(Guid.Empty))
            {
                query.IsSeries = true;

                if (_libraryManager.GetItemById(request.LibrarySeriesId) is Series series)
                {
                    query.Name = series.Name;
                }
            }

            var result = await _liveTvManager.GetPrograms(query, GetDtoOptions(_authContext, request), CancellationToken.None).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        public object Get(GetRecommendedPrograms request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var query = new InternalItemsQuery(user)
            {
                IsAiring = request.IsAiring,
                Limit = request.Limit,
                HasAired = request.HasAired,
                IsSeries = request.IsSeries,
                IsMovie = request.IsMovie,
                IsKids = request.IsKids,
                IsNews = request.IsNews,
                IsSports = request.IsSports,
                EnableTotalRecordCount = request.EnableTotalRecordCount
            };

            query.GenreIds = GetGuids(request.GenreIds);

            var result = _liveTvManager.GetRecommendedPrograms(query, GetDtoOptions(_authContext, request), CancellationToken.None);

            return ToOptimizedResult(result);
        }

        public object Post(GetPrograms request)
        {
            return Get(request);
        }

        public void Delete(DeleteRecording request)
        {
            AssertUserCanManageLiveTv();

            _libraryManager.DeleteItem(_libraryManager.GetItemById(request.Id), new DeleteOptions
            {
                DeleteFileLocation = false
            });
        }

        public Task Delete(CancelTimer request)
        {
            AssertUserCanManageLiveTv();

            return _liveTvManager.CancelTimer(request.Id);
        }

        public Task Post(UpdateTimer request)
        {
            AssertUserCanManageLiveTv();

            return _liveTvManager.UpdateTimer(request, CancellationToken.None);
        }

        public async Task<object> Get(GetSeriesTimers request)
        {
            var result = await _liveTvManager.GetSeriesTimers(new SeriesTimerQuery
            {
                SortOrder = request.SortOrder,
                SortBy = request.SortBy

            }, CancellationToken.None).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        public async Task<object> Get(GetSeriesTimer request)
        {
            var result = await _liveTvManager.GetSeriesTimer(request.Id, CancellationToken.None).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        public Task Delete(CancelSeriesTimer request)
        {
            AssertUserCanManageLiveTv();

            return _liveTvManager.CancelSeriesTimer(request.Id);
        }

        public Task Post(UpdateSeriesTimer request)
        {
            AssertUserCanManageLiveTv();

            return _liveTvManager.UpdateSeriesTimer(request, CancellationToken.None);
        }

        public async Task<object> Get(GetDefaultTimer request)
        {
            if (string.IsNullOrEmpty(request.ProgramId))
            {
                var result = await _liveTvManager.GetNewTimerDefaults(CancellationToken.None).ConfigureAwait(false);

                return ToOptimizedResult(result);
            }
            else
            {
                var result = await _liveTvManager.GetNewTimerDefaults(request.ProgramId, CancellationToken.None).ConfigureAwait(false);

                return ToOptimizedResult(result);
            }
        }

        public async Task<object> Get(GetProgram request)
        {
            var user = request.UserId.Equals(Guid.Empty) ? null : _userManager.GetUserById(request.UserId);

            var result = await _liveTvManager.GetProgram(request.Id, CancellationToken.None, user).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        public Task Post(CreateSeriesTimer request)
        {
            AssertUserCanManageLiveTv();

            return _liveTvManager.CreateSeriesTimer(request, CancellationToken.None);
        }

        public Task Post(CreateTimer request)
        {
            AssertUserCanManageLiveTv();

            return _liveTvManager.CreateTimer(request, CancellationToken.None);
        }

        public object Get(GetRecordingGroups request)
        {
            return ToOptimizedResult(new QueryResult<BaseItemDto>());
        }

        public object Get(GetRecordingGroup request)
        {
            throw new FileNotFoundException();
        }

        public object Get(GetGuideInfo request)
        {
            return ToOptimizedResult(_liveTvManager.GetGuideInfo());
        }

        public Task Post(ResetTuner request)
        {
            AssertUserCanManageLiveTv();

            return _liveTvManager.ResetTuner(request.Id, CancellationToken.None);
        }
    }
}
