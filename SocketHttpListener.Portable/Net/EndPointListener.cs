using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Text;
using SocketHttpListener.Primitives;

namespace SocketHttpListener.Net
{
    sealed class EndPointListener
    {
        HttpListener listener;
        IpEndPointInfo endpoint;
        ISocket sock;
        Dictionary<ListenerPrefix,HttpListener> prefixes;  // Dictionary <ListenerPrefix, HttpListener>
        List<ListenerPrefix> unhandled; // List<ListenerPrefix> unhandled; host = '*'
        List<ListenerPrefix> all;       // List<ListenerPrefix> all;  host = '+'
        ICertificate cert;
        bool secure;
        Dictionary<HttpConnection, HttpConnection> unregistered;
        private readonly ILogger _logger;
        private bool _closed;
        private readonly bool _enableDualMode;
        private readonly ICryptoProvider _cryptoProvider;
        private readonly IStreamFactory _streamFactory;
        private readonly ISocketFactory _socketFactory;
        private readonly ITextEncoding _textEncoding;
        private readonly IMemoryStreamFactory _memoryStreamFactory;

        public EndPointListener(HttpListener listener, IpAddressInfo addr, int port, bool secure, ICertificate cert, ILogger logger, ICryptoProvider cryptoProvider, IStreamFactory streamFactory, ISocketFactory socketFactory, IMemoryStreamFactory memoryStreamFactory, ITextEncoding textEncoding)
        {
            this.listener = listener;
            _logger = logger;
            _cryptoProvider = cryptoProvider;
            _streamFactory = streamFactory;
            _socketFactory = socketFactory;
            _memoryStreamFactory = memoryStreamFactory;
            _textEncoding = textEncoding;

            this.secure = secure;
            this.cert = cert;

            _enableDualMode = addr.Equals(IpAddressInfo.IPv6Any);
            endpoint = new IpEndPointInfo(addr, port);

            prefixes = new Dictionary<ListenerPrefix, HttpListener>();
            unregistered = new Dictionary<HttpConnection, HttpConnection>();

            CreateSocket();
        }

        internal HttpListener Listener
        {
            get
            {
                return listener;
            }
        }

        private void CreateSocket()
        {
            sock = _socketFactory.CreateSocket(endpoint.IpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp, _enableDualMode);

            sock.Bind(endpoint);

            // This is the number TcpListener uses.
            sock.Listen(2147483647);

            sock.StartAccept(ProcessAccept, () => _closed);
            _closed = false;
        }

        private async void ProcessAccept(ISocket accepted)
        {
            try
            {
                var listener = this;

                if (listener.secure && listener.cert == null)
                {
                    accepted.Close();
                    return;
                }

                HttpConnection conn = await HttpConnection.Create(_logger, accepted, listener, listener.secure, listener.cert, _cryptoProvider, _streamFactory, _memoryStreamFactory, _textEncoding).ConfigureAwait(false);

                //_logger.Debug("Adding unregistered connection to {0}. Id: {1}", accepted.RemoteEndPoint, connectionId);
                lock (listener.unregistered)
                {
                    listener.unregistered[conn] = conn;
                }
                conn.BeginReadRequest();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error in ProcessAccept", ex);
            }
        }

