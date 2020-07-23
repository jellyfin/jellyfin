using System.Threading;
using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.SyncPlay;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.SyncPlay
{
    [Route("/SyncPlay/New", "POST", Summary = "Create a new SyncPlay group")]
    [Authenticated]
    public class SyncPlayNew : IReturnVoid
    {
    }

    [Route("/SyncPlay/Join", "POST", Summary = "Join an existing SyncPlay group")]
    [Authenticated]
    public class SyncPlayJoin : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the Group id.
        /// </summary>
        /// <value>The Group id to join.</value>
        [ApiMember(Name = "GroupId", Description = "Group Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string GroupId { get; set; }
    }

    [Route("/SyncPlay/Leave", "POST", Summary = "Leave joined SyncPlay group")]
    [Authenticated]
    public class SyncPlayLeave : IReturnVoid
    {
    }

    [Route("/SyncPlay/List", "GET", Summary = "List SyncPlay groups")]
    [Authenticated]
    public class SyncPlayList : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the filter item id.
        /// </summary>
        /// <value>The filter item id.</value>
        [ApiMember(Name = "FilterItemId", Description = "Filter by item id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string FilterItemId { get; set; }
    }

    [Route("/SyncPlay/Play", "POST", Summary = "Request play in SyncPlay group")]
    [Authenticated]
    public class SyncPlayPlay : IReturnVoid
    {
    }

    [Route("/SyncPlay/Pause", "POST", Summary = "Request pause in SyncPlay group")]
    [Authenticated]
    public class SyncPlayPause : IReturnVoid
    {
    }

    [Route("/SyncPlay/Seek", "POST", Summary = "Request seek in SyncPlay group")]
    [Authenticated]
    public class SyncPlaySeek : IReturnVoid
    {
        [ApiMember(Name = "PositionTicks", IsRequired = true, DataType = "long", ParameterType = "query", Verb = "POST")]
        public long PositionTicks { get; set; }
    }

    [Route("/SyncPlay/Buffering", "POST", Summary = "Request group wait in SyncPlay group while buffering")]
    [Authenticated]
    public class SyncPlayBuffering : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the date used to pin PositionTicks in time.
        /// </summary>
        /// <value>The date related to PositionTicks.</value>
        [ApiMember(Name = "When", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string When { get; set; }

        [ApiMember(Name = "PositionTicks", IsRequired = true, DataType = "long", ParameterType = "query", Verb = "POST")]
        public long PositionTicks { get; set; }

        /// <summary>
        /// Gets or sets whether this is a buffering or a ready request.
        /// </summary>
        /// <value><c>true</c> if buffering is complete; <c>false</c> otherwise.</value>
        [ApiMember(Name = "BufferingDone", IsRequired = true, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool BufferingDone { get; set; }
    }

    [Route("/SyncPlay/Ping", "POST", Summary = "Update session ping")]
    [Authenticated]
    public class SyncPlayPing : IReturnVoid
    {
        [ApiMember(Name = "Ping", IsRequired = true, DataType = "double", ParameterType = "query", Verb = "POST")]
        public double Ping { get; set; }
    }

    /// <summary>
    /// Class SyncPlayService.
    /// </summary>
    public class SyncPlayService : BaseApiService
    {
        /// <summary>
        /// The session context.
        /// </summary>
        private readonly ISessionContext _sessionContext;

        /// <summary>
        /// The SyncPlay manager.
        /// </summary>
        private readonly ISyncPlayManager _syncPlayManager;

        public SyncPlayService(
            ILogger<SyncPlayService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            ISessionContext sessionContext,
            ISyncPlayManager syncPlayManager)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _sessionContext = sessionContext;
            _syncPlayManager = syncPlayManager;
        }

        /// <summary>
        /// Handles the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(SyncPlayNew request)
        {
            var currentSession = GetSession(_sessionContext);
            _syncPlayManager.NewGroup(currentSession, CancellationToken.None);
        }

        /// <summary>
        /// Handles the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(SyncPlayJoin request)
        {
            var currentSession = GetSession(_sessionContext);

            Guid groupId;
            if (!Guid.TryParse(request.GroupId, out groupId))
            {
                Logger.LogError("JoinGroup: {0} is not a valid format for GroupId. Ignoring request.", request.GroupId);
                return;
            }

            var joinRequest = new JoinGroupRequest()
            {
                GroupId = groupId
            };

            _syncPlayManager.JoinGroup(currentSession, groupId, joinRequest, CancellationToken.None);
        }

        /// <summary>
        /// Handles the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(SyncPlayLeave request)
        {
            var currentSession = GetSession(_sessionContext);
            _syncPlayManager.LeaveGroup(currentSession, CancellationToken.None);
        }

        /// <summary>
        /// Handles the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <value>The requested list of groups.</value>
        public List<GroupInfoView> Get(SyncPlayList request)
        {
            var currentSession = GetSession(_sessionContext);
            var filterItemId = Guid.Empty;

            if (!string.IsNullOrEmpty(request.FilterItemId) && !Guid.TryParse(request.FilterItemId, out filterItemId))
            {
                Logger.LogWarning("ListGroups: {0} is not a valid format for FilterItemId. Ignoring filter.", request.FilterItemId);
            }

            return _syncPlayManager.ListGroups(currentSession, filterItemId);
        }

        /// <summary>
        /// Handles the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(SyncPlayPlay request)
        {
            var currentSession = GetSession(_sessionContext);
            var syncPlayRequest = new PlaybackRequest()
            {
                Type = PlaybackRequestType.Play
            };
            _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
        }

        /// <summary>
        /// Handles the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(SyncPlayPause request)
        {
            var currentSession = GetSession(_sessionContext);
            var syncPlayRequest = new PlaybackRequest()
            {
                Type = PlaybackRequestType.Pause
            };
            _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
        }

        /// <summary>
        /// Handles the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(SyncPlaySeek request)
        {
            var currentSession = GetSession(_sessionContext);
            var syncPlayRequest = new PlaybackRequest()
            {
                Type = PlaybackRequestType.Seek,
                PositionTicks = request.PositionTicks
            };
            _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
        }

        /// <summary>
        /// Handles the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(SyncPlayBuffering request)
        {
            var currentSession = GetSession(_sessionContext);
            var syncPlayRequest = new PlaybackRequest()
            {
                Type = request.BufferingDone ? PlaybackRequestType.Ready : PlaybackRequestType.Buffer,
                When = DateTime.Parse(request.When),
                PositionTicks = request.PositionTicks
            };
            _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
        }

        /// <summary>
        /// Handles the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(SyncPlayPing request)
        {
            var currentSession = GetSession(_sessionContext);
            var syncPlayRequest = new PlaybackRequest()
            {
                Type = PlaybackRequestType.Ping,
                Ping = Convert.ToInt64(request.Ping)
            };
            _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
        }
    }
}
