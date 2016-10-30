using System;

namespace Rssdp.Infrastructure
{
	/// <summary>
	/// Interface for components that check an <see cref="SsdpDevice"/> object's properties meet the UPnP specification for a particular version.
	/// </summary>
	public interface IUpnpDeviceValidator
	{
		/// <summary>
		/// Returns an enumerable set of strings, each one being a description of an invalid property on the specified root device.
		/// </summary>
		/// <param name="device">The <see cref="SsdpRootDevice"/> to validate.</param>
		System.Collections.Generic.IEnumerable<string> GetValidationErrors(SsdpRootDevice device);

		/// <summary>
		/// Returns an enumerable set of strings, each one being a description of an invalid property on the specified device.
		/// </summary>
		/// <param name="device">The <see cref="SsdpDevice"/> to validate.</param>
		System.Collections.Generic.IEnumerable<string> GetValidationErrors(SsdpDevice device);

		/// <summary>
		/// Validates the specified device and throws an <see cref="System.InvalidOperationException"/> if there are any validation errors.
		/// </summary>
		void ThrowIfDeviceInvalid(SsdpDevice device);
	}
}
