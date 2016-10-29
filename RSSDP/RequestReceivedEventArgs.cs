using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Rssdp.Infrastructure
{
	/// <summary>
	/// Provides arguments for the <see cref="ISsdpCommunicationsServer.RequestReceived"/> event.
	/// </summary>
	public sealed class RequestReceivedEventArgs : EventArgs
	{

		#region Fields

		private readonly HttpRequestMessage _Message;
		private readonly UdpEndPoint _ReceivedFrom;

		#endregion

		#region Constructors

		/// <summary>
		/// Full constructor.
		/// </summary>
		/// <param name="message">The <see cref="HttpRequestMessage"/> that was received.</param>
		/// <param name="receivedFrom">A <see cref="UdpEndPoint"/> representing the sender's address (sometimes used for replies).</param>
		public RequestReceivedEventArgs(HttpRequestMessage message, UdpEndPoint receivedFrom)
		{
			_Message = message;
			_ReceivedFrom = receivedFrom;
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
		public UdpEndPoint ReceivedFrom
		{
			get { return _ReceivedFrom; }
		}

		#endregion

	}
}