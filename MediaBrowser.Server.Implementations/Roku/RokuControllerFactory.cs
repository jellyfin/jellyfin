using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Server.Implementations.Roku
{
    public class RokuControllerFactory : ISessionControllerFactory
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _json;
        private readonly IServerApplicationHost _appHost;

        public RokuControllerFactory(IHttpClient httpClient, IJsonSerializer json, IServerApplicationHost appHost)
        {
            _httpClient = httpClient;
            _json = json;
            _appHost = appHost;
        }

        public ISessionController GetSessionController(SessionInfo session)
        {
            if (string.Equals(session.Client, "roku", StringComparison.OrdinalIgnoreCase))
            {
                session.PlayableMediaTypes = new List<string> { MediaType.Video, MediaType.Audio };
                session.SupportsFullscreenToggle = false;

                return new RokuSessionController(_httpClient, _json, _appHost, session);
            }

            return null;
        }
    }
}
