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
	/// Provides arguments for the <see cref="ISsdpCommunicationsServer.ResponseReceived"/> event.
	/// </summary>
	public sealed class ResponseReceivedEventArgs : EventArgs
	{

		#region Fields

		private readonly HttpResponseMessage _Message;
		private readonly IpEndPointInfo _ReceivedFrom;

		#endregion

		#region Constructors

		/// <summary>
		/// Full constructor.
		/// </summary>
		public ResponseReceivedEventArgs(HttpResponseMessage message, IpEndPointInfo receivedFrom)
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
		public IpEndPointInfo ReceivedFrom
		{
			get { return _ReceivedFrom; }
		}

		#endregion

	}
}
