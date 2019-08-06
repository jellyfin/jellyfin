using System;
using System.Net;
using System.Net.Http;

namespace Rssdp.Infrastructure
{
    /// <summary>
    /// Provides arguments for the <see cref="ISsdpCommunicationsServer.ResponseReceived"/> event.
    /// </summary>
    public sealed class ResponseReceivedEventArgs : EventArgs
    {

        public IPAddress LocalIpAddress { get; set; }

        #region Fields

        private readonly HttpResponseMessage _Message;
        private readonly IPEndPoint _ReceivedFrom;

        #endregion

        #region Constructors

        /// <summary>
        /// Full constructor.
        /// </summary>
        public ResponseReceivedEventArgs(HttpResponseMessage message, IPEndPoint receivedFrom)
        {
            _Message = message;
            _ReceivedFrom = receivedFrom;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// The <see cref="HttpResponseMessage"/> that was received.
        /// </summary>
        public HttpResponseMessage Message
        {
            get { return _Message; }
        }

        /// <summary>
        /// The <see cref="UdpEndPoint"/> the response came from.
        /// </summary>
        public IPEndPoint ReceivedFrom
        {
            get { return _ReceivedFrom; }
        }

        #endregion
    }
}
