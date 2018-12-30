using System;
using System.Net;
using System.Security.Principal;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Text;
using SocketHttpListener.Net.WebSockets;
using System.Threading.Tasks;

namespace SocketHttpListener.Net
{
    public sealed unsafe partial class HttpListenerContext
    {
        private HttpListenerResponse _response;
        private IPrincipal _user;

        public HttpListenerRequest Request { get; }

        public IPrincipal User => _user;

        // This can be used to cache the results of HttpListener.AuthenticationSchemeSelectorDelegate.
        internal AuthenticationSchemes AuthenticationSchemes { get; set; }

        public HttpListenerResponse Response
        {
            get
            {
                return _response;
            }
        }

        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol)
        {
            return AcceptWebSocketAsync(subProtocol, HttpWebSocket.DefaultReceiveBufferSize, WebSocket.DefaultKeepAliveInterval);
        }

        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol, TimeSpan keepAliveInterval)
        {
            return AcceptWebSocketAsync(subProtocol, HttpWebSocket.DefaultReceiveBufferSize, keepAliveInterval);
        }
    }

    public class GenericPrincipal : IPrincipal
    {
        private IIdentity m_identity;
        private string[] m_roles;

        public GenericPrincipal(IIdentity identity, string[] roles)
        {
            if (identity == null)
                throw new ArgumentNullException("identity");

            m_identity = identity;
            if (roles != null)
            {
                m_roles = new string[roles.Length];
                for (int i = 0; i < roles.Length; ++i)
                {
                    m_roles[i] = roles[i];
                }
            }
            else
            {
                m_roles = null;
            }
        }

        public virtual IIdentity Identity
        {
            get
            {
                return m_identity;
            }
        }

        public virtual bool IsInRole(string role)
        {
            if (role == null || m_roles == null)
                return false;

            for (int i = 0; i < m_roles.Length; ++i)
            {
                if (m_roles[i] != null && String.Compare(m_roles[i], role, StringComparison.OrdinalIgnoreCase) == 0)
                    return true;
            }
            return false;
        }
    }
}
