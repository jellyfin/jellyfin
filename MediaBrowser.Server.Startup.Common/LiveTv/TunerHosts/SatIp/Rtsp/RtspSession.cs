/*  
    Copyright (C) <2007-2016>  <Kay Diefenthal>

    SatIp.RtspSample is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    SatIp.RtspSample is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with SatIp.RtspSample.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Server.Implementations.LiveTv.TunerHosts.SatIp.Rtsp
{
    public class RtspSession : IDisposable
    {
        #region Private Fields
        private static readonly Regex RegexRtspSessionHeader = new Regex(@"\s*([^\s;]+)(;timeout=(\d+))?");
        private const int DefaultRtspSessionTimeout = 30;    // unit = s
        private static readonly Regex RegexDescribeResponseSignalInfo = new Regex(@";tuner=\d+,(\d+),(\d+),(\d+),", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private string _address;
        private string _rtspSessionId;

        public string RtspSessionId
        {
            get { return _rtspSessionId; }
            set { _rtspSessionId = value; }
        }
        private int _rtspSessionTimeToLive = 0;
        private string _rtspStreamId;
        private int _clientRtpPort;
        private int _clientRtcpPort;
        private int _serverRtpPort;
        private int _serverRtcpPort;
        private int _rtpPort;
        private int _rtcpPort;
        private string _rtspStreamUrl;
        private string _destination;
        private string _source;
        private string _transport;
        private int _signalLevel;
        private int _signalQuality;
        private Socket _rtspSocket;
        private int _rtspSequenceNum = 1;
        private bool _disposed = false;
        private readonly ILogger _logger;
        #endregion

        #region Constructor

        public RtspSession(string address, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentNullException("address");
            }

            _address = address;
            _logger = logger;

            _logger.Info("Creating RtspSession with url {0}", address);
        }
        ~RtspSession()
        {
            Dispose(false);
        }
        #endregion

        #region Properties

        #region Rtsp

        public string RtspStreamId
        {
            get { return _rtspStreamId; }
            set { if (_rtspStreamId != value) { _rtspStreamId = value; OnPropertyChanged("RtspStreamId"); } }
        }
        public string RtspStreamUrl
        {
            get { return _rtspStreamUrl; }
            set { if (_rtspStreamUrl != value) { _rtspStreamUrl = value; OnPropertyChanged("RtspStreamUrl"); } }
        }

        public int RtspSessionTimeToLive
        {
            get
            {
                if (_rtspSessionTimeToLive == 0)
                    _rtspSessionTimeToLive = DefaultRtspSessionTimeout;
                return _rtspSessionTimeToLive * 1000 - 20;
            }
            set { if (_rtspSessionTimeToLive != value) { _rtspSessionTimeToLive = value; OnPropertyChanged("RtspSessionTimeToLive"); } }
        }

        #endregion

        #region Rtp Rtcp

        /// <summary>
        /// The LocalEndPoint Address
        /// </summary>
        public string Destination
        {
            get
            {
                if (string.IsNullOrEmpty(_destination))
                {
                    var result = "";
                    var host = Dns.GetHostName();
                    var hostentry = Dns.GetHostEntry(host);
                    foreach (var ip in hostentry.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork))
                    {
                        result = ip.ToString();
                    }

                    _destination = result;
                }
                return _destination;
            }
            set
            {
                if (_destination != value)
                {
                    _destination = value;
                    OnPropertyChanged("Destination");
                }
            }
        }

        /// <summary>
        /// The RemoteEndPoint Address
        /// </summary>
        public string Source
        {
            get { return _source; }
            set
            {
                if (_source != value)
                {
                    _source = value;
                    OnPropertyChanged("Source");
                }
            }
        }

        /// <summary>
        /// The Media Data Delivery RemoteEndPoint Port if we use Unicast
        /// </summary>
        public int ServerRtpPort
        {
            get
            {
                return _serverRtpPort;
            }
            set { if (_serverRtpPort != value) { _serverRtpPort = value; OnPropertyChanged("ServerRtpPort"); } }
        }

        /// <summary>
        /// The Media Metadata Delivery RemoteEndPoint Port if we use Unicast
        /// </summary>
        public int ServerRtcpPort
        {
            get { return _serverRtcpPort; }
            set { if (_serverRtcpPort != value) { _serverRtcpPort = value; OnPropertyChanged("ServerRtcpPort"); } }
        }

        /// <summary>
        /// The Media Data Delivery LocalEndPoint Port if we use Unicast
        /// </summary>
        public int ClientRtpPort
        {
            get { return _clientRtpPort; }
            set { if (_clientRtpPort != value) { _clientRtpPort = value; OnPropertyChanged("ClientRtpPort"); } }
        }

        /// <summary>
        /// The Media Metadata Delivery LocalEndPoint Port if we use Unicast
        /// </summary>
        public int ClientRtcpPort
        {
            get { return _clientRtcpPort; }
            set { if (_clientRtcpPort != value) { _clientRtcpPort = value; OnPropertyChanged("ClientRtcpPort"); } }
        }

        /// <summary>
        /// The Media Data Delivery RemoteEndPoint Port if we use Multicast 
        /// </summary>
        public int RtpPort
        {
            get { return _rtpPort; }
            set { if (_rtpPort != value) { _rtpPort = value; OnPropertyChanged("RtpPort"); } }
        }

        /// <summary>
        /// The Media Meta Delivery RemoteEndPoint Port if we use Multicast 
        /// </summary>
        public int RtcpPort
        {
            get { return _rtcpPort; }
            set { if (_rtcpPort != value) { _rtcpPort = value; OnPropertyChanged("RtcpPort"); } }
        }

        #endregion

        public string Transport
        {
            get
            {
                if (string.IsNullOrEmpty(_transport))
                {
                    _transport = "unicast";
                }
                return _transport;
            }
            set
            {
                if (_transport != value)
                {
                    _transport = value;
                    OnPropertyChanged("Transport");
                }
            }
        }
        public int SignalLevel
        {
            get { return _signalLevel; }
            set { if (_signalLevel != value) { _signalLevel = value; OnPropertyChanged("SignalLevel"); } }
        }
        public int SignalQuality
        {
            get { return _signalQuality; }
            set { if (_signalQuality != value) { _signalQuality = value; OnPropertyChanged("SignalQuality"); } }
        }

        #endregion

        #region Private Methods

        private void ProcessSessionHeader(string sessionHeader, string response)
        {
            if (!string.IsNullOrEmpty(sessionHeader))
            {
                var m = RegexRtspSessionHeader.Match(sessionHeader);
                if (!m.Success)
                {
                    _logger.Error("Failed to tune, RTSP {0} response session header {1} format not recognised", response, sessionHeader);
                }
                _rtspSessionId = m.Groups[1].Captures[0].Value;
                _rtspSessionTimeToLive = m.Groups[3].Captures.Count == 1 ? int.Parse(m.Groups[3].Captures[0].Value) : DefaultRtspSessionTimeout;
            }
        }
        private void ProcessTransportHeader(string transportHeader)
        {
            if (!string.IsNullOrEmpty(transportHeader))
            {
                var transports = transportHeader.Split(',');
                foreach (var transport in transports)
                {
                    if (transport.Trim().StartsWith("RTP/AVP"))
                    {
                        var sections = transport.Split(';');
                        foreach (var section in sections)
                        {
                            var parts = section.Split('=');
                            if (parts[0].Equals("server_port"))
                            {
                                var ports = parts[1].Split('-');
                                _serverRtpPort = int.Parse(ports[0]);
                                _serverRtcpPort = int.Parse(ports[1]);
                            }
                            else if (parts[0].Equals("destination"))
                            {
                                _destination = parts[1];
                            }
                            else if (parts[0].Equals("port"))
                            {
                                var ports = parts[1].Split('-');
                                _rtpPort = int.Parse(ports[0]);
                                _rtcpPort = int.Parse(ports[1]);
                            }
                            else if (parts[0].Equals("ttl"))
                            {
                                _rtspSessionTimeToLive = int.Parse(parts[1]);
                            }
                            else if (parts[0].Equals("source"))
                            {
                                _source = parts[1];
                            }
                            else if (parts[0].Equals("client_port"))
                            {
                                var ports = parts[1].Split('-');
                                var rtp = int.Parse(ports[0]);
                                var rtcp = int.Parse(ports[1]);
                                //if (!rtp.Equals(_rtpPort))
                                //{
                                //    Logger.Error("SAT>IP base: server specified RTP client port {0} instead of {1}", rtp, _rtpPort);
                                //}
                                //if (!rtcp.Equals(_rtcpPort))
                                //{
                                //    Logger.Error("SAT>IP base: server specified RTCP client port {0} instead of {1}", rtcp, _rtcpPort);
                                //}
                                _rtpPort = rtp;
                                _rtcpPort = rtcp;
                            }
                        }
                    }
                }
            }
        }
        private void Connect()
        {
            _rtspSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var ip = IPAddress.Parse(_address);
            var rtspEndpoint = new IPEndPoint(ip, 554);
            _rtspSocket.Connect(rtspEndpoint);
        }
        private void Disconnect()
        {
            if (_rtspSocket != null && _rtspSocket.Connected)
            {
                _rtspSocket.Shutdown(SocketShutdown.Both);
                _rtspSocket.Close();
            }
        }
        private void SendRequest(RtspRequest request)
        {
            if (_rtspSocket == null)
            {
                Connect();
            }
            try
            {
                request.Headers.Add("CSeq", _rtspSequenceNum.ToString());
                _rtspSequenceNum++;
                byte[] requestBytes = request.Serialise();
                if (_rtspSocket != null)
                {
                    var requestBytesCount = _rtspSocket.Send(requestBytes, requestBytes.Length, SocketFlags.None);
                    if (requestBytesCount < 1)
                    {

                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
            }
        }
        private void ReceiveResponse(out RtspResponse response)
        {
            response = null;
            var responseBytesCount = 0;
            byte[] responseBytes = new byte[1024];
            try
            {
                responseBytesCount = _rtspSocket.Receive(responseBytes, responseBytes.Length, SocketFlags.None);
                response = RtspResponse.Deserialise(responseBytes, responseBytesCount);
                string contentLengthString;
                int contentLength = 0;
                if (response.Headers.TryGetValue("Content-Length", out contentLengthString))
                {
                    contentLength = int.Parse(contentLengthString);
                    if ((string.IsNullOrEmpty(response.Body) && contentLength > 0) || response.Body.Length < contentLength)
                    {
                        if (response.Body == null)
                        {
                            response.Body = string.Empty;
                        }
                        while (responseBytesCount > 0 && response.Body.Length < contentLength)
                        {
                            responseBytesCount = _rtspSocket.Receive(responseBytes, responseBytes.Length, SocketFlags.None);
                            response.Body += System.Text.Encoding.UTF8.GetString(responseBytes, 0, responseBytesCount);
                        }
                    }
                }
            }
            catch (SocketException)
            {
            }
        }

        #endregion

        #region Public Methods

        public RtspStatusCode Setup(string query, string transporttype)
        {

            RtspRequest request;
            RtspResponse response;
            //_rtspClient = new RtspClient(_rtspDevice.ServerAddress);
            if ((_rtspSocket == null))
            {
                Connect();
            }
            if (string.IsNullOrEmpty(_rtspSessionId))
            {
                request = new RtspRequest(RtspMethod.Setup, string.Format("rtsp://{0}:{1}/?{2}", _address, 554, query), 1, 0);
                switch (transporttype)
                {
                    case "multicast":
                        request.Headers.Add("Transport", string.Format("RTP/AVP;multicast"));
                        break;
                    case "unicast":
                        var activeTcpConnections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();
                        var usedPorts = new HashSet<int>();
                        foreach (var connection in activeTcpConnections)
                        {
                            usedPorts.Add(connection.LocalEndPoint.Port);
                        }
                        for (var port = 40000; port <= 65534; port += 2)
                        {
                            if (!usedPorts.Contains(port) && !usedPorts.Contains(port + 1))
                            {

                                _clientRtpPort = port;
                                _clientRtcpPort = port + 1;
                                break;
                            }
                        }
                        request.Headers.Add("Transport", string.Format("RTP/AVP;unicast;client_port={0}-{1}", _clientRtpPort, _clientRtcpPort));
                        break;
                }
            }
            else
            {
                request = new RtspRequest(RtspMethod.Setup, string.Format("rtsp://{0}:{1}/?{2}", _address, 554, query), 1, 0);
                switch (transporttype)
                {
                    case "multicast":
                        request.Headers.Add("Transport", string.Format("RTP/AVP;multicast"));
                        break;
                    case "unicast":
                        request.Headers.Add("Transport", string.Format("RTP/AVP;unicast;client_port={0}-{1}", _clientRtpPort, _clientRtcpPort));
                        break;
                }

            }
            SendRequest(request);
            ReceiveResponse(out response);

            //if (_rtspClient.SendRequest(request, out response) != RtspStatusCode.Ok)
            //{
            //    Logger.Error("Failed to tune, non-OK RTSP SETUP status code {0} {1}", response.StatusCode, response.ReasonPhrase);
            //}
            if (!response.Headers.TryGetValue("com.ses.streamID", out _rtspStreamId))
            {
                _logger.Error(string.Format("Failed to tune, not able to locate Stream ID header in RTSP SETUP response"));
            }
            string sessionHeader;
            if (!response.Headers.TryGetValue("Session", out sessionHeader))
            {
                _logger.Error(string.Format("Failed to tune, not able to locate Session header in RTSP SETUP response"));
            }
            ProcessSessionHeader(sessionHeader, "Setup");
            string transportHeader;
            if (!response.Headers.TryGetValue("Transport", out transportHeader))
            {
                _logger.Error(string.Format("Failed to tune, not able to locate Transport header in RTSP SETUP response"));
            }
            ProcessTransportHeader(transportHeader);
            return response.StatusCode;
        }

        public RtspStatusCode Play(string query)
        {
            if ((_rtspSocket == null))
            {
                Connect();
            }
            //_rtspClient = new RtspClient(_rtspDevice.ServerAddress);
            RtspResponse response;
            string data;
            if (string.IsNullOrEmpty(query))
            {
                data = string.Format("rtsp://{0}:{1}/stream={2}", _address,
                    554, _rtspStreamId);
            }
            else
            {
                data = string.Format("rtsp://{0}:{1}/stream={2}?{3}", _address,
                    554, _rtspStreamId, query);
            }
            var request = new RtspRequest(RtspMethod.Play, data, 1, 0);
            request.Headers.Add("Session", _rtspSessionId);
            SendRequest(request);
            ReceiveResponse(out response);
            //if (_rtspClient.SendRequest(request, out response) != RtspStatusCode.Ok)
            //{
            //    Logger.Error("Failed to tune, non-OK RTSP SETUP status code {0} {1}", response.StatusCode, response.ReasonPhrase);
            //}
            //Logger.Info("RtspSession-Play : \r\n {0}", response);
            string sessionHeader;
            if (!response.Headers.TryGetValue("Session", out sessionHeader))
            {
                _logger.Error(string.Format("Failed to tune, not able to locate Session header in RTSP Play response"));
            }
            ProcessSessionHeader(sessionHeader, "Play");
            string rtpinfoHeader;
            if (!response.Headers.TryGetValue("RTP-Info", out rtpinfoHeader))
            {
                _logger.Error(string.Format("Failed to tune, not able to locate Rtp-Info header in RTSP Play response"));
            }
            return response.StatusCode;
        }

        public RtspStatusCode Options()
        {
            if ((_rtspSocket == null))
            {
                Connect();
            }
            //_rtspClient = new RtspClient(_rtspDevice.ServerAddress);
            RtspRequest request;
            RtspResponse response;


            if (string.IsNullOrEmpty(_rtspSessionId))
            {
                request = new RtspRequest(RtspMethod.Options, string.Format("rtsp://{0}:{1}/", _address, 554), 1, 0);
            }
            else
            {
                request = new RtspRequest(RtspMethod.Options, string.Format("rtsp://{0}:{1}/", _address, 554), 1, 0);
                request.Headers.Add("Session", _rtspSessionId);
            }
            SendRequest(request);
            ReceiveResponse(out response);
            //if (_rtspClient.SendRequest(request, out response) != RtspStatusCode.Ok)
            //{
            //    Logger.Error("Failed to tune, non-OK RTSP SETUP status code {0} {1}", response.StatusCode, response.ReasonPhrase);
            //}
            //Logger.Info("RtspSession-Options : \r\n {0}", response);
            string sessionHeader;
            if (!response.Headers.TryGetValue("Session", out sessionHeader))
            {
                _logger.Error(string.Format("Failed to tune, not able to locate session header in RTSP Options response"));
            }
            ProcessSessionHeader(sessionHeader, "Options");
            string optionsHeader;
            if (!response.Headers.TryGetValue("Public", out optionsHeader))
            {
                _logger.Error(string.Format("Failed to tune, not able to Options header in RTSP Options response"));
            }
            return response.StatusCode;
        }

        public RtspStatusCode Describe(out int level, out int quality)
        {
            if ((_rtspSocket == null))
            {
                Connect();
            }
            //_rtspClient = new RtspClient(_rtspDevice.ServerAddress);
            RtspRequest request;
            RtspResponse response;
            level = 0;
            quality = 0;

            if (string.IsNullOrEmpty(_rtspSessionId))
            {
                request = new RtspRequest(RtspMethod.Describe, string.Format("rtsp://{0}:{1}/", _address, 554), 1, 0);
                request.Headers.Add("Accept", "application/sdp");

            }
            else
            {
                request = new RtspRequest(RtspMethod.Describe, string.Format("rtsp://{0}:{1}/stream={2}", _address, 554, _rtspStreamId), 1, 0);
                request.Headers.Add("Accept", "application/sdp");
                request.Headers.Add("Session", _rtspSessionId);
            }
            SendRequest(request);
            ReceiveResponse(out response);
            //if (_rtspClient.SendRequest(request, out response) != RtspStatusCode.Ok)
            //{
            //    Logger.Error("Failed to tune, non-OK RTSP Describe status code {0} {1}", response.StatusCode, response.ReasonPhrase);
            //}
            //Logger.Info("RtspSession-Describe : \r\n {0}", response);
            string sessionHeader;
            if (!response.Headers.TryGetValue("Session", out sessionHeader))
            {
                _logger.Error(string.Format("Failed to tune, not able to locate session header in RTSP Describe response"));
            }
            ProcessSessionHeader(sessionHeader, "Describe");
            var m = RegexDescribeResponseSignalInfo.Match(response.Body);
            if (m.Success)
            {

                //isSignalLocked = m.Groups[2].Captures[0].Value.Equals("1");
                level = int.Parse(m.Groups[1].Captures[0].Value) * 100 / 255;    // level: 0..255 => 0..100
                quality = int.Parse(m.Groups[3].Captures[0].Value) * 100 / 15;   // quality: 0..15 => 0..100

            }
            /*              
                v=0
                o=- 1378633020884883 1 IN IP4 192.168.2.108
                s=SatIPServer:1 4
                t=0 0
                a=tool:idl4k
                m=video 52780 RTP/AVP 33
                c=IN IP4 0.0.0.0
                b=AS:5000
                a=control:stream=4
                a=fmtp:33 ver=1.0;tuner=1,0,0,0,12344,h,dvbs2,,off,,22000,34;pids=0,100,101,102,103,106
                =sendonly
             */


            return response.StatusCode;
        }

        public RtspStatusCode TearDown()
        {
            if ((_rtspSocket == null))
            {
                Connect();
            }
            //_rtspClient = new RtspClient(_rtspDevice.ServerAddress);
            RtspResponse response;

            var request = new RtspRequest(RtspMethod.Teardown, string.Format("rtsp://{0}:{1}/stream={2}", _address, 554, _rtspStreamId), 1, 0);
            request.Headers.Add("Session", _rtspSessionId);
            SendRequest(request);
            ReceiveResponse(out response);
            //if (_rtspClient.SendRequest(request, out response) != RtspStatusCode.Ok)
            //{
            //    Logger.Error("Failed to tune, non-OK RTSP Teardown status code {0} {1}", response.StatusCode, response.ReasonPhrase);
            //}            
            return response.StatusCode;
        }

        #endregion

        #region Public Events

        ////public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Protected Methods

        protected void OnPropertyChanged(string name)
        {
            //var handler = PropertyChanged;
            //if (handler != null)
            //{
            //    handler(this, new PropertyChangedEventArgs(name));
            //}
        }

        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);//Disconnect();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    TearDown();
                    Disconnect();
                }
            }
            _disposed = true;
        }
    }
}
