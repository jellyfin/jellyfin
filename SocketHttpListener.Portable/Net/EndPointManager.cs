using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using SocketHttpListener.Primitives;

namespace SocketHttpListener.Net
{
    sealed class EndPointManager
    {
        // Dictionary<IPAddress, Dictionary<int, EndPointListener>>
        static Dictionary<string, Dictionary<int, EndPointListener>> ip_to_endpoints = new Dictionary<string, Dictionary<int, EndPointListener>>();

        private EndPointManager()
        {
        }

        public static void AddListener(ILogger logger, HttpListener listener)
        {
            List<string> added = new List<string>();
            try
            {
                lock (ip_to_endpoints)
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
            lock (ip_to_endpoints)
            {
                AddPrefixInternal(logger, prefix, listener);
            }
        }

        static void AddPrefixInternal(ILogger logger, string p, HttpListener listener)
        {
            ListenerPrefix lp = new ListenerPrefix(p);
            if (lp.Path.IndexOf('%') != -1)
                throw new HttpListenerException(400, "Invalid path.");

            if (lp.Path.IndexOf("//", StringComparison.Ordinal) != -1) // TODO: Code?
                throw new HttpListenerException(400, "Invalid path.");

            // listens on all the interfaces if host name cannot be parsed by IPAddress.
            EndPointListener epl = GetEPListener(logger, lp.Host, lp.Port, listener, lp.Secure).Result;
            epl.AddPrefix(lp, listener);
        }

        private static IpAddressInfo GetIpAnyAddress(HttpListener listener)
        {
            return listener.EnableDualMode ? IpAddressInfo.IPv6Any : IpAddressInfo.Any;
        }

        static async Task<EndPointListener> GetEPListener(ILogger logger, string host, int port, HttpListener listener, bool secure)
        {
            var networkManager = listener.NetworkManager;

            IpAddressInfo addr;
            if (host == "*" || host == "+")
                addr = GetIpAnyAddress(listener);
            else if (networkManager.TryParseIpAddress(host, out addr) == false)
            {
                try
                {
                    addr = (await networkManager.GetHostAddressesAsync(host).ConfigureAwait(false)).FirstOrDefault() ?? 
                        GetIpAnyAddress(listener);
                }
                catch
                {
                    addr = GetIpAnyAddress(listener);
                }
            }

            Dictionary<int, EndPointListener> p = null;  // Dictionary<int, EndPointListener>
            if (!ip_to_endpoints.TryGetValue(addr.Address, out p))
            {
                p = new Dictionary<int, EndPointListener>();
                ip_to_endpoints[addr.Address] = p;
            }

            EndPointListener epl = null;
            if (p.ContainsKey(port))
            {
                epl = (EndPointListener)p[port];
            }
            else
            {
                epl = new EndPointListener(listener, addr, port, secure, listener.Certificate, logger, listener.CryptoProvider, listener.StreamFactory, listener.SocketFactory, listener.MemoryStreamFactory, listener.TextEncoding, listener.FileSystem);
                p[port] = epl;
            }

            return epl;
        }

        public static void RemoveEndPoint(EndPointListener epl, IpEndPointInfo ep)
        {
            lock (ip_to_endpoints)
            {
                // Dictionary<int, EndPointListener> p
                Dictionary<int, EndPointListener> p;
                if (ip_to_endpoints.TryGetValue(ep.IpAddress.Address, out p))
                {
                    p.Remove(ep.Port);
                    if (p.Count == 0)
                    {
                        ip_to_endpoints.Remove(ep.IpAddress.Address);
                    }
                }
                epl.Close();
            }
        }

        public static void RemoveListener(ILogger logger, HttpListener listener)
        {
            lock (ip_to_endpoints)
            {
                foreach (string prefix in listener.Prefixes)
                {
                    RemovePrefixInternal(logger, prefix, listener);
                }
            }
        }

        public static void RemovePrefix(ILogger logger, string prefix, HttpListener listener)
        {
            lock (ip_to_endpoints)
            {
                RemovePrefixInternal(logger, prefix, listener);
            }
        }

        static void RemovePrefixInternal(ILogger logger, string prefix, HttpListener listener)
        {
            ListenerPrefix lp = new ListenerPrefix(prefix);
            if (lp.Path.IndexOf('%') != -1)
                return;

            if (lp.Path.IndexOf("//", StringComparison.Ordinal) != -1)
                return;

            EndPointListener epl = GetEPListener(logger, lp.Host, lp.Port, listener, lp.Secure).Result;
            epl.RemovePrefix(lp, listener);
        }
    }
}
