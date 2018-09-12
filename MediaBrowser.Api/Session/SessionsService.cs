using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Services;
using MediaBrowser.Controller;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Api.Session
{
    /// <summary>
    /// Class GetSessions
    /// </summary>
    [Route("/Sessions", "GET", Summary = "Gets a list of sessions")]
    [Authenticated]
    public class GetSessions : IReturn<SessionInfo[]>
    {
        [ApiMember(Name = "ControllableByUserId", Description = "Optional. Filter by sessions that a given user is allowed to remote control.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid ControllableByUserId { get; set; }

        [ApiMember(Name = "DeviceId", Description = "Optional. Filter by device id.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string DeviceId { get; set; }

        public int? ActiveWithinSeconds { get; set; }
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
    public class Play : PlayRequest
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }
    }

    [Route("/Sessions/{Id}/Playing/{Command}", "POST", Summary = "Issues a playstate command to a client")]
    [Authenticated]
    public class SendPlaystateCommand : PlaystateRequest, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }
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
        public string UserId { get; set; }
    }

    [Route("/Sessions/{Id}/Users/{UserId}", "DELETE", Summary = "Removes an additional user from a session")]
    [Authenticated]
    public class RemoveUserFromSession : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        [ApiMember(Name = "UserId", Description = "UserId Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string UserId { get; set; }
    }

    [Route("/Sessions/Capabilities", "POST", Summary = "Updates capabilities for a device")]
    [Authenticated]
    public class PostCapabilities : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Id { get; set; }

        [ApiMember(Name = "PlayableMediaTypes", Description = "A list of playable media types, comma delimited. Audio, Video, Book, Game, Photo.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string PlayableMediaTypes { get; set; }

        [ApiMember(Name = "SupportedCommands", Description = "A list of supported remote control commands, comma delimited", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string SupportedCommands { get; set; }

        [ApiMember(Name = "SupportsMediaControl", Description = "Determines whether media can be played remotely.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool SupportsMediaControl { get; set; }

        [ApiMember(Name = "SupportsSync", Description = "Determines whether sync is supported.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool SupportsSync { get; set; }

        [ApiMember(Name = "SupportsPersistentIdentifier", Description = "Determines whether the device supports a unique identifier.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool SupportsPersistentIdentifier { get; set; }

        public PostCapabilities()
        {
            SupportsPersistentIdentifier = true;
        }
    }

    [Route("/Sessions/Capabilities/Full", "POST", Summary = "Updates capabilities for a device")]
    [Authenticated]
    public class PostFullCapabilities : ClientCapabilities, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Id { get; set; }
    }

    [Route("/Sessions/Logout", "POST", Summary = "Reports that a session has ended")]
    [Authenticated]
    public class ReportSessionEnded : IReturnVoid
    {
    }

    [Route("/Auth/Keys", "GET")]
    [Authenticated(Roles = "Admin")]
    public class GetApiKeys
    {
    }

    [Route("/Auth/Providers", "GET")]
    [Authenticated(Roles = "Admin")]
    public class GetAuthProviders : IReturn<NameIdPair[]>
    {
    }

    [Route("/Auth/Keys/{Key}", "DELETE")]
    [Authenticated(Roles = "Admin")]
    public class RevokeKey
    {
        [ApiMember(Name = "Key", Description = "Auth Key", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Key { get; set; }
    }

    [Route("/Auth/Keys", "POST")]
    [Authenticated(Roles = "Admin")]
    public class CreateKey
    {
        [ApiMember(Name = "App", Description = "App", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string App { get; set; }
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
        private readonly IAuthorizationContext _authContext;
        private readonly IAuthenticationRepository _authRepo;
        private readonly IDeviceManager _deviceManager;
        private readonly ISessionContext _sessionContext;
        private IServerApplicationHost _appHost;

        public SessionsService(ISessionManager sessionManager, IServerApplicationHost appHost, IUserManager userManager, IAuthorizationContext authContext, IAuthenticationRepository authRepo, IDeviceManager deviceManager, ISessionContext sessionContext)
        {
            _sessionManager = sessionManager;
            _userManager = userManager;
            _authContext = authContext;
            _authRepo = authRepo;
            _deviceManager = deviceManager;
            _sessionContext = sessionContext;
            _appHost = appHost;
        }

        public object Get(GetAuthProviders request)
        {
            return _userManager.GetAuthenticationProviders();
        }

        public void Delete(RevokeKey request)
        {
            _sessionManager.RevokeToken(request.Key);

        }

        public void Post(CreateKey request)
        {
            _authRepo.Create(new AuthenticationInfo
            {
                AppName = request.App,
                AccessToken = Guid.NewGuid().ToString("N"),
                DateCreated = DateTime.UtcNow,
                DeviceId = _appHost.SystemId,
                DeviceName = _appHost.FriendlyName,
                AppVersion = _appHost.ApplicationVersion.ToString()

            });
        }

        public void Post(ReportSessionEnded request)
        {
            var auth = _authContext.GetAuthorizationInfo(Request);

            _sessionManager.Logout(auth.Token);
        }

        public object Get(GetApiKeys request)
        {
            var result = _authRepo.Get(new AuthenticationInfoQuery
            {
                HasUser = false
            });

            return result;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSessions request)
        {
            var result = _sessionManager.Sessions;

            if (!string.IsNullOrEmpty(request.DeviceId))
            {
                result = result.Where(i => string.Equals(i.DeviceId, request.DeviceId, StringComparison.OrdinalIgnoreCase));
            }

            if (!request.ControllableByUserId.Equals(Guid.Empty))
            {
                result = result.Where(i => i.SupportsRemoteControl);

                var user = _userManager.GetUserById(request.ControllableByUserId);

                if (!user.Policy.EnableRemoteControlOfOtherUsers)
                {
                    result = result.Where(i => i.UserId.Equals(Guid.Empty) || i.ContainsUser(request.ControllableByUserId));
                }

                if (!user.Policy.EnableSharedDeviceControl)
                {
                    result = result.Where(i => !i.UserId.Equals(Guid.Empty));
                }

                if (request.ActiveWithinSeconds.HasValue && request.ActiveWithinSeconds.Value > 0)
                {
                    var minActiveDate = DateTime.UtcNow.AddSeconds(0 - request.ActiveWithinSeconds.Value);
                    result = result.Where(i => i.LastActivityDate >= minActiveDate);
                }

                result = result.Where(i =>
                {
                    var deviceId = i.DeviceId;

                    if (!string.IsNullOrWhiteSpace(deviceId))
                    {
                        if (!_deviceManager.CanAccessDevice(user, deviceId))
                        {
                            return false;
                        }
                    }

                    return true;
                });
            }

            return ToOptimizedResult(result.ToArray());
        }

        public Task Post(SendPlaystateCommand request)
        {
            return _sessionManager.SendPlaystateCommand(GetSession(_sessionContext).Id, request.Id, request, CancellationToken.None);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public Task Post(DisplayContent request)
        {
            var command = new BrowseRequest
            {
                ItemId = request.ItemId,
                ItemName = request.ItemName,
                ItemType = request.ItemType
            };

            return _sessionManager.SendBrowseCommand(GetSession(_sessionContext).Id, request.Id, command, CancellationToken.None);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public Task Post(SendSystemCommand request)
        {
            GeneralCommandType commandType;
            var name = request.Command;

            if (Enum.TryParse(name, true, out commandType))
            {
                name = commandType.ToString();
            }

            var currentSession = GetSession(_sessionContext);

            var command = new GeneralCommand
            {
                Name = name,
                ControllingUserId = currentSession.UserId
            };

            return _sessionManager.SendGeneralCommand(currentSession.Id, request.Id, command, CancellationToken.None);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public Task Post(SendMessageCommand request)
        {
            var command = new MessageCommand
            {
                Header = string.IsNullOrEmpty(request.Header) ? "Message from Server" : request.Header,
                TimeoutMs = request.TimeoutMs,
                Text = request.Text
            };

            return _sessionManager.SendMessageCommand(GetSession(_sessionContext).Id, request.Id, command, CancellationToken.None);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public Task Post(Play request)
        {
            return _sessionManager.SendPlayCommand(GetSession(_sessionContext).Id, request.Id, request, CancellationToken.None);
        }

        public Task Post(SendGeneralCommand request)
        {
            var currentSession = GetSession(_sessionContext);

            var command = new GeneralCommand
            {
                Name = request.Command,
                ControllingUserId = currentSession.UserId
            };

            return _sessionManager.SendGeneralCommand(currentSession.Id, request.Id, command, CancellationToken.None);
        }

        public Task Post(SendFullGeneralCommand request)
        {
            var currentSession = GetSession(_sessionContext);

            request.ControllingUserId = currentSession.UserId;

            return _sessionManager.SendGeneralCommand(currentSession.Id, request.Id, request, CancellationToken.None);
        }

        public void Post(AddUserToSession request)
        {
            _sessionManager.AddAdditionalUser(request.Id, new Guid(request.UserId));
        }

        public void Delete(RemoveUserFromSession request)
        {
            _sessionManager.RemoveAdditionalUser(request.Id, new Guid(request.UserId));
        }

        public void Post(PostCapabilities request)
        {
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                request.Id = GetSession(_sessionContext).Id;
            }
            _sessionManager.ReportCapabilities(request.Id, new ClientCapabilities
            {
                PlayableMediaTypes = SplitValue(request.PlayableMediaTypes, ','),

                SupportedCommands = SplitValue(request.SupportedCommands, ','),

                SupportsMediaControl = request.SupportsMediaControl,

                SupportsSync = request.SupportsSync,

                SupportsPersistentIdentifier = request.SupportsPersistentIdentifier
            });
        }

        public void Post(PostFullCapabilities request)
        {
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                request.Id = GetSession(_sessionContext).Id;
            }
            _sessionManager.ReportCapabilities(request.Id, request);
        }
    }
}