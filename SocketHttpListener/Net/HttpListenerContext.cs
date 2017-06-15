using System;
using System.Net;
using System.Security.Principal;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Text;
using SocketHttpListener.Net.WebSockets;
using SocketHttpListener.Primitives;

namespace SocketHttpListener.Net
{
    public sealed class HttpListenerContext
    {
        HttpListenerRequest request;
        HttpListenerResponse response;
        IPrincipal user;
        HttpConnection cnc;
        string error;
        int err_status = 400;
        private readonly ICryptoProvider _cryptoProvider;
        private readonly IMemoryStreamFactory _memoryStreamFactory;
        private readonly ITextEncoding _textEncoding;

        internal HttpListenerContext(HttpConnection cnc, ILogger logger, ICryptoProvider cryptoProvider, IMemoryStreamFactory memoryStreamFactory, ITextEncoding textEncoding, IFileSystem fileSystem)
        {
            this.cnc = cnc;
            _cryptoProvider = cryptoProvider;
            _memoryStreamFactory = memoryStreamFactory;
            _textEncoding = textEncoding;
            request = new HttpListenerRequest(this, _textEncoding);
            response = new HttpListenerResponse(this, _textEncoding);
        }

        internal int ErrorStatus
        {
            get { return err_status; }
            set { err_status = value; }
        }

        internal string ErrorMessage
        {
            get { return error; }
            set { error = value; }
        }

        internal bool HaveError
        {
            get { return (error != null); }
        }

        internal HttpConnection Connection
        {
            get { return cnc; }
        }

        public HttpListenerRequest Request
        {
            get { return request; }
        }

        public HttpListenerResponse Response
        {
            get { return response; }
        }

        public IPrincipal User
        {
            get { return user; }
        }

        internal void ParseAuthentication(AuthenticationSchemes expectedSchemes)
        {
            if (expectedSchemes == AuthenticationSchemes.Anonymous)
                return;

            // TODO: Handle NTLM/Digest modes
            string header = request.Headers["Authorization"];
            if (header == null || header.Length < 2)
                return;

            string[] authenticationData = header.Split(new char[] { ' ' }, 2);
            if (string.Equals(authenticationData[0], "basic", StringComparison.OrdinalIgnoreCase))
            {
                user = ParseBasicAuthentication(authenticationData[1]);
            }
            // TODO: throw if malformed -> 400 bad request
        }

        internal IPrincipal ParseBasicAuthentication(string authData)
        {
            try
            {
                // Basic AUTH Data is a formatted Base64 String
                //string domain = null;
                string user = null;
                string password = null;
                int pos = -1;
                var authDataBytes = Convert.FromBase64String(authData);
                string authString = _textEncoding.GetDefaultEncoding().GetString(authDataBytes, 0, authDataBytes.Length);

                // The format is DOMAIN\username:password
                // Domain is optional

                pos = authString.IndexOf(':');

                // parse the password off the end
                password = authString.Substring(pos + 1);

                // discard the password
                authString = authString.Substring(0, pos);

                // check if there is a domain
                pos = authString.IndexOf('\\');

                if (pos > 0)
                {
                    //domain = authString.Substring (0, pos);
                    user = authString.Substring(pos);
                }
                else
                {
                    user = authString;
                }

                HttpListenerBasicIdentity identity = new HttpListenerBasicIdentity(user, password);
                // TODO: What are the roles MS sets
                return new GenericPrincipal(identity, new string[0]);
            }
            catch (Exception)
            {
                // Invalid auth data is swallowed silently
                return null;
            }
        }

        public HttpListenerWebSocketContext AcceptWebSocket(string protocol)
        {
            if (protocol != null)
            {
                if (protocol.Length == 0)
                    throw new ArgumentException("An empty string.", "protocol");

                if (!protocol.IsToken())
                    throw new ArgumentException("Contains an invalid character.", "protocol");
            }

            return new HttpListenerWebSocketContext(this, protocol, _cryptoProvider, _memoryStreamFactory);
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
