using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Security.Principal;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Services;
using SocketHttpListener.Primitives;

namespace SocketHttpListener.Net.WebSockets
{
    public class HttpListenerWebSocketContext : WebSocketContext
    {
        private readonly Uri _requestUri;
        private readonly QueryParamCollection _headers;
        private readonly CookieCollection _cookieCollection;
        private readonly IPrincipal _user;
        private readonly bool _isAuthenticated;
        private readonly bool _isLocal;
        private readonly bool _isSecureConnection;

        private readonly string _origin;
        private readonly IEnumerable<string> _secWebSocketProtocols;
        private readonly string _secWebSocketVersion;
        private readonly string _secWebSocketKey;

        private readonly WebSocket _webSocket;

        internal HttpListenerWebSocketContext(
            Uri requestUri,
            QueryParamCollection headers,
            CookieCollection cookieCollection,
            IPrincipal user,
            bool isAuthenticated,
            bool isLocal,
            bool isSecureConnection,
            string origin,
            IEnumerable<string> secWebSocketProtocols,
            string secWebSocketVersion,
            string secWebSocketKey,
            WebSocket webSocket)
        {
            _cookieCollection = new CookieCollection();
            _cookieCollection.Add(cookieCollection);

            //_headers = new NameValueCollection(headers);
            _headers = headers;
            _user = CopyPrincipal(user);

            _requestUri = requestUri;
            _isAuthenticated = isAuthenticated;
            _isLocal = isLocal;
            _isSecureConnection = isSecureConnection;
            _origin = origin;
            _secWebSocketProtocols = secWebSocketProtocols;
            _secWebSocketVersion = secWebSocketVersion;
            _secWebSocketKey = secWebSocketKey;
            _webSocket = webSocket;
        }

        public override Uri RequestUri => _requestUri;

        public override QueryParamCollection Headers => _headers;

        public override string Origin => _origin;

        public override IEnumerable<string> SecWebSocketProtocols => _secWebSocketProtocols;

        public override string SecWebSocketVersion => _secWebSocketVersion;

        public override string SecWebSocketKey => _secWebSocketKey;

        public override CookieCollection CookieCollection => _cookieCollection;

        public override IPrincipal User => _user;

        public override bool IsAuthenticated => _isAuthenticated;

        public override bool IsLocal => _isLocal;

        public override bool IsSecureConnection => _isSecureConnection;

        public override WebSocket WebSocket => _webSocket;

        private static IPrincipal CopyPrincipal(IPrincipal user)
        {
            if (user != null)
            {
                throw new NotImplementedException();
            }

            return null;
        }
    }
}
