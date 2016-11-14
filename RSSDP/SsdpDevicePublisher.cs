using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Threading;
using Rssdp.Infrastructure;

namespace Rssdp
{
	/// <summary>
	/// Allows publishing devices both as notification and responses to search requests.
	/// </summary>
	/// <remarks>
	/// This is  the 'server' part of the system. You add your devices to an instance of this class so clients can find them.
	/// </remarks>
	public class SsdpDevicePublisher : SsdpDevicePublisherBase
	{

		#region Constructors

		/// <summary>
		/// Default constructor. 
		/// </summary>
		/// <remarks>
		/// <para>Uses the default <see cref="ISsdpCommunicationsServer"/> implementation and network settings for Windows and the SSDP specification.</para>
		/// </remarks>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "No way to do this here, and we don't want to dispose it except in the (rare) case of an exception anyway.")]
		public SsdpDevicePublisher(ISsdpCommunicationsServer communicationsServer, ITimerFactory timerFactory, string osName, string osVersion)
			: base(communicationsServer, timerFactory, osName, osVersion)
		{

		}

		#endregion

    }
}