using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Syncplay;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Syncplay;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.Syncplay
{
    [Route("/Syncplay/{SessionId}/NewGroup", "POST", Summary = "Create a new Syncplay group")]
    [Authenticated]
    public class SyncplayNewGroup : IReturnVoid
    {
        [ApiMember(Name = "SessionId", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string SessionId { get; set; }
    }

    [Route("/Syncplay/{SessionId}/JoinGroup", "POST", Summary = "Join an existing Syncplay group")]
    [Authenticated]
    public class SyncplayJoinGroup : IReturnVoid
    {
        [ApiMember(Name = "SessionId", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string SessionId { get; set; }

        /// <summary>
        /// Gets or sets the Group id.
        /// </summary>
        /// <value>The Group id to join.</value>
        [ApiMember(Name = "GroupId", Description = "Group Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string GroupId { get; set; }
    }

    [Route("/Syncplay/{SessionId}/LeaveGroup", "POST", Summary = "Leave joined Syncplay group")]
    [Authenticated]
    public class SyncplayLeaveGroup : IReturnVoid
    {
        [ApiMember(Name = "SessionId", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string SessionId { get; set; }
    }

    [Route("/Syncplay/{SessionId}/ListGroups", "POST", Summary = "List Syncplay groups playing same item")]
    [Authenticated]
    public class SyncplayListGroups : IReturnVoid
    {
        [ApiMember(Name = "SessionId", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string SessionId { get; set; }
    }

    [Route("/Syncplay/{SessionId}/PlayRequest", "POST", Summary = "Request play in Syncplay group")]
    [Authenticated]
    public class SyncplayPlayRequest : IReturnVoid
    {
        [ApiMember(Name = "SessionId", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string SessionId { get; set; }
    }

    [Route("/Syncplay/{SessionId}/PauseRequest", "POST", Summary = "Request pause in Syncplay group")]
    [Authenticated]
    public class SyncplayPauseRequest : IReturnVoid
    {
        [ApiMember(Name = "SessionId", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string SessionId { get; set; }
    }

    [Route("/Syncplay/{SessionId}/SeekRequest", "POST", Summary = "Request seek in Syncplay group")]
    [Authenticated]
    public class SyncplaySeekRequest : IReturnVoid
    {
        [ApiMember(Name = "SessionId", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string SessionId { get; set; }

        [ApiMember(Name = "PositionTicks", IsRequired = true, DataType = "long", ParameterType = "query", Verb = "POST")]
        public long PositionTicks { get; set; }
    }

    [Route("/Syncplay/{SessionId}/BufferingRequest", "POST", Summary = "Request group wait in Syncplay group while buffering")]
    [Authenticated]
    public class SyncplayBufferingRequest : IReturnVoid
    {
        [ApiMember(Name = "SessionId", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string SessionId { get; set; }

        [ApiMember(Name = "When", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string When { get; set; }

        [ApiMember(Name = "PositionTicks", IsRequired = true, DataType = "long", ParameterType = "query", Verb = "POST")]
        public long PositionTicks { get; set; }

        [ApiMember(Name = "Resume", IsRequired = true, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool Resume { get; set; }
    }

    [Route("/Syncplay/{SessionId}/KeepAlive", "POST", Summary = "Keep session alive")]
    [Authenticated]
    public class SyncplayKeepAlive : IReturnVoid
    {
        [ApiMember(Name = "SessionId", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string SessionId { get; set; }

        [ApiMember(Name = "Ping", IsRequired = true, DataType = "double", ParameterType = "query", Verb = "POST")]
        public double Ping { get; set; }
    }

    [Route("/Syncplay/{SessionId}/GetUtcTime", "POST", Summary = "Get UtcTime")]
    [Authenticated]
    public class SyncplayGetUtcTime : IReturnVoid
    {
        [ApiMember(Name = "SessionId", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string SessionId { get; set; }
    }

    /// <summary>
    /// Class SyncplayService.
    /// </summary>
    public class SyncplayService : BaseApiService
    {
        /// <summary>
        /// The session manager.
        /// </summary>
        private readonly ISessionManager _sessionManager;

        private readonly ISessionContext _sessionContext;

        /// <summary>
        /// The Syncplay manager.
        /// </summary>
        private readonly ISyncplayManager _syncplayManager;

        public SyncplayService(
            ILogger<SyncplayService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            ISessionManager sessionManager,
            ISessionContext sessionContext,
            ISyncplayManager syncplayManager)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _sessionManager = sessionManager;
            _sessionContext = sessionContext;
            _syncplayManager = syncplayManager;
        }

        /// <summary>
        /// Handles the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(SyncplayNewGroup request)
        {
            var currentSession = GetSession(_sessionContext);
            _syncplayManager.NewGroup(currentSession);
        }

        /// <summary>
        /// Handles the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(SyncplayJoinGroup request)
        {
            var currentSession = GetSession(_sessionContext);
            _syncplayManager.JoinGroup(currentSession, request.GroupId);
        }

        /// <summary>
        /// Handles the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(SyncplayLeaveGroup request)
        {
            var currentSession = GetSession(_sessionContext);
            _syncplayManager.LeaveGroup(currentSession);
        }

        /// <summary>
        /// Handles the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <value>The requested list of groups.</value>
        public List<GroupInfoView> Post(SyncplayListGroups request)
        {
            var currentSession = GetSession(_sessionContext);
            return _syncplayManager.ListGroups(currentSession);
        }

        /// <summary>
        /// Handles the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(SyncplayPlayRequest request)
        {
            var currentSession = GetSession(_sessionContext);
            var syncplayRequest = new SyncplayRequestInfo();
            syncplayRequest.Type = SyncplayRequestType.Play;
            _syncplayManager.HandleRequest(currentSession, syncplayRequest);
        }

        /// <summary>
        /// Handles the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(SyncplayPauseRequest request)
        {
            var currentSession = GetSession(_sessionContext);
            var syncplayRequest = new SyncplayRequestInfo();
            syncplayRequest.Type = SyncplayRequestType.Pause;
            _syncplayManager.HandleRequest(currentSession, syncplayRequest);
        }

        /// <summary>
        /// Handles the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(SyncplaySeekRequest request)
        {
            var currentSession = GetSession(_sessionContext);
            var syncplayRequest = new SyncplayRequestInfo();
            syncplayRequest.Type = SyncplayRequestType.Seek;
            syncplayRequest.PositionTicks = request.PositionTicks;
            _syncplayManager.HandleRequest(currentSession, syncplayRequest);
        }

        /// <summary>
        /// Handles the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(SyncplayBufferingRequest request)
        {
            var currentSession = GetSession(_sessionContext);
            var syncplayRequest = new SyncplayRequestInfo();
            syncplayRequest.Type = request.Resume ? SyncplayRequestType.BufferingComplete : SyncplayRequestType.Buffering;
            syncplayRequest.When = DateTime.Parse(request.When);
            syncplayRequest.PositionTicks = request.PositionTicks;
            _syncplayManager.HandleRequest(currentSession, syncplayRequest);
        }

        /// <summary>
        /// Handles the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(SyncplayKeepAlive request)
        {
            var currentSession = GetSession(_sessionContext);
            var syncplayRequest = new SyncplayRequestInfo();
            syncplayRequest.Type = SyncplayRequestType.KeepAlive;
            syncplayRequest.Ping = Convert.ToInt64(request.Ping);
            _syncplayManager.HandleRequest(currentSession, syncplayRequest);
        }

        /// <summary>
        /// Handles the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <value>The current UTC time.</value>
        public string Post(SyncplayGetUtcTime request)
        {
            return DateTime.UtcNow.ToUniversalTime().ToString("o");
        }
    }
}
