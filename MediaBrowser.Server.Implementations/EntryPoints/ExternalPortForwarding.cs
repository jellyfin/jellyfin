using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using Mono.Nat;
using System;
using System.IO;
using System.Text;

namespace MediaBrowser.Server.Implementations.EntryPoints
{
    public class ExternalPortForwarding : IServerEntryPoint
    {
        private readonly IServerApplicationHost _appHost;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;

        private bool _isStarted;

        public ExternalPortForwarding(ILogger logger, IServerApplicationHost appHost, IServerConfigurationManager config)
        {
            _logger = logger;
            _appHost = appHost;
            _config = config;

            _config.ConfigurationUpdated += _config_ConfigurationUpdated;
        }

        void _config_ConfigurationUpdated(object sender, EventArgs e)
        {
            var enable = _config.Configuration.EnableUPnP;

            if (enable && !_isStarted)
            {
                Reload();
            }
            else if (!enable && _isStarted)
            {
                DisposeNat();
            }
        }

        public void Run()
        {
            //NatUtility.Logger = new LogWriter(_logger);
            
            Reload();
        }

        private void Reload()
        {
            if (_config.Configuration.EnableUPnP)
            {
                _logger.Debug("Starting NAT discovery");
                
                NatUtility.DeviceFound += NatUtility_DeviceFound;
                NatUtility.DeviceLost += NatUtility_DeviceLost;
                NatUtility.UnhandledException += NatUtility_UnhandledException;
                NatUtility.StartDiscovery();

                _isStarted = true;
            }
        }

        void NatUtility_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            //var ex = e.ExceptionObject as Exception;

            //if (ex == null)
            //{
            //    _logger.Error("Unidentified error reported by Mono.Nat");
            //}
            //else
            //{
            //    // Seeing some blank exceptions coming through here
            //    _logger.ErrorException("Error reported by Mono.Nat: ", ex);
            //}
        }

        void NatUtility_DeviceFound(object sender, DeviceEventArgs e)
        {
            try
            {
                var device = e.Device;
                _logger.Debug("NAT device found: {0}", device.LocalAddress.ToString());

                CreateRules(device);
            }
            catch (Exception ex)
            {
                //_logger.ErrorException("Error creating port forwarding rules", ex);
            }
        }

        private void CreateRules(INatDevice device)
        {
            var info = _appHost.GetSystemInfo();

            CreatePortMap(device, info.HttpServerPortNumber);

            if (info.WebSocketPortNumber != info.HttpServerPortNumber)
            {
                CreatePortMap(device, info.WebSocketPortNumber);
            }
        }

        private void CreatePortMap(INatDevice device, int port)
        {
            _logger.Debug("Creating port map on port {0}", port);

            device.CreatePortMap(new Mapping(Protocol.Tcp, port, port)
            {
                Description = "Media Browser Server"
            });
        }

        void NatUtility_DeviceLost(object sender, DeviceEventArgs e)
        {
            var device = e.Device;
            _logger.Debug("NAT device lost: {0}", device.LocalAddress.ToString());
        }

        public void Dispose()
        {
            DisposeNat();
        }

        private void DisposeNat()
        {
            _logger.Debug("Stopping NAT discovery");

            try
            {
                NatUtility.DeviceFound -= NatUtility_DeviceFound;
                NatUtility.DeviceLost -= NatUtility_DeviceLost;
                NatUtility.UnhandledException -= NatUtility_UnhandledException;
                NatUtility.StopDiscovery();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error stopping NAT Discovery", ex);
            }
            finally
            {
                _isStarted = false;
            }
        }

        private class LogWriter : TextWriter
        {
            private readonly ILogger _logger;

            public LogWriter(ILogger logger)
            {
                _logger = logger;
            }

            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }

            public override void WriteLine(string format, params object[] arg)
            {
                _logger.Debug(format, arg);
            }

            public override void WriteLine(string value)
            {
                _logger.Debug(value);
            }
        }
    }
}
