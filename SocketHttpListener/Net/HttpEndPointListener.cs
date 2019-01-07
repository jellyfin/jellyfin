using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Text;
using SocketHttpListener.Primitives;
using ProtocolType = MediaBrowser.Model.Net.ProtocolType;
using SocketType = MediaBrowser.Model.Net.SocketType;
using System.Threading.Tasks;

namespace SocketHttpListener.Net
{
    internal sealed class HttpEndPointListener
    {
        private HttpListener _listener;
        private IPEndPoint _endpoint;
        private Socket _socket;
        private Dictionary<ListenerPrefix, HttpListener> _prefixes;
        private List<ListenerPrefix> _unhandledPrefixes; // host = '*'
        private List<ListenerPrefix> _allPrefixes;       // host = '+'
        private X509Certificate _cert;
        private bool _secure;
        private Dictionary<HttpConnection, HttpConnection> _unregisteredConnections;

        private readonly ILogger _logger;
        private bool _closed;
        private bool _enableDualMode;
        private readonly ICryptoProvider _cryptoProvider;
        private readonly ISocketFactory _socketFactory;
        private readonly ITextEncoding _textEncoding;
        private readonly IStreamHelper _streamHelper;
        private readonly IFileSystem _fileSystem;
        private readonly IEnvironmentInfo _environment;

        public HttpEndPointListener(HttpListener listener, IPAddress addr, int port, bool secure, X509Certificate cert, ILogger logger, ICryptoProvider cryptoProvider, ISocketFactory socketFactory, IStreamHelper streamHelper, ITextEncoding textEncoding, IFileSystem fileSystem, IEnvironmentInfo environment)
        {
            this._listener = listener;
            _logger = logger;
            _cryptoProvider = cryptoProvider;
            _socketFactory = socketFactory;
            _streamHelper = streamHelper;
            _textEncoding = textEncoding;
            _fileSystem = fileSystem;
            _environment = environment;

            this._secure = secure;
            this._cert = cert;

            _enableDualMode = addr.Equals(IPAddress.IPv6Any);
            _endpoint = new IPEndPoint(addr, port);

            _prefixes = new Dictionary<ListenerPrefix, HttpListener>();
            _unregisteredConnections = new Dictionary<HttpConnection, HttpConnection>();

            CreateSocket();
        }

        internal HttpListener Listener
        {
            get
            {
                return _listener;
            }
        }

        private void CreateSocket()
        {
            try
            {
                _socket = CreateSocket(_endpoint.Address.AddressFamily, _enableDualMode);
            }
            catch (SocketCreateException ex)
            {
                if (_enableDualMode && _endpoint.Address.Equals(IPAddress.IPv6Any) &&
                    (string.Equals(ex.ErrorCode, "AddressFamilyNotSupported", StringComparison.OrdinalIgnoreCase) ||
                    // mono 4.8.1 and lower on bsd is throwing this
                    string.Equals(ex.ErrorCode, "ProtocolNotSupported", StringComparison.OrdinalIgnoreCase) ||
                    // mono 5.2 on bsd is throwing this
                    string.Equals(ex.ErrorCode, "OperationNotSupported", StringComparison.OrdinalIgnoreCase)))
                {
                    _endpoint = new IPEndPoint(IPAddress.Any, _endpoint.Port);
                    _enableDualMode = false;
                    _socket = CreateSocket(_endpoint.Address.AddressFamily, _enableDualMode);
                }
                else
                {
                    throw;
                }
            }

            try
            {
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            }
            catch (SocketException)
            {
                // This is not supported on all operating systems (qnap)
            }

            _socket.Bind(_endpoint);

            // This is the number TcpListener uses.
            _socket.Listen(2147483647);

            Accept();

            _closed = false;
        }

        private void Accept()
        {
            var acceptEventArg = new SocketAsyncEventArgs();
            acceptEventArg.UserToken = this;
            acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnAccept);

            Accept(acceptEventArg);
        }

        private static void TryCloseAndDispose(Socket socket)
        {
            try
            {
                using (socket)
                {
                    socket.Close();
                }
            }
            catch
            {

            }
        }

        private static void TryClose(Socket socket)
        {
            try
            {
                socket.Close();
            }
            catch
            {

            }
        }

