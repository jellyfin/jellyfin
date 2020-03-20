using System;
using System.Globalization;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class MarkPlayedItem
    /// </summary>
    [Route("/Users/{UserId}/PlayedItems/{Id}", "POST", Summary = "Marks an item as played")]
    public class MarkPlayedItem : IReturn<UserItemDataDto>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string UserId { get; set; }

        [ApiMember(Name = "DatePlayed", Description = "The date the item was played (if any). Format = yyyyMMddHHmmss", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string DatePlayed { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class MarkUnplayedItem
    /// </summary>
    [Route("/Users/{UserId}/PlayedItems/{Id}", "DELETE", Summary = "Marks an item as unplayed")]
    public class MarkUnplayedItem : IReturn<UserItemDataDto>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }
    }

    [Route("/Sessions/Playing", "POST", Summary = "Reports playback has started within a session")]
    public class ReportPlaybackStart : PlaybackStartInfo, IReturnVoid
    {
    }

    [Route("/Sessions/Playing/Progress", "POST", Summary = "Reports playback progress within a session")]
    public class ReportPlaybackProgress : PlaybackProgressInfo, IReturnVoid
    {
    }

    [Route("/Sessions/Playing/Ping", "POST", Summary = "Pings a playback session")]
    public class PingPlaybackSession : IReturnVoid
    {
        [ApiMember(Name = "PlaySessionId", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string PlaySessionId { get; set; }
    }

    [Route("/Sessions/Playing/Stopped", "POST", Summary = "Reports playback has stopped within a session")]
    public class ReportPlaybackStopped : PlaybackStopInfo, IReturnVoid
    {
    }

    /// <summary>
    /// Class OnPlaybackStart
    /// </summary>
    [Route("/Users/{UserId}/PlayingItems/{Id}", "POST", Summary = "Reports that a user has begun playing an item")]
    public class OnPlaybackStart : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        [ApiMember(Name = "MediaSourceId", Description = "The id of the MediaSource", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string MediaSourceId { get; set; }

        [ApiMember(Name = "CanSeek", Description = "Indicates if the client can seek", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "POST")]
        public bool CanSeek { get; set; }

        [ApiMember(Name = "AudioStreamIndex", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "POST")]
        public int? AudioStreamIndex { get; set; }

        [ApiMember(Name = "SubtitleStreamIndex", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "POST")]
        public int? SubtitleStreamIndex { get; set; }

        [ApiMember(Name = "PlayMethod", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public PlayMethod PlayMethod { get; set; }

        [ApiMember(Name = "LiveStreamId", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string LiveStreamId { get; set; }

        [ApiMember(Name = "PlaySessionId", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string PlaySessionId { get; set; }
    }

    /// <summary>
    /// Class OnPlaybackProgress
    /// </summary>
    [Route("/Users/{UserId}/PlayingItems/{Id}/Progress", "POST", Summary = "Reports a user's playback progress")]
    public class OnPlaybackProgress : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        [ApiMember(Name = "MediaSourceId", Description = "The id of the MediaSource", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string MediaSourceId { get; set; }

        /// <summary>
        /// Gets or sets the position ticks.
        /// </summary>
        /// <value>The position ticks.</value>
        [ApiMember(Name = "PositionTicks", Description = "Optional. The current position, in ticks. 1 tick = 10000 ms", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "POST")]
        public long? PositionTicks { get; set; }

        [ApiMember(Name = "IsPaused", Description = "Indicates if the player is paused.", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "POST")]
        public bool IsPaused { get; set; }

        [ApiMember(Name = "IsMuted", Description = "Indicates if the player is muted.", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "POST")]
        public bool IsMuted { get; set; }

        [ApiMember(Name = "AudioStreamIndex", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "POST")]
        public int? AudioStreamIndex { get; set; }

        [ApiMember(Name = "SubtitleStreamIndex", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "POST")]
        public int? SubtitleStreamIndex { get; set; }

        [ApiMember(Name = "VolumeLevel", Description = "Scale of 0-100", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "POST")]
        public int? VolumeLevel { get; set; }

        [ApiMember(Name = "PlayMethod", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public PlayMethod PlayMethod { get; set; }

        [ApiMember(Name = "LiveStreamId", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string LiveStreamId { get; set; }

        [ApiMember(Name = "PlaySessionId", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string PlaySessionId { get; set; }

        [ApiMember(Name = "RepeatMode", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public RepeatMode RepeatMode { get; set; }
    }

    /// <summary>
    /// Class OnPlaybackStopped
    /// </summary>
    [Route("/Users/{UserId}/PlayingItems/{Id}", "DELETE", Summary = "Reports that a user has stopped playing an item")]
    public class OnPlaybackStopped : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }

        [ApiMember(Name = "MediaSourceId", Description = "The id of the MediaSource", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "DELETE")]
        public string MediaSourceId { get; set; }

        [ApiMember(Name = "NextMediaType", Description = "The next media type that will play", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "DELETE")]
        public string NextMediaType { get; set; }

        /// <summary>
        /// Gets or sets the position ticks.
        /// </summary>
        /// <value>The position ticks.</value>
        [ApiMember(Name = "PositionTicks", Description = "Optional. The position, in ticks, where playback stopped. 1 tick = 10000 ms", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "DELETE")]
        public long? PositionTicks { get; set; }

        [ApiMember(Name = "LiveStreamId", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string LiveStreamId { get; set; }

        [ApiMember(Name = "PlaySessionId", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string PlaySessionId { get; set; }
    }

    [Authenticated]
    public class PlaystateService : BaseApiService
    {
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataRepository;
        private readonly ILibraryManager _libraryManager;
        private readonly ISessionManager _sessionManager;
        private readonly ISessionContext _sessionContext;
        private readonly IAuthorizationContext _authContext;

        public PlaystateService(
            ILogger<PlaystateService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IUserManager userManager,
            IUserDataManager userDataRepository,
            ILibraryManager libraryManager,
            ISessionManager sessionManager,
            ISessionContext sessionContext,
            IAuthorizationContext authContext)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _userManager = userManager;
            _userDataRepository = userDataRepository;
            _libraryManager = libraryManager;
            _sessionManager = sessionManager;
            _sessionContext = sessionContext;
            _authContext = authContext;
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public object Post(MarkPlayedItem request)
        {
            var result = MarkPlayed(request);

            return ToOptimizedResult(result);
        }

        private UserItemDataDto MarkPlayed(MarkPlayedItem request)
        {
            var user = _userManager.GetUserById(Guid.Parse(request.UserId));

            DateTime? datePlayed = null;

            if (!string.IsNullOrEmpty(request.DatePlayed))
            {
                datePlayed = DateTime.ParseExact(request.DatePlayed, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            }

            var session = GetSession(_sessionContext);

            var dto = UpdatePlayedStatus(user, request.Id, true, datePlayed);

            foreach (var additionalUserInfo in session.AdditionalUsers)
            {
                var additionalUser = _userManager.GetUserById(additionalUserInfo.UserId);

                UpdatePlayedStatus(additionalUser, request.Id, true, datePlayed);
            }

            return dto;
        }

        private PlayMethod ValidatePlayMethod(PlayMethod method, string playSessionId)
        {
            if (method == PlayMethod.Transcode)
            {
                var job = string.IsNullOrWhiteSpace(playSessionId) ? null : ApiEntryPoint.Instance.GetTranscodingJob(playSessionId);
                if (job == null)
                {
                    return PlayMethod.DirectPlay;
                }
            }

            return method;
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(OnPlaybackStart request)
        {
            Post(new ReportPlaybackStart
            {
                CanSeek = request.CanSeek,
                ItemId = new Guid(request.Id),
                MediaSourceId = request.MediaSourceId,
                AudioStreamIndex = request.AudioStreamIndex,
                SubtitleStreamIndex = request.SubtitleStreamIndex,
                PlayMethod = request.PlayMethod,
                PlaySessionId = request.PlaySessionId,
                LiveStreamId = request.LiveStreamId
            });
        }

        public void Post(ReportPlaybackStart request)
        {
            request.PlayMethod = ValidatePlayMethod(request.PlayMethod, request.PlaySessionId);

            request.SessionId = GetSession(_sessionContext).Id;

            var task = _sessionManager.OnPlaybackStart(request);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(OnPlaybackProgress request)
        {
            Post(new ReportPlaybackProgress
            {
                ItemId = new Guid(request.Id),
                PositionTicks = request.PositionTicks,
                IsMuted = request.IsMuted,
                IsPaused = request.IsPaused,
                MediaSourceId = request.MediaSourceId,
                AudioStreamIndex = request.AudioStreamIndex,
                SubtitleStreamIndex = request.SubtitleStreamIndex,
                VolumeLevel = request.VolumeLevel,
                PlayMethod = request.PlayMethod,
                PlaySessionId = request.PlaySessionId,
                LiveStreamId = request.LiveStreamId,
                RepeatMode = request.RepeatMode
            });
        }

        public void Post(ReportPlaybackProgress request)
        {
            request.PlayMethod = ValidatePlayMethod(request.PlayMethod, request.PlaySessionId);

            request.SessionId = GetSession(_sessionContext).Id;

            var task = _sessionManager.OnPlaybackProgress(request);

            Task.WaitAll(task);
        }

        public void Post(PingPlaybackSession request)
        {
            ApiEntryPoint.Instance.PingTranscodingJob(request.PlaySessionId, null);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public Task Delete(OnPlaybackStopped request)
        {
            return Post(new ReportPlaybackStopped
            {
                ItemId = new Guid(request.Id),
                PositionTicks = request.PositionTicks,
                MediaSourceId = request.MediaSourceId,
                PlaySessionId = request.PlaySessionId,
                LiveStreamId = request.LiveStreamId,
                NextMediaType = request.NextMediaType
            });
        }

        public async Task Post(ReportPlaybackStopped request)
        {
            Logger.LogDebug("ReportPlaybackStopped PlaySessionId: {0}", request.PlaySessionId ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(request.PlaySessionId))
            {
                await ApiEntryPoint.Instance.KillTranscodingJobs(_authContext.GetAuthorizationInfo(Request).DeviceId, request.PlaySessionId, s => true);
            }

            request.SessionId = GetSession(_sessionContext).Id;

            await _sessionManager.OnPlaybackStopped(request);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public object Delete(MarkUnplayedItem request)
        {
            var task = MarkUnplayed(request);

            return ToOptimizedResult(task);
        }

        private UserItemDataDto MarkUnplayed(MarkUnplayedItem request)
        {
            var user = _userManager.GetUserById(Guid.Parse(request.UserId));

            var session = GetSession(_sessionContext);

            var dto = UpdatePlayedStatus(user, request.Id, false, null);

            foreach (var additionalUserInfo in session.AdditionalUsers)
            {
                var additionalUser = _userManager.GetUserById(additionalUserInfo.UserId);

                UpdatePlayedStatus(additionalUser, request.Id, false, null);
            }

            return dto;
        }

        /// <summary>
        /// Updates the played status.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="wasPlayed">if set to <c>true</c> [was played].</param>
        /// <param name="datePlayed">The date played.</param>
        /// <returns>Task.</returns>
        private UserItemDataDto UpdatePlayedStatus(User user, string itemId, bool wasPlayed, DateTime? datePlayed)
        {
            var item = _libraryManager.GetItemById(itemId);

            if (wasPlayed)
            {
                item.MarkPlayed(user, datePlayed, true);
            }
            else
            {
                item.MarkUnplayed(user);
            }

            return _userDataRepository.GetUserDataDto(item, user);
        }
    }
}
