using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetSessions
    /// </summary>
    [Route("/Sessions", "GET")]
    [Api(("Gets a list of sessions"))]
    public class GetSessions : IReturn<List<SessionInfoDto>>
    {
        /// <summary>
        /// Gets or sets a value indicating whether [supports remote control].
        /// </summary>
        /// <value><c>null</c> if [supports remote control] contains no value, <c>true</c> if [supports remote control]; otherwise, <c>false</c>.</value>
        [ApiMember(Name = "SupportsRemoteControl", Description = "Optional. Filter by sessions that can be remote controlled.", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? SupportsRemoteControl { get; set; }

        [ApiMember(Name = "ControllableByUserId", Description = "Optional. Filter by sessions that a given user is allowed to remote control.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid? ControllableByUserId { get; set; }

        [ApiMember(Name = "DeviceId", Description = "Optional. Filter by device id.", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public string DeviceId { get; set; }
    }

    /// <summary>
    /// Class BrowseTo
    /// </summary>
    [Route("/Sessions/{Id}/Viewing", "POST")]
    [Api(("Instructs a session to browse to an item or view"))]
    public class BrowseTo : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid Id { get; set; }

        /// <summary>
        /// Artist, Genre, Studio, Person, or any kind of BaseItem
        /// </summary>
        /// <value>The type of the item.</value>
        [ApiMember(Name = "ItemType", Description = "The type of item to browse to.", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string ItemType { get; set; }

        /// <summary>
        /// Artist name, genre name, item Id, etc
        /// </summary>
        /// <value>The item identifier.</value>
        [ApiMember(Name = "ItemId", Description = "The Id of the item.", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string ItemId { get; set; }

        /// <summary>
        /// Gets or sets the name of the item.
        /// </summary>
        /// <value>The name of the item.</value>
        [ApiMember(Name = "ItemName", Description = "The name of the item.", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string ItemName { get; set; }

        /// <summary>
        /// Gets or sets the context (Movies, Music, TvShows, etc)
        /// Applicable to genres, studios and persons only because the context of items and artists can be inferred.
        /// This is optional to supply and clients are free to ignore it.
        /// </summary>
        /// <value>The context.</value>
        [ApiMember(Name = "Context", Description = "The ui context for the client (movies, music, tv, games etc). This is optional to supply and clients are free to ignore it.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Context { get; set; }
    }

    [Route("/Sessions/{Id}/Playing", "POST")]
    [Api(("Instructs a session to play an item"))]
    public class Play : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid Id { get; set; }

        /// <summary>
        /// Artist, Genre, Studio, Person, or any kind of BaseItem
        /// </summary>
        /// <value>The type of the item.</value>
        [ApiMember(Name = "ItemIds", Description = "The ids of the items to play, comma delimited", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST", AllowMultiple = true)]
        public string ItemIds { get; set; }

        /// <summary>
        /// Gets or sets the start position ticks that the first item should be played at
        /// </summary>
        /// <value>The start position ticks.</value>
        [ApiMember(Name = "StartPositionTicks", Description = "The starting position of the first item.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public long? StartPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the play command.
        /// </summary>
        /// <value>The play command.</value>
        [ApiMember(Name = "PlayCommand", Description = "The type of play command to issue (PlayNow, PlayNext, PlayLast). Clients who have not yet implemented play next and play last may play now.", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public PlayCommand PlayCommand { get; set; }
    }

    [Route("/Sessions/{Id}/Playing/{Command}", "POST")]
    [Api(("Issues a playstate command to a client"))]
    public class SendPlaystateCommand : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the position to seek to
        /// </summary>
        [ApiMember(Name = "SeekPositionTicks", Description = "The position to seek to.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public long? SeekPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the play command.
        /// </summary>
        /// <value>The play command.</value>
        [ApiMember(Name = "Command", Description = "The command to send - stop, pause, unpause, nexttrack, previoustrack, seek.", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public PlaystateCommand Command { get; set; }
    }

    [Route("/Sessions/{Id}/System/{Command}", "POST")]
    [Api(("Issues a system command to a client"))]
    public class SendSystemCommand : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the command.
        /// </summary>
        /// <value>The play command.</value>
        [ApiMember(Name = "Command", Description = "The command to send.", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public SystemCommand Command { get; set; }
    }

    [Route("/Sessions/{Id}/Message", "POST")]
    [Api(("Issues a command to a client to display a message to the user"))]
    public class SendMessageCommand : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid Id { get; set; }

        [ApiMember(Name = "Text", Description = "The message text.", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Text { get; set; }

        [ApiMember(Name = "Header", Description = "The message header.", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Header { get; set; }

        [ApiMember(Name = "TimeoutMs", Description = "The message timeout. If omitted the user will have to confirm viewing the message.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public long? TimeoutMs { get; set; }
    }

    [Route("/Sessions/{Id}/Users/{UserId}", "POST")]
    [Api(("Adds an additional user to a session"))]
    public class AddUserToSession : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid Id { get; set; }

        [ApiMember(Name = "UserId", Description = "UserId Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid UserId { get; set; }
    }

    [Route("/Sessions/{Id}/Users/{UserId}", "DELETE")]
    [Api(("Removes an additional user from a session"))]
    public class RemoveUserFromSession : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid Id { get; set; }

        [ApiMember(Name = "UserId", Description = "UserId Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid UserId { get; set; }
    }

    [Route("/Sessions/{Id}/Capabilities", "POST")]
    [Api(("Updates capabilities for a device"))]
    public class PostCapabilities : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid Id { get; set; }

        [ApiMember(Name = "PlayableMediaTypes", Description = "A list of playable media types, comma delimited. Audio, Video, Book, Game, Photo.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string PlayableMediaTypes { get; set; }

        [ApiMember(Name = "SupportsFullscreenToggle", Description = "Whether or not the session supports fullscreen toggle", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool SupportsFullscreenToggle { get; set; }
    }

    /// <summary>
    /// Class SessionsService
    /// </summary>
    public class SessionsService : BaseApiService
    {
        /// <summary>
        /// The _session manager
        /// </summary>
        private readonly ISessionManager _sessionManager;

        private readonly IDtoService _dtoService;
        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionsService" /> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="dtoService">The dto service.</param>
        public SessionsService(ISessionManager sessionManager, IDtoService dtoService, IUserManager userManager)
        {
            _sessionManager = sessionManager;
            _dtoService = dtoService;
            _userManager = userManager;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSessions request)
        {
            var result = _sessionManager.Sessions.Where(i => i.IsActive);

            if (request.SupportsRemoteControl.HasValue)
            {
                result = result.Where(i => i.SupportsRemoteControl == request.SupportsRemoteControl.Value);
            }

            if (!string.IsNullOrEmpty(request.DeviceId))
            {
                result = result.Where(i => string.Equals(i.DeviceId, request.DeviceId, StringComparison.OrdinalIgnoreCase));
            }

            if (request.ControllableByUserId.HasValue)
            {
                var user = _userManager.GetUserById(request.ControllableByUserId.Value);

                if (!user.Configuration.EnableRemoteControlOfOtherUsers)
                {
                    result = result.Where(i => !i.UserId.HasValue || i.ContainsUser(request.ControllableByUserId.Value));
                }
            }

            return ToOptimizedResult(result.Select(_dtoService.GetSessionInfoDto).ToList());
        }

        public void Post(SendPlaystateCommand request)
        {
            var command = new PlaystateRequest
            {
                Command = request.Command,
                SeekPositionTicks = request.SeekPositionTicks
            };

            var task = _sessionManager.SendPlaystateCommand(GetSession().Id, request.Id, command, CancellationToken.None);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(BrowseTo request)
        {
            var command = new BrowseRequest
            {
                Context = request.Context,
                ItemId = request.ItemId,
                ItemName = request.ItemName,
                ItemType = request.ItemType
            };

            var task = _sessionManager.SendBrowseCommand(GetSession().Id, request.Id, command, CancellationToken.None);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(SendSystemCommand request)
        {
            var task = _sessionManager.SendSystemCommand(GetSession().Id, request.Id, request.Command, CancellationToken.None);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(SendMessageCommand request)
        {
            var command = new MessageCommand
            {
                Header = string.IsNullOrEmpty(request.Header) ? "Message from Server" : request.Header,
                TimeoutMs = request.TimeoutMs,
                Text = request.Text
            };

            var task = _sessionManager.SendMessageCommand(GetSession().Id, request.Id, command, CancellationToken.None);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(Play request)
        {
            var command = new PlayRequest
            {
                ItemIds = request.ItemIds.Split(',').ToArray(),

                PlayCommand = request.PlayCommand,
                StartPositionTicks = request.StartPositionTicks
            };

            var task = _sessionManager.SendPlayCommand(GetSession().Id, request.Id, command, CancellationToken.None);

            Task.WaitAll(task);
        }

        public void Post(AddUserToSession request)
        {
            _sessionManager.AddAdditionalUser(request.Id, request.UserId);
        }

        public void Delete(RemoveUserFromSession request)
        {
            _sessionManager.RemoveAdditionalUser(request.Id, request.UserId);
        }

        public void Post(PostCapabilities request)
        {
            _sessionManager.ReportCapabilities(request.Id, new SessionCapabilities
            {
                PlayableMediaTypes = request.PlayableMediaTypes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),

                SupportsFullscreenToggle = request.SupportsFullscreenToggle
            });
        }

        private SessionInfo GetSession()
        {
            var auth = AuthorizationRequestFilterAttribute.GetAuthorization(Request);

            return _sessionManager.Sessions.First(i => string.Equals(i.DeviceId, auth.DeviceId) &&
                string.Equals(i.Client, auth.Client) &&
                string.Equals(i.ApplicationVersion, auth.Version));
        }
    }
}