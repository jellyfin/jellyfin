using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
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
    [Route("/Sessions", "GET", Summary = "Gets a list of sessions")]
    [Authenticated]
    public class GetSessions : IReturn<List<SessionInfoDto>>
    {
        [ApiMember(Name = "ControllableByUserId", Description = "Optional. Filter by sessions that a given user is allowed to remote control.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid? ControllableByUserId { get; set; }

        [ApiMember(Name = "DeviceId", Description = "Optional. Filter by device id.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string DeviceId { get; set; }
    }

    /// <summary>
    /// Class DisplayContent
    /// </summary>
    [Route("/Sessions/{Id}/Viewing", "POST", Summary = "Instructs a session to browse to an item or view")]
    [Authenticated]
    public class DisplayContent : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

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
    }

    [Route("/Sessions/{Id}/Playing", "POST", Summary = "Instructs a session to play an item")]
    [Authenticated]
    public class Play : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

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

    [Route("/Sessions/{Id}/Playing/{Command}", "POST", Summary = "Issues a playstate command to a client")]
    [Authenticated]
    public class SendPlaystateCommand : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the position to seek to
        /// </summary>
        [ApiMember(Name = "SeekPositionTicks", Description = "The position to seek to.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public long? SeekPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the play command.
        /// </summary>
        /// <value>The play command.</value>
        [ApiMember(Name = "Command", Description = "The command to send - stop, pause, unpause, nexttrack, previoustrack, seek, fullscreen.", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public PlaystateCommand Command { get; set; }
    }

    [Route("/Sessions/{Id}/System/{Command}", "POST", Summary = "Issues a system command to a client")]
    [Authenticated]
    public class SendSystemCommand : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the command.
        /// </summary>
        /// <value>The play command.</value>
        [ApiMember(Name = "Command", Description = "The command to send.", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Command { get; set; }
    }

    [Route("/Sessions/{Id}/Command/{Command}", "POST", Summary = "Issues a system command to a client")]
    [Authenticated]
    public class SendGeneralCommand : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the command.
        /// </summary>
        /// <value>The play command.</value>
        [ApiMember(Name = "Command", Description = "The command to send.", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Command { get; set; }
    }

    [Route("/Sessions/{Id}/Command", "POST", Summary = "Issues a system command to a client")]
    [Authenticated]
    public class SendFullGeneralCommand : GeneralCommand, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }
    }

    [Route("/Sessions/{Id}/Message", "POST", Summary = "Issues a command to a client to display a message to the user")]
    [Authenticated]
    public class SendMessageCommand : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        [ApiMember(Name = "Text", Description = "The message text.", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Text { get; set; }

        [ApiMember(Name = "Header", Description = "The message header.", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Header { get; set; }

        [ApiMember(Name = "TimeoutMs", Description = "The message timeout. If omitted the user will have to confirm viewing the message.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public long? TimeoutMs { get; set; }
    }

    [Route("/Sessions/{Id}/Users/{UserId}", "POST", Summary = "Adds an additional user to a session")]
    [Authenticated]
    public class AddUserToSession : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        [ApiMember(Name = "UserId", Description = "UserId Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid UserId { get; set; }
    }

    [Route("/Sessions/{Id}/Users/{UserId}", "DELETE", Summary = "Removes an additional user from a session")]
    [Authenticated]
    public class RemoveUserFromSession : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        [ApiMember(Name = "UserId", Description = "UserId Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid UserId { get; set; }
    }

    [Route("/Sessions/Capabilities", "POST", Summary = "Updates capabilities for a device")]
    public class PostCapabilities : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        [ApiMember(Name = "PlayableMediaTypes", Description = "A list of playable media types, comma delimited. Audio, Video, Book, Game, Photo.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string PlayableMediaTypes { get; set; }

        [ApiMember(Name = "SupportedCommands", Description = "A list of supported remote control commands, comma delimited", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string SupportedCommands { get; set; }

        [ApiMember(Name = "MessageCallbackUrl", Description = "A url to post messages to, including remote control commands.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string MessageCallbackUrl { get; set; }

        [ApiMember(Name = "SupportsMediaControl", Description = "Determines whether media can be played remotely.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool SupportsMediaControl { get; set; }
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

        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionsService" /> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="userManager">The user manager.</param>
        public SessionsService(ISessionManager sessionManager, IUserManager userManager)
        {
            _sessionManager = sessionManager;
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

            if (!string.IsNullOrEmpty(request.DeviceId))
            {
                result = result.Where(i => string.Equals(i.DeviceId, request.DeviceId, StringComparison.OrdinalIgnoreCase));
            }

            if (request.ControllableByUserId.HasValue)
            {
                result = result.Where(i => i.SupportsMediaControl);

                var user = _userManager.GetUserById(request.ControllableByUserId.Value);

                if (!user.Configuration.EnableRemoteControlOfOtherUsers)
                {
                    result = result.Where(i => !i.UserId.HasValue || i.ContainsUser(request.ControllableByUserId.Value));
                }
            }

            return ToOptimizedResult(result.Select(_sessionManager.GetSessionInfoDto).ToList());
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
        public void Post(DisplayContent request)
        {
            var command = new BrowseRequest
            {
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
            GeneralCommandType commandType;

            if (Enum.TryParse(request.Command, true, out commandType))
            {
                var currentSession = GetSession();

                var command = new GeneralCommand
                {
                    Name = commandType.ToString(),
                    ControllingUserId = currentSession.UserId.HasValue ? currentSession.UserId.Value.ToString("N") : null
                };

                var task = _sessionManager.SendGeneralCommand(currentSession.Id, request.Id, command, CancellationToken.None);

                Task.WaitAll(task);
            }
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

        public void Post(SendGeneralCommand request)
        {
            var currentSession = GetSession();

            var command = new GeneralCommand
            {
                Name = request.Command,
                ControllingUserId = currentSession.UserId.HasValue ? currentSession.UserId.Value.ToString("N") : null
            };

            var task = _sessionManager.SendGeneralCommand(currentSession.Id, request.Id, command, CancellationToken.None);

            Task.WaitAll(task);
        }

        public void Post(SendFullGeneralCommand request)
        {
            var currentSession = GetSession();

            request.ControllingUserId = currentSession.UserId.HasValue ? currentSession.UserId.Value.ToString("N") : null;

            var task = _sessionManager.SendGeneralCommand(currentSession.Id, request.Id, request, CancellationToken.None);

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
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                request.Id = GetSession().Id;
            }
            _sessionManager.ReportCapabilities(request.Id, new SessionCapabilities
            {
                PlayableMediaTypes = request.PlayableMediaTypes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList(),

                SupportedCommands = request.SupportedCommands.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList(),

                SupportsMediaControl = request.SupportsMediaControl,

                MessageCallbackUrl = request.MessageCallbackUrl
            });
        }
    }
}