        internal void RemoveConnection(HttpConnection conn)
        {
            lock (unregistered)
            {
                unregistered.Remove(conn);
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

            listener.UnregisterContext(context);
        }

        HttpListener SearchListener(Uri uri, out ListenerPrefix prefix)
        {
            prefix = null;
            if (uri == null)
                return null;

            string host = uri.Host;
            int port = uri.Port;
            string path = WebUtility.UrlDecode(uri.AbsolutePath);
            string path_slash = path[path.Length - 1] == '/' ? path : path + "/";

            HttpListener best_match = null;
            int best_length = -1;

            if (host != null && host != "")
            {
                var p_ro = prefixes;
                foreach (ListenerPrefix p in p_ro.Keys)
                {
                    string ppath = p.Path;
                    if (ppath.Length < best_length)
                        continue;

                    if (p.Host != host || p.Port != port)
                        continue;

                    if (path.StartsWith(ppath) || path_slash.StartsWith(ppath))
                    {
                        best_length = ppath.Length;
                        best_match = (HttpListener)p_ro[p];
                        prefix = p;
                    }
                }
                if (best_length != -1)
                    return best_match;
            }

            List<ListenerPrefix> list = unhandled;
            best_match = MatchFromList(host, path, list, out prefix);
            if (path != path_slash && best_match == null)
                best_match = MatchFromList(host, path_slash, list, out prefix);
            if (best_match != null)
                return best_match;

            list = all;
            best_match = MatchFromList(host, path, list, out prefix);
            if (path != path_slash && best_match == null)
                best_match = MatchFromList(host, path_slash, list, out prefix);
            if (best_match != null)
                return best_match;

            return null;
        }

        HttpListener MatchFromList(string host, string path, List<ListenerPrefix> list, out ListenerPrefix prefix)
        {
            prefix = null;
            if (list == null)
                return null;

            HttpListener best_match = null;
            int best_length = -1;

            foreach (ListenerPrefix p in list)
            {
                string ppath = p.Path;
                if (ppath.Length < best_length)
                    continue;

                if (path.StartsWith(ppath))
                {
                    best_length = ppath.Length;
                    best_match = p.Listener;
                    prefix = p;
                }
            }

            return best_match;
        }

        void AddSpecial(List<ListenerPrefix> coll, ListenerPrefix prefix)
        {
            if (coll == null)
                return;

            foreach (ListenerPrefix p in coll)
            {
                if (p.Path == prefix.Path) //TODO: code
                    throw new HttpListenerException(400, "Prefix already in use.");
            }
            coll.Add(prefix);
        }

        bool RemoveSpecial(List<ListenerPrefix> coll, ListenerPrefix prefix)
        {
            if (coll == null)
                return false;

            int c = coll.Count;
            for (int i = 0; i < c; i++)
            {
                ListenerPrefix p = (ListenerPrefix)coll[i];
                if (p.Path == prefix.Path)
                {
                    coll.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        void CheckIfRemove()
        {
            if (prefixes.Count > 0)
                return;

            List<ListenerPrefix> list = unhandled;
            if (list != null && list.Count > 0)
                return;

            list = all;
            if (list != null && list.Count > 0)
                return;

            EndPointManager.RemoveEndPoint(this, endpoint);
        }

        public void Close()
        {
            _closed = true;
            sock.Close();
            lock (unregistered)
            {
                //
                // Clone the list because RemoveConnection can be called from Close
                //
                var connections = new List<HttpConnection>(unregistered.Keys);

                foreach (HttpConnection c in connections)
                    c.Close(true);
                unregistered.Clear();
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
                    current = unhandled;
                    future = (current != null) ? current.ToList() : new List<ListenerPrefix>();
                    prefix.Listener = listener;
                    AddSpecial(future, prefix);
                } while (Interlocked.CompareExchange(ref unhandled, future, current) != current);
                return;
            }

            if (prefix.Host == "+")
            {
                do
                {
                    current = all;
                    future = (current != null) ? current.ToList() : new List<ListenerPrefix>();
                    prefix.Listener = listener;
                    AddSpecial(future, prefix);
                } while (Interlocked.CompareExchange(ref all, future, current) != current);
                return;
            }

            Dictionary<ListenerPrefix, HttpListener> prefs;
            Dictionary<ListenerPrefix, HttpListener> p2;
            do
            {
                prefs = prefixes;
                if (prefs.ContainsKey(prefix))
                {
                    HttpListener other = (HttpListener)prefs[prefix];
                    if (other != listener) // TODO: code.
                        throw new HttpListenerException(400, "There's another listener for " + prefix);
                    return;
                }
                p2 = new Dictionary<ListenerPrefix, HttpListener>(prefs);
                p2[prefix] = listener;
            } while (Interlocked.CompareExchange(ref prefixes, p2, prefs) != prefs);
        }

        public void RemovePrefix(ListenerPrefix prefix, HttpListener listener)
        {
            List<ListenerPrefix> current;
            List<ListenerPrefix> future;
            if (prefix.Host == "*")
            {
                do
                {
                    current = unhandled;
                    future = (current != null) ? current.ToList() : new List<ListenerPrefix>();
                    if (!RemoveSpecial(future, prefix))
                        break; // Prefix not found
                } while (Interlocked.CompareExchange(ref unhandled, future, current) != current);
                CheckIfRemove();
                return;
            }

            if (prefix.Host == "+")
            {
                do
                {
                    current = all;
                    future = (current != null) ? current.ToList() : new List<ListenerPrefix>();
                    if (!RemoveSpecial(future, prefix))
                        break; // Prefix not found
                } while (Interlocked.CompareExchange(ref all, future, current) != current);
                CheckIfRemove();
                return;
            }

            Dictionary<ListenerPrefix, HttpListener> prefs;
            Dictionary<ListenerPrefix, HttpListener> p2;
            do
            {
                prefs = prefixes;
                if (!prefs.ContainsKey(prefix))
                    break;

                p2 = new Dictionary<ListenerPrefix, HttpListener>(prefs);
                p2.Remove(prefix);
            } while (Interlocked.CompareExchange(ref prefixes, p2, prefs) != prefs);
            CheckIfRemove();
        }
    }
}
