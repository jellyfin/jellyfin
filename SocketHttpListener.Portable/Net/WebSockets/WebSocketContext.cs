using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Security.Principal;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Services;

namespace SocketHttpListener.Net.WebSockets
{
    /// <summary>
    /// Exposes the properties used to access the information in a WebSocket connection request.
    /// </summary>
    /// <remarks>
    /// The WebSocketContext class is an abstract class.
    /// </remarks>
    public abstract class WebSocketContext
    {
        #region Protected Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketContext"/> class.
        /// </summary>
        protected WebSocketContext()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the HTTP cookies included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.CookieCollection"/> that contains the cookies.
        /// </value>
        public abstract CookieCollection CookieCollection { get; }

        /// <summary>
        /// Gets the HTTP headers included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="QueryParamCollection"/> that contains the headers.
        /// </value>
        public abstract QueryParamCollection Headers { get; }

        /// <summary>
        /// Gets the value of the Host header included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the value of the Host header.
        /// </value>
        public abstract string Host { get; }

        /// <summary>
        /// Gets a value indicating whether the client is authenticated.
        /// </summary>
        /// <value>
        /// <c>true</c> if the client is authenticated; otherwise, <c>false</c>.
        /// </value>
        public abstract bool IsAuthenticated { get; }

        /// <summary>
        /// Gets a value indicating whether the client connected from the local computer.
        /// </summary>
        /// <value>
        /// <c>true</c> if the client connected from the local computer; otherwise, <c>false</c>.
        /// </value>
        public abstract bool IsLocal { get; }

        /// <summary>
        /// Gets a value indicating whether the WebSocket connection is secured.
        /// </summary>
        /// <value>
        /// <c>true</c> if the connection is secured; otherwise, <c>false</c>.
        /// </value>
        public abstract bool IsSecureConnection { get; }

        /// <summary>
        /// Gets a value indicating whether the request is a WebSocket connection request.
        /// </summary>
        /// <value>
        /// <c>true</c> if the request is a WebSocket connection request; otherwise, <c>false</c>.
        /// </value>
        public abstract bool IsWebSocketRequest { get; }

        /// <summary>
        /// Gets the value of the Origin header included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the value of the Origin header.
        /// </value>
        public abstract string Origin { get; }

        /// <summary>
        /// Gets the query string included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="QueryParamCollection"/> that contains the query string parameters.
        /// </value>
        public abstract QueryParamCollection QueryString { get; }

        /// <summary>
        /// Gets the URI requested by the client.
        /// </summary>
        /// <value>
        /// A <see cref="Uri"/> that represents the requested URI.
        /// </value>
        public abstract Uri RequestUri { get; }

        /// <summary>
        /// Gets the value of the Sec-WebSocket-Key header included in the request.
        /// </summary>
        /// <remarks>
        /// This property provides a part of the information used by the server to prove that it
        /// received a valid WebSocket connection request.
        /// </remarks>
        /// <value>
        /// A <see cref="string"/> that represents the value of the Sec-WebSocket-Key header.
        /// </value>
        public abstract string SecWebSocketKey { get; }

        /// <summary>
        /// Gets the values of the Sec-WebSocket-Protocol header included in the request.
        /// </summary>
        /// <remarks>
        /// This property represents the subprotocols requested by the client.
        /// </remarks>
        /// <value>
        /// An <see cref="T:System.Collections.Generic.IEnumerable{string}"/> instance that provides
        /// an enumerator which supports the iteration over the values of the Sec-WebSocket-Protocol
        /// header.
        /// </value>
        public abstract IEnumerable<string> SecWebSocketProtocols { get; }

        /// <summary>
        /// Gets the value of the Sec-WebSocket-Version header included in the request.
        /// </summary>
        /// <remarks>
        /// This property represents the WebSocket protocol version.
        /// </remarks>
        /// <value>
        /// A <see cref="string"/> that represents the value of the Sec-WebSocket-Version header.
        /// </value>
        public abstract string SecWebSocketVersion { get; }

        /// <summary>
        /// Gets the server endpoint as an IP address and a port number.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.IPEndPoint"/> that represents the server endpoint.
        /// </value>
        public abstract IpEndPointInfo ServerEndPoint { get; }

        /// <summary>
        /// Gets the client information (identity, authentication, and security roles).
        /// </summary>
        /// <value>
        /// A <see cref="IPrincipal"/> that represents the client information.
        /// </value>
        public abstract IPrincipal User { get; }

        /// <summary>
        /// Gets the client endpoint as an IP address and a port number.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.IPEndPoint"/> that represents the client endpoint.
        /// </value>
        public abstract IpEndPointInfo UserEndPoint { get; }

        /// <summary>
        /// Gets the <see cref="SocketHttpListener.WebSocket"/> instance used for two-way communication
        /// between client and server.
        /// </summary>
        /// <value>
        /// A <see cref="SocketHttpListener.WebSocket"/>.
        /// </value>
        public abstract WebSocket WebSocket { get; }

        #endregion
    }
}
