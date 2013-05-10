using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Session;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetSessions
    /// </summary>
    [Route("/Sessions", "GET")]
    [Api(("Gets a list of sessions"))]
    public class GetSessions : IReturn<List<SessionInfoDto>>
    {
        [ApiMember(Name = "SupportsRemoteControl", Description = "Optional. Filter by sessions that can be remote controlled.", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? SupportsRemoteControl { get; set; }
    }

    [Route("/Sessions/{Id}/Viewing", "POST")]
    [Api(("Instructs a session to browse to an item or view"))]
    public class BrowseTo : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid Id { get; set; }

        /// <summary>
        /// Artist, Genre, Studio, Person, or any kind of BaseItem
        /// </summary>
        /// <value>The type of the item.</value>
        [ApiMember(Name = "ItemType", Description = "Only required if the item is an Artist, Genre, Studio, or Person.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string ItemType { get; set; }

        /// <summary>
        /// Artist name, genre name, item Id, etc
        /// </summary>
        /// <value>The item identifier.</value>
        [ApiMember(Name = "ItemIdentifier", Description = "The Id of the item, unless it is an Artist, Genre, Studio, or Person, in which case it should be the name.", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string ItemIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the context (Movies, Music, TvShows, etc)
        /// Applicable to genres, studios and persons only because the context of items and artists can be inferred.
        /// This is optional to supply and clients are free to ignore it.
        /// </summary>
        /// <value>The context.</value>
        [ApiMember(Name = "Context", Description = "The navigation context for the client (movies, music, tvshows, games etc). This is optional to supply and clients are free to ignore it.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Context { get; set; }
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

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionsService"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        public SessionsService(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
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
                result = result.Where(i => i.IsActive == request.SupportsRemoteControl.Value);
            }

            return ToOptimizedResult(result.Select(SessionInfoDtoBuilder.GetSessionInfoDto).ToList());
        }

        public void Post(BrowseTo request)
        {
            var session = _sessionManager.Sessions.FirstOrDefault(i => i.Id == request.Id);

            if (session == null)
            {
                throw new ResourceNotFoundException(string.Format("Session {0} not found.", request.Id));
            }

            session.WebSocket.SendAsync(new WebSocketMessage<BrowseTo>
            {
                MessageType = "Browse",
                Data = request

            }, CancellationToken.None);
        }
    }
}
