using System;

namespace Rssdp.Infrastructure
{
	/// <summary>
	/// Interface for components that discover the existence of SSDP devices.
	/// </summary>
	/// <remarks>
	/// <para>Discovering devices includes explicit search requests as well as listening for broadcast status notifications.</para>
	/// </remarks>
	/// <seealso cref="DiscoveredSsdpDevice"/>
	/// <seealso cref="SsdpDevice"/>
	/// <seealso cref="ISsdpDevicePublisher"/>
	public interface ISsdpDeviceLocator
	{

		#region Events

		/// <summary>
		/// Event raised when a device becomes available or is found by a search request.
		/// </summary>
		/// <seealso cref="NotificationFilter"/>
		/// <seealso cref="DeviceUnavailable"/>
		/// <seealso cref="StartListeningForNotifications"/>
		/// <seealso cref="StopListeningForNotifications"/>
		event EventHandler<DeviceAvailableEventArgs> DeviceAvailable;
		
		/// <summary>
		/// Event raised when a device explicitly notifies of shutdown or a device expires from the cache.
		/// </summary>
		/// <seeseealso cref="NotificationFilter"/>
		/// <seealso cref="DeviceAvailable"/>
		/// <seealso cref="StartListeningForNotifications"/>
		/// <seealso cref="StopListeningForNotifications"/>
		event EventHandler<DeviceUnavailableEventArgs> DeviceUnavailable;

		#endregion

		#region Properties

		/// <summary>
		/// Sets or returns a string containing the filter for notifications. Notifications not matching the filter will not raise the <see cref="DeviceAvailable"/> or <see cref="DeviceUnavailable"/> events.
		/// </summary>
		/// <remarks>
		/// <para>Device alive/byebye notifications whose NT header does not match this filter value will still be captured and cached internally, but will not raise events about device availability. Usually used with either a device type of uuid NT header value.</para>
		/// <para>Example filters follow;</para>
		/// <example>upnp:rootdevice</example>
		/// <example>urn:schemas-upnp-org:device:WANDevice:1</example>
		/// <example>"uuid:9F15356CC-95FA-572E-0E99-85B456BD3012"</example>
		/// </remarks>
		/// <seealso cref="DeviceAvailable"/>
		/// <seealso cref="DeviceUnavailable"/>
		/// <seealso cref="StartListeningForNotifications"/>
		/// <seealso cref="StopListeningForNotifications"/>
		string NotificationFilter
		{
			get;
			set;
		}

		/// <summary>
		/// Returns a boolean indicating whether or not a search is currently active.
		/// </summary>
		bool IsSearching { get; }

		#endregion

		#region Methods

		#region SearchAsync Overloads

		/// <summary>
		/// Aynchronously performs a search for all devices using the default search timeout, and returns an awaitable task that can be used to retrieve the results.
		/// </summary>
		/// <returns>A task whose result is an <see cref="System.Collections.Generic.IEnumerable{T}"/> of <see cref="DiscoveredSsdpDevice" /> instances, representing all found devices.</returns>
		System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<DiscoveredSsdpDevice>> SearchAsync();

		/// <summary>
		/// Performs a search for the specified search target (criteria) and default search timeout.
		/// </summary>
		/// <param name="searchTarget">The criteria for the search. Value can be;
		/// <list type="table">
		/// <item><term>Root devices</term><description>upnp:rootdevice</description></item>
		/// <item><term>Specific device by UUID</term><description>uuid:&lt;device uuid&gt;</description></item>
		/// <item><term>Device type</term><description>Fully qualified device type starting with urn: i.e urn:schemas-upnp-org:Basic:1</description></item>
		/// </list>
		/// </param>
		/// <returns>A task whose result is an <see cref="System.Collections.Generic.IEnumerable{T}"/> of <see cref="DiscoveredSsdpDevice" /> instances, representing all found devices.</returns>
		System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<DiscoveredSsdpDevice>> SearchAsync(string searchTarget);

		/// <summary>
		/// Performs a search for the specified search target (criteria) and search timeout.
		/// </summary>
		/// <param name="searchTarget">The criteria for the search. Value can be;
		/// <list type="table">
		/// <item><term>Root devices</term><description>upnp:rootdevice</description></item>
		/// <item><term>Specific device by UUID</term><description>uuid:&lt;device uuid&gt;</description></item>
		/// <item><term>Device type</term><description>A device namespace and type in format of urn:&lt;device namespace&gt;:device:&lt;device type&gt;:&lt;device version&gt; i.e urn:schemas-upnp-org:device:Basic:1</description></item>
		/// <item><term>Service type</term><description>A service namespace and type in format of urn:&lt;service namespace&gt;:service:&lt;servicetype&gt;:&lt;service version&gt; i.e urn:my-namespace:service:MyCustomService:1</description></item>
		/// </list>
		/// </param>
		/// <param name="searchWaitTime">The amount of time to wait for network responses to the search request. Longer values will likely return more devices, but increase search time. A value between 1 and 5 is recommended by the UPnP 1.1 specification. Specify TimeSpan.Zero to return only devices already in the cache.</param>
		/// <remarks>
		/// <para>By design RSSDP does not support 'publishing services' as it is intended for use with non-standard UPnP devices that don't publish UPnP style services. However, it is still possible to use RSSDP to search for devices implemetning these services if you know the service type.</para>
		/// </remarks>
		/// <returns>A task whose result is an <see cref="System.Collections.Generic.IEnumerable{T}"/> of <see cref="DiscoveredSsdpDevice" /> instances, representing all found devices.</returns>
		System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<DiscoveredSsdpDevice>> SearchAsync(string searchTarget, TimeSpan searchWaitTime);

		/// <summary>
		/// Performs a search for all devices using the specified search timeout.
		/// </summary>
		/// <param name="searchWaitTime">The amount of time to wait for network responses to the search request. Longer values will likely return more devices, but increase search time. A value between 1 and 5 is recommended by the UPnP 1.1 specification. Specify TimeSpan.Zero to return only devices already in the cache.</param>
		/// <returns>A task whose result is an <see cref="System.Collections.Generic.IEnumerable{T}"/> of <see cref="DiscoveredSsdpDevice" /> instances, representing all found devices.</returns>
		System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<DiscoveredSsdpDevice>> SearchAsync(TimeSpan searchWaitTime);

		#endregion
				
		/// <summary>
		/// Starts listening for broadcast notifications of service availability.
		/// </summary>
		/// <remarks>
		/// <para>When called the system will listen for 'alive' and 'byebye' notifications. This can speed up searching, as well as provide dynamic notification of new devices appearing on the network, and previously discovered devices disappearing.</para>
		/// </remarks>
		/// <seealso cref="StopListeningForNotifications"/>
		/// <seealso cref="DeviceAvailable"/>
		/// <seealso cref="DeviceUnavailable"/>
		/// <seealso cref="NotificationFilter"/>
		void StartListeningForNotifications();

		/// <summary>
		/// Stops listening for broadcast notifications of service availability.
		/// </summary>
		/// <remarks>
		/// <para>Does nothing if this instance is not already listening for notifications.</para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">Throw if the <see cref="DisposableManagedObjectBase.IsDisposed"/> property is true.</exception>
		/// <seealso cref="StartListeningForNotifications"/>
		/// <seealso cref="DeviceAvailable"/>
		/// <seealso cref="DeviceUnavailable"/>
		/// <seealso cref="NotificationFilter"/>
		void StopListeningForNotifications();

		#endregion

	}
}