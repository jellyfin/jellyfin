using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Session;
using ServiceStack;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

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

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="UpdateUserItemRating" /> is likes.
        /// </summary>
        /// <value><c>true</c> if likes; otherwise, <c>false</c>.</value>
        [ApiMember(Name = "CanSeek", Description = "Indicates if the client can seek", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "POST")]
        public bool CanSeek { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "QueueableMediaTypes", Description = "A list of media types that can be queued from this item, comma delimited. Audio,Video,Book,Game", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST", AllowMultiple = true)]
        public string QueueableMediaTypes { get; set; }

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

        public PlaystateService(IUserManager userManager, IUserDataManager userDataRepository, ILibraryManager libraryManager, ISessionManager sessionManager)
        {
            _userManager = userManager;
            _userDataRepository = userDataRepository;
            _libraryManager = libraryManager;
            _sessionManager = sessionManager;
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public async Task<object> Post(MarkPlayedItem request)
        {
            var result = await MarkPlayed(request).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        private async Task<UserItemDataDto> MarkPlayed(MarkPlayedItem request)
        {
            var user = _userManager.GetUserById(request.UserId);

            DateTime? datePlayed = null;

            if (!string.IsNullOrEmpty(request.DatePlayed))
            {
                datePlayed = DateTime.ParseExact(request.DatePlayed, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            }

            var session = await GetSession().ConfigureAwait(false);

            var dto = await UpdatePlayedStatus(user, request.Id, true, datePlayed).ConfigureAwait(false);

            foreach (var additionalUserInfo in session.AdditionalUsers)
            {
                var additionalUser = _userManager.GetUserById(additionalUserInfo.UserId);

                await UpdatePlayedStatus(additionalUser, request.Id, true, datePlayed).ConfigureAwait(false);
            }

            return dto;
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(OnPlaybackStart request)
        {
            var queueableMediaTypes = request.QueueableMediaTypes ?? string.Empty;

            Post(new ReportPlaybackStart
            {
                CanSeek = request.CanSeek,
                ItemId = request.Id,
                QueueableMediaTypes = queueableMediaTypes.Split(',').ToList(),
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
            request.SessionId = GetSession().Result.Id;

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
                ItemId = request.Id,
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
            request.SessionId = GetSession().Result.Id;

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
        public void Delete(OnPlaybackStopped request)
        {
            Post(new ReportPlaybackStopped
            {
                ItemId = request.Id,
                PositionTicks = request.PositionTicks,
                MediaSourceId = request.MediaSourceId,
                PlaySessionId = request.PlaySessionId,
                LiveStreamId = request.LiveStreamId
            });
        }

        public void Post(ReportPlaybackStopped request)
        {
            Logger.Debug("ReportPlaybackStopped PlaySessionId: {0}", request.PlaySessionId ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(request.PlaySessionId))
            {
                ApiEntryPoint.Instance.KillTranscodingJobs(AuthorizationContext.GetAuthorizationInfo(Request).DeviceId, request.PlaySessionId, s => true);
            }

            request.SessionId = GetSession().Result.Id;

            var task = _sessionManager.OnPlaybackStopped(request);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public object Delete(MarkUnplayedItem request)
        {
            var task = MarkUnplayed(request);

            return ToOptimizedResult(task.Result);
        }

        private async Task<UserItemDataDto> MarkUnplayed(MarkUnplayedItem request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var session = await GetSession().ConfigureAwait(false);

            var dto = await UpdatePlayedStatus(user, request.Id, false, null).ConfigureAwait(false);

            foreach (var additionalUserInfo in session.AdditionalUsers)
            {
                var additionalUser = _userManager.GetUserById(additionalUserInfo.UserId);

                await UpdatePlayedStatus(additionalUser, request.Id, false, null).ConfigureAwait(false);
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
        private async Task<UserItemDataDto> UpdatePlayedStatus(User user, string itemId, bool wasPlayed, DateTime? datePlayed)
        {
            var item = _libraryManager.GetItemById(itemId);

            if (wasPlayed)
            {
                await item.MarkPlayed(user, datePlayed, true).ConfigureAwait(false);
            }
            else
            {
                await item.MarkUnplayed(user).ConfigureAwait(false);
            }

            return await _userDataRepository.GetUserDataDto(item, user).ConfigureAwait(false);
        }
    }
}