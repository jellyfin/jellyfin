using System.ComponentModel;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System;
using MediaBrowser.Model.Text;
using SocketHttpListener.Net.WebSockets;

namespace SocketHttpListener.Net
{
    public sealed unsafe partial class HttpListenerContext
    {
        private HttpConnection _connection;

        internal HttpListenerContext(HttpConnection connection, ITextEncoding textEncoding)
        {
            _connection = connection;
            _response = new HttpListenerResponse(this, textEncoding);
            Request = new HttpListenerRequest(this);
            ErrorStatus = 400;
        }

        internal int ErrorStatus { get; set; }

        internal string ErrorMessage { get; set; }

        internal bool HaveError => ErrorMessage != null;

        internal HttpConnection Connection => _connection;

        internal void ParseAuthentication(System.Net.AuthenticationSchemes expectedSchemes)
        {
            if (expectedSchemes == System.Net.AuthenticationSchemes.Anonymous)
                return;

            string header = Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(header))
                return;

            if (IsBasicHeader(header))
            {
                _user = ParseBasicAuthentication(header.Substring(AuthenticationTypes.Basic.Length + 1));
            }
        }

        internal IPrincipal ParseBasicAuthentication(string authData) =>
            TryParseBasicAuth(authData, out HttpStatusCode errorCode, out string username, out string password) ?
                new GenericPrincipal(new HttpListenerBasicIdentity(username, password), Array.Empty<string>()) :
                null;

        internal static bool IsBasicHeader(string header) =>
            header.Length >= 6 &&
            header[5] == ' ' &&
            string.Compare(header, 0, AuthenticationTypes.Basic, 0, 5, StringComparison.OrdinalIgnoreCase) == 0;

        internal static bool TryParseBasicAuth(string headerValue, out HttpStatusCode errorCode, out string username, out string password)
        {
            errorCode = HttpStatusCode.OK;
            username = password = null;
            try
            {
                if (string.IsNullOrWhiteSpace(headerValue))
                {
                    return false;
                }

                string authString = Encoding.UTF8.GetString(Convert.FromBase64String(headerValue));
                int colonPos = authString.IndexOf(':');
                if (colonPos < 0)
                {
                    // username must be at least 1 char
                    errorCode = HttpStatusCode.BadRequest;
                    return false;
                }

                username = authString.Substring(0, colonPos);
                password = authString.Substring(colonPos + 1);
                return true;
            }
            catch
            {
                errorCode = HttpStatusCode.InternalServerError;
                return false;
            }
        }

        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol, int receiveBufferSize, TimeSpan keepAliveInterval)
        {
            return HttpWebSocket.AcceptWebSocketAsyncCore(this, subProtocol, receiveBufferSize, keepAliveInterval);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol, int receiveBufferSize, TimeSpan keepAliveInterval, ArraySegment<byte> internalBuffer)
        {
            WebSocketValidate.ValidateArraySegment(internalBuffer, nameof(internalBuffer));
            HttpWebSocket.ValidateOptions(subProtocol, receiveBufferSize, HttpWebSocket.MinSendBufferSize, keepAliveInterval);
            return HttpWebSocket.AcceptWebSocketAsyncCore(this, subProtocol, receiveBufferSize, keepAliveInterval, internalBuffer);
        }
    }
}
