using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Net;

namespace Rssdp.Infrastructure
{
	/// <summary>
	/// Provides arguments for the <see cref="ISsdpCommunicationsServer.RequestReceived"/> event.
	/// </summary>
	public sealed class RequestReceivedEventArgs : EventArgs
	{

		#region Fields

		private readonly HttpRequestMessage _Message;
		private readonly IpEndPointInfo _ReceivedFrom;

		#endregion

	    public IpAddressInfo LocalIpAddress { get; private set; }

	    #region Constructors

		/// <summary>
		/// Full constructor.
		/// </summary>
		public RequestReceivedEventArgs(HttpRequestMessage message, IpEndPointInfo receivedFrom, IpAddressInfo localIpAddress)
		{
			_Message = message;
			_ReceivedFrom = receivedFrom;
		    LocalIpAddress = localIpAddress;
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// The <see cref="HttpRequestMessage"/> that was received.
		/// </summary>
		public HttpRequestMessage Message
		{
			get { return _Message; }
		}

		/// <summary>
		/// The <see cref="UdpEndPoint"/> the request came from.
		/// </summary>
		public IpEndPointInfo ReceivedFrom
		{
			get { return _ReceivedFrom; }
		}

		#endregion

	}
}