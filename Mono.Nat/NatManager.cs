using System;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Logging;
using System.Linq;

namespace Mono.Nat
{
    public class NatManager : IDisposable
    {
        public event EventHandler<DeviceEventArgs> DeviceFound;

        private List<ISearcher> controllers = new List<ISearcher>();

        private ILogger Logger;
        private IHttpClient HttpClient;

        public NatManager(ILogger logger, IHttpClient httpClient)
        {
            Logger = logger;
            HttpClient = httpClient;
        }

        private object _runSyncLock = new object();
        public void StartDiscovery()
        {
            lock (_runSyncLock)
            {
                if (controllers.Count > 0)
                {
                    return;
                }

                controllers.Add(new PmpSearcher(Logger));

                foreach (var searcher in controllers)
                {
                    searcher.DeviceFound += Searcher_DeviceFound;
                }
            }
        }

        public void StopDiscovery()
        {
            lock (_runSyncLock)
            {
                var disposables = controllers.OfType<IDisposable>().ToList();
                controllers.Clear();

                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
            }
        }

        public void Dispose()
        {
            StopDiscovery();
        }

        public Task Handle(IPAddress localAddress, UpnpDeviceInfo deviceInfo, IPEndPoint endpoint, NatProtocol protocol)
        {
            switch (protocol)
            {
                case NatProtocol.Upnp:
                    var searcher = new UpnpSearcher(Logger, HttpClient);
                    searcher.DeviceFound += Searcher_DeviceFound;
                    return searcher.Handle(localAddress, deviceInfo, endpoint);
                default:
                    throw new ArgumentException("Unexpected protocol: " + protocol);
            }
        }

        private void Searcher_DeviceFound(object sender, DeviceEventArgs e)
        {
            if (DeviceFound != null)
            {
                DeviceFound(sender, e);
            }
        }
    }
}
