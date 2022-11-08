using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Session;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Playstate controller.
    /// </summary>
    [Route("")]
    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class PlaystateController : BaseJellyfinApiController
    {
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataRepository;
        private readonly ILibraryManager _libraryManager;
        private readonly ISessionManager _sessionManager;
        private readonly ILogger<PlaystateController> _logger;
        private readonly TranscodingJobHelper _transcodingJobHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaystateController"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="userDataRepository">Instance of the <see cref="IUserDataManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="sessionManager">Instance of the <see cref="ISessionManager"/> interface.</param>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="transcodingJobHelper">Th <see cref="TranscodingJobHelper"/> singleton.</param>
        public PlaystateController(
            IUserManager userManager,
            IUserDataManager userDataRepository,
            ILibraryManager libraryManager,
            ISessionManager sessionManager,
            ILoggerFactory loggerFactory,
            TranscodingJobHelper transcodingJobHelper)
        {
            _userManager = userManager;
            _userDataRepository = userDataRepository;
            _libraryManager = libraryManager;
            _sessionManager = sessionManager;
            _logger = loggerFactory.CreateLogger<PlaystateController>();

            _transcodingJobHelper = transcodingJobHelper;
        }

        /// <summary>
        /// Marks an item as played for user.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="itemId">Item id.</param>
        /// <param name="datePlayed">Optional. The date the item was played.</param>
        /// <response code="200">Item marked as played.</response>
        /// <returns>An <see cref="OkResult"/> containing the <see cref="UserItemDataDto"/>.</returns>
        [HttpPost("Users/{userId}/PlayedItems/{itemId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<UserItemDataDto>> MarkPlayedItem(
            [FromRoute, Required] Guid userId,
            [FromRoute, Required] Guid itemId,
            [FromQuery, ModelBinder(typeof(LegacyDateTimeModelBinder))] DateTime? datePlayed)
        {
            var user = _userManager.GetUserById(userId);
            var session = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
            var dto = UpdatePlayedStatus(user, itemId, true, datePlayed);
            foreach (var additionalUserInfo in session.AdditionalUsers)
            {
                var additionalUser = _userManager.GetUserById(additionalUserInfo.UserId);
                UpdatePlayedStatus(additionalUser, itemId, true, datePlayed);
            }

            return dto;
        }

        /// <summary>
        /// Marks an item as unplayed for user.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="itemId">Item id.</param>
        /// <response code="200">Item marked as unplayed.</response>
        /// <returns>A <see cref="OkResult"/> containing the <see cref="UserItemDataDto"/>.</returns>
        [HttpDelete("Users/{userId}/PlayedItems/{itemId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<UserItemDataDto>> MarkUnplayedItem([FromRoute, Required] Guid userId, [FromRoute, Required] Guid itemId)
        {
            var user = _userManager.GetUserById(userId);
            var session = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
            var dto = UpdatePlayedStatus(user, itemId, false, null);
            foreach (var additionalUserInfo in session.AdditionalUsers)
            {
                var additionalUser = _userManager.GetUserById(additionalUserInfo.UserId);
                UpdatePlayedStatus(additionalUser, itemId, false, null);
            }

            return dto;
        }

        /// <summary>
        /// Reports playback has started within a session.
        /// </summary>
        /// <param name="playbackStartInfo">The playback start info.</param>
        /// <response code="204">Playback start recorded.</response>
        /// <returns>A <see cref="NoContentResult"/>.</returns>
        [HttpPost("Sessions/Playing")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> ReportPlaybackStart([FromBody] PlaybackStartInfo playbackStartInfo)
        {
            playbackStartInfo.PlayMethod = ValidatePlayMethod(playbackStartInfo.PlayMethod, playbackStartInfo.PlaySessionId);
            playbackStartInfo.SessionId = await RequestHelpers.GetSessionId(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
            await _sessionManager.OnPlaybackStart(playbackStartInfo).ConfigureAwait(false);
            return NoContent();
        }

        /// <summary>
        /// Reports playback progress within a session.
        /// </summary>
        /// <param name="playbackProgressInfo">The playback progress info.</param>
        /// <response code="204">Playback progress recorded.</response>
        /// <returns>A <see cref="NoContentResult"/>.</returns>
        [HttpPost("Sessions/Playing/Progress")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> ReportPlaybackProgress([FromBody] PlaybackProgressInfo playbackProgressInfo)
        {
            playbackProgressInfo.PlayMethod = ValidatePlayMethod(playbackProgressInfo.PlayMethod, playbackProgressInfo.PlaySessionId);
            playbackProgressInfo.SessionId = await RequestHelpers.GetSessionId(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
            await _sessionManager.OnPlaybackProgress(playbackProgressInfo).ConfigureAwait(false);
            return NoContent();
        }

        /// <summary>
        /// Pings a playback session.
        /// </summary>
        /// <param name="playSessionId">Playback session id.</param>
        /// <response code="204">Playback session pinged.</response>
        /// <returns>A <see cref="NoContentResult"/>.</returns>
        [HttpPost("Sessions/Playing/Ping")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult PingPlaybackSession([FromQuery, Required] string playSessionId)
        {
            _transcodingJobHelper.PingTranscodingJob(playSessionId, null);
            return NoContent();
        }

        /// <summary>
        /// Reports playback has stopped within a session.
        /// </summary>
        /// <param name="playbackStopInfo">The playback stop info.</param>
        /// <response code="204">Playback stop recorded.</response>
        /// <returns>A <see cref="NoContentResult"/>.</returns>
        [HttpPost("Sessions/Playing/Stopped")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> ReportPlaybackStopped([FromBody] PlaybackStopInfo playbackStopInfo)
        {
            _logger.LogDebug("ReportPlaybackStopped PlaySessionId: {0}", playbackStopInfo.PlaySessionId ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(playbackStopInfo.PlaySessionId))
            {
                await _transcodingJobHelper.KillTranscodingJobs(User.GetDeviceId()!, playbackStopInfo.PlaySessionId, s => true).ConfigureAwait(false);
            }

            playbackStopInfo.SessionId = await RequestHelpers.GetSessionId(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
            await _sessionManager.OnPlaybackStopped(playbackStopInfo).ConfigureAwait(false);
            return NoContent();
        }

        /// <summary>
        /// Reports that a user has begun playing an item.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="itemId">Item id.</param>
        /// <param name="mediaSourceId">The id of the MediaSource.</param>
        /// <param name="audioStreamIndex">The audio stream index.</param>
        /// <param name="subtitleStreamIndex">The subtitle stream index.</param>
        /// <param name="playMethod">The play method.</param>
        /// <param name="liveStreamId">The live stream id.</param>
        /// <param name="playSessionId">The play session id.</param>
        /// <param name="canSeek">Indicates if the client can seek.</param>
        /// <response code="204">Play start recorded.</response>
        /// <returns>A <see cref="NoContentResult"/>.</returns>
        [HttpPost("Users/{userId}/PlayingItems/{itemId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "userId", Justification = "Required for ServiceStack")]
        public async Task<ActionResult> OnPlaybackStart(
            [FromRoute, Required] Guid userId,
            [FromRoute, Required] Guid itemId,
            [FromQuery] string? mediaSourceId,
            [FromQuery] int? audioStreamIndex,
            [FromQuery] int? subtitleStreamIndex,
            [FromQuery] PlayMethod? playMethod,
            [FromQuery] string? liveStreamId,
            [FromQuery] string? playSessionId,
            [FromQuery] bool canSeek = false)
        {
            var playbackStartInfo = new PlaybackStartInfo
            {
                CanSeek = canSeek,
                ItemId = itemId,
                MediaSourceId = mediaSourceId,
                AudioStreamIndex = audioStreamIndex,
                SubtitleStreamIndex = subtitleStreamIndex,
                PlayMethod = playMethod ?? PlayMethod.Transcode,
                PlaySessionId = playSessionId,
                LiveStreamId = liveStreamId
            };

            playbackStartInfo.PlayMethod = ValidatePlayMethod(playbackStartInfo.PlayMethod, playbackStartInfo.PlaySessionId);
            playbackStartInfo.SessionId = await RequestHelpers.GetSessionId(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
            await _sessionManager.OnPlaybackStart(playbackStartInfo).ConfigureAwait(false);
            return NoContent();
        }

        /// <summary>
        /// Reports a user's playback progress.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="itemId">Item id.</param>
        /// <param name="mediaSourceId">The id of the MediaSource.</param>
        /// <param name="positionTicks">Optional. The current position, in ticks. 1 tick = 10000 ms.</param>
        /// <param name="audioStreamIndex">The audio stream index.</param>
        /// <param name="subtitleStreamIndex">The subtitle stream index.</param>
        /// <param name="volumeLevel">Scale of 0-100.</param>
        /// <param name="playMethod">The play method.</param>
        /// <param name="liveStreamId">The live stream id.</param>
        /// <param name="playSessionId">The play session id.</param>
        /// <param name="repeatMode">The repeat mode.</param>
        /// <param name="isPaused">Indicates if the player is paused.</param>
        /// <param name="isMuted">Indicates if the player is muted.</param>
        /// <response code="204">Play progress recorded.</response>
        /// <returns>A <see cref="NoContentResult"/>.</returns>
        [HttpPost("Users/{userId}/PlayingItems/{itemId}/Progress")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "userId", Justification = "Required for ServiceStack")]
        public async Task<ActionResult> OnPlaybackProgress(
            [FromRoute, Required] Guid userId,
            [FromRoute, Required] Guid itemId,
            [FromQuery] string? mediaSourceId,
            [FromQuery] long? positionTicks,
            [FromQuery] int? audioStreamIndex,
            [FromQuery] int? subtitleStreamIndex,
            [FromQuery] int? volumeLevel,
            [FromQuery] PlayMethod? playMethod,
            [FromQuery] string? liveStreamId,
            [FromQuery] string? playSessionId,
            [FromQuery] RepeatMode? repeatMode,
            [FromQuery] bool isPaused = false,
            [FromQuery] bool isMuted = false)
        {
            var playbackProgressInfo = new PlaybackProgressInfo
            {
                ItemId = itemId,
                PositionTicks = positionTicks,
                IsMuted = isMuted,
                IsPaused = isPaused,
                MediaSourceId = mediaSourceId,
                AudioStreamIndex = audioStreamIndex,
                SubtitleStreamIndex = subtitleStreamIndex,
                VolumeLevel = volumeLevel,
                PlayMethod = playMethod ?? PlayMethod.Transcode,
                PlaySessionId = playSessionId,
                LiveStreamId = liveStreamId,
                RepeatMode = repeatMode ?? RepeatMode.RepeatNone
            };

            playbackProgressInfo.PlayMethod = ValidatePlayMethod(playbackProgressInfo.PlayMethod, playbackProgressInfo.PlaySessionId);
            playbackProgressInfo.SessionId = await RequestHelpers.GetSessionId(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
            await _sessionManager.OnPlaybackProgress(playbackProgressInfo).ConfigureAwait(false);
            return NoContent();
        }

        /// <summary>
        /// Reports that a user has stopped playing an item.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="itemId">Item id.</param>
        /// <param name="mediaSourceId">The id of the MediaSource.</param>
        /// <param name="nextMediaType">The next media type that will play.</param>
        /// <param name="positionTicks">Optional. The position, in ticks, where playback stopped. 1 tick = 10000 ms.</param>
        /// <param name="liveStreamId">The live stream id.</param>
        /// <param name="playSessionId">The play session id.</param>
        /// <response code="204">Playback stop recorded.</response>
        /// <returns>A <see cref="NoContentResult"/>.</returns>
        [HttpDelete("Users/{userId}/PlayingItems/{itemId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "userId", Justification = "Required for ServiceStack")]
        public async Task<ActionResult> OnPlaybackStopped(
            [FromRoute, Required] Guid userId,
            [FromRoute, Required] Guid itemId,
            [FromQuery] string? mediaSourceId,
            [FromQuery] string? nextMediaType,
            [FromQuery] long? positionTicks,
            [FromQuery] string? liveStreamId,
            [FromQuery] string? playSessionId)
        {
            var playbackStopInfo = new PlaybackStopInfo
            {
                ItemId = itemId,
                PositionTicks = positionTicks,
                MediaSourceId = mediaSourceId,
                PlaySessionId = playSessionId,
                LiveStreamId = liveStreamId,
                NextMediaType = nextMediaType
            };

            _logger.LogDebug("ReportPlaybackStopped PlaySessionId: {0}", playbackStopInfo.PlaySessionId ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(playbackStopInfo.PlaySessionId))
            {
                await _transcodingJobHelper.KillTranscodingJobs(User.GetDeviceId()!, playbackStopInfo.PlaySessionId, s => true).ConfigureAwait(false);
            }

            playbackStopInfo.SessionId = await RequestHelpers.GetSessionId(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
            await _sessionManager.OnPlaybackStopped(playbackStopInfo).ConfigureAwait(false);
            return NoContent();
        }

        /// <summary>
        /// Updates the played status.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="wasPlayed">if set to <c>true</c> [was played].</param>
        /// <param name="datePlayed">The date played.</param>
        /// <returns>Task.</returns>
        private UserItemDataDto UpdatePlayedStatus(User user, Guid itemId, bool wasPlayed, DateTime? datePlayed)
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

        private PlayMethod ValidatePlayMethod(PlayMethod method, string? playSessionId)
        {
            if (method == PlayMethod.Transcode)
            {
                var job = string.IsNullOrWhiteSpace(playSessionId) ? null : _transcodingJobHelper.GetTranscodingJob(playSessionId);
                if (job == null)
                {
                    return PlayMethod.DirectPlay;
                }
            }

            return method;
        }
    }
}
