using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Security.Principal;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Services;

namespace SocketHttpListener.Net.WebSockets
{
    public abstract class WebSocketContext
    {
        public abstract Uri RequestUri { get; }
        public abstract QueryParamCollection Headers { get; }
        public abstract string Origin { get; }
        public abstract IEnumerable<string> SecWebSocketProtocols { get; }
        public abstract string SecWebSocketVersion { get; }
        public abstract string SecWebSocketKey { get; }
        public abstract CookieCollection CookieCollection { get; }
        public abstract IPrincipal User { get; }
        public abstract bool IsAuthenticated { get; }
        public abstract bool IsLocal { get; }
        public abstract bool IsSecureConnection { get; }
        public abstract WebSocket WebSocket { get; }
    }
}
