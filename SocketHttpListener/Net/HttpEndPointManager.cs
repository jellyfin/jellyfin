using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Net;
using SocketHttpListener.Primitives;

namespace SocketHttpListener.Net
{
    internal sealed class HttpEndPointManager
    {
        private static Dictionary<IPAddress, Dictionary<int, HttpEndPointListener>> s_ipEndPoints = new Dictionary<IPAddress, Dictionary<int, HttpEndPointListener>>();

        private HttpEndPointManager()
        {
        }

        public static void AddListener(ILogger logger, HttpListener listener)
        {
            List<string> added = new List<string>();
            try
            {
                lock ((s_ipEndPoints as ICollection).SyncRoot)
                {
                    foreach (string prefix in listener.Prefixes)
                    {
                        AddPrefixInternal(logger, prefix, listener);
                        added.Add(prefix);
                    }
                }
            }
            catch
            {
                foreach (string prefix in added)
                {
                    RemovePrefix(logger, prefix, listener);
                }
                throw;
            }
        }

        public static void AddPrefix(ILogger logger, string prefix, HttpListener listener)
        {
            lock ((s_ipEndPoints as ICollection).SyncRoot)
            {
                AddPrefixInternal(logger, prefix, listener);
            }
        }

        private static void AddPrefixInternal(ILogger logger, string p, HttpListener listener)
        {
            int start = p.IndexOf(':') + 3;
            int colon = p.IndexOf(':', start);
            if (colon != -1)
            {
                // root can't be -1 here, since we've already checked for ending '/' in ListenerPrefix.
                int root = p.IndexOf('/', colon, p.Length - colon);
                string portString = p.Substring(colon + 1, root - colon - 1);

                int port;
                if (!int.TryParse(portString, out port) || port <= 0 || port >= 65536)
                {
                    throw new HttpListenerException((int)HttpStatusCode.BadRequest, "net_invalid_port");
                }
            }

            ListenerPrefix lp = new ListenerPrefix(p);
            if (lp.Host != "*" && lp.Host != "+" && Uri.CheckHostName(lp.Host) == UriHostNameType.Unknown)
                throw new HttpListenerException((int)HttpStatusCode.BadRequest, "net_listener_host");

            if (lp.Path.IndexOf('%') != -1)
                throw new HttpListenerException((int)HttpStatusCode.BadRequest, "net_invalid_path");

            if (lp.Path.IndexOf("//", StringComparison.Ordinal) != -1)
                throw new HttpListenerException((int)HttpStatusCode.BadRequest, "net_invalid_path");

            // listens on all the interfaces if host name cannot be parsed by IPAddress.
            HttpEndPointListener epl = GetEPListener(logger, lp.Host, lp.Port, listener, lp.Secure);
            epl.AddPrefix(lp, listener);
        }

        private static IPAddress GetIpAnyAddress(HttpListener listener)
        {
            return listener.EnableDualMode ? IPAddress.IPv6Any : IPAddress.Any;
        }

        private static HttpEndPointListener GetEPListener(ILogger logger, string host, int port, HttpListener listener, bool secure)
        {
            IPAddress addr;
            if (host == "*" || host == "+")
            {
                addr = GetIpAnyAddress(listener);
            }
            else
            {
                const int NotSupportedErrorCode = 50;
                try
                {
                    addr = Dns.GetHostAddresses(host)[0];
                }
                catch
                {
                    // Throw same error code as windows, request is not supported.
                    throw new HttpListenerException(NotSupportedErrorCode, "net_listener_not_supported");
                }

                if (IPAddress.Any.Equals(addr))
                {
                    // Don't support listening to 0.0.0.0, match windows behavior.
                    throw new HttpListenerException(NotSupportedErrorCode, "net_listener_not_supported");
                }
            }

            Dictionary<int, HttpEndPointListener> p = null;
            if (s_ipEndPoints.ContainsKey(addr))
            {
                p = s_ipEndPoints[addr];
            }
            else
            {
                p = new Dictionary<int, HttpEndPointListener>();
                s_ipEndPoints[addr] = p;
            }

            HttpEndPointListener epl = null;
            if (p.ContainsKey(port))
            {
                epl = p[port];
            }
            else
            {
                try
                {
                    epl = new HttpEndPointListener(listener, addr, port, secure, listener.Certificate, logger, listener.CryptoProvider, listener.SocketFactory, listener.StreamHelper, listener.TextEncoding, listener.FileSystem, listener.EnvironmentInfo);
                }
                catch (SocketException ex)
                {
                    throw new HttpListenerException(ex.ErrorCode, ex.Message);
                }
                p[port] = epl;
            }

            return epl;
        }

        public static void RemoveEndPoint(HttpEndPointListener epl, IPEndPoint ep)
        {
            lock ((s_ipEndPoints as ICollection).SyncRoot)
            {
                Dictionary<int, HttpEndPointListener> p = null;
                p = s_ipEndPoints[ep.Address];
                p.Remove(ep.Port);
                if (p.Count == 0)
                {
                    s_ipEndPoints.Remove(ep.Address);
                }
                epl.Close();
            }
        }

        public static void RemoveListener(ILogger logger, HttpListener listener)
        {
            lock ((s_ipEndPoints as ICollection).SyncRoot)
            {
                foreach (string prefix in listener.Prefixes)
                {
                    RemovePrefixInternal(logger, prefix, listener);
                }
            }
        }

        public static void RemovePrefix(ILogger logger, string prefix, HttpListener listener)
        {
            lock ((s_ipEndPoints as ICollection).SyncRoot)
            {
                RemovePrefixInternal(logger, prefix, listener);
            }
        }

        private static void RemovePrefixInternal(ILogger logger, string prefix, HttpListener listener)
        {
            ListenerPrefix lp = new ListenerPrefix(prefix);
            if (lp.Path.IndexOf('%') != -1)
                return;

            if (lp.Path.IndexOf("//", StringComparison.Ordinal) != -1)
                return;

            HttpEndPointListener epl = GetEPListener(logger, lp.Host, lp.Port, listener, lp.Secure);
            epl.RemovePrefix(lp, listener);
        }
    }
}