        private void Accept(SocketAsyncEventArgs acceptEventArg)
        {
            // acceptSocket must be cleared since the context object is being reused
            acceptEventArg.AcceptSocket = null;

            try
            {
                bool willRaiseEvent = _socket.AcceptAsync(acceptEventArg);

                if (!willRaiseEvent)
                {
                    ProcessAccept(acceptEventArg);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                HttpEndPointListener epl = (HttpEndPointListener)acceptEventArg.UserToken;

                epl._logger.LogError(ex, "Error in socket.AcceptAsync");
            }
        }

        // This method is the callback method associated with Socket.AcceptAsync  
        // operations and is invoked when an accept operation is complete 
        // 
        private static void OnAccept(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private static async void ProcessAccept(SocketAsyncEventArgs args)
        {
            HttpEndPointListener epl = (HttpEndPointListener)args.UserToken;

            if (epl._closed)
            {
                return;
            }

            // http://msdn.microsoft.com/en-us/library/system.net.sockets.acceptSocket.acceptasync%28v=vs.110%29.aspx
            // Under certain conditions ConnectionReset can occur
            // Need to attept to re-accept
            var socketError = args.SocketError;
            var accepted = args.AcceptSocket;

            epl.Accept(args);

            if (socketError == SocketError.ConnectionReset)
            {
                epl._logger.LogError("SocketError.ConnectionReset reported. Attempting to re-accept.");
                return;
            }

            if(accepted == null)
            {
                return;
            }

            if (epl._secure && epl._cert == null)
            {
                TryClose(accepted);
                return;
            }

            try
            {
                var remoteEndPointString = accepted.RemoteEndPoint == null ? string.Empty : accepted.RemoteEndPoint.ToString();
                var localEndPointString = accepted.LocalEndPoint == null ? string.Empty : accepted.LocalEndPoint.ToString();
                //_logger.LogInformation("HttpEndPointListener Accepting connection from {0} to {1} secure connection requested: {2}", remoteEndPointString, localEndPointString, _secure);

                HttpConnection conn = new HttpConnection(epl._logger, accepted, epl, epl._secure, epl._cert, epl._cryptoProvider, epl._streamHelper, epl._textEncoding, epl._fileSystem, epl._environment);

                await conn.Init().ConfigureAwait(false);

                //_logger.LogDebug("Adding unregistered connection to {0}. Id: {1}", accepted.RemoteEndPoint, connectionId);
                lock (epl._unregisteredConnections)
                {
                    epl._unregisteredConnections[conn] = conn;
                }
                conn.BeginReadRequest();
            }
            catch (Exception ex)
            {
                epl._logger.LogError(ex, "Error in ProcessAccept");

                TryClose(accepted);
                epl.Accept();
                return;
            }
        }

        private Socket CreateSocket(AddressFamily addressFamily, bool dualMode)
        {
            try
            {
                var socket = new Socket(addressFamily, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);

                if (dualMode)
                {
                    socket.DualMode = true;
                }

                return socket;
            }
            catch (SocketException ex)
            {
                throw new SocketCreateException(ex.SocketErrorCode.ToString(), ex);
            }
            catch (ArgumentException ex)
            {
                if (dualMode)
                {
                    // Mono for BSD incorrectly throws ArgumentException instead of SocketException
                    throw new SocketCreateException("AddressFamilyNotSupported", ex);
                }
                else
                {
                    throw;
                }
            }
        }

        internal void RemoveConnection(HttpConnection conn)
        {
            lock (_unregisteredConnections)
            {
                _unregisteredConnections.Remove(conn);
            }
        }

        public bool BindContext(HttpListenerContext context)
        {
            HttpListenerRequest req = context.Request;
            ListenerPrefix prefix;
            HttpListener listener = SearchListener(req.Url, out prefix);
            if (listener == null)
                return false;

            context.Connection.Prefix = prefix;
            return true;
        }

        public void UnbindContext(HttpListenerContext context)
        {
            if (context == null || context.Request == null)
                return;

            _listener.UnregisterContext(context);
        }

        private HttpListener SearchListener(Uri uri, out ListenerPrefix prefix)
        {
            prefix = null;
            if (uri == null)
                return null;

            string host = uri.Host;
            int port = uri.Port;
            string path = WebUtility.UrlDecode(uri.AbsolutePath);
            string pathSlash = path[path.Length - 1] == '/' ? path : path + "/";

            HttpListener bestMatch = null;
            int bestLength = -1;

            if (host != null && host != "")
            {
                Dictionary<ListenerPrefix, HttpListener> localPrefixes = _prefixes;
                foreach (ListenerPrefix p in localPrefixes.Keys)
                {
                    string ppath = p.Path;
                    if (ppath.Length < bestLength)
                        continue;

                    if (p.Host != host || p.Port != port)
                        continue;

                    if (path.StartsWith(ppath) || pathSlash.StartsWith(ppath))
                    {
                        bestLength = ppath.Length;
                        bestMatch = localPrefixes[p];
                        prefix = p;
                    }
                }
                if (bestLength != -1)
                    return bestMatch;
            }

            List<ListenerPrefix> list = _unhandledPrefixes;
            bestMatch = MatchFromList(host, path, list, out prefix);

            if (path != pathSlash && bestMatch == null)
                bestMatch = MatchFromList(host, pathSlash, list, out prefix);

            if (bestMatch != null)
                return bestMatch;

            list = _allPrefixes;
            bestMatch = MatchFromList(host, path, list, out prefix);

            if (path != pathSlash && bestMatch == null)
                bestMatch = MatchFromList(host, pathSlash, list, out prefix);

            if (bestMatch != null)
                return bestMatch;

            return null;
        }

        private HttpListener MatchFromList(string host, string path, List<ListenerPrefix> list, out ListenerPrefix prefix)
        {
            prefix = null;
            if (list == null)
                return null;

            HttpListener bestMatch = null;
            int bestLength = -1;

            foreach (ListenerPrefix p in list)
            {
                string ppath = p.Path;
                if (ppath.Length < bestLength)
                    continue;

                if (path.StartsWith(ppath))
                {
                    bestLength = ppath.Length;
                    bestMatch = p._listener;
                    prefix = p;
                }
            }

            return bestMatch;
        }

        private void AddSpecial(List<ListenerPrefix> list, ListenerPrefix prefix)
        {
            if (list == null)
                return;

            foreach (ListenerPrefix p in list)
            {
                if (p.Path == prefix.Path)
                    throw new Exception("net_listener_already");
            }
            list.Add(prefix);
        }

        private bool RemoveSpecial(List<ListenerPrefix> list, ListenerPrefix prefix)
        {
            if (list == null)
                return false;

            int c = list.Count;
            for (int i = 0; i < c; i++)
            {
                ListenerPrefix p = list[i];
                if (p.Path == prefix.Path)
                {
                    list.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        private void CheckIfRemove()
        {
            if (_prefixes.Count > 0)
                return;

            List<ListenerPrefix> list = _unhandledPrefixes;
            if (list != null && list.Count > 0)
                return;

            list = _allPrefixes;
            if (list != null && list.Count > 0)
                return;

            HttpEndPointManager.RemoveEndPoint(this, _endpoint);
        }

        public void Close()
        {
            _closed = true;
            _socket.Close();
            lock (_unregisteredConnections)
            {
                // Clone the list because RemoveConnection can be called from Close
                var connections = new List<HttpConnection>(_unregisteredConnections.Keys);

                foreach (HttpConnection c in connections)
                    c.Close(true);
                _unregisteredConnections.Clear();
            }
        }

        public void AddPrefix(ListenerPrefix prefix, HttpListener listener)
        {
            List<ListenerPrefix> current;
            List<ListenerPrefix> future;
            if (prefix.Host == "*")
            {
                do
                {
                    current = _unhandledPrefixes;
                    future = current != null ? new List<ListenerPrefix>(current) : new List<ListenerPrefix>();
                    prefix._listener = listener;
                    AddSpecial(future, prefix);
                } while (Interlocked.CompareExchange(ref _unhandledPrefixes, future, current) != current);
                return;
            }

            if (prefix.Host == "+")
            {
                do
                {
                    current = _allPrefixes;
                    future = current != null ? new List<ListenerPrefix>(current) : new List<ListenerPrefix>();
                    prefix._listener = listener;
                    AddSpecial(future, prefix);
                } while (Interlocked.CompareExchange(ref _allPrefixes, future, current) != current);
                return;
            }

            Dictionary<ListenerPrefix, HttpListener> prefs, p2;
            do
            {
                prefs = _prefixes;
                if (prefs.ContainsKey(prefix))
                {
                    throw new Exception("net_listener_already");
                }
                p2 = new Dictionary<ListenerPrefix, HttpListener>(prefs);
                p2[prefix] = listener;
            } while (Interlocked.CompareExchange(ref _prefixes, p2, prefs) != prefs);
        }

        public void RemovePrefix(ListenerPrefix prefix, HttpListener listener)
        {
            List<ListenerPrefix> current;
            List<ListenerPrefix> future;
            if (prefix.Host == "*")
            {
                do
                {
                    current = _unhandledPrefixes;
                    future = current != null ? new List<ListenerPrefix>(current) : new List<ListenerPrefix>();
                    if (!RemoveSpecial(future, prefix))
                        break; // Prefix not found
                } while (Interlocked.CompareExchange(ref _unhandledPrefixes, future, current) != current);

                CheckIfRemove();
                return;
            }

            if (prefix.Host == "+")
            {
                do
                {
                    current = _allPrefixes;
                    future = current != null ? new List<ListenerPrefix>(current) : new List<ListenerPrefix>();
                    if (!RemoveSpecial(future, prefix))
                        break; // Prefix not found
                } while (Interlocked.CompareExchange(ref _allPrefixes, future, current) != current);
                CheckIfRemove();
                return;
            }

            Dictionary<ListenerPrefix, HttpListener> prefs, p2;
            do
            {
                prefs = _prefixes;
                if (!prefs.ContainsKey(prefix))
                    break;

                p2 = new Dictionary<ListenerPrefix, HttpListener>(prefs);
                p2.Remove(prefix);
            } while (Interlocked.CompareExchange(ref _prefixes, p2, prefs) != prefs);
            CheckIfRemove();
        }
    }
}
