using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rssdp
{
	/// <summary>
	/// Extensions for <see cref="SsdpDevice"/> and derived types.
	/// </summary>
	public static class SsdpDeviceExtensions
	{

		/// <summary>
		/// Returns the root device associated with a device instance derived from <see cref="SsdpDevice"/>.
		/// </summary>
		/// <param name="device">The device instance to find the <see cref="SsdpRootDevice"/> for.</param>
		/// <remarks>
		/// <para>The <paramref name="device"/> must be or inherit from <see cref="SsdpRootDevice"/> or <see cref="SsdpEmbeddedDevice"/>, otherwise an <see cref="System.InvalidCastException"/> will occur.</para>
		/// <para>May return null if the <paramref name="device"/> instance is an embedded device not yet associated with a <see cref="SsdpRootDevice"/> instance yet.</para>
		/// <para>If <paramref name="device"/> is an instance of <see cref="SsdpRootDevice"/> (or derives from it), returns the same instance cast to <see cref="SsdpRootDevice"/>.</para>
		/// </remarks>
		/// <returns>The <see cref="SsdpRootDevice"/> instance associated with the device instance specified, or null otherwise.</returns>
		/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="device"/> is null.</exception>
		/// <exception cref="System.InvalidCastException">Thrown if <paramref name="device"/> is not an instance of or dervied from either <see cref="SsdpRootDevice"/> or <see cref="SsdpEmbeddedDevice"/>.</exception>
		public static SsdpRootDevice ToRootDevice(this SsdpDevice device)
		{
			if (device == null) throw new System.ArgumentNullException("device");

			var rootDevice = device as SsdpRootDevice;
			if (rootDevice == null)
				rootDevice = ((SsdpEmbeddedDevice)device).RootDevice;

			return rootDevice;
		}
	}
}
