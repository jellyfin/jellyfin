using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
		public SsdpDevicePublisher()
			: this(new SsdpCommunicationsServer(new SocketFactory(null)))
		{

		}

		/// <summary>
		/// Full constructor. 
		/// </summary>
		/// <remarks>
		/// <para>Allows the caller to specify their own <see cref="ISsdpCommunicationsServer"/> implementation for full control over the networking, or for mocking/testing purposes..</para>
		/// </remarks>
		public SsdpDevicePublisher(ISsdpCommunicationsServer communicationsServer)
			: base(communicationsServer, GetOSName(), GetOSVersion())
		{

		}

		/// <summary>
		/// Partial constructor. 
		/// </summary>
		/// <param name="localPort">The local port to use for socket communications, specify 0 to have the system choose it's own.</param>
		/// <remarks>
		/// <para>Uses the default <see cref="ISsdpCommunicationsServer"/> implementation and network settings for Windows and the SSDP specification, but specifies the local port to use for socket communications. Specify 0 to indicate the system should choose it's own port.</para>
		/// </remarks>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "No way to do this here, and we don't want to dispose it except in the (rare) case of an exception anyway.")]
		public SsdpDevicePublisher(int localPort)
			: this(new SsdpCommunicationsServer(new SocketFactory(null), localPort))
		{

		}

		/// <summary>
		/// Partial constructor. 
		/// </summary>
		/// <param name="localPort">The local port to use for socket communications, specify 0 to have the system choose it's own.</param>
		/// <param name="multicastTimeToLive">The number of hops a multicast packet can make before it expires. Must be 1 or greater.</param>
		/// <remarks>
		/// <para>Uses the default <see cref="ISsdpCommunicationsServer"/> implementation and network settings for Windows and the SSDP specification, but specifies the local port to use and multicast time to live setting for socket communications.</para>
		/// <para>Specify 0 for the <paramref name="localPort"/> argument to indicate the system should choose it's own port.</para>
		/// <para>The <paramref name="multicastTimeToLive"/> is actually a number of 'hops' on the network and not a time based argument.</para>
		/// </remarks>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "No way to do this here, and we don't want to dispose it except in the (rare) case of an exception anyway.")]
		public SsdpDevicePublisher(int localPort, int multicastTimeToLive)
			: this(new SsdpCommunicationsServer(new SocketFactory(null), localPort, multicastTimeToLive))
		{
		}

		#endregion

		#region Private Methods

		private static string GetOSName()
		{
#if NET46
            return Environment.OSVersion.Platform.ToString();
#elif NETSTANDARD1_6
            return System.Runtime.InteropServices.RuntimeInformation.OSDescription;
#endif
            return "Operating System";
        }

        private static string GetOSVersion()
		{
#if NET46
            return Environment.OSVersion.Version.ToString() + " " + Environment.OSVersion.ServicePack.ToString();
#elif NETSTANDARD1_6
            return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
#endif
            return "1.0";
        }

#endregion

    }
